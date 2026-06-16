using System.Collections.Generic;
using UnityEngine;

public class RuneManager : MonoBehaviour
{
    public static RuneManager instance;

    public bool IsCurrentCombinationValid { get; private set; } = true;
    public string CurrentWarningMessage { get; private set; } = string.Empty;

    /// <summary>
    /// 룬 쿨타임 배율. 기본값 1f.
    /// 특수 물약 사용 시 PotionEffect가 0.5f로 설정 → 쿨타임 절반.
    /// </summary>
    public float CooldownMultiplier { get; set; } = 1f;

    const int BaseSlotCount = 3;
    const int MaxSlotCount  = 6;   // 고대 문양 최대 3개 중첩

    // ExtraRuneSlot 악세사리로 동적 확장 가능
    int extraSlots = 0;
    int SlotCount  => Mathf.Min(BaseSlotCount + extraSlots, MaxSlotCount);

    RuneData[] slots = new RuneData[MaxSlotCount];
    readonly List<RuneData> activeRunesCache = new List<RuneData>();

    [SerializeField] RuneData[] initialRunes = new RuneData[3];

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        for (int i = 0; i < initialRunes.Length; i++)
        {
            if (initialRunes[i] != null)
                SetRune(i, initialRunes[i]);
        }
    }

    public void SetRune(int slotIndex, RuneData data)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return;

        int finalSlot = SlotCount - 1;

        // Final(Recursion) 룬은 항상 마지막 슬롯 — 기존 룬은 다른 빈 칸으로 이동
        if (data != null && data.category == RuneCategory.Final)
        {
            RuneData displaced = slots[finalSlot];
            if (displaced != null && displaced != data)
            {
                int emptySlot = FindFirstEmptySlot(finalSlot);
                if (emptySlot >= 0)
                    slots[emptySlot] = displaced;
            }

            slotIndex = finalSlot;
        }

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

    public void SwapSlots(int a, int b)
    {
        if (a < 0 || a >= SlotCount || b < 0 || b >= SlotCount) return;
        if (IsFinalSlotLocked(a) || IsFinalSlotLocked(b)) return;
        (slots[a], slots[b]) = (slots[b], slots[a]);
        Validate();
    }

    /// <summary>Final 룬이 장착된 마지막 슬롯은 순서 변경 불가</summary>
    public bool IsFinalSlotLocked(int slotIndex)
    {
        int finalSlot = SlotCount - 1;
        if (slotIndex != finalSlot)
            return false;

        RuneData rune = slots[finalSlot];
        return rune != null && rune.category == RuneCategory.Final;
    }

    int FindFirstEmptySlot(int excludeIndex = -1)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (i == excludeIndex)
                continue;

            if (slots[i] == null)
                return i;
        }

        return -1;
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return;
        slots[slotIndex] = null;
        Validate();
    }

	public void ClearAll()
	{
		for (int i = 0; i < MaxSlotCount; i++)
			slots[i] = null;

		extraSlots = 0;
		CooldownMultiplier = 1f;
		Validate();
	}

	public bool ContainsRune(RuneData rune)
	{
		if (rune == null)
			return false;

		for (int i = 0; i < MaxSlotCount; i++)
		{
			if (slots[i] == rune)
				return true;
		}

		return false;
	}

    public void ResetToInitial()
    {
        ClearAll();

        if (initialRunes == null)
            return;

        for (int i = 0; i < initialRunes.Length && i < SlotCount; i++)
        {
            if (initialRunes[i] != null)
                SetRune(i, initialRunes[i]);
        }
    }

    public List<RuneData> GetActiveRunes()
    {
        activeRunesCache.Clear();
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null)
                activeRunesCache.Add(slots[i]);
        }

        return activeRunesCache;
    }

    void Validate()
    {
        if (!RuneValidator.ValidateSlots(slots, out string slotError))
        {
            IsCurrentCombinationValid = false;
            CurrentWarningMessage = slotError;
            return;
        }

        var active = GetActiveRunes();
        IsCurrentCombinationValid = RuneValidator.IsValidCombination(active, out string comboError);
        CurrentWarningMessage = IsCurrentCombinationValid
            ? string.Empty
            : (string.IsNullOrEmpty(comboError)
                ? RuneValidator.GetWarningMessage(active)
                : comboError);

        if (!IsCurrentCombinationValid)
            Debug.LogWarning($"[RuneManager] {CurrentWarningMessage}");
    }

    public RuneData GetSlot(int i) => (i >= 0 && i < SlotCount) ? slots[i] : null;
    public int SlotCount_ => SlotCount;

    /// <summary>[악세사리] 룬 슬롯 1개 추가. 최대 MaxSlotCount까지.</summary>
    public void AddExtraSlot()
    {
        if (BaseSlotCount + extraSlots >= MaxSlotCount) return;
        extraSlots++;
        Debug.Log($"[RuneManager] 룬 슬롯 확장 → {SlotCount}칸");
    }

    public int GetFilledSlotCount()
    {
        int count = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null)
                count++;
        }

        return count;
    }

    public bool IsFull => GetFilledSlotCount() >= SlotCount;

    /// <summary>첫 빈 슬롯에 룬을 추가합니다. Final 룬은 마지막 슬롯에 고정됩니다.</summary>
    public bool TryAddRune(RuneData data)
    {
        if (data == null)
            return false;

        if (data.category == RuneCategory.Final)
        {
            SetRune(SlotCount - 1, data);
            return true;
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i] != null)
                continue;

            SetRune(i, data);
            return true;
        }

        return false;
    }
}