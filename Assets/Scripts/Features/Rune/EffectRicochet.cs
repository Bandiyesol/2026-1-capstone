using UnityEngine;

public class EffectRicochet : RuneEffect, ITriggerEffect
{
	int remainingBounces;

	public bool DestroyOnExecute => remainingBounces <= 0 && data != null && data.isDestroyed;
	public bool ProtectParent => false;

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		remainingBounces = RuneDataAccess.GetBounceCount(data);
	}

	void Update() => UpdateCooltime();

	public void OnReflect(Collider2D collision)
	{
		if (!isReady || remainingBounces <= 0 || !TryGetDamageable(collision, out _))
			return;

		float reverseAngle = 180f + Random.Range(-35f, 35f);
		transform.rotation = Quaternion.Euler(0f, 0f, transform.eulerAngles.z + reverseAngle);
		transform.position += transform.right * 0.15f;

		remainingBounces--;
		ResetCooltime();
	}
}