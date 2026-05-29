using System.Collections.Generic;
using UnityEngine;

public class EffectChain : RuneEffect, ITriggerEffect
{
	public bool DestroyOnExecute => data != null && data.isDestroyed;
	public bool ProtectParent => false;


	private void Update() => UpdateCooltime();


	public void OnReflect(Collider2D collision)
	{
		if (!isReady || collision.GetComponent<IDamageable>() == null)
			return;

		int chainCount = RuneDataAccess.GetChainCount(data);
		float radius = RuneDataAccess.GetChainRadius(data);
		if (chainCount <= 0 || radius <= 0f)
			return;

		float chainDamage = DamageCalculator.CalculateBaseDamage(weapon, data);
		Collider2D[] colliders = Physics2D.OverlapCircleAll(
			collision.transform.position,
			radius,
			LayerMask.GetMask("Enemy")
		);
		Vector3 hitPosition = collision.transform.position;

		System.Array.Sort(colliders, (a, b) =>
		{
			float distA = (a.transform.position - hitPosition).sqrMagnitude;
			float distB = (b.transform.position - hitPosition).sqrMagnitude;
			return distA.CompareTo(distB);
		});

		List<IDamageable> targets = new();
		foreach (Collider2D enemyCollider in colliders)
		{
			if (enemyCollider == collision) continue;

			IDamageable damageable = enemyCollider.GetComponent<IDamageable>();
			if (damageable == null || targets.Contains(damageable)) continue;

			targets.Add(damageable);
			if (targets.Count >= chainCount) break;
		}

		foreach (IDamageable target in targets)
			target.TakeDamage(chainDamage);

		ResetCooltime();
	}
}
