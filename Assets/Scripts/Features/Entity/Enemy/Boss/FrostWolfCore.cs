using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 서리 늑대 군주 코어 매니저
/// - 3마리 개체(A, B, C)의 체력을 통합하여 실시간 공유 체력 표현
/// - 단일 개체 연속 피격 시 버프 유도 및 타겟 변경 시 리셋 (집중 타격 기믹)
/// - 마지막 생존 개체 판별 후 최종 단계 광폭화(Enrage) 트리거링
/// - 전멸 시 단 한 번 WaveManager에 통보하여 웨이브 카운트 오버랩 방지 및 보상 처리
/// </summary>
public class FrostWolfCore : MonoBehaviour
{
    [Header("늑대 3마리 개체 참조")]
    [SerializeField] FrostWolfBoss wolfA;
    [SerializeField] FrostWolfBoss wolfB;
    [SerializeField] FrostWolfBoss wolfC;

    [Header("최종 클리어 보상")]
    [SerializeField] int portalGimmickIndex = 13; // 다음 방 이동용 게이트 포탈의 오브젝트 풀 인덱스

    // 💡 [핵심 연동] WaveManager 스폰 주입부에서 이 코어 오브젝트를 검출해 참조를 채워줍니다.
    // 개별 늑대들은 사망 시 부모(BossBase)의 Dead()를 타지 않으므로, 이 코어가 전멸을 확인한 순간 단 한 번만 호출합니다.
    [HideInInspector]
    public WaveManager waveManager;

    [Header("집중 타격 강화 계수")]
    [SerializeField] float attackBonusPerHit = 0.05f; // 동일 타겟 연속 피격 스택당 공격력 가산치 (5%)
    [SerializeField] float speedBonusPerHit = 0.03f;  // 동일 타겟 연속 피격 스택당 이동속도 가산치 (3%)

    // 런타임 실시간 생존 제어용 늑대 리스트
    readonly List<FrostWolfBoss> wolves = new();

    [Header("공유 체력 상태")]
    public float maxHealth;     // 3마리의 기본 체력을 모두 합산한 총합 최대 체력
    public float currentHealth; // 현재 생존해 있는 늑대들의 잔여 체력 합산

    // 집중 타격 시스템 상태 변수
    FrostWolfBoss focusedWolf;  // 현재 플레이어가 일점사하고 있는 타겟 늑대 참조
    int comboHitCount;          // 동일 타겟 연속 타격 횟수 (콤보 스택)

    bool enraged;               // 최종 1인 광폭화 페이즈 진입 여부 플래그
    bool cleared;               // 보상 중복 생성 및 웨이브 중복 차감 방지용 클리어 플래그

    Vector2 lastDeathPosition;  // 마지막 늑대가 처치된 월드 좌표 (보상 상자 및 포탈 스폰 기준점)

    // ============================================================
    // 초기화 및 가동 시점 처리
    // ============================================================

    void OnEnable()
    {
        // WaveManager.SpawnEnemy()는 FrostWolfCore 타입을 별도로 처리하지 않으므로
        // VolcanoPumpkinCore와 동일하게 StageManager를 통해 직접 참조를 확보한다.
        if (waveManager == null)
            waveManager = StageManager.instance.waveManager;

        wolves.Clear();

        // 족보 등록 및 상호 참조 역주입 처리
        if (wolfA != null) RegisterWolf(wolfA);
        if (wolfB != null) RegisterWolf(wolfB);
        if (wolfC != null) RegisterWolf(wolfC);

        // 전투 상태 데이터 초기화
        focusedWolf = null;
        comboHitCount = 0;
        enraged = false;
        cleared = false;

        // 보스용 UI 상단 체력 바 구성을 위한 스케일링 계산
        CalculateMaxHealth();
        SyncCurrentHealth();
    }

    void Update()
    {
        // 실시간 대미지 반영 및 UI 동기화를 위해 매 프레임 잔여 체력 누적 집계
        SyncCurrentHealth();
    }

    // -------------------------------------------------
    // 늑대 개체 등록 및 상호 관계 설정
    // -------------------------------------------------

    void RegisterWolf(FrostWolfBoss wolf)
    {
        if (wolf == null) return;

        // 개별 보스 스크립트에 이 컨트롤러 코어의 주소 주입 (역참조)
        wolf.SetCore(this);

        if (!wolves.Contains(wolf))
            wolves.Add(wolf);
    }

    // -------------------------------------------------
    // 총합 최대 체력 산출 (런타임 최초 1회)
    // -------------------------------------------------

    void CalculateMaxHealth()
    {
        maxHealth = 0f;

        foreach (var wolf in wolves)
        {
            if (wolf == null) continue;
            maxHealth += wolf.data.maxHealth; // 각 보스 데이터 에셋(ScriptableObject)에 정의된 기초 체력 합산
        }

        currentHealth = maxHealth;
    }

    // -------------------------------------------------
    // 실시간 공유 체력 합산 및 생존 상태 필터링
    // -------------------------------------------------

    public void SyncCurrentHealth()
    {
        float hp = 0f;

        foreach (var wolf in wolves)
        {
            if (wolf == null) continue;
            if (!wolf.gameObject.activeSelf) continue; // 이미 사망하여 비활성화(풀 반환)된 개체는 합산에서 제외

            hp += wolf.health;
        }

        currentHealth = hp;
    }

    // -------------------------------------------------
    // [기믹] 역성장형 집중 타격 시스템 (피격 신호 수신부)
    // -------------------------------------------------

    public void NotifyWolfDamaged(FrostWolfBoss damagedWolf)
    {
        // 💡 이미 마지막 늑대 광폭화 상태라면 밸런스를 위해 집중 타격 메커니즘을 가동하지 않음
        if (enraged) return;

        // [상황 1] 전투 시작 후 최초 타격이거나 타겟이 초기화된 상태인 경우
        if (focusedWolf == null)
        {
            focusedWolf = damagedWolf;
            comboHitCount = 1;

            ApplyFocusBuff();
            return;
        }

        // [상황 2] 직전에 공격받던 동일한 늑대를 연속으로 가격한 경우 (스택 누적 증폭)
        if (focusedWolf == damagedWolf)
        {
            comboHitCount++;
            ApplyFocusBuff();
        }
        // [상황 3] 다른 늑대로 타겟을 변경하여 공격한 경우 (기존 스택 정화 후 교체)
        else
        {
            ResetFocusBuff(); // 이전 타겟의 버프 상태를 깨끗이 롤백

            focusedWolf = damagedWolf;
            comboHitCount = 1;

            ApplyFocusBuff(); // 새로운 타겟 기준으로 버프 스택 새로 가동
        }
    }

    /// <summary>
    /// 누적된 콤보 수치에 근거하여 가변 배율을 연산한 뒤 대상 늑대에게 버프 스탯을 주입하는 함수
    /// </summary>
    void ApplyFocusBuff()
    {
        if (focusedWolf == null) return;

        // 선형 증폭 공식: 1.0 + (현재 콤보 수 * 단위 보너스 수치)
        float attackMultiplier = 1f + (comboHitCount * attackBonusPerHit);
        float speedMultiplier = 1f + (comboHitCount * speedBonusPerHit);

        focusedWolf.SetFocusBuff(attackMultiplier, speedMultiplier);
    }

    /// <summary>
    /// 포커싱 상태를 종료하고 강화되었던 대상을 순수 순정 기본 스탯 상태로 원상복구하는 함수
    /// </summary>
    void ResetFocusBuff()
    {
        if (focusedWolf != null)
            focusedWolf.ResetFocusBuff();

        focusedWolf = null;
        comboHitCount = 0;
    }

    // -------------------------------------------------
    // 개체 사망 이벤트 수신 및 라운드 클로징 판정
    // -------------------------------------------------

    public void NotifyWolfDead(FrostWolfBoss wolf)
    {
        // 보상 아이템 및 포탈이 스폰될 중심점을 잡기 위해 사망 위치 실시간 백업
        lastDeathPosition = wolf.transform.position;

        // 런타임 제어 리스트에서 소거
        wolves.Remove(wolf);

        // 타겟이 사망했으므로 버프 타겟 콤보 상태 완전 리셋
        ResetFocusBuff();

        // 🎯 [분기 1] 단 한 마리만 남았고 아직 광폭화가 발동되지 않은 상태 -> 파이널 레이지 트리거 가동
        if (wolves.Count == 1 && !enraged)
        {
            StartFinalEnrage(wolves[0]);
            return;
        }

        // 🎯 [분기 2] 리스트 내의 모든 개체가 전멸(0)하면 보스전 종료 및 클리어 보상 팝업
        if (wolves.Count <= 0)
        {
            SpawnRewards();
        }
    }

    // -------------------------------------------------
    // 파이널 레이지: 마지막 생존자 광폭화 개방
    // -------------------------------------------------

    void StartFinalEnrage(FrostWolfBoss wolf)
    {
        enraged = true;

        // 대상 늑대 스크립트 내부의 체력 풀(Full) 회복 및 스탯 스케일링 가동
        wolf.EnterFinalPhase();
    }

    // -------------------------------------------------
    // 최종 보상 팩토리 및 웨이브 싱크 정리
    // -------------------------------------------------

    void SpawnRewards()
    {
        if (cleared) return;
        cleared = true; // 프레임워크 오버랩 및 중복 연산 전면 차단

        // 1. 보스 전용 골드 재화 드롭 시스템 연동
        if (CoinDropManager.Instance != null)
            CoinDropManager.Instance.TryDropFromBoss(lastDeathPosition);

        // 2. 등급형 보물 상자 드롭 매니저 연동
        if (ChestDropManager.Instance != null)
            ChestDropManager.Instance.TryDropFromBoss(lastDeathPosition);

        // 3. 다음 방/스테이지 프리패스 전환용 글로벌 포탈 오브젝트 팝업
        if (PoolManager.Instance != null)
        {
            GameObject portal = PoolManager.Instance.GetGimmick(portalGimmickIndex);
            if (portal != null)
                portal.transform.position = lastDeathPosition;
        }

        // 💡 [핵심 예외 처리] 세 마리가 모두 전멸한 이 시점에 '딱 한 번만' 웨이브 카운트를 차감합니다.
        // 개별 늑대의 Dead()에서는 base.Dead()를 스킵했기 때문에, 
        // 몬스터가 3마리 죽었다고 해서 WaveManager의 카운트가 3번 차감되는 버그를 원천 봉쇄합니다.
        waveManager?.OnEnemyDead();

        // 코어 자체를 비활성화하여 보스룸 전체 세션을 최종 클로징
        gameObject.SetActive(false);
    }
}