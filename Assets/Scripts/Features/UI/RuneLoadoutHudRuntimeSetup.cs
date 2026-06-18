using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬에 룬 순서 HUD가 없을 때 런타임에 버튼·패널을 복구합니다.
/// </summary>
public static class RuneLoadoutHudRuntimeSetup
{
	const float HudButtonGap = 50f;
	static bool ensured;

	public static void Ensure()
	{
		if (ensured)
			return;

		ensured = true;

		Button runeButton = FindHudButton("RuneLoadout");
		RuneLoadoutViewUI viewUi = Object.FindFirstObjectByType<RuneLoadoutViewUI>(FindObjectsInactive.Include);
		if (runeButton != null && viewUi != null)
			return;

		Button settingButton = FindHudButton("Setting");
		RuneSelectUI runeSelect = Object.FindFirstObjectByType<RuneSelectUI>(FindObjectsInactive.Include);
		Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
		if (settingButton == null || runeSelect == null || canvas == null)
		{
			Debug.LogWarning("[RuneLoadoutHudRuntimeSetup] Setting 버튼 또는 RuneSelectUI를 찾지 못했습니다.");
			return;
		}

		Sprite hudIcon = LoadRuneHudIcon();
		if (hudIcon == null)
			hudIcon = GetIconFromButton(settingButton);

		if (runeButton == null)
			runeButton = CreateHudButton(settingButton, hudIcon);

		if (viewUi == null)
			viewUi = CreateViewPanel(canvas.transform, runeSelect.transform);

		if (runeButton != null && viewUi != null)
			Debug.Log("[RuneLoadoutHudRuntimeSetup] 룬 순서 HUD를 런타임에 복구했습니다.");
	}

	static Button FindHudButton(string objectName)
	{
		foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (button.name != objectName)
				continue;

			if (button.GetComponentInParent<Canvas>() == null)
				continue;

			return button;
		}

		return null;
	}

	static Button CreateHudButton(Button settingButton, Sprite hudIcon)
	{
		Transform parent = settingButton.transform.parent;
		GameObject clone = Object.Instantiate(settingButton.gameObject, parent);
		clone.name = "RuneLoadout";

		if (clone.TryGetComponent(out SettingsHudButton settingsHud))
			Object.Destroy(settingsHud);

		RuneLoadoutHudButton hud = clone.GetComponent<RuneLoadoutHudButton>();
		if (hud == null)
			hud = clone.AddComponent<RuneLoadoutHudButton>();

		hud.ConfigureIcon(hudIcon);
		EnsureIconChild(clone.transform, hudIcon);

		Transform iconChild = clone.transform.Find("Image");
		if (iconChild != null && iconChild.name != "Icon")
			iconChild.name = "Icon";

		if (clone.transform is RectTransform runeRect && settingButton.transform is RectTransform settingRect)
		{
			runeRect.anchorMin = settingRect.anchorMin;
			runeRect.anchorMax = settingRect.anchorMax;
			runeRect.pivot = settingRect.pivot;
			runeRect.sizeDelta = settingRect.sizeDelta;
			runeRect.anchoredPosition = new Vector2(
				settingRect.anchoredPosition.x - settingRect.sizeDelta.x - HudButtonGap,
				settingRect.anchoredPosition.y);
		}

		clone.SetActive(true);
		return clone.GetComponent<Button>();
	}

	static RuneLoadoutViewUI CreateViewPanel(Transform canvas, Transform runeSelectRoot)
	{
		Transform loadoutTemplate = runeSelectRoot.Find("LoadoutPanel");
		if (loadoutTemplate == null)
			return null;

		var panelRoot = new GameObject("RuneLoadoutViewPanel", typeof(RectTransform));
		panelRoot.transform.SetParent(canvas, false);

		if (panelRoot.transform is RectTransform panelRect)
		{
			panelRect.anchorMin = Vector2.zero;
			panelRect.anchorMax = Vector2.one;
			panelRect.offsetMin = Vector2.zero;
			panelRect.offsetMax = Vector2.zero;
		}

		GameObject loadoutClone = Object.Instantiate(loadoutTemplate.gameObject, panelRoot.transform);
		loadoutClone.name = "LoadoutPanel";
		PrepareReadOnlyPanel(loadoutClone.transform);

		var viewUi = panelRoot.AddComponent<RuneLoadoutViewUI>();
		panelRoot.SetActive(false);
		return viewUi;
	}

	static void PrepareReadOnlyPanel(Transform panelRoot)
	{
		foreach (Transform child in panelRoot.GetComponentsInChildren<Transform>(true))
		{
			if (child == null)
				continue;

			if (child.name is "StartButton" or "WarningText")
				Object.Destroy(child.gameObject);
		}

		foreach (Button button in panelRoot.GetComponentsInChildren<Button>(true))
		{
			if (button == null)
				continue;

			Transform label = button.transform.Find("Label");
			if (label != null && label.TryGetComponent(out TMPro.TextMeshProUGUI tmp)
			    && tmp.text.Equals("Start", System.StringComparison.OrdinalIgnoreCase))
			{
				Object.Destroy(button.gameObject);
				continue;
			}

			if (button.gameObject.name.StartsWith("Btn"))
				button.interactable = false;
		}
	}

	static void EnsureIconChild(Transform buttonRoot, Sprite icon)
	{
		if (icon == null)
			return;

		Transform iconTransform = buttonRoot.Find("Icon") ?? buttonRoot.Find("Image");
		if (iconTransform == null)
		{
			var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			go.transform.SetParent(buttonRoot, false);
			var rect = go.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.anchoredPosition = new Vector2(0f, 12f);
			rect.sizeDelta = new Vector2(56f, 56f);
			iconTransform = go.transform;
		}

		if (iconTransform.TryGetComponent(out Image image))
		{
			image.sprite = icon;
			image.preserveAspect = true;
			image.raycastTarget = false;
		}
	}

	static Sprite GetIconFromButton(Button button)
	{
		Transform iconTransform = button.transform.Find("Icon") ?? button.transform.Find("Image");
		return iconTransform != null && iconTransform.TryGetComponent(out Image image) ? image.sprite : null;
	}

	static Sprite LoadRuneHudIcon()
	{
		return UiRuntimeAssets.LoadRuneHudIcon();
	}
}
