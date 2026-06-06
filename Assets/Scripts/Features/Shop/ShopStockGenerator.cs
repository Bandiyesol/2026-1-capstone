using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>상점 오픈 시 카테고리별 랜덤 진열 생성.</summary>
public static class ShopStockGenerator
{
	static readonly CompareInfo KoreanComparer = CultureInfo.GetCultureInfo("ko-KR").CompareInfo;

	public static List<ShopListing> GenerateWeapons(ShopCatalogSettings settings)
	{
		var result = new List<ShopListing>();
		if (settings == null || settings.weaponListingCount <= 0)
			return result;

		List<string> pool = BuildWeaponPool(settings);
		if (pool.Count == 0)
			return result;

		int target = settings.weaponListingCount;
		int safety = 0;

		while (result.Count < target && safety < 100)
		{
			safety++;
			string weaponId = pool[Random.Range(0, pool.Count)];

			WeaponInstance instance = WeaponRewardService.CreateInstance(weaponId);
			if (instance == null)
				continue;

			ShopItemGrade grade = ShopGradeUtility.Parse(instance.info.grade);

			result.Add(new ShopListing
			{
				category = ShopItemCategory.Weapon,
				grade = grade,
				weapon = instance,
				price = settings.GetPrice(grade),
			});
		}

		result.Sort(CompareListings);

		return result;
	}

	public static List<ShopListing> GenerateAccessories(ShopCatalogSettings settings)
	{
		// 악세서리 데이터 추가 전까지 빈 목록
		return new List<ShopListing>();
	}

	public static List<ShopListing> GeneratePotions(ShopCatalogSettings settings)
	{
		// 물약 데이터 추가 전까지 빈 목록
		return new List<ShopListing>();
	}

	static List<string> BuildWeaponPool(ShopCatalogSettings settings)
	{
		var pool = new List<string>();

		if (settings.weaponIdPool != null && settings.weaponIdPool.Length > 0)
		{
			foreach (string id in settings.weaponIdPool)
			{
				if (!string.IsNullOrWhiteSpace(id))
					pool.Add(id.Trim());
			}

			return pool;
		}

		if (WeaponManager.Instance == null)
			return pool;

		foreach (WeaponInfo info in WeaponManager.Instance.GetAllWeaponInfos())
		{
			if (info != null && !string.IsNullOrEmpty(info.id))
				pool.Add(info.id);
		}

		return pool;
	}

	static int CompareListings(ShopListing a, ShopListing b)
	{
		int grade = ((int)(b?.grade ?? ShopItemGrade.Common)).CompareTo((int)(a?.grade ?? ShopItemGrade.Common));
		if (grade != 0)
			return grade;

		string nameA = a?.DisplayName ?? string.Empty;
		string nameB = b?.DisplayName ?? string.Empty;
		return KoreanComparer.Compare(nameA, nameB, CompareOptions.StringSort);
	}
}
