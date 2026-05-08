using UnityEngine;


public class MotionBow : Motion
{
	private Vector3 startPos;
	private bool isInitialized = false;


	protected override void OnStart()
	{
		startPos = transform.position;
		Rigidbody2D rigidBody = GetComponent<Rigidbody2D>();

		if (rigidBody != null) rigidBody.linearVelocity = Vector2.zero;

		isInitialized = true;
	}

	private void FixedUpdate()
	{
		if (!isInitialized || instance == null) return;

		transform.position += transform.right * instance.speed * Time.fixedDeltaTime;

		CheckDistance();
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.CompareTag("Wall") || collision.CompareTag("Ground")) Destroy(gameObject);

		if (collision.CompareTag("Enemy"))
		{
			collision.GetComponent<Enemy>()?.TakeDamage(instance.damage);
			ApplyKnockback(collision);
			Destroy(gameObject);
		}
		
	}

	private void CheckDistance()
	{
		float distance = Vector2.Distance(startPos, transform.position);
		
		if (distance > instance.reach) Destroy(gameObject);
	}
}