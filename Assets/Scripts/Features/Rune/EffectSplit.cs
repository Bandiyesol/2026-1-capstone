using System.Collections.Generic;
using UnityEngine;

public class EffectSplit : RuneEffect, ITriggerEffect
{
	public bool DestroyOnExecute => data != null && data.isDestroyed;
	public bool ProtectParent => false;

	SplitRuneData SplitData => data as SplitRuneData;

	void Update() => UpdateCooltime();

	public void OnReflect(Collider2D collision)
	{
		if (weapon.isSplitChild || !isReady || !TryGetDamageable(collision, out _))
			return;

		// 근접 무기는 분열 미지원
		if (weapon?.info?.type is "Sword" or "Hammer" or "Sickle" or "Whip")
			return;

		int spawns = RuneDataAccess.GetSpawnsPerTrigger(data);
		if (spawns <= 0) return;

		List<RuneData> childRunes = GetRunesAfterSplit(parentMotion.GetRunes());
		float baseZ = transform.eulerAngles.z;
		float spread = SplitData != null && SplitData.spreadDegrees > 0f ? SplitData.spreadDegrees : 30f;

		for (int i = 0; i < spawns; i++)
			SpawnChild(SymmetricAngle(baseZ, spread, i, spawns), childRunes);

		ResetCooltime();
	}

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
		WeaponInstance childInstance = new WeaponInstance(weapon) { isSplitChild = true };
		childInstance.damage *= data.power > 0 ? data.power : 0.5f;

		Vector3 spawnPos = transform.position + transform.right * 0.25f;
		GameObject prefab = WeaponManager.Instance != null
			? WeaponManager.Instance.GetMotionPrefab(weapon.info.motionId)
			: null;

		Motion childMotion = PoolManager.Instance != null
			? PoolManager.Instance.SpawnMotion(weapon.info.motionId, spawnPos, Quaternion.Euler(0f, 0f, angleZ), activateImmediately: false)
			: null;

		if (childMotion == null && prefab != null)
		{
			GameObject clone = Object.Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 0f, angleZ));
			clone.SetActive(false);
			childMotion = clone.GetComponent<Motion>();
		}

		if (childMotion == null)
			return;

		childMotion.Initialize(childInstance, childRunes, parentMotion.GetRemainingLife());
		childMotion.gameObject.SetActive(true);
	}
}
