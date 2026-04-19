using UnityEngine;

public class WeaponProjectile : Weapon
{
	[Header("[ Projectiles Settings ]")]
	public GameObject projectilePrefab;

	[Header("[ Aim Setting ]")]
	public float viewAngle;
	private Vector2 lastDirection;


	public override void Init(WeaponInfo newInfo, WeaponBalance balance, LayerMask layer)
	{
		base.Init(newInfo, balance, layer);
		// projectilePrefab = WeaponManager.Instance.GetProjectilePrefab(newInfo.spriteId);
	}

	private void Update()
	{
		PlayerController player = GetComponentInParent<PlayerController>();

		if (player != null) this.lastDirection = player.lastDirection;
	}

	protected override void Attack()
	{
		Transform target = GetNearestEnemy();
		Vector3 fireDirection = target != null ? (target.position - transform.position).normalized : (Vector3)lastDirection;
		
		GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
		Projectile projectile = obj.GetComponent<Projectile>();

		if(projectile != null) projectile.Setup(finalDamage, finalReach, finalSpeed, fireDirection);
	}


	Transform GetNearestEnemy()
	{
		Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, finalReach, monsterLayer);
		Transform nearest = null;
		
		float minDistance = Mathf.Infinity;

		foreach (var enemy in enemies)
		{
			Vector3 direction = (enemy.transform.position - transform.position).normalized;
			
			float angle = Vector2.Angle(lastDirection, direction);
			float distance = Vector2.Distance(transform.position, enemy.transform.position);

			if (distance < minDistance && angle <= viewAngle * 0.5f)
			{
				minDistance = distance;
				nearest = enemy.transform;
			}
		}

		return nearest;
	}

	void OnDrawGizmos()
	{
		if (info == null) return;

		Gizmos.color = Color.cyan;
		GizmosExtensions.DrawWireShift(transform.position, lastDirection, viewAngle, finalReach);
	}
}

public static class GizmosExtensions
{
	public static void DrawWireShift(Vector3 pos, Vector3 dir, float angle, float range)
	{
		Vector3 left = Quaternion.AngleAxis(-angle * 0.5f, Vector3.forward) * dir;
		Vector3 right = Quaternion.AngleAxis(angle * 0.5f, Vector3.forward) * dir;

		Gizmos.DrawLine(pos, pos + left * range);
		Gizmos.DrawLine(pos, pos + right * range);

		int segments = 10;
		Vector3 previousPoint = pos + left * range;

		for (int i = 1; i <= segments; i++)
		{
			float currentAngle = angle * 0.5f + (angle / segments) * i;
			Vector3 nextPoint = pos + (Quaternion.AngleAxis(currentAngle, Vector3.forward) * dir) * range;

			Gizmos.DrawLine(previousPoint, nextPoint);
			
			previousPoint = nextPoint;
		}
	}
}