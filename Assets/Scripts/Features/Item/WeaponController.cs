using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
	[Header("[ References ]")]
	public GameObject weaponProjectilePrefab;
	public GameObject weaponMeleePrefab;
	public LayerMask monsterLayer;

	private List<Weapon> equippedWeapons = new List<Weapon>();


	void Update()
	{
		float dlt = Time.deltaTime;

		foreach (var weapon in equippedWeapons) weapon.Tick(dlt);
	}
	
	public void AddWeapon(string weaponId)
	{
		WeaponInfo info = WeaponManager.Instance.GetWeaponInfo(weaponId);
		if (info == null) return;

		WeaponBalance balance = WeaponManager.Instance.GetWeaponBalance(weaponId);
		if (balance == null) return;

		GameObject prefabToSpawn = GetPrefabByType(info.type);
		if (prefabToSpawn == null) return;

		GameObject obj = Instantiate(prefabToSpawn, transform);
		obj.name = info.name;

		Weapon weaponScript = obj.GetComponent<Weapon>();
		weaponScript.Init(info, balance, monsterLayer);
		equippedWeapons.Add(weaponScript);

		Debug.Log($"{info.grade} 등급 무기 [{info.name}] 장착 완료!");
	}

	private GameObject GetPrefabByType(string type)
	{
		switch (type)
		{
			case "Sword":
			case "Hammer":
			case "Sickle":
			case "Whip":
				return weaponMeleePrefab;

			case "Bow":
			case "Gun":
			case "Staff":
				return weaponProjectilePrefab;

			case "Boomerang":
				break;
			
			case "Grimore":
				break;

			case "Orb":
				break;
		}
		
		return weaponMeleePrefab;
	}
}