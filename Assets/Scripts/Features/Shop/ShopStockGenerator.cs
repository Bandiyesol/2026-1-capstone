using System;
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
			ShopItemGrade targetGrade = settings.RollListingGrade();
			string weaponId = PickWeaponId(pool, targetGrade);
			if (string.IsNullOrEmpty(weaponId))
				continue;

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
		var result = new List<ShopListing>();
		if (settings == null || settings.accessoryListingCount <= 0)
			return result;

		List<AccessoryData> pool = BuildAccessoryPool(settings);
		if (pool.Count == 0)
			return result;

		int target = settings.accessoryListingCount;
		int safety = 0;
		var pickedNames = new HashSet<string>();

		while (result.Count < target && safety < 100)
		{
			safety++;
			ShopItemGrade targetGrade = settings.RollListingGrade();
			AccessoryData picked = PickAccessory(pool, targetGrade, pickedNames);
			if (picked == null)
				continue;

			pickedNames.Add(picked.name);
			ShopItemGrade grade = ToShopGrade(picked.grade);
			result.Add(new ShopListing
			{
				category = ShopItemCategory.Accessory,
				grade = grade,
				accessory = picked,
				price = settings.GetPrice(grade),
			});
		}

		result.Sort(CompareListings);
		return result;
	}

	public static List<ShopListing> GeneratePotions(ShopCatalogSettings settings)
	{
		var result = new List<ShopListing>();
		if (settings == null || settings.potionListingCount <= 0)
			return result;

		List<PotionData> pool = BuildPotionPool(settings);
		if (pool.Count > 0)
		{
			int target = settings.potionListingCount;
			int safety = 0;

			while (result.Count < target && safety < 100)
			{
				safety++;
				PotionData picked = pool[UnityEngine.Random.Range(0, pool.Count)];
				if (picked == null)
					continue;

				result.Add(new ShopListing
				{
					category = ShopItemCategory.Potion,
					grade = ShopItemGrade.Common,
					potion = picked,
					price = picked.price > 0 ? picked.price : settings.potionDefaultPrice,
				});
			}
		}
		else
		{
			var types = new List<PotionType>((PotionType[])Enum.GetValues(typeof(PotionType)));
			Shuffle(types);

			int count = Mathf.Min(settings.potionListingCount, types.Count);
			for (int i = 0; i < count; i++)
			{
				PotionType type = types[i];
				result.Add(new ShopListing
				{
					category = ShopItemCategory.Potion,
					grade = ShopItemGrade.Common,
					fallbackPotionType = type,
					fallbackPotionName = ShopPotionDefaults.GetDisplayName(type),
					price = ShopPotionDefaults.GetDefaultPrice(type),
				});
			}
		}

		result.Sort(CompareListings);
		return result;
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

	static List<AccessoryData> BuildAccessoryPool(ShopCatalogSettings settings)
	{
		var pool = new List<AccessoryData>();

		if (settings.accessoryPool != null)
		{
			foreach (AccessoryData data in settings.accessoryPool)
			{
				if (data != null)
					pool.Add(data);
			}
		}

		if (pool.Count == 0)
		{
			RewardCatalogSettings catalog = RewardCatalogSettings.Load();
			if (catalog?.allAccessories != null)
				pool.AddRange(catalog.allAccessories);
		}

		pool.RemoveAll(a => a == null);
		return pool;
	}

	static List<PotionData> BuildPotionPool(ShopCatalogSettings settings)
	{
		var pool = new List<PotionData>();

		if (settings.potionPool != null)
		{
			foreach (PotionData data in settings.potionPool)
			{
				if (data != null)
					pool.Add(data);
			}
		}

		if (pool.Count == 0)
		{
			PotionData[] fromResources = Resources.LoadAll<PotionData>("Data/Potion");
			if (fromResources != null && fromResources.Length > 0)
				pool.AddRange(fromResources);
		}

		pool.RemoveAll(p => p == null);
		return pool;
	}

	static ShopItemGrade ToShopGrade(AccessoryGrade grade) => grade switch
	{
		AccessoryGrade.Rare => ShopItemGrade.Rare,
		AccessoryGrade.Unique => ShopItemGrade.Unique,
		AccessoryGrade.Legendary => ShopItemGrade.Legendary,
		_ => ShopItemGrade.Common
	};

	static AccessoryGrade ToAccessoryGrade(ShopItemGrade grade) => grade switch
	{
		ShopItemGrade.Rare => AccessoryGrade.Rare,
		ShopItemGrade.Unique => AccessoryGrade.Unique,
		ShopItemGrade.Legendary => AccessoryGrade.Legendary,
		_ => AccessoryGrade.Common
	};

	static string PickWeaponId(List<string> pool, ShopItemGrade targetGrade)
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
			filtered.AddRange(pool);

		return filtered[UnityEngine.Random.Range(0, filtered.Count)];
	}

	static AccessoryData PickAccessory(
		List<AccessoryData> pool,
		ShopItemGrade targetGrade,
		HashSet<string> excludeNames)
	{
		if (pool == null || pool.Count == 0)
			return null;

		AccessoryGrade accessoryGrade = ToAccessoryGrade(targetGrade);
		var filtered = new List<AccessoryData>();
		foreach (AccessoryData data in pool)
		{
			if (data == null || excludeNames.Contains(data.name))
				continue;

			if (data.grade == accessoryGrade)
				filtered.Add(data);
		}

		if (filtered.Count == 0)
		{
			foreach (AccessoryData data in pool)
			{
				if (data != null && !excludeNames.Contains(data.name))
					filtered.Add(data);
			}
		}

		if (filtered.Count == 0)
			return null;

		return filtered[UnityEngine.Random.Range(0, filtered.Count)];
	}

	static void Shuffle<T>(List<T> list)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = UnityEngine.Random.Range(0, i + 1);
			(list[i], list[j]) = (list[j], list[i]);
		}
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
