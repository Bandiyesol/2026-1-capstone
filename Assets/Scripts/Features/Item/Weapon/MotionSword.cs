using UnityEngine;

public class MotionSword : Motion
{
	private Animator animationCtrl;
	private bool isFinished = false;



	protected override void OnStartMotion() => animationCtrl = GetComponent<Animator>();

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => false;


	public void OnAnimationFinished()
	{
		if (currentActiveRune != null && currentActiveRune is IActiveDriver driver && !driver.isFinished)
		{
			if (animationCtrl != null) animationCtrl.Play("Attack", 0, 0f);
			return;
		}

		isFinished = true;
		RequestDestroy(DestroyReason.WeaponLogic);
	}


	protected override bool ActuallyDestroy()
	{
		return base.ActuallyDestroy() && isFinished;
	}
}