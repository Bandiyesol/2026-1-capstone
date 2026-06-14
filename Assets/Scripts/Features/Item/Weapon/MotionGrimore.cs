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
		orbitRadius = Mathf.Max(1.0f, instance.reach * 0.45f);
	}

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => false;


	protected override void UpdateMovement()
	{
		base.UpdateMovement();

		// 수명 종료로 파괴됐다면 instance가 null이므로 즉시 중단
		if (IsDestroyed) return;
		if (currentActiveRune != null) return;

		if (owner == null)
		{
			RequestDestroy(DestroyReason.WeaponLogic);
			return;
		}

		orbitAngle += Mathf.Max(30f, instance.movespeed * 25f) * Time.deltaTime;
		float radian = orbitAngle * Mathf.Deg2Rad;
		Vector3 offset = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0f) * orbitRadius;
		transform.position = owner.position + offset;
	}
}
