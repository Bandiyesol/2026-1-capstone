using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 획득한 악세서리. 획득·드롭 로직은 추후 연동. 준비 전에는 Player에 붙이지 않아도 됩니다.
/// </summary>
[CreateAssetMenu(fileName = "Accessory", menuName = "Scriptable Object/AccessoryData")]
public class AccessoryData : ScriptableObject
{
	public string accessoryId;
	public string displayName;
	public string grade = "Common";
	public string accessoryType;
	public Sprite icon;
	[TextArea] public string description;
	public float statA;
	public float statB;
}

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
		if (data == null)
			return false;

		if (accessories.Count >= maxAccessories)
		{
			Debug.LogWarning($"[AccessoryInventory] 악세서리 상한({maxAccessories})에 도달했습니다.");
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
