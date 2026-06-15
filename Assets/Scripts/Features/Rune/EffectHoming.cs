using UnityEngine;

public class EffectHoming : RuneEffect, IActiveDriver
{
	float elapsedtime;
	float duration;
	float searchRadius;
	float turnSpeed;
	Transform target;

	public override bool isFinished => elapsedtime >= duration;

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);

		elapsedtime = 0f;
		duration = RuneDataAccess.GetDuration(data);
		searchRadius = Mathf.Max(1f, RuneDataAccess.GetAffectedRange(data));
		turnSpeed = Mathf.Max(120f, RuneDataAccess.GetSpeedMultiplier(data) * 180f);
		target = FindClosestEnemy();
	}

	public void UpdateMovement()
	{
		elapsedtime += Time.deltaTime;

		if (target == null)
			target = FindClosestEnemy();

		if (target != null)
		{
			Vector2 direction = (target.position - transform.position).normalized;
			float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle, turnSpeed * Time.deltaTime);
			transform.rotation = Quaternion.Euler(0f, 0f, angle);
		}

		transform.Translate(Vector3.right * weapon.movespeed * Time.deltaTime);
	}

	Transform FindClosestEnemy()
	{
		Collider2D[] hits = FindEnemyColliders(transform.position, searchRadius);
		Transform closest = null;
		float minSqrDistance = float.MaxValue;

		foreach (Collider2D hit in hits)
		{
			if (!TryGetDamageable(hit, out _)) continue;

			float sqrDistance = (hit.transform.position - transform.position).sqrMagnitude;
			if (sqrDistance >= minSqrDistance) continue;

			minSqrDistance = sqrDistance;
			closest = hit.transform;
		}

		return closest;
	}
}