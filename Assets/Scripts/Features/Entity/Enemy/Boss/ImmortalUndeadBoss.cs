using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 몬스터: 불굴의 언데드 (ImmortalUndeadBoss)
/// 부모 클래스(BossBase)의 기본 명세를 상속받아 올바르게 오버라이드하고,
/// 독자적인 수호대 시스템 및 광폭화 소환 매커니즘을 제어하는 클래스입니다.
/// </summary>
public class ImmortalUndeadBoss : BossBase
{
    [Header("수호대 몬스터 소환")]
    [Tooltip("수호대(UndeadGuard)의 프리팹 인덱스 (PoolManager 연동)")]
    [SerializeField] private int guardEnemyIndex;
    [Tooltip("수호대가 배치될 보스 중심의 원형 반경")]
    [SerializeField] private float guardRadius = 3f;
    [Tooltip("수호대 전멸 시 약점 노출(무적 해제) 유지 시간")]
    [SerializeField] private float vulnerableDuration = 5f;
    [Tooltip("각 수호대 사망 시 개별 부활까지 걸리는 시간 (30초)")]
    [SerializeField] private float guardRespawnDelay = 30f;

    [Header("공격대 몬스터 소환")]
    [Tooltip("공격대 몬스터 일반 소환 풀 인덱스 배열")]
    [SerializeField] private int[] summonEnemyIndexes;
    [Tooltip("체력 50% 이하 광폭화 시 추가/변경될 소환 풀 인덱스 배열")]
    [SerializeField] private int[] rageSummonEnemyIndexes;
    [Tooltip("공격대 몬스터가 소환될 보스 주변 최소/최대 반경")]
    [SerializeField] private float summonRadius = 5f;
    [Tooltip("공격대 몬스터 소환 주기 (기본 0.75초)")]
    [SerializeField] private float summonInterval = 0.75f;

    // --- 내부 상태 제어 변수 ---
    private bool isInvincible = true;       // 현재 보스의 무적 상태 여부 (수호대 생존 시 무적)
    private bool isSummoningActive = true;  // 공격대 잡몹 지속 소환 루프의 가동 여부
    private int deadGuardCount = 0;         // 현재 처치되어 부재 중인 수호대의 총 개수 (최대 8개)

    // 8마리의 수호대 슬롯 데이터 및 각각의 개별 부활 타이머 코루틴을 추적하는 배열
    private UndeadGuard[] dynamicGuards = new UndeadGuard[8];
    private Coroutine[] guardRespawnCoroutines = new Coroutine[8];

    // 코루틴 중복 실행 방지 및 정밀 제어를 위한 참조 변수
    private Coroutine raidSummonCoroutine;
    private Coroutine vulnerabilityCoroutine;

    // 소환한 공격대 몬스터 오브젝트 추적 리스트 (보스 사망 시 일괄 비활성화 및 청소용)
    private readonly List<GameObject> summonedEnemies = new List<GameObject>();

    protected override void Start()
    {
        // 부모 클래스(BossBase)의 기초 초기화 로직 실행
        base.Start();

        // 테이블 데이터(data)가 할당되지 않은 경우를 대비한 하드코딩 예외 처리 (디폴트 스펙 설정)
        if (data == null)
        {
            maxHealth = 1000f;
            health = maxHealth; // 부모 클래스의 health 변수 초기화
            moveSpeed = 2f;     // 부모 클래스의 moveSpeed 변수 초기화
        }

        // 보스 고유 패턴 및 소환 메커니즘 가동
        InitializeBossPattern();
    }

    /// <summary>
    /// 보스의 행동 제어권 및 모든 소환 루프를 초기 상태로 리셋하고 가동하는 메서드
    /// </summary>
    private void InitializeBossPattern()
    {
        isInvincible = true;
        canMove = true;             // 부모 클래스의 이동 플래그 활성화
        isSummoningActive = true;

        // 패턴 시작 시점에 수호대 8마리 전체 일제 스폰
        SpawnAllGuards();

        // 기존에 작동 중이던 소환 루프 코루틴이 있다면 안전하게 정지 후 재시작 (중복 방지)
        if (raidSummonCoroutine != null) StopCoroutine(raidSummonCoroutine);
        raidSummonCoroutine = StartCoroutine(RaidSummonRoutine());
    }

    /// <summary>
    /// 모든 수호대 슬롯을 순회하며 기존 타이머와 객체를 초기화한 후, 8마리를 완전히 새로 채우는 메서드
    /// </summary>
    private void SpawnAllGuards()
    {
        deadGuardCount = 0; // 부재 중 카운트 리셋

        for (int i = 0; i < 8; i++)
        {
            // 가동 중이던 개별 부활(30초) 타이머가 있다면 강제 종료 및 청소
            if (guardRespawnCoroutines[i] != null)
            {
                StopCoroutine(guardRespawnCoroutines[i]);
                guardRespawnCoroutines[i] = null;
            }

            // 슬롯 관리 배열에 기존 객체 참조가 남아있다면 오브젝트 풀 반환 및 참조 삭제 (메모리 누수 및 중복 방지)
            if (dynamicGuards[i] != null)
            {
                dynamicGuards[i].gameObject.SetActive(false);
                dynamicGuards[i] = null;
            }

            // i번째 고유 슬롯에 단일 수호대 스폰 실행
            SpawnSingleGuard(i);
        }
    }

    /// <summary>
    /// 지정된 단 하나의 고유 슬롯 인덱스(0~7)에 매칭되는 원형 좌표를 연산하여 수호대를 생성하는 메서드
    /// </summary>
    private void SpawnSingleGuard(int slotIndex)
    {
        // 360도를 8등분(45도 간격)한 뒤 호도법(라디안) 각도로 변환
        float angle = slotIndex * 45f * Mathf.Deg2Rad;
        // 삼각함수를 이용하여 보스 본체 중심 기준의 원형 오프셋 계산
        Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * guardRadius;
        Vector3 spawnPosition = transform.position + spawnOffset;

        // PoolManager를 통해 수호대 객체 인출
        GameObject guardObj = PoolManager.Instance.GetEnemy(guardEnemyIndex);

        // 컴포넌트 유효성 검증 후 수호대 본체에 필요한 데이터(보스 주소, 슬롯 ID, 궤도 변수) 주입 및 링크
        if (guardObj != null && guardObj.TryGetComponent<UndeadGuard>(out var guard))
        {
            guard.transform.position = spawnPosition;
            guard.InitializeGuard(this, slotIndex, angle, guardRadius);
            dynamicGuards[slotIndex] = guard; // 관리용 슬롯 배열에 등록
        }
    }

    /// <summary>
    /// 수호대가 처치되었을 때 호출되어 사망 이벤트를 접수하고 후속 기믹을 트리거하는 콜백 메서드
    /// </summary>
    public void OnGuardDead(int slotIndex)
    {
        // 이미 비어있는 슬롯이거나 중복 접수된 이벤트인 경우 예외 차단
        if (dynamicGuards[slotIndex] == null) return;

        dynamicGuards[slotIndex] = null; // 슬롯 비우기
        deadGuardCount++;                // 사망하여 부재 중인 수호대 카운트 증가

        // [수호대 처치 보상] 보스 최대 체력의 1%를 시스템적으로 즉시 감산
        health -= maxHealth * 0.01f;
        if (health <= 0f) health = 0f;

        // [전멸 조건 체크] 부활이 완료되기 전에 8마리가 한꺼번에 모두 전멸했는가?
        if (deadGuardCount >= 8)
        {
            // 전멸 시: 대기 중이던 개별 부활 타이머 시퀀스를 전부 파괴하고 즉시 그로기(약점 노출) 페이즈로 전환
            EnterVulnerablePhase();
        }
        else
        {
            // 생존자가 남아있다면: 방금 처치된 해당 슬롯에 대해서만 30초 독자 부활 타이머 코루틴 기동
            if (guardRespawnCoroutines[slotIndex] != null) StopCoroutine(guardRespawnCoroutines[slotIndex]);
            guardRespawnCoroutines[slotIndex] = StartCoroutine(RespawnGuardRoutine(slotIndex));
        }
    }

    /// <summary>
    /// 특정 슬롯 수호대의 사망 후 30초 대기 및 단독 재생성을 처리하는 타이머 코루틴
    /// </summary>
    private IEnumerator RespawnGuardRoutine(int slotIndex)
    {
        // 설정된 시간(30초) 동안 대기하며 전멸 여부 관망
        yield return new WaitForSeconds(guardRespawnDelay);

        // 시간 만료 시점까지 보스가 전멸 페이즈로 빠지지 않았다면 해당 슬롯 복구 스폰
        SpawnSingleGuard(slotIndex);
        deadGuardCount--; // 부재 중 카운트 복구 차감
        guardRespawnCoroutines[slotIndex] = null;
    }

    /// <summary>
    /// 전멸 조건 충족 시 모든 개별 부활 스케줄러를 강제 취소하고 메인 그로기 루틴을 트리거하는 메서드
    /// </summary>
    private void EnterVulnerablePhase()
    {
        // 가동 중이던 모든 개별 부활 코루틴을 강제 종료하여 타이머 및 데이터 꼬임 현상 원천 차단
        for (int i = 0; i < 8; i++)
        {
            if (guardRespawnCoroutines[i] != null)
            {
                StopCoroutine(guardRespawnCoroutines[i]);
                guardRespawnCoroutines[i] = null;
            }
        }

        // 약점 노출(그로기 딜타임) 메인 코루틴 가동
        if (vulnerabilityCoroutine != null) StopCoroutine(vulnerabilityCoroutine);
        vulnerabilityCoroutine = StartCoroutine(VulnerableRoutine());
    }

    /// <summary>
    /// 무적 해제(그로기 딜타임) 상태의 제어 및 종료 후 수호대 일제 복구를 총괄하는 코루틴
    /// </summary>
    private IEnumerator VulnerableRoutine()
    {
        // [그로기 즉시 돌입 상태 제어]
        isInvincible = false;       // 무적 상태 전면 해제 (플레이어 타격 유효화)
        canMove = false;            // 보스 이동 강제 중지
        isSummoningActive = false;  // 공격대 잡몹 지속 소환 루프 조건 차단

        // 가동 중이던 공격대 소환 루프 코루틴 정지 및 리셋
        if (raidSummonCoroutine != null)
        {
            StopCoroutine(raidSummonCoroutine);
            raidSummonCoroutine = null;
        }

        // 설정된 약점 노출 지속 시간(예: 5초) 동안 딜 타임 유지하며 대기
        yield return new WaitForSeconds(vulnerableDuration);

        // [딜 타임 종료 -> 강력한 패턴 상태로 복구]
        isInvincible = true;        // 보스 다시 무적 상태 돌입
        canMove = true;             // 보스 추적 이동 재개
        isSummoningActive = true;   // 공격대 잡몹 소환 조건 재활성화

        // 일반 공격대 지속 소환 루프 재가동
        raidSummonCoroutine = StartCoroutine(RaidSummonRoutine());

        // ★ [핵심 메커니즘] 무적 복구와 동시에, 비어있던 수호대 8마리를 한꺼번에 클린 재소환 및 타이머 전면 초기화!
        SpawnAllGuards();
    }

    public override void TakeDamage(float damage)
    {
        // 수호대가 생존해 있는 무적 상태라면 외부 모든 타격 데미지 전면 무시
        if (isInvincible) return;

        // 무적이 풀린 그로기 상태일 때만 부모 클래스(BossBase)의 실제 연산 피격 로직 처리
        base.TakeDamage(damage);
    }

    /// <summary>
    /// 보스 사망 시 부모 Dead()를 호출하기 전에,
    /// 살아있는 수호대 및 소환된 공격대 몬스터를 모두 비활성화합니다.
    /// </summary>
    protected override void Dead()
    {
        // 보스 객체 내에서 가동 중이던 모든 코루틴 즉시 강제 정지 (부활 타이머, 소환 루프 등 완전 중단)
        StopAllCoroutines();

        // 필드에 생존해 있는 수호대 전원 즉시 추적하여 오브젝트 풀 반환 및 비활성화
        for (int i = 0; i < 8; i++)
        {
            if (dynamicGuards[i] != null)
            {
                dynamicGuards[i].gameObject.SetActive(false);
                dynamicGuards[i] = null;
            }
        }

        // 그동안 실시간으로 누적 소환했던 필드의 모든 공격대 잡몹들을 전원 추적하여 풀 복귀 처리
        // 이미 파괴되었거나(null) 수동으로 비활성화된 항목은 예외 차단
        foreach (GameObject enemy in summonedEnemies)
        {
            if (enemy != null && enemy.activeSelf)
                enemy.SetActive(false);
        }
        // 청소가 끝난 리스트의 모든 요소 클리어
        summonedEnemies.Clear();

        // 부모 클래스의 Dead() 실행 (WaveManager 스택 차감, 재화/보물상자 드롭, 클리어 포탈 스폰 등 후속 시퀀스 가동)
        base.Dead();
    }

    /// <summary>
    /// 보스 주변 무작위 반경에 주기적으로 공격대 잡몹 무리를 낙하 소환하는 패턴 루틴
    /// </summary>
    private IEnumerator RaidSummonRoutine()
    {
        while (isSummoningActive)
        {
            // 설정된 주기(기본 0.75초)만큼 텀을 두고 대기
            yield return new WaitForSeconds(summonInterval);

            // 보스의 현재 체력이 최대치의 50% 이하 조건에 도달했는지 실시간 체크하여 광폭화(Rage) 여부 판정
            bool isRage = health <= (maxHealth * 0.5f);
            int spawnCount = isRage ? 2 : 1; // 광폭화(Rage) 상태 돌입 시 주기당 소환수 2배 증가 기믹

            for (int i = 0; i < spawnCount; i++)
            {
                // 보스 월드 좌표 기준, 지정 반경 내 임의의 2D 원형 분포 좌표 연산
                Vector2 randomCircle = Random.insideUnitCircle * summonRadius;
                Vector3 summonPos = transform.position + new Vector3(randomCircle.x, randomCircle.y, 0f);

                int targetIndex = 0;

                // 광폭화 판정 여부 및 전용 소환 풀 배열 유효성에 따라 스폰할 적 프리팹 인덱스(ID) 차등 선정
                if (isRage && rageSummonEnemyIndexes != null && rageSummonEnemyIndexes.Length > 0)
                    targetIndex = rageSummonEnemyIndexes[Random.Range(0, rageSummonEnemyIndexes.Length)];
                else if (summonEnemyIndexes != null && summonEnemyIndexes.Length > 0)
                    targetIndex = summonEnemyIndexes[Random.Range(0, summonEnemyIndexes.Length)];

                // PoolManager에서 적 인스턴스를 확보하여 계산된 위치에 실시간 배치
                GameObject spawnedEnemy = PoolManager.Instance.GetEnemy(targetIndex);
                if (spawnedEnemy != null)
                {
                    spawnedEnemy.transform.position = summonPos;
                    // 보스 사망 시 일괄 정리할 수 있도록 소환 성공 즉시 관리용 추적 리스트에 등록
                    summonedEnemies.Add(spawnedEnemy);
                }
            }
        }
    }
}