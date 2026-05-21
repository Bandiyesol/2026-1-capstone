using System.Collections.Generic;
using UnityEngine;

public class RuneManager : MonoBehaviour
{
	public static RuneManager instance;

    [Header("# 테스트용 초기 룬")]
    public bool IsCurrentCombinationValid { get; private set; } = true;
	public string CurrentWarningMessage { get; private set; } = string.Empty;
    private const int SlotCount = 3;
    private RuneData[] slots = new RuneData[SlotCount];
	private List<RuneData> activeRunesCache = new List<RuneData>();
	[SerializeField] private RuneData[] initialRunes = new RuneData[3];



	void Awake()
	{
    	if (instance != null && instance != this) Destroy(gameObject);
		instance = this;

		for (int i = 0; i < initialRunes.Length; i++)
		{
			if (initialRunes[i] != null) SetRune(i, initialRunes[i]);
		}
	}


    // ─────────────────────────────────────────────────────
    // 슬롯 조작
    // ─────────────────────────────────────────────────────
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
        for (int i = 0; i < SlotCount; i++) slots[i] = null;
        Validate();
    }


    // ─────────────────────────────────────────────────────
    // Weapon이 호출하는 접근자
    // ─────────────────────────────────────────────────────
    public List<RuneData> GetActiveRunes()
    {
        activeRunesCache.Clear();
		for(int i = 0; i < SlotCount; i++)
		{
			if (slots[i] != null) activeRunesCache.Add(slots[i]);
		}

		return activeRunesCache;
    }


    // ─────────────────────────────────────────────────────
    // 검증
    // ─────────────────────────────────────────────────────
    void Validate()
    {
        var activeList = GetActiveRunes();
        IsCurrentCombinationValid = RuneValidator.IsValidCombination(activeList, out string errorMsg);
		CurrentWarningMessage = IsCurrentCombinationValid ? string.Empty : errorMsg;
		if (!IsCurrentCombinationValid) Debug.LogWarning($"[RuneManager] 현재 조합 위험: {errorMsg}");
    }


    // ─────────────────────────────────────────────────────
    // 디버그용
    // ─────────────────────────────────────────────────────
    public RuneData GetSlot(int i) => (i >= 0 && i < SlotCount) ? slots[i] : null;
    public int SlotCount_ => SlotCount;
}
