using System.Collections.Generic;
using UnityEngine;


public class EffectSplit : RuneEffect
{
	private void Start() => Split();


	private void Split()
	{
		if (parentMotion == null) return;

		List<RuneData> remainingRunes = parentMotion.GetRemainingRunes();
		WeaponInstance weaponInstance = parentMotion.instance;
		
		float[] angles = {-30f, 30f};
		foreach (float angle in angles)
		{
			Quaternion splitRotation = transform.rotation * Quaternion.Euler(0, 0, angle);
			GameObject clone = Instantiate(gameObject, transform.position, splitRotation);
			Motion cloneMotion = clone.GetComponent<Motion>();
			
			if (cloneMotion != null) cloneMotion.Initialize(weaponInstance, remainingRunes);
		}
	}
}