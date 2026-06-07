using System;
using UnityEngine;

/// <summary>상점 슬롯 1칸 UI 데이터.</summary>
public class ShopSlotViewData
{
	public Sprite icon;
	public string tooltip;
	public int price;
	public bool soldOut;
	public Action onPurchase;
}
