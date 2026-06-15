using UnityEngine;

public class EffectExplode : RuneEffect, ITriggerEffect
{
	public bool DestroyOnExecute => data != null && data.isDestroyed;
	public bool ProtectParent => false;

	void Update() => UpdateCooltime();

	public void OnReflect(Collider2D collision)
	{
		if (!isReady || parentMotion == null || parentMotion.IsExplosionRunning)
			return;

		if (!TryGetDamageable(collision, out _))
			return;

		Vector3 explodePos = collision.ClosestPoint(transform.position);
		float radius = RuneDataAccess.GetExplodeRadius(data);
		float explodeDamage = DamageCalculator.CalculateBaseDamage(weapon, data);

		parentMotion.StartExplosionAt(explodePos, radius, explodeDamage, DestroyOnExecute);
		ResetCooltime();
	}
}
