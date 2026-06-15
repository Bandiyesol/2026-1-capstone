using UnityEngine;

public class EffectRicochet : RuneEffect, ITriggerEffect
{
	const float RicochetMoveSpeedScale = 0.45f;
	const float BounceMoveSpeedRetention = 0.88f;
	const float MinMoveSpeed = 0.35f;

	int remainingBounces;
	Collider2D lastHitCollider;
	float ignoreHitUntil;
	bool useStraightTravel;

	public bool DestroyOnExecute => remainingBounces <= 0 && data != null && data.isDestroyed;
	public bool ProtectParent => false;

	/// <summary>도탄 후 유도/홈링을 끄고 반사 방향으로만 이동 (스태프 진동 방지).</summary>
	public bool PreferStraightTravel => useStraightTravel;

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		remainingBounces = RuneDataAccess.GetBounceCount(data);
		lastHitCollider = null;
		ignoreHitUntil = 0f;
		useStraightTravel = false;

		if (weapon != null)
			weapon.movespeed = Mathf.Max(MinMoveSpeed, weapon.movespeed * RicochetMoveSpeedScale);
	}

	void Update()
	{
		UpdateCooltime();
		if (ignoreHitUntil > 0f)
			ignoreHitUntil -= Time.deltaTime;
	}

	public void OnReflect(Collider2D collision)
	{
		if (!isReady || remainingBounces <= 0 || !TryGetDamageable(collision, out _))
			return;

		if (collision == lastHitCollider && ignoreHitUntil > 0f)
			return;

		Vector2 hitPoint = collision.ClosestPoint(transform.position);
		Vector2 normal = ((Vector2)transform.position - hitPoint).normalized;
		if (normal.sqrMagnitude < 0.0001f)
			normal = -transform.right;

		Vector2 reflectDir = Vector2.Reflect(transform.right, normal).normalized;
		float angle = Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, angle);

		float separation = Mathf.Max(1.2f, weapon.size * 0.9f);
		transform.position += (Vector3)(reflectDir * separation);

		lastHitCollider = collision;
		ignoreHitUntil = Mathf.Max(0.3f, RuneDataAccess.GetInterval(data));
		useStraightTravel = true;
		remainingBounces--;

		if (weapon != null)
			weapon.movespeed = Mathf.Max(MinMoveSpeed, weapon.movespeed * BounceMoveSpeedRetention);

		ResetCooltime();
	}
}
