using System.Collections.Generic;
using UnityEngine;

public class EffectGrowth : RuneEffect, IStateEffect
{
	float maxGrowthTime;
	float maxScaleRatio;
	float maxDamageRatio;
	float baseDamage;
	Vector3 baseScale;
	readonly Dictionary<Collider2D, Vector2> baseColliderSizes = new();

	public override bool isFinished => false;

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		baseScale = transform.localScale;
		baseDamage = weapon.damage;

		maxGrowthTime = RuneDataAccess.GetGrowthDuration(data);
		maxScaleRatio = RuneDataAccess.GetGrowthScaleRatio(data);
		maxDamageRatio = RuneDataAccess.GetGrowthDamageRatio(data);

		baseColliderSizes.Clear();
		foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
		{
			if (col is BoxCollider2D box)
				baseColliderSizes[col] = box.size;
			else if (col is CircleCollider2D circle)
				baseColliderSizes[col] = Vector2.one * circle.radius;
		}
	}

	public void UpdateState()
	{
		float progress = GetGrowthProgress();
		float scaleRatio = Mathf.Lerp(1f, maxScaleRatio, progress);
		float damageRatio = Mathf.Lerp(1f, maxDamageRatio, progress);

		transform.localScale = baseScale * scaleRatio;
		weapon.damage = baseDamage * damageRatio;

		foreach (var pair in baseColliderSizes)
		{
			Collider2D col = pair.Key;
			if (col == null)
				continue;

			if (col is BoxCollider2D box)
				box.size = pair.Value * scaleRatio;
			else if (col is CircleCollider2D circle)
				circle.radius = pair.Value.x * scaleRatio;
		}
	}

	float GetGrowthProgress()
	{
		if (parentMotion == null || weapon == null)
			return 0f;

		float growthDuration = Mathf.Max(0.01f, maxGrowthTime);
		float totalLife = Mathf.Max(0.01f, weapon.spawntime);
		float elapsed = totalLife - parentMotion.GetRemainingLife();
		return Mathf.Clamp01(elapsed / growthDuration);
	}
}
