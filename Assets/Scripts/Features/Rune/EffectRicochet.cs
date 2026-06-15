using UnityEngine;

public class EffectRicochet : RuneEffect, ITriggerEffect
{
	int remainingBounces;
	Collider2D lastHitCollider;
	float ignoreHitUntil;

	public bool DestroyOnExecute => remainingBounces <= 0 && data != null && data.isDestroyed;
	public bool ProtectParent => false;

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		remainingBounces = RuneDataAccess.GetBounceCount(data);
		lastHitCollider = null;
		ignoreHitUntil = 0f;
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
		transform.position += (Vector3)(reflectDir * 0.65f);

		lastHitCollider = collision;
		ignoreHitUntil = 0.2f;
		remainingBounces--;
		ResetCooltime();
	}
}
