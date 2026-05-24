using UnityEngine;

public class EffectRicochet : RuneEffect, ITriggerEffect
{
	private int maxCount;
    private int currentCount;
	public bool DestroyOnExecute => data.isDestroyed;
	public bool ProtectParent => currentCount > 0;


	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		maxCount = RuneDataAccess.GetBounceCount(data);
		currentCount = 0;
	}


	private void Update()
	{
		if (currentCount <= 0) UpdateCooltime();
	}


	public void OnReflect(Collider2D collision)
	{
		if (currentCount > 0)
		{
			PerformPhysicalReflect(collision);
			currentCount--;

			if (currentCount <= 0) ResetCooltime();
			return;
		}

		if (isReady)
		{
			currentCount = maxCount;
			PerformPhysicalReflect(collision);
			currentCount--;

			if (currentCount <= 0) ResetCooltime();
		}
	}


	private void PerformPhysicalReflect(Collider2D collision)
	{
		Vector2 incoming = transform.right;
		Vector2 hitPoint = collision.ClosestPoint(transform.position);
		Vector2 normal = ((Vector2)transform.position - hitPoint).normalized;
		Vector2 reflect = Vector2.Reflect(incoming, normal);

		float angle = Mathf.Atan2(reflect.y, reflect.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0, 0, angle);
	}
}