using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 획득해서 보유하고 있는 무기 리스트(WeaponInstance)를 관리합니다.
/// 게임 중 선택하여 고정된 스탯을 지닌 무기들이 여기에 담겨 WeaponController에서 쿨타임 관리를 받습니다.
/// </summary>
public class WeaponInventory : MonoBehaviour
{
	[SerializeField] int maxWeapons = 6;

	readonly List<WeaponInstance> weapons = new List<WeaponInstance>();

	public IReadOnlyList<WeaponInstance> Weapons => weapons;

	public int MaxWeapons => maxWeapons;

	public event Action OnInventoryChanged;

	public bool TryAdd(WeaponInstance instance)
	{
		if (instance == null) return false;

		if (weapons.Count >= maxWeapons)
		{
			Debug.LogWarning($"[PlayerWeaponInventory] 무기 상한({maxWeapons})에 도달했습니다.");
			return false;
		}

		weapons.Add(instance);
		OnInventoryChanged?.Invoke();
		return true;
	}

	public void Clear()
	{
		weapons.Clear();
		OnInventoryChanged?.Invoke();
	}
}
