using System;
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

	[Header("등급 출현 가중치 (추후 확장용 — 현재는 WeaponInfo 등급 사용)")]
	public ShopGradeWeight[] gradeWeights =
	{
		new ShopGradeWeight { grade = ShopItemGrade.Common, weight = 1f },
	};

	[Header("등급별 판매 가격 (무기·악세서리)")]
	public ShopGradePrice[] gradePrices =
	{
		new ShopGradePrice { grade = ShopItemGrade.Common, price = 30 },
	};

	[Header("무기 풀 (비우면 WeaponManager 전체)")]
	public string[] weaponIdPool;

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
		if (gradePrices == null)
			return 0;

		foreach (ShopGradePrice entry in gradePrices)
		{
			if (entry != null && entry.grade == grade)
				return Mathf.Max(0, entry.price);
		}

		return 0;
	}

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
