using UnityEngine;

public class EffectBoing : RuneEffect, ILogicEffect
{
	private void Update() => UpdateCooltime();


	public void UpdateLogic()
	{
		if (!isReady)
			return;

		Camera cam = Camera.main;
		if (cam == null)
			return;

		Vector3 viewPos = cam.WorldToViewportPoint(transform.position);
		if (viewPos.x >= 0f && viewPos.x <= 1f && viewPos.y >= 0f && viewPos.y <= 1f)
			return;

		float depth = Mathf.Abs(transform.position.z - cam.transform.position.z);
		float wrappedX = viewPos.x < 0f ? 1f : (viewPos.x > 1f ? 0f : viewPos.x);
		float wrappedY = viewPos.y < 0f ? 1f : (viewPos.y > 1f ? 0f : viewPos.y);
		Vector3 wrappedWorld = cam.ViewportToWorldPoint(new Vector3(wrappedX, wrappedY, depth));

		transform.position = new Vector3(wrappedWorld.x, wrappedWorld.y, transform.position.z);
		ResetCooltime();
	}
}
