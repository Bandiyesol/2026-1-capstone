using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 획득해서 보유하고 있는 무기 리스트(WeaponInstance)를 관리합니다.
/// 게임 중 선택하여 고정된 스탯을 지닌 무기들이 여기에 담겨 WeaponController에서 쿨타임 관리를 받습니다.
/// </summary>
public class WeaponInventory : MonoBehaviour
{
	// 플레이어가 가질 수 있는 최대 무기 슬롯 개수 제한
	[SerializeField] int maxWeapons = 6;

	// 인벤토리에 들어있는 무기 인스턴스를 담는 리스트
	readonly List<WeaponInstance> weapons = new List<WeaponInstance>();

	// 외부에서 무기 리스트를 읽기 전용으로 안전하게 참조할 수 있는 프로퍼티
	public IReadOnlyList<WeaponInstance> Weapons => weapons;

	// 최대 무기 갯수 외부 참조 프로퍼티
	public int MaxWeapons => maxWeapons;

	// 무기 획득/제거 등 인벤토리 변화가 생겼을 때 UI 갱신 등을 위해 호출되는 이벤트
	public event Action OnInventoryChanged;

	/// <summary>
	/// 새로운 무기 인스턴스를 인벤토리에 추가 시도합니다.
	/// </summary>
	public bool TryAdd(WeaponInstance instance)
	{
		if (instance == null) return false;

		// 인벤토리가 가득 차서 더 들어갈 자리가 없다면 실패 처리
		if (weapons.Count >= maxWeapons)
		{
			Debug.LogWarning($"[PlayerWeaponInventory] 무기 상한({maxWeapons})에 도달했습니다.");
			return false;
		}

		// 정상 추가 및 인벤토리 갱신 이벤트 발생
		weapons.Add(instance);
		OnInventoryChanged?.Invoke();
		return true; // 성공 반환
	}

	/// <summary>
	/// 게임 재시작이나 초기화 등을 위해 보유한 무기를 모두 비웁니다.
	/// </summary>
	public void Clear()
	{
		weapons.Clear();
		// 비워졌음을 UI등에 알리기 위해 이벤트 호출
		OnInventoryChanged?.Invoke();
	}
}