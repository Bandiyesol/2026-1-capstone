using System;
using System.Collections.Generic;
using UnityEngine;

public static class ShopDisplayService
{
	public static List<ShopSlotViewData> BuildSlots(
		IReadOnlyList<ShopListing> listings,
		Action<ShopListing> onPurchase)
	{
		var result = new List<ShopSlotViewData>();
		if (listings == null)
			return result;

		foreach (ShopListing listing in listings)
		{
			if (listing == null)
				continue;

			ShopListing captured = listing;
			result.Add(new ShopSlotViewData
			{
				icon = listing.GetIcon(),
				tooltip = listing.GetTooltip(),
				price = listing.price,
				soldOut = listing.soldOut,
				onPurchase = () => onPurchase?.Invoke(captured),
			});
		}

		return result;
	}
}
