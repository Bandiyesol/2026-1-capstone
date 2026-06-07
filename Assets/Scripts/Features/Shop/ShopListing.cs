using UnityEngine;

/// <summary>상점 진열 1칸 런타임 데이터.</summary>
public class ShopListing
{
	public ShopItemCategory category;
	public ShopItemGrade grade;
	public int price;
	public bool soldOut;

	public WeaponInstance weapon;
	public AccessoryData accessory;
	public PotionData potion;
	public PotionType fallbackPotionType;
	public string fallbackPotionName;

	public string DisplayName
	{
		get
		{
			if (weapon?.info != null)
				return weapon.info.name;
			if (accessory != null)
				return accessory.displayName;
			if (potion != null)
				return potion.potionName;
			if (!string.IsNullOrEmpty(fallbackPotionName))
				return fallbackPotionName;

			return string.Empty;
		}
	}

	public Sprite GetIcon()
	{
		switch (category)
		{
			case ShopItemCategory.Weapon when weapon != null:
				return WeaponRewardService.GetIcon(weapon);
			case ShopItemCategory.Accessory when accessory != null:
				return AccessoryIconResolver.Resolve(accessory);
			case ShopItemCategory.Potion:
				if (potion?.icon != null)
					return potion.icon;
				return null;
			default:
				return null;
		}
	}

	public string GetTooltip()
	{
		if (soldOut)
			return "품절";

		switch (category)
		{
			case ShopItemCategory.Weapon when weapon != null:
				return $"{WeaponRewardService.FormatTitle(weapon)}\n{WeaponRewardService.FormatChoiceDetail(weapon)}\n\n클릭하여 구매 ({price}G)";

			case ShopItemCategory.Accessory when accessory != null:
				return BuildAccessoryTooltip(accessory);

			case ShopItemCategory.Potion:
				return BuildPotionTooltip();

			default:
				return string.Empty;
		}
	}

	static string BuildAccessoryTooltip(AccessoryData data)
	{
		var sb = new System.Text.StringBuilder(256);
		sb.AppendLine(data.displayName);
		sb.AppendLine(ChoiceGradeDisplay.FormatGradeLine(data.grade.ToString(), data.accessoryType));
		if (!string.IsNullOrEmpty(data.description))
			sb.AppendLine(data.description);
		return sb.ToString().TrimEnd();
	}

	string BuildPotionTooltip()
	{
		var sb = new System.Text.StringBuilder(192);
		if (potion != null)
		{
			sb.AppendLine(potion.potionName);
			if (!string.IsNullOrEmpty(potion.description))
				sb.AppendLine(potion.description);
		}
		else
		{
			sb.AppendLine(fallbackPotionName);
			sb.AppendLine(ShopPotionDefaults.GetDescription(fallbackPotionType));
		}

		sb.AppendLine($"\n클릭하여 구매 ({price}G)");
		return sb.ToString().TrimEnd();
	}
}
