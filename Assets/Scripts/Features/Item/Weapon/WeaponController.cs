using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어가 보유한 모든 무기의 업데이트(쿨타임 및 공격 실행)를 중앙에서 지휘하는 컨트롤러입니다.
/// </summary>
public class WeaponController : MonoBehaviour
{
	// 플레이어가 가진 무기 데이터들이 담긴 인벤토리
	[SerializeField] WeaponInventory inventory;

	/// <summary>
	/// 초기화 시 인벤토리 컴포넌트가 연결 안 되어있으면 자동 탐색합니다.
	/// </summary>
	void Awake()
	{
		if (inventory == null)
			inventory = GetComponent<WeaponInventory>();

		// 컴포넌트를 못 찾으면 오류 로그 출력
		if (inventory == null)
			Debug.LogError("[WeaponController] Player에 WeaponInventory가 필요합니다.");
	}

	/// <summary>
	/// 매 프레임 모든 무기들의 쿨타임을 갱신하고 발사 조건을 체크합니다.
	/// </summary>
	void Update()
	{
		// 게임이 정지 상태이거나 인벤토리가 없으면 동작 안 함
		if (!GameManager.instance.isLive || inventory == null) return;

		// 인벤토리에 들어있는 모든 무기 스탯 데이터(WeaponInstance)를 순회하며 Tick 실행
		foreach (WeaponInstance weapon in inventory.Weapons)
			// 현재 무기 타이머를 증가시키고, 공격 시 기준점이 될 플레이어 Transform을 전달
			weapon.Tick(Time.deltaTime, transform);
	}
}