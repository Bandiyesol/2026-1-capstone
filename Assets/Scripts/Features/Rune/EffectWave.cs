using UnityEngine;

public class EffectWave : RuneEffect, IActiveDriver
{
	float elapsedtime;
	float forwardDistance;
	float wavePhase;
	float amplitude;
	float angularFrequency;
	float waveFrequencyPerUnit;
	float moveSpeed;
	Vector3 origin;
	Vector2 forward;
	Vector2 perpendicular;
	Vector3 prevPosition;

	// 제자리 무기만 duration 종료. 투사체는 날아가는 동안 계속 물결.
	public override bool isFinished =>
		IsStationaryWeapon() && elapsedtime >= RuneDataAccess.GetDuration(data);

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);

		elapsedtime = 0f;
		forwardDistance = 0f;
		wavePhase = 0f;
		origin = transform.position;
		prevPosition = origin;

		forward = transform.right;
		perpendicular = transform.up;

		float range = RuneDataAccess.GetAffectedRange(data);
		float speedMultiplier = RuneDataAccess.GetSpeedMultiplier(data);
		moveSpeed = GetActiveMoveSpeed();

		if (IsStationaryWeapon())
		{
			amplitude = (range > 0f ? range : 0.5f) * weapon.size * 0.35f;
			angularFrequency = Mathf.Max(0.8f, speedMultiplier * 0.6f);
			waveFrequencyPerUnit = 0f;
		}
		else
		{
			amplitude = (range > 0f ? range : 0.5f) * weapon.size * 0.65f;
			// 이동 거리 기준 파형 — 빠른 투사체도 같은 비율로 흔들림
			float wavelength = Mathf.Max(2.5f, weapon.reach / Mathf.Max(1f, speedMultiplier * 1.5f));
			waveFrequencyPerUnit = (Mathf.PI * 2f) / wavelength;
			angularFrequency = 0f;
		}
	}

	public void UpdateMovement()
	{
		elapsedtime += Time.deltaTime;
		forwardDistance += moveSpeed * Time.deltaTime;

		if (IsStationaryWeapon())
			wavePhase += angularFrequency * Time.deltaTime;
		else
			wavePhase = forwardDistance * waveFrequencyPerUnit;

		Vector2 offset = forward * forwardDistance + perpendicular * (Mathf.Sin(wavePhase) * amplitude);
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
