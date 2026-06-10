using System.Collections.Generic;
using UnityEngine;

/// <summary>룬 선택 UI용 — 카탈로그에서 후보를 랜덤 추출합니다.</summary>
public static class RuneRewardService
{
	public static List<RuneData> RollCandidates(IReadOnlyList<RuneData> catalog, int count = 3, IReadOnlyCollection<RuneData> exclude = null)
	{
		var result = new List<RuneData>();
		if (catalog == null || catalog.Count == 0)
			return result;

		var pool = new List<RuneData>();
		foreach (RuneData rune in catalog)
		{
			if (rune == null || rune.runeType == RuneType.None)
				continue;
			if (exclude != null && ContainsRune(exclude, rune))
				continue;
			pool.Add(rune);
		}

		if (pool.Count == 0)
			return result;

		var picked = new HashSet<RuneType>();
		int safety = 0;
		while (result.Count < count && safety < 100)
		{
			safety++;
			RuneData candidate = pool[Random.Range(0, pool.Count)];
			if (!picked.Add(candidate.runeType))
				continue;
			result.Add(candidate);
		}

		return result;
	}

	static bool ContainsRune(IReadOnlyCollection<RuneData> collection, RuneData rune)
	{
		foreach (RuneData owned in collection)
		{
			if (owned == null)
				continue;
			if (owned == rune || owned.runeType == rune.runeType)
				return true;
		}

		return false;
	}

	public static string FormatTitle(RuneData rune)
	{
		if (rune == null)
			return "(없음)";
		return rune.runeName;
	}

	public static string FormatType(RuneData rune)
	{
		if (rune == null || rune.runeType == RuneType.None)
			return "(없음)";
		return rune.runeType.ToString();
	}

	public static string FormatCategory(RuneData rune)
	{
		if (rune == null)
			return "(없음)";
		return RuneCategoryDisplay.GetLabel(rune.category);
	}

	public static string FormatDetail(RuneData rune) => FormatType(rune);

	public static string FormatChoiceDetail(RuneData rune)
	{
		if (rune == null)
			return string.Empty;
		return RuneCategoryDisplay.FormatColoredCategory(rune);
	}

	/// <summary>선택 카드 Detail — RuneType(색상은 카테고리 틴트).</summary>
	public static string FormatColoredType(RuneData rune)
	{
		if (rune == null || rune.runeType == RuneType.None)
			return "(없음)";

		Color tint = RuneCategoryDisplay.GetTint(rune.category);
		string hex = ColorUtility.ToHtmlStringRGB(tint);
		return $"<color=#{hex}>{rune.runeType}</color>";
	}

	/// <summary>선택 카드 Detail — 아이콘 아래 설명 (검은색 텍스트).</summary>
	public static string FormatDescription(RuneData rune)
	{
		if (rune == null)
			return string.Empty;

		if (!string.IsNullOrWhiteSpace(rune.runeDesc))
			return rune.runeDesc.Trim();

		if (!string.IsNullOrWhiteSpace(rune.runeDescription))
			return rune.runeDescription.Trim();

		return string.Empty;
	}
}

