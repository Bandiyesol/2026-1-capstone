using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 슬롯 Image 에 붙여 마우스 오버 시 InventoryUI 툴팁을 띄웁니다.
/// </summary>
[RequireComponent(typeof(UnityEngine.UI.Image))]
public class InventorySlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	string tooltipText;

	public void SetTooltip(string text)
	{
		tooltipText = text;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (string.IsNullOrEmpty(tooltipText))
			return;

		InventoryUI.ShowTooltipStatic(tooltipText, transform);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		InventoryUI.HideTooltipStatic();
	}

	void OnDisable()
	{
		InventoryUI.HideTooltipStatic();
	}
}
