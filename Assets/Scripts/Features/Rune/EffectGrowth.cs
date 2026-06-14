using UnityEngine;

public class EffectGrowth : RuneEffect, IStateEffect
{
	private float elapsedtime;
	private float maxGrowthTime;
	private float maxScaleRatio;
	private float maxDamageRatio;
	private float baseDamage;
	private Vector3 baseScale;


	public override bool isFinished => elapsedtime >= maxGrowthTime;


	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		elapsedtime = 0f;
		baseScale = transform.localScale;
		baseDamage = weapon.damage;

		maxGrowthTime = RuneDataAccess.GetGrowthDuration(data);
		maxScaleRatio = RuneDataAccess.GetGrowthScaleRatio(data);
		maxDamageRatio = RuneDataAccess.GetGrowthDamageRatio(data);

		// 무기 수명(spawntime)이 maxGrowthTime보다 짧으면 수명에 맞춰 스케일 시간을 단축.
		// 이렇게 하면 검처럼 짧게 사는 무기도 수명 끝 무렵에 최대 크기에 도달한다.
		float weaponLife = weapon.spawntime;
		if (weaponLife > 0f && weaponLife < maxGrowthTime)
			maxGrowthTime = weaponLife;
	}


	public void UpdateState()
	{
		if (maxGrowthTime <= 0f)
			return;

		if (!isFinished)
			elapsedtime += Time.deltaTime;

		float progress = Mathf.Clamp01(elapsedtime / maxGrowthTime);
		float scaleRatio = Mathf.Lerp(1f, maxScaleRatio, progress);
		float damageRatio = Mathf.Lerp(1f, maxDamageRatio, progress);

		transform.localScale = baseScale * scaleRatio;
		weapon.damage = baseDamage * damageRatio;
	}
}
