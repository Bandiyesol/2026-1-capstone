using UnityEngine;

public class EffectSpiral : RuneEffect, IActiveDriver
{
	float elapsedtime;
	float currentRadius;
	float currentAngle;
	float radialSpeed;
	float angularSpeed;
	Vector3 centerPoint;

	public override bool isFinished => elapsedtime >= RuneDataAccess.GetDuration(data);

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);

		elapsedtime = 0f;
		centerPoint = transform.position;
		currentAngle = transform.eulerAngles.z * Mathf.Deg2Rad;
		currentRadius = 0.1f * weapon.size;

		float range = RuneDataAccess.GetAffectedRange(data);
		float radialMultiplier = range > 0f ? range : 1f;
		radialSpeed = GetActiveMoveSpeed() * (IsStationaryWeapon() ? 0.12f : 0.25f) * radialMultiplier;

		float speedMultiplier = RuneDataAccess.GetSpeedMultiplier(data);
		angularSpeed = Mathf.Max(1f, speedMultiplier * (IsStationaryWeapon() ? 0.8f : 1.5f));
	}

	public void UpdateMovement()
	{
		elapsedtime += Time.deltaTime;

		currentRadius += radialSpeed * Time.deltaTime;
		float safeRadius = Mathf.Max(currentRadius, 0.1f);
		currentAngle += angularSpeed * Time.deltaTime;

		float x = Mathf.Cos(currentAngle) * currentRadius;
		float y = Mathf.Sin(currentAngle) * currentRadius;
		transform.position = centerPoint + new Vector3(x, y, 0f);

		float tangentAngle = (currentAngle + Mathf.PI * 0.5f) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, tangentAngle);
	}
}
