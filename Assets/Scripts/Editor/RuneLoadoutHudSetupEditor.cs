#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>인게임 HUD 룬 정보 버튼 + 읽기 전용 룬 순서 패널을 씬에 배치합니다.</summary>
public static class RuneLoadoutHudSetupEditor
{
	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";
	const string RuneIconAssetPath = "Assets/Arts/UI/Vol 6 Ui Expansion Pack/Runes/Runes_13_01.png";
	const float HudButtonGap = 50f;

	[MenuItem("Tools/UI/Setup Rune Loadout HUD")]
	public static void SetupFromMenu()
	{
		if (Application.isPlaying)
		{
			EditorUtility.DisplayDialog("Rune Loadout HUD", "플레이 모드에서는 실행할 수 없습니다.", "확인");
			return;
		}

		Scene scene = EditorSceneManager.GetActiveScene();
		if (scene.path != ScenePath)
			scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		if (!Apply(saveScene: true))
		{
			EditorUtility.DisplayDialog("Rune Loadout HUD", "Setting 버튼 또는 RuneSelectUI를 찾지 못했습니다.", "확인");
			return;
		}

		EditorUtility.DisplayDialog("Rune Loadout HUD", "룬 정보 HUD 버튼과 패널을 설정했습니다.", "확인");
	}

	public static bool Apply(bool saveScene)
	{
		Button settingButton = FindHudButton("Setting");
		RuneSelectUI runeSelect = Object.FindFirstObjectByType<RuneSelectUI>(FindObjectsInactive.Include);
		Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
		if (settingButton == null || runeSelect == null || canvas == null)
			return false;

		EnsureHudButton(settingButton);
		if (EnsureViewPanel(canvas.transform, runeSelect.transform) == null
		    && Object.FindFirstObjectByType<RuneLoadoutViewUI>(FindObjectsInactive.Include) == null)
			return false;

		RuneLoadoutViewUI viewUi = Object.FindFirstObjectByType<RuneLoadoutViewUI>(FindObjectsInactive.Include);
		if (viewUi != null)
		{
			SerializedObject viewSo = new SerializedObject(viewUi);
			GameObject panelRef = viewSo.FindProperty("panel").objectReferenceValue as GameObject;
			if (panelRef != null)
			{
				CleanupReadOnlyPanel(panelRef.transform);
				EnsureInfoText(panelRef.transform);
			}

			ChoiceSelectUILayout.Apply(viewUi.transform);
			EditorUtility.SetDirty(viewUi);
		}

		if (saveScene)
		{
			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
			EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
		}

		return true;
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

	static void EnsureHudButton(Button settingButton)
	{
		Transform parent = settingButton.transform.parent;
		Button runeButton = FindHudButton("RuneLoadout");
		if (runeButton == null)
		{
			GameObject clone = Object.Instantiate(settingButton.gameObject, parent);
			clone.name = "RuneLoadout";
			Undo.RegisterCreatedObjectUndo(clone, "Create RuneLoadout HUD Button");
			runeButton = clone.GetComponent<Button>();
		}

		if (runeButton.GetComponent<RuneLoadoutHudButton>() == null)
			Undo.AddComponent<RuneLoadoutHudButton>(runeButton.gameObject);

		Sprite hudIcon = LoadRuneHudIcon();
		EnsureIconChild(runeButton.transform, hudIcon);
		RuneLoadoutHudButton hud = runeButton.GetComponent<RuneLoadoutHudButton>();
		if (hud != null)
		{
			SerializedObject hudSo = new SerializedObject(hud);
			hudSo.FindProperty("hudIcon").objectReferenceValue = hudIcon;
			hudSo.ApplyModifiedPropertiesWithoutUndo();
		}

		if (settingButton.GetComponent<SettingsHudButton>() != null
		    && runeButton.GetComponent<SettingsHudButton>() != null)
			Object.DestroyImmediate(runeButton.GetComponent<SettingsHudButton>());

		if (runeButton.transform is RectTransform runeRect && settingButton.transform is RectTransform settingRect)
		{
			runeRect.anchorMin = settingRect.anchorMin;
			runeRect.anchorMax = settingRect.anchorMax;
			runeRect.pivot = settingRect.pivot;
			runeRect.sizeDelta = settingRect.sizeDelta;
			runeRect.anchoredPosition = new Vector2(
				settingRect.anchoredPosition.x - settingRect.sizeDelta.x - HudButtonGap,
				settingRect.anchoredPosition.y);
		}

		EnsureIconChild(runeButton.transform, LoadRuneHudIcon());
		EditorUtility.SetDirty(runeButton.gameObject);
	}

	static void EnsureIconChild(Transform buttonRoot, Sprite icon)
	{
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

		if (iconTransform.TryGetComponent(out Image image) && icon != null)
		{
			image.sprite = icon;
			image.preserveAspect = true;
			image.raycastTarget = false;
		}
	}

	static RuneLoadoutViewUI EnsureViewPanel(Transform canvas, Transform runeSelectRoot)
	{
		RuneLoadoutViewUI existing = Object.FindFirstObjectByType<RuneLoadoutViewUI>(FindObjectsInactive.Include);
		if (existing != null)
			return existing;

		Transform loadoutTemplate = runeSelectRoot.Find("LoadoutPanel");
		if (loadoutTemplate == null)
			return null;

		var panelRoot = new GameObject("RuneLoadoutViewPanel", typeof(RectTransform));
		Undo.RegisterCreatedObjectUndo(panelRoot, "Create RuneLoadoutViewPanel");
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
		Undo.RegisterCreatedObjectUndo(loadoutClone, "Clone Rune Loadout Panel");

		CleanupReadOnlyPanel(loadoutClone.transform);
		EnsureInfoText(loadoutClone.transform);
		EnsureCloseButton(loadoutClone.transform);

		foreach (Button slotButton in loadoutClone.GetComponentsInChildren<Button>(true))
		{
			if (slotButton.gameObject.name.StartsWith("Btn"))
				slotButton.interactable = false;
		}

		var viewUi = panelRoot.AddComponent<RuneLoadoutViewUI>();
		SerializedObject so = new SerializedObject(viewUi);
		so.FindProperty("panel").objectReferenceValue = loadoutClone;
		so.ApplyModifiedPropertiesWithoutUndo();

		panelRoot.SetActive(false);
		return viewUi;
	}

	static void EnsureInfoText(Transform panelRoot)
	{
		Transform existing = panelRoot.Find("InfoText");
		GameObject go;
		if (existing != null)
		{
			go = existing.gameObject;
		}
		else
		{
			go = new GameObject("InfoText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
			go.transform.SetParent(panelRoot, false);
		}

		var rect = go.GetComponent<RectTransform>();
		rect.anchorMin = new Vector2(0.5f, 0f);
		rect.anchorMax = new Vector2(0.5f, 0f);
		rect.pivot = new Vector2(0.5f, 0f);
		rect.anchoredPosition = new Vector2(0f, 168f);
		rect.sizeDelta = new Vector2(900f, 56f);

		var tmp = go.GetComponent<TextMeshProUGUI>();
		tmp.fontSize = 30f;
		tmp.alignment = TextAlignmentOptions.Center;
		tmp.text = "현재 장착 순서입니다.";
	}

	static void EnsureCloseButton(Transform panelRoot)
	{
		Transform box = OverlayPanelUILayout.FindBoxPanel(panelRoot);
		if (box == null)
			return;

		if (box.Find("CloseBtn") != null || box.Find("Close") != null)
			return;

		StatusUI status = Object.FindFirstObjectByType<StatusUI>(FindObjectsInactive.Include);
		if (status == null)
			return;

		SerializedObject statusSo = new SerializedObject(status);
		Button template = statusSo.FindProperty("closeButton").objectReferenceValue as Button;
		if (template == null)
			return;

		GameObject clone = Object.Instantiate(template.gameObject, box);
		clone.name = "CloseBtn";
		Undo.RegisterCreatedObjectUndo(clone, "Create Rune Loadout Close Button");
	}

	static void DestroyIfPresent(Transform root, string childName)
	{
		foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
		{
			if (child != null && child.name == childName)
				Object.DestroyImmediate(child.gameObject);
		}
	}

	static void HideTransform(Transform target)
	{
		if (target != null)
			target.gameObject.SetActive(false);
	}

	static void CleanupReadOnlyPanel(Transform panelRoot)
	{
		DestroyIfPresent(panelRoot, "StartButton");
		DestroyIfPresent(panelRoot, "WarningText");

		foreach (Button button in panelRoot.GetComponentsInChildren<Button>(true))
		{
			if (button == null)
				continue;

			Transform label = button.transform.Find("Label");
			if (label != null && label.TryGetComponent(out TextMeshProUGUI tmp)
			    && tmp.text.Equals("Start", System.StringComparison.OrdinalIgnoreCase))
				Object.DestroyImmediate(button.gameObject);
		}

		Transform box = OverlayPanelUILayout.FindBoxPanel(panelRoot);
		if (box == null)
			return;

		for (int i = 0; i < 3; i++)
		{
			Transform btn = box.Find($"Btn{i}");
			if (btn == null)
				continue;

			if (btn.TryGetComponent(out Image cardImage))
			{
				Color color = cardImage.color;
				color.a = 1f;
				cardImage.color = color;
			}

			if (btn.TryGetComponent(out Button button))
			{
				button.interactable = false;
				ColorBlock colors = button.colors;
				colors.normalColor = Color.white;
				colors.highlightedColor = Color.white;
				colors.pressedColor = Color.white;
				colors.selectedColor = Color.white;
				colors.disabledColor = Color.white;
				button.colors = colors;
			}
		}

		TMP_FontAsset koreanFont = TmpKoreanFontUtility.ResolveNeoDgmFont(null);
		foreach (TextMeshProUGUI tmp in panelRoot.GetComponentsInChildren<TextMeshProUGUI>(true))
		{
			if (tmp.transform.name == "WarningText")
			{
				tmp.gameObject.SetActive(false);
				continue;
			}

			TmpKoreanFontUtility.ApplyFont(tmp, koreanFont);
		}
	}

	static Sprite LoadRuneHudIcon()
	{
		Object[] assets = AssetDatabase.LoadAllAssetsAtPath(RuneIconAssetPath);
		foreach (Object asset in assets)
		{
			if (asset is Sprite sprite)
				return sprite;
		}

		return null;
	}
}
#endif
