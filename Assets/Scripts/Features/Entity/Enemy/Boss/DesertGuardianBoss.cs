using UnityEngine;

// 사막 보스 클래스 (부모 클래스 BossBase를 상속받음)
public class DesertGuardianBoss : BossBase
{
    [Header("탄 및 기믹 인덱스")]
    [SerializeField] int sandstormGimmickIndex; // 풀링 시스템에서 꺼낼 모래바람 기믹(SandstormGimmick)의 인덱스
    [SerializeField] int fanBulletIndex;        // 부채꼴 공격에 사용할 일반 탄(Bullet)의 인덱스

    [Header("모래바람 패턴 설정")]
    [SerializeField] float sandstormInterval = 3f; // 모래바람 기믹을 생성할 주기 (초 단위)

    [Header("워프(텔레포트) 설정")]
    [SerializeField] float warpDistance = 3f;   // 플레이어의 '진짜 뒤'로 이동할 거리
    [SerializeField] float verticalOffset = 2f; // 워프 시 위/아래로 분산될 최대 무작위 범위

    [Header("대기 상태 설정")]
    [SerializeField] float attackWaitTime = 1f; // 워프 및 공격 후 보스가 잠시 정지해 있는 시간

    [Header("시야 판정 설정")]
    [SerializeField] float frontAngle = 60f; // 플레이어가 보스를 바라본다고 판정할 전방 시야 각도 (반각)

    [Header("부채꼴 패턴 설정")]
    [SerializeField] int fanBulletCount = 7; // 부채꼴 공격 시 발사할 탄의 개수
    [SerializeField] float fanAngle = 100f;  // 부채꼴 탄막이 퍼지는 전체 각도

    [Header("부채꼴 발사 확률")]
    [SerializeField, Range(0f, 1f)]
    float fanAttackChance = 0.5f; // 워프 직후 부채꼴 탄막을 발사할 확률 (0.0 ~ 1.0)

    // 내부 상태 제어 변수들
    float sandstormTimer; // 모래바람 타이머 (시간 경과 누적)
    float waitTimer;      // 워프 후 대기 시간을 측정하기 위한 타이머
    bool isWaiting;       // 현재 보스가 정지(대기) 상태인지 여부
    bool wantsToWarp = false; // 대기 시간 도중 플레이어가 쳐다봐서 '예약된 텔레포트' 상태인지 확인하는 변수

    PoolManager pool; // 오브젝트 풀 매니저 참조 변수

    // 오브젝트가 활성화될 때마다 호출 (초기화 작업)
    protected override void OnEnable()
    {
        base.OnEnable(); // 부모 클래스의 OnEnable 로직 수행

        // 패턴 관련 타이머 및 상태 초기화
        sandstormTimer = 0f;
        waitTimer = 0f;
        isWaiting = false;

        // 게임 매니저 싱글톤을 통해 오브젝트 풀링 컴포넌트 연결
        pool = GameManager.instance.pool;
    }

    // 매 프레임마다 보스의 행동을 제어하는 업데이트 루프
    protected override void Update()
    {
        if (target == null)
            return;

        // 1. 모래바람 기믹 소환 패턴 (상태와 무관하게 항상 타이머 흐름)
        sandstormTimer += Time.deltaTime;
        if (sandstormTimer >= sandstormInterval)
        {
            sandstormTimer = 0f;
            SpawnSandstormGimmickCircle();
        }

        // 2. 시야 지속 체크 (대기 상태 중이더라도 시선을 체크하여 워프 예약)
        if (IsPlayerLookingAtBoss())
        {
            wantsToWarp = true; // 플레이어가 쳐다봄! 대기가 끝나면 즉시 워프하도록 플래그 켬
        }

        // 3. 워프 후 대기(정지) 상태 처리
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= attackWaitTime)
            {
                isWaiting = false;
                waitTimer = 0f;
            }
            return; // 아직 대기 중이라면 이 프레임은 여기서 종료 (워프 대기)
        }

        // 4. 즉시 워프 판정 (대기가 풀린 상태에서 방금 플레이어가 쳐다봤거나, 예약된 워프가 있을 때)
        if (wantsToWarp)
        {
            wantsToWarp = false;  // 플래그 초기화
            WarpAwayFromPlayer(); // 진짜 뒤로 즉시 워프

            // 확률 반격 (부채꼴)
            if (Random.value <= fanAttackChance)
                FireFanBullets();

            isWaiting = true; // 패턴 수행 후 다시 대기 상태 활성화
            return; // 워프 프레임에는 일반 행동 생략
        }

        // 위의 특수 패턴에 걸리지 않았다면 기본 보스 행동 수행
        base.Update();
    }

    // ========================================================
    // [시야 판정] Player 스크립트의 GetFacingDirection()을 활용하여 정확하게 계산
    // ========================================================
    bool IsPlayerLookingAtBoss()
    {
        // 보스의 위치를 향하는 방향 벡터 계산 (정규화)
        Vector2 toBoss = (transform.position - target.position).normalized;

        // [중요 수정] target.right(항상 오른쪽) 대신, Player 스크립트의 실제 시선/이동 방향을 가져옴
        Player playerScript = target.GetComponent<Player>();
        if (playerScript == null)
            return false;

        Vector2 forward = playerScript.GetFacingDirection();

        // 플레이어 시선 벡터와 보스를 향하는 벡터 사이의 각도 계산 (0 ~ 180도)
        float angle = Vector2.Angle(forward, toBoss);

        // 계산된 사잇각이 설정된 시야 범위(frontAngle)보다 작거나 같으면 쳐다보는 것으로 판정
        return angle <= frontAngle;
    }

    // 부모의 무작위 기본 패턴 시작 함수를 오버라이드 (여기서는 빈칸으로 두어 기본 행동 제어)
    protected override void StartRandomPattern() { }
    // ========================================================
    // [텔레포트] 플레이어 시선 방향 기준이 아닌, "진짜 뒤쪽" 공간으로 워프
    // ========================================================
    void WarpAwayFromPlayer()
    {
        if (target == null) return;

        // 1. 플레이어에서 현재 보스를 바라보는 벡터 계산
        Vector2 toBoss = (transform.position - target.position).normalized;

        // 2. 그 반대 방향을 '진짜 뒤쪽' 기준 방향으로 설정 (플레이어 시선과 상관없이 보스가 있던 반대편 고수)
        Vector2 backDir = -toBoss;

        // 3. 뒤쪽 방향 벡터에 수직인 벡터 계산 (수직 정렬용 위치 분산 분기 생성)
        Vector2 perp = new Vector2(-backDir.y, backDir.x);

        // 4. 완전 무작위 좌/우 분산 값 및 위/아래 추가 랜덤 오프셋 계산
        float side = Random.Range(-1f, 1f);
        float vertical = Random.Range(-verticalOffset, verticalOffset);

        // 5. 최종 목적지 좌표 조립: 플레이어 위치 + 뒤쪽 거리 + 수직 좌우 오프셋 조합
        Vector3 basePos = (Vector2)target.position
                          + backDir * warpDistance
                          + perp * side * verticalOffset
                          + perp * vertical * 0.3f;

        // 보스의 위치를 최종 계산된 워프 좌표로 즉시 이동
        transform.position = basePos;
    }

    // ========================================================
    // [8방향 모래바람 기믹 생성] 
    // ========================================================
    void SpawnSandstormGimmickCircle()
    {
        if (pool == null)
            return;

        // 360도를 8방향으로 나누어 (45도 간격) 기믹 오브젝트 배치
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f; // 0, 45, 90, 135 ...

            // 라디안 변환을 통해 방향 벡터(X, Y) 도출
            Vector2 dir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            // 오브젝트 풀에서 모래바람 기믹 꺼내오기
            GameObject gimmickObj = pool.GetBossBullet(sandstormGimmickIndex);

            if (gimmickObj != null)
            {
                // 생성 위치를 보스 위치로 맞춤
                gimmickObj.transform.position = transform.position;

                // 💡 [수정됨] SandstormGimmick 컴포넌트를 가져와서 Init 함수로 방향을 쏴줌!
                SandstormGimmick gimmickScript = gimmickObj.GetComponent<SandstormGimmick>();
                if (gimmickScript != null)
                {
                    gimmickScript.Init(dir);
                }
            }
        }
    }

    // ========================================================
    // [부채꼴 탄막 패턴] 워프 성공 시 확률적으로 플레이어를 향해 부채꼴 발사
    // ========================================================
    void FireFanBullets()
    {
        if (pool == null || target == null)
            return;

        // 보스 위치에서 플레이어 위치를 향하는 중앙 조준 방향 벡터 계산
        Vector2 dir = (target.position - transform.position).normalized;

        // 중앙 조준 방향의 각도(오프셋 기준점) 산출
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 부채꼴이 시작될 가장 왼쪽(혹은 오른쪽) 시작 각도 계산
        float startAngle = baseAngle - fanAngle * 0.5f;

        // 탄과 탄 사이의 각도 간격 계산 (탄 개수가 1개보다 많을 때 분할)
        float step = fanBulletCount > 1
            ? fanAngle / (fanBulletCount - 1)
            : 0f;

        // 설정된 개수만큼 순회하며 탄막 생성 및 발사 방향 할당
        for (int i = 0; i < fanBulletCount; i++)
        {
            // 순서에 따른 최종 발사 각도 계산
            float angle = startAngle + step * i;

            // 각도를 삼각함수를 통해 방향 벡터로 변환
            Vector2 bulletDir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            // 오브젝트 풀에서 부채꼴 전용 일반 보스 탄막을 가져옴
            GameObject b = pool.GetBossBullet(fanBulletIndex);

            if (b != null)
            {
                // 탄막의 발사 시작 위치를 보스의 현재 위치로 지정
                b.transform.position = transform.position;

                // 탄막 스크립트(BossBullet)를 가져와 계산된 방향 벡터로 초기화 및 날아가게 처리
                b.GetComponent<BossBullet>().Init(bulletDir);
            }
        }
    }
}