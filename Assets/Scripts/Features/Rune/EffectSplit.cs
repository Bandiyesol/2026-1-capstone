using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EffectSplit : RuneEffect, ITriggerEffect
{
	public bool DestroyOnExecute => data.isDestroyed;
	public bool ProtectParent => false;

	SplitRuneData SplitData => data as SplitRuneData;


	private void Update() => UpdateCooltime();


	public void OnReflect(Collider2D collision)
	{
		if (weapon.isSplited || !isReady) return;

		int spawns = RuneDataAccess.GetSpawnsPerTrigger(data);
		if (spawns <= 0) return;

		List<RuneData> childRunes = GetRunesAfterSplit(parentMotion.GetRunes());
		float baseZ = transform.eulerAngles.z;
		float spread = SplitData != null && SplitData.spreadDegrees > 0f ? SplitData.spreadDegrees : 30f;

		for (int i = 0; i < spawns; i++)
			SpawnChild(SymmetricAngle(baseZ, spread, i, spawns), childRunes);

		ResetCooltime();
	}


	/// <summary>분열 룬 슬롯 뒤에 있는 룬만 자식이 물려받음.</summary>
	public static List<RuneData> GetRunesAfterSplit(IReadOnlyList<RuneData> runes)
	{
		if (runes == null) return new List<RuneData>();

		int splitIdx = -1;
		for (int i = 0; i < runes.Count; i++)
		{
			if (runes[i] != null && runes[i].runeType == RuneType.Split)
			{
				splitIdx = i;
				break;
			}
		}

		if (splitIdx < 0) return new List<RuneData>();

		var list = new List<RuneData>();
		for (int i = splitIdx + 1; i < runes.Count; i++)
		{
			if (runes[i] != null) list.Add(runes[i]);
		}
		return list;
	}


	static float SymmetricAngle(float baseZ, float totalSpread, int index, int count)
	{
		if (count <= 1) return baseZ;
		float t = (float)index / (count - 1);
		return baseZ - totalSpread * 0.5f + totalSpread * t;
	}


	void SpawnChild(float angleZ, List<RuneData> childRunes)
	{
		WeaponInstance childInstance = new WeaponInstance(weapon) { isSplited = true };
		childInstance.damage *= data.power > 0 ? data.power : 0.5f;

		GameObject prefab = WeaponManager.Instance.GetMotionPrefab(weapon.info.motionId);
		GameObject clone = Instantiate(prefab, transform.position, Quaternion.Euler(0f, 0f, angleZ));
		clone.GetComponent<Motion>().Initialize(childInstance, childRunes, parentMotion.GetRemainingLife());
	}
}
