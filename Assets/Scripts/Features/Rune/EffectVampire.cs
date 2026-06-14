using UnityEngine;

public class EffectVampire : RuneEffect, ITriggerEffect
{
	public bool DestroyOnExecute => data != null && data.isDestroyed;
	public bool ProtectParent => false;


	private void Update() => UpdateCooltime();


	public void OnReflect(Collider2D collision)
	{
		if (!isReady || !TryGetDamageable(collision, out _))
			return;

		float healAmount = DamageCalculator.CalculateBaseDamage(weapon, data);

		PlayerStats stats = DamageCalculator.ResolvePlayerStats();
		if (stats != null)
		{
			float beforeHeal = stats.CurrentHP;
			stats.Heal(healAmount);
#if UNITY_EDITOR
			Debug.Log($"[Vampire] Heal {stats.CurrentHP - beforeHeal:0.##}/{healAmount:0.##}");
#endif
		}

		ResetCooltime();
	}
}
