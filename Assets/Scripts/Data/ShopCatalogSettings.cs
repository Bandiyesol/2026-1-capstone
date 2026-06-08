using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
	fileName = "ShopCatalogSettings",
	menuName = "Scriptable/Shop Catalog Settings")]
public class ShopCatalogSettings : ScriptableObject
{
	[Header("카테고리별 진열 개수")]
	[Min(0)]
	public int weaponListingCount = 5;

	[Min(0)]
	public int accessoryListingCount = 5;

	[Min(0)]
	public int potionListingCount = 5;

	[Header("등급 출현 가중치 (무기·악세서리 진열)")]
	public ShopGradeWeight[] gradeWeights =
	{
		new ShopGradeWeight { grade = ShopItemGrade.Common, weight = 55f },
		new ShopGradeWeight { grade = ShopItemGrade.Rare, weight = 28f },
		new ShopGradeWeight { grade = ShopItemGrade.Unique, weight = 12f },
		new ShopGradeWeight { grade = ShopItemGrade.Legendary, weight = 5f },
	};

	[Header("등급별 판매 가격 (무기·악세서리)")]
	public ShopGradePrice[] gradePrices =
	{
		new ShopGradePrice { grade = ShopItemGrade.Common, price = 30 },
	};

	[Header("무기 풀 (비우면 WeaponManager 전체)")]
	public string[] weaponIdPool;

	[Header("악세서리·물약 풀 (비우면 RewardCatalog / Resources 자동)")]
	public List<AccessoryData> accessoryPool = new List<AccessoryData>();
	public List<PotionData> potionPool = new List<PotionData>();

	[Min(0)]
	public int potionDefaultPrice = 35;

	[Header("상인")]
	public Sprite merchantPortrait;
	[TextArea(2, 4)]
	public string[] merchantDialogues;

	[Header("진열 리롤")]
	[Tooltip("상점을 열 때 리롤 비용. 이후 리롤마다 2배씩 증가합니다.")]
	[Min(0)]
	public int rerollCost = 10;

	public int GetPrice(ShopItemGrade grade)
	{
		if (gradePrices != null)
		{
			foreach (ShopGradePrice entry in gradePrices)
			{
				if (entry != null && entry.grade == grade && entry.price > 0)
					return entry.price;
			}
		}

		return DefaultPriceForGrade(grade);
	}

	public static int DefaultPriceForGrade(ShopItemGrade grade) => grade switch
	{
		ShopItemGrade.Rare => 60,
		ShopItemGrade.Unique => 90,
		ShopItemGrade.Legendary => 150,
		_ => 30
	};

	public float GetTotalGradeWeight()
	{
		float total = 0f;
		if (gradeWeights == null)
			return total;

		foreach (ShopGradeWeight entry in gradeWeights)
		{
			if (entry != null && entry.weight > 0f)
				total += entry.weight;
		}

		return total;
	}

	public ShopItemGrade RollListingGrade()
	{
		if (gradeWeights == null || gradeWeights.Length == 0)
			return ShopItemGrade.Common;

		float total = GetTotalGradeWeight();
		if (total <= 0f)
			return ShopItemGrade.Common;

		float roll = UnityEngine.Random.Range(0f, total);
		foreach (ShopGradeWeight entry in gradeWeights)
		{
			if (entry == null || entry.weight <= 0f)
				continue;

			if (roll < entry.weight)
				return entry.grade;

			roll -= entry.weight;
		}

		return ShopItemGrade.Common;
	}

	public float GetGradeWeight(ShopItemGrade grade)
	{
		if (gradeWeights == null)
			return 0f;

		foreach (ShopGradeWeight entry in gradeWeights)
		{
			if (entry != null && entry.grade == grade)
				return Mathf.Max(0f, entry.weight);
		}

		return 0f;
	}

	static ShopCatalogSettings cached;

	public static ShopCatalogSettings Instance
	{
		get
		{
			if (cached == null)
				cached = Resources.Load<ShopCatalogSettings>("Data/ShopCatalogSettings");

			return cached;
		}
	}
}

[Serializable]
public class ShopGradeWeight
{
	public ShopItemGrade grade = ShopItemGrade.Common;
	[Min(0f)]
	public float weight = 1f;
}

[Serializable]
public class ShopGradePrice
{
	public ShopItemGrade grade = ShopItemGrade.Common;
	[Min(0)]
	public int price = 50;
}
