using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨업 보상이나 상점 등에서 보여줄 무기를 생성하는 서비스 클래스.
/// 무기의 랜덤 스탯(데미지 범위 내 뽑기 등)은 여기서 인스턴스화 될 때 한 번 굴려져서 확정(고정)됩니다.
/// </summary>
public static class WeaponRewardService
{
	/// <summary>
	/// 무기 ID를 받아 매니저에서 베이스 정보를 찾아 스탯이 굴려진(Roll) 새 인스턴스를 만듭니다.
	/// </summary>
	public static WeaponInstance CreateInstance(string weaponId)
	{
		// 무기 데이터를 관리하는 싱글톤 매니저 체크
		if (WeaponManager.Instance == null)
		{
			Debug.LogError("[WeaponRewardService] WeaponManager.Instance가 없습니다.");
			return null;
		}

		// ID로 공통 베이스 정보(이름, 모션타입 등) 획득
		WeaponInfo info = WeaponManager.Instance.GetWeaponInfo(weaponId);
		if (info == null)
		{
			Debug.LogWarning($"[WeaponRewardService] 알 수 없는 무기 id: {weaponId}");
			return null;
		}

		// 밸런스 키를 통해 이 무기의 최소/최대 스탯 범위(Balance) 정보 획득
		WeaponBalance balance = WeaponManager.Instance.GetWeaponBalance(info.balanceKey);
		if (balance == null)
		{
			Debug.LogWarning($"[WeaponRewardService] balance 없음: {info.balanceKey}");
			return null;
		}

		// 정보와 밸런스를 넘겨서, 생성자 안에서 랜덤 스탯이 결정된 최종 무기 객체 반환
		return new WeaponInstance(info, balance);
	}

	/// <summary>
	/// 보상 선택 창에 띄워줄 다수의 무기 후보(3개 등)를 무작위로 뽑아 리스트로 반환합니다.
	/// </summary>
	public static List<WeaponInstance> RollCandidates(IReadOnlyList<string> pool, int count = 3)
	{
		var result = new List<WeaponInstance>();
		// 뽑을 무기 풀(Pool)이 비어있으면 빈 리스트 바로 리턴
		if (pool == null || pool.Count == 0) return result;

		// 선택지 창에 동일한 무기가 중복해서 나오는 것을 막기 위한 해시셋
		var pickedIds = new HashSet<string>();
		// 풀이 너무 적은데 여러 개를 뽑으려다 무한루프에 빠지는 것을 막기 위한 안전장치
		int safety = 0;

		// 원하는 개수(count)를 채울 때까지 루프 (최대 50번까지만 방어적 탐색)
		while (result.Count < count && safety < 50)
		{
			safety++;
			// 풀 안에서 랜덤하게 하나의 무기 ID 선택
			string id = pool[Random.Range(0, pool.Count)];

			// 이미 선택된 무기 ID라면 해시셋 Add가 false를 반환하므로 무시하고 다시 뽑음
			if (!pickedIds.Add(id)) continue;

			// 중복이 아니라면 스탯이 확정된 새 무기 인스턴스 생성
			WeaponInstance instance = CreateInstance(id);
			if (instance != null) result.Add(instance);
		}

		return result;
	}

	/// <summary>
	/// 선택 창 UI 등에 표시하기 위해 무기의 핵심 정보(이름, 등급, 데미지, 쿨타임)를 문자열로 예쁘게 포맷팅합니다.
	/// </summary>
	public static string FormatPreview(WeaponInstance w)
	{
		if (w?.info == null) return "(없음)";

		// F0는 소수점 제거, F1은 소수점 첫째 자리까지 표기하여 보기 쉽게 문자열 생성
		return $"{w.info.name}\n{w.info.grade}\n데미지 {w.damage:F0}\n쿨 {w.cooltime:F1}s";
	}
}