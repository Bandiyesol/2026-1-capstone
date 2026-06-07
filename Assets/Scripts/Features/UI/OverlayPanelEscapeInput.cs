using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Status / Inventory / Shop / Settings 패널 — Esc 닫기, 6 상점 Toggle.</summary>
public class OverlayPanelEscapeInput : MonoBehaviour
{
	void Update()
	{
		TryToggleShopWithKeyboard();

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

	static void TryToggleShopWithKeyboard()
	{
		Keyboard keyboard = Keyboard.current;
		if (keyboard == null || !keyboard.digit6Key.wasPressedThisFrame)
			return;

		ShopUI shop = Object.FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
		bool shopOpen = shop != null && shop.IsPanelOpen;

		if (!shopOpen && (GameManager.instance == null || !GameManager.instance.isLive))
			return;

		shop = ShopUIBootstrap.EnsureShopUI();
		shop?.Toggle();
	}
}
