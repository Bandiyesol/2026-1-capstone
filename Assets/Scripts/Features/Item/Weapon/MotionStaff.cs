using UnityEngine;

public class MotionStaff : Motion
{
	private Vector3 startPos;
	private Transform target;


	protected override void OnStartMotion()
	{
		startPos = transform.position;
		target = FindClosestEnemy();
	}

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => true;


	protected override void Update()
	{
		base.Update();
		if (Vector2.Distance(startPos, transform.position) > instance.reach) RequestDestroy(DestroyReason.WeaponLogic);
	}

	protected override void UpdateMovement()
	{
		base.UpdateMovement();
		if (currentActiveRune != null) return;

		if (target == null)
			target = FindClosestEnemy();

		Vector2 direction = transform.right;
		if (target != null)
			direction = (target.position - transform.position).normalized;

		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, angle);
		transform.Translate(Vector3.right * instance.movespeed * Time.deltaTime);
	}

	Transform FindClosestEnemy()
	{
		Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Mathf.Max(0.5f, instance.reach));
		Transform closest = null;
		float minSqrDistance = float.MaxValue;

		foreach (Collider2D hit in hits)
		{
			if (hit == null || !hit.CompareTag("Enemy")) continue;

			float sqrDistance = (hit.transform.position - transform.position).sqrMagnitude;
			if (sqrDistance >= minSqrDistance) continue;

			minSqrDistance = sqrDistance;
			closest = hit.transform;
		}

		return closest;
	}
}
