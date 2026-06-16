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
	const float ReachFromRangeBonusRatio = 0.7f;

	// 무기의 이름, 모션 ID, 타입(검, 활 등)이 들어있는 기본 고정 데이터
	public WeaponInfo info;

	// 무기가 룬 효과 등에 의해 분열된 상태인지 나타내는 플래그 (밸런스: 분열 가능 여부)
	public bool isSplited;

	// Split 룬으로 생성된 자식 투사체인지 (재분열 방지)
	public bool isSplitChild;

	// 부활(재사용) 여부를 나타내는 플래그
	public bool isRevived;

	// [무기의 랜덤 적용된 개별 능력치들]
	public float damage;      // 공격력
	public float weight;      // 밀어내는 힘(넉백)이나 무게
	public float size;        // 투사체 및 타격 범위 크기
	public float reach;       // 사거리 (활의 소멸 거리, 오브의 생성 범위 등)
	public float spawntime;   // 필드 지속 시간
	public float cooltime;    // 공격 재사용 대기 시간
	public float attackspeed; // 무기 공격 속도 배율 (높을수록 빠름)
	public float tickInterval; // Orb 전용: 틱 데미지 기본 간격(초)
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
		tickInterval = balance.tickIntervalRange != null && balance.tickIntervalRange.Length >= 2
			? UnityEngine.Random.Range(balance.tickIntervalRange[0], balance.tickIntervalRange[1])
			: 0f;
		movespeed = UnityEngine.Random.Range(balance.movespeedRange[0], balance.movespeedRange[1]);
	}

	/// <summary>
	/// 이미 있는 무기 인스턴스의 스탯을 그대로 복사하여 새로운 인스턴스를 만들 때 사용합니다. (깊은 복사)
	/// </summary>
	public WeaponInstance(WeaponInstance other)
	{
		info = other.info;
		isSplited = other.isSplited;
		isSplitChild = other.isSplitChild;
		isRevived = other.isRevived;
		damage = other.damage;
		weight = other.weight;
		size = other.size;
		reach = other.reach;
		spawntime = other.spawntime;
		cooltime = other.cooltime;
		attackspeed = other.attackspeed;
		tickInterval = other.tickInterval;
		movespeed = other.movespeed;
	}

	/// <summary>
	/// WeaponController에서 매 프레임 호출하며 쿨타임을 누적하고 공격 조건을 체크합니다.
	/// </summary>
	public void Tick(float dlt, Transform playerPos)
	{
		timer += dlt;

		float effectiveCooltime = cooltime / ResolveEffectiveAttackSpeed();

		if (timer >= effectiveCooltime)
		{
			Attack(playerPos);
			timer -= effectiveCooltime;
		}
	}

	/// <summary>플레이어 AttackSpeed × 무기 attackspeed 배율 (높을수록 빠름).</summary>
	public float ResolveEffectiveAttackSpeed()
	{
		PlayerStats stats = DamageCalculator.ResolvePlayerStats();
		float playerMultiplier = stats != null ? stats.AttackSpeed : 1f;
		return playerMultiplier * Mathf.Max(0.01f, attackspeed);
	}

	/// <summary>Orb 틱 간격(초). tickInterval ÷ effectiveAS.</summary>
	public float ResolveEffectiveTickInterval()
	{
		float baseInterval = tickInterval > 0f ? tickInterval : 1f;
		return Mathf.Max(0.05f, baseInterval / ResolveEffectiveAttackSpeed());
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

			// [악세사리] 랜턴 — 10% 확률로 투사체 복제 (같은 위치/방향에 추가 소환)
			if (AccessoryEffect.instance != null &&
			    AccessoryEffect.instance.Has(AccessoryEffectType.DuplicateBullet) &&
			    UnityEngine.Random.value < AccessoryEffect.instance.duplicateBulletChance)
			{
				SpawnMotion(prefab, spawnPos, spawnRotation, activeRunes);
			}

			// [악세사리] 그림자 가면 — 25% 확률로 분신 위치에서 동일 투사체 소환
			if (AccessoryEffect.instance != null &&
			    AccessoryEffect.instance.Has(AccessoryEffectType.ShadowClone) &&
			    AccessoryEffect.instance.shadowCloneInstance != null &&
			    UnityEngine.Random.value < AccessoryEffect.instance.shadowCloneAttackChance)
			{
				Vector3 clonePos = AccessoryEffect.instance.shadowCloneInstance.transform.position;
				SpawnMotion(prefab, clonePos, spawnRotation, activeRunes);
			}
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

		// [PlayerStats 연동] 매 공격마다 최신 스탯을 복제본에 반영 (원본 스탯은 보존)
		ApplyPlayerStats(cloneInstance);

		motion.Initialize(cloneInstance, activeRunes);
	}

	/// <summary>
	/// 악세사리 등으로 변한 PlayerStats를 무기 복제본에 배율로 적용합니다.
	/// - ProjectileSpeed  → 투사체 속도(movespeed)
	/// - ProjectileRange  → 원거리 사거리(reach)만 (flat은 수치 가산)
	/// - ProjectileSize   → 원거리 투사체 크기(size)만
	/// - MeleeRange       → 근접 범위(reach)만
	/// </summary>
	void ApplyPlayerStats(WeaponInstance clone)
	{
		PlayerStats stats = DamageCalculator.ResolvePlayerStats();
		if (stats == null) return;

		clone.movespeed *= stats.ProjectileSpeed;

		switch (info.type)
		{
			case "Sword":
			case "Hammer":
			case "Sickle":
			case "Grimore":
			case "Whip":
				ApplyReachScaling(clone, stats.MeleeRangeMultiplier, stats.MeleeRangeFlatAdd);
				break;

			case "Orb":
			case "Bow":
			case "Gun":
			case "Boomerang":
			case "Staff":
				ApplyReachScaling(clone, stats.ProjectileRangeMultiplier, stats.ProjectileRangeFlatAdd);
				ApplyProjectileSize(clone, stats.ProjectileSize);
				break;
		}
	}

	static void ApplyReachScaling(WeaponInstance clone, float rangeMultiplier, float rangeFlatAdd)
	{
		float mult = SoftenedRangeMultiplier(rangeMultiplier, ReachFromRangeBonusRatio);
		clone.reach = (clone.reach + rangeFlatAdd) * mult;
	}

	static void ApplyProjectileSize(WeaponInstance clone, float sizeMultiplier)
	{
		if (sizeMultiplier <= 0f)
			return;

		clone.size *= sizeMultiplier;
	}

	static float SoftenedRangeMultiplier(float rangeStat, float bonusRatio)
	{
		if (rangeStat <= 0f)
			return 0.25f;

		return 1f + (rangeStat - 1f) * bonusRatio;
	}
}
