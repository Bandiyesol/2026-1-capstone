using System.Collections.Generic;
using UnityEngine;

// 수명이 다했을 때 자기를 복제해 다시 발사하는 최종형(Final) 룬 효과
public class EffectRecursion : RuneEffect, IFinalEffect
{
	public void OnFinalExecute()
	{
		// 이미 재귀로 부활한 무기라면 무한 증식 방지
		if (weapon.isRevived) return;

		// 부활 플래그를 세팅하여 새로운 무기 인스턴스 복사
		WeaponInstance again = new WeaponInstance(weapon) { isRevived = true };

		List<RuneData> parentRunes = parentMotion.GetRunes();
		List<RuneData> childRunes = new List<RuneData>();

		// 부모의 룬 중에서 'Recursion(재귀)' 룬만 제외하고 자식에게 물려줌
		foreach (var r in parentRunes)
		{
			if (r.runeType != RuneType.Recursion) childRunes.Add(r);
		}

		// 동일한 무기 프리팹 가져오기
		GameObject prefab = WeaponManager.Instance.GetMotionPrefab(weapon.info.motionId);
		if (prefab == null) return;

		// 현재 위치와 방향에서 새 무기 생성
		GameObject clone = Instantiate(prefab, transform.position, transform.rotation);

		// 자식 룬 리스트와 새로운 무기 정보로 초기화
		clone.GetComponent<Motion>().Initialize(again, childRunes);
	}
}