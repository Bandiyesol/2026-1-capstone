using UnityEngine;

/// <summary>상점 진열 1칸 런타임 데이터.</summary>
public class ShopListing
{
	public ShopItemCategory category;
	public ShopItemGrade grade;
	public int price;
	public bool soldOut;

	public WeaponInstance weapon;
	// public AccessoryData accessory; // 추후
	// public PotionStack potion;      // 추후

	public string DisplayName
	{
		get
		{
			if (weapon?.info != null)
				return weapon.info.name;

			return string.Empty;
		}
	}

	public Sprite GetIcon()
	{
		if (category == ShopItemCategory.Weapon && weapon != null)
			return WeaponRewardService.GetIcon(weapon);

		return null;
	}

	public string GetTooltip()
	{
		if (soldOut)
			return "품절";

		switch (category)
		{
			case ShopItemCategory.Weapon when weapon != null:
				return $"{WeaponRewardService.FormatTitle(weapon)}\n{WeaponRewardService.FormatStats(weapon)}\n\n클릭하여 구매 ({price}G)";
			default:
				return string.Empty;
		}
	}
}
