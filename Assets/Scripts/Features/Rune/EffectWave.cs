using UnityEngine;

public class EffectWave : RuneEffect, IActiveDriver
{
	float elapsedtime;
	float forwardDistance;
	float wavePhase;
	float amplitude;
	float angularFrequency;
	float moveSpeed;
	Vector3 origin;
	Vector2 forward;
	Vector2 perpendicular;
	Vector3 prevPosition;

	public override bool isFinished => elapsedtime >= RuneDataAccess.GetDuration(data);

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);

		elapsedtime = 0f;
		forwardDistance = 0f;
		wavePhase = 0f;
		origin = transform.position;

		float angleRad = transform.eulerAngles.z * Mathf.Deg2Rad;
		forward = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;
		perpendicular = new Vector2(-forward.y, forward.x);

		float range = RuneDataAccess.GetAffectedRange(data);
		amplitude = (range > 0f ? range : 0.75f) * weapon.size * (IsStationaryWeapon() ? 1.2f : 1.8f);

		float speedMultiplier = RuneDataAccess.GetSpeedMultiplier(data);
		angularFrequency = Mathf.Max(1.5f, speedMultiplier) * (IsStationaryWeapon() ? 2.5f : 5f);
		moveSpeed = GetActiveMoveSpeed() * (IsStationaryWeapon() ? 0.35f : 0.75f);
		prevPosition = transform.position;
	}

	public void UpdateMovement()
	{
		elapsedtime += Time.deltaTime;

		forwardDistance += moveSpeed * Time.deltaTime;
		wavePhase += angularFrequency * Time.deltaTime;

		Vector2 offset = forward * forwardDistance + perpendicular * Mathf.Sin(wavePhase) * amplitude;
		Vector3 nextPosition = origin + new Vector3(offset.x, offset.y, 0f);
		transform.position = nextPosition;

		Vector2 moveDir = nextPosition - prevPosition;
		if (moveDir.sqrMagnitude > 0.0001f)
		{
			float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
			transform.rotation = Quaternion.Euler(0f, 0f, angle);
		}

		prevPosition = nextPosition;
	}
}
