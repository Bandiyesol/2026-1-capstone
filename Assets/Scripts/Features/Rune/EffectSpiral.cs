using UnityEngine;

public class EffectSpiral : RuneEffect, IActiveDriver
{
	float elapsedtime;
	float forwardDistance;
	float currentAngle;
	float currentRadius;
	float moveSpeed;
	float angularSpeed;
	float maxRadius;
	float radiusGrowthRate;
	float startAngle;
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
		origin = transform.position;
		prevPosition = origin;

		float angleRad = transform.eulerAngles.z * Mathf.Deg2Rad;
		forward = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;
		perpendicular = new Vector2(-forward.y, forward.x);
		startAngle = angleRad;
		currentAngle = startAngle;
		currentRadius = 0.08f * weapon.size;

		float range = RuneDataAccess.GetAffectedRange(data);
		maxRadius = (range > 0f ? range : 1f) * weapon.size
			* (IsStationaryWeapon() ? 0.35f : 0.25f);

		float speedMultiplier = RuneDataAccess.GetSpeedMultiplier(data);
		float duration = Mathf.Max(0.1f, RuneDataAccess.GetDuration(data));

		// 회전은 촘촘하게, 반지름은 duration 동안 천천히 maxRadius까지
		angularSpeed = Mathf.Max(4f, speedMultiplier * (IsStationaryWeapon() ? 4f : 9f));
		radiusGrowthRate = maxRadius / duration;

		moveSpeed = GetActiveMoveSpeed();
	}

	public void UpdateMovement()
	{
		elapsedtime += Time.deltaTime;

		currentAngle += angularSpeed * Time.deltaTime;
		currentRadius = Mathf.Min(maxRadius, 0.08f * weapon.size + radiusGrowthRate * elapsedtime);

		float localAngle = currentAngle - startAngle;

		if (IsStationaryWeapon())
		{
			float x = Mathf.Cos(localAngle) * currentRadius;
			float y = Mathf.Sin(localAngle) * currentRadius;
			transform.position = origin + new Vector3(x, y, 0f);
		}
		else
		{
			forwardDistance += moveSpeed * Time.deltaTime;
			Vector2 center = (Vector2)origin + forward * forwardDistance;
			Vector2 orbitOffset = forward * (Mathf.Cos(localAngle) * currentRadius)
				+ perpendicular * (Mathf.Sin(localAngle) * currentRadius);
			transform.position = center + orbitOffset;
		}

		Vector2 moveDir = (Vector2)transform.position - (Vector2)prevPosition;
		if (moveDir.sqrMagnitude > 0.0001f)
		{
			float faceAngle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
			transform.rotation = Quaternion.Euler(0f, 0f, faceAngle);
		}

		prevPosition = transform.position;
	}
}
