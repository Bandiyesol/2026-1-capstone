using System.Collections.Generic;
using UnityEngine;

public class EffectHoming : RuneEffect, IActiveDriver
{
	float elapsedtime;
	float duration;
	float searchRadius;
	float turnSpeed;
	float moveSpeed;
	Transform target;
	EffectRicochet cachedRicochet;

	public override bool isFinished => elapsedtime >= duration;

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);

		elapsedtime = 0f;
		duration = RuneDataAccess.GetDuration(data);
		searchRadius = Mathf.Max(2f, RuneDataAccess.GetAffectedRange(data));
		turnSpeed = GetActiveTurnSpeed();
		moveSpeed = GetActiveMoveSpeed();
		cachedRicochet = GetComponent<EffectRicochet>();
		target = FindClosestEnemy();
	}

	public void UpdateMovement()
	{
		if (cachedRicochet != null && cachedRicochet.PreferStraightTravel)
		{
			transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
			return;
		}

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

		transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
	}

	Transform FindClosestEnemy()
	{
		IReadOnlyList<Collider2D> hits = FindEnemyColliders(transform.position, searchRadius);
		Transform closest = null;
		float minSqrDistance = float.MaxValue;

		for (int i = 0; i < hits.Count; i++)
		{
			Collider2D hit = hits[i];
			if (!TryGetDamageable(hit, out _)) continue;

			float sqrDistance = (hit.transform.position - transform.position).sqrMagnitude;
			if (sqrDistance >= minSqrDistance) continue;

			minSqrDistance = sqrDistance;
			closest = hit.transform;
		}

		return closest;
	}
}
