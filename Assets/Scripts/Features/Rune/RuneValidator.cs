using System.Collections.Generic;
using UnityEngine;

public static class RuneValidator
{
	public const int MaxSlots = 3;

	/// <summary>
	/// 슬롯 배열 기준 검증. Final 위치·중복만 검사 (호환/비호환 표 없음).
	/// </summary>
	public static bool ValidateSlots(IReadOnlyList<RuneData> slots, out string errorMsg)
	{
		errorMsg = string.Empty;
		if (slots == null || slots.Count == 0) return true;

		var seen = new HashSet<RuneType>();
		int lastFilledIndex = -1;
		int finalCount = 0;
		int finalIndex = -1;

		for (int i = 0; i < slots.Count; i++)
		{
			RuneData r = slots[i];
			if (r == null) continue;

			lastFilledIndex = i;

			if (!seen.Add(r.runeType))
			{
				errorMsg = $"[RuneValidator] 중복 룬: {r.runeType}";
				return false;
			}

			if (r.category == RuneCategory.Final)
			{
				finalCount++;
				finalIndex = i;
			}
		}

		if (finalCount > 1)
		{
			errorMsg = "[RuneValidator] Final 룬은 1개만 장착할 수 있습니다.";
			return false;
		}

		if (finalCount == 1 && finalIndex != lastFilledIndex)
		{
			errorMsg = "[RuneValidator] Final 룬은 마지막 슬롯에만 장착할 수 있습니다.";
			return false;
		}

		return true;
	}

	/// <summary>
	/// 공격 시 룬 리스트(슬롯 순, 빈 슬롯 제외)용. 슬롯 위치 없이 중복·Final 개수만 검사.
	/// Final 위치는 <see cref="ValidateSlots"/>에서만 검증합니다.
	/// </summary>
	public static bool IsValidCombination(List<RuneData> runeList, out string errorMsg)
	{
		errorMsg = string.Empty;
		if (runeList == null || runeList.Count == 0) return true;

		var seen = new HashSet<RuneType>();
		int finalCount = 0;

		foreach (var r in runeList)
		{
			if (r == null) continue;

			if (!seen.Add(r.runeType))
			{
				errorMsg = $"[RuneValidator] 중복 룬: {r.runeType}";
				return false;
			}

			if (r.category == RuneCategory.Final) finalCount++;
		}

		if (finalCount > 1)
		{
			errorMsg = "[RuneValidator] Final 룬은 1개만 장착할 수 있습니다.";
			return false;
		}

		return true;
	}
}
