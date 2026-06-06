using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Status / Inventory / Shop / Settings 패널에서 Esc → 닫기 버튼과 동일.</summary>
public class OverlayPanelEscapeInput : MonoBehaviour
{
	void Update()
	{
		if (!WasEscapePressedThisFrame())
			return;

		SettingsUI settings = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
		if (settings != null && settings.TryHandleEscape())
			return;

		ShopUI shop = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
		if (shop != null && shop.TryHandleEscape())
			return;

		InventoryUI inventory = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
		if (inventory != null && inventory.TryHandleEscape())
			return;

		StatusUI status = FindFirstObjectByType<StatusUI>(FindObjectsInactive.Include);
		if (status != null && status.TryHandleEscape())
			return;
	}

	static bool WasEscapePressedThisFrame()
	{
		Keyboard keyboard = Keyboard.current;
		return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
	}
}
