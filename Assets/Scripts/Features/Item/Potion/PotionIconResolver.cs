using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 물약 UI 아이콘 — SO.icon 우선, 없으면 Resources/Sprites/Potion/{spriteName} (무기·악세와 동일 패턴).
/// </summary>
public static class PotionIconResolver
{
	const string ResourcesFolder = "Sprites/Potion";

	static readonly Dictionary<PotionType, string> SpriteNameByType = new Dictionary<PotionType, string>
	{
		{ PotionType.HealthRestore, "potion_078" },
		{ PotionType.AttackBuff, "potion_334" },
		{ PotionType.DefenseBuff, "potion_270" },
		{ PotionType.SpeedBuff, "potion_142" },
		{ PotionType.RuneBuff, "potion_206" },
	};

	static readonly Dictionary<PotionType, Sprite> Cache = new Dictionary<PotionType, Sprite>();
	static bool catalogIndexed;

	public static Sprite Resolve(PotionData data)
	{
		if (data == null)
			return null;

		if (data.icon != null)
			return data.icon;

		return Resolve(data.potionType);
	}

	public static Sprite Resolve(PotionType type)
	{
		EnsureCatalogIndex();

		if (Cache.TryGetValue(type, out Sprite cached))
			return cached;

		if (!SpriteNameByType.TryGetValue(type, out string spriteName))
			return null;

		Sprite loaded = LoadFromResources(spriteName);
		if (loaded != null)
			Cache[type] = loaded;

		return loaded;
	}

	public static Sprite ResolveFromId(string potionId)
	{
		if (string.IsNullOrEmpty(potionId))
			return null;

		if (System.Enum.TryParse(potionId, out PotionType type))
			return Resolve(type);

		return null;
	}

	static void EnsureCatalogIndex()
	{
		if (catalogIndexed)
			return;

		catalogIndexed = true;

		PotionData[] fromResources = Resources.LoadAll<PotionData>("Data/Potion");
		if (fromResources == null)
			return;

		foreach (PotionData entry in fromResources)
		{
			if (entry == null || entry.icon == null)
				continue;

			Cache[entry.potionType] = entry.icon;
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
