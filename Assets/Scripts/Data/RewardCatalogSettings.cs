using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상자 보상(RewardRollService)용 악세사리·성물 풀.
/// Resources/Data/RewardCatalogSettings 에 두고 에디터에서 자동 갱신합니다.
/// </summary>
[CreateAssetMenu(
	fileName = "RewardCatalogSettings",
	menuName = "Scriptable/Reward Catalog Settings")]
public class RewardCatalogSettings : ScriptableObject
{
	public List<AccessoryData> allAccessories = new List<AccessoryData>();
	public List<RelicData> allRelics = new List<RelicData>();

	static RewardCatalogSettings cached;

	public static RewardCatalogSettings Load()
	{
		if (cached == null)
			cached = Resources.Load<RewardCatalogSettings>("Data/RewardCatalogSettings");

		return cached;
	}

#if UNITY_EDITOR
	public static void SetCached(RewardCatalogSettings settings)
	{
		cached = settings;
	}
#endif
}
