using UnityEngine;

public static class ShopService
{
	public static bool TryPurchase(ShopListing listing, out string message)
	{
		message = string.Empty;

		if (listing == null)
		{
			message = "잘못된 상품입니다.";
			return false;
		}

		if (listing.soldOut)
		{
			message = "이미 판매된 상품입니다.";
			return false;
		}

		if (GameManager.instance == null)
		{
			message = "게임 상태를 확인할 수 없습니다.";
			return false;
		}

		if (!GameManager.instance.TrySpendCoin(listing.price))
		{
			message = "코인이 부족합니다.";
			return false;
		}

		switch (listing.category)
		{
			case ShopItemCategory.Weapon:
				return TryPurchaseWeapon(listing, out message);

			case ShopItemCategory.Accessory:
				message = "악세서리 상점은 준비 중입니다.";
				GameManager.instance.AddCoin(listing.price);
				return false;

			case ShopItemCategory.Potion:
				message = "물약 상점은 준비 중입니다.";
				GameManager.instance.AddCoin(listing.price);
				return false;

			default:
				message = "구매할 수 없는 상품입니다.";
				GameManager.instance.AddCoin(listing.price);
				return false;
		}
	}

	static bool TryPurchaseWeapon(ShopListing listing, out string message)
	{
		message = string.Empty;

		if (listing.weapon == null)
		{
			message = "무기 정보가 없습니다.";
			Refund(listing.price);
			return false;
		}

		WeaponInventory inventory = Object.FindFirstObjectByType<WeaponInventory>();
		if (inventory == null)
		{
			message = "무기 인벤토리를 찾을 수 없습니다.";
			Refund(listing.price);
			return false;
		}

		if (!inventory.TryAdd(listing.weapon))
		{
			message = "무기 슬롯이 가득 찼습니다.";
			Refund(listing.price);
			return false;
		}

		listing.soldOut = true;
		message = $"{listing.weapon.info.name} 구매 완료!";
		return true;
	}

	static void Refund(int amount)
	{
		if (amount > 0 && GameManager.instance != null)
			GameManager.instance.AddCoin(amount);
	}
}
