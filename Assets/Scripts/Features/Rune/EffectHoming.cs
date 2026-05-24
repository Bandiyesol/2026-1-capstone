using UnityEngine;

public class EffectHoming : RuneEffect, IActiveDriver
{
    private Transform target;
	private float elapsedtime;
	private float searchtime;


	public override bool isFinished => elapsedtime >= RuneDataAccess.GetDuration(data);


	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		elapsedtime = 0f;
		searchtime = 0f;
		FindTarget();
	}


	public void UpdateMovement()
	{
		elapsedtime += Time.deltaTime;
		searchtime += Time.deltaTime;

		if (target == null || !target.gameObject.activeInHierarchy)
		{
			if (searchtime >= 0.2f)
			{
				FindTarget();
				searchtime = 0f;
			}
		}

		if (target == null)
		{
			transform.Translate(Vector3.right * weapon.movespeed * Time.deltaTime);
			return;
		}

		Vector2 direction = (Vector2)target.position - (Vector2)transform.position;
		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

		float speedMultiplier = RuneDataAccess.GetSpeedMultiplier(data);
		float rotationSpeed = weapon.movespeed * (speedMultiplier > 0f ? speedMultiplier : 1f);
		transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

		transform.Translate(Vector3.right * weapon.movespeed * Time.deltaTime);
		if (Vector2.Distance(transform.position, target.position) < 0.2f) target = null;
	}


	private void FindTarget()
	{
		float range = RuneDataAccess.GetAffectedRange(data);
		float radius = range > 0f ? range : 10f;
		Collider2D[] Enemies = Physics2D.OverlapCircleAll(transform.position, radius, LayerMask.GetMask("Enemy"));

		float minDistance = Mathf.Infinity;
		Transform nearest = null;

		foreach (var enemy in Enemies)
		{
			float distance = Vector2.Distance(transform.position, enemy.transform.position);
			if (distance < minDistance)
			{
				minDistance = distance;
				nearest = enemy.transform;
			}

			target = nearest;
		}
	}
}