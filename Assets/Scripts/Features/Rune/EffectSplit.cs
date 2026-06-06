using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 충돌 시 무기가 여러 개로 쪼개지는 트리거 룬 효과
public class EffectSplit : RuneEffect, ITriggerEffect
{
	public bool DestroyOnExecute => data.isDestroyed;
	public bool ProtectParent => false; // 분열은 본체를 보호하지 않음

	// SplitRuneData로 형변환하여 전용 데이터 접근
	SplitRuneData SplitData => data as SplitRuneData;

	private void Update() => UpdateCooltime(); // 쿨타임 갱신

	public void OnReflect(Collider2D collision)
	{
		// 이미 분열된 무기이거나 쿨타임이 안 돌았으면 무시
		if (weapon.isSplited || !isReady) return;

		int spawns = RuneDataAccess.GetSpawnsPerTrigger(data);
		if (spawns <= 0) return;

		// 현재 분열 룬 "이후"에 장착된 룬들만 자식에게 물려줌
		List<RuneData> childRunes = GetRunesAfterSplit(parentMotion.GetRunes());
		float baseZ = transform.eulerAngles.z;
		// 분열 각도(부채꼴) 가져오기, 기본값 30도
		float spread = SplitData != null && SplitData.spreadDegrees > 0f ? SplitData.spreadDegrees : 30f;

		// 지정된 개수만큼 부채꼴로 각도를 분배하여 자식 무기 생성
		for (int i = 0; i < spawns; i++)
			SpawnChild(SymmetricAngle(baseZ, spread, i, spawns), childRunes);

		ResetCooltime();
	}

	/// <summary>분열 룬 슬롯 뒤에 있는 룬만 자식이 물려받음.</summary>
	public static List<RuneData> GetRunesAfterSplit(IReadOnlyList<RuneData> runes)
	{
		if (runes == null) return new List<RuneData>();

		// 분열 룬의 인덱스 찾기
		int splitIdx = -1;
		for (int i = 0; i < runes.Count; i++)
		{
			if (runes[i] != null && runes[i].runeType == RuneType.Split)
			{
				splitIdx = i;
				break;
			}
		}

		if (splitIdx < 0) return new List<RuneData>();

		// 분열 룬 다음 슬롯부터 끝까지 잘라내기
		var list = new List<RuneData>();
		for (int i = splitIdx + 1; i < runes.Count; i++)
		{
			if (runes[i] != null) list.Add(runes[i]);
		}
		return list;
	}

	// 총 각도(totalSpread) 안에서 지정된 개수(count)만큼 대칭되게 각도를 나눠주는 수학 함수
	static float SymmetricAngle(float baseZ, float totalSpread, int index, int count)
	{
		if (count <= 1) return baseZ;
		float t = (float)index / (count - 1);
		return baseZ - totalSpread * 0.5f + totalSpread * t;
	}

	// 복제된 자식 무기 소환
	void SpawnChild(float angleZ, List<RuneData> childRunes)
	{
		// isSplited 플래그를 true로 하여 2차 분열 방지
		WeaponInstance childInstance = new WeaponInstance(weapon) { isSplited = true };
		// 자식은 데미지가 반감됨 (power 값이 있으면 그 값 사용, 없으면 0.5배)
		childInstance.damage *= data.power > 0 ? data.power : 0.5f;

		GameObject prefab = WeaponManager.Instance.GetMotionPrefab(weapon.info.motionId);
		GameObject clone = Instantiate(prefab, transform.position, Quaternion.Euler(0f, 0f, angleZ));

		// 남은 수명을 부모로부터 물려받아 초기화
		clone.GetComponent<Motion>().Initialize(childInstance, childRunes, parentMotion.GetRemainingLife());
	}
}