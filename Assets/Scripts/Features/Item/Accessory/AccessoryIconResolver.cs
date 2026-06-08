using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 악세사리 UI 아이콘 — SO.icon 우선, 없으면 Resources/Sprites/Accessory/{assetName} (무기와 동일 패턴).
/// </summary>
public static class AccessoryIconResolver
{
	const string ResourcesFolder = "Sprites/Accessory";

	static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
	static bool catalogIndexed;

	public static Sprite Resolve(AccessoryData data)
	{
		if (data == null)
			return null;

		if (data.icon != null)
			return data.icon;

		EnsureCatalogIndex();

		if (Cache.TryGetValue(data.name, out Sprite cached))
			return cached;

		Sprite loaded = LoadFromResources(data.name);
		if (loaded != null)
			Cache[data.name] = loaded;

		return loaded;
	}

	static void EnsureCatalogIndex()
	{
		if (catalogIndexed)
			return;

		catalogIndexed = true;

		RewardCatalogSettings catalog = RewardCatalogSettings.Load();
		if (catalog?.allAccessories == null)
			return;

		foreach (AccessoryData entry in catalog.allAccessories)
		{
			if (entry == null || string.IsNullOrEmpty(entry.name))
				continue;

			if (entry.icon != null)
				Cache[entry.name] = entry.icon;
		}
	}

	static Sprite LoadFromResources(string assetName)
	{
		if (string.IsNullOrEmpty(assetName))
			return null;

		string path = $"{ResourcesFolder}/{assetName}";
		Sprite sprite = Resources.Load<Sprite>(path);
		if (sprite != null)
			return sprite;

		Sprite[] subs = Resources.LoadAll<Sprite>(path);
		if (subs != null && subs.Length > 0)
			return subs[0];

		return null;
	}
}
