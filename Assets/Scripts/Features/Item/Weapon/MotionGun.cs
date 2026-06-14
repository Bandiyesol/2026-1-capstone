using UnityEngine;

public class MotionGun : Motion
{
	private Vector3 startPos;


	protected override void OnStartMotion() => startPos = transform.position;

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => true;


	protected override void Update()
	{
		base.Update();
		// base.Update()에서 수명 종료로 파괴됐다면 instance가 null이므로 즉시 중단
		if (IsDestroyed) return;

		if (Vector2.Distance(startPos, transform.position) > instance.reach) RequestDestroy(DestroyReason.WeaponLogic);
	}

	protected override void UpdateMovement()
	{
		base.UpdateMovement();
		if (currentActiveRune == null) transform.Translate(Vector3.right * instance.movespeed * Time.deltaTime);
	}
}
