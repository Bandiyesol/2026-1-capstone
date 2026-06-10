using UnityEngine;

public class MotionHammer : Motion
{
	const float SlamDurationRatio = 0.25f;
	const float SlamScaleBonus = 0.25f;
	const float ForwardReachRatio = 0.2f;

	Vector3 startPosition;
	Vector3 baseScale;
	float elapsed;

	protected override void OnStartMotion()
	{
		elapsed = 0f;
		startPosition = transform.position;
		baseScale = transform.localScale;
	}

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => false;

	protected override void UpdateMovement()
	{
		base.UpdateMovement();

		if (instance == null)
			return;

		elapsed += Time.deltaTime;

		float slamDuration = Mathf.Max(0.08f, instance.spawntime * SlamDurationRatio);
		float t = Mathf.Clamp01(elapsed / slamDuration);
		float pulse = 1f + Mathf.Sin(t * Mathf.PI) * SlamScaleBonus;

		transform.localScale = baseScale * pulse;
		transform.position = startPosition + transform.right * (instance.reach * ForwardReachRatio * t);
	}

	public override void ResetForPool()
	{
		base.ResetForPool();
		elapsed = 0f;
		transform.localScale = baseScale == Vector3.zero ? transform.localScale : baseScale;
	}
}
