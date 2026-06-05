using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 획득한 악세사리 소지 목록.
/// AccessoryManager.Add()에서 자동 호출됨.
/// OnInventoryChanged 이벤트로 InventoryUI 자동 갱신.
/// </summary>
public class AccessoryInventory : MonoBehaviour
{
    public static AccessoryInventory Instance { get; private set; }

    [SerializeField] int maxAccessories = 12;

    readonly List<AccessoryData> accessories = new List<AccessoryData>();

    public IReadOnlyList<AccessoryData> Accessories => accessories;
    public int MaxAccessories => maxAccessories;
    public event Action OnInventoryChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public bool TryAdd(AccessoryData data)
    {
        if (data == null) return false;

        if (accessories.Count >= maxAccessories)
        {
            Debug.LogWarning($"[AccessoryInventory] 악세사리 상한({maxAccessories})에 도달했습니다.");
            return false;
        }

        accessories.Add(data);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        accessories.Clear();
        OnInventoryChanged?.Invoke();
    }
}
