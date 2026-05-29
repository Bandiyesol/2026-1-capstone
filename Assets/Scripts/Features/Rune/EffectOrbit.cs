using UnityEngine;

// 특정 지점을 중심으로 궤도를 도는 룬 효과
public class EffectOrbit : RuneEffect, IActiveDriver
{
	private float elapsedtime;     // 진행 시간
	private Vector3 point;         // 공전의 중심점
	private float currentAngle;    // 현재 공전 각도(라디안)
	private float orbitRange;      // 공전 반경 범위

	public override bool isFinished => elapsedtime >= RuneDataAccess.GetDuration(data);

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);

		orbitRange = RuneDataAccess.GetAffectedRange(data);
		// 무기 크기를 반영한 초기 공전 반경 계산
		float initialRadius = orbitRange > 0f ? orbitRange * weapon.size : 2f * weapon.size;
		elapsedtime = 0f;
		currentAngle = 0f;

		// 무기가 바로 궤도를 돌 수 있도록, 현재 위치를 기반으로 중심점(point) 역산
		point = transform.position - new Vector3(Mathf.Cos(currentAngle) * initialRadius, Mathf.Sin(currentAngle) * initialRadius, 0);
	}

	public void UpdateMovement()
	{
		elapsedtime += Time.deltaTime;

		float currentRadius = orbitRange > 0f ? orbitRange * weapon.size : 2f * weapon.size;
		// 각속도 계산 (v = r * ω 공식 응용)
		float angularSpeed = weapon.movespeed * RuneDataAccess.GetSpeedMultiplier(data) / currentRadius;
		currentAngle += angularSpeed * Time.deltaTime;

		// 원운동의 X, Y 좌표 계산
		float x = Mathf.Cos(currentAngle) * currentRadius;
		float y = Mathf.Sin(currentAngle) * currentRadius;

		// 무기를 중심점(point)에서 계산된 위치로 이동
		transform.position = point + new Vector3(x, y, 0f);

		// 진행 방향을 바라보도록 회전 (접선 방향이므로 90도(PI/2) 더함)
		float lookAngle = (currentAngle + Mathf.PI / 2f) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0, 0, lookAngle);
	}
}