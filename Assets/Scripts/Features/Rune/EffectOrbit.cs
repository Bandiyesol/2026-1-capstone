using UnityEngine;

public class EffectOrbit : RuneEffect, IActiveDriver
{
	private float elapsedtime;
	private Vector3 point;
	private float currentAngle;
	private float orbitRange;


	public override bool isFinished => elapsedtime >= RuneDataAccess.GetDuration(data);


	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);

		orbitRange = RuneDataAccess.GetAffectedRange(data);
		float initialRadius = orbitRange > 0f ? orbitRange * weapon.size : 2f * weapon.size;
		elapsedtime = 0f;
		currentAngle = 0f;
		point = transform.position - new Vector3(Mathf.Cos(currentAngle) * initialRadius, Mathf.Sin(currentAngle) * initialRadius, 0);
	}


	public void UpdateMovement()
	{
		elapsedtime += Time.deltaTime;

		float currentRadius = orbitRange > 0f ? orbitRange * weapon.size : 2f * weapon.size;
		float angularSpeed = weapon.movespeed * data.power / currentRadius;
		currentAngle += angularSpeed * Time.deltaTime;

		float x = Mathf.Cos(currentAngle) * currentRadius;
		float y = Mathf.Sin(currentAngle) * currentRadius;

		transform.position = point + new Vector3(x, y, 0f);

		float lookAngle = (currentAngle + Mathf.PI / 2f) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0, 0, lookAngle);
	}
}