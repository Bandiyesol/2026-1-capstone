using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 물약 효과 실행 클래스.
/// 상점에서 구매 시 pendingTypes에 등록 (즉시 적용 안 함).
/// 포탈 통과 → NextStage() → OnStageChanged()에서 현재 버프 해제 후 대기 목록 적용.
/// </summary>
public class PotionEffect : MonoBehaviour
{
    public static PotionEffect instance;

    // ── 버프 수치 ────────────────────────────────
    const float AttackPowerBonus      = 0.5f;  // +50%
    const float AttackSpeedBonus      = 0.2f;  // +20%
    const float DamageReductionBonus  = 0.5f;  // +50%
    const float MoveSpeedBonus        = 0.4f;  // +40%
    const float EvasionBonus          = 0.2f;  // +20%
    const float RuneCooldownMultiplier = 0.5f; // 쿨타임 절반

    // ── 현재 적용 중인 버프 ──────────────────────
    bool isAttackBuffActive;
    bool isDefenseBuffActive;
    bool isSpeedBuffActive;
    bool isRuneBuffActive;

    // ── 구매 대기 목록 (포탈 통과 시 적용) ───────
    readonly HashSet<PotionType> pendingTypes = new HashSet<PotionType>();

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    // ───────────────────────────────────────────
    //  외부 진입점 — 상점에서 구매 확정 시 호출
    //  즉시 적용하지 않고 대기 목록에 등록
    // ───────────────────────────────────────────

    public void Use(PotionData data)
    {
        if (data == null) return;
        Use(data.potionType);
    }

    public void Use(PotionType type)
    {
        // [악세사리 훅] 신비한 약병
        AccessoryEffect.instance?.NotifyPotionUsed();

        if (type == PotionType.HealthRestore)
        {
            // 체력 회복만 즉시 적용
            UseHealthRestore();
            return;
        }

        // 나머지 버프는 대기 목록에 등록
        pendingTypes.Add(type);
        Debug.Log($"[PotionEffect] {type} 구매 완료 — 다음 스테이지 진입 시 적용");
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
    //  포탈 통과 시 — StageManager.NextStage()에서 호출
    //  1. 현재 적용 중인 버프 해제
    //  2. 대기 목록의 버프 적용
    // ───────────────────────────────────────────
    public void OnStageChanged()
    {
        // 1단계: 현재 버프 해제
        RemoveActiveBuffs();

        // 2단계: 대기 목록 적용
        foreach (PotionType type in pendingTypes)
            ApplyBuff(type);

        pendingTypes.Clear();
    }

    void RemoveActiveBuffs()
    {
        if (PlayerStats.Instance != null)
        {
            if (isAttackBuffActive)
            {
                PlayerStats.Instance.AddMulti(StatType.AttackPower, -AttackPowerBonus);
                PlayerStats.Instance.AddMulti(StatType.AttackSpeed, -AttackSpeedBonus);
                isAttackBuffActive = false;
                Debug.Log("[PotionEffect] 공격 버프 해제");
            }

            if (isDefenseBuffActive)
            {
                PlayerStats.Instance.AddFlat(StatType.DamageReduction, -DamageReductionBonus);
                isDefenseBuffActive = false;
                Debug.Log("[PotionEffect] 방어 버프 해제");
            }

            if (isSpeedBuffActive)
            {
                PlayerStats.Instance.AddMulti(StatType.MovementSpeed, -MoveSpeedBonus);
                PlayerStats.Instance.AddMulti(StatType.Evasion, -EvasionBonus);
                isSpeedBuffActive = false;
                Debug.Log("[PotionEffect] 속도 버프 해제");
            }
        }

        if (isRuneBuffActive && RuneManager.instance != null)
        {
            RuneManager.instance.CooldownMultiplier = 1f;
            isRuneBuffActive = false;
            Debug.Log("[PotionEffect] 룬 버프 해제");
        }
    }

    void ApplyBuff(PotionType type)
    {
        if (PlayerStats.Instance == null) return;

        switch (type)
        {
            case PotionType.AttackBuff:
                PlayerStats.Instance.AddMulti(StatType.AttackPower, AttackPowerBonus);
                PlayerStats.Instance.AddMulti(StatType.AttackSpeed, AttackSpeedBonus);
                isAttackBuffActive = true;
                Debug.Log("[PotionEffect] 공격 버프 적용");
                break;

            case PotionType.DefenseBuff:
                PlayerStats.Instance.AddFlat(StatType.DamageReduction, DamageReductionBonus);
                isDefenseBuffActive = true;
                Debug.Log("[PotionEffect] 방어 버프 적용");
                break;

            case PotionType.SpeedBuff:
                PlayerStats.Instance.AddMulti(StatType.MovementSpeed, MoveSpeedBonus);
                PlayerStats.Instance.AddMulti(StatType.Evasion, EvasionBonus);
                isSpeedBuffActive = true;
                Debug.Log("[PotionEffect] 속도 버프 적용");
                break;

            case PotionType.RuneBuff:
                if (RuneManager.instance != null)
                    RuneManager.instance.CooldownMultiplier = RuneCooldownMultiplier;
                isRuneBuffActive = true;
                Debug.Log("[PotionEffect] 룬 버프 적용");
                break;
        }
    }

    // ───────────────────────────────────────────
    //  게임 오버/재시작 시 완전 초기화
    // ───────────────────────────────────────────
    public void ClearAllBuffs()
    {
        RemoveActiveBuffs();
        pendingTypes.Clear();
    }

    /// <summary>현재 특정 버프가 적용 중인지 확인.</summary>
    public bool IsBuffActive(PotionType type) => type switch
    {
        PotionType.AttackBuff  => isAttackBuffActive,
        PotionType.DefenseBuff => isDefenseBuffActive,
        PotionType.SpeedBuff   => isSpeedBuffActive,
        PotionType.RuneBuff    => isRuneBuffActive,
        _                      => false
    };

    /// <summary>특정 물약이 구매 대기 중인지 확인 (UI 표시용).</summary>
    public bool IsPending(PotionType type) => pendingTypes.Contains(type);
}