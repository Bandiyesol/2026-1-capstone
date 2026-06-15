using UnityEngine;

public class MotionBoomerang : Motion
{
	Vector3 startPos;
	Transform owner;
	bool isReturning;
	float outboundSpeed;
	float returnSpeed;
	const float CatchDistance = 0.35f;

	protected override void OnStartMotion()
	{
		startPos = transform.position;
		isReturning = false;
		outboundSpeed = instance.movespeed * 1.15f;
		returnSpeed = instance.movespeed * 0.6f;
		if (PlayerStats.Instance != null) owner = PlayerStats.Instance.transform;
	}

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => false;

	protected override void Update()
	{
		base.Update();
		if (IsDestroyed) return;

		if (!isReturning && Vector2.Distance(startPos, transform.position) >= instance.reach)
			isReturning = true;

		if (isReturning && owner != null && Vector2.Distance(owner.position, transform.position) <= CatchDistance)
			RequestDestroy(DestroyReason.WeaponLogic);
	}

	protected override void UpdateMovement()
	{
		base.UpdateMovement();

		if (currentActiveRune is IActiveDriver driver && !driver.isFinished)
			return;

		Vector2 moveDirection = transform.right;
		float speed = outboundSpeed;

		if (isReturning)
		{
			if (owner == null)
			{
				RequestDestroy(DestroyReason.WeaponLogic);
				return;
			}

			moveDirection = ((Vector2)owner.position - (Vector2)transform.position).normalized;
			speed = returnSpeed;
		}

		float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, angle);
		transform.Translate(Vector3.right * speed * Time.deltaTime, Space.Self);
	}

	public override void ResetForPool()
	{
		base.ResetForPool();
		isReturning = false;
		startPos = Vector3.zero;
		owner = null;
		outboundSpeed = 0f;
		returnSpeed = 0f;
	}
}
