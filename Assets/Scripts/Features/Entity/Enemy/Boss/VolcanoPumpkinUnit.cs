using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화산 호박 공통 유닛 (대장 + 소대원 통합 제어 스크립트)
/// 위치 프레임 기록 기반 추종 이동, 리더 사망 승계 및 대규모 재배치(순간이동) 감지 포함
/// </summary>
public class VolcanoPumpkinUnit : BossBase
{
    [Header("코어")]
    [SerializeField] protected VolcanoPumpkinCore core; // 소대 전체 생존 및 순간이동 동기화를 관리하는 코어

    [Header("유도탄")]
    [SerializeField] protected int homingBulletIndex; // 오브젝트 풀에서 꺼낼 유도탄 인덱스
    [SerializeField] protected int bulletCount = 3;     // 1회당 발사할 유도탄 개수
    [SerializeField] protected float bulletInterval = 0.15f; // 탄막 발사 간격
    [SerializeField] protected float bulletSpawnOffset = 1f; // 본체 중심 기준 탄막 스폰 거리 오프셋

    [Header("리더 설정")]
    public bool isLeader; // 현재 이 객체가 소대의 머리(대장)인지 여부

    [SerializeField] int followerPrefabIndex; // 소환할 소대원 프리팹 인덱스
    [SerializeField] float followerSpacing = 1.5f; // 최초 생성 및 재배치 시 소대원 간 격차 거리

    [Header("몬스터 소환")]
    [SerializeField] int summonEnemyIndex; // 소환 패턴 시 등장할 일반 몬스터 인덱스
    [SerializeField] int summonCount = 3;      // 1회당 소환할 몬스터 수
    [SerializeField] float summonRadius = 2f;  // 소환 반경

    [Header("추종 설정 (소대원 전용)")]
    [SerializeField] float followSpeed = 5f;   // 앞 개체를 쫓아가는 이동 속도
    [SerializeField] int historyOffset = 25;   // 앞 개체의 몇 프레임 전 좌표를 타겟팅할 것인가 (꼬리 간격 조절)
    [SerializeField] int maxHistoryCount = 300; // 위치 기록 리스트의 최대 저장 한계선

    // 앞 개체 참조 변수 (리더는 최전방이므로 null)
    VolcanoPumpkinUnit frontUnit;

    // 자신의 과거 위치들을 실시간 기록하는 큐 배열
    readonly List<Vector2> positionHistory = new List<Vector2>();

    // 소대원 중복 생성 방지 플래그 (리더 전용)
    bool squadSpawned;

    // 🎯 [재배치 기믹 변수] 리더가 순간이동(강제 배치 등)했는지 감지하기 위한 위치 백업 변수
    Vector2 lastPosition;
    [Header("재배치 감지")]
    [SerializeField] float repositionThreshold = 3f; // 이 거리 이상 프레임 갭이 벌어지면 '순간이동'으로 판단

    // 프로퍼티: 외부(소대원/코어)에서 리더의 시선 방향을 실시간 동기화하기 위한 참조 필드
    public bool IsFlipX => spriter != null && spriter.flipX;

    // Inspector에 세팅된 원본 isLeader 값 (풀 재사용 시 복원용)
    bool defaultIsLeader;

    // -------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();
        // Awake 시점에 Inspector 원본값 저장 (최초 1회)
        defaultIsLeader = isLeader;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // 풀 재사용 시 승계로 변경된 isLeader를 원본값으로 복원
        isLeader = defaultIsLeader;
        frontUnit = null;

        // 위치 백업 리스트 초기화 및 현재 최초 스폰 좌표 주입
        positionHistory.Clear();
        positionHistory.Add(transform.position);

        squadSpawned = false;
        lastPosition = transform.position; // 최초 재배치 감지용 좌표 세팅

        core?.RegisterUnit(this); // 중앙 코어 매니저 명단에 자신을 생존 멤버로 등록

        // 태초의 대장으로 설정된 개체라면 즉시 뒤를 따를 소대원(꼬리)들 스폰
        if (isLeader)
            SpawnFollowers();
    }

    void LateUpdate()
    {
        if (isDead)
            return;

        // 최신 위치를 항상 리스트의 가장 앞(0번 index)에 삽입
        positionHistory.Insert(0, transform.position);

        // 설정된 최대 기록 범위를 초과하면 가장 오래된 좌표부터 큐 배열에서 제거
        if (positionHistory.Count > maxHistoryCount)
            positionHistory.RemoveAt(positionHistory.Count - 1);

        // 🎯 [리더 전용] 프레임 간 이동 거리를 계산하여 대규모 맵 강제 이동(순간이동) 감지
        if (isLeader)
        {
            Vector2 currentPos = transform.position;

            // 연산 효율을 위해 제곱근 연산(Vector2.Distance) 대신 스퀘어 매그니추드 비교 활용
            if ((currentPos - lastPosition).sqrMagnitude > repositionThreshold * repositionThreshold)
            {
                // 임계값을 넘는 순간이동 포착 시: 코어를 통해 살아있는 소대원 전체를 내 뒤로 즉시 강제 텔레포트 정렬
                core?.RepositionFollowers(currentPos, followerSpacing, IsFlipX);
            }

            // 실시간 프레임 위치 업데이트 백업
            lastPosition = currentPos;
        }
    }

    protected override void Update()
    {
        // 소대원은 패턴 타이머 독자 운용 금지 (리더와 별개로 패턴이 터지는 현상 방지)
        if (!isLeader)
            return;

        base.Update();
    }

    protected override void FixedUpdate()
    {
        if (!GameManager.instance.isLive || isDead)
            return;

        // 1. 대장(리더)인 경우의 이동 처리
        if (isLeader)
        {
            // 리더는 BossBase에 내장된 독자적인 AI 추적 및 물리 직접 이동 로직을 수행합니다.
        }
        // 2. 소대원(추종자)인 경우의 이동 처리
        else
        {
            // 시선 동기화: 바로 앞 개체의 좌우 반전 상태를 복사하여 일체감 유지
            if (frontUnit != null)
                spriter.flipX = frontUnit.IsFlipX;

            // 행동 불가 상태 시 관성 정지 + 위치 고정 (충돌 밀림 방지)
            if (!canMove)
            {
                rigid.linearVelocity = Vector2.zero;
                rigid.MovePosition(rigid.position); // 외부 충돌에 의한 밀림 차단
                return;
            }

            // [기차놀이 공식] 앞 개체가 과거에 지나갔던 자취 좌표를 타겟으로 지정
            Vector2 targetPos = frontUnit != null
                ? frontUnit.GetHistoryPosition(historyOffset)
                : (Vector2)transform.position;

            Vector2 dir = targetPos - rigid.position;

            // 타겟 지점에 거의 도달했다면 정지 및 대기 애니메이션 전환
            if (dir.sqrMagnitude < 0.01f)
            {
                rigid.linearVelocity = Vector2.zero;
                anim?.SetInteger("Moving", 0);
                return;
            }

            // 물리 직접 이동 처리 및 이동 애니메이션 가동
            rigid.MovePosition(rigid.position + dir.normalized * followSpeed * Time.fixedDeltaTime);
            anim?.SetInteger("Moving", 1);

            // 중요: 소대원은 BossBase의 독자 추적 연산을 우회해야 하므로 여기서 물리 강제 리턴
            return;
        }

        // 리더는 BossBase 본연의 추적 이동 로직 작동
        base.FixedUpdate();
    }

    // -------------------------------------------------------
    // 패턴 실행 세션
    // -------------------------------------------------------
    protected override void StartRandomPattern()
    {
        if (isLeader)
        {
            int pattern = Random.Range(0, 2);
            switch (pattern)
            {
                case 0:
                    StartCoroutine(FireHomingRoutine());
                    // 소대원들도 동시에 유도탄 발사
                    core?.BroadcastFire();
                    break;
                case 1:
                    StartCoroutine(SummonEnemyRoutine());
                    break;
            }
        }
    }

    // 소대원 전용 외부 발사 트리거 (Core에서 호출)
    public void TriggerFire()
    {
        if (isDead || isPatternPlaying) return;
        StartCoroutine(FireHomingRoutine());
    }

    // [패턴 1] 플레이어 조준 유도 탄막 연사 코루틴
    IEnumerator FireHomingRoutine()
    {
        isPatternPlaying = true;
        canMove = false; // 패턴 진행 도중 제자리 말뚝 고정

        if (target == null)
        {
            canMove = true;
            isPatternPlaying = false;
            yield break;
        }

        for (int i = 0; i < bulletCount; i++)
        {
            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            Vector2 spawnPos = (Vector2)transform.position + dir * bulletSpawnOffset; // 투사체 발사체 구체 중심 배치

            anim?.SetTrigger("Attack"); // 공격 트리거 발동

            GameObject bullet = PoolManager.Instance.GetBossBullet(homingBulletIndex);

            if (bullet != null)
            {
                bullet.transform.position = spawnPos;

                BossBullet bossBullet = bullet.GetComponent<BossBullet>();
                bossBullet?.Init(dir);

                // 코어 명단에 탄막 등록
                core?.RegisterBullet(bullet);
            }

            yield return new WaitForSeconds(bulletInterval); // 연사 딜레이
        }

        yield return new WaitForSeconds(0.3f); // 부드러운 전환을 위한 후딜레이

        canMove = true;
        isPatternPlaying = false;
    }

    // [패턴 2] 일반 몬스터 부하 소환 코루틴 (리더 전용)
    IEnumerator SummonEnemyRoutine()
    {
        isPatternPlaying = true;
        canMove = false;

        anim?.SetTrigger("Attack");

        for (int i = 0; i < summonCount; i++)
        {
            GameObject enemy = PoolManager.Instance.GetEnemy(summonEnemyIndex);

            if (enemy != null)
            {
                // 자신 주변의 무작위 반경 좌표를 연산하여 스폰 안착
                Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * summonRadius;
                enemy.transform.position = pos;
                core?.RegisterEnemy(enemy); // 전멸 시 함께 청소하기 위해 코어에 등록
            }

            yield return null; // 프레임 분산 스폰
        }

        yield return new WaitForSeconds(0.3f);

        canMove = true;
        isPatternPlaying = false;
    }

    // -------------------------------------------------------
    // 소대 생성 (최초 런타임 시 오직 리더만 단 1회 호출)
    // -------------------------------------------------------
    void SpawnFollowers()
    {
        if (squadSpawned)
            return;

        squadSpawned = true;
        VolcanoPumpkinUnit prev = null; // 기차 체인 구조 연결용 임시 캐싱 변수

        // 총 2마리의 소대원을 대장 뒤편에 일렬 종대로 배치 생성
        for (int i = 0; i < 2; i++)
        {
            GameObject obj = PoolManager.Instance.GetBoss(followerPrefabIndex);

            if (obj == null)
                continue;

            // 대장이 바라보는 반대 방향 뒤쪽 좌표 계산
            float dir = IsFlipX ? 1f : -1f;
            Vector3 offset = Vector3.right * dir * followerSpacing * (i + 1);
            obj.transform.position = transform.position + offset;

            VolcanoPumpkinUnit follower = obj.GetComponent<VolcanoPumpkinUnit>();

            if (follower == null)
                continue;

            follower.SetCore(core);
            follower.isLeader = false; // 소대원 신분 확정

            // 체인 링크 세팅: 첫 번째는 리더를, 두 번째는 첫 번째 소대원을 추종
            follower.SetFrontUnit(i == 0 ? this : prev);

            // 명시적으로 코어에 등록
            core?.RegisterUnit(follower);

            prev = follower; // 다음 순번 연결을 위해 교체 백업
        }
    }

    // -------------------------------------------------------
    // 지휘권 승계 프로세스
    // -------------------------------------------------------
    public void PromoteToLeader()
    {
        isLeader = true;
        frontUnit = null; // 쫓아가야 할 머리가 되었으므로 앞 링크 전격 해제 (독자 AI 기동)
    }

    // 승계 직후 코어에 의해 살아남은 소대원들의 대열 고리 재연결
    public void ReassignFollowers(List<VolcanoPumpkinUnit> remaining)
    {
        VolcanoPumpkinUnit prev = this; // 새로운 리더(자신)가 기차 대열의 맨 머리가 됨

        foreach (VolcanoPumpkinUnit unit in remaining)
        {
            if (unit == this) // 리더 자신은 제외
                continue;

            unit.SetFrontUnit(prev); // 남은 소대원들을 차례대로 내 뒤에 자물쇠 체인 연결
            prev = unit;
        }
    }

    // -------------------------------------------------------
    // 위치 기록 추적 접근 및 초기화
    // -------------------------------------------------------
    public Vector2 GetHistoryPosition(int index)
    {
        if (positionHistory.Count == 0)
            return transform.position;

        index = Mathf.Clamp(index, 0, positionHistory.Count - 1);
        return positionHistory[index];
    }

    public void ResetPositionHistory()
    {
        positionHistory.Clear();
        positionHistory.Add(transform.position);
    }

    // -------------------------------------------------------
    // 외부 의존성 주입 세터
    // -------------------------------------------------------
    public void SetCore(VolcanoPumpkinCore newCore)
    {
        core = newCore;
    }

    public void SetFrontUnit(VolcanoPumpkinUnit unit)
    {
        frontUnit = unit;
    }

    // -------------------------------------------------------
    // 피격 처리
    // -------------------------------------------------------
    public override void TakeDamage(float damage)
    {
        // 소대원은 리더가 살아있는 동안 데미지 무시
        if (!isLeader && core != null && core.IsLeaderAlive())
            return;

        base.TakeDamage(damage);
    }

    // -------------------------------------------------------
    // 사망 프로세스
    // -------------------------------------------------------
    protected override void Dead()
    {
        if (isDead)
            return;

        isDead = true;
        canMove = false;

        // 물리 속도, 충돌체, 그래픽 렌더러 기능 완전 동결 및 봉인
        if (rigid != null)
            rigid.linearVelocity = Vector2.zero;

        if (col != null)
            col.enabled = false;

        if (spriter != null)
            spriter.enabled = false;

        // 🎯 [완전 봉쇄] base.Dead()는 절대로 호출하지 않습니다.
        // 오직 부모 스크립트 관계를 끊고 자신을 들고 있는 전용 코어 매니저 명단에서만 해제 처리합니다.
        if (core != null)
        {
            core.UnregisterUnit(this, isLeader);
        }

        gameObject.SetActive(false); // 오브젝트 풀 비활성화 반환
    }
}