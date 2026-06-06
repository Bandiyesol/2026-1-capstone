/// <summary>ShopItemGrade ↔ WeaponInfo.grade / balanceKey 변환.</summary>
public static class ShopGradeUtility
{
	public static string ToGradeName(ShopItemGrade grade)
	{
		switch (grade)
		{
			case ShopItemGrade.Rare: return "Rare";
			case ShopItemGrade.Unique: return "Unique";
			case ShopItemGrade.Legendary: return "Legendary";
			default: return "Common";
		}
	}

	public static ShopItemGrade Parse(string gradeName)
	{
		if (string.IsNullOrEmpty(gradeName))
			return ShopItemGrade.Common;

		switch (gradeName.Trim().ToLowerInvariant())
		{
			case "rare": return ShopItemGrade.Rare;
			case "unique": return ShopItemGrade.Unique;
			case "legendary":
			case "legend": return ShopItemGrade.Legendary;
			default: return ShopItemGrade.Common;
		}
	}

	public static string BuildBalanceKey(string weaponType, ShopItemGrade grade)
	{
		if (string.IsNullOrEmpty(weaponType))
			return string.Empty;

		return $"{weaponType}_{ToGradeName(grade)}";
	}
}
