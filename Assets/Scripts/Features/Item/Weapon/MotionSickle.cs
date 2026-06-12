using UnityEngine;

public class MotionSickle : Motion
{
	private Vector3 startPos;
	private const float RotationSpeed = 540f;


	protected override void OnStartMotion() => startPos = transform.position;

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => false;


	protected override void Update()
	{
		base.Update();
		if (Vector2.Distance(startPos, transform.position) > instance.reach) RequestDestroy(DestroyReason.WeaponLogic);
	}

	protected override void UpdateMovement()
	{
		base.UpdateMovement();
		if (currentActiveRune != null) return;

		transform.Translate(Vector3.right * instance.movespeed * Time.deltaTime);
		transform.Rotate(0f, 0f, RotationSpeed * Time.deltaTime);
	}
}
