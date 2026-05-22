using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class WeaponInstance
{
	public WeaponInfo info;
	public bool isSplited;
	public bool isRevived;
	public float damage;
	public float weight;
	public float size;
	public float reach;
	public float spawntime;
	public float cooltime;
	public float attackspeed;
	public float movespeed;
	private float timer;


	public WeaponInstance(WeaponInfo info, WeaponBalance balance)
	{
		this.info = info;
		isSplited = balance.isSplited;
		isRevived = balance.isRevived;
		damage = UnityEngine.Random.Range(balance.damageRange[0], balance.damageRange[1]);
		weight = UnityEngine.Random.Range(balance.weightRange[0], balance.weightRange[1]);
		size = UnityEngine.Random.Range(balance.sizeRange[0], balance.sizeRange[1]);
		reach = UnityEngine.Random.Range(balance.reachRange[0], balance.reachRange[1]);
		spawntime = UnityEngine.Random.Range(balance.spawntimeRange[0], balance.spawntimeRange[1]);
		cooltime = UnityEngine.Random.Range(balance.cooltimeRange[0], balance.cooltimeRange[1]);
		attackspeed = UnityEngine.Random.Range(balance.attackspeedRange[0], balance.attackspeedRange[1]);
		movespeed = UnityEngine.Random.Range(balance.movespeedRange[0], balance.movespeedRange[1]);
	}


	public WeaponInstance(WeaponInstance other)
	{
		info = other.info;
		isSplited = other.isSplited;
		isRevived = other.isRevived;
		damage = other.damage;
		weight = other.weight;
		size = other.size;
		reach = other.reach;
		spawntime = other.spawntime;
		cooltime = other.cooltime;
		attackspeed = other.attackspeed;
		movespeed = other.movespeed;
	}


	public void Tick(float dlt, Transform playerPos)
	{
		timer += dlt;
		
		if (timer >= cooltime)
		{
			Attack(playerPos);
			timer = 0;
		}
	}


	public void Attack(Transform playerPos)
	{
		if (RuneManager.instance != null && !RuneManager.instance.IsCurrentCombinationValid)
		{
			Debug.LogWarning($"[WeaponInstance] 룬 조합 오류: {RuneManager.instance.CurrentWarningMessage} → 공격 취소");
			return;
		}

		List<RuneData> activeRunes = RuneManager.instance != null
			? RuneManager.instance.GetActiveRunes()
			: new List<RuneData>();

		GameObject prefab = WeaponManager.Instance.GetMotionPrefab(info.motionId);
		if (prefab == null) return;

		Player player = playerPos.GetComponent<Player>();
		Vector2 direction = player.lastTravelDirection;
		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		
		Vector3 spawnPos = playerPos.position;
		Quaternion spawnRotation= Quaternion.identity;

		switch (info.type)
		{
			case "Sword": 
				spawnPos = playerPos.position + (Vector3)(direction *0.7f);
				spawnRotation = Quaternion.Euler(0, 0, angle);
				break;

			case "Bow":
				spawnPos = playerPos.position + (Vector3)(direction *0.7f);
				spawnRotation = Quaternion.Euler(0, 0, angle);
				break;

			case "Orb":
				Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * reach;

				spawnPos = playerPos.position + new Vector3(randomOffset.x, randomOffset.y, 0);
				spawnRotation = Quaternion.identity;
				break;
			
			default:
				Debug.LogWarning($"{info.type}은(는) 정의되지 않은 소환 방식입니다.");
				break;
		}
		
		GameObject motionobj = UnityEngine.Object.Instantiate(prefab, spawnPos, spawnRotation);
		WeaponInstance cloneInstance = new WeaponInstance(this);
		motionobj.GetComponent<Motion>().Initialize(cloneInstance, activeRunes);
	}
}