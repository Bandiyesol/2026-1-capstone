using UnityEngine;
using System;

/// <summary>
/// 플레이어의 모든 스탯을 관리하는 클래스.
/// 기본값(Base) + 아이템/버프에 의한 누적 보너스(Bonus)로 최종 스탯을 계산한다.
/// 무기 시스템(WeaponController, Motion 등)에서 최종값을 참조해 사용한다.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  싱글톤 (GameManager처럼 어디서든 접근 가능)
    // ─────────────────────────────────────────
    public static PlayerStats Instance { get; private set; }

    // 스탯 변경 시 UI 등 외부에 알리는 이벤트
    public event Action OnStatsChanged;

    // Player.cs 참조 — 이동 속도·체력 등을 직접 동기화하기 위해 캐싱
    private Player player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeStats();
    }

    private void Start()
    {
        // Player 컴포넌트 캐싱 (같은 오브젝트 또는 씬에서 탐색)
        player = GetComponent<Player>();
        if (player == null)
            player = FindFirstObjectByType<Player>();

        // 스탯 변경 시 Player.speed 자동 동기화 등록
        OnStatsChanged += SyncToPlayer;

        // 시작 시 한 번 동기화
        SyncToPlayer();
    }

    /// <summary>
    /// PlayerStats의 최종값을 Player.cs의 필드에 반영한다.
    /// 스탯이 바뀔 때마다 OnStatsChanged 이벤트로 자동 호출된다.
    /// </summary>
    private void SyncToPlayer()
    {
        if (player == null) return;

        // 이동 속도 동기화 → Player.cs의 speed 필드를 덮어씀
        player.speed = MovementSpeed;

        // 체력 동기화 → GameManager.instance.Health에 현재 체력 반영
        if (GameManager.instance != null)
            GameManager.instance.Health = currentHP;
    }

    // ─────────────────────────────────────────
    //  내부 스탯 데이터 구조
    // ─────────────────────────────────────────

    /// <summary>
    /// 하나의 스탯을 기본값 + 합산 보너스로 관리하는 구조체.
    /// flat : 고정 수치 증가 (예: 공격력 +5)
    /// multi : 배율 증가 (예: 공격력 +20% → 0.2f 누적)
    /// 최종값 = (base + flat) * (1 + multi)
    /// </summary>
    [Serializable]
    private struct StatValue
    {
        public float baseValue;   // 기본값 (인스펙터에서 초기 설정)
        public float flatBonus;   // 누적 고정 보너스
        public float multiBonus;  // 누적 배율 보너스 (0.2 = +20%)

        public float Final => (baseValue + flatBonus) * (1f + multiBonus);
    }

    // ─────────────────────────────────────────
    //  ① 공격 (Offense)
    // ─────────────────────────────────────────
    [Header("── 공격 (Offense) ──────────────────")]

    [Tooltip("무기 기본 데미지에 곱해지는 배율. 최종 데미지 = 무기 데미지 × 공격력")]
    [SerializeField] private StatValue attackPower     = new StatValue { baseValue = 1.0f };

    [Tooltip("무기 쿨타임에 곱해지는 배율. 최종 쿨타임 = 무기 쿨타임 × 공격 속도 (값이 낮을수록 빠름)")]
    [SerializeField] private StatValue attackSpeed     = new StatValue { baseValue = 1.0f };

    [Tooltip("한 번의 발사에서 나오는 투사체 수 (활, 총, 스태프, 마도서, 오브 계열)")]
    [SerializeField] private StatValue projectileCount = new StatValue { baseValue = 1f };

    [Tooltip("투사체가 날아가는 속도 (활, 총, 스태프, 마도서 계열)")]
    [SerializeField] private StatValue projectileSpeed = new StatValue { baseValue = 10f };

    [Tooltip("투사체 최대 사거리 배율. 최종 사거리 = 무기 리치 × 투사체 사거리")]
    [SerializeField] private StatValue projectileRange = new StatValue { baseValue = 1.0f };

    [Tooltip("근접 무기 공격 범위 배율. 최종 범위 = 무기 기본 범위 × 공격 범위 (검, 망치, 낫, 채찍, 부메랑)")]
    [SerializeField] private StatValue meleeRange      = new StatValue { baseValue = 1.0f };

    [Tooltip("치명타가 발동될 확률 (0~1 사이 값. 1 = 100%)")]
    [SerializeField] private StatValue critChance      = new StatValue { baseValue = 0.05f };

    [Tooltip("치명타 발동 시 데미지 배율 (2.0 = 200% 데미지)")]
    [SerializeField] private StatValue critDamage      = new StatValue { baseValue = 2.0f };

    // ─────────────────────────────────────────
    //  ② 방어 (Defense)
    // ─────────────────────────────────────────
    [Header("── 방어 (Defense) ──────────────────")]

    [Tooltip("받는 피해에서 고정 수치로 차감되는 방어력")]
    [SerializeField] private StatValue defense          = new StatValue { baseValue = 0f };

    [Tooltip("캐릭터 최대 체력")]
    [SerializeField] private StatValue maxHP            = new StatValue { baseValue = 100f };

    [Tooltip("최종 수신 데미지 % 감소 (0~1. 0.3 = 30% 감소)")]
    [SerializeField] private StatValue damageReduction  = new StatValue { baseValue = 0f };

    [Tooltip("적 공격을 완전 무효화할 확률 (0~1)")]
    [SerializeField] private StatValue evasion          = new StatValue { baseValue = 0f };

    [Tooltip("피격 후 발생하는 무적 지속 시간(초)")]
    [SerializeField] private StatValue invincibilityFrames = new StatValue { baseValue = 0.5f };

    [Tooltip("포션/흡혈 등으로 회복되는 체력에 곱해지는 배율")]
    [SerializeField] private StatValue healingBonus     = new StatValue { baseValue = 1.0f };

    // ─────────────────────────────────────────
    //  ③ 유틸리티 (Utility)
    // ─────────────────────────────────────────
    [Header("── 유틸리티 (Utility) ───────────────")]

    [Tooltip("캐릭터 이동 속도")]
    [SerializeField] private StatValue movementSpeed    = new StatValue { baseValue = 5f };

    [Tooltip("골드·아이템 자동 흡수 반경")]
    [SerializeField] private StatValue magnetRange      = new StatValue { baseValue = 3f };

    [Tooltip("쿨다운 감소 배율. 최종 쿨타임 = 기본 쿨타임 × (1 - 쿨다운 감소)")]
    [SerializeField] private StatValue cooldownReduction = new StatValue { baseValue = 0f };

    [Tooltip("안개 제거 시야 반경")]
    [SerializeField] private StatValue visionRange      = new StatValue { baseValue = 10f };

    // ─────────────────────────────────────────
    //  ④ 속성 (Elemental) — 활성화 여부 + 세기
    // ─────────────────────────────────────────
    [Header("── 속성 (Elemental) ──────────────────")]

    [Tooltip("화염 속성 활성화 여부 (지속 피해)")]
    public bool fireEnabled;
    [Tooltip("화염 데미지 세기 배율")]
    [SerializeField] private StatValue firePower        = new StatValue { baseValue = 1.0f };

    [Tooltip("독 속성 활성화 여부 (약화)")]
    public bool poisonEnabled;
    [Tooltip("독 데미지 세기 배율")]
    [SerializeField] private StatValue poisonPower      = new StatValue { baseValue = 1.0f };

    [Tooltip("빙결 속성 활성화 여부 (둔화)")]
    public bool freezeEnabled;
    [Tooltip("빙결 둔화 강도 배율")]
    [SerializeField] private StatValue freezePower      = new StatValue { baseValue = 1.0f };

    [Tooltip("물 속성 활성화 여부 (침식)")]
    public bool waterEnabled;
    [Tooltip("물 침식 강도 배율")]
    [SerializeField] private StatValue waterPower       = new StatValue { baseValue = 1.0f };

    [Tooltip("번개 속성 활성화 여부 (연쇄)")]
    public bool lightningEnabled;
    [Tooltip("번개 연쇄 강도 배율")]
    [SerializeField] private StatValue lightningPower   = new StatValue { baseValue = 1.0f };

    // ─────────────────────────────────────────
    //  현재 체력 (런타임)
    // ─────────────────────────────────────────
    [Header("── 런타임 상태 ───────────────────────")]
    [SerializeField, Tooltip("현재 체력 (읽기 전용 확인용)")]
    private float currentHP;

    // ═════════════════════════════════════════
    //  초기화
    // ═════════════════════════════════════════

    private void InitializeStats()
    {
        currentHP = maxHP.Final;
    }

    /// <summary>메인 메뉴 복귀 시 체력 등 런타임 스탯을 기본값으로 되돌립니다.</summary>
    public void ResetRuntimeState()
    {
        InitializeStats();
        OnStatsChanged?.Invoke();
    }

    // ═════════════════════════════════════════
    //  공격 스탯 — 외부 읽기 프로퍼티
    // ═════════════════════════════════════════

    /// <summary>무기 기본 데미지에 곱할 최종 공격력 배율</summary>
    public float AttackPower      => attackPower.Final;

    /// <summary>무기 쿨타임에 곱할 공격 속도 배율</summary>
    public float AttackSpeed      => attackSpeed.Final;

    /// <summary>한 번에 발사되는 투사체 수 (정수로 변환해서 사용)</summary>
    public int   ProjectileCount  => Mathf.Max(1, Mathf.RoundToInt(projectileCount.Final));

    /// <summary>투사체 이동 속도</summary>
    public float ProjectileSpeed  => projectileSpeed.Final;

    /// <summary>투사체 사거리 배율 (무기 리치와 곱함)</summary>
    public float ProjectileRange  => projectileRange.Final;

    /// <summary>근접 무기 공격 범위 배율 (무기 기본 범위와 곱함)</summary>
    public float MeleeRange       => meleeRange.Final;

    /// <summary>치명타 확률 (0~1)</summary>
    public float CritChance       => Mathf.Clamp01(critChance.Final);

    /// <summary>치명타 데미지 배율</summary>
    public float CritDamage       => critDamage.Final;

    // ═════════════════════════════════════════
    //  방어 스탯 — 외부 읽기 프로퍼티
    // ═════════════════════════════════════════

    /// <summary>고정 수치 방어력</summary>
    public float Defense           => defense.Final;

    /// <summary>최대 체력</summary>
    public float MaxHP             => maxHP.Final;

    /// <summary>현재 체력</summary>
    public float CurrentHP         => currentHP;

    /// <summary>피해 감소율 (0~1)</summary>
    public float DamageReduction   => Mathf.Clamp01(damageReduction.Final);

    /// <summary>회피율 (0~1)</summary>
    public float Evasion           => Mathf.Clamp01(evasion.Final);

    /// <summary>피격 무적 시간(초)</summary>
    public float InvincibilityFrames => invincibilityFrames.Final;

    /// <summary>회복량 배율</summary>
    public float HealingBonus      => healingBonus.Final;

    // ═════════════════════════════════════════
    //  유틸리티 스탯 — 외부 읽기 프로퍼티
    // ═════════════════════════════════════════

    /// <summary>이동 속도</summary>
    public float MovementSpeed     => movementSpeed.Final;

    /// <summary>아이템 자동 흡수 반경</summary>
    public float MagnetRange       => magnetRange.Final;

    /// <summary>쿨다운 감소율 (0~1)</summary>
    public float CooldownReduction => Mathf.Clamp01(cooldownReduction.Final);

    /// <summary>시야 범위</summary>
    public float VisionRange       => visionRange.Final;

    // ═════════════════════════════════════════
    //  속성 스탯 — 외부 읽기 프로퍼티
    // ═════════════════════════════════════════

    public float FirePower      => firePower.Final;
    public float PoisonPower    => poisonPower.Final;
    public float FreezePower    => freezePower.Final;
    public float WaterPower     => waterPower.Final;
    public float LightningPower => lightningPower.Final;

    // ═════════════════════════════════════════
    //  보너스 추가/제거 (아이템, 버프, 레벨업 등에서 호출)
    // ═════════════════════════════════════════

    /// <summary>
    /// 특정 스탯에 고정 보너스를 더하거나 뺀다.
    /// 예) 공격력 +5 아이템 장착: AddFlat(StatType.AttackPower, 5f)
    /// </summary>
    public void AddFlat(StatType stat, float value)
    {
        ModifyStat(stat, flat: value, multi: 0f);
        NotifyChange();
    }

    /// <summary>
    /// 특정 스탯에 배율 보너스를 더하거나 뺀다.
    /// 예) 공격력 +20% 아이템: AddMulti(StatType.AttackPower, 0.2f)
    /// </summary>
    public void AddMulti(StatType stat, float value)
    {
        ModifyStat(stat, flat: 0f, multi: value);
        NotifyChange();
    }

    // ═════════════════════════════════════════
    //  전투 유틸
    // ═════════════════════════════════════════

    /// <summary>
    /// 최종 수신 데미지 계산.
    /// 회피 → 방어력 차감 → 피해 감소율 순으로 적용.
    /// </summary>
    public float CalculateReceivedDamage(float rawDamage)
    {
        // 회피 판정
        if (UnityEngine.Random.value < Evasion) return 0f;

        // 방어력(고정 차감) 적용
        float afterDefense = Mathf.Max(0f, rawDamage - Defense);

        // 피해 감소율(%) 적용
        float finalDamage = afterDefense * (1f - DamageReduction);

        return Mathf.Max(0f, finalDamage);
    }

    /// <summary>
    /// 치명타 여부를 판정하고 최종 데미지를 반환한다.
    /// WeaponController/Motion에서 데미지 계산 시 사용.
    /// </summary>
    public float CalculateAttackDamage(float weaponBaseDamage, out bool isCrit)
    {
        isCrit = UnityEngine.Random.value < CritChance;
        float damage = weaponBaseDamage * AttackPower;
        if (isCrit) damage *= CritDamage;
        return damage;
    }

    /// <summary>
    /// 회복량을 적용하고 최대 체력을 초과하지 않도록 클램프.
    /// HealingBonus 배율이 자동 적용된다.
    /// </summary>
    public void Heal(float amount)
    {
        currentHP = Mathf.Min(currentHP + amount * HealingBonus, MaxHP);
        // GameManager.Health에도 반영
        if (GameManager.instance != null)
            GameManager.instance.Health = currentHP;
        NotifyChange();
    }

    /// <summary>
    /// 피해를 받는다. CalculateReceivedDamage를 통해 최종 수치 결정.
    /// Player.cs의 OnCollisionStay2D에서 넘어온 rawDamage를 그대로 전달하면 된다.
    /// </summary>
    public void TakeDamage(float rawDamage)
    {
        float finalDamage = CalculateReceivedDamage(rawDamage);
        currentHP = Mathf.Max(0f, currentHP - finalDamage);

        // GameManager.Health에도 반영 → Player.cs의 사망 조건 체크가 그대로 동작함
        if (GameManager.instance != null)
            GameManager.instance.Health = currentHP;

        NotifyChange();

        if (currentHP <= 0f)
        {
            OnDeath();
        }
    }

    private void OnDeath()
    {
        // GameManager나 Player.cs에서 사망 처리를 위임받아 사용
        Debug.Log("[PlayerStats] 플레이어 사망");
        // GameManager는 소문자 instance 싱글톤 사용
        GameManager.instance?.GameOver();
    }

    // ═════════════════════════════════════════
    //  최대 체력 변경 시 현재 체력 동기화
    // ═════════════════════════════════════════

    /// <summary>
    /// 최대 체력이 바뀌었을 때 현재 체력도 같은 비율로 보정.
    /// 아이템 등으로 MaxHP가 변하면 이 메서드를 호출한다.
    /// </summary>
    private void SyncCurrentHPOnMaxHPChange(float prevMaxHP)
    {
        float ratio = prevMaxHP > 0f ? currentHP / prevMaxHP : 1f;
        currentHP = Mathf.Clamp(ratio * MaxHP, 0f, MaxHP);
    }

    // ═════════════════════════════════════════
    //  내부 스탯 수정 (switch로 분기)
    // ═════════════════════════════════════════

    private void ModifyStat(StatType stat, float flat, float multi)
    {
        float prevMaxHP = maxHP.Final;

        switch (stat)
        {
            // 공격
            case StatType.AttackPower:      attackPower.flatBonus     += flat; attackPower.multiBonus     += multi; break;
            case StatType.AttackSpeed:      attackSpeed.flatBonus     += flat; attackSpeed.multiBonus     += multi; break;
            case StatType.ProjectileCount:  projectileCount.flatBonus += flat; projectileCount.multiBonus += multi; break;
            case StatType.ProjectileSpeed:  projectileSpeed.flatBonus += flat; projectileSpeed.multiBonus += multi; break;
            case StatType.ProjectileRange:  projectileRange.flatBonus += flat; projectileRange.multiBonus += multi; break;
            case StatType.CritChance:       critChance.flatBonus      += flat; critChance.multiBonus      += multi; break;
            case StatType.CritDamage:       critDamage.flatBonus      += flat; critDamage.multiBonus      += multi; break;
            case StatType.MeleeRange:       meleeRange.flatBonus      += flat; meleeRange.multiBonus      += multi; break;

            // 방어
            case StatType.Defense:              defense.flatBonus              += flat; defense.multiBonus              += multi; break;
            case StatType.MaxHP:                maxHP.flatBonus                += flat; maxHP.multiBonus                += multi; SyncCurrentHPOnMaxHPChange(prevMaxHP); break;
            case StatType.DamageReduction:      damageReduction.flatBonus      += flat; damageReduction.multiBonus      += multi; break;
            case StatType.Evasion:              evasion.flatBonus              += flat; evasion.multiBonus              += multi; break;
            case StatType.InvincibilityFrames:  invincibilityFrames.flatBonus  += flat; invincibilityFrames.multiBonus  += multi; break;
            case StatType.HealingBonus:         healingBonus.flatBonus         += flat; healingBonus.multiBonus         += multi; break;

            // 유틸
            case StatType.MovementSpeed:      movementSpeed.flatBonus      += flat; movementSpeed.multiBonus      += multi; break;
            case StatType.MagnetRange:        magnetRange.flatBonus        += flat; magnetRange.multiBonus        += multi; break;
            case StatType.CooldownReduction:  cooldownReduction.flatBonus  += flat; cooldownReduction.multiBonus  += multi; break;
            case StatType.VisionRange:        visionRange.flatBonus        += flat; visionRange.multiBonus        += multi; break;

            // 속성
            case StatType.FirePower:       firePower.flatBonus       += flat; firePower.multiBonus       += multi; break;
            case StatType.PoisonPower:     poisonPower.flatBonus     += flat; poisonPower.multiBonus     += multi; break;
            case StatType.FreezePower:     freezePower.flatBonus     += flat; freezePower.multiBonus     += multi; break;
            case StatType.WaterPower:      waterPower.flatBonus      += flat; waterPower.multiBonus      += multi; break;
            case StatType.LightningPower:  lightningPower.flatBonus  += flat; lightningPower.multiBonus  += multi; break;

            default:
                Debug.LogWarning($"[PlayerStats] 알 수 없는 StatType: {stat}");
                break;
        }
    }

    private void NotifyChange() => OnStatsChanged?.Invoke();

    // ═════════════════════════════════════════
    //  디버그 (에디터 전용)
    // ═════════════════════════════════════════

#if UNITY_EDITOR
    [ContextMenu("현재 스탯 출력")]
    private void PrintStats()
    {
        Debug.Log("─── PlayerStats 현재 최종값 ───");
        Debug.Log($"[공격] 공격력={AttackPower:F2}  공격속도={AttackSpeed:F2}  투사체수={ProjectileCount}  투사체속도={ProjectileSpeed:F2}  사거리={ProjectileRange:F2}  근접범위={MeleeRange:F2}  크리확률={CritChance:P0}  크리배율={CritDamage:F2}");
        Debug.Log($"[방어] 방어력={Defense:F2}  최대HP={MaxHP:F0}  현재HP={currentHP:F0}  피해감소={DamageReduction:P0}  회피={Evasion:P0}  무적={InvincibilityFrames:F2}s  힐배율={HealingBonus:F2}");
        Debug.Log($"[유틸] 이동속도={MovementSpeed:F2}  자석범위={MagnetRange:F2}  쿨감={CooldownReduction:P0}  시야={VisionRange:F2}");
        Debug.Log($"[속성] 화염={fireEnabled}({FirePower:F2})  독={poisonEnabled}({PoisonPower:F2})  빙결={freezeEnabled}({FreezePower:F2})  물={waterEnabled}({WaterPower:F2})  번개={lightningEnabled}({LightningPower:F2})");
    }
#endif
}

// ─────────────────────────────────────────────────────────────────────────────
//  StatType 열거형 — 아이템/버프 시스템에서 어떤 스탯을 건드릴지 지정할 때 사용
// ─────────────────────────────────────────────────────────────────────────────
public enum StatType
{
    // 공격
    AttackPower,
    AttackSpeed,
    ProjectileCount,
    ProjectileSpeed,
    ProjectileRange,
    CritChance,
    CritDamage,
    MeleeRange,

    // 방어
    Defense,
    MaxHP,
    DamageReduction,
    Evasion,
    InvincibilityFrames,
    HealingBonus,

    // 유틸
    MovementSpeed,
    MagnetRange,
    CooldownReduction,
    VisionRange,

    // 속성
    FirePower,
    PoisonPower,
    FreezePower,
    WaterPower,
    LightningPower,
}
