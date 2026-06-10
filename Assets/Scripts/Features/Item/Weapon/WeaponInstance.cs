using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무기 1개의 실제 스탯 정보(데미지, 쿨타임 등)와 공격 발동 로직을 담는 클래스입니다.
/// 이 데이터 기반으로 필드에 Motion 오브젝트가 복제(Instantiate)됩니다.
/// </summary>
[Serializable]
public class WeaponInstance
{
	const float MeleeSpawnOffset = 0.7f;
	const float ProjectileSpreadDegrees = 12f;

	// 무기의 이름, 모션 ID, 타입(검, 활 등)이 들어있는 기본 고정 데이터
	public WeaponInfo info;

	// 무기가 룬 효과 등에 의해 분열된 상태인지 나타내는 플래그
	public bool isSplited;

	// 부활(재사용) 여부를 나타내는 플래그
	public bool isRevived;

	// [무기의 랜덤 적용된 개별 능력치들]
	public float damage;      // 공격력
	public float weight;      // 밀어내는 힘(넉백)이나 무게
	public float size;        // 투사체 및 타격 범위 크기
	public float reach;       // 사거리 (활의 소멸 거리, 오브의 생성 범위 등)
	public float spawntime;   // 필드 지속 시간
	public float cooltime;    // 공격 재사용 대기 시간
	public float attackspeed; // 공격 속도 (오브 틱 데미지 주기 등)
	public float movespeed;   // 투사체 날아가는 속도

	// 다음 공격까지 남은 시간을 재는 내부 타이머
	private float timer;

	/// <summary>
	/// 새로운 무기를 얻을 때, 밸런스 데이터에 정의된 최소~최대 값 사이에서 랜덤 스탯을 뽑아 인스턴스를 생성합니다.
	/// </summary>
	public WeaponInstance(WeaponInfo info, WeaponBalance balance)
	{
		this.info = info;

		// 특수 상태 동기화
		isSplited = balance.isSplited;
		isRevived = balance.isRevived;

		// 배열의 [0]최소값 ~ [1]최대값 사이를 랜덤하게 굴려 개별 스탯 확정
		damage = UnityEngine.Random.Range(balance.damageRange[0], balance.damageRange[1]);
		weight = UnityEngine.Random.Range(balance.weightRange[0], balance.weightRange[1]);
		size = UnityEngine.Random.Range(balance.sizeRange[0], balance.sizeRange[1]);
		reach = UnityEngine.Random.Range(balance.reachRange[0], balance.reachRange[1]);
		spawntime = UnityEngine.Random.Range(balance.spawntimeRange[0], balance.spawntimeRange[1]);
		cooltime = UnityEngine.Random.Range(balance.cooltimeRange[0], balance.cooltimeRange[1]);
		attackspeed = UnityEngine.Random.Range(balance.attackspeedRange[0], balance.attackspeedRange[1]);
		movespeed = UnityEngine.Random.Range(balance.movespeedRange[0], balance.movespeedRange[1]);
	}

	/// <summary>
	/// 이미 있는 무기 인스턴스의 스탯을 그대로 복사하여 새로운 인스턴스를 만들 때 사용합니다. (깊은 복사)
	/// </summary>
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

	/// <summary>
	/// WeaponController에서 매 프레임 호출하며 쿨타임을 누적하고 공격 조건을 체크합니다.
	/// </summary>
	public void Tick(float dlt, Transform playerPos)
	{
		timer += dlt;

		if (timer >= cooltime)
		{
			Attack(playerPos);
			timer = 0;
		}
	}

	/// <summary>
	/// 쿨타임이 찼을 때 실제 게임 씬에 무기(Motion) 프리팹을 스폰하는 역할입니다.
	/// </summary>
	public void Attack(Transform playerPos)
	{
		List<RuneData> activeRunes = RuneManager.instance != null
			? RuneManager.instance.GetActiveRunes()
			: new List<RuneData>();

		GameObject prefab = WeaponManager.Instance.GetMotionPrefab(info.motionId);
		if (prefab == null) return;

		Vector2 aimDirection = ResolveAimDirection(playerPos);
		int projectileCount = ResolveProjectileCount();

		for (int i = 0; i < projectileCount; i++)
		{
			ResolveSpawnTransform(playerPos.position, aimDirection, i, projectileCount, out Vector3 spawnPos, out Quaternion spawnRotation);
			SpawnMotion(prefab, spawnPos, spawnRotation, activeRunes);
		}
	}

	static int ResolveProjectileCount()
	{
		PlayerStats stats = DamageCalculator.ResolvePlayerStats();
		return stats != null ? stats.ProjectileCount : 1;
	}

	static Vector2 ResolveAimDirection(Transform playerPos)
	{
		Player player = playerPos.GetComponent<Player>();
		Vector2 direction = player != null ? player.lastTravelDirection : Vector2.right;
		if (direction.sqrMagnitude < 0.0001f)
			direction = Vector2.right;
		return direction.normalized;
	}

	void ResolveSpawnTransform(Vector3 playerPosition, Vector2 aimDirection, int index, int total, out Vector3 spawnPos, out Quaternion spawnRotation)
	{
		Vector2 shotDirection = SpreadDirection(aimDirection, index, total);
		float angle = Mathf.Atan2(shotDirection.y, shotDirection.x) * Mathf.Rad2Deg;
		spawnRotation = Quaternion.Euler(0f, 0f, angle);

		switch (info.type)
		{
			case "Sword":
			case "Hammer":
			case "Sickle":
			case "Grimore":
				spawnPos = playerPosition + (Vector3)(shotDirection * MeleeSpawnOffset);
				break;

			case "Bow":
			case "Gun":
			case "Whip":
			case "Boomerang":
			case "Staff":
				spawnPos = playerPosition + (Vector3)(shotDirection * MeleeSpawnOffset);
				break;

			case "Orb":
				Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * reach;
				spawnPos = playerPosition + new Vector3(randomOffset.x, randomOffset.y, 0f);
				spawnRotation = Quaternion.identity;
				break;

			default:
				Debug.LogWarning($"[WeaponInstance] 정의되지 않은 무기 타입: {info.type}. 기본 투사체 소환을 사용합니다.");
				spawnPos = playerPosition + (Vector3)(shotDirection * MeleeSpawnOffset);
				break;
		}
	}

	static Vector2 SpreadDirection(Vector2 baseDirection, int index, int total)
	{
		if (total <= 1)
			return baseDirection.normalized;

		float spreadTotal = (total - 1) * ProjectileSpreadDegrees;
		float angleOffset = -spreadTotal * 0.5f + index * ProjectileSpreadDegrees;
		float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
		float finalAngle = (baseAngle + angleOffset) * Mathf.Deg2Rad;
		return new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle)).normalized;
	}

	void SpawnMotion(GameObject prefab, Vector3 spawnPos, Quaternion spawnRotation, List<RuneData> activeRunes)
	{
		Motion motion = null;
		if (PoolManager.Instance != null)
			motion = PoolManager.Instance.SpawnMotion(info.motionId, spawnPos, spawnRotation);

		if (motion == null)
		{
			GameObject motionObject = UnityEngine.Object.Instantiate(prefab, spawnPos, spawnRotation);
			motion = motionObject.GetComponent<Motion>();
		}

		if (motion == null)
		{
			Debug.LogError($"[WeaponInstance] Motion 컴포넌트가 없습니다: {prefab.name}");
			return;
		}

		WeaponInstance cloneInstance = new WeaponInstance(this);
		motion.Initialize(cloneInstance, activeRunes);
	}
}
