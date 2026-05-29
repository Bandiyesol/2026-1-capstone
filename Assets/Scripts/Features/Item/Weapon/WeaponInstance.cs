using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class WeaponInstance
{
	const float MultiShotSpreadAngle = 15f;

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

		if (timer >= GetEffectiveCooltime())
		{
			Attack(playerPos);
			timer = 0f;
		}
	}

	float GetEffectiveCooltime()
	{
		float effectiveCooltime = cooltime;
		PlayerStats stats = PlayerStats.Instance;
		if (stats != null)
			effectiveCooltime = effectiveCooltime / stats.AttackSpeed * (1f - stats.CooldownReduction);

		if (RuneManager.instance != null)
			effectiveCooltime += RuneManager.instance.GetTotalCooldownPenalty();

		return Mathf.Max(0.01f, effectiveCooltime);
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
		if (player == null)
			return;

		Vector2 baseDirection = player.GetFacingDirection();
		if (baseDirection.sqrMagnitude <= 0.0001f)
			baseDirection = Vector2.right;
		baseDirection.Normalize();

		PlayerStats stats = PlayerStats.Instance;
		bool isProjectileWeapon = IsProjectileWeaponType(info.type);
		int projectileCount = (stats != null && isProjectileWeapon) ? stats.ProjectileCount : 1;
		float projectileRangeMultiplier = stats != null ? Mathf.Max(0.1f, stats.ProjectileRange) : 1f;
		float projectileSpeedMultiplier = stats != null ? Mathf.Max(0.1f, stats.ProjectileSpeed) : 1f;
		float meleeRangeMultiplier = stats != null ? Mathf.Max(0.1f, stats.MeleeRange) : 1f;

		for (int index = 0; index < projectileCount; index++)
		{
			float spreadAngle = ComputeSpreadAngle(index, projectileCount);
			Vector2 shotDirection = Quaternion.Euler(0f, 0f, spreadAngle) * baseDirection;
			SpawnOne(
				prefab,
				playerPos,
				shotDirection,
				activeRunes,
				projectileRangeMultiplier,
				projectileSpeedMultiplier,
				meleeRangeMultiplier
			);
		}
	}

	float ComputeSpreadAngle(int shotIndex, int shotCount)
	{
		if (shotCount <= 1)
			return 0f;

		float t = shotIndex / (shotCount - 1f);
		return Mathf.Lerp(-MultiShotSpreadAngle, MultiShotSpreadAngle, t);
	}

	bool IsProjectileWeaponType(string weaponType)
	{
		return weaponType == "Bow"
			|| weaponType == "Gun"
			|| weaponType == "Whip"
			|| weaponType == "Boomerang"
			|| weaponType == "Staff"
			|| weaponType == "Orb";
	}

	bool IsMeleeWeaponType(string weaponType)
	{
		return weaponType == "Sword"
			|| weaponType == "Hammer"
			|| weaponType == "Sickle"
			|| weaponType == "Grimore";
	}

	void SpawnOne(
		GameObject prefab,
		Transform playerPos,
		Vector2 direction,
		List<RuneData> activeRunes,
		float projectileRangeMultiplier,
		float projectileSpeedMultiplier,
		float meleeRangeMultiplier
	)
	{
		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		Vector3 spawnPos = playerPos.position;
		Quaternion spawnRotation = Quaternion.identity;

		switch (info.type)
		{
			case "Sword":
			case "Bow":
			case "Gun":
			case "Hammer":
			case "Sickle":
			case "Whip":
			case "Boomerang":
			case "Staff":
				spawnPos = playerPos.position + (Vector3)(direction * 0.7f);
				spawnRotation = Quaternion.Euler(0f, 0f, angle);
				break;
			case "Grimore":
				spawnPos = playerPos.position;
				spawnRotation = Quaternion.identity;
				break;
			case "Orb":
				Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * (reach * projectileRangeMultiplier);
				spawnPos = playerPos.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
				spawnRotation = Quaternion.identity;
				break;
			default:
				Debug.LogWarning($"{info.type}은(는) 정의되지 않은 소환 방식입니다.");
				break;
		}

		GameObject motionObj = UnityEngine.Object.Instantiate(prefab, spawnPos, spawnRotation);
		Motion motion = motionObj.GetComponent<Motion>();
		if (motion == null)
		{
			UnityEngine.Object.Destroy(motionObj);
			return;
		}

		WeaponInstance cloneInstance = new WeaponInstance(this);
		if (IsMeleeWeaponType(info.type))
			cloneInstance.size *= meleeRangeMultiplier;

		if (IsProjectileWeaponType(info.type))
		{
			cloneInstance.reach *= projectileRangeMultiplier;
			cloneInstance.movespeed *= projectileSpeedMultiplier;
		}

		motion.Initialize(cloneInstance, activeRunes);
	}
}