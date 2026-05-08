using UnityEngine;
using System.Collections.Generic;

public class WeaponController : MonoBehaviour
{
	public List<WeaponInstance> myWeapons = new List<WeaponInstance>();


	void Start()
	{
		WeaponInfo info1 = WeaponManager.Instance.GetWeaponInfo("SWORD_001");
		WeaponBalance balance1 = WeaponManager.Instance.GetWeaponBalance(info1.balanceKey);

		if (info1 != null && balance1 != null)
		{
			myWeapons.Add(new WeaponInstance(info1, balance1));
			Debug.Log("sword 장착 완료!");
		}

		WeaponInfo info2 = WeaponManager.Instance.GetWeaponInfo("BOW_001");
		WeaponBalance balance2 = WeaponManager.Instance.GetWeaponBalance(info2.balanceKey);

		if (info2 != null && balance2 != null)
		{
			myWeapons.Add(new WeaponInstance(info2, balance2));
			Debug.Log("bow 장착 완료!");
		}

		WeaponInfo info3 = WeaponManager.Instance.GetWeaponInfo("ORB_001");
		WeaponBalance balance3 = WeaponManager.Instance.GetWeaponBalance(info3.balanceKey);

		if (info3 != null && balance3 != null)
		{
			myWeapons.Add(new WeaponInstance(info3, balance3));
			Debug.Log("orb 장착 완료!");
		}
	}


	void Update()
	{
		foreach (var weapon in myWeapons)
		{
			weapon.Tick(Time.deltaTime, transform);
		}
	}
}