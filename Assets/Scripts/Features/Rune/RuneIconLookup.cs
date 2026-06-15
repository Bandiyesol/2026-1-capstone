using System.Collections.Generic;
using UnityEngine;

/// <summary>룬 카탈로그에서 타입별 아이콘을 조회합니다. RuneData.runeIcon이 비어 있을 때 폴백으로 사용합니다.</summary>
public static class RuneIconLookup
{
	static Dictionary<RuneType, Sprite> byType;
	static bool loaded;

	public static Sprite GetByType(RuneType type)
	{
		if (type == RuneType.None)
			return null;

		EnsureLoaded();
		return byType.TryGetValue(type, out Sprite sprite) ? sprite : null;
	}

	static void EnsureLoaded()
	{
		if (loaded)
			return;

		loaded = true;
		byType = new Dictionary<RuneType, Sprite>();

		RuneCatalog catalog = Resources.Load<RuneCatalog>("Data/RuneCatalog");
		if (catalog?.runes == null)
			return;

		foreach (RuneData rune in catalog.runes)
		{
			if (rune == null || rune.runeType == RuneType.None || rune.runeIcon == null)
				continue;

			byType[rune.runeType] = rune.runeIcon;
		}
	}
}
