using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 얼음 거인 보스 (IceGiantBoss)
/// - 플레이어 실시간 추적 및 이동 중 무작위 얼음 파편 사격
/// - 사거리 진입 시 경고 장판(전조) 표시 후 강한 내려찍기 + 원형 파편 복합 패턴 시전
/// - [신규 기믹] 원형 파편 사격 시 매번 각도를 반 칸씩 틀어 쏘는 교차(지그재그) 방사 시스템 도입
/// - [1페이즈] 체력 50% 이하: 이동 속도가 증가하는 상시 광폭화 돌입
/// - [2페이즈] 체력 20% 이하: 스테이지가 끝날 때까지 부하 몬스터를 무한 소환하는 모드 가동
/// - [사망 처리] 사망 시 자가 생성한 모든 탄막과 소환 몹을 화면에서 일괄 소거 (오버랩 버그 방지)
/// </summary>
public class IceGiantBoss : BossBase
{
    [Header("오브젝트 풀 탄막 인덱스")]
    [SerializeField] int slamBulletIndex;     // 내려찍기 충격파(장판) 프리패브 인덱스
    [SerializeField] int shardBulletIndex;    // 얼음 파편 투사체 프리패브 인덱스

    [Header("범위 전조(Telegraph)")]
    [SerializeField] GameObject warningCircle; // 공격 전 타격 범위를 미리 보여줄 경고 원형 가이드

    [Header("내려찍기 패턴 설정")]
    [SerializeField] float slamCooldown = 5f; // 패턴 재사용 대기 시간
    [SerializeField] float slamRange = 4f;    // 패턴 발동 조건 거리 (사거리)
    [SerializeField] float warningTime = 1.2f; // 경고 장판 노출 지속 시간 (선딜레이)

    [Header("이동 중 파편 사격 설정")]
    [SerializeField] float shardChance = 0.015f; // Update 루프당 얼음 파편 원형 발사 확률
    [SerializeField] int shardCount = 8;         // 원형 발사 시 방사형으로 퍼질 파편 개수 (360도 분할)

    [Header("광폭화 페이즈 설정")]
    [SerializeField] float enragedSpeedMultiplier = 1.5f; // 체력 50% 이하 시 이동 속도 증폭 배율

    [Header("부하 몬스터 소환 설정")]
    [SerializeField] int[] summonEnemyIndexes;      // 소환 풀에 등록된 잡몹들의 인덱스 배열
    [SerializeField] float summonInterval = 0.75f; // 소환 루프 주기 (초 단위)
    [SerializeField] float summonRadius = 4f;      // 보스 중심 기준 스폰 가능한 무작위 반경

    bool summonMode;             // 2페이즈(체력 20% 이하 소환 모드) 진입 확인 플래그
    bool alternateShardPattern;  // 💡 [신규] 탄막의 궤적을 지그재그(교차) 형태로 번갈아 쏘기 위한 스위치 플래그

    float slamTimer;         // 내려찍기 쿨타임 카운트용 타이머
    bool isCasting;          // 현재 스킬 시전 중인지 여부 (이동 및 타 패턴 캔슬 방지)
    float originMoveSpeed;   // 디버프 또는 페이즈 해제 시 롤백을 위한 원본 기본 이속 저장소

    PoolManager pool;        // 글로벌 오브젝트 풀 매니저 캐싱 참조
    Collider2D bossCollider; // 보스 자체의 충돌체 (투사체 자가 충돌 방지용)

    // 💡 [메모리 및 버그 관리] 보스가 죽었을 때 맵에 남아 유저를 공격하는 잔여물을 지우기 위한 추적 리스트
    List<GameObject> spawnedBullets = new List<GameObject>();  // 보스가 발사한 모든 탄막 추적
    List<GameObject> summonedEnemies = new List<GameObject>(); // 보스가 소환한 모든 잡몹 추적

    // ============================================================
    // 초기화 및 활성화 시점 처리
    // ============================================================

    protected override void OnEnable()
    {
        base.OnEnable();

        // 런타임 가변 상태 변수 초기화
        slamTimer = 0f;
        isCasting = false;
        summonMode = false;
        alternateShardPattern = false; // 교차 플래그 초기화

        // 풀 재사용(오브젝트 풀링) 환경에서 이전 세션 데이터가 섞이지 않도록 리스트 완전 정화
        spawnedBullets.Clear();
        summonedEnemies.Clear();

        // 밥줄 컴포넌트 및 싱글톤 주소 캐싱 (성능 최적화)
        pool = GameManager.instance.pool;
        originMoveSpeed = moveSpeed;
        bossCollider = GetComponent<Collider2D>();

        // 최초 기동 시 전조 장판 숨김 안전 처리
        if (warningCircle != null)
            warningCircle.SetActive(false);
    }

    // ============================================================
    // 보스 핵심 상태 머신 및 AI 제어 루프
    // ============================================================

    protected override void Update()
    {
        // 타겟(플레이어)이 유실되었거나 사망 상태인 경우 AI 완전 중지
        if (target == null) return;

        // 내려찍기 쿨타임 타이머 누적
        slamTimer += Time.deltaTime;

        // [조건 체크 1] 체력 50% 이하인 경우 상시 광폭화 이동 속도 적용
        moveSpeed = health <= maxHealth * 0.5f
            ? originMoveSpeed * enragedSpeedMultiplier
            : originMoveSpeed;

        // [조건 체크 2] 체력 20% 이하 시 무한 잡몹 소환 코루틴 최초 1회 트리거
        if (!summonMode && health <= maxHealth * 0.2f)
        {
            summonMode = true;
            StartCoroutine(SummonLoop());
        }

        // 현재 장판 패턴을 캐스팅(시전) 중인 상태라면 추적 이동 및 타 행동을 전면 차단
        if (isCasting) return;

        // 플레이어와의 2D 직선 거리 연산
        float dist = Vector2.Distance(transform.position, target.position);

        // 플레이어 추적 및 이동 처리 (canMove 상태 및 유효 속도 확인)
        if (canMove && moveSpeed > 0)
        {
            Vector2 dir = (target.position - transform.position).normalized;
            transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
        }

        // [패턴 판정] 타겟이 사거리 안에 들어왔고, 쿨타임이 충족된 경우 내려찍기 시전
        if (dist <= slamRange && slamTimer >= slamCooldown)
        {
            StartCoroutine(SlamPattern());
            return;
        }

        // 추적 이동 도중 프레임마다 주어지는 확률에 따라 무작위 얼음 파편 원형 사격 가동
        if (Random.value <= shardChance)
            FireShardCircle();
    }

    // 부모 클래스(BossBase)의 공통 랜덤 패턴 스케줄러가 작동하지 않도록 오버라이드하여 무력화
    protected override void StartRandomPattern() { }

    // ============================================================
    // 메인 스킬: 내려찍기 (장판 + 탄막 복합 패턴)
    // ============================================================

    IEnumerator SlamPattern()
    {
        // 제어권 잠금: 이동 및 타 패턴 진입 불가 상태 지정
        isCasting = true;
        isPatternPlaying = true;
        canMove = false;

        // 임시 속도 백업 후 완전히 정지
        float prevSpeed = moveSpeed;
        moveSpeed = 0f;

        // 애니메이터 이동 상태 롤백 및 파라미터 안전 리셋
        if (anim != null)
        {
            anim.SetInteger("Moving", 0);
            anim.ResetTrigger("Attack");
        }

        // 1단계: 플레이어에게 공격 영역 시각화 (경고 원 활성화)
        if (warningCircle != null)
            warningCircle.SetActive(true);

        // 공격 전조 대기 시간 (선딜레이)
        yield return new WaitForSeconds(warningTime);

        // 2단계: 선딜이 끝나면 경고 원 비활성화 및 타격 프로세스 가동
        if (warningCircle != null)
            warningCircle.SetActive(false);

        // 공격 애니메이션 트리거 가동
        if (anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.SetTrigger("Attack");
        }

        // 연산의 정확성을 기하기 위해 보스의 현재 기준 위치 백업
        Vector3 spawnOrigin = transform.position;

        // 3단계: 오브젝트 풀에서 내려찍기 대형 장판 판정 탄막 로드 및 배치
        GameObject slam = pool.GetBossBullet(slamBulletIndex);
        slam.transform.position = spawnOrigin;

        // 추후 일괄 소거를 위해 탄막 추적 리스트에 수집
        spawnedBullets.Add(slam);

        // 보스가 소환한 부하들이나 자기 자신과 충돌하여 지워지는 현상 방지
        IgnoreEnemyCollision(slam);

        // 4단계: 내려찍기 충격과 동시에 주변으로 뻗어나가는 방사형 얼음 파편 생성
        FireShardCircleFromPoint(spawnOrigin);

        // 내부 쿨타임 타이머 리셋
        slamTimer = 0f;

        // 공격 후 경직 시간 (후딜레이)
        yield return new WaitForSeconds(0.7f);

        // 제어권 반환: 이동 속도 복구 및 AI 상태 복원
        moveSpeed = prevSpeed;
        canMove = true;
        isPatternPlaying = false;
        isCasting = false;
    }

    // ============================================================
    // 서브 스킬: 방사형 얼음 파편 원형 발사 시스템 (지그재그 교차형)
    // ============================================================

    /// <summary>
    /// 지정된 중심점 좌표를 기준으로 360도를 등분하여 사방으로 얼음 파편을 사격하는 함수
    /// </summary>
    void FireShardCircleFromPoint(Vector3 centerOrigin)
    {
        if (pool == null) return;

        // 탄과 탄 사이의 기본 간격 각도 (ex: 8발이면 45도)
        float angleStep = 360f / shardCount;

        // 💡 [교차 알고리즘] 이번 발사 차례가 alternate 상태라면 기본 각도에서 딱 '반 칸(기본 간격의 절반)'을 밀어줍니다.
        // 이를 통해 이전 발사 때의 안전지대가 이번에는 공격 지대가 되도록 보완합니다.
        float angleOffset = alternateShardPattern ? angleStep * 0.5f : 0f;

        for (int i = 0; i < shardCount; i++)
        {
            // 순번 기준 각도에 오프셋을 더하고 삼각함수용 라디안(Rad)으로 변환
            float angle = (i * angleStep + angleOffset) * Mathf.Deg2Rad;

            // 방향 벡터 연산
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // 오브젝트 풀에서 투사체 개체 꺼내기
            GameObject shard = pool.GetBossBullet(shardBulletIndex);
            shard.transform.position = centerOrigin;

            // 탄막 고유 스크립트에 이동 방향 주입
            shard.GetComponent<BossBullet>().Init(dir);

            // 투사체의 진행 방향에 맞게 스프라이트 회전 각도 조정 (+180은 리소스 텍스처 방향 보정값)
            float degree = (i * angleStep + angleOffset) + 180f;
            shard.transform.rotation = Quaternion.Euler(0, 0, degree);

            // 아군 몬스터들과의 오인 사격 충돌 무시 예외 처리
            IgnoreEnemyCollision(shard);

            // 🎯 [버그 수정 완료] 기존 코드에서 누락되었던 소거 리스트 등록 코드 복구. 
            // 이 코드가 있어야 보스가 죽을 때 맵에 날아다니던 파편들이 깔끔하게 풀로 들어갑니다.
            spawnedBullets.Add(shard);
        }

        // 토글 플래그 반전: 다음 발사 때는 반대 형태(정방향 <-> 절반 오프셋)로 나가도록 스위칭
        alternateShardPattern = !alternateShardPattern;
    }

    /// <summary>
    /// 보스의 현재 위치에서 즉시 원형 탄막을 난사하는 헬퍼 함수
    /// </summary>
    void FireShardCircle()
    {
        FireShardCircleFromPoint(transform.position);
    }

    /// <summary>
    /// 생성된 보스 전용 투사체가 보스 본인 및 맵 상의 다른 일반 몬스터들과 충돌하여 소멸하는 현상을 방지하는 함수
    /// </summary>
    void IgnoreEnemyCollision(GameObject bullet)
    {
        Collider2D bulletCol = bullet.GetComponent<Collider2D>();
        if (bulletCol == null) return;

        // 1. 보스 본체 콜라이더와의 상호 충돌 예외 처리
        if (bossCollider != null)
            Physics2D.IgnoreCollision(bulletCol, bossCollider);

        // 2. 현재 월드상에 생존해 있는 모든 일반 적(Enemy)들과의 상호 충돌 무시 연산
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            Collider2D enemyCol = enemy.GetComponent<Collider2D>();
            if (enemyCol != null)
                Physics2D.IgnoreCollision(bulletCol, enemyCol);
        }
    }

    // ============================================================
    // 페이즈 2 기믹: 부하 몬스터 무한 스폰 코루틴
    // ============================================================

    IEnumerator SummonLoop()
    {
        // 보스 오브젝트가 월드에 활성화되어 있는 동안 무한 반복 작동
        while (gameObject.activeSelf)
        {
            // 이미 체력이 다해 사망 프로세스를 밟고 있다면 루프 강제 파괴
            if (isDead)
                yield break;

            SummonEnemy();

            // 설정된 주기마다 한 번씩 리스폰 수행
            yield return new WaitForSeconds(summonInterval);
        }
    }

    /// <summary>
    /// 지정된 인덱스 배열 풀에서 무작위 몬스터를 추려내어 보스 주변에 배치 및 활성화하는 함수
    /// </summary>
    void SummonEnemy()
    {
        if (pool == null) return;
        if (summonEnemyIndexes == null || summonEnemyIndexes.Length == 0) return;

        // 기획 데이터 배열에서 랜덤으로 소환할 몹의 ID(인덱스) 추적
        int enemyIndex = summonEnemyIndexes[Random.Range(0, summonEnemyIndexes.Length)];

        // 오브젝트 풀에서 인스턴스 획득
        GameObject enemy = pool.GetEnemy(enemyIndex);
        if (enemy == null) return;

        // 보스 좌표를 기반으로 원형 가상 구역 내의 무작위 스폰 포인트 연산
        Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * summonRadius;

        enemy.transform.position = spawnPos;
        enemy.SetActive(true);

        // 보스 사망 시 동시 클리어를 위해 소환 몹 리스트에 등재
        summonedEnemies.Add(enemy);
    }

    // ============================================================
    // 세션 종료: 사망 처리 및 필드 오브젝트 완전 청소
    // ============================================================

    protected override void Dead()
    {
        // 1. 보스가 생전에 발사하여 화면에 남아 유저를 저격하던 잔여 탄막/장판 전부 비활성화
        foreach (GameObject bullet in spawnedBullets)
        {
            if (bullet != null && bullet.activeSelf)
                bullet.SetActive(false);
        }
        spawnedBullets.Clear();

        // 2. 보스가 소환하여 필드에 남아 찌꺼기가 되던 부하 잡몹들 전부 강제 비활성화
        foreach (GameObject enemy in summonedEnemies)
        {
            if (enemy != null && enemy.activeSelf)
                enemy.SetActive(false);
        }
        summonedEnemies.Clear();

        // 3. 상위 부모 클래스(BossBase)의 기초 보상 드롭 및 웨이브 카운트 통보 가동
        base.Dead();
    }
}