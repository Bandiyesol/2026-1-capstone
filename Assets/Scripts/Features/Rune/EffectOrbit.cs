using UnityEngine;

public class EffectOrbit : RuneEffect, IActiveDriver
{
	float elapsedtime;
	float duration;
	float orbitRadius;
	float orbitAngle;
	float angularSpeed;
	Vector3 center;
	Vector2 forward;
	Transform owner;
	bool followOwner;

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
		angularSpeed = Mathf.Max(1f, RuneDataAccess.GetSpeedMultiplier(data)) * 180f * Mathf.Deg2Rad;

		float angleRad = transform.eulerAngles.z * Mathf.Deg2Rad;
		forward = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;

		owner = PlayerStats.Instance != null ? PlayerStats.Instance.transform : null;
		followOwner = weapon.info != null && IsPlayerAnchoredWeapon(weapon.info.type);
	}

	public void UpdateMovement()
	{
		elapsedtime += Time.deltaTime;

		if (followOwner && owner != null)
			center = owner.position;
		else
			center += (Vector3)(forward * weapon.movespeed * Time.deltaTime);

		orbitAngle += angularSpeed * Time.deltaTime;
		Vector3 offset = new Vector3(Mathf.Cos(orbitAngle), Mathf.Sin(orbitAngle), 0f) * orbitRadius;
		transform.position = center + offset;

		float tangentAngle = (orbitAngle + Mathf.PI * 0.5f) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, tangentAngle);
	}

	static bool IsPlayerAnchoredWeapon(string weaponType)
	{
		return weaponType == "Sword"
			|| weaponType == "Hammer"
			|| weaponType == "Sickle"
			|| weaponType == "Grimore"
			|| weaponType == "Whip";
	}
}