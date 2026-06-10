using UnityEngine;

public class EffectFreeze : RuneEffect, ITriggerEffect
{
	public bool DestroyOnExecute => data != null && data.isDestroyed;
	public bool ProtectParent => false;


	private void Update() => UpdateCooltime();


	public void OnReflect(Collider2D collision)
	{
		if (!isReady || collision.GetComponent<IDamageable>() == null)
			return;

		float radius = RuneDataAccess.GetFreezeRadius(data);
		float duration = RuneDataAccess.GetFreezeDuration(data);
		if (radius <= 0f || duration <= 0f)
			return;

		Collider2D[] enemies = Physics2D.OverlapCircleAll(
			collision.transform.position,
			radius,
			LayerMask.GetMask("Enemy")
		);

		foreach (Collider2D enemyCollider in enemies)
		{
			Enemy enemy = enemyCollider.GetComponent<Enemy>();
			if (enemy != null) enemy.ApplyFreeze(duration);
		}

		ResetCooltime();
	}
}
