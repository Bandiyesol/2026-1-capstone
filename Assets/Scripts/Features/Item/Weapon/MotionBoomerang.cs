using UnityEngine;

public class MotionBoomerang : Motion
{
	private Vector3 startPos;
	private Transform owner;
	private bool isReturning;


	protected override void OnStartMotion()
	{
		startPos = transform.position;
		if (PlayerStats.Instance != null) owner = PlayerStats.Instance.transform;
	}

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => false;


	protected override void Update()
	{
		base.Update();

		if (!isReturning && Vector2.Distance(startPos, transform.position) >= instance.reach)
			isReturning = true;

		if (isReturning && owner != null && Vector2.Distance(owner.position, transform.position) <= 0.2f)
			RequestDestroy(DestroyReason.WeaponLogic);
	}

	protected override void UpdateMovement()
	{
		base.UpdateMovement();
		if (currentActiveRune != null) return;

		Vector2 moveDirection = transform.right;
		if (isReturning)
		{
			if (owner == null)
			{
				RequestDestroy(DestroyReason.WeaponLogic);
				return;
			}

			moveDirection = (owner.position - transform.position).normalized;
		}

		float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, angle);
		transform.Translate(Vector3.right * instance.movespeed * Time.deltaTime);
	}
}
