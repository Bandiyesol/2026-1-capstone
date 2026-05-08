using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


[Serializable]
public class WeaponInstance
{
	public WeaponInfo info;
	public float damage;
	public float weight;
	public float size;
	public float spawntime;
	public float cooltime;
	public float reach;
	public float speed;
	private float timer;


	public WeaponInstance(WeaponInfo info, WeaponBalance balance)
	{
		this.info = info;
		damage = UnityEngine.Random.Range(balance.damageRange[0], balance.damageRange[1]);
		weight = UnityEngine.Random.Range(balance.weightRange[0], balance.weightRange[1]);
		size = UnityEngine.Random.Range(balance.sizeRange[0], balance.sizeRange[1]);
		spawntime = UnityEngine.Random.Range(balance.spawntimeRange[0], balance.spawntimeRange[1]);
		cooltime = UnityEngine.Random.Range(balance.cooltimeRange[0], balance.cooltimeRange[1]);
		reach = UnityEngine.Random.Range(balance.reachRange[0], balance.reachRange[1]);
		speed = UnityEngine.Random.Range(balance.speedRange[0], balance.speedRange[1]);
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
				float spawnRadius = size * 1.5f;
				Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * spawnRadius;

				spawnPos = playerPos.position + new Vector3(randomOffset.x, randomOffset.y, 0);
				spawnRotation = Quaternion.identity;
				break;
			
			default:
				Debug.LogWarning($"{info.type}은(는) 정의되지 않은 소환 방식입니다.");
				break;
		}
		
		GameObject motion = UnityEngine.Object.Instantiate(prefab, spawnPos, spawnRotation);
		
		if (info.type == "Sword") motion.transform.SetParent(playerPos);
		
		motion.GetComponent<Motion>().Initialize(this);
	}
}