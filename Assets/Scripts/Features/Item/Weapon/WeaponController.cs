using UnityEngine;
using System.Collections.Generic;

public class WeaponController : MonoBehaviour
{
	[SerializeField] WeaponInventory inventory;


	void Awake()
	{
		if (inventory == null)
			inventory = GetComponent<WeaponInventory>();

		if (inventory == null)
			Debug.LogError("[WeaponController] Player에 WeaponInventory가 필요합니다.");
	}

	void Update()
	{
		if (!GameManager.instance.isLive || inventory == null) return;

		foreach (WeaponInstance weapon in inventory.Weapons)
			weapon.Tick(Time.deltaTime, transform);
	}
}