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
		pullRadius = RuneDataAccess.GetGravityRadius(data);
	}

	public void UpdateState()
	{
		if (isFinished)
			return;

		elapsedtime += Time.deltaTime;
		if (pullForce <= 0f || pullRadius <= 0f)
			return;

		Vector2 center = transform.position;
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
