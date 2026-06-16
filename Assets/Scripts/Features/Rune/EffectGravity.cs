using UnityEngine;

public class EffectGravity : RuneEffect, IStateEffect
{
	float elapsedtime;
	float duration;
	float pullForce;
	float pullRadius;

	public override bool isFinished => elapsedtime >= duration;

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		elapsedtime = 0f;
		duration = RuneDataAccess.GetDuration(data);
		pullForce = RuneDataAccess.GetPullForce(data) * 2f;

		float baseRadius = RuneDataAccess.GetGravityRadius(data);
		// 모션 프리팹 scale(weapon.size)에 비례해 중력 범위 확장
		pullRadius = baseRadius * Mathf.Max(1f, weapon.size);
	}

	public void UpdateState()
	{
		if (!ShouldRunEffect() || isFinished)
			return;

		elapsedtime += Time.deltaTime;
	}

	void FixedUpdate()
	{
		if (weapon.isSplitChild || !ShouldRunEffect() || isFinished || parentMotion == null || parentMotion.instance == null)
			return;

		if (pullForce <= 0f || pullRadius <= 0f || duration <= 0f)
			return;

		ApplyPullAt(transform.position);
	}

	void ApplyPullAt(Vector2 center)
	{
		Collider2D[] enemies = FindEnemyColliders(center, pullRadius);

		foreach (Collider2D enemyCollider in enemies)
		{
			if (!TryGetDamageable(enemyCollider, out _))
				continue;

			Enemy enemy = enemyCollider.GetComponent<Enemy>()
				?? enemyCollider.GetComponentInParent<Enemy>();

			if (enemy == null)
				continue;

			Vector2 currentPosition = enemy.transform.position;
			float distance = Vector2.Distance(currentPosition, center);
			if (distance < 0.05f)
				continue;

			float strength = pullForce * (1f + (pullRadius - distance) / pullRadius);
			enemy.ApplyGravityPull(center, strength);
		}
	}
}
