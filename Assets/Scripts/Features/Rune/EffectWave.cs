using UnityEngine;

public class EffectWave : RuneEffect, IActiveDriver
{
	private float elapsedtime;
	private float forwardDistance;
	private float wavePhase;
	private float amplitude;
	private float angularFrequency;
	private Vector3 origin;
	private Vector2 forward;
	private Vector2 perpendicular;
	private Vector3 prevPosition;


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
		amplitude = (range > 0f ? range : 0.75f) * weapon.size;

		float speedMultiplier = RuneDataAccess.GetSpeedMultiplier(data);
		angularFrequency = Mathf.Max(1f, speedMultiplier) * 6f;
		prevPosition = transform.position;
	}


	public void UpdateMovement()
	{
		elapsedtime += Time.deltaTime;

		forwardDistance += weapon.movespeed * Time.deltaTime;
		wavePhase += angularFrequency * Time.deltaTime;

		Vector2 offset = forward * forwardDistance + perpendicular * Mathf.Sin(wavePhase) * amplitude;
		Vector3 nextPosition = origin + new Vector3(offset.x, offset.y, 0f);
		transform.position = nextPosition;

		Vector2 moveDir = (nextPosition - prevPosition);
		if (moveDir.sqrMagnitude > 0.0001f)
		{
			float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
			transform.rotation = Quaternion.Euler(0f, 0f, angle);
		}

		prevPosition = nextPosition;
	}
}
