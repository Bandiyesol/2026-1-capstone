using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 악세사리 획득 및 PlayerStats 적용을 담당하는 싱글톤.
/// 보상 선택 시 Add(data)를 호출하면 즉시 영구 적용된다.
/// </summary>
public class AccessoryManager : MonoBehaviour
{
    public static AccessoryManager instance;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    /// <summary>
    /// 악세사리를 획득하고 PlayerStats + AccessoryInventory에 즉시 적용.
    /// 보상 선택 시 이 메서드만 호출하면 됨.
    /// </summary>
    public void Add(AccessoryData data)
    {
        if (data == null) return;
        if (PlayerStats.Instance == null)
        {
            Debug.LogWarning("[AccessoryManager] PlayerStats 인스턴스가 없습니다.");
            return;
        }

        // 스탯 적용
        foreach (StatModifier mod in data.modifiers)
        {
            if (mod.isMulti)
                PlayerStats.Instance.AddMulti(mod.statType, mod.value);
            else
                PlayerStats.Instance.AddFlat(mod.statType, mod.value);
        }

        // 인벤토리 추가 (가방 UI 자동 갱신)
        if (AccessoryInventory.Instance != null)
            AccessoryInventory.Instance.TryAdd(data);
        else
            Debug.LogWarning("[AccessoryManager] AccessoryInventory.Instance가 null입니다. AccessoryInventory 컴포넌트가 활성화된 오브젝트에 붙어있는지 확인하세요.");

        // 특수 효과 활성화
        if (data.effectType != AccessoryEffectType.None)
            AccessoryEffect.instance?.Activate(data.effectType, data);

        Debug.Log($"[AccessoryManager] 악세사리 획득: {data.displayName} ({data.grade})");
    }
}