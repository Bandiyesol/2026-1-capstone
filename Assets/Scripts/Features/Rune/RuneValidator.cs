using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 장착 슬롯 구조(중복·Final 위치)만 검증합니다.
/// 호환/비호환은 기획상 Motion 런타임에서 처리하며, 공격을 막지 않습니다.
/// </summary>
public static class RuneValidator
{
    public const int MaxSlots = 3;

    public static bool ValidateSlots(IReadOnlyList<RuneData> slots, out string errorMsg)
    {
        errorMsg = string.Empty;
        if (slots == null || slots.Count == 0)
            return true;

        var seen = new HashSet<RuneType>();
        int lastFilledIndex = -1;
        int finalCount = 0;
        int finalIndex = -1;

        for (int i = 0; i < slots.Count; i++)
        {
            RuneData r = slots[i];
            if (r == null)
                continue;

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

    public static bool IsValidCombination(List<RuneData> runeList)
    {
        return IsValidCombination(runeList, out _);
    }

    public static bool IsValidCombination(List<RuneData> runeList, out string errorMsg)
    {
        errorMsg = string.Empty;
        if (runeList == null || runeList.Count == 0)
            return true;

        var seen = new HashSet<RuneType>();
        int finalCount = 0;

        foreach (RuneData r in runeList)
        {
            if (r == null)
                continue;

            if (!seen.Add(r.runeType))
            {
                errorMsg = $"[RuneValidator] 중복 룬: {r.runeType}";
                return false;
            }

            if (r.category == RuneCategory.Final)
                finalCount++;
        }

        if (finalCount > 1)
        {
            errorMsg = "[RuneValidator] Final 룬은 1개만 장착할 수 있습니다.";
            return false;
        }

        return true;
    }

    public static string GetWarningMessage(List<RuneData> runeList)
    {
        if (IsValidCombination(runeList, out string error))
            return string.Empty;

        return error;
    }
}
