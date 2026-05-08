using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 선택한 룬 슬롯 3개를 관리한다.
/// Weapon이 Fire() 시 GetActiveRunes()로 현재 룬 구성을 가져간다.
/// </summary>
public class RuneManager : MonoBehaviour
{
    [Header("# 테스트용 초기 룬")]
[SerializeField] RuneData testRune0;
[SerializeField] RuneData testRune1;
[SerializeField] RuneData testRune2;
    public static RuneManager instance;

    // 슬롯은 항상 3개, 비어있으면 null
    const int SlotCount = 3;
    RuneData[] slots = new RuneData[SlotCount];

    // 마지막 검증 결과 캐시 (UI 경고 표시용)
    public bool IsCurrentCombinationValid { get; private set; } = true;
    public string CurrentWarningMessage   { get; private set; } = string.Empty;

 void Awake()
{
    // 싱글톤 등록 (이게 빠져있었음!)
    if (instance != null && instance != this) { Destroy(gameObject); return; }
    instance = this;

    // 테스트용 초기 룬 자동 장착
    if (testRune0 != null) SetRune(0, testRune0);
    if (testRune1 != null) SetRune(1, testRune1);
    if (testRune2 != null) SetRune(2, testRune2);
}
    // ─────────────────────────────────────────────────────
    // 슬롯 조작
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// 특정 슬롯에 룬을 등록한다. 중복이면 자동으로 기존 슬롯을 지운다.
    /// </summary>
    public void SetRune(int slotIndex, RuneData data)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return;

        // 같은 룬이 다른 슬롯에 이미 있으면 제거 (중복 불가)
        if (data != null)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (i != slotIndex && slots[i] == data)
                    slots[i] = null;
            }
        }

        slots[slotIndex] = data;
        Validate();
    }

    /// <summary>두 슬롯의 순서를 교환한다.</summary>
    public void SwapSlots(int a, int b)
    {
        if (a < 0 || a >= SlotCount || b < 0 || b >= SlotCount) return;
        (slots[a], slots[b]) = (slots[b], slots[a]);
        Validate();
    }

    /// <summary>슬롯을 비운다.</summary>
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return;
        slots[slotIndex] = null;
        Validate();
    }

    /// <summary>모든 슬롯을 비운다.</summary>
    public void ClearAll()
    {
        for (int i = 0; i < SlotCount; i++) slots[i] = null;
        Validate();
    }

    // ─────────────────────────────────────────────────────
    // Weapon이 호출하는 접근자
    // ─────────────────────────────────────────────────────
    /// <summary>
    /// 현재 슬롯을 null 제거 후 순서대로 반환한다.
    /// Weapon.Fire()에서 이 리스트를 BulletRune.Init()에 전달한다.
    /// </summary>
    public List<RuneData> GetActiveRunes()
    {
        var result = new List<RuneData>();
        foreach (var slot in slots)
            if (slot != null) result.Add(slot);
        return result;
    }

    // ─────────────────────────────────────────────────────
    // 검증
    // ─────────────────────────────────────────────────────
    void Validate()
    {
        var active = GetActiveRunes();
        IsCurrentCombinationValid = RuneValidator.IsValidCombination(active);
        CurrentWarningMessage     = RuneValidator.GetWarningMessage(active);
    }

    // ─────────────────────────────────────────────────────
    // 쿨다운 보정값 계산 (Weapon에서 사용)
    // ─────────────────────────────────────────────────────
    /// <summary>장착된 룬들의 쿨다운 페널티 합산값을 반환한다.</summary>
    public float GetTotalCooldownPenalty()
    {
        float total = 0f;
        foreach (var slot in slots)
            if (slot != null) total += slot.cooldownPenalty;
        return total;
    }

    // ─────────────────────────────────────────────────────
    // 디버그용
    // ─────────────────────────────────────────────────────
    public RuneData GetSlot(int i) => (i >= 0 && i < SlotCount) ? slots[i] : null;
    public int SlotCount_ => SlotCount;
}
