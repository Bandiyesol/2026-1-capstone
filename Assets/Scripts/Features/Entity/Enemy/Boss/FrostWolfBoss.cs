using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 서리 늑대 군주를 구성하는 세 늑대(A / B / C) 공용 스크립트.
/// BossBase를 상속하며, Inspector에서 지정된 WolfType에 따라 패턴을 분기 수행한다.
/// </summary>
public class FrostWolfBoss : BossBase
{
    // --------------------------------------------------------
    // 늑대 종류 열거형 (인스펙터 지정용)
    // --------------------------------------------------------
    public enum WolfType
    {
        A_Freeze,   // 빙결 탄막 담당 늑대
        B_Charge,   // 타겟 돌진 담당 늑대
        C_Summon    // 몬스터 소환 담당 늑대
    }

    // --------------------------------------------------------
    // Inspector 노출 필드
    // --------------------------------------------------------

    [Header("늑대 종류")]
    [SerializeField] WolfType wolfType = WolfType.A_Freeze; // 이 오브젝트가 수행할 고유 역할

    [Header("탄막 (Wolf A / 광폭화 공용)")]
    [Tooltip("빙결 탄막 PoolManager 인덱스")]
    [SerializeField] int freezeBulletIndex = 0;
    [Tooltip("일반 상태 탄막 발수")]
    [SerializeField] int normalBulletCount = 5;
    [Tooltip("광폭화 부채꼴 탄막 발수")]
    [SerializeField] int rageBulletCount = 9;
    [Tooltip("부채꼴 총 각도 (광폭화)")]
    [SerializeField] float fanAngle = 90f;

    [Header("돌진 (Wolf B / 광폭화 공용)")]
    [Tooltip("돌진 전 멈춤 시간 (초)")]
    [SerializeField] float chargeWindupTime = 0.6f;
    [Tooltip("돌진 이동 속도")]
    [SerializeField] float chargeSpeed = 20f;
    [Tooltip("돌진 도착 판정 거리 (유닛)")]
    [SerializeField] float chargeArrivalThreshold = 0.3f;
    [Tooltip("돌진 최대 지속 시간 (무한 돌진 방지)")]
    [SerializeField] float chargeMaxDuration = 2.5f;

    [Header("소환 (Wolf C / 광폭화 공용)")]
    [Tooltip("소환 몬스터 PoolManager 인덱스")]
    [SerializeField] int summonEnemyIndex = 0;
    [Tooltip("일반 상태 소환 수")]
    [SerializeField] int normalSummonCount = 3;
    [Tooltip("광폭화 소환 수 배율")]
    [SerializeField] float rageSummonMultiplier = 2f;
    [Tooltip("소환 위치 랜덤 반경")]
    [SerializeField] float summonRadius = 3f;

    [Header("집중 타격 버프 (코어에서 제어)")]
    [Tooltip("패턴 쿨타임 스택당 감소율 (0.05 = 5%)")]
    [SerializeField] float cooldownReductionPerStack = 0.05f;
    [Tooltip("패턴 쿨타임 감소 최대치 (0.5 = 50%)")]
    [SerializeField] float maxCooldownReduction = 0.5f;

    // --------------------------------------------------------
    // 내부 상태 및 캐싱 변수
    // --------------------------------------------------------

    FrostWolfCore core;           // 세 마리를 총괄하는 메인 코어 매니저 참조
    Transform playerTransform;    // 플레이어의 실시간 위치 추적용 컴포넌트 캐시

    float baseAttackDamage;       // 런타임 시작 시점의 순수 기본 공격력 보관
    float baseMoveSpeed;          // 런타임 시작 시점의 순수 기본 이동속도 보관
    float basePatternCooldown;    // 런타임 시작 시점의 순수 기본 패턴 쿨타임 보관

    int currentFocusStack;        // 타 늑대가 피격될 때 코어로부터 인계받은 버프 스택 수
    bool isFinalPhase;            // 마지막 생존 늑대가 되어 광폭화 상태에 진입했는지 여부

    // 광폭화 단계에서 무작위 패턴을 추첨하기 위한 인덱스 풀 (0:돌진, 1:빙결, 2:소환)
    static readonly int[] finalPatternPool = { 0, 1, 2 };

    bool isCharging;              // 돌진 중복 실행 방지용 플래그

    // ============================================================
    // 초기화 및 활성화 시점 처리
    // ============================================================

    protected override void OnEnable()
    {
        base.OnEnable(); // 부모(BossBase)의 초기화 로직 실행

        // 태그 검색을 통한 플레이어 트랜스폼 캐싱
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;

        // 버프 연산 및 광폭화 해제 시 되돌아갈 기초 데이터 원본 저장
        baseAttackDamage = attackDamage;
        baseMoveSpeed = moveSpeed;
        basePatternCooldown = patternCooldown;

        // 활성화(오브젝트 풀 컴백)에 따른 논리 상태 초기화
        currentFocusStack = 0;
        isFinalPhase = false;
        isCharging = false;
    }

    // ============================================================
    // 상위 제어 시스템 연동 (Core 주입)
    // ============================================================

    public void SetCore(FrostWolfCore coreRef)
    {
        core = coreRef;
    }

    // ============================================================
    // 피격 처리 오버라이드
    // ============================================================

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        // base.TakeDamage 내부에서 Dead()가 호출되어 이미 사망 처리가 완료된 경우
        // 죽은 늑대로 집중 타격 스택이 올라가는 것을 차단
        if (isDead) return;

        // 플레이어가 자신을 공격 중임을 코어에 전달 -> 타 늑대들에게 집중 타격 버프 전파 유도
        if (core != null)
            core.NotifyWolfDamaged(this);
    }

    // ============================================================
    // 사망 처리 오버라이드
    // ============================================================

    protected override void Dead()
    {
        if (isDead) return;
        isDead = true; // 중복 사망 및 데미지 연산 인터셉트 차단

        // 돌진 도중 사망 시 관성 스킵 및 코루틴 강제 정리
        // StopAllCoroutines로 인해 Pattern_Charge의 복원 코드가 실행되지 않으므로 여기서 직접 복원
        StopAllCoroutines();
        isCharging = false;
        isPatternPlaying = false;
        moveSpeed = baseMoveSpeed; // 돌진 속도(chargeSpeed)가 잔류하지 않도록 명시적 복원

        // 유니티 이벤트가 아닌 풀링 해제 방식이므로 구형 매니저 킬 카운트 수동 가산
        if (GameManager.instance != null)
            GameManager.instance.Kill++;

        // 코어에 본인의 사망 보고 -> 생존 카운트 차감 및 마지막 개체일 시 광폭화 트리거 구동
        if (core != null)
            core.NotifyWolfDead(this);

        // 오브젝트 비활성화를 통한 풀 반환
        gameObject.SetActive(false);
    }

    // ============================================================
    // 패턴 실행 분기 처리 (패턴 타이머 만료 시 부모 Update에서 자동 호출)
    // ============================================================

    protected override void StartRandomPattern()
    {
        // ?? 부모의 isPatternPlaying 제어 메커니즘 덕분에 현재 패턴 완료 전까지 중복 진입이 전면 차단됨
        if (isFinalPhase)
        {
            // ?? [광폭화 상태] 자신의 고유 타입을 무시하고 전 패턴 공용 풀에서 무작위 추첨 실행
            int roll = Random.Range(0, finalPatternPool.Length);

            switch (finalPatternPool[roll])
            {
                case 0: StartCoroutine(Pattern_Charge()); break;
                case 1: StartCoroutine(Pattern_FanFreezeAsync()); break;
                case 2: StartCoroutine(Pattern_SummonAsync(GetRageSummonCount())); break;
            }
        }
        else
        {
            // ?? [일반 상태] 인스펙터에 설정된 본인의 고유 포지션 패턴만 전담 수행
            switch (wolfType)
            {
                case WolfType.A_Freeze: StartCoroutine(Pattern_FreezeAsync()); break;
                case WolfType.B_Charge: StartCoroutine(Pattern_Charge()); break;
                case WolfType.C_Summon: StartCoroutine(Pattern_SummonAsync(normalSummonCount)); break;
            }
        }
    }

    // ============================================================
    // 패턴 1 : 빙결 직선 탄막 (Wolf A 일반 상태)
    // ============================================================

    /// <summary>
    /// 즉발성이지만 시스템의 플레이 플래그(isPatternPlaying) 스케줄링 호환을 위한 코루틴 래퍼
    /// </summary>
    IEnumerator Pattern_FreezeAsync()
    {
        isPatternPlaying = true;
        Pattern_Freeze(); // 실제 발사 연산
        yield return null; // 1프레임 대기 후 종료 보장
        isPatternPlaying = false;
    }

    void Pattern_Freeze()
    {
        if (playerTransform == null) return;

        // 플레이어 중심을 조준하는 기준 방향 벡터 정규화
        Vector2 dir = (playerTransform.position - transform.position).normalized;

        for (int i = 0; i < normalBulletCount; i++)
        {
            // 머신건처럼 직선상에 살짝 흐트러진 산탄 형태를 만들기 위해 무작위 구체 오차 합산
            Vector2 spawnPos = (Vector2)transform.position
                               + dir * 0.5f
                               + Random.insideUnitCircle * 0.2f;

            FireFreezeBullet(spawnPos, dir);
        }
    }

    // ============================================================
    // 패턴 2 : 빙결 부채꼴 탄막 (광폭화 전용)
    // ============================================================

    /// <summary>
    /// 광폭화 부채꼴 투사 처리를 위한 코루틴 래퍼
    /// </summary>
    IEnumerator Pattern_FanFreezeAsync()
    {
        isPatternPlaying = true;
        Pattern_FanFreeze(); // 부채꼴 분산 발사 연산
        yield return null;
        isPatternPlaying = false;
    }

    void Pattern_FanFreeze()
    {
        if (playerTransform == null) return;

        // 플레이어 방향을 기준으로 삼을 기초 각도(Degree) 연산
        Vector2 baseDir = (playerTransform.position - transform.position).normalized;
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        // 설정 발수가 1발 이하인 예외 케이스 처리 (중앙 직사)
        if (rageBulletCount <= 1)
        {
            FireFreezeBullet(transform.position, baseDir);
            return;
        }

        // 지정된 각도 영역(fanAngle) 내에서 발수만큼 균등 분배할 간격 스텝 계산
        float halfFan = fanAngle * 0.5f;
        float step = fanAngle / (rageBulletCount - 1);

        for (int i = 0; i < rageBulletCount; i++)
        {
            // 최소 각도(좌측 끝)부터 스텝 단위로 우측으로 돌며 순차 각도 계산
            float angle = baseAngle - halfFan + step * i;
            float rad = angle * Mathf.Deg2Rad; // 삼각함수 대입용 라디안 변환
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            FireFreezeBullet(transform.position, dir);
        }
    }

    // ============================================================
    // 탄막 스폰 유틸리티 헬퍼
    // ============================================================

    void FireFreezeBullet(Vector2 spawnPos, Vector2 dir)
    {
        if (PoolManager.Instance == null) return;

        // 글로벌 풀 매니저로부터 보스 투사체 인스턴스 팝업
        GameObject bulletObj = PoolManager.Instance.GetBossBullet(freezeBulletIndex);
        if (bulletObj == null) return;

        bulletObj.transform.position = spawnPos;

        // 컴포넌트 추출 후 방향 벡터 주입 (세부 스탯은 투사체 자체 프리팹 데이터 사용)
        FreezeBullet bullet = bulletObj.GetComponent<FreezeBullet>();
        if (bullet != null)
            bullet.Init(dir);

        bulletObj.SetActive(true);
    }

    // ============================================================
    // 패턴 3 : 고속 락온 돌진 (Wolf B 일반 / 광폭화 공용)
    // ============================================================

    IEnumerator Pattern_Charge()
    {
        if (isCharging) yield break;
        isCharging = true;
        isPatternPlaying = true;

        // [단계 1] 캐스팅 모션: 기존 이속 백업 후 물리 속도를 영(0)으로 묶어 돌진 자세 연출
        float savedMoveSpeed = moveSpeed;
        moveSpeed = 0f;

        yield return new WaitForSeconds(chargeWindupTime); // 돌진 전 징후 딜레이 타임 대기

        // [단계 2] 타게팅 스냅샷: 대기가 끝난 시점의 플레이어 위치 좌표를 최종 목적지로 고정 (유도 기능 종료)
        Vector2 targetPos = playerTransform != null
            ? (Vector2)playerTransform.position
            : (Vector2)transform.position;

        moveSpeed = chargeSpeed; // 인스펙터 지정 돌진 속도 전격 가동
        float elapsed = 0f;

        // [단계 3] 실제 고속 이동 루프 (도착 판정 마진 진입 또는 타임아웃 오버 시까지 무한 반복)
        while (elapsed < chargeMaxDuration)
        {
            elapsed += Time.deltaTime;

            // 목표점과의 잔여 선형 거리 체크
            float dist = Vector2.Distance(transform.position, targetPos);
            if (dist <= chargeArrivalThreshold)
                break; // 유효 범위 도착 시 루프 조기 탈출

            // 목적지 방향 정규화 후 등속 Translate 프레임 워크 구동
            Vector2 dir = ((Vector3)targetPos - transform.position).normalized;
            transform.Translate(dir * chargeSpeed * Time.deltaTime);

            yield return null; // 다음 프레임까지 연산 제어권 양보
        }

        // [단계 4] 후처리 정리: 버프 상태에 유연하게 대응하기 위해 백업했던 원본 속도로 가변 복원
        moveSpeed = savedMoveSpeed;
        isCharging = false;
        isPatternPlaying = false;
    }

    // ============================================================
    // 패턴 4 : 부하 몬스터 소환 (Wolf C 일반 / 광폭화 공용)
    // ============================================================

    /// <summary>
    /// 소환 수량 가변 처리를 위한 코루틴 래퍼
    /// </summary>
    IEnumerator Pattern_SummonAsync(int count)
    {
        isPatternPlaying = true;
        Pattern_Summon(count); // 실제 소환 팩토리 구동
        yield return null;
        isPatternPlaying = false;
    }

    void Pattern_Summon(int count)
    {
        if (PoolManager.Instance == null) return;

        for (int i = 0; i < count; i++)
        {
            // 잡몹 전용 오브젝트 풀 라인업에서 인스턴스 팝업
            GameObject enemyObj = PoolManager.Instance.GetEnemy(summonEnemyIndex);
            if (enemyObj == null) continue;

            // 늑대 본체 중심으로 지정된 스폰 반경(summonRadius) 내부의 랜덤 원형 좌표 산출
            Vector2 spawnPos = (Vector2)transform.position
                               + Random.insideUnitCircle * summonRadius;

            enemyObj.transform.position = spawnPos;
            enemyObj.SetActive(true); // 활성화와 동시에 잡몹 자체 AI 작동 가동
        }
    }

    // ============================================================
    // 수학적 연산 및 광폭화 계수 도출
    // ============================================================

    int GetRageSummonCount()
    {
        // 광폭화 배율 곱셈 연산 후 정밀 수량 도출을 위한 반올림 정수 변환
        return Mathf.RoundToInt(normalSummonCount * rageSummonMultiplier);
    }

    // ============================================================
    // 최종 페이즈 : 광폭화 락 시스템 개방 (Core 전용 트리거)
    // ============================================================

    public void EnterFinalPhase()
    {
        isFinalPhase = true;

        // 광폭화 단독 스탯 증폭과 꼬이지 않도록 타격 버프 효과 클리어
        ResetFocusBuff();

        // [보스 기믹] 마지막 한 마리 생존 시 체력 풀(Full) 보정
        health = maxHealth;

        // 기본 베이스 스탯 대비 물리 수치 일괄 200% 더블 스케일링 가산
        attackDamage = baseAttackDamage * 2f;
        moveSpeed = baseMoveSpeed * 2f;

        // 패턴 순환 딜레이 50% 컷오프 처리 (극단적 패턴 연사 유도)
        patternCooldown = basePatternCooldown * 0.5f;

        Debug.Log($"[FrostWolfBoss] {gameObject.name} 광폭화 진입!");
    }

    // ============================================================
    // 코어 연동형 역성장 집중 타격 버프 시스템 (스탯 조절)
    // ============================================================

    /// <summary>
    /// 유저가 다른 늑대를 집중 타격할 때 반사 이익으로 강해지는 버프 로직 (Core에서 주입)
    /// </summary>
    public void SetFocusBuff(float attackMultiplier, float speedMultiplier)
    {
        // 중첩 곱연산 오차를 배제하기 위해 매번 원본 베이스 스탯 기준으로 정밀 재계산 진행
        attackDamage = baseAttackDamage * attackMultiplier;
        moveSpeed = baseMoveSpeed * speedMultiplier;

        // 동적 쿨타임 스택 계산부 갱신 가동
        currentFocusStack++;
        ApplyCooldownReduction();
    }

    /// <summary>
    /// 어그로 대상 타겟이 변경되거나 정화될 시 순수 원본 스탯으로 되돌리는 초기화 함수
    /// </summary>
    public void ResetFocusBuff()
    {
        attackDamage = baseAttackDamage;
        moveSpeed = baseMoveSpeed;
        patternCooldown = basePatternCooldown;
        currentFocusStack = 0;
    }

    /// <summary>
    /// 누적된 포커스 스택 수치만큼 프레임 워크 쿨타임을 차감하는 수학적 수식 처리부
    /// </summary>
    void ApplyCooldownReduction()
    {
        // (현재 스택 * 단위 감소 수치) 연산값이 한계치(maxCooldownReduction)를 넘지 못하도록 클램프 락킹
        float reductionRate = Mathf.Min(
            currentFocusStack * cooldownReductionPerStack,
            maxCooldownReduction
        );

        // 산출된 최종 감소율(%)을 기본 쿨타임에 적용 반영
        patternCooldown = basePatternCooldown * (1f - reductionRate);
    }
}