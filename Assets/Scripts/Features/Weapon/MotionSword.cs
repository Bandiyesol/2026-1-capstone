using UnityEngine;


public class MotionSword : Motion
{
	protected override void OnStart() {}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.CompareTag("Enemy"))
		{
			collision.GetComponent<Enemy>()?.TakeDamage(instance.damage);
			ApplyKnockback(collision);
		}
	}

	public void OnAnimationFinished() => Destroy(gameObject);
}