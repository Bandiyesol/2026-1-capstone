using System.Collections.Generic;
using UnityEngine;

public static class RuneValidator
{
	// 1. 단순 조합 불가 (순서 상관없이 같이 있으면 에러)
    private static readonly Dictionary<RuneType, RuneType[]>  IncompatiblePairs = new()
    {
        { RuneType.Orbit, new[] { RuneType.Homing } },   // 공전 중 유도 불가
        { RuneType.Gravity, new[] { RuneType.Wave } }     // 중력 + 파동: 이동 계산 충돌
    };


    // 2. 순서 의존 비호환 (앞 룬 -> 뒤 룬 순서일 때만 에러)
    private static readonly (RuneType first, RuneType second)[] OrderRules =
    {
        (RuneType.Explode, RuneType.Homing),  // 폭발을 유도할 수 없음
        (RuneType.Freeze, RuneType.Chain)  // 얼어붙은 적에게 연쇄 전이 불가
    };


    public static bool IsValidCombination(List<RuneData> runeList, out string errorMsg)
    {
		errorMsg = string.Empty;
        if (runeList == null || runeList.Count == 0) return true;

        // 1) 중복 검사
        var seen = new HashSet<RuneType>();
        foreach (var r in runeList)
        {
            if (r == null) continue;
            if (!seen.Add(r.runeType))
            {
				errorMsg = $"[RuneValidator] 중복 룬 감지: {r.runeType} → 런타임 에러";
                Debug.Log(errorMsg);
                return false;
            }
        }

        // 2) 조합 비호환 검사
        for (int i = 0; i < runeList.Count; i++)
        {
			RuneType type = runeList[i].runeType;
			if (IncompatiblePairs.TryGetValue(type, out var bads))
			{
				foreach (var bad in bads)
				{
					if (runeList.Exists(r => r != null && r.runeType == bad))
					{
						errorMsg = $"[RuneValidator] 비호환 조합: {type} + {bad} → 런타임 에러";
						Debug.Log(errorMsg);
						return false;
					}
				}
			}
        }

        // 3) 순서 비호환 검사
        foreach (var rule in OrderRules)
		{
			int firstIdx = runeList.FindIndex(rune => rune != null && rune.runeType == rule.first);
			int secondIdx = runeList.FindIndex(rune => rune != null && rune.runeType == rule.second);

			if (firstIdx != -1 && secondIdx != -1 && firstIdx < secondIdx)
			{
				errorMsg = $"[RuneValidator] 순서 비호환 조합: {rule.first} → {rule.second} → 런타임 에러";
				Debug.Log(errorMsg);
				return false;
			}
		}

        return true;
    }
}
