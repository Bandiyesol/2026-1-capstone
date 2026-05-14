using UnityEngine;

public class EffectExplode : RuneEffect
{
	private void Start() => Explode();

	
	private void Explode()
	{
		float radius = 0.5f * weapon.reach;
		float damage = 0.8f * weapon.damage;

		Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
		foreach (Collider2D hit in hits)
		{
			if (hit.CompareTag("Enemy"))
			{
				IDamageable target = hit.GetComponent<IDamageable>();
				if (target != null) target.TakeDamage(damage);
			}
		}

		Destroy(gameObject);
	}
}