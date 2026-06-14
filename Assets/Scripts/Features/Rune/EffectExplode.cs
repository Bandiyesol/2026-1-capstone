using System.Collections.Generic;
using UnityEngine;

public class EffectExplode : RuneEffect, ITriggerEffect
{
	public bool DestroyOnExecute => data != null && data.isDestroyed;
	public bool ProtectParent => false;


	private void Update() => UpdateCooltime();


	public void OnReflect(Collider2D collision)
	{
		if (!isReady || !TryGetDamageable(collision, out _))
			return;

		float radius = RuneDataAccess.GetExplodeRadius(data);
		if (radius <= 0f)
			return;

		float explodeDamage = DamageCalculator.CalculateBaseDamage(weapon, data);
		Collider2D[] colliders = FindEnemyColliders(collision.transform.position, radius);

		HashSet<IDamageable> targets = new();
		foreach (Collider2D enemyCollider in colliders)
		{
			if (enemyCollider == collision) continue;

			if (TryGetDamageable(enemyCollider, out IDamageable damageable))
				targets.Add(damageable);
		}

		foreach (IDamageable target in targets)
			target.TakeDamage(explodeDamage);

		ResetCooltime();
	}
}
