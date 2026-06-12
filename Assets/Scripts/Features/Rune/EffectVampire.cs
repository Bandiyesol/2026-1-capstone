using UnityEngine;

public class EffectVampire : RuneEffect, ITriggerEffect
{
	public bool DestroyOnExecute => data != null && data.isDestroyed;
	public bool ProtectParent => false;


	private void Update() => UpdateCooltime();


	public void OnReflect(Collider2D collision)
	{
		if (!isReady || collision.GetComponent<IDamageable>() == null)
			return;

		float healAmount = DamageCalculator.CalculateBaseDamage(weapon, data);

		PlayerStats stats = DamageCalculator.ResolvePlayerStats();
		if (stats != null)
			stats.Heal(healAmount);

		ResetCooltime();
	}
}
