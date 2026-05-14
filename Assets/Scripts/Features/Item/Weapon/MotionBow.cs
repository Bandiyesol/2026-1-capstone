using UnityEngine;

public class MotionBow : Motion
{
	private Vector3 startPos;
	private bool isInitialized = false;



	protected override void OnStartMotion()
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


	protected override void OnTriggerEnter2D(Collider2D collision) => HandleCollision(collision);


	private void CheckDistance()
	{
		float distance = Vector2.Distance(startPos, transform.position);
		
		if (distance > instance.reach) CheckDestruction();
	}
}