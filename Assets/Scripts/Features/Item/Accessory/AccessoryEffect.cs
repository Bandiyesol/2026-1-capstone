using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 악세사리 특수 효과 실행 클래스 (싱글톤).
/// 단순 스탯형은 AccessoryManager가 처리하고, 특수 로직은 전부 여기서 처리한다.
/// </summary>
public class AccessoryEffect : MonoBehaviour
{
    public static AccessoryEffect instance;

    readonly HashSet<AccessoryEffectType> owned = new HashSet<AccessoryEffectType>();

    // Revive
    int reviveCharges = 0;

    // ShieldOnLowHP
    bool lowHpShieldActive = false;

    // SpeedOnHit
    Coroutine speedOnHitRoutine;

    // MovingDamage
    Vector3 lastPosition;

    // BurningAura
    Coroutine burningAuraRoutine;

    // TimeStop
    Coroutine timeStopRoutine;

    // GoldenFinger
    Coroutine goldenFingerRoutine;

    [Header("[ 피격 계열 ]")]
    public float reflectRadius          = 3f;
    public float speedOnHitBonus        = 0.3f;
    public float speedOnHitDuration     = 3f;
    public float slowOnHitFreezeTime    = 1.5f;
    public float slowOnHitRadius        = 3f;

    [Header("[ 방어 계열 ]")]
    public float lowHpThreshold         = 0.3f;
    public float lowHpShieldBonus       = 0.3f;
    public float heavyDamageRatio       = 0.2f;
    public float blockChance            = 0.3f;
    public float reviveHpRatio          = 0.5f;

    [Header("[ 공격 계열 ]")]
    public float goldOnHitChance        = 0.1f;
    public float freezeChance           = 0.15f;
    public float freezeDuration         = 1f;
    public float lifeStealAmount        = 2f;

    [Header("[ 상태이상 계열 ]")]
    [Tooltip("부싯돌 — 화상 틱 피해")]       public float burnDamagePerTick  = 5f;
    [Tooltip("부싯돌 — 화상 틱 간격(초)")]   public float burnTickInterval   = 0.5f;
    [Tooltip("부싯돌 — 화상 지속 시간(초)")] public float burnDuration       = 3f;

    [Tooltip("메두사의 이빨 — 독 틱 피해")]       public float poisonDamagePerTick = 3f;
    [Tooltip("메두사의 이빨 — 독 틱 간격(초)")]   public float poisonTickInterval  = 1f;
    [Tooltip("메두사의 이빨 — 독 지속 시간(초)")] public float poisonDuration      = 5f;

    [Tooltip("사막의 전갈 꼬리 — 출혈 틱 피해")]       public float bleedDamagePerTick = 4f;
    [Tooltip("사막의 전갈 꼬리 — 출혈 틱 간격(초)")]   public float bleedTickInterval  = 0.5f;
    [Tooltip("사막의 전갈 꼬리 — 출혈 지속 시간(초)")] public float bleedDuration      = 4f;
    [Tooltip("사막의 전갈 꼬리 — 출혈 발동 확률")]     public float bleedChance        = 0.2f;

    [Tooltip("맹독성 확산기 — 독 전이 범위")] public float poisonSpreadRadius = 3f;

    [Header("[ 유틸 계열 ]")]
    public float potionInvincibleTime   = 2f;
    public float autoHealInterval       = 5f;
    public float autoHealAmount         = 2f;

    [Header("[ MovingDamage — 대지의 신발 ]")]
    [Tooltip("이동 중 주변 적에게 주는 초당 피해")]
    public float movingDamagePerSec     = 5f;
    [Tooltip("이동 판정 최소 속도")]
    public float movingDamageMinSpeed   = 0.5f;
    [Tooltip("피해 범위")]
    public float movingDamageRadius     = 1.5f;

    [Header("[ BurningAura — 화염의 외투 ]")]
    [Tooltip("주변 적에게 주는 초당 피해")]
    public float burningAuraDamagePerSec = 8f;
    [Tooltip("피해 틱 간격(초)")]
    public float burningAuraTickInterval = 0.5f;
    [Tooltip("화염 범위")]
    public float burningAuraRadius      = 2.5f;

    [Header("[ ExecutionEye — 집행자의 눈가리개 ]")]
    [Tooltip("즉사 발동 HP 비율 (0.2 = 20% 이하)")]
    public float executionHpThreshold   = 0.2f;
    [Tooltip("즉사 확률")]
    public float executionChance        = 0.3f;

    [Header("[ MidasGlove — 미다스의 장갑 ]")]
    [Tooltip("처치 시 추가 골드")]
    public int midasGoldPerKill         = 2;

    [Header("[ BlackHolePull — 검은 구멍 ]")]
    [Tooltip("끌어당기는 범위")]
    public float blackHoleRadius        = 6f;
    [Tooltip("끌어당기는 힘")]
    public float blackHoleForce         = 3f;
    [Tooltip("틱 간격(초)")]
    public float blackHoleTickInterval  = 0.1f;

    [Header("[ GoldenFinger — 황금 손가락 ]")]
    [Tooltip("골드 1개당 공격력 배율 보너스")]
    public float goldenFingerRatioPerCoin = 0.001f;
    [Tooltip("최대 보너스 비율")]
    public float goldenFingerMaxBonus   = 0.5f;

    [Header("[ MinervaWisdom — 미네르바의 지혜 ]")]
    [Tooltip("룬 쿨타임 배율 (0.5 = 절반)")]
    public float minervaCooldownMultiplier = 0.7f;

    [Header("[ TimeStop — 시간술사의 모래시계 ]")]
    [Tooltip("시간 정지 지속 시간(초)")]
    public float timeStopDuration       = 3f;
    [Tooltip("시간 정지 쿨타임(초)")]
    public float timeStopCooldown       = 30f;
    float timeStopTimer = 0f;

    // DimensionBoots
    float lastMoveSpeedBonus = 0f;

    // GodShield
    Coroutine godShieldRoutine;
    bool godShieldDamageFixed = false;

    // BloodContract
    float lastBloodContractBonus = 0f;

    // PhoenixFeather
    bool phoenixUsed = false;

    // InfiniteMana
    Coroutine infiniteManaRoutine;

    // CalamitySeed
    readonly System.Collections.Generic.Dictionary<Enemy,Coroutine> seedRoutines
        = new System.Collections.Generic.Dictionary<Enemy,Coroutine>();

    // LightningStrike / ChainLightning
    [Header("[ LightningStrike — 번개 맞은 나뭇가지 ]")]
    // Resources/Effects/LightningEffect 에서 자동 로드
    GameObject lightningEffectPrefab;
    [Tooltip("낙뢰 발동 확률")]           public float lightningChance       = 0.1f;
    [Tooltip("낙뢰 피해")]                public float lightningDamage        = 30f;
    [Tooltip("낙뢰 범위")]                public float lightningRadius        = 1.5f;
    [Tooltip("낙뢰 이펙트 지속 시간")]    public float lightningEffectTime    = 0.5f;
    [Tooltip("낙뢰 이펙트 크기")]          public float lightningEffectScale   = 10f;

    [Header("[ ChainLightning — 에키드나의 목걸이 ]")]
    // Resources/Effects/ChainLightningEffect 에서 자동 로드
    GameObject chainLightningEffectPrefab;

    [Header("[ RevengeArrow — 가시 목걸이 ]")]
    // Resources/Effects/RevengeArrowEffect 에서 자동 로드
    GameObject revengeArrowEffectPrefab;
    [Tooltip("연쇄 번개 발동 확률")]      public float chainLightningChance   = 0.15f;
    [Tooltip("연쇄 번개 피해")]           public float chainLightningDamage   = 15f;
    [Tooltip("연쇄 최대 횟수")]           public int   chainLightningCount    = 3;
    [Tooltip("연쇄 탐색 범위")]           public float chainLightningRadius   = 5f;
    [Tooltip("연쇄 번개 이펙트 지속 시간")] public float chainLightningEffectTime = 0.4f;
    [Tooltip("연쇄 번개 이펙트 크기")]      public float chainLightningEffectScale = 5f;

    [Header("[ RevengeArrow — 가시 목걸이 ]")]
    [Tooltip("피격 시 발사할 화살 개수")] public int   revengeArrowCount      = 8;
    [Tooltip("화살 피해 배율 (공격력 ×)")]public float revengeArrowDamageRatio= 0.3f;
    [Tooltip("화살 이동 속도")]           public float revengeArrowSpeed      = 8f;
    [Tooltip("화살 사거리")]              public float revengeArrowRange      = 6f;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        // 이펙트 프리팹 자동 로드 (Assets/Resources/Effects/ 폴더에 있어야 함)
        lightningEffectPrefab      = Resources.Load<GameObject>("Effects/LightningEffect");
        chainLightningEffectPrefab = Resources.Load<GameObject>("Effects/ChainLightningEffect");

        if (lightningEffectPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/LightningEffect 프리팹을 찾을 수 없습니다.");
        if (chainLightningEffectPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/ChainLightningEffect 프리팹을 찾을 수 없습니다.");

        revengeArrowEffectPrefab = Resources.Load<GameObject>("Effects/RevengeArrowEffect");
        if (revengeArrowEffectPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/RevengeArrowEffect 프리팹을 찾을 수 없습니다.");
    }

    // ───────────────────────────────────────────
    //  효과 활성화
    // ───────────────────────────────────────────
    public void Activate(AccessoryEffectType effectType, AccessoryData data)
    {
        if (effectType == AccessoryEffectType.None) return;

        switch (effectType)
        {
            case AccessoryEffectType.AutoHeal:
                if (!owned.Contains(AccessoryEffectType.AutoHeal))
                    StartCoroutine(AutoHealRoutine());
                break;

            case AccessoryEffectType.Revive:
                reviveCharges++;
                break;

            case AccessoryEffectType.BurningAura:
                if (!owned.Contains(AccessoryEffectType.BurningAura))
                {
                    if (burningAuraRoutine != null) StopCoroutine(burningAuraRoutine);
                    burningAuraRoutine = StartCoroutine(BurningAuraRoutine());
                }
                break;

            case AccessoryEffectType.BlackHolePull:
                if (!owned.Contains(AccessoryEffectType.BlackHolePull))
                    StartCoroutine(BlackHolePullRoutine());
                break;

            case AccessoryEffectType.MinervaWisdom:
                if (!owned.Contains(AccessoryEffectType.MinervaWisdom))
                    if (RuneManager.instance != null)
                        RuneManager.instance.CooldownMultiplier = minervaCooldownMultiplier;
                break;

            case AccessoryEffectType.GoldenFinger:
                if (!owned.Contains(AccessoryEffectType.GoldenFinger))
                {
                    if (goldenFingerRoutine != null) StopCoroutine(goldenFingerRoutine);
                    goldenFingerRoutine = StartCoroutine(GoldenFingerRoutine());
                }
                break;

            case AccessoryEffectType.ExtraRuneSlot:
                RuneManager.instance?.AddExtraSlot();
                break;

            case AccessoryEffectType.TheLastRune:
                if (!owned.Contains(AccessoryEffectType.TheLastRune))
                {
                    if (RuneManager.instance != null)
                        RuneManager.instance.CooldownMultiplier = 0f;
                    Debug.Log("[AccessoryEffect] The Last Rune — 룬 쿨타임 0!");
                }
                break;

            case AccessoryEffectType.PhoenixFeather:
                phoenixUsed = false; // 획득할 때마다 부활 초기화
                break;

            case AccessoryEffectType.GodShield:
                if (!owned.Contains(AccessoryEffectType.GodShield))
                {
                    if (godShieldRoutine != null) StopCoroutine(godShieldRoutine);
                    godShieldRoutine = StartCoroutine(GodShieldRoutine());
                }
                break;

            case AccessoryEffectType.InfiniteMana:
                if (!owned.Contains(AccessoryEffectType.InfiniteMana))
                {
                    if (infiniteManaRoutine != null) StopCoroutine(infiniteManaRoutine);
                    infiniteManaRoutine = StartCoroutine(InfiniteManaRoutine());
                }
                break;

            case AccessoryEffectType.LightningStrike:
            case AccessoryEffectType.ChainLightning:
            case AccessoryEffectType.RevengeArrow:
                // 훅(NotifyEnemyHit / NotifyPlayerDamaged)에서 처리
                break;

            default:
                if (!IsImplemented(effectType))
                    Debug.Log($"[AccessoryEffect] '{effectType}' 효과는 아직 구현되지 않았습니다.");
                break;
        }

        owned.Add(effectType);
    }

    bool IsImplemented(AccessoryEffectType t) =>
        t == AccessoryEffectType.DamageReflect   ||
        t == AccessoryEffectType.SpeedOnHit      ||
        t == AccessoryEffectType.SlowOnHit       ||
        t == AccessoryEffectType.FreezeChance    ||
        t == AccessoryEffectType.LifeStealOnKill ||
        t == AccessoryEffectType.GoldOnHit       ||
        t == AccessoryEffectType.ShieldOnLowHP   ||
        t == AccessoryEffectType.BlockHeavyDamage||
        t == AccessoryEffectType.InvincibleOnPotion||
        t == AccessoryEffectType.MovingDamage    ||
        t == AccessoryEffectType.ExecutionEye    ||
        t == AccessoryEffectType.MidasGlove      ||
        t == AccessoryEffectType.TimeStop        ||
        t == AccessoryEffectType.ExtraRuneSlot   ||
        t == AccessoryEffectType.TheLastRune      ||
        t == AccessoryEffectType.PhoenixFeather   ||
        t == AccessoryEffectType.GodShield        ||
        t == AccessoryEffectType.InfiniteMana     ||
        t == AccessoryEffectType.BloodContract    ||
        t == AccessoryEffectType.DimensionBoots   ||
        t == AccessoryEffectType.CalamitySeed    ||
        t == AccessoryEffectType.BurnOnAttack    ||
        t == AccessoryEffectType.PoisonOnAttack  ||
        t == AccessoryEffectType.BleedOnAttack   ||
        t == AccessoryEffectType.PoisonSpread    ||
        t == AccessoryEffectType.LightningStrike ||
        t == AccessoryEffectType.ChainLightning  ||
        t == AccessoryEffectType.RevengeArrow;

    public bool Has(AccessoryEffectType type) => owned.Contains(type);

    // ───────────────────────────────────────────
    //  훅 1) 받을 피해 수정
    // ───────────────────────────────────────────
    public float ModifyIncomingDamage(float finalDamage)
    {
        if (Has(AccessoryEffectType.BlockHeavyDamage) && PlayerStats.Instance != null)
        {
            bool isHeavy = finalDamage >= PlayerStats.Instance.MaxHP * heavyDamageRatio;
            if (isHeavy && Random.value < blockChance)
            {
                Debug.Log("[AccessoryEffect] 단단한 껍질 — 큰 피해 무효화!");
                return 0f;
            }
        }
        // 신의 방패 — 활성 중 받는 피해 1 고정
        if (Has(AccessoryEffectType.GodShield) && godShieldDamageFixed)
            return 1f;

        return finalDamage;
    }

    // ───────────────────────────────────────────
    //  훅 2) 플레이어 피격 후
    // ───────────────────────────────────────────
    public void NotifyPlayerDamaged(float finalDamage)
    {
        if (finalDamage <= 0f) return;

        Vector3 playerPos = PlayerStats.Instance != null
            ? PlayerStats.Instance.transform.position : Vector3.zero;

        if (Has(AccessoryEffectType.DamageReflect) && PlayerStats.Instance != null)
        {
            float reflectRatio = PlayerStats.Instance.DamageReflect;
            if (reflectRatio > 0f)
            {
                float reflectDamage = finalDamage * reflectRatio;
                foreach (Enemy e in FindEnemiesAround(playerPos, reflectRadius))
                    e.TakeDamage(reflectDamage);
            }
        }

        if (Has(AccessoryEffectType.SpeedOnHit))
        {
            if (speedOnHitRoutine != null) StopCoroutine(speedOnHitRoutine);
            speedOnHitRoutine = StartCoroutine(SpeedOnHitRoutine());
        }

        if (Has(AccessoryEffectType.SlowOnHit))
        {
            foreach (Enemy e in FindEnemiesAround(playerPos, slowOnHitRadius))
                e.ApplyFreeze(slowOnHitFreezeTime);
        }

        // 가시 목걸이 — 피격 시 8방향 화살 발사
        if (Has(AccessoryEffectType.RevengeArrow))
            FireRevengeArrows(playerPos);
    }

    // ───────────────────────────────────────────
    //  훅 3) 사망 직전 부활
    // ───────────────────────────────────────────
    public bool TryRevive()
    {
        if (reviveCharges <= 0 || PlayerStats.Instance == null) return false;
        reviveCharges--;
        PlayerStats.Instance.SetCurrentHPDirect(PlayerStats.Instance.MaxHP * reviveHpRatio);
        Debug.Log($"[AccessoryEffect] 부활의 씨앗 발동! (남은 횟수: {reviveCharges})");
        return true;
    }

    /// <summary>불사조의 깃털 — 사망 직전 풀체력 부활 (1회)</summary>
    public bool TryPhoenixRevive()
    {
        if (!Has(AccessoryEffectType.PhoenixFeather) || phoenixUsed) return false;
        if (PlayerStats.Instance == null) return false;

        phoenixUsed = true;
        PlayerStats.Instance.SetCurrentHPDirect(PlayerStats.Instance.MaxHP);
        PlayerStats.Instance.GrantInvincibility(5f);
        Debug.Log("[AccessoryEffect] 불사조의 깃털 — 풀체력 부활 + 5초 무적!");
        return true;
    }

    // ───────────────────────────────────────────
    //  훅 4) 적 적중 시
    // ───────────────────────────────────────────
    public void NotifyEnemyHit(Enemy enemy)
    {
        // 황금 손목 보호대
        if (Has(AccessoryEffectType.GoldOnHit) && Random.value < goldOnHitChance)
            GameManager.instance?.AddCoin(1);

        // 눈꽃 송이
        if (Has(AccessoryEffectType.FreezeChance) && Random.value < freezeChance)
            enemy.ApplyFreeze(freezeDuration);

        // 집행자의 눈가리개 — HP 일정 % 이하 적 즉사
        if (Has(AccessoryEffectType.ExecutionEye))
        {
            float hpRatio = enemy.health / enemy.maxHealth;
            if (hpRatio <= executionHpThreshold && Random.value < executionChance)
            {
                Debug.Log("[AccessoryEffect] 집행자의 눈가리개 — 즉사!");
                enemy.KillInstantly();
                return;
            }
        }

        // 재앙의 씨앗 — 적중 시 씨앗 심기 (3초 후 최대체력 5% 피해)
        if (Has(AccessoryEffectType.CalamitySeed) && enemy != null && enemy.IsLive)
        {
            if (!seedRoutines.ContainsKey(enemy) || seedRoutines[enemy] == null)
                seedRoutines[enemy] = StartCoroutine(CalamitySeedRoutine(enemy));
        }

        // 부싯돌 — 공격 시 화상
        if (Has(AccessoryEffectType.BurnOnAttack) && enemy != null && enemy.IsLive)
            enemy.ApplyBurn(burnDamagePerTick, burnTickInterval, burnDuration);

        // 메두사의 이빨 — 공격 시 독
        if (Has(AccessoryEffectType.PoisonOnAttack) && enemy != null && enemy.IsLive)
            enemy.ApplyPoison(poisonDamagePerTick, poisonTickInterval, poisonDuration);

        // 사막의 전갈 꼬리 — 확률 출혈
        if (Has(AccessoryEffectType.BleedOnAttack) && enemy != null && enemy.IsLive)
            if (Random.value < bleedChance)
                enemy.ApplyBleed(bleedDamagePerTick, bleedTickInterval, bleedDuration);

        // 번개 맞은 나뭇가지 — 확률 낙뢰 (적중 위치 주변 광역 피해)
        if (Has(AccessoryEffectType.LightningStrike) && enemy != null && enemy.IsLive)
            if (Random.value < lightningChance)
                TriggerLightning(enemy.transform.position, lightningDamage, lightningRadius, null);

        // 에키드나의 목걸이 — 확률 연쇄 번개
        if (Has(AccessoryEffectType.ChainLightning) && enemy != null && enemy.IsLive)
            if (Random.value < chainLightningChance)
                TriggerChainLightning(enemy, chainLightningDamage, chainLightningCount);
    }

    // ───────────────────────────────────────────
    //  훅 5) 적 처치 시
    // ───────────────────────────────────────────
    public void NotifyEnemyKilled()
    {
        // 흡혈귀의 송곳니
        if (Has(AccessoryEffectType.LifeStealOnKill) && PlayerStats.Instance != null)
            PlayerStats.Instance.Heal(lifeStealAmount);

        // 미다스의 장갑
        if (Has(AccessoryEffectType.MidasGlove) && GameManager.instance != null)
            GameManager.instance.AddCoin(midasGoldPerKill);
    }

    /// <summary>맹독성 확산기 — 독 상태인 적 처치 시 주변 적에게 독 전이.</summary>
    public void NotifyPoisonedEnemyKilled(Enemy killedEnemy)
    {
        if (!Has(AccessoryEffectType.PoisonSpread)) return;
        if (killedEnemy == null) return;

        foreach (Enemy nearby in FindEnemiesAround(killedEnemy.transform.position, poisonSpreadRadius))
        {
            if (nearby == killedEnemy) continue;
            nearby.ApplyPoison(poisonDamagePerTick, poisonTickInterval, poisonDuration);
        }
        Debug.Log("[AccessoryEffect] 맹독성 확산기 — 독 전이!");
    }

    // ───────────────────────────────────────────
    //  훅 6) 포션 사용 시
    // ───────────────────────────────────────────
    public void NotifyPotionUsed()
    {
        if (Has(AccessoryEffectType.InvincibleOnPotion) && PlayerStats.Instance != null)
            PlayerStats.Instance.GrantInvincibility(potionInvincibleTime);
    }

    // ───────────────────────────────────────────
    //  Update — 지속 체크형 효과
    // ───────────────────────────────────────────
    void Update()
    {
        if (PlayerStats.Instance == null) return;

        // 강철의 심장 — 체력 낮을 때 방어 토글
        if (Has(AccessoryEffectType.ShieldOnLowHP))
        {
            bool isLow = PlayerStats.Instance.CurrentHP
                         <= PlayerStats.Instance.MaxHP * lowHpThreshold;
            if (isLow && !lowHpShieldActive)
            {
                PlayerStats.Instance.AddMulti(StatType.DamageReduction, lowHpShieldBonus);
                lowHpShieldActive = true;
            }
            else if (!isLow && lowHpShieldActive)
            {
                PlayerStats.Instance.AddMulti(StatType.DamageReduction, -lowHpShieldBonus);
                lowHpShieldActive = false;
            }
        }

        // 대지의 신발 — 이동 중 주변 적 피해
        if (Has(AccessoryEffectType.MovingDamage))
        {
            Vector3 curPos = PlayerStats.Instance.transform.position;
            float moved = Vector3.Distance(curPos, lastPosition) / Time.deltaTime;
            if (moved >= movingDamageMinSpeed)
            {
                float dmg = movingDamagePerSec * Time.deltaTime;
                foreach (Enemy e in FindEnemiesAround(curPos, movingDamageRadius))
                    e.TakeDamage(dmg);
            }
            lastPosition = curPos;
        }

        // 시간술사의 모래시계 — 쿨타임 체크 후 시간 정지
        if (Has(AccessoryEffectType.TimeStop) && timeStopRoutine == null)
        {
            timeStopTimer += Time.deltaTime;
            if (timeStopTimer >= timeStopCooldown)
            {
                timeStopTimer = 0f;
                timeStopRoutine = StartCoroutine(TimeStopRoutine());
            }
        }

        // 차원 여행자의 장화 — 이속 비례 공격력 (1초마다 갱신)
        if (Has(AccessoryEffectType.DimensionBoots) && PlayerStats.Instance != null)
        {
            float newBonus = (PlayerStats.Instance.MovementSpeed - 1f) * 0.5f;
            newBonus = Mathf.Max(0f, newBonus);
            if (!Mathf.Approximately(newBonus, lastMoveSpeedBonus))
            {
                PlayerStats.Instance.AddMulti(StatType.AttackPower, -lastMoveSpeedBonus);
                PlayerStats.Instance.AddMulti(StatType.AttackPower,  newBonus);
                lastMoveSpeedBonus = newBonus;
            }
        }

        // 피의 계약서 — 현재 HP에 비례 공격력 (매 프레임)
        if (Has(AccessoryEffectType.BloodContract) && PlayerStats.Instance != null)
        {
            float hpRatio    = PlayerStats.Instance.CurrentHP / PlayerStats.Instance.MaxHP;
            float newBonus   = (1f - hpRatio) * 0.7f; // HP가 낮을수록 최대 +70%
            if (!Mathf.Approximately(newBonus, lastBloodContractBonus))
            {
                PlayerStats.Instance.AddMulti(StatType.AttackPower, -lastBloodContractBonus);
                PlayerStats.Instance.AddMulti(StatType.AttackPower,  newBonus);
                lastBloodContractBonus = newBonus;
            }
        }
    }

    // ───────────────────────────────────────────
    //  코루틴 — 지속 효과
    // ───────────────────────────────────────────

    IEnumerator AutoHealRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoHealInterval);
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.Heal(autoHealAmount);
        }
    }

    IEnumerator SpeedOnHitRoutine()
    {
        if (PlayerStats.Instance == null) yield break;
        PlayerStats.Instance.AddMulti(StatType.MovementSpeed, speedOnHitBonus);
        yield return new WaitForSeconds(speedOnHitDuration);
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.AddMulti(StatType.MovementSpeed, -speedOnHitBonus);
        speedOnHitRoutine = null;
    }

    IEnumerator BurningAuraRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(burningAuraTickInterval);
            if (PlayerStats.Instance == null) continue;
            float dmg = burningAuraDamagePerSec * burningAuraTickInterval;
            foreach (Enemy e in FindEnemiesAround(
                PlayerStats.Instance.transform.position, burningAuraRadius))
                e.TakeDamage(dmg);
        }
    }

    IEnumerator BlackHolePullRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(blackHoleTickInterval);
            if (PlayerStats.Instance == null) continue;

            Vector3 center = PlayerStats.Instance.transform.position;
            foreach (Enemy e in FindEnemiesAround(center, blackHoleRadius))
            {
                Vector3 dir = (center - e.transform.position).normalized;
                e.transform.position += dir * blackHoleForce * blackHoleTickInterval;
            }
        }
    }

    IEnumerator GoldenFingerRoutine()
    {
        float lastBonus = 0f;
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (PlayerStats.Instance == null || GameManager.instance == null) continue;

            // 이전 보너스 제거 후 새 보너스 적용
            if (lastBonus > 0f)
                PlayerStats.Instance.AddMulti(StatType.AttackPower, -lastBonus);

            float newBonus = Mathf.Min(
                GameManager.instance.Coin * goldenFingerRatioPerCoin,
                goldenFingerMaxBonus);
            PlayerStats.Instance.AddMulti(StatType.AttackPower, newBonus);
            lastBonus = newBonus;
        }
    }

    IEnumerator TimeStopRoutine()
    {
        Debug.Log("[AccessoryEffect] 시간술사의 모래시계 — 시간 정지!");
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(timeStopDuration);
        Time.timeScale = 1f;
        Debug.Log("[AccessoryEffect] 시간 정지 해제");
        timeStopRoutine = null;
    }

    /// <summary>신의 방패 — 15초 피해 1 고정, 이후 재충전 15초</summary>
    IEnumerator GodShieldRoutine()
    {
        while (true)
        {
            godShieldDamageFixed = true;
            Debug.Log("[AccessoryEffect] 신의 방패 활성화 — 피해 1 고정");
            yield return new WaitForSeconds(15f);
            godShieldDamageFixed = false;
            Debug.Log("[AccessoryEffect] 신의 방패 비활성화 — 재충전 중");
            yield return new WaitForSeconds(15f);
        }
    }

    /// <summary>무한의 마력 — 30초마다 3초간 룬 쿨타임 0</summary>
    IEnumerator InfiniteManaRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f);
            if (RuneManager.instance == null) continue;
            float prev = RuneManager.instance.CooldownMultiplier;
            RuneManager.instance.CooldownMultiplier = 0f;
            Debug.Log("[AccessoryEffect] 무한의 마력 — 쿨타임 0!");
            yield return new WaitForSeconds(3f);
            // TheLastRune이 없으면 원래 값으로 복구
            if (!Has(AccessoryEffectType.TheLastRune))
                RuneManager.instance.CooldownMultiplier = prev;
        }
    }

    /// <summary>재앙의 씨앗 — 3초 후 최대체력 5% 고정 피해</summary>
    IEnumerator CalamitySeedRoutine(Enemy enemy)
    {
        yield return new WaitForSeconds(3f);
        if (enemy != null && enemy.IsLive)
        {
            float dmg = enemy.maxHealth * 0.05f;
            enemy.TakeDamage(dmg);
            Debug.Log($"[AccessoryEffect] 재앙의 씨앗 — {dmg:F0} 피해!");
        }
        if (seedRoutines.ContainsKey(enemy))
            seedRoutines.Remove(enemy);
    }

    // ───────────────────────────────────────────
    //  낙뢰 / 연쇄 번개 / 복수 화살
    // ───────────────────────────────────────────

    /// <summary>낙뢰 — 지정 위치 주변 적에게 광역 피해 + 이펙트</summary>
    void TriggerLightning(Vector3 pos, float damage, float radius, Enemy exclude)
    {
        // 이펙트 소환
        if (lightningEffectPrefab != null)
            StartCoroutine(SpawnEffectRoutine(lightningEffectPrefab, pos, lightningEffectTime, lightningEffectScale));

        foreach (Enemy e in FindEnemiesAround(pos, radius))
        {
            if (e == exclude) continue;
            e.TakeDamage(damage);
        }
        Debug.Log($"[AccessoryEffect] 낙뢰 발동 — {damage:F0} 피해 (반경 {radius}m)");
    }

    /// <summary>연쇄 번개 — 첫 적부터 가까운 적으로 count회 연쇄 + 이펙트</summary>
    void TriggerChainLightning(Enemy first, float damage, int count)
    {
        Enemy current = first;
        var hit = new HashSet<Enemy> { first };
        first.TakeDamage(damage);

        // 첫 번째 이펙트
        if (chainLightningEffectPrefab != null)
            StartCoroutine(SpawnEffectRoutine(chainLightningEffectPrefab, first.transform.position, chainLightningEffectTime, chainLightningEffectScale));

        for (int i = 1; i < count; i++)
        {
            Enemy next = null;
            float minDist = float.MaxValue;

            foreach (Enemy e in FindEnemiesAround(current.transform.position, chainLightningRadius))
            {
                if (hit.Contains(e)) continue;
                float dist = Vector3.Distance(current.transform.position, e.transform.position);
                if (dist < minDist) { minDist = dist; next = e; }
            }

            if (next == null) break;
            next.TakeDamage(damage * (1f - i * 0.1f)); // 연쇄마다 10% 감쇠
            hit.Add(next);

            // 연쇄 이펙트
            if (chainLightningEffectPrefab != null)
                StartCoroutine(SpawnEffectRoutine(chainLightningEffectPrefab, next.transform.position, chainLightningEffectTime, chainLightningEffectScale));

            current = next;
        }
        Debug.Log($"[AccessoryEffect] 연쇄 번개 — {hit.Count}마리 적중");
    }

    /// <summary>가시 목걸이 — 피격 시 n방향 화살 발사 (WeaponInstance 없이 직접 피해)</summary>
    void FireRevengeArrows(Vector3 from)
    {
        if (PlayerStats.Instance == null) return;

        float baseDamage = PlayerStats.Instance.AttackPower * revengeArrowDamageRatio;
        float angleStep  = 360f / revengeArrowCount;

        for (int i = 0; i < revengeArrowCount; i++)
        {
            float   angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 dir   = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            StartCoroutine(ArrowRoutine(from, dir, baseDamage));
        }
    }

    IEnumerator ArrowRoutine(Vector3 startPos, Vector2 dir, float damage)
    {
        float elapsed  = 0f;
        float duration = revengeArrowRange / revengeArrowSpeed;
        Vector3 pos    = startPos;
        var hit = new HashSet<Enemy>();

        // 화살 이펙트 오브젝트 소환
        GameObject arrowFx = null;
        if (revengeArrowEffectPrefab != null)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrowFx = Instantiate(revengeArrowEffectPrefab, pos, Quaternion.Euler(0, 0, angle));
            arrowFx.transform.localScale = Vector3.one * 5f;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            pos     += (Vector3)(dir * revengeArrowSpeed * Time.deltaTime);

            // 이펙트 위치 갱신
            if (arrowFx != null)
                arrowFx.transform.position = pos;

            // 이동 경로에서 적 감지
            Collider2D[] cols = Physics2D.OverlapCircleAll(pos, 0.3f);
            foreach (Collider2D col in cols)
            {
                Enemy e = col.GetComponent<Enemy>();
                if (e != null && e.IsLive && !hit.Contains(e))
                {
                    e.TakeDamage(damage);
                    hit.Add(e);
                }
            }
            yield return null;
        }

        // 화살 이펙트 제거
        if (arrowFx != null)
            Destroy(arrowFx);
    }

    /// <summary>이펙트 프리팹을 소환하고 일정 시간 후 제거</summary>
    IEnumerator SpawnEffectRoutine(GameObject prefab, Vector3 pos, float duration, float scale = 1f)
    {
        GameObject fx = Instantiate(prefab, pos, Quaternion.identity);
        fx.transform.localScale = Vector3.one * scale;
        yield return new WaitForSeconds(duration);
        if (fx != null) Destroy(fx);
    }

    // ───────────────────────────────────────────
    //  유틸
    // ───────────────────────────────────────────
    List<Enemy> FindEnemiesAround(Vector3 center, float radius)
    {
        var result = new List<Enemy>();
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        foreach (Collider2D hit in hits)
        {
            Enemy e = hit.GetComponent<Enemy>();
            if (e != null) result.Add(e);
        }
        return result;
    }
}