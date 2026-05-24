using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 보유한 무기 인스턴스(선택 시 수치 확정). UI 인벤토리 연동 전까지 WeaponController가 참조.
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
