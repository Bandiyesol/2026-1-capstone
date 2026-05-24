using System.Collections.Generic;
using UnityEngine;

public class EffectRecursion : RuneEffect, IFinalEffect
{
	public void OnFinalExecute()
	{
		if (weapon.isRevived) return;

		WeaponInstance again = new WeaponInstance(weapon){ isRevived = true };

		List<RuneData> parentRunes = parentMotion.GetRunes();
		List<RuneData> childRunes = new List<RuneData>();

		foreach (var r in parentRunes)
		{
			if (r.runeType != RuneType.Recursion) childRunes.Add(r);
		}

		GameObject prefab = WeaponManager.Instance.GetMotionPrefab(weapon.info.motionId);
		if (prefab == null) return;

		GameObject clone = Instantiate(prefab, transform.position, transform.rotation);

		clone.GetComponent<Motion>().Initialize(again, childRunes);
	}
}