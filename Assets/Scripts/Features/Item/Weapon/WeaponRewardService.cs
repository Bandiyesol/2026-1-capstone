using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보상/선택창용 WeaponInstance 생성. 수치는 여기서 한 번 롤링되어 고정됩니다.
/// </summary>
public static class WeaponRewardService
{
	public static WeaponInstance CreateInstance(string weaponId)
	{
		if (WeaponManager.Instance == null)
		{
			Debug.LogError("[WeaponRewardService] WeaponManager.Instance가 없습니다.");
			return null;
		}

		WeaponInfo info = WeaponManager.Instance.GetWeaponInfo(weaponId);
		if (info == null)
		{
			Debug.LogWarning($"[WeaponRewardService] 알 수 없는 무기 id: {weaponId}");
			return null;
		}

		WeaponBalance balance = WeaponManager.Instance.GetWeaponBalance(info.balanceKey);
		if (balance == null)
		{
			Debug.LogWarning($"[WeaponRewardService] balance 없음: {info.balanceKey}");
			return null;
		}

		return new WeaponInstance(info, balance);
	}

	public static List<WeaponInstance> RollCandidates(IReadOnlyList<string> pool, int count = 3)
	{
		var result = new List<WeaponInstance>();
		if (pool == null || pool.Count == 0) return result;

		var pickedIds = new HashSet<string>();
		int safety = 0;

		while (result.Count < count && safety < 50)
		{
			safety++;
			string id = pool[Random.Range(0, pool.Count)];
			if (!pickedIds.Add(id)) continue;

			WeaponInstance instance = CreateInstance(id);
			if (instance != null) result.Add(instance);
		}

		return result;
	}

	public static string FormatPreview(WeaponInstance w)
	{
		if (w?.info == null) return "(없음)";
		return $"{w.info.name}\n{w.info.grade}\n데미지 {w.damage:F0}\n쿨 {w.cooltime:F1}s";
	}
}
