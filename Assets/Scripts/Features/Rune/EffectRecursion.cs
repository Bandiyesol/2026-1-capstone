using System.Collections.Generic;
using UnityEngine;

public class EffectRecursion : RuneEffect, IFinalEffect
{
	public void OnFinalExecute()
	{
		if (weapon == null || parentMotion == null || weapon.isRevived)
			return;

		GameObject prefab = WeaponManager.Instance != null
			? WeaponManager.Instance.GetMotionPrefab(weapon.info.motionId)
			: null;
		if (prefab == null)
			return;

		WeaponInstance revived = new WeaponInstance(weapon) { isRevived = true };
		if (data != null && data.power > 0f)
			revived.damage *= data.power;

		List<RuneData> childRunes = parentMotion.GetRunes();
		childRunes.RemoveAll(r => r == null || r.runeType == RuneType.Recursion);

		Motion childMotion = PoolManager.Instance != null
			? PoolManager.Instance.SpawnMotion(weapon.info.motionId, transform.position, transform.rotation, activateImmediately: false)
			: null;

		if (childMotion == null)
		{
			GameObject clone = Instantiate(prefab, transform.position, transform.rotation);
			clone.SetActive(false);
			childMotion = clone.GetComponent<Motion>();
		}

		if (childMotion != null)
		{
			childMotion.Initialize(revived, childRunes);
			childMotion.gameObject.SetActive(true);
		}
	}
}