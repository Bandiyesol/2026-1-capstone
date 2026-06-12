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
    [SerializeField] private int summonEnemyIndex;
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
        float angle = slotIndex * 45f * Mathf.Deg2Rad;
        Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * guardRadius;
        Vector3 spawnPosition = transform.position + spawnOffset;

        if (anim != null) anim.SetTrigger("Attack");

        GameObject guardObj = PoolManager.Instance.GetEnemy(guardEnemyIndex);

        if (guardObj != null && guardObj.TryGetComponent<UndeadGuard>(out var guard))
        {
            guard.transform.position = spawnPosition;
            guard.InitializeGuard(this, slotIndex, angle, guardRadius);
            dynamicGuards[slotIndex] = guard;
        }
    }

    /// <summary>
    /// 수호대가 처치되었을 때 호출되어 사망 이벤트를 접수하고 후속 기믹을 트리거하는 콜백 메서드
    /// </summary>
    public void OnGuardDead(int slotIndex)
    {
        if (dynamicGuards[slotIndex] == null) return;

        dynamicGuards[slotIndex] = null;
        deadGuardCount++;

        // [수호대 처치 보상] 보스 최대 체력의 1%를 시스템적으로 즉시 감산
        health -= maxHealth * 0.01f;

        // ★ [수정] 수호대 처치 대미지로 인해 보스 체력이 0 이하가 되었을 때의 사망 처리 예외 구현
        if (health <= 0f)
        {
            health = 0f;
            Dead(); // 즉시 본 스크립트의 사망 시퀀스 가동
            return; // 보스가 사망했으므로 하단의 수호대 부활/그로기 패턴 계산은 스킵
        }

        // [전멸 조건 체크] 부활이 완료되기 전에 8마리가 한꺼번에 모두 전멸했는가?
        if (deadGuardCount >= 8)
        {
            EnterVulnerablePhase();
        }
        else
        {
            if (guardRespawnCoroutines[slotIndex] != null) StopCoroutine(guardRespawnCoroutines[slotIndex]);
            guardRespawnCoroutines[slotIndex] = StartCoroutine(RespawnGuardRoutine(slotIndex));
        }
    }

    private IEnumerator RespawnGuardRoutine(int slotIndex)
    {
        yield return new WaitForSeconds(guardRespawnDelay);

        SpawnSingleGuard(slotIndex);
        deadGuardCount--;
        guardRespawnCoroutines[slotIndex] = null;
    }

    private void EnterVulnerablePhase()
    {
        for (int i = 0; i < 8; i++)
        {
            if (guardRespawnCoroutines[i] != null)
            {
                StopCoroutine(guardRespawnCoroutines[i]);
                guardRespawnCoroutines[i] = null;
            }
        }

        if (vulnerabilityCoroutine != null) StopCoroutine(vulnerabilityCoroutine);
        vulnerabilityCoroutine = StartCoroutine(VulnerableRoutine());
    }

    private IEnumerator VulnerableRoutine()
    {
        isInvincible = false;
        canMove = false;
        isSummoningActive = false;

        if (raidSummonCoroutine != null)
        {
            StopCoroutine(raidSummonCoroutine);
            raidSummonCoroutine = null;
        }

        yield return new WaitForSeconds(vulnerableDuration);

        isInvincible = true;
        canMove = true;
        isSummoningActive = true;

        raidSummonCoroutine = StartCoroutine(RaidSummonRoutine());
        SpawnAllGuards();
    }

    public override void TakeDamage(float damage)
    {
        if (isInvincible) return;
        base.TakeDamage(damage);
    }

    /// <summary>
    /// 보스 사망 시 부모 Dead()를 호출하기 전에 자신 및 부하 오브젝트들을 정돈합니다.
    /// </summary>
    protected override void Dead()
    {
        // ★ [수정] 부모 클래스(BossBase)의 고유 코루틴(마법진 생성 등)을 해치지 않기 위해, 
        // 전체 정지(StopAllCoroutines)를 폐기하고 이 클래스가 켜놓은 코루틴만 저격해서 안전하게 정지합니다.
        if (raidSummonCoroutine != null) StopCoroutine(raidSummonCoroutine);
        if (vulnerabilityCoroutine != null) StopCoroutine(vulnerabilityCoroutine);

        for (int i = 0; i < 8; i++)
        {
            if (guardRespawnCoroutines[i] != null)
            {
                StopCoroutine(guardRespawnCoroutines[i]);
                guardRespawnCoroutines[i] = null;
            }
        }

        // 필드에 생존해 있는 수호대 전원 즉시 오브젝트 풀 반환 및 비활성화
        for (int i = 0; i < 8; i++)
        {
            if (dynamicGuards[i] != null)
            {
                dynamicGuards[i].gameObject.SetActive(false);
                dynamicGuards[i] = null;
            }
        }

        // 필드의 모든 공격대 잡몹들을 청소
        foreach (GameObject enemy in summonedEnemies)
        {
            if (enemy != null && enemy.activeSelf)
                enemy.SetActive(false);
        }
        summonedEnemies.Clear();

        // 이제 안전해진 상태에서 부모 클래스의 Dead() 실행 (클리어 포탈/마법진 정상 작동)
        base.Dead();
    }

    private IEnumerator RaidSummonRoutine()
    {
        while (isSummoningActive)
        {
            yield return new WaitForSeconds(summonInterval);

            bool isRage = health <= (maxHealth * 0.5f);
            int spawnCount = isRage ? 2 : 1;

            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * summonRadius;
                Vector3 summonPos = transform.position + new Vector3(randomCircle.x, randomCircle.y, 0f);

                int targetIndex = 0;

                if (isRage && rageSummonEnemyIndexes != null && rageSummonEnemyIndexes.Length > 0)
                    targetIndex = rageSummonEnemyIndexes[Random.Range(0, rageSummonEnemyIndexes.Length)];
                else
                    targetIndex = summonEnemyIndex;

                GameObject spawnedEnemy = PoolManager.Instance.GetEnemy(targetIndex);
                if (spawnedEnemy != null)
                {
                    spawnedEnemy.transform.position = summonPos;
                    summonedEnemies.Add(spawnedEnemy);
                }
            }
        }
    }
}