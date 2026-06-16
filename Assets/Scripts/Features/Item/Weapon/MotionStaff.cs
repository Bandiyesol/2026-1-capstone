using UnityEngine;

/// <summary>
/// 스태프 투사체 — 활/총과 동일하게 발사 방향으로 직선 이동합니다.
/// </summary>
public class MotionStaff : Motion
{
	private Vector3 startPos;

	protected override void OnStartMotion() => startPos = transform.position;

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => true;

	protected override void Update()
	{
		base.Update();
		if (IsDestroyed) return;

		if (Vector2.Distance(startPos, transform.position) > instance.reach)
			RequestDestroy(DestroyReason.WeaponLogic);
	}

	protected override void UpdateMovement()
	{
		base.UpdateMovement();
		if (currentActiveRune == null && !RicochetStraightMovementActive)
			transform.Translate(Vector3.right * instance.movespeed * Time.deltaTime);
	}
}
