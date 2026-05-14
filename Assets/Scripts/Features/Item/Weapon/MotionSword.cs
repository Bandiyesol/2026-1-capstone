using UnityEngine;

public class MotionSword : Motion
{
	private bool isInitalized = false;



	protected override void OnStartMotion() {}


	protected override void OnTriggerEnter2D(Collider2D collision) => HandleCollision(collision);


	protected override void CheckDestruction() 
	{
		if(isInitalized) Destroy(gameObject);
	}


	public void OnAnimationFinished()
	{
		isInitalized = true;
		if (activeRunes == null || activeRunes.Count == 0) CheckDestruction();
	}
}