using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 물약 효과를 실제로 실행하는 클래스.
/// 상점에서 구매 확정 시 PotionEffect.instance.Use(data)를 호출하면 된다.
/// 버프 물약은 코루틴으로 시간이 지나면 자동 해제.
/// </summary>
public class PotionEffect : MonoBehaviour
{
    public static PotionEffect instance;

    // ── 공격 버프 수치 ──────────────────────────
    const float AttackPowerBonus   = 0.5f;  // +50%
    const float AttackSpeedBonus   = 0.2f;  // +20%
    const float AttackBuffDuration = 10f;

    // ── 방어 버프 수치 ──────────────────────────
    const float DamageReductionBonus  = 0.5f;  // +50%
    const float DefenseBuffDuration   = 10f;

    // ── 속도 버프 수치 ──────────────────────────
    const float MoveSpeedBonus     = 0.4f;  // +40%
    const float EvasionBonus       = 0.2f;  // +20%
    const float SpeedBuffDuration  = 15f;

    // ── 특수 버프 수치 ──────────────────────────
    const float RuneCooldownMultiplier = 0.5f;  // 쿨타임 절반
    const float RuneBuffDuration       = 10f;

    // 중복 방지 플래그
    bool isAttackBuffActive;
    bool isDefenseBuffActive;
    bool isSpeedBuffActive;
    bool isRuneBuffActive;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    // ───────────────────────────────────────────
    //  테스트용 키 입력 — 상점 구현 후 삭제
    //  1: 체력 회복 / 2: 공격 버프 / 3: 방어 버프
    //  4: 속도 버프 / 5: 룬 버프
    // ───────────────────────────────────────────
    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame) Use(PotionType.HealthRestore);
        if (kb.digit2Key.wasPressedThisFrame) Use(PotionType.AttackBuff);
        if (kb.digit3Key.wasPressedThisFrame) Use(PotionType.DefenseBuff);
        if (kb.digit4Key.wasPressedThisFrame) Use(PotionType.SpeedBuff);
        if (kb.digit5Key.wasPressedThisFrame) Use(PotionType.RuneBuff);
    }

    // ───────────────────────────────────────────
    //  외부 진입점 — 상점에서 이 메서드만 호출하면 됨
    // ───────────────────────────────────────────

    /// <summary>PotionData를 받아서 효과 실행. 상점 구매 확정 시 호출.</summary>
    public void Use(PotionData data)
    {
        if (data == null) return;
        Use(data.potionType);
    }

    /// <summary>PotionType만으로 효과 실행.</summary>
    public void Use(PotionType type)
    {
        switch (type)
        {
            case PotionType.HealthRestore: UseHealthRestore();  break;
            case PotionType.AttackBuff:    StartAttackBuff();   break;
            case PotionType.DefenseBuff:   StartDefenseBuff();  break;
            case PotionType.SpeedBuff:     StartSpeedBuff();    break;
            case PotionType.RuneBuff:      StartRuneBuff();     break;
            default:
                Debug.LogWarning($"[PotionEffect] 알 수 없는 PotionType: {type}");
                break;
        }
    }

    // ───────────────────────────────────────────
    //  체력 회복 (즉시)
    // ───────────────────────────────────────────
    void UseHealthRestore()
    {
        if (PlayerStats.Instance == null) return;
        float healAmount = PlayerStats.Instance.MaxHP * 0.3f;
        PlayerStats.Instance.Heal(healAmount);
        Debug.Log($"[PotionEffect] 체력 회복 +{healAmount:F0}");
    }

    // ───────────────────────────────────────────
    //  공격 버프 (10초)
    // ───────────────────────────────────────────
    void StartAttackBuff()
    {
        if (isAttackBuffActive) StopCoroutine(nameof(AttackBuffRoutine));
        StartCoroutine(nameof(AttackBuffRoutine));
    }

    IEnumerator AttackBuffRoutine()
    {
        isAttackBuffActive = true;
        if (PlayerStats.Instance == null) { isAttackBuffActive = false; yield break; }

        PlayerStats.Instance.AddMulti(StatType.AttackPower, AttackPowerBonus);
        PlayerStats.Instance.AddMulti(StatType.AttackSpeed, AttackSpeedBonus);
        Debug.Log($"[PotionEffect] 공격 버프 시작 ({AttackBuffDuration}초)");

        yield return new WaitForSeconds(AttackBuffDuration);

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddMulti(StatType.AttackPower, -AttackPowerBonus);
            PlayerStats.Instance.AddMulti(StatType.AttackSpeed, -AttackSpeedBonus);
        }
        Debug.Log("[PotionEffect] 공격 버프 종료");
        isAttackBuffActive = false;
    }

    // ───────────────────────────────────────────
    //  방어 버프 (10초)
    // ───────────────────────────────────────────
    void StartDefenseBuff()
    {
        if (isDefenseBuffActive) StopCoroutine(nameof(DefenseBuffRoutine));
        StartCoroutine(nameof(DefenseBuffRoutine));
    }

    IEnumerator DefenseBuffRoutine()
    {
        isDefenseBuffActive = true;
        if (PlayerStats.Instance == null) { isDefenseBuffActive = false; yield break; }

        PlayerStats.Instance.AddFlat(StatType.DamageReduction, DamageReductionBonus);
        Debug.Log($"[PotionEffect] 방어 버프 시작 ({DefenseBuffDuration}초)");

        yield return new WaitForSeconds(DefenseBuffDuration);

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.AddFlat(StatType.DamageReduction, -DamageReductionBonus);
        Debug.Log("[PotionEffect] 방어 버프 종료");
        isDefenseBuffActive = false;
    }

    // ───────────────────────────────────────────
    //  속도 버프 (15초)
    // ───────────────────────────────────────────
    void StartSpeedBuff()
    {
        if (isSpeedBuffActive) StopCoroutine(nameof(SpeedBuffRoutine));
        StartCoroutine(nameof(SpeedBuffRoutine));
    }

    IEnumerator SpeedBuffRoutine()
    {
        isSpeedBuffActive = true;
        if (PlayerStats.Instance == null) { isSpeedBuffActive = false; yield break; }

        PlayerStats.Instance.AddMulti(StatType.MovementSpeed, MoveSpeedBonus);
        PlayerStats.Instance.AddMulti(StatType.Evasion, EvasionBonus);
        Debug.Log($"[PotionEffect] 속도 버프 시작 ({SpeedBuffDuration}초)");

        yield return new WaitForSeconds(SpeedBuffDuration);

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddMulti(StatType.MovementSpeed, -MoveSpeedBonus);
            PlayerStats.Instance.AddMulti(StatType.Evasion, -EvasionBonus);
        }
        Debug.Log("[PotionEffect] 속도 버프 종료");
        isSpeedBuffActive = false;
    }

    // ───────────────────────────────────────────
    //  특수 버프 — 룬 쿨타임 절반 (10초)
    // ───────────────────────────────────────────
    void StartRuneBuff()
    {
        if (isRuneBuffActive) StopCoroutine(nameof(RuneBuffRoutine));
        StartCoroutine(nameof(RuneBuffRoutine));
    }

    IEnumerator RuneBuffRoutine()
    {
        isRuneBuffActive = true;

        if (RuneManager.instance != null)
            RuneManager.instance.CooldownMultiplier = RuneCooldownMultiplier;

        Debug.Log($"[PotionEffect] 룬 버프 시작 — 쿨타임 x{RuneCooldownMultiplier} ({RuneBuffDuration}초)");

        yield return new WaitForSeconds(RuneBuffDuration);

        if (RuneManager.instance != null)
            RuneManager.instance.CooldownMultiplier = 1f;

        Debug.Log("[PotionEffect] 룬 버프 종료");
        isRuneBuffActive = false;
    }
}