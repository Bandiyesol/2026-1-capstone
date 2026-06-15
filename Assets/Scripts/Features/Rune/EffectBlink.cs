using UnityEngine;

public class EffectBlink : RuneEffect, ILogicEffect
{
	float travelAccumulator;
	float minTravelTime;
	float travelSpeed;

	void Update() => UpdateCooltime();

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		travelAccumulator = 0f;
		minTravelTime = 0.35f;
		travelSpeed = IsStationaryWeapon()
			? Mathf.Max(4f, RuneDataAccess.GetAffectedRange(data) * 0.5f)
			: Mathf.Max(0.5f, weapon.movespeed);
	}

	public void UpdateLogic()
	{
		if (!isReady)
			return;

		travelAccumulator += Time.deltaTime;
		transform.Translate(Vector3.right * travelSpeed * Time.deltaTime);

		if (travelAccumulator < minTravelTime)
			return;

		float distance = RuneDataAccess.GetLogicDistance(data);
		if (distance <= 0f)
			return;

		transform.position += transform.right * distance;
		travelAccumulator = 0f;
		ResetCooltime();
	}
}
