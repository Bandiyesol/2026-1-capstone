#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 로그인·설정·메인스토리·보스알리미 등 오버레이 메뉴 UI 크기/입력창 가독성 조정.
/// Tools/UI/Polish Overlay Menu Layout
/// </summary>
public static class OverlayMenuUiLayoutEditor
{
	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";
	const string Vol6Root = "Assets/Arts/UI/Vol 6 Ui Expansion Pack";

	static readonly string[] MenuStyleRootNames =
	{
		"AuthScreen",
		"LoginPanel",
		"SignUpPanel",
		"ForgotPasswordPanel",
		"SettingPanel",
		"MainStoryPanel",
		"EndingStoryPanel",
		"BossAlarmPanel",
		"LoadoutPanel",
		"GameRecordPanel",
	};

	static readonly Vector2 ButtonSize = new(340f, 96f);
	static readonly Vector2 AuthButtonSize = new(340f, 96f);
	static readonly Vector2 AuthWideButtonSize = new(400f, 96f);
	static readonly Vector2 InputSize = new(500f, 64f);
	static readonly Color InputBackgroundColor = new(0.98f, 0.96f, 0.93f, 1f);
	static readonly Color InputTextColor = new(0.12f, 0.10f, 0.08f, 1f);
	static readonly Color AuthInputBackgroundColor = new(1f, 0.98f, 0.92f, 1f);
	static readonly Color AuthInputTextColor = new(0.35f, 0.28f, 0.20f, 1f);
	static readonly Color AuthPlaceholderColor = new(0.55f, 0.48f, 0.40f, 0.85f);
	static readonly Color SliderTrackColor = new(0.32f, 0.26f, 0.20f, 0.85f);
	static readonly Color DropdownArrowColor = new(0.72f, 0.58f, 0.28f, 1f);
	static readonly Vector2 DropdownSize = new(500f, 64f);

	static readonly List<string> Report = new();

	[MenuItem("Tools/UI/Polish Overlay Menu Layout")]
	[MenuItem("Window/The Last Rune/UI/Polish Overlay Menu Layout")]
	public static void ApplyFromMenu()
	{
		ApplyInternal(showDialog: true);
	}

	public static void ApplyInternal(bool showDialog)
	{
		Report.Clear();
		Sprite panelSprite = LoadSprite($"{Vol6Root}/Panels/Panels_06.png", "Panels_06_0");

		if (SceneManager.GetActiveScene().path != ScenePath)
			EditorSceneManager.OpenScene(ScenePath);

		HashSet<Transform> roots = CollectMenuStyleRoots();
		if (roots.Count == 0)
		{
			Debug.LogWarning("[OverlayMenuUiLayoutEditor] 대상 루트를 찾지 못했습니다.");
			return;
		}

		PolishInputs(roots, panelSprite);
		EnlargeButtons(roots);
		ApplyPanelLayoutOverrides();
		FixSettingSliders();
		StyleSettingDropdowns();
		FixLoadoutStartLabel();

		EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
		EditorSceneManager.SaveOpenScenes();

		string reportText = string.Join("\n", Report);
		Debug.Log($"[OverlayMenuUiLayoutEditor]\n{reportText}");

		if (showDialog)
		{
			EditorUtility.DisplayDialog(
				"오버레이 메뉴 UI",
				$"{Report.Count}개 항목의 크기·입력창 스타일을 조정했습니다.\n\nConsole 로그를 확인하세요.",
				"확인");
		}
	}

	static void PolishInputs(HashSet<Transform> roots, Sprite panelSprite)
	{
		Sprite inputPanel = LoadSprite($"{Vol6Root}/Buttons/Gold Buttons/Gold Btn C_02.png", "Gold Btn C_02_center")
		              ?? LoadSprite($"{Vol6Root}/Panels/Panels_03.png", "Panels_03_0")
		              ?? panelSprite;

		foreach (TMP_InputField input in Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (!IsUnderAnyRoot(input.transform, roots))
				continue;

			if (!IsUnderAuthPanel(input.transform))
				continue;

			if (input.TryGetComponent(out RectTransform rect))
			{
				rect.sizeDelta = InputSize;
				EditorUtility.SetDirty(rect);
			}

			if (input.TryGetComponent(out Image bg) && inputPanel != null)
			{
				bg.sprite = inputPanel;
				bg.type = Image.Type.Sliced;
				bg.color = AuthInputBackgroundColor;
				EditorUtility.SetDirty(bg);
			}

			input.pointSize = 24f;

			if (input.textComponent != null)
			{
				input.textComponent.fontSize = 24f;
				input.textComponent.color = AuthInputTextColor;
				input.textComponent.verticalAlignment = VerticalAlignmentOptions.Middle;
				EditorUtility.SetDirty(input.textComponent);
			}

			if (input.placeholder is TMP_Text placeholder)
			{
				placeholder.fontSize = 22f;
				placeholder.color = AuthPlaceholderColor;
				placeholder.verticalAlignment = VerticalAlignmentOptions.Middle;
				EditorUtility.SetDirty(placeholder);
			}

			if (input.textViewport != null && input.textViewport.TryGetComponent(out RectTransform viewport))
			{
				viewport.offsetMin = new Vector2(12f, 8f);
				viewport.offsetMax = new Vector2(-12f, -8f);
				EditorUtility.SetDirty(viewport);
			}

			Report.Add($"{GetPath(input.transform)} → 입력창 Panels_03 {InputSize.x}x{InputSize.y}");
		}
	}

	static bool IsUnderAuthPanel(Transform transform)
	{
		while (transform != null)
		{
			if (transform.name is "LoginPanel" or "SignUpPanel" or "ForgotPasswordPanel" or "DeleteAccountPanel")
				return true;
			transform = transform.parent;
		}

		return false;
	}

	static void EnlargeButtons(HashSet<Transform> roots)
	{
		foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (!IsUnderAnyRoot(button.transform, roots))
				continue;

			if (button.name.Contains("Close"))
				continue;

			if (!button.TryGetComponent(out RectTransform rect))
				continue;

			Vector2 previous = rect.sizeDelta;
			Vector2 targetSize = ResolveButtonSize(button.name, rect.sizeDelta);
			rect.sizeDelta = targetSize;
			EditorUtility.SetDirty(rect);

			foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
			{
				label.fontSize = Mathf.Max(label.fontSize, 28f);
				label.enableAutoSizing = false;
				if (button.name == "StartButton")
					label.color = new Color(0.1f, 0.08f, 0.06f, 1f);
				EditorUtility.SetDirty(label);
			}

			Report.Add($"{GetPath(button.transform)} → 버튼 {previous.x}x{previous.y} → {targetSize.x}x{targetSize.y}");
		}
	}

	static void ApplyPanelLayoutOverrides()
	{
		// 로그인 패널: 입력 2개 + 우측 로그인 버튼 + 하단 3버튼
		SetRect("LoginPanel/LoginIdInput", new Vector2(-210f, -95f), InputSize);
		SetRect("LoginPanel/LoginPasswordInput", new Vector2(-210f, -175f), InputSize);
		SetRect("LoginPanel/LoginButton", new Vector2(270f, -135f), AuthButtonSize);
		SetRect("LoginPanel/GoSignUpButton", new Vector2(-400f, -310f), AuthButtonSize);
		SetRect("LoginPanel/ForgotPasswordButton", new Vector2(-50f, -310f), AuthButtonSize);
		SetRect("LoginPanel/QuitButton", new Vector2(280f, -310f), AuthButtonSize);

		// 회원가입
		SetRect("SignUpPanel/SignUpUsernameInput", new Vector2(0f, -15f), InputSize);
		SetRect("SignUpPanel/SignUpEmailInput", new Vector2(0f, -95f), InputSize);
		SetRect("SignUpPanel/SignUpPasswordInput", new Vector2(0f, -175f), InputSize);
		SetRect("SignUpPanel/SignUpPasswordConfirmInput", new Vector2(0f, -255f), InputSize);
		SetRect("SignUpPanel/SignUpNicknameInput", new Vector2(0f, -335f), InputSize);
		SetRect("SignUpPanel/SignUpButton", new Vector2(-210f, -430f), AuthButtonSize);
		SetRect("SignUpPanel/BackToLoginButton", new Vector2(210f, -430f), AuthButtonSize);

		// 비밀번호 찾기 — 입력 아래 버튼 2개 (옆으로 붙이지 않음)
		SetRect("ForgotPasswordPanel/ForgotPasswordInput", new Vector2(0f, -100f), InputSize);
		SetRect("ForgotPasswordPanel/SendResetEmailButton", new Vector2(-190f, -210f), AuthWideButtonSize);
		SetRect("ForgotPasswordPanel/ForgotBackToLoginButton", new Vector2(190f, -210f), AuthButtonSize);
		FixSendResetEmailButtonLabel();
		ApplyAuthButtonLabelPadding();

		// 설정
		SetRect("SettingPanel/ScreenModeDropdown", new Vector2(100f, 200f), DropdownSize);
		SetRect("SettingPanel/ResolutionDropdown", new Vector2(100f, 100f), DropdownSize);
		SetRect("SettingPanel/MainMenuButton", new Vector2(-360f, 180f), ButtonSize);
		SetRect("SettingPanel/QuitButton", new Vector2(0f, 180f), ButtonSize);
		SetRect("SettingPanel/DeleteAccountButton", new Vector2(360f, 180f), ButtonSize);

		// 회원 탈퇴 확인
		SetRect("SettingPanel/BoxPanel/DeleteAccountPanel/DeleteAccountPasswordInput", new Vector2(0f, 100f), InputSize);
		SetRect("SettingPanel/BoxPanel/DeleteAccountPanel/DeleteAccountConfirmButton", new Vector2(-190f, -100f), ButtonSize);
		SetRect("SettingPanel/BoxPanel/DeleteAccountPanel/DeleteAccountCancelButton", new Vector2(190f, -100f), ButtonSize);

		// 메인 스토리 / 엔딩 / 보스 알리미 / 룬 순서
		SetRect("MainStoryPanel/SkipButton", new Vector2(-220f, 120f), ButtonSize);
		SetRect("EndingStoryPanel/SkipButton", new Vector2(-220f, 120f), ButtonSize);
		SetRect("BossAlarmPanel/BossAlarmContinueButton", new Vector2(-220f, 120f), ButtonSize);
		SetRect("LoadoutPanel/StartButton", new Vector2(0f, 149f), ButtonSize);

		// 플레이 기록
		FixGameRecordPanelLayout();
	}

	static void FixGameRecordPanelLayout()
	{
		const float buttonBottomReserve = 128f;
		const float titleTopReserve = 72f;
		const float horizontalPad = 24f;

		Transform scroll = FindByPath("GameRecordPanel/Window/RecordScrollView");
		if (scroll != null && scroll.TryGetComponent(out RectTransform scrollRect))
		{
			scrollRect.anchorMin = new Vector2(0f, 0f);
			scrollRect.anchorMax = new Vector2(1f, 1f);
			scrollRect.pivot = new Vector2(0.5f, 0.5f);
			scrollRect.anchoredPosition = Vector2.zero;
			scrollRect.sizeDelta = Vector2.zero;
			scrollRect.offsetMin = new Vector2(horizontalPad, buttonBottomReserve);
			scrollRect.offsetMax = new Vector2(-horizontalPad, -titleTopReserve);
			EditorUtility.SetDirty(scrollRect);
			Report.Add("GameRecordPanel/Window/RecordScrollView → 하단 버튼 여백 확보");
		}

		SetRect("GameRecordPanel/Window/ConfirmButton", new Vector2(0f, 52f), ButtonSize);
	}

	static Vector2 ResolveButtonSize(string buttonName, Vector2 current)
	{
		if (buttonName == "SendResetEmailButton")
			return AuthWideButtonSize;

		if (IsOverlayMenuButtonName(buttonName))
			return ButtonSize;

		return current;
	}

	static bool IsOverlayMenuButtonName(string buttonName) =>
		buttonName is "LoginButton" or "GoSignUpButton" or "ForgotPasswordButton" or "QuitButton"
			or "SignUpButton" or "BackToLoginButton" or "ForgotBackToLoginButton"
			or "MainMenuButton" or "DeleteAccountButton" or "DeleteAccountConfirmButton"
			or "DeleteAccountCancelButton" or "StartButton" or "SkipButton" or "BossAlarmContinueButton"
			or "ConfirmButton";

	static void ApplyAuthButtonLabelPadding()
	{
		string[] buttonPaths =
		{
			"LoginPanel/LoginButton",
			"LoginPanel/GoSignUpButton",
			"LoginPanel/ForgotPasswordButton",
			"LoginPanel/QuitButton",
			"SignUpPanel/SignUpButton",
			"SignUpPanel/BackToLoginButton",
			"ForgotPasswordPanel/SendResetEmailButton",
			"ForgotPasswordPanel/ForgotBackToLoginButton",
			"SettingPanel/MainMenuButton",
			"SettingPanel/QuitButton",
			"SettingPanel/DeleteAccountButton",
			"SettingPanel/BoxPanel/DeleteAccountPanel/DeleteAccountConfirmButton",
			"SettingPanel/BoxPanel/DeleteAccountPanel/DeleteAccountCancelButton",
			"LoadoutPanel/StartButton",
			"MainStoryPanel/SkipButton",
			"EndingStoryPanel/SkipButton",
			"BossAlarmPanel/BossAlarmContinueButton",
			"GameRecordPanel/Window/ConfirmButton",
		};

		foreach (string path in buttonPaths)
		{
			Transform button = FindByPath(path);
			if (button == null)
				continue;

			float horizontalPad = path.Contains("SendResetEmailButton") ? 52f : 44f;
			foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
			{
				if (!label.TryGetComponent(out RectTransform rect))
					continue;

				rect.offsetMin = new Vector2(horizontalPad, 8f);
				rect.offsetMax = new Vector2(-horizontalPad, -8f);
				EditorUtility.SetDirty(rect);
			}
		}
	}

	static void FixSettingSliders()
	{
		Sprite panelSprite = LoadSprite($"{Vol6Root}/Panels/Panels_06.png", "Panels_06_0");

		foreach (Slider slider in Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (slider.name is not ("BgmSlider" or "SfxSlider"))
				continue;

			if (!IsUnderPanel(slider.transform, "SettingPanel"))
				continue;

			Transform background = slider.transform.Find("Background");
			if (background != null && background.TryGetComponent(out Image track) && panelSprite != null)
			{
				track.sprite = panelSprite;
				track.type = Image.Type.Sliced;
				track.color = SliderTrackColor;
				EditorUtility.SetDirty(track);
				Report.Add($"{GetPath(background)} → 슬라이더 트랙 (은색 캡 제거)");
			}

			if (slider.fillRect != null && slider.fillRect.TryGetComponent(out Image fill))
			{
				fill.type = Image.Type.Filled;
				fill.fillMethod = Image.FillMethod.Horizontal;
				fill.fillOrigin = (int)Image.OriginHorizontal.Left;
				EditorUtility.SetDirty(fill);
				Report.Add($"{GetPath(fill.transform)} → 가로 Fill");
			}
		}
	}

	static void StyleSettingDropdowns()
	{
		Sprite panelSprite = LoadSprite($"{Vol6Root}/Panels/Panels_06.png", "Panels_06_0");
		Sprite arrowSprite = LoadSprite($"{Vol6Root}/Slide bars/Handle 1.png", "Handle 1_0");
		if (panelSprite == null)
			return;

		foreach (TMP_Dropdown dropdown in Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (!IsUnderPanel(dropdown.transform, "SettingPanel"))
				continue;

			if (dropdown.TryGetComponent(out RectTransform rect))
			{
				rect.sizeDelta = DropdownSize;
				EditorUtility.SetDirty(rect);
			}

			if (dropdown.TryGetComponent(out Image rootImage))
			{
				rootImage.sprite = panelSprite;
				rootImage.type = Image.Type.Sliced;
				rootImage.color = InputBackgroundColor;
				EditorUtility.SetDirty(rootImage);
			}

			Transform arrow = dropdown.transform.Find("Arrow");
			if (arrow != null && arrow.TryGetComponent(out RectTransform arrowRect))
			{
				arrowRect.sizeDelta = new Vector2(24f, 24f);
				arrowRect.anchoredPosition = new Vector2(-28f, 0f);
				arrowRect.localEulerAngles = Vector3.zero;
				EditorUtility.SetDirty(arrowRect);
			}

			if (arrow != null && arrow.TryGetComponent(out Image arrowImage) && arrowSprite != null)
			{
				arrowImage.sprite = arrowSprite;
				arrowImage.type = Image.Type.Simple;
				arrowImage.color = Color.white;
				EditorUtility.SetDirty(arrowImage);
			}

			if (dropdown.captionText != null)
			{
				dropdown.captionText.fontSize = 24f;
				dropdown.captionText.color = InputTextColor;
				EditorUtility.SetDirty(dropdown.captionText);
			}

			foreach (Image image in dropdown.GetComponentsInChildren<Image>(true))
			{
				if (image.transform == dropdown.transform || image.transform == arrow)
					continue;

				if (image.name is "Arrow")
					continue;

				image.sprite = panelSprite;
				image.type = Image.Type.Sliced;
				image.color = InputBackgroundColor;
				EditorUtility.SetDirty(image);
				Report.Add($"{GetPath(image.transform)} → 드롭다운 Panels_06");
			}

			foreach (TMP_Text label in dropdown.GetComponentsInChildren<TMP_Text>(true))
			{
				label.fontSize = 24f;
				label.color = InputTextColor;
				EditorUtility.SetDirty(label);
			}

			Report.Add($"{GetPath(dropdown.transform)} → 드롭다운 전체 {DropdownSize.x}x{DropdownSize.y}");
		}
	}

	static void FixLoadoutStartLabel()
	{
		Transform start = FindByPath("LoadoutPanel/StartButton");
		if (start == null)
			return;

		foreach (TMP_Text label in start.GetComponentsInChildren<TMP_Text>(true))
		{
			label.color = new Color(0.1f, 0.08f, 0.06f, 1f);
			label.fontSize = 28f;
			EditorUtility.SetDirty(label);
		}
	}

	static bool IsUnderPanel(Transform transform, string panelName)
	{
		while (transform != null)
		{
			if (transform.name == panelName)
				return true;
			transform = transform.parent;
		}

		return false;
	}

	static void FixSendResetEmailButtonLabel()
	{
		Transform button = FindByPath("ForgotPasswordPanel/SendResetEmailButton");
		if (button == null)
			return;

		foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
		{
			label.text = "재설정 메일 보내기";
			label.fontSize = 22f;
			label.enableAutoSizing = false;
			EditorUtility.SetDirty(label);
		}
	}

	static void SetRect(string hierarchyPath, Vector2 anchoredPosition, Vector2 sizeDelta)
	{
		Transform target = FindByPath(hierarchyPath);
		if (target == null || !target.TryGetComponent(out RectTransform rect))
			return;

		rect.anchoredPosition = anchoredPosition;
		rect.sizeDelta = sizeDelta;
		EditorUtility.SetDirty(rect);
		Report.Add($"{hierarchyPath} → pos {anchoredPosition}, size {sizeDelta}");
	}

	static Transform FindByPath(string hierarchyPath)
	{
		string[] parts = hierarchyPath.Split('/');
		Transform current = null;

		foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
		{
			if (root.name != parts[0])
				continue;

			current = root.transform;
			for (int i = 1; i < parts.Length && current != null; i++)
				current = current.Find(parts[i]);

			if (current != null)
				return current;
		}

		// Canvas 하위 탐색
		foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (transform.name != parts[0])
				continue;

			current = transform;
			for (int i = 1; i < parts.Length && current != null; i++)
				current = current.Find(parts[i]);

			if (current != null)
				return current;
		}

		return null;
	}

	static HashSet<Transform> CollectMenuStyleRoots()
	{
		var roots = new HashSet<Transform>();
		foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			foreach (string rootName in MenuStyleRootNames)
			{
				if (transform.name == rootName)
					roots.Add(transform);
			}
		}

		return roots;
	}

	static bool IsUnderAnyRoot(Transform transform, HashSet<Transform> roots)
	{
		while (transform != null)
		{
			if (roots.Contains(transform))
				return true;
			transform = transform.parent;
		}

		return false;
	}

	static string GetPath(Transform transform)
	{
		var stack = new Stack<string>();
		while (transform != null)
		{
			stack.Push(transform.name);
			transform = transform.parent;
		}

		var sb = new StringBuilder();
		while (stack.Count > 0)
		{
			if (sb.Length > 0)
				sb.Append('/');
			sb.Append(stack.Pop());
		}

		return sb.ToString();
	}

	static Sprite LoadSprite(string assetPath, string spriteName)
	{
		Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
		if (assets == null)
			return null;

		foreach (Object asset in assets)
		{
			if (asset is Sprite sprite && sprite.name == spriteName)
				return sprite;
		}

		foreach (Object asset in assets)
		{
			if (asset is Sprite sprite)
				return sprite;
		}

		return null;
	}
}
#endif
