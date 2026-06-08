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
		// 델타 타임 누적
		timer += dlt;

		// 타이머가 이 무기의 쿨타임을 넘어섰다면 타격(Attack) 실행
		if (timer >= cooltime)
		{
			Attack(playerPos);
			// 쿨타임 초기화
			timer = 0;
		}
	}

	/// <summary>
	/// 쿨타임이 찼을 때 실제 게임 씬에 무기(Motion) 프리팹을 스폰하는 역할입니다.
	/// </summary>
	public void Attack(Transform playerPos)
	{
		// 룬 매니저가 존재하는데 현재 유저가 맞춘 룬 조합이 무효/오류 상태라면 무기 생성 취소
		if (RuneManager.instance != null && !RuneManager.instance.IsCurrentCombinationValid)
		{
			Debug.LogWarning($"[WeaponInstance] 룬 조합 오류: {RuneManager.instance.CurrentWarningMessage} → 공격 취소");
			return;
		}

		// 무기에 적용해줄 현재 활성화된 룬 목록 가져오기
		List<RuneData> activeRunes = RuneManager.instance != null
			? RuneManager.instance.GetActiveRunes()
			: new List<RuneData>();

		// WeaponManager에서 이 무기에 맞는 외형/동작 프리팹(Motion)을 가져옴
		GameObject prefab = WeaponManager.Instance.GetMotionPrefab(info.motionId);
		if (prefab == null) return; // 프리팹 누락 시 방어 처리

		// 플레이어 스크립트에 접근해서 플레이어가 마지막으로 보고 있던/이동하던 방향 정보를 가져옴
		Player player = playerPos.GetComponent<Player>();
		Vector2 direction = player.lastTravelDirection;

		// 방향 벡터(x, y)를 이용해 2D 회전 각도(Z축 기준) 수학적 변환
		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

		// 무기가 생성될 위치와 방향 초기화
		Vector3 spawnPos = playerPos.position;
		Quaternion spawnRotation = Quaternion.identity;

		// 무기 타입에 따른 소환 방식 분기 처리
		switch (info.type)
		{
			case "Sword":

				// 플레이어 중심에서 이동 방향으로 약간 앞(0.7f)에 생성하여 베기 쉽게 함
				spawnPos = playerPos.position + (Vector3)(direction * 0.7f);

				// 무기가 이동 방향을 바라보도록 각도 회전
				spawnRotation = Quaternion.Euler(0, 0, angle);
				break;

			case "Bow":

				// 활(화살)도 플레이어 앞쪽에서 발사되도록 위치 지정
				spawnPos = playerPos.position + (Vector3)(direction * 0.7f);

				// 화살 촉이 날아갈 방향을 바라보도록 회전
				spawnRotation = Quaternion.Euler(0, 0, angle);
				break;

			case "Orb":

				// 오브는 플레이어 근처가 아니라, 무기 사거리(reach) 반경 내 임의의 위치에 랜덤으로 스폰됨
				Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * reach;

				// z축은 유지한 채 랜덤 x, y 편차 더함
				spawnPos = playerPos.position + new Vector3(randomOffset.x, randomOffset.y, 0);
				// 오브는 보통 둥그니 회전값이 불필요 (기본값)
				spawnRotation = Quaternion.identity;
				break;

			default:
				Debug.LogWarning($"{info.type}은(는) 정의되지 않은 소환 방식입니다.");
				break;
		}

		// 위에서 계산한 위치와 회전값으로 실제 프리팹을 게임 씬에 생성
		GameObject motionobj = UnityEngine.Object.Instantiate(prefab, spawnPos, spawnRotation);

		// 생성된 무기 오브젝트가 참조할 자신의 스탯 독립성을 위해 현재 스탯 데이터를 복제
		WeaponInstance cloneInstance = new WeaponInstance(this);

		// 생성된 오브젝트의 Motion 스크립트를 가져와 스탯 데이터와 룬 목록을 넘겨주며 초기화
		motionobj.GetComponent<Motion>().Initialize(cloneInstance, activeRunes);
	}
}