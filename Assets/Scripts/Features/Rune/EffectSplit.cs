using System.Collections.Generic;
using UnityEngine;

public class EffectSplit : RuneEffect, ITriggerEffect
{
	public bool DestroyOnExecute => data != null && data.isDestroyed;
	public bool ProtectParent => false;

	static readonly List<RuneData> s_EmptyChildRunes = new List<RuneData>();

	SplitRuneData SplitData => data as SplitRuneData;

	bool hasSplit;
	List<RuneData> cachedChildRunes;

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		hasSplit = false;
		cachedChildRunes = BuildSplitChildRunes(parentMotion != null ? parentMotion.GetRunes() : null);
	}

	void Update() => UpdateCooltime();

	public void OnReflect(Collider2D collision)
	{
		if (weapon.isSplitChild || hasSplit || !isReady || !TryGetDamageable(collision, out _))
			return;

		// 근접 무기는 분열 미지원
		if (weapon?.info?.type is "Sword" or "Hammer" or "Sickle" or "Whip")
			return;

		int requestedSpawns = RuneDataAccess.GetSpawnsPerTrigger(data);
		if (requestedSpawns <= 0)
			return;

		int spawnBudget = PoolManager.Instance != null
			? PoolManager.Instance.GetRemainingMotionBudget()
			: requestedSpawns;
		int spawns = Mathf.Min(requestedSpawns, spawnBudget);
		if (spawns <= 0)
			return;

		hasSplit = true;

		float baseZ = transform.eulerAngles.z;
		float spread = SplitData != null && SplitData.spreadDegrees > 0f ? SplitData.spreadDegrees : 30f;

		for (int i = 0; i < spawns; i++)
			SpawnChild(SymmetricAngle(baseZ, spread, i, spawns), cachedChildRunes ?? s_EmptyChildRunes);

		ResetCooltime();
	}

	/// <summary>Split 이후 슬롯 중 Trigger 룬만 상속 — 분열체에 중력/유도 등을 붙이지 않아 부하를 줄입니다.</summary>
	public static List<RuneData> BuildSplitChildRunes(IReadOnlyList<RuneData> runes)
	{
		List<RuneData> afterSplit = GetRunesAfterSplit(runes);
		if (afterSplit.Count == 0)
			return afterSplit;

		var filtered = new List<RuneData>(afterSplit.Count);
		for (int i = 0; i < afterSplit.Count; i++)
		{
			RuneData rune = afterSplit[i];
			if (rune != null && rune.category == RuneCategory.Trigger)
				filtered.Add(rune);
		}

		return filtered;
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
		if (PoolManager.Instance != null && !PoolManager.Instance.CanSpawnMotion(1))
			return;

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
			if (PoolManager.Instance != null && !PoolManager.Instance.CanSpawnMotion(1))
				return;

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
