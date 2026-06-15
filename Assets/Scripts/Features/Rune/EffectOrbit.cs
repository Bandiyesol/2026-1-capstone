using UnityEngine;

public class EffectOrbit : RuneEffect, IActiveDriver
{
	float elapsedtime;
	float duration;
	float orbitRadius;
	float orbitAngle;
	float angularSpeed;
	Vector3 center;

	public override bool isFinished => elapsedtime >= duration;

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);

		elapsedtime = 0f;
		duration = RuneDataAccess.GetDuration(data);
		center = transform.position;
		orbitAngle = transform.eulerAngles.z * Mathf.Deg2Rad;

		float range = RuneDataAccess.GetAffectedRange(data);
		orbitRadius = Mathf.Max(0.8f, (range > 0f ? range : weapon.reach) * 0.15f * weapon.size);
		angularSpeed = Mathf.Max(25f, RuneDataAccess.GetSpeedMultiplier(data) * (IsStationaryWeapon() ? 35f : 55f)) * Mathf.Deg2Rad;
	}

	public void UpdateMovement()
	{
		elapsedtime += Time.deltaTime;

		// 생성 시점 위치를 중심으로 고정 공전 (플레이어 추적 없음)
		orbitAngle += angularSpeed * Time.deltaTime;
		Vector3 offset = new Vector3(Mathf.Cos(orbitAngle), Mathf.Sin(orbitAngle), 0f) * orbitRadius;
		transform.position = center + offset;

		float tangentAngle = (orbitAngle + Mathf.PI * 0.5f) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, tangentAngle);
	}
}
