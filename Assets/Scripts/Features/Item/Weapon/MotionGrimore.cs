using UnityEngine;

public class MotionGrimore : Motion
{
	private Transform owner;
	private float orbitAngle;
	private float orbitRadius;


	protected override void OnStartMotion()
	{
		if (PlayerStats.Instance != null) owner = PlayerStats.Instance.transform;
		orbitAngle = Random.Range(0f, 360f);
		orbitRadius = Mathf.Max(0.7f, instance.reach * 0.3f);
	}

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => false;


	protected override void UpdateMovement()
	{
		base.UpdateMovement();
		if (currentActiveRune != null) return;

		if (owner == null)
		{
			RequestDestroy(DestroyReason.WeaponLogic);
			return;
		}

		orbitAngle += Mathf.Max(90f, instance.movespeed * 90f) * Time.deltaTime;
		float radian = orbitAngle * Mathf.Deg2Rad;
		Vector3 offset = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0f) * orbitRadius;
		transform.position = owner.position + offset;
	}
}
