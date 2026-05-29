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
