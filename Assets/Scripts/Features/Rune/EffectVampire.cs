using UnityEngine;

/// <summary>
/// 적 충돌 시 쿨타임마다 피해 비율만큼 체력 회복 (흡혈).
/// </summary>
public class EffectVampire : RuneEffect, ITriggerEffect
{
    public bool DestroyOnExecute => data != null && data.isDestroyed;
    public bool ProtectParent => false;

    void Update() => UpdateCooltime();

    public void OnReflect(Collider2D collision)
    {
        if (!isReady || collision.GetComponent<IDamageable>() == null)
            return;

        float ratio = data != null && data.power > 0f ? data.power : 0.1f;
        float healAmount = weapon.damage * ratio;

        PlayerStats stats = DamageCalculator.ResolvePlayerStats();
        if (stats != null)
            stats.Heal(healAmount);
        else if (GameManager.instance != null)
        {
            GameManager.instance.Health = Mathf.Min(
                GameManager.instance.Health + healAmount,
                GameManager.instance.maxHealth);
        }

        ResetCooltime();
    }
}
