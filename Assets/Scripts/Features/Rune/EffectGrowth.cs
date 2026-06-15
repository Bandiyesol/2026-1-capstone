using UnityEngine;

public class EffectGrowth : RuneEffect, IStateEffect
{
	float maxGrowthTime;
	float maxScaleRatio;
	float maxDamageRatio;
	float baseDamage;
	Vector3 baseScale;

	public override bool isFinished => false;

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		parentMotion?.RestoreDefaultColliderSizes();
		baseScale = transform.localScale;
		baseDamage = weapon.damage;

		maxGrowthTime = RuneDataAccess.GetGrowthDuration(data);
		maxScaleRatio = RuneDataAccess.GetGrowthScaleRatio(data);
		maxDamageRatio = RuneDataAccess.GetGrowthDamageRatio(data);
	}

	public void UpdateState()
	{
		float progress = GetGrowthProgress();
		float scaleRatio = Mathf.Lerp(1f, maxScaleRatio, progress);
		float damageRatio = Mathf.Lerp(1f, maxDamageRatio, progress);

		// 시각·히트박스는 transform 스케일만 맞춥니다. 콜라이더 로컬 크기까지 키우면 이중 확대됩니다.
		transform.localScale = baseScale * scaleRatio;
		weapon.damage = baseDamage * damageRatio;
	}

	float GetGrowthProgress()
	{
		if (parentMotion == null || weapon == null)
			return 0f;

		float growthDuration = Mathf.Max(0.01f, maxGrowthTime);
		float totalLife = Mathf.Max(0.01f, weapon.spawntime);
		float elapsed = totalLife - parentMotion.GetRemainingLife();
		return Mathf.Clamp01(elapsed / growthDuration);
	}
}
