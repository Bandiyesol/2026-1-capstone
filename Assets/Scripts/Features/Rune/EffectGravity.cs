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

		Collider2D[] enemies = Physics2D.OverlapCircleAll(
			transform.position,
			pullRadius,
			LayerMask.GetMask("Enemy")
		);

		foreach (Collider2D enemyCollider in enemies)
		{
			Rigidbody2D enemyBody = enemyCollider.attachedRigidbody;
			if (enemyBody == null) continue;

			Vector2 direction = ((Vector2)transform.position - enemyBody.position).normalized;
			Vector2 nextPosition = enemyBody.position + direction * pullForce * Time.deltaTime;
			enemyBody.MovePosition(nextPosition);
		}
	}
}
