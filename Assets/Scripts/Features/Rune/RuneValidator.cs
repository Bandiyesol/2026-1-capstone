using System.Collections.Generic;
using UnityEngine;

public static class RuneValidator
{
    public const int MaxSlots = 3;

    static readonly Dictionary<RuneType, RuneType[]> hardIncompatible = new()
    {
        { RuneType.Split, new[] { RuneType.Return } },
        { RuneType.Orbit, new[] { RuneType.Homing } },
        { RuneType.Recursion, new[] { RuneType.Recursion } },
        { RuneType.Delay, new[] { RuneType.Blink } },
        { RuneType.Gravity, new[] { RuneType.Wave } },
    };

    static readonly (RuneType first, RuneType second)[] orderIncompatible =
    {
        (RuneType.Explode, RuneType.Homing),
        (RuneType.Freeze, RuneType.Chain),
    };

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

    public static bool IsValidCombination(List<RuneData> runeList)
    {
        return IsValidCombination(runeList, out _);
    }

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

        for (int i = 0; i < runeList.Count; i++)
        {
            if (runeList[i] == null) continue;
            if (runeList[i].incompatibleWith == null) continue;

            foreach (var bad in runeList[i].incompatibleWith)
            {
                for (int j = 0; j < runeList.Count; j++)
                {
                    if (i == j || runeList[j] == null) continue;
                    if (runeList[j].runeType == bad)
                    {
                        errorMsg = $"[RuneValidator] 비호환 조합: {runeList[i].runeType} + {bad}";
                        return false;
                    }
                }
            }
        }

        for (int i = 0; i < runeList.Count; i++)
        {
            if (runeList[i] == null) continue;
            if (!hardIncompatible.TryGetValue(runeList[i].runeType, out var bads)) continue;

            foreach (var bad in bads)
            {
                for (int j = 0; j < runeList.Count; j++)
                {
                    if (i == j || runeList[j] == null) continue;
                    if (runeList[j].runeType == bad)
                    {
                        errorMsg = $"[RuneValidator] 비호환 조합: {runeList[i].runeType} + {bad}";
                        return false;
                    }
                }
            }
        }

        var types = new List<RuneType>();
        foreach (var r in runeList)
            if (r != null) types.Add(r.runeType);

        foreach (var (first, second) in orderIncompatible)
        {
            int fi = types.IndexOf(first);
            int si = types.IndexOf(second);
            if (fi >= 0 && si >= 0 && fi < si)
            {
                errorMsg = $"[RuneValidator] 순서 비호환: {first} → {second}";
                return false;
            }
        }

        return true;
    }

    public static string GetWarningMessage(List<RuneData> runeList)
    {
        if (IsValidCombination(runeList, out _))
            return string.Empty;

        return "⚠ 이 조합은 런타임 에러를 발생시킵니다. 탄환이 즉시 소멸됩니다.";
    }
}
