using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보상/선택창용 WeaponInstance 생성. 수치는 여기서 한 번 롤링되어 고정됩니다.
/// </summary>
public static class WeaponRewardService
{
	public static WeaponInstance CreateInstance(string weaponId)
	{
		return CreateInstanceWithGrade(weaponId, ParseGradeFromInfo(weaponId));
	}

	public static WeaponInstance CreateInstanceWithGrade(string weaponId, ShopItemGrade grade)
	{
		if (WeaponManager.Instance == null)
		{
			Debug.LogError("[WeaponRewardService] WeaponManager.Instance가 없습니다.");
			return null;
		}

		WeaponInfo baseInfo = WeaponManager.Instance.GetWeaponInfo(weaponId);
		if (baseInfo == null)
		{
			Debug.LogWarning($"[WeaponRewardService] 알 수 없는 무기 id: {weaponId}");
			return null;
		}

		WeaponInfo shopInfo = CloneWeaponInfo(baseInfo, grade);
		WeaponBalance balance = WeaponManager.Instance.GetWeaponBalance(shopInfo.balanceKey);
		if (balance == null)
		{
			Debug.LogWarning($"[WeaponRewardService] balance 없음: {shopInfo.balanceKey}");
			balance = WeaponManager.Instance.GetWeaponBalance(baseInfo.balanceKey);
		}

		if (balance == null)
			return null;

		return new WeaponInstance(shopInfo, balance);
	}

	static ShopItemGrade ParseGradeFromInfo(string weaponId)
	{
		if (WeaponManager.Instance == null)
			return ShopItemGrade.Common;

		WeaponInfo info = WeaponManager.Instance.GetWeaponInfo(weaponId);
		if (info == null || string.IsNullOrEmpty(info.grade))
			return ShopItemGrade.Common;

		return ShopGradeUtility.Parse(info.grade);
	}

	static WeaponInfo CloneWeaponInfo(WeaponInfo source, ShopItemGrade grade)
	{
		string gradeName = ShopGradeUtility.ToGradeName(grade);
		string balanceKey = ShopGradeUtility.BuildBalanceKey(source.type, grade);

		return new WeaponInfo
		{
			id = source.id,
			name = source.name,
			spriteId = source.spriteId,
			motionId = source.motionId,
			type = source.type,
			grade = gradeName,
			balanceKey = balanceKey,
			weaponCategory = source.weaponCategory,
			legendaryPassiveId = grade == ShopItemGrade.Legendary ? source.legendaryPassiveId : null,
		};
	}

	public static List<WeaponInstance> RollCandidates(IReadOnlyList<string> pool, int count = 3)
	{
		return RollCandidates(pool, count, ShopCatalogSettings.Instance);
	}

	/// <summary>등급 가중치(ShopCatalogSettings)로 후보 무기를 뽑습니다. 높은 등급일수록 확률이 낮습니다.</summary>
	public static List<WeaponInstance> RollCandidates(
		IReadOnlyList<string> pool,
		int count,
		ShopCatalogSettings gradeSettings)
	{
		var result = new List<WeaponInstance>();
		if (pool == null || pool.Count == 0)
			return result;

		var pickedIds = new HashSet<string>();
		int safety = 0;

		while (result.Count < count && safety < 100)
		{
			safety++;
			ShopItemGrade grade = gradeSettings != null
				? gradeSettings.RollListingGrade()
				: ShopItemGrade.Common;

			string id = PickWeaponIdForGrade(pool, grade);
			if (string.IsNullOrEmpty(id) || !pickedIds.Add(id))
				continue;

			WeaponInstance instance = CreateInstance(id);
			if (instance != null)
				result.Add(instance);
		}

		return result;
	}

	static string PickWeaponIdForGrade(IReadOnlyList<string> pool, ShopItemGrade targetGrade)
	{
		if (pool == null || pool.Count == 0)
			return null;

		string gradeName = ShopGradeUtility.ToGradeName(targetGrade);
		var filtered = new List<string>();
		foreach (string id in pool)
		{
			if (WeaponRewardService.GetWeaponGrade(id) == gradeName)
				filtered.Add(id);
		}

		if (filtered.Count == 0)
		{
			foreach (string id in pool)
				filtered.Add(id);
		}

		return filtered[Random.Range(0, filtered.Count)];
	}

	public static string FormatTitle(WeaponInstance w)
	{
		if (w?.info == null) return "(없음)";
		return w.info.name;
	}

	public static string FormatStats(WeaponInstance w)
	{
		if (w?.info == null) return "";
		return $"데미지 {w.damage:F0}\n쿨 {w.cooltime:F1}s";
	}

	/// <summary>선택 카드 Detail — 등급(색상) + 스탯.</summary>
	public static string FormatChoiceDetail(WeaponInstance w)
	{
		if (w?.info == null) return "";
		return $"{ChoiceGradeDisplay.FormatColored(w.info.grade)}\n{FormatStats(w)}";
	}

	/// <summary>Title + Stats 한 줄에 (레거시). 새 UI는 FormatTitle / FormatStats 사용.</summary>
	public static string FormatPreview(WeaponInstance w)
	{
		if (w?.info == null) return "(없음)";
		return $"{FormatTitle(w)}\n{FormatStats(w)}";
	}

	public static Sprite GetIcon(WeaponInstance w)
	{
		if (w?.info == null)
			return null;
		if (WeaponManager.Instance == null)
			return null;
		return WeaponManager.Instance.GetWeaponSprite(w.info);
	}

	/// <summary>
	/// WeaponManager에 등록된 전체 무기 ID 목록 반환.
	/// RewardRollService에서 무기 풀로 사용.
	/// </summary>
	public static List<string> GetAllWeaponIds()
	{
		if (WeaponManager.Instance == null) return new List<string>();
		return WeaponManager.Instance.GetAllWeaponIds();
	}

	/// <summary>
	/// 무기 ID로 등급(string) 반환.
	/// RewardRollService에서 등급 필터링에 사용.
	/// </summary>
	public static string GetWeaponGrade(string weaponId)
	{
		if (WeaponManager.Instance == null) return string.Empty;
		WeaponInfo info = WeaponManager.Instance.GetWeaponInfo(weaponId);
		return info?.grade ?? string.Empty;
	}
}
