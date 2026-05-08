using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 룬 조합의 유효성을 검사한다.
/// "잘못된 조합 → 탄환이 즉시 사라지는 런타임 에러" 연출의 핵심.
/// </summary>
public static class RuneValidator
{
    // ─────────────────────────────────────────────────────
    // 하드코딩 비호환 규칙
    // (RuneData.incompatibleWith는 에디터용, 이 테이블은 코드 레벨 규칙)
    // ─────────────────────────────────────────────────────
    static readonly Dictionary<RuneType, RuneType[]> hardIncompatible = new()
    {
        // Split(분열)된 탄환은 폭발을 '유도'할 수 없음 → Split + Homing + Explode 순서 시 에러
        // 단순 두 룬 조합 규칙만 여기서 관리 (순서는 별도 체크)
        { RuneType.Split,     new[] { RuneType.Return } },   // 분열 후 회귀는 불가
        { RuneType.Orbit,     new[] { RuneType.Homing } },   // 공전 중 유도 불가
        { RuneType.Recursion, new[] { RuneType.Recursion } },// 재귀의 재귀 (중복 불가이지만 이중 방어)
        { RuneType.Delay,     new[] { RuneType.Blink } },    // 지연 + 점멸: 좌표 불일치로 허공 소멸
        { RuneType.Gravity,   new[] { RuneType.Wave } },     // 중력 + 파동: 이동 계산 충돌
    };

    // 순서 의존 비호환: [앞 룬][뒤 룬] 조합이 이 순서일 때만 에러
    // 예) Explode(속성)가 Split(로직) 앞에 오면 폭발을 유도할 수 없음
    static readonly (RuneType first, RuneType second)[] orderIncompatible =
    {
        (RuneType.Explode, RuneType.Homing),  // 폭발을 유도할 수 없음
        (RuneType.Freeze,  RuneType.Chain),   // 얼어붙은 적에게 연쇄 전이 불가
    };

    /// <summary>
    /// 전달된 룬 리스트가 유효한 조합인지 검사한다.
    /// false를 반환하면 BulletRune이 런타임 에러 연출을 수행한다.
    /// </summary>
    public static bool IsValidCombination(List<RuneData> runeList)
    {
        if (runeList == null || runeList.Count == 0) return true;

        // 1) 중복 검사
        var seen = new HashSet<RuneType>();
        foreach (var r in runeList)
        {
            if (r == null) continue;
            if (!seen.Add(r.runeType))
            {
                Debug.Log($"[RuneValidator] 중복 룬 감지: {r.runeType} → 런타임 에러");
                return false;
            }
        }

        // 2) RuneData.incompatibleWith 기반 검사
        for (int i = 0; i < runeList.Count; i++)
        {
            if (runeList[i] == null) continue;
            foreach (var bad in runeList[i].incompatibleWith)
            {
                for (int j = 0; j < runeList.Count; j++)
                {
                    if (i == j || runeList[j] == null) continue;
                    if (runeList[j].runeType == bad)
                    {
                        Debug.Log($"[RuneValidator] 비호환 조합: {runeList[i].runeType} + {bad} → 런타임 에러");
                        return false;
                    }
                }
            }
        }

        // 3) 하드코딩 규칙 검사
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
                        Debug.Log($"[RuneValidator] 하드 비호환: {runeList[i].runeType} + {bad} → 런타임 에러");
                        return false;
                    }
                }
            }
        }

        // 4) 순서 의존 검사
        var types = new List<RuneType>();
        foreach (var r in runeList)
            if (r != null) types.Add(r.runeType);

        foreach (var (first, second) in orderIncompatible)
        {
            int fi = types.IndexOf(first);
            int si = types.IndexOf(second);
            if (fi >= 0 && si >= 0 && fi < si)
            {
                Debug.Log($"[RuneValidator] 순서 비호환: {first} → {second} 순서 → 런타임 에러");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// UI에서 미리 보여줄 경고 메시지 (빨간 글씨 등 연출용)
    /// </summary>
    public static string GetWarningMessage(List<RuneData> runeList)
    {
        if (IsValidCombination(runeList)) return string.Empty;
        return "⚠ 이 조합은 런타임 에러를 발생시킵니다. 탄환이 즉시 소멸됩니다.";
    }
}
