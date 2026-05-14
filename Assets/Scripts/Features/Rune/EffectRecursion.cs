using System.Collections.Generic;
using UnityEngine;

public class EffectRecursion : RuneEffect
{
	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);

		if (!instance.isRecursion)
		{
			instance.isRecursion = true;

			GameObject clone = Instantiate(parentMotion.gameObject, parentMotion.transform.position, parentMotion.transform.rotation);
			Motion cloneMotion = clone.GetComponent<Motion>();
			
			List<RuneData> remainingRunes = parentMotion.GetRemainingRunes();
			cloneMotion.Initialize(instance, remainingRunes);
		}

		Destroy(parentMotion.gameObject);
	}
}