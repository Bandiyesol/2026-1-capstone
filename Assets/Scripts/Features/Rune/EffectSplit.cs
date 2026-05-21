using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class EffectSplit : RuneEffect, ITriggerEffect
{
	public bool DestroyOnExecute => data.isDestroyed;
	public bool ProtectParent => false;


	private void Update() => UpdateCooltime();


	public void OnReflect(Collider2D collision)
	{
		if (weapon.isSplited || !isReady) return;
		int splitCount = RuneDataAccess.GetSplitCount(data);
		if (splitCount <= 0) return;

		for (int i = 0; i < splitCount; i++) SpawnChild();

		ResetCooltime();
	}


	private void SpawnChild()
	{
		WeaponInstance childInstance = new WeaponInstance(weapon){ isSplited = true };
		childInstance.damage *= data.power > 0 ? data.power : 0.5f;
		
		List<RuneData> runes = parentMotion.GetRunes().Where(r => r.runeType != RuneType.Split).ToList();
		GameObject prefab = WeaponManager.Instance.GetMotionPrefab(weapon.info.motionId);
		float randomAngle = Random.Range(0f, 360f);

		GameObject clone = Instantiate(prefab, transform.position, Quaternion.Euler(0, 0, randomAngle));
		clone.GetComponent<Motion>().Initialize(childInstance, runes, parentMotion.GetRemainingLife());
	}
}