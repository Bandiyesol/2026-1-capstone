using System.Collections.Generic;
using UnityEngine;

// 장착된 룬 조합이 시스템적으로 안전한지(무한 루프, 논리 충돌 등) 검증하는 클래스
public static class RuneValidator
{
    // 무기에 뚫려있는 최대 룬 슬롯 개수
    public const int MaxSlots = 3;

    // 절대 같이 장착할 수 없는 '치명적 비호환' 룬 쌍 딕셔너리
    static readonly Dictionary<RuneType, RuneType[]> hardIncompatible = new()
    {
        { RuneType.Split, new[] { RuneType.Return } },       // 분열 후 귀환 시 생성물 과부하 우려
        { RuneType.Orbit, new[] { RuneType.Homing } },       // 궤도를 돌아야 하는데 유도되면 움직임 충돌
        { RuneType.Recursion, new[] { RuneType.Recursion } },// 재귀가 재귀를 낳으면 무한 증식 에러 발생
        { RuneType.Delay, new[] { RuneType.Blink } },        // 지연과 점멸 동시 사용 시 위치 계산 꼬임
        { RuneType.Gravity, new[] { RuneType.Wave } },       // 중력과 파동 기동의 물리적 계산 충돌
    };

    // 장착 '순서'가 잘못되면 안 되는 룬 쌍 (A → B 순서로 와야지, B → A 로 오면 안 됨)
    static readonly (RuneType first, RuneType second)[] orderIncompatible =
    {
        (RuneType.Explode, RuneType.Homing), // 폭발이 유도보다 먼저 계산되면 안 됨
        (RuneType.Freeze, RuneType.Chain),   // 빙결이 연쇄 번개보다 먼저 계산되면 안 됨
    };

    // UI 장착 창 등에서 빈 슬롯(null)을 포함한 전체 슬롯 배열을 검사할 때 사용
    public static bool ValidateSlots(IReadOnlyList<RuneData> slots, out string errorMsg)
    {
        errorMsg = string.Empty;
        if (slots == null || slots.Count == 0) return true;

        var seen = new HashSet<RuneType>(); // 중복 장착을 막기 위한 해시셋
        int lastFilledIndex = -1;           // 룬이 채워진 마지막 슬롯의 인덱스 추적용
        int finalCount = 0;                 // 장착된 Final(최종형) 룬의 개수
        int finalIndex = -1;                // Final 룬이 꽂힌 슬롯 번호

        for (int i = 0; i < slots.Count; i++)
        {
            RuneData r = slots[i];
            if (r == null) continue; // 빈 슬롯은 건너뜀

            lastFilledIndex = i; // 가장 마지막으로 발견된 룬의 위치 업데이트

            // HashSet에 추가를 실패했다는 건 이미 같은 룬이 있다는 뜻 (중복 에러)
            if (!seen.Add(r.runeType))
            {
                errorMsg = $"[RuneValidator] 중복 룬: {r.runeType}";
                return false;
            }

            // 룬 카테고리가 Final이면 개수와 위치를 기록
            if (r.category == RuneCategory.Final)
            {
                finalCount++;
                finalIndex = i;
            }
        }

        // 1. Final 카테고리 룬은 무기당 오직 1개만 허용
        if (finalCount > 1)
        {
            errorMsg = "[RuneValidator] Final 룬은 1개만 장착할 수 있습니다.";
            return false;
        }

        // 2. Final 룬은 장착된 룬들 중 무조건 '가장 마지막 슬롯'에 배치되어야 함
        if (finalCount == 1 && finalIndex != lastFilledIndex)
        {
            errorMsg = "[RuneValidator] Final 룬은 마지막 슬롯에만 장착할 수 있습니다.";
            return false;
        }

        return true;
    }

    // 에러 메시지 반환 없이 유효성 여부(true/false)만 빠르게 확인할 때 쓰는 오버로딩 함수
    public static bool IsValidCombination(List<RuneData> runeList)
    {
        return IsValidCombination(runeList, out _);
    }

    // 실제 발사 직전이나 데이터 로드 시 빈칸 없는 순수 룬 리스트를 검증하는 메인 함수
    public static bool IsValidCombination(List<RuneData> runeList, out string errorMsg)
    {
        errorMsg = string.Empty;
        if (runeList == null || runeList.Count == 0) return true;

        var seen = new HashSet<RuneType>();
        int finalCount = 0;

        // [기본 검사] 중복 장착 및 Final 룬 개수 제한 검사
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

        // [데이터 호환성 검사] RuneData (ScriptableObject) 안에 기입된 비호환 리스트 체크
        for (int i = 0; i < runeList.Count; i++)
        {
            if (runeList[i] == null) continue;
            if (runeList[i].incompatibleWith == null) continue; // 설정된 비호환 룬이 없으면 패스

            // 현재 룬(i)의 비호환 목록에 있는 룬(bad)이 리스트 안에 같이 있는지 이중 for문으로 검사
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

        // [하드코딩 호환성 검사] 클래스 상단에 정의해둔 치명적 충돌 딕셔너리(hardIncompatible) 체크
        for (int i = 0; i < runeList.Count; i++)
        {
            if (runeList[i] == null) continue;
            // 딕셔너리에 현재 룬이 등록되어 있지 않으면 패스
            if (!hardIncompatible.TryGetValue(runeList[i].runeType, out var bads)) continue;

            // 등록된 비호환 룬이 같이 장착되어 있는지 검사
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

        // [장착 순서 검사] orderIncompatible에 명시된 순서를 어겼는지 확인
        var types = new List<RuneType>();
        foreach (var r in runeList)
            if (r != null) types.Add(r.runeType); // 검사를 쉽게 하기 위해 타입만 추출

        foreach (var (first, second) in orderIncompatible)
        {
            int fi = types.IndexOf(first);
            int si = types.IndexOf(second);

            // 두 룬이 모두 장착되어 있고(인덱스가 0 이상), first가 second보다 앞쪽(작은 인덱스)에 있을 때 에러 처리
            if (fi >= 0 && si >= 0 && fi < si)
            {
                errorMsg = $"[RuneValidator] 순서 비호환: {first} → {second}";
                return false;
            }
        }

        return true; // 모든 검증 통과 시 안전함!
    }

    // 검증 실패 시 플레이어나 디버그 로그에 보여줄 경고 메시지 출력용 래퍼 함수
    public static string GetWarningMessage(List<RuneData> runeList)
    {
        if (IsValidCombination(runeList, out _))
            return string.Empty;

        return "⚠ 이 조합은 런타임 에러를 발생시킵니다. 탄환이 즉시 소멸됩니다.";
    }
}