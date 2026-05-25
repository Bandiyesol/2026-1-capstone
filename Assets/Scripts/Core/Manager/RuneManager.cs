using System.Collections.Generic;
using UnityEngine;

public class RuneManager : MonoBehaviour
{
    public static RuneManager instance;

    public bool IsCurrentCombinationValid { get; private set; } = true;
    public string CurrentWarningMessage { get; private set; } = string.Empty;

    const int SlotCount = 3;
    readonly RuneData[] slots = new RuneData[SlotCount];
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
        (slots[a], slots[b]) = (slots[b], slots[a]);
        Validate();
    }

    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return;
        slots[slotIndex] = null;
        Validate();
    }

    public void ClearAll()
    {
        for (int i = 0; i < SlotCount; i++)
            slots[i] = null;
        Validate();
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

    public float GetTotalCooldownPenalty()
    {
        float total = 0f;
        foreach (var slot in slots)
        {
            if (slot != null)
                total += slot.cooldownPenalty;
        }

        return total;
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
}
