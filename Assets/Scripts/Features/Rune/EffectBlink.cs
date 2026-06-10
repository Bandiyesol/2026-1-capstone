using UnityEngine;

public class EffectBlink : RuneEffect, ILogicEffect
{
	private void Update() => UpdateCooltime();


	public void UpdateLogic()
	{
		if (!isReady)
			return;

		float distance = RuneDataAccess.GetLogicDistance(data);
		if (distance <= 0f)
			return;

		transform.position += transform.right * distance;
		ResetCooltime();
	}
}
