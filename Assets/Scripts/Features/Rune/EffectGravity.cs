using UnityEngine;

public class EffectGravity : RuneEffect, IStateEffect
{
	private float elapsedtime;
	private float duration;
	private float pullForce;
	private float pullRadius;


	public override bool isFinished => elapsedtime >= duration;


	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		elapsedtime = 0f;
		duration = RuneDataAccess.GetDuration(data);
		pullForce = RuneDataAccess.GetPullForce(data);
		pullRadius = RuneDataAccess.GetGravityRadius(data);
	}


	public void UpdateState()
	{
		if (isFinished)
			return;

		elapsedtime += Time.deltaTime;
		if (pullForce <= 0f || pullRadius <= 0f)
			return;

		Collider2D[] enemies = FindEnemyColliders(transform.position, pullRadius);

		foreach (Collider2D enemyCollider in enemies)
		{
			if (!TryGetDamageable(enemyCollider, out _)) continue;

			Rigidbody2D enemyBody = enemyCollider.attachedRigidbody;
			Vector2 currentPosition = enemyBody != null
				? enemyBody.position
				: (Vector2)enemyCollider.transform.position;

			Vector2 direction = ((Vector2)transform.position - currentPosition).normalized;
			Vector2 nextPosition = currentPosition + direction * pullForce * Time.deltaTime;

			if (enemyBody != null)
			{
				enemyBody.linearVelocity = Vector2.zero;
				enemyBody.MovePosition(nextPosition);
			}
			else
			{
				enemyCollider.transform.position = nextPosition;
			}
		}
	}
}
