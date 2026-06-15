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

    // CalamitySeed (재앙의 씨앗)
    [Header("[ CalamitySeed — 재앙의 씨앗 ]")]
    GameObject seedEffectPrefab;
    GameObject seedExplosionPrefab;
    [Tooltip("씨앗 폭발까지 시간(초)")] public float seedFuseTime      = 2f;
    [Tooltip("폭발 피해 (최대체력 %)")] public float seedDamageRatio   = 0.05f;
    [Tooltip("전이 범위")]              public float seedSpreadRadius  = 5f;
    [Tooltip("씨앗 머리 위 오프셋")]    public float seedHeadOffset    = 1f;
    [Tooltip("프리팹 크기")]            public float seedScale         = 2f;

    // InfiniteMana (무한의 마력)
    [Header("[ InfiniteMana — 무한의 마력 ]")]
    GameObject infiniteManaPrefab;
    GameObject infiniteManaInstance;
    [Tooltip("효과 지속 시간(초)")]  public float infiniteManaDuration   = 30f;
    [Tooltip("쿨타임(초)")]          public float infiniteManaCooldown   = 15f;
    [Tooltip("공격속도 보너스")]     public float infiniteManaSpeedBonus = 0.5f;
    [Tooltip("투사체 배율")]         public int   infiniteManaProjectiles = 5;
    [Tooltip("프리팹 크기")]         public float infiniteManaScale      = 2f;

    // GodShield (신의 방패)
    [Header("[ GodShield — 신의 방패 ]")]
    GameObject godShieldPrefab;
    GameObject godShieldInstance;
    [Tooltip("방패 활성 시간(초)")]   public float godShieldActiveTime  = 15f;
    [Tooltip("방패 재충전 시간(초)")] public float godShieldRechargeTime = 15f;
    [Tooltip("프리팹 크기")]          public float godShieldScale        = 2f;

    // TimeStop (시간술사의 모래시계)
    [Header("[ TimeStop — 시간술사의 모래시계 ]")]
    GameObject hourglassPrefab;
    [Tooltip("발동 주기(초)")]       public float hourglassCooldown  = 10f;
    [Tooltip("정지 지속 시간(초)")] public float hourglassDuration  = 2f;
    [Tooltip("프리팹 알파값")]       public float hourglassAlpha     = 0.6f;
    [Tooltip("프리팹 크기")]         public float hourglassScale     = 5f;

    // MidasGlove (황금색 틴트)
    SpriteRenderer playerSpriter;

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

    // DimensionBoots
    float lastMoveSpeedBonus = 0f;

    // GodShield
    Coroutine godShieldRoutine;
    bool godShieldDamageFixed = false;

    // BloodContract
    float lastBloodContractBonus = 0f;

    // PhoenixFeather
    bool phoenixUsed = false;
    [Tooltip("오라 화상 범위 (크기와 동일)")] public float phoenixAuraBurnRadius   = 4f;
    [Tooltip("화상 틱 피해")]                  public float phoenixAuraBurnDmg      = 10f;
    [Tooltip("화상 틱 간격")]                  public float phoenixAuraBurnInterval = 0.5f;
    [Tooltip("화상 지속 시간")]                public float phoenixAuraBurnDuration = 2f;
    Coroutine phoenixAuraBurnRoutine;

    // InfiniteMana
    Coroutine infiniteManaRoutine;

    // CalamitySeed
    readonly System.Collections.Generic.Dictionary<Enemy,Coroutine> seedRoutines
        = new System.Collections.Generic.Dictionary<Enemy,Coroutine>();

    // MinervaWisdom (미네르바의 지혜)
    [Header("[ MinervaWisdom — 미네르바의 지혜 ]")]
    GameObject minervaStackPrefab;
    GameObject minervaStackInstance;
    [Tooltip("스프라이트 시트 프레임 수")] public int   minervaMaxFrames    = 28;
    [Tooltip("스택당 공격력 보너스")]       public float minervaStackBonus    = 0.1f;
    int   minervaCurrentFrame = 0;
    float minervaCurrentBonus = 0f;
    // 스프라이트 시트 슬라이싱된 스프라이트 배열 (Resources에서 로드)
    Sprite[] minervaSprites;

    // PhoenixFeather (불사조의 망토)
    [Header("[ PhoenixFeather — 불사조의 망토 ]")]
    GameObject phoenixExplosionPrefab;
    GameObject phoenixAuraPrefab;
    [Tooltip("폭발 피해 (공격력 배율)")] public float phoenixExplosionRatio  = 30f;   // 3000%
    [Tooltip("폭발 범위")]               public float phoenixExplosionRadius  = 10f;   // 크기 10배 기준
    [Tooltip("폭발 이펙트 크기")]        public float phoenixExplosionScale   = 10f;
    [Tooltip("무적 지속 시간(초)")]      public float phoenixInvincibleTime   = 5f;
    [Tooltip("오라 프리팹 크기")]        public float phoenixAuraScale        = 18f;

    // AbyssLord (심연의 군주)
    [Header("[ AbyssLord — 심연의 군주 ]")]
    GameObject tentaclePrefab;
    [Tooltip("촉수 개수")]            public int   abyssCount          = 4;
    [Tooltip("소환 반경")]            public float abyssSpawnRadius     = 3f;
    [Tooltip("촉수 지속 시간(초)")]   public float abyssDuration        = 3f;
    [Tooltip("쿨타임(초)")]           public float abyssCooldown        = 5f;
    [Tooltip("촉수 공격 범위")]       public float abyssAttackRadius    = 4f;
    [Tooltip("촉수 틱 간격(초)")]     public float abyssTickInterval    = 0.5f;
    [Tooltip("촉수 틱 피해")]         public float abyssDamagePerTick   = 100f;
    [Tooltip("흡혈 비율 (최대체력 %)")]public float abyssLifeStealRatio = 0.01f;
    Coroutine abyssRoutine;

    // ZeusJudgment (제우스의 심판)
    [Header("[ ZeusJudgment — 제우스의 심판 ]")]
    GameObject zeusLightningPrefab;
    GameObject zeusChainPrefab;
    [Tooltip("발동 확률")] public float zeusChance = 0.2f;
    [Tooltip("낙뢰 피해")] public float zeusDamage = 50f;
    [Tooltip("연쇄 피해")] public float zeusChainDamage = 30f;
    [Tooltip("연쇄 대상 수")] public int zeusChainCount = 5;
    [Tooltip("연쇄 탐색 범위")] public float zeusChainRadius = 6f;
    [Tooltip("감전(스턴) 시간")] public float zeusStunDuration = 0.5f;
    [Tooltip("낙뢰 이펙트 크기")] public float zeusLightningScale = 15f;
    [Tooltip("연쇄 이펙트 크기")] public float zeusChainScale = 10f;
    [Tooltip("이펙트 지속 시간")] public float zeusEffectTime = 0.5f;

    // BossArrow (신기한 화살 — 보스 방향 안내)
    [Header("[ BossArrow — 신기한 화살 ]")]
    GameObject bossArrowPrefab;
    GameObject bossArrowInstance;
    [HideInInspector] public Transform bossTarget;
    [Tooltip("화살표 플레이어로부터 거리")] public float bossArrowDistance = 1.5f;

    // SkeletonOnKill (흑마법의 인장 — 처치 시 주변 적 이동속도 감소)
    [Header("[ SkeletonOnKill — 흑마법의 인장 ]")]
    [Tooltip("이동속도 감소율 (0.2 = 20%)")] public float skeletonSlowRatio    = 0.2f;
    [Tooltip("감소 지속 시간(초)")]           public float skeletonSlowDuration = 3f;
    [Tooltip("범위")]                         public float skeletonSlowRadius   = 5f;

    // ShadowTracker (투명 망토)
    Coroutine shadowTrackerRoutine;
    [Header("[ ShadowTracker — 투명 망토 ]")]
    [Tooltip("은신 지속 시간(초)")] public float shadowTrackerDuration = 1f;
    [Tooltip("은신 중 알파값 (0=완전투명)")] public float shadowTrackerAlpha = 0.3f;

    // Explosion (폭탄광)
    [Header("[ Explosion — 폭탄광 ]")]
    GameObject explosionEffectPrefab;
    [Tooltip("폭발 발동 확률 (0.2 = 20%)")] public float explosionChance  = 0.2f;
    [Tooltip("폭발 피해")]                   public float explosionDamage  = 50f;
    [Tooltip("폭발 범위")]                   public float explosionRadius  = 3f;
    [Tooltip("폭발 이펙트 지속 시간")]       public float explosionEffectTime = 0.5f;
    [Tooltip("폭발 이펙트 크기")]            public float explosionEffectScale = 3f;

    // ShadowClone
    [Header("[ ShadowClone — 그림자 가면 ]")]
    GameObject shadowClonePrefab;
    [HideInInspector] public GameObject shadowCloneInstance;
    readonly HashSet<GameObject> homingBullets = new HashSet<GameObject>(); // 유도 중인 탄환
    [Tooltip("분신 이동 딜레이")] public float shadowCloneDelay = 0.15f;
    [Tooltip("분신 공격 발동 확률 (0.25 = 25%)")] public float shadowCloneAttackChance = 0.25f;
    Vector3 shadowCloneTargetPos;

    // SoulBullet
    [Header("[ SoulBullet — 영혼의 등불 ]")]
    GameObject soulBulletPrefab;
    [Tooltip("영혼 탄환 개수")]          public int   soulBulletCount       = 3;
    [Tooltip("궤도 반경")]               public float soulBulletOrbitRadius  = 2f;
    [Tooltip("궤도 회전 속도 (도/초)")]  public float soulBulletOrbitSpeed   = 180f;
    [Tooltip("유도 전환까지 시간(초)")]  public float soulBulletHomingDelay  = 5f;
    [Tooltip("유도 이동 속도")]          public float soulBulletHomingSpeed  = 8f;
    [Tooltip("유도 피해")]               public float soulBulletDamage       = 300f;
    [Tooltip("탄환 재소환 주기(초)")]    public float soulBulletRespawnTime  = 8f;
    readonly List<GameObject> soulBullets = new List<GameObject>();
    float soulBulletAngleOffset = 0f;
    Coroutine soulBulletRoutine;

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

    [Header("[ DuplicateBullet — 랜턴 ]")]
    [Tooltip("투사체 복제 확률 (0.1 = 10%)")] public float duplicateBulletChance = 0.1f;

    [Header("[ RevengeArrow — 가시 목걸이 / 마법의 구 ]")]
    // Resources/Effects/RevengeArrowEffect 에서 자동 로드
    GameObject revengeArrowEffectPrefab;
    // Resources/Effects/MagicOrbEffect 에서 자동 로드
    GameObject magicOrbEffectPrefab;
    [Tooltip("연쇄 번개 발동 확률")]      public float chainLightningChance   = 0.15f;
    [Tooltip("연쇄 번개 피해")]           public float chainLightningDamage   = 15f;
    [Tooltip("연쇄 최대 횟수")]           public int   chainLightningCount    = 3;
    [Tooltip("연쇄 탐색 범위")]           public float chainLightningRadius   = 5f;
    [Tooltip("연쇄 번개 이펙트 지속 시간")] public float chainLightningEffectTime = 0.4f;
    [Tooltip("연쇄 번개 이펙트 크기")]      public float chainLightningEffectScale = 5f;

    [Header("[ DuplicateBullet — 랜턴 ]")]

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

        explosionEffectPrefab = Resources.Load<GameObject>("Effects/ExplosionEffect");
        if (explosionEffectPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/ExplosionEffect 프리팹을 찾을 수 없습니다.");

        bossArrowPrefab = Resources.Load<GameObject>("Effects/BossArrowEffect");
        if (bossArrowPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/BossArrowEffect 프리팹을 찾을 수 없습니다.");

        seedEffectPrefab = Resources.Load<GameObject>("Effects/SeedEffect");
        if (seedEffectPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/SeedEffect 프리팹을 찾을 수 없습니다.");
        seedExplosionPrefab = Resources.Load<GameObject>("Effects/SeedExplosionEffect");
        if (seedExplosionPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/SeedExplosionEffect 프리팹을 찾을 수 없습니다.");

        infiniteManaPrefab = Resources.Load<GameObject>("Effects/InfiniteManaEffect");
        if (infiniteManaPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/InfiniteManaEffect 프리팹을 찾을 수 없습니다.");

        godShieldPrefab = Resources.Load<GameObject>("Effects/GodShieldEffect");
        if (godShieldPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/GodShieldEffect 프리팹을 찾을 수 없습니다.");

        hourglassPrefab = Resources.Load<GameObject>("Effects/HourglassEffect");
        if (hourglassPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/HourglassEffect 프리팹을 찾을 수 없습니다.");

        minervaStackPrefab = Resources.Load<GameObject>("Effects/MinervaStackEffect");
        if (minervaStackPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/MinervaStackEffect 프리팹을 찾을 수 없습니다.");
        // 스프라이트 시트 슬라이싱된 스프라이트 로드
        minervaSprites = Resources.LoadAll<Sprite>("Effects/10_weaponhit_spritesheet");
        if (minervaSprites == null || minervaSprites.Length == 0)
            Debug.LogWarning("[AccessoryEffect] 미네르바 스프라이트 시트를 찾을 수 없습니다.");

        phoenixExplosionPrefab = Resources.Load<GameObject>("Effects/PhoenixExplosionEffect");
        if (phoenixExplosionPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/PhoenixExplosionEffect 프리팹을 찾을 수 없습니다.");
        phoenixAuraPrefab = Resources.Load<GameObject>("Effects/PhoenixAuraEffect");
        if (phoenixAuraPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/PhoenixAuraEffect 프리팹을 찾을 수 없습니다.");

        tentaclePrefab = Resources.Load<GameObject>("Effects/TentacleEffect");
        if (tentaclePrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/TentacleEffect 프리팹을 찾을 수 없습니다.");

        zeusLightningPrefab = Resources.Load<GameObject>("Effects/ZeusLightningEffect");
        if (zeusLightningPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/ZeusLightningEffect 프리팹을 찾을 수 없습니다.");
        zeusChainPrefab = Resources.Load<GameObject>("Effects/ZeusChainEffect");
        if (zeusChainPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/ZeusChainEffect 프리팹을 찾을 수 없습니다.");

        shadowClonePrefab = Resources.Load<GameObject>("Effects/ShadowCloneEffect");
        if (shadowClonePrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/ShadowCloneEffect 프리팹을 찾을 수 없습니다.");

        soulBulletPrefab = Resources.Load<GameObject>("Effects/SoulBulletEffect");
        if (soulBulletPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/SoulBulletEffect 프리팹을 찾을 수 없습니다.");

        magicOrbEffectPrefab = Resources.Load<GameObject>("Effects/MagicOrbEffect");
        if (magicOrbEffectPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/MagicOrbEffect 프리팹을 찾을 수 없습니다.");
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
                    SpawnMinervaStack();
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
                // phoenixUsed는 초기화하지 않음 — 게임 내 딱 1번만 발동
                break;

            case AccessoryEffectType.GodShield:
                if (!owned.Contains(AccessoryEffectType.GodShield))
                {
                    if (godShieldRoutine != null) StopCoroutine(godShieldRoutine);
                    godShieldRoutine = StartCoroutine(GodShieldRoutine());
                }
                break;

            case AccessoryEffectType.TimeStop:
                if (!owned.Contains(AccessoryEffectType.TimeStop))
                {
                    if (timeStopRoutine != null) StopCoroutine(timeStopRoutine);
                    timeStopRoutine = StartCoroutine(HourglassRoutine());
                    Debug.Log("[AccessoryEffect] 시간술사의 모래시계 — 활성화!");
                }
                break;

            case AccessoryEffectType.InfiniteMana:
                if (!owned.Contains(AccessoryEffectType.InfiniteMana))
                {
                    if (infiniteManaRoutine != null) StopCoroutine(infiniteManaRoutine);
                    infiniteManaRoutine = StartCoroutine(InfiniteManaRoutine());
                }
                break;

            case AccessoryEffectType.DuplicateBullet:
                // WeaponInstance.Attack()에서 직접 체크
                break;

            case AccessoryEffectType.Explosion:
                // NotifyEnemyHit에서 처리
                break;

            case AccessoryEffectType.AbyssLord:
                if (!owned.Contains(AccessoryEffectType.AbyssLord))
                {
                    if (abyssRoutine != null) StopCoroutine(abyssRoutine);
                    abyssRoutine = StartCoroutine(AbyssLordRoutine());
                }
                break;

            case AccessoryEffectType.ZeusJudgment:
                // NotifyEnemyHit에서 처리
                break;

            case AccessoryEffectType.BossArrow:
                // NotifyBossSpawn/NotifyBossDead에서 처리
                break;

            case AccessoryEffectType.RandomElement:
                // NotifyEnemyHit에서 처리
                break;

            case AccessoryEffectType.ShadowTracker:
                // NotifyEvasionSuccess에서 처리
                break;

            case AccessoryEffectType.SkeletonOnKill:
                // NotifyEnemyKilled에서 처리
                break;

            case AccessoryEffectType.ShadowClone:
                if (!owned.Contains(AccessoryEffectType.ShadowClone))
                    SpawnShadowClone();
                break;

            case AccessoryEffectType.SoulBullet:
                if (!owned.Contains(AccessoryEffectType.SoulBullet))
                {
                    if (soulBulletRoutine != null) StopCoroutine(soulBulletRoutine);
                    soulBulletRoutine = StartCoroutine(SoulBulletRoutine());
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
        t == AccessoryEffectType.DuplicateBullet   ||
        t == AccessoryEffectType.Explosion          ||
        t == AccessoryEffectType.MinervaWisdom      ||
        t == AccessoryEffectType.AbyssLord          ||
        t == AccessoryEffectType.ZeusJudgment       ||
        t == AccessoryEffectType.BossArrow          ||
        t == AccessoryEffectType.RandomElement      ||
        t == AccessoryEffectType.ShadowTracker      ||
        t == AccessoryEffectType.SkeletonOnKill     ||
        t == AccessoryEffectType.ShadowClone        ||
        t == AccessoryEffectType.SoulBullet         ||
        t == AccessoryEffectType.LightningStrike ||
        t == AccessoryEffectType.ChainLightning  ||
        t == AccessoryEffectType.RevengeArrow;

    public bool Has(AccessoryEffectType type) => owned.Contains(type);

    /// <summary>신의 방패 활성 중 상태이상 면역 여부</summary>
    public bool IsStatusImmune => Has(AccessoryEffectType.GodShield) && godShieldDamageFixed;

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

    /// <summary>불사조의 망토 — 사망 직전 풀체력 부활 (스테이지당 1회)</summary>
    public bool TryPhoenixRevive()
    {
        if (!Has(AccessoryEffectType.PhoenixFeather) || phoenixUsed) return false;
        if (PlayerStats.Instance == null) return false;

        phoenixUsed = true;

        // 풀체력 부활
        PlayerStats.Instance.SetCurrentHPDirect(PlayerStats.Instance.MaxHP);

        // 5초 무적
        PlayerStats.Instance.GrantInvincibility(phoenixInvincibleTime);

        // 폭발 이펙트 + 광역 3000% 피해
        Vector3 pos = PlayerStats.Instance.transform.position;
        if (phoenixExplosionPrefab != null)
            StartCoroutine(SpawnEffectRoutine(phoenixExplosionPrefab, pos, 1f, phoenixExplosionScale));

        float explosionDamage = PlayerStats.Instance.AttackPower * phoenixExplosionRatio;
        foreach (Enemy e in FindEnemiesAround(pos, phoenixExplosionRadius))
            e.TakeDamage(explosionDamage);

        // 무적 빨간 오라 5초 동안 표시
        StartCoroutine(PhoenixAuraRoutine(pos));

        Debug.Log("[AccessoryEffect] 불사조의 망토 — 풀체력 부활 + 3000% 폭발 + 5초 무적!");
        return true;
    }

    IEnumerator PhoenixAuraRoutine(Vector3 startPos)
    {
        if (phoenixAuraPrefab == null || PlayerStats.Instance == null) yield break;

        // 플레이어 자식으로 소환 → 플레이어가 움직이면 오라도 같이 움직임
        GameObject aura = Instantiate(phoenixAuraPrefab, Vector3.zero, Quaternion.identity,
                                      PlayerStats.Instance.transform);
        aura.transform.localPosition = Vector3.zero; // 플레이어 중심에 딱 붙임
        aura.transform.localScale    = Vector3.one * phoenixAuraScale;

        // 오라 범위 내 적에게 화상 적용
        if (phoenixAuraBurnRoutine != null) StopCoroutine(phoenixAuraBurnRoutine);
        phoenixAuraBurnRoutine = StartCoroutine(PhoenixAuraBurnRoutine());

        yield return new WaitForSeconds(phoenixInvincibleTime);

        if (phoenixAuraBurnRoutine != null) StopCoroutine(phoenixAuraBurnRoutine);
        if (aura != null) Destroy(aura);
    }

    IEnumerator PhoenixAuraBurnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(phoenixAuraBurnInterval);
            if (PlayerStats.Instance == null) break;

            foreach (Enemy e in FindEnemiesAround(
                PlayerStats.Instance.transform.position, phoenixAuraBurnRadius))
            {
                e.ApplyBurn(phoenixAuraBurnDmg, phoenixAuraBurnInterval, phoenixAuraBurnDuration);
            }
        }
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

        // 제우스의 심판 — 20% 확률 거대 낙뢰 + 연쇄 5명 감전
        if (Has(AccessoryEffectType.ZeusJudgment) && enemy != null && enemy.IsLive)
        {
            if (Random.value < zeusChance)
                StartCoroutine(ZeusJudgmentRoutine(enemy));
        }

        // 불투명한 프리즘 — 공격 시 랜덤 상태이상 (화상/독/출혈 중 하나)
        if (Has(AccessoryEffectType.RandomElement) && enemy != null && enemy.IsLive)
        {
            int rand = UnityEngine.Random.Range(0, 3);
            switch (rand)
            {
                case 0: enemy.ApplyBurn(burnDamagePerTick, burnTickInterval, burnDuration); break;
                case 1: enemy.ApplyPoison(poisonDamagePerTick, poisonTickInterval, poisonDuration); break;
                case 2: enemy.ApplyBleed(bleedDamagePerTick, bleedTickInterval, bleedDuration); break;
            }
        }

        // 폭탄광 — 20% 확률 광역 폭발
        if (Has(AccessoryEffectType.Explosion) && enemy != null && enemy.IsLive)
        {
            if (Random.value < explosionChance)
            {
                Vector3 pos = enemy.transform.position;
                // 폭발 이펙트 소환
                if (explosionEffectPrefab != null)
                    StartCoroutine(SpawnEffectRoutine(explosionEffectPrefab, pos, explosionEffectTime, explosionEffectScale));
                // 광역 피해
                foreach (Enemy e in FindEnemiesAround(pos, explosionRadius))
                    e.TakeDamage(explosionDamage);
                Debug.Log("[AccessoryEffect] 폭탄광 — 폭발!");
            }
        }

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

    /// <summary>흑마법의 인장 — 처치 시 주변 적 이동속도 20% 감소 3초.</summary>
    public void NotifyEnemyKilledWithPos(Vector3 pos)
    {
        if (!Has(AccessoryEffectType.SkeletonOnKill)) return;
        foreach (Enemy e in FindEnemiesAround(pos, skeletonSlowRadius))
            e.ApplySlow(skeletonSlowRatio, skeletonSlowDuration);
        Debug.Log("[AccessoryEffect] 흑마법의 인장 — 주변 적 이동속도 감소!");
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
    //  훅 6) 회피 성공 시
    // ───────────────────────────────────────────
    public void NotifyEvasionSuccess()
    {
        if (!Has(AccessoryEffectType.ShadowTracker)) return;
        if (shadowTrackerRoutine != null) StopCoroutine(shadowTrackerRoutine);
        shadowTrackerRoutine = StartCoroutine(ShadowTrackerRoutine());
    }

    IEnumerator ShadowTrackerRoutine()
    {
        // 플레이어 SpriteRenderer 찾기
        SpriteRenderer sr = PlayerStats.Instance?.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) yield break;

        // 반투명 적용
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, shadowTrackerAlpha);
        Debug.Log("[AccessoryEffect] 투명 망토 — 은신!");

        // 모든 적 어그로 해제 (target = null)
        Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy e in enemies)
            e.ClearTarget();

        yield return new WaitForSeconds(shadowTrackerDuration);

        // 원래 알파값 복구
        if (sr != null)
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);

        // 어그로 복구
        foreach (Enemy e in enemies)
            if (e != null) e.RestoreTarget();

        shadowTrackerRoutine = null;
    }

    // ───────────────────────────────────────────
    //  훅 7) 포션 사용 시
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

        // 미다스의 장갑 — 골드 500개 단위로 황금색 틴트
        if (Has(AccessoryEffectType.MidasGlove) && GameManager.instance != null)
        {
            if (playerSpriter == null && PlayerStats.Instance != null)
                playerSpriter = PlayerStats.Instance.GetComponentInChildren<SpriteRenderer>();

            if (playerSpriter != null)
            {
                int gold = GameManager.instance.Coin;
                // 500골드마다 단계 증가, 최대 5단계 (2500골드)
                float t = Mathf.Clamp01(gold / 2500f);
                // 흰색 → 황금색 (1f, 0.84f, 0f)
                Color goldColor = Color.Lerp(Color.white, new Color(1f, 0.84f, 0f), t);
                playerSpriter.color = goldColor;
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

        // 신기한 화살 — 보스 방향으로 화살표 회전
        if (Has(AccessoryEffectType.BossArrow) && bossArrowInstance != null
            && bossTarget != null && PlayerStats.Instance != null)
        {
            Vector3 dir = (bossTarget.position - PlayerStats.Instance.transform.position).normalized;
            // 스프라이트 기본 방향이 위(↑)이므로 -90도 오프셋
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            bossArrowInstance.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            bossArrowInstance.transform.position = PlayerStats.Instance.transform.position
                                                   + dir * bossArrowDistance;
        }

        // 영혼의 등불 — 궤도 회전
        if (Has(AccessoryEffectType.SoulBullet) && soulBullets.Count > 0 && PlayerStats.Instance != null)
        {
            soulBulletAngleOffset += soulBulletOrbitSpeed * Time.deltaTime;
            float angleStep = 360f / soulBullets.Count;
            for (int i = 0; i < soulBullets.Count; i++)
            {
                if (soulBullets[i] == null) continue;
                float angle = (soulBulletAngleOffset + angleStep * i) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * soulBulletOrbitRadius;
                soulBullets[i].transform.position = PlayerStats.Instance.transform.position + offset;

                // 공전 중 충돌 피해 (0.5f 반경 내 적에게 틱 데미지)
                Collider2D[] cols = Physics2D.OverlapCircleAll(soulBullets[i].transform.position, 0.5f);
                foreach (Collider2D col in cols)
                {
                    Enemy e = col.GetComponent<Enemy>();
                    if (e != null && e.IsLive)
                        e.TakeDamage(soulBulletDamage * Time.deltaTime);
                }
            }
            // 플레이어 바라보는 방향으로 탄환 뒤집기
            UpdateSoulBulletFacing();

        }

        // 그림자 가면 — 분신이 플레이어 위치를 딜레이로 추적
        if (Has(AccessoryEffectType.ShadowClone) && shadowCloneInstance != null && PlayerStats.Instance != null)
        {
            shadowCloneTargetPos = PlayerStats.Instance.transform.position;
            shadowCloneInstance.transform.position = Vector3.Lerp(
                shadowCloneInstance.transform.position,
                shadowCloneTargetPos,
                Time.deltaTime / shadowCloneDelay
            );
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

    IEnumerator HourglassRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(hourglassCooldown);

            // 모래시계 프리팹 — 화면 중앙에 반투명으로 소환
            GameObject hourglass = null;
            if (hourglassPrefab != null)
            {
                // 카메라 중앙 위치
                Vector3 centerPos = Camera.main != null
                    ? Camera.main.transform.position
                    : Vector3.zero;
                centerPos.z = 0f;

                hourglass = Instantiate(hourglassPrefab, centerPos, Quaternion.identity);
                // 반투명 적용
                SpriteRenderer sr = hourglass.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    Color col = sr.color;
                    sr.color = new Color(col.r, col.g, col.b, hourglassAlpha);
                }

                // UI 위에 렌더링되도록 Order 설정
                if (sr != null) sr.sortingOrder = 999;
            }

            // 적/보스만 정지 (플레이어는 계속 움직임)
            Debug.Log("[AccessoryEffect] 시간술사의 모래시계 — 시간 정지!");
            Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            BossBase[] bosses = Object.FindObjectsByType<BossBase>(FindObjectsSortMode.None);

            // 적 이동속도 0으로 설정
            foreach (Enemy e in enemies) e.ApplyFreeze(hourglassDuration);
            // 보스 비활성화 (isPatternPlaying 방식으로 멈춤)
            foreach (BossBase b in bosses) b.enabled = false;

            yield return new WaitForSeconds(hourglassDuration);

            // 보스 다시 활성화
            foreach (BossBase b in bosses) if (b != null) b.enabled = true;
            Debug.Log("[AccessoryEffect] 시간 정지 해제");

            // 모래시계 제거
            if (hourglass != null) Destroy(hourglass);

            timeStopRoutine = StartCoroutine(HourglassRoutine());
            yield break;
        }
    }

    /// <summary>신의 방패 — 15초 피해 1 고정 + 상태이상 면역 + 프리팹, 이후 재충전 15초</summary>
    IEnumerator GodShieldRoutine()
    {
        while (true)
        {
            // 방패 프리팹 소환 (플레이어 자식)
            if (godShieldPrefab != null && PlayerStats.Instance != null)
            {
                if (godShieldInstance != null) Destroy(godShieldInstance);
                godShieldInstance = Instantiate(godShieldPrefab, Vector3.zero,
                                                Quaternion.identity, PlayerStats.Instance.transform);
                godShieldInstance.transform.localPosition = Vector3.zero;
                godShieldInstance.transform.localScale    = Vector3.one * godShieldScale;

                // 애니메이션 속도 3배로 설정
                foreach (Animator anim in godShieldInstance.GetComponentsInChildren<Animator>(true))
                    anim.speed = 3f;


            }

            godShieldDamageFixed = true;
            Debug.Log("[AccessoryEffect] 신의 방패 활성화 — 피해 1 고정 + 상태이상 면역");

            yield return new WaitForSeconds(godShieldActiveTime);

            // 방패 비활성화
            godShieldDamageFixed = false;
            if (godShieldInstance != null)
            {
                Destroy(godShieldInstance);
                godShieldInstance = null;
            }
            Debug.Log("[AccessoryEffect] 신의 방패 비활성화 — 재충전 중");

            yield return new WaitForSeconds(godShieldRechargeTime);
        }
    }

    /// <summary>무한의 마력 — 30초 지속 (투사체 2배 + 공격속도 +50%) → 15초 쿨타임</summary>
    IEnumerator InfiniteManaRoutine()
    {
        while (true)
        {
            // 효과 적용
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddMulti(StatType.AttackSpeed, -infiniteManaSpeedBonus);
                PlayerStats.Instance.AddFlat(StatType.ProjectileCount, infiniteManaProjectiles - 1);
            }

            // 파란 파티클 소환 (플레이어 자식)
            if (infiniteManaPrefab != null && PlayerStats.Instance != null)
            {
                if (infiniteManaInstance != null) Destroy(infiniteManaInstance);
                infiniteManaInstance = Instantiate(infiniteManaPrefab, Vector3.zero,
                                                   Quaternion.identity, PlayerStats.Instance.transform);
                infiniteManaInstance.transform.localPosition = Vector3.zero;
                infiniteManaInstance.transform.localScale    = Vector3.one * infiniteManaScale;
            }
            Debug.Log("[AccessoryEffect] 무한의 마력 — 발동! 투사체 2배 + 공속 +50%");

            yield return new WaitForSeconds(infiniteManaDuration);

            // 효과 해제
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddMulti(StatType.AttackSpeed, infiniteManaSpeedBonus);
                PlayerStats.Instance.AddFlat(StatType.ProjectileCount, -(infiniteManaProjectiles - 1));
            }

            // 파티클 제거
            if (infiniteManaInstance != null)
            {
                Destroy(infiniteManaInstance);
                infiniteManaInstance = null;
            }
            Debug.Log("[AccessoryEffect] 무한의 마력 — 쿨타임 중");

            yield return new WaitForSeconds(infiniteManaCooldown);
        }
    }

    /// <summary>재앙의 씨앗 — 2초 후 폭발, 몹 처치 시 주변 전이</summary>
    IEnumerator CalamitySeedRoutine(Enemy enemy)
    {
        // 씨앗 프리팹 — 적 머리 위에 소환
        GameObject seedFx = null;
        if (seedEffectPrefab != null && enemy != null)
        {
            Vector3 headPos = enemy.transform.position + Vector3.up * seedHeadOffset;
            seedFx = Instantiate(seedEffectPrefab, headPos, Quaternion.identity);
            seedFx.transform.localScale = Vector3.one * seedScale;
        }

        float elapsed = 0f;
        while (elapsed < seedFuseTime)
        {
            elapsed += Time.deltaTime;

            // 씨앗이 적 머리 위 따라다니기
            if (seedFx != null && enemy != null && enemy.IsLive)
                seedFx.transform.position = enemy.transform.position + Vector3.up * seedHeadOffset;

            // 적이 죽으면 씨앗 제거 (전이는 NotifyEnemyKilledWithPos에서 처리)
            if (enemy == null || !enemy.IsLive)
            {
                if (seedFx != null) Destroy(seedFx);
                if (seedRoutines.ContainsKey(enemy)) seedRoutines.Remove(enemy);
                yield break;
            }

            yield return null;
        }

        // 씨앗 제거 후 폭발
        if (seedFx != null) Destroy(seedFx);

        if (enemy != null && enemy.IsLive)
        {
            // 폭발 이펙트
            if (seedExplosionPrefab != null)
            {
                GameObject exFx = Instantiate(seedExplosionPrefab, enemy.transform.position, Quaternion.identity);
                exFx.transform.localScale = Vector3.one * seedScale;
                Destroy(exFx, 1f);
            }

            // 최대체력 5% 피해
            float dmg = enemy.maxHealth * seedDamageRatio;
            enemy.TakeDamage(dmg);
            Debug.Log($"[AccessoryEffect] 재앙의 씨앗 — 폭발! {dmg:F0} 피해");

            // 주변 적에게 씨앗 전이
            SpreadSeed(enemy);
        }

        if (seedRoutines.ContainsKey(enemy)) seedRoutines.Remove(enemy);
    }

    /// <summary>씨앗 주변 전이</summary>
    void SpreadSeed(Enemy origin)
    {
        foreach (Enemy e in FindEnemiesAround(origin.transform.position, seedSpreadRadius))
        {
            if (e == origin) continue;
            if (seedRoutines.ContainsKey(e)) continue;
            seedRoutines[e] = StartCoroutine(CalamitySeedRoutine(e));
        }
        Debug.Log("[AccessoryEffect] 재앙의 씨앗 — 주변 전이!");
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

    /// <summary>현재 어느 악세사리가 발동했는지에 따라 이펙트 프리팹 선택</summary>
    GameObject ResolveArrowEffectPrefab()
    {
        // 마법의 구(ACC_R_019)가 활성화된 경우 MagicOrbEffect 우선 사용
        if (magicOrbEffectPrefab != null)
            return magicOrbEffectPrefab;
        return revengeArrowEffectPrefab;
    }

    IEnumerator ArrowRoutine(Vector3 startPos, Vector2 dir, float damage)
    {
        float elapsed  = 0f;
        float duration = revengeArrowRange / revengeArrowSpeed;
        Vector3 pos    = startPos;
        var hit = new HashSet<Enemy>();

        // 화살/마법구 이펙트 오브젝트 소환 (악세사리 종류에 따라 자동 선택)
        GameObject arrowFx = null;
        GameObject fxPrefab = ResolveArrowEffectPrefab();
        if (fxPrefab != null)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrowFx = Instantiate(fxPrefab, pos, Quaternion.Euler(0, 0, angle));
            arrowFx.transform.localScale = Vector3.one * 0.5f;
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
    //  MinervaWisdom
    // ───────────────────────────────────────────

    void SpawnMinervaStack()
    {
        // Awake에서 못 불렸을 경우 재시도
        if (minervaStackPrefab == null)
            seedEffectPrefab = Resources.Load<GameObject>("Effects/SeedEffect");
        if (seedEffectPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/SeedEffect 프리팹을 찾을 수 없습니다.");
        seedExplosionPrefab = Resources.Load<GameObject>("Effects/SeedExplosionEffect");
        if (seedExplosionPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/SeedExplosionEffect 프리팹을 찾을 수 없습니다.");

        infiniteManaPrefab = Resources.Load<GameObject>("Effects/InfiniteManaEffect");
        if (infiniteManaPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/InfiniteManaEffect 프리팹을 찾을 수 없습니다.");

        godShieldPrefab = Resources.Load<GameObject>("Effects/GodShieldEffect");
        if (godShieldPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/GodShieldEffect 프리팹을 찾을 수 없습니다.");

        hourglassPrefab = Resources.Load<GameObject>("Effects/HourglassEffect");
        if (hourglassPrefab == null)
            Debug.LogWarning("[AccessoryEffect] Effects/HourglassEffect 프리팹을 찾을 수 없습니다.");

        minervaStackPrefab = Resources.Load<GameObject>("Effects/MinervaStackEffect");
        if (minervaSprites == null || minervaSprites.Length == 0)
            minervaSprites = Resources.LoadAll<Sprite>("Effects/10_weaponhit_spritesheet");

        if (minervaStackPrefab == null)
        {
            Debug.LogError("[미네르바] 프리팹 로드 실패!");
            return;
        }
        if (PlayerStats.Instance == null)
        {
            Debug.LogError("[미네르바] PlayerStats.Instance null!");
            return;
        }

        if (minervaStackInstance != null) Destroy(minervaStackInstance);

        // 플레이어 자식으로 소환 → 항상 중심에 붙어있음
        minervaStackInstance = Instantiate(minervaStackPrefab, Vector3.zero,
                                           Quaternion.identity, PlayerStats.Instance.transform);
        Debug.Log($"[미네르바] Instantiate 결과: {(minervaStackInstance != null ? "성공" : "실패")}");

        // Animator 비활성화 — 코드에서 직접 프레임 제어
        foreach (Animator anim in minervaStackInstance.GetComponentsInChildren<Animator>(true))
            anim.enabled = false;
        minervaStackInstance.transform.localPosition = Vector3.zero;
        minervaStackInstance.transform.localScale    = Vector3.one * 10f;

        // 첫 번째 프레임으로 초기화
        minervaCurrentFrame = 0;
        UpdateMinervaSprite();
        Debug.Log($"[AccessoryEffect] 미네르바의 지혜 — 스택 소환! 스프라이트 수: {(minervaSprites != null ? minervaSprites.Length : 0)}");
    }

    /// <summary>악세사리/무기 획득 시 호출 — 다음 프레임으로 진행 + 공격력 +10%</summary>
    public void NotifyItemAcquired()
    {
        if (!Has(AccessoryEffectType.MinervaWisdom)) return;
        if (minervaCurrentFrame >= minervaMaxFrames - 1) return; // 최대 프레임 도달 시 중단

        minervaCurrentFrame++;
        UpdateMinervaSprite();

        // 공격력 +10% 스택
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddMulti(StatType.AttackPower, minervaStackBonus);
            minervaCurrentBonus += minervaStackBonus;
        }
        Debug.Log($"[AccessoryEffect] 미네르바 스택 {minervaCurrentFrame}/{minervaMaxFrames - 1} — 공격력 +{minervaCurrentBonus * 100f:F0}%");
    }

    void UpdateMinervaSprite()
    {
        if (minervaStackInstance == null) { Debug.LogWarning("[미네르바] 인스턴스 없음"); return; }
        if (minervaSprites == null || minervaSprites.Length == 0) { Debug.LogWarning("[미네르바] 스프라이트 배열 없음"); return; }

        int frameIndex = Mathf.Clamp(minervaCurrentFrame, 0, minervaSprites.Length - 1);
        SpriteRenderer sr = minervaStackInstance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = minervaSprites[frameIndex];
            Debug.Log($"[미네르바] 프레임 {frameIndex} 적용 — {minervaSprites[frameIndex]?.name}");
        }
        else Debug.LogWarning("[미네르바] SpriteRenderer 없음");
    }

    // ───────────────────────────────────────────
    //  AbyssLord
    // ───────────────────────────────────────────

    IEnumerator AbyssLordRoutine()
    {
        while (true)
        {
            // 5초 쿨타임 대기
            yield return new WaitForSeconds(abyssCooldown);

            if (PlayerStats.Instance == null) continue;

            // 촉수 4개 소환
            List<Coroutine> tentacleRoutines = new List<Coroutine>();
            for (int i = 0; i < abyssCount; i++)
            {
                // 플레이어 주변 랜덤 위치
                Vector2 randomOffset = Random.insideUnitCircle.normalized * abyssSpawnRadius;
                Vector3 spawnPos = PlayerStats.Instance.transform.position
                                 + new Vector3(randomOffset.x, randomOffset.y, 0f);

                if (tentaclePrefab != null)
                {
                    GameObject tentacle = Instantiate(tentaclePrefab, spawnPos, Quaternion.identity);
                    tentacle.transform.localScale = Vector3.one * 6f;
                    tentacleRoutines.Add(StartCoroutine(TentacleAttackRoutine(tentacle, spawnPos)));
                }
            }
            Debug.Log("[AccessoryEffect] 심연의 군주 — 촉수 4개 소환!");

            // 3초 지속 후 소멸은 TentacleAttackRoutine에서 처리
            yield return new WaitForSeconds(abyssDuration);
        }
    }

    IEnumerator TentacleAttackRoutine(GameObject tentacle, Vector3 pos)
    {
        float elapsed = 0f;

        while (elapsed < abyssDuration && tentacle != null)
        {
            elapsed += abyssTickInterval;
            yield return new WaitForSeconds(abyssTickInterval);

            if (tentacle == null || PlayerStats.Instance == null) break;

            // 촉수 위치 주변 적 공격 (레이어 무관하게 직접 탐색)
            bool hitAny = false;
            Vector3 currentPos = tentacle != null ? tentacle.transform.position : pos;
            Collider2D[] tentacleHits = Physics2D.OverlapCircleAll(currentPos, abyssAttackRadius);
            foreach (Collider2D col in tentacleHits)
            {
                Enemy e = col.GetComponent<Enemy>();
                if (e != null && e.IsLive)
                {
                    e.TakeDamage(abyssDamagePerTick);
                    hitAny = true;
                }
            }


            // 적중 시 흡혈
            if (hitAny)
            {
                float healAmount = PlayerStats.Instance.MaxHP * abyssLifeStealRatio;
                PlayerStats.Instance.Heal(healAmount);
            }
        }

        // 3초 후 소멸
        if (tentacle != null) Destroy(tentacle);
    }

    // ───────────────────────────────────────────
    //  ZeusJudgment
    // ───────────────────────────────────────────

    IEnumerator ZeusJudgmentRoutine(Enemy first)
    {
        // 첫 번째 적에게 낙뢰 이펙트 + 피해 + 감전
        if (zeusLightningPrefab != null)
            StartCoroutine(SpawnEffectRoutine(zeusLightningPrefab,
                first.transform.position, zeusEffectTime, zeusLightningScale));
        first.TakeDamage(zeusDamage);
        first.ApplyFreeze(zeusStunDuration);
        Debug.Log("[AccessoryEffect] 제우스의 심판 — 낙뢰!");

        yield return new WaitForSeconds(0.1f);

        // 연쇄 — 주변 최대 5명에게 전이
        var hit = new HashSet<Enemy> { first };
        Enemy current = first;

        for (int i = 0; i < zeusChainCount; i++)
        {
            Enemy next = null;
            float minDist = float.MaxValue;
            foreach (Enemy e in FindEnemiesAround(current.transform.position, zeusChainRadius))
            {
                if (hit.Contains(e)) continue;
                float dist = Vector3.Distance(current.transform.position, e.transform.position);
                if (dist < minDist) { minDist = dist; next = e; }
            }
            if (next == null) break;

            // 연쇄 이펙트 + 피해 + 감전
            if (zeusChainPrefab != null)
                StartCoroutine(SpawnEffectRoutine(zeusChainPrefab,
                    next.transform.position, zeusEffectTime, zeusChainScale));
            next.TakeDamage(zeusChainDamage);
            next.ApplyFreeze(zeusStunDuration);

            hit.Add(next);
            current = next;
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log($"[AccessoryEffect] 제우스의 심판 — 연쇄 {hit.Count}마리 감전!");
    }

    // ───────────────────────────────────────────
    //  BossArrow
    // ───────────────────────────────────────────

    /// <summary>보스 스폰 시 WaveManager에서 호출</summary>
    public void NotifyBossSpawn(Transform boss)
    {
        if (!Has(AccessoryEffectType.BossArrow)) return;
        bossTarget = boss;

        if (bossArrowInstance != null) Destroy(bossArrowInstance);
        if (bossArrowPrefab != null)
        {
            bossArrowInstance = Instantiate(bossArrowPrefab);

            // 크기 4배
            bossArrowInstance.transform.localScale = Vector3.one * 4f;

            // Animator가 rotation을 덮어쓰지 않도록 전부 비활성화
            foreach (Animator anim in bossArrowInstance.GetComponentsInChildren<Animator>(true))
                anim.enabled = false;
        }
        Debug.Log("[AccessoryEffect] 신기한 화살 — 보스 방향 안내 시작!");
    }

    /// <summary>보스 사망 시 WaveManager에서 호출</summary>
    public void NotifyBossDead()
    {
        if (!Has(AccessoryEffectType.BossArrow)) return;
        bossTarget = null;
        if (bossArrowInstance != null)
        {
            Destroy(bossArrowInstance);
            bossArrowInstance = null;
        }
        Debug.Log("[AccessoryEffect] 신기한 화살 — 보스 처치, 화살표 제거");
    }

    // ───────────────────────────────────────────
    //  SoulBullet
    // ───────────────────────────────────────────
    IEnumerator SoulBulletRoutine()
    {
        while (true)
        {
            // 탄환 소환
            SpawnSoulBullets();

            // 5초 공전 (Update에서 궤도 회전 + 충돌 데미지)
            yield return new WaitForSeconds(soulBulletHomingDelay);

            // 5초 후 적에게 유도 발사
            FireSoulBulletsAtEnemies();

            // 재소환 대기 (적중/시간초과 후 RespawnOneSoulBullet이 처리)
            yield return new WaitForSeconds(soulBulletRespawnTime);
        }
    }

    void FireSoulBulletsAtEnemies()
    {
        if (PlayerStats.Instance == null) return;
        List<Enemy> nearby = FindEnemiesAround(PlayerStats.Instance.transform.position, 20f);
        if (nearby.Count == 0) return;

        for (int i = soulBullets.Count - 1; i >= 0; i--)
        {
            if (soulBullets[i] == null) { soulBullets.RemoveAt(i); continue; }
            GameObject b = soulBullets[i];
            soulBullets.RemoveAt(i);
            Enemy t = i < nearby.Count ? nearby[i] : nearby[0];
            StartCoroutine(HomingBulletMove(b, t));
        }
    }

    void SpawnSoulBullets()
    {
        // 기존 탄환 제거
        foreach (GameObject b in soulBullets)
            if (b != null) Destroy(b);
        soulBullets.Clear();

        if (soulBulletPrefab == null || PlayerStats.Instance == null) return;

        for (int i = 0; i < soulBulletCount; i++)
        {
            GameObject bullet = Instantiate(soulBulletPrefab,
                PlayerStats.Instance.transform.position, Quaternion.identity);
            bullet.transform.localScale = Vector3.one * 10f;
            soulBullets.Add(bullet);
        }
        Debug.Log("[AccessoryEffect] 영혼의 등불 — 탄환 3개 소환!");
    }

    /// <summary>플레이어 이동 방향 기준으로 영혼 탄환 flipX 갱신</summary>
    void UpdateSoulBulletFacing()
    {
        if (PlayerStats.Instance == null) return;
        Player player = PlayerStats.Instance.GetComponent<Player>();
        if (player == null) return;

        bool facingLeft = player.lastTravelDirection.x < 0f;
        foreach (GameObject b in soulBullets)
        {
            if (b == null) continue;
            if (homingBullets.Contains(b)) continue; // 유도 중인 탄환은 rotation으로 제어
            SpriteRenderer sr = b.GetComponent<SpriteRenderer>();
            if (sr != null) sr.flipX = facingLeft;
        }
    }

    IEnumerator HomingSoulBullets()
    {
        if (PlayerStats.Instance == null) yield break;

        // 각 탄환마다 가장 가까운 적을 찾아 유도
        var targets = new List<Enemy>();
        List<Enemy> nearby = FindEnemiesAround(PlayerStats.Instance.transform.position, 20f);

        for (int i = 0; i < soulBullets.Count; i++)
        {
            if (soulBullets[i] == null) continue;

            // 아직 타겟되지 않은 적 중 가장 가까운 적 선택
            Enemy target = null;
            float minDist = float.MaxValue;
            foreach (Enemy e in nearby)
            {
                if (targets.Contains(e)) continue;
                float dist = Vector3.Distance(soulBullets[i].transform.position, e.transform.position);
                if (dist < minDist) { minDist = dist; target = e; }
            }
            if (target != null) targets.Add(target);

            // 유도 이동
            StartCoroutine(HomingBulletMove(soulBullets[i], target));
        }

        // 유도 완료 대기
        yield return new WaitForSeconds(3f);
    }

    IEnumerator HomingBulletMove(GameObject bullet, Enemy target)
    {
        if (bullet == null) yield break;

        // 유도 시작 — flipX 초기화, rotation으로만 방향 제어
        homingBullets.Add(bullet);
        SpriteRenderer sr = bullet.GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = false;

        float elapsed  = 0f;
        float maxTime  = 5f;
        Enemy current  = target;

        while (elapsed < maxTime && bullet != null)
        {
            elapsed += Time.deltaTime;

            // 현재 타겟이 죽었으면 새 타겟 탐색
            if (current == null || !current.IsLive)
            {
                if (PlayerStats.Instance != null)
                {
                    List<Enemy> nearby = FindEnemiesAround(bullet.transform.position, 20f);
                    current = nearby.Count > 0 ? nearby[0] : null;
                }
            }

            // 타겟 있으면 유도, 없으면 계속 공전 (플레이어 주변 유지)
            Vector3 dest;
            if (current != null && current.IsLive)
            {
                dest = current.transform.position;
            }
            else if (PlayerStats.Instance != null)
            {
                // 타겟 없으면 플레이어 주변 궤도로 복귀
                float angle = elapsed * soulBulletOrbitSpeed * Mathf.Deg2Rad;
                dest = PlayerStats.Instance.transform.position +
                       new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * soulBulletOrbitRadius;
            }
            else
            {
                yield return null;
                continue;
            }

            // 날아가는 방향으로 스프라이트 회전
            Vector3 moveDir = (dest - bullet.transform.position).normalized;
            if (moveDir != Vector3.zero)
            {
                float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
                bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            bullet.transform.position = Vector3.MoveTowards(
                bullet.transform.position, dest,
                soulBulletHomingSpeed * Time.deltaTime);

            // 적에게 도달하면 피해 후 제거
            if (current != null && current.IsLive &&
                Vector3.Distance(bullet.transform.position, current.transform.position) < 0.5f)
            {
                current.TakeDamage(soulBulletDamage);
                Debug.Log("[AccessoryEffect] 영혼 탄환 적중!");
                homingBullets.Remove(bullet);
                if (bullet != null) Destroy(bullet);
                if (Has(AccessoryEffectType.SoulBullet) && soulBullets.Count < soulBulletCount)
                    StartCoroutine(RespawnOneSoulBullet());
                yield break;
            }

            yield return null;
        }

        // 시간 초과 시 제거 후 재소환
        Debug.Log("[AccessoryEffect] 영혼 탄환 유도 시간 초과 — 재소환");
        homingBullets.Remove(bullet);
        if (bullet != null) Destroy(bullet);
        yield return new WaitForSeconds(1f);
        if (Has(AccessoryEffectType.SoulBullet) && soulBullets.Count < soulBulletCount)
            StartCoroutine(RespawnOneSoulBullet());
    }

    IEnumerator RespawnOneSoulBullet()
    {
        yield return new WaitForSeconds(2f);
        if (soulBulletPrefab == null || PlayerStats.Instance == null) yield break;
        GameObject b = Instantiate(soulBulletPrefab,
            PlayerStats.Instance.transform.position, Quaternion.identity);
        b.transform.localScale = Vector3.one * 10f;
        soulBullets.Add(b);
        Debug.Log("[AccessoryEffect] 영혼 탄환 재소환!");
    }

    // ───────────────────────────────────────────
    //  ShadowClone
    // ───────────────────────────────────────────
    void SpawnShadowClone()
    {
        if (shadowClonePrefab == null || PlayerStats.Instance == null) return;

        // 기존 분신 제거 후 새로 소환
        if (shadowCloneInstance != null)
            Destroy(shadowCloneInstance);

        Vector3 spawnPos = PlayerStats.Instance.transform.position + Vector3.left * 1f;
        shadowCloneInstance = Instantiate(shadowClonePrefab, spawnPos, Quaternion.identity);
        Debug.Log("[AccessoryEffect] 그림자 가면 — 분신 소환!");
    }

    // ───────────────────────────────────────────
    //  유틸
    // ───────────────────────────────────────────
    List<Enemy> FindEnemiesAround(Vector3 center, float radius)
    {
        var result = new List<Enemy>();
        // Enemy 레이어 마스크 사용 (Physics2D 충돌 설정 무관하게 탐색)
        int enemyLayer = LayerMask.GetMask("Enemy");
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayer);
        foreach (Collider2D hit in hits)
        {
            Enemy e = hit.GetComponent<Enemy>();
            if (e != null) result.Add(e);
        }
        return result;
    }
}