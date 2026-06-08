using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>상점 슬롯 — 마우스 오버 툴팁, 클릭 구매.</summary>
[RequireComponent(typeof(UnityEngine.UI.Image))]
public class ShopSlotInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	string tooltipText;
	bool canPurchase;
	System.Action onPurchase;

	public void Setup(string tooltip, int price, bool soldOut, System.Action purchaseCallback)
	{
		tooltipText = tooltip;
		canPurchase = !soldOut && purchaseCallback != null;
		onPurchase = purchaseCallback;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (string.IsNullOrEmpty(tooltipText))
			return;

		ShopUI.ShowTooltipStatic(tooltipText, transform);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		ShopUI.HideTooltipStatic();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (!canPurchase)
			return;

		onPurchase?.Invoke();
	}

	void OnDisable()
	{
		ShopUI.HideTooltipStatic();
	}
}
