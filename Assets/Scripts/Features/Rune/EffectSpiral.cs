using UnityEngine;

public class EffectSpiral : RuneEffect, IActiveDriver
{
	private float elapsedtime;
	private float currentRadius;
	private float currentAngle;
	private float radialSpeed;
	private float angularSpeedMultiplier;
	private Vector3 centerPoint;


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
		radialSpeed = weapon.movespeed * 0.35f * radialMultiplier;

		float speedMultiplier = RuneDataAccess.GetSpeedMultiplier(data);
		angularSpeedMultiplier = speedMultiplier > 0f ? speedMultiplier : 1f;
	}


	public void UpdateMovement()
	{
		elapsedtime += Time.deltaTime;

		currentRadius += radialSpeed * Time.deltaTime;
		float safeRadius = Mathf.Max(currentRadius, 0.1f);
		currentAngle += weapon.movespeed * angularSpeedMultiplier * Time.deltaTime / safeRadius;

		float x = Mathf.Cos(currentAngle) * currentRadius;
		float y = Mathf.Sin(currentAngle) * currentRadius;
		transform.position = centerPoint + new Vector3(x, y, 0f);

		float tangentAngle = (currentAngle + Mathf.PI * 0.5f) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, tangentAngle);
	}
}
