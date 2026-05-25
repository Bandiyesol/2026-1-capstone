using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 설정 패널 — 예전 SettingWindow 와 같이 슬라이더/드롭다운 onValueChanged 로 동작합니다.
/// </summary>
public class SettingsUI : MonoBehaviour
{
	[Header("# 패널 (필수)")]
	[SerializeField] GameObject panel;
	[SerializeField] Button closeButton;

	[Header("# 화면 / 해상도 (TMP_Dropdown Inspector 연결)")]
	[SerializeField] TMP_Dropdown screenModeDropdown;
	[SerializeField] TMP_Dropdown resolutionDropdown;

	[Header("# 볼륨")]
	[SerializeField] Slider bgmSlider;
	[SerializeField] Slider sfxSlider;

	[Header("# 하단 버튼")]
	[SerializeField] Button mainMenuButton;
	[SerializeField] Button quitButton;
	[SerializeField] Button deleteAccountButton;

	[Header("# 회원 탈퇴 확인 (선택)")]
	[SerializeField] GameObject deleteAccountPanel;
	[SerializeField] Component deleteAccountPasswordInput;
	[SerializeField] Button deleteAccountConfirmButton;
	[SerializeField] Button deleteAccountCancelButton;
	[SerializeField] TextMeshProUGUI deleteAccountMessageText;

	[Header("# 라벨 (선택)")]
	[SerializeField] TextMeshProUGUI titleLabel;
	[SerializeField] TextMeshProUGUI screenModeLabel;
	[SerializeField] TextMeshProUGUI resolutionLabel;
	[SerializeField] TextMeshProUGUI bgmLabel;
	[SerializeField] TextMeshProUGUI sfxLabel;

	[Header("# 폰트 / 글자 색")]
	[SerializeField] TMP_FontAsset koreanFont;
	[SerializeField] Color dropdownValueTextColor = new Color(0.12f, 0.12f, 0.12f, 1f);
	[Tooltip("닫힌 상태 값 칸 글자 크기")]
	[SerializeField] float dropdownCaptionFontSize = 28f;
	[Tooltip("펼친 목록(Item) 글자 크기")]
	[SerializeField] float dropdownListFontSize = 28f;
	[Tooltip("펼친 목록 한 줄 높이")]
	[SerializeField] float dropdownItemHeight = 44f;

	bool isOpen;
	bool panelSetupDone;
	bool pausedBySettings;
	bool uiListenersBound;

	void Awake()
	{
		EnsurePanelSetup();
	}

	void OnEnable()
	{
		if (panel != null && panel.activeInHierarchy)
			FixUiRaycastBlockers();
	}

	void OnDestroy()
	{
		ResumeGameIfPausedBySettings();
		UnbindUiListeners();

		if (closeButton != null)
			closeButton.onClick.RemoveListener(Close);
	}

	public void Toggle()
	{
		EnsurePanelSetup();

		if (isOpen)
			Close();
		else
			Open();
	}

	public void Open()
	{
		EnsurePanelSetup();

		if (panel == null)
		{
			Debug.LogError("[SettingsUI] Panel이 비어 있습니다. Inspector에 SettingPanel을 연결하세요.");
			return;
		}

		isOpen = true;
		HideDeleteAccountPanel();
		panel.SetActive(true);
		panel.transform.SetAsLastSibling();
		EnsurePanelInteractable();
		FixUiRaycastBlockers();
		PrepareDropdowns();
		GameAudioSettings.Instance?.RefreshSources();
		SyncUiFromSettings();
		PauseGameIfLive();
	}

	public void Close()
	{
		if (panel == null)
			return;

		isOpen = false;
		HideDeleteAccountPanel();
		panel.SetActive(false);
		ResumeGameIfPausedBySettings();
	}

	void EnsurePanelInteractable()
	{
		if (panel == null)
			return;

		if (panel.TryGetComponent(out CanvasGroup group))
		{
			group.interactable = true;
			group.blocksRaycasts = true;
			group.alpha = 1f;
		}
	}

	void PauseGameIfLive()
	{
		if (GameManager.instance == null || !GameManager.instance.isLive)
			return;

		GameManager.instance.PauseOverlay();
		FreezePlayerMovement();
		pausedBySettings = true;
	}

	void ResumeGameIfPausedBySettings()
	{
		if (!pausedBySettings || GameManager.instance == null)
			return;

		pausedBySettings = false;
		GameManager.instance.ResumeOverlay();
	}

	static void FreezePlayerMovement()
	{
		if (GameManager.instance?.player == null)
			return;

		Player player = GameManager.instance.player;
		player.inputVec = Vector2.zero;

		if (player.TryGetComponent(out Rigidbody2D rigid))
			rigid.linearVelocity = Vector2.zero;
	}

	void EnsurePanelSetup()
	{
		if (panelSetupDone)
			return;

		panelSetupDone = true;
		GameSettings.EnsureLoaded();
		AutoBindReferences();

		if (panel == null)
		{
			Debug.LogError("[SettingsUI] SettingPanel을 찾지 못했습니다.");
			return;
		}

		if (!isOpen)
			panel.SetActive(false);

		RemoveLegacyCycleHitAreas(panel.transform);
		FixUiRaycastBlockers();
		PrepareDropdowns();
		PopulateDropdownOptions();

		if (closeButton != null)
		{
			closeButton.onClick.AddListener(Close);
			EnsureCloseButtonPressedSprite(closeButton.gameObject);
		}

		ResolveKoreanFont();
		ApplyKoreanFont();
		ApplyDefaultRowLabels();
		ApplyDropdownTextColors();
		BindUiListeners();
		SyncUiFromSettings();
	}

	void BindUiListeners()
	{
		if (uiListenersBound)
			return;

		uiListenersBound = true;

		if (screenModeDropdown != null)
			screenModeDropdown.onValueChanged.AddListener(OnScreenModeDropdownChanged);

		if (resolutionDropdown != null)
			resolutionDropdown.onValueChanged.AddListener(OnResolutionDropdownChanged);

		if (bgmSlider != null)
			bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);

		if (sfxSlider != null)
			sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

		if (mainMenuButton != null)
			mainMenuButton.onClick.AddListener(OnMainMenuClicked);

		if (quitButton != null)
			quitButton.onClick.AddListener(OnQuitClicked);

		if (deleteAccountButton != null)
			deleteAccountButton.onClick.AddListener(OnDeleteAccountClicked);

		if (deleteAccountConfirmButton != null)
			deleteAccountConfirmButton.onClick.AddListener(OnDeleteAccountConfirmClicked);

		if (deleteAccountCancelButton != null)
			deleteAccountCancelButton.onClick.AddListener(HideDeleteAccountPanel);
	}

	void UnbindUiListeners()
	{
		if (!uiListenersBound)
			return;

		uiListenersBound = false;

		if (screenModeDropdown != null)
			screenModeDropdown.onValueChanged.RemoveListener(OnScreenModeDropdownChanged);

		if (resolutionDropdown != null)
			resolutionDropdown.onValueChanged.RemoveListener(OnResolutionDropdownChanged);

		if (bgmSlider != null)
			bgmSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);

		if (sfxSlider != null)
			sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);

		if (mainMenuButton != null)
			mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);

		if (quitButton != null)
			quitButton.onClick.RemoveListener(OnQuitClicked);

		if (deleteAccountButton != null)
			deleteAccountButton.onClick.RemoveListener(OnDeleteAccountClicked);

		if (deleteAccountConfirmButton != null)
			deleteAccountConfirmButton.onClick.RemoveListener(OnDeleteAccountConfirmClicked);

		if (deleteAccountCancelButton != null)
			deleteAccountCancelButton.onClick.RemoveListener(HideDeleteAccountPanel);
	}

	void PrepareDropdowns()
	{
		StyleDropdown(screenModeDropdown);
		StyleDropdown(resolutionDropdown);
	}

	void StyleDropdown(TMP_Dropdown dropdown)
	{
		if (dropdown == null)
			return;

		dropdown.interactable = true;
		dropdown.enabled = true;

		if (dropdown.targetGraphic != null)
			dropdown.targetGraphic.raycastTarget = true;

		EnsureDropdownListOnTop(dropdown);
		ApplyDropdownFontSizes(dropdown);
	}

	void ApplyDropdownFontSizes(TMP_Dropdown dropdown)
	{
		if (dropdown.captionText != null)
		{
			dropdown.captionText.fontSize = dropdownCaptionFontSize;
			dropdown.captionText.enableAutoSizing = false;
			dropdown.captionText.color = dropdownValueTextColor;
			TmpKoreanFontUtility.ApplyFont(dropdown.captionText, koreanFont);
		}

		if (dropdown.itemText != null)
		{
			dropdown.itemText.fontSize = dropdownListFontSize;
			dropdown.itemText.enableAutoSizing = false;
			dropdown.itemText.color = dropdownValueTextColor;
			TmpKoreanFontUtility.ApplyFont(dropdown.itemText, koreanFont);
		}

		Transform label = dropdown.transform.Find("Label");
		if (label != null && label.TryGetComponent(out TextMeshProUGUI labelTmp))
		{
			labelTmp.fontSize = dropdownCaptionFontSize;
			labelTmp.enableAutoSizing = false;
			labelTmp.color = dropdownValueTextColor;
			TmpKoreanFontUtility.ApplyFont(labelTmp, koreanFont);
		}

		ResizeDropdownTemplateItem(dropdown);
	}

	void ResizeDropdownTemplateItem(TMP_Dropdown dropdown)
	{
		if (dropdown?.template == null)
			return;

		Transform item = dropdown.template.Find("Viewport/Content/Item");
		if (item == null)
			item = FindDeep(dropdown.template, "Item");

		if (item == null)
			return;

		if (item.TryGetComponent(out RectTransform itemRect))
		{
			Vector2 size = itemRect.sizeDelta;
			size.y = dropdownItemHeight;
			itemRect.sizeDelta = size;
		}

		if (!item.TryGetComponent(out LayoutElement layout))
			layout = item.gameObject.AddComponent<LayoutElement>();

		layout.preferredHeight = dropdownItemHeight;
		layout.minHeight = dropdownItemHeight;
	}

	static void EnsureDropdownListOnTop(TMP_Dropdown dropdown)
	{
		if (dropdown == null)
			return;

		PrepareDropdownTemplate(dropdown);

		if (!dropdown.TryGetComponent(out TmpDropdownOpenedListFix _))
			dropdown.gameObject.AddComponent<TmpDropdownOpenedListFix>();
	}

	/// <summary>
	/// TMP_Dropdown.Show() 가 템플릿 Canvas 를 사용합니다. 클릭은 복제된 목록에 Raycaster 가 필요합니다.
	/// </summary>
	static void PrepareDropdownTemplate(TMP_Dropdown dropdown)
	{
		if (dropdown?.template == null)
			return;

		RectTransform template = dropdown.template;

		if (template.GetComponent<Canvas>() == null)
			template.gameObject.AddComponent<Canvas>();

		if (template.GetComponent<GraphicRaycaster>() == null)
			template.gameObject.AddComponent<GraphicRaycaster>();

		template.gameObject.SetActive(false);
	}

	static void RemoveLegacyCycleHitAreas(Transform root)
	{
		if (root == null)
			return;

		foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
		{
			if (child != null && child.name == "SettingsCycleHitArea")
				Destroy(child.gameObject);
		}
	}

	/// <summary>
	/// Row/Image 가 BoxPanel 전체를 덮으며 raycast 로 클릭을 가로채는 Unity 레이아웃 문제를 런타임에 해제합니다.
	/// </summary>
	void FixUiRaycastBlockers()
	{
		if (panel == null)
			return;

		Transform box = FindBoxPanel(panel.transform) ?? panel.transform;

		DisableRowBackgroundRaycast(box, "ScreenModeRow");
		DisableRowBackgroundRaycast(box, "ResolutionRow");
		DisableRowBackgroundRaycast(box, "BgmRow");
		DisableRowBackgroundRaycast(box, "SfxRow");

		Transform boxPanel = FindDeep(box, "BoxPanel") ?? box;
		if (boxPanel.TryGetComponent(out Image boxImage))
			boxImage.raycastTarget = false;
	}

	static void DisableRowBackgroundRaycast(Transform root, string rowName)
	{
		Transform row = FindDeep(root, rowName);
		if (row == null)
			return;

		if (row.TryGetComponent(out Image rowImage))
			rowImage.raycastTarget = false;
	}

	void OnScreenModeDropdownChanged(int index)
	{
		GameSettings.SetScreenModeIndex(index);
	}

	void OnResolutionDropdownChanged(int index)
	{
		GameSettings.SetResolutionIndex(index);
	}

	void OnBgmVolumeChanged(float value)
	{
		GameSettings.SetBgmVolume(value);
		GameAudioSettings.Instance?.RefreshSources();
	}

	void OnSfxVolumeChanged(float value)
	{
		GameSettings.SetSfxVolume(value);
	}

	void PopulateDropdownOptions()
	{
		GameSettings.EnsureLoaded();

		if (screenModeDropdown != null)
		{
			screenModeDropdown.ClearOptions();
			screenModeDropdown.AddOptions(new List<string>(GameSettings.ScreenModeOptions));
		}

		if (resolutionDropdown != null)
		{
			resolutionDropdown.ClearOptions();
			var labels = new List<string>();
			foreach (GameSettings.ResolutionOption option in GameSettings.GetResolutionOptions())
				labels.Add(option.Label);

			resolutionDropdown.AddOptions(labels);
		}
	}

	void SyncUiFromSettings()
	{
		GameSettings.EnsureLoaded();
		ApplyDefaultRowLabels();
		ApplyDropdownTextColors();

		if (screenModeDropdown != null && screenModeDropdown.options.Count > 0)
		{
			int index = Mathf.Clamp(GameSettings.ScreenModeIndex, 0, screenModeDropdown.options.Count - 1);
			screenModeDropdown.SetValueWithoutNotify(index);
			screenModeDropdown.RefreshShownValue();
		}

		if (resolutionDropdown != null && resolutionDropdown.options.Count > 0)
		{
			int index = Mathf.Clamp(GameSettings.ResolutionIndex, 0, resolutionDropdown.options.Count - 1);
			resolutionDropdown.SetValueWithoutNotify(index);
			resolutionDropdown.RefreshShownValue();
		}

		if (bgmSlider != null)
			bgmSlider.SetValueWithoutNotify(GameSettings.BgmVolume);

		if (sfxSlider != null)
			sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
	}

	void ApplyDropdownTextColors()
	{
		if (screenModeDropdown != null)
			ApplyDropdownFontSizes(screenModeDropdown);

		if (resolutionDropdown != null)
			ApplyDropdownFontSizes(resolutionDropdown);
	}

	void AutoBindReferences()
	{
		if (panel == null)
		{
			if (IsSettingsPanelName(gameObject.name))
				panel = gameObject;
			else
			{
				Transform child = transform.Find("SettingPanel") ?? transform.Find("SettingsPanel");
				if (child != null)
					panel = child.gameObject;
			}
		}

		if (panel == null)
			panel = GameObject.Find("SettingPanel") ?? GameObject.Find("SettingsPanel");

		if (panel == null)
			return;

		Transform root = panel.transform;
		Transform box = FindBoxPanel(root);

		if (closeButton == null)
		{
			Transform close = root.Find("CloseBtn") ?? box?.Find("CloseBtn");
			if (close != null)
				closeButton = close.GetComponent<Button>();
		}

		if (titleLabel == null)
			titleLabel = FindTmpDeep(root, "Title") ?? FindTmpDeep(box, "Title");

		Transform screenRow = FindDeep(root, "ScreenModeRow");
		Transform resolutionRow = FindDeep(root, "ResolutionRow");
		Transform bgmRow = FindDeep(root, "BgmRow");
		Transform sfxRow = FindDeep(root, "SfxRow");

		if (screenModeDropdown == null)
			screenModeDropdown = FindDropdownDeep(screenRow, root, "ScreenModeDropdown");

		if (resolutionDropdown == null)
			resolutionDropdown = FindDropdownDeep(resolutionRow, root, "ResolutionDropdown");

		if (screenModeLabel == null && screenRow != null)
			screenModeLabel = FindTmpDeep(screenRow, "ScreenModeLabel");

		if (resolutionLabel == null && resolutionRow != null)
			resolutionLabel = FindTmpDeep(resolutionRow, "ResolutionLabel");

		if (bgmSlider == null && bgmRow != null)
			bgmSlider = FindSliderDeep(bgmRow, "BgmSlider") ?? bgmRow.GetComponentInChildren<Slider>(true);

		if (sfxSlider == null && sfxRow != null)
			sfxSlider = FindSliderDeep(sfxRow, "SfxSlider") ?? sfxRow.GetComponentInChildren<Slider>(true);

		if (bgmSlider != null && bgmSlider == sfxSlider)
			Debug.LogError("[SettingsUI] BgmSlider와 SfxSlider가 같은 오브젝트입니다.");

		if (mainMenuButton == null)
			mainMenuButton = FindButtonDeep(root, "MainMenuButton");

		if (quitButton == null)
			quitButton = FindButtonDeep(root, "QuitButton");

		if (deleteAccountButton == null)
			deleteAccountButton = FindButtonDeep(root, "DeleteAccountButton");

		if (deleteAccountPanel == null)
		{
			Transform confirm = FindDeep(root, "DeleteAccountPanel");
			if (confirm != null)
				deleteAccountPanel = confirm.gameObject;
		}

		if (deleteAccountPanel != null)
		{
			if (deleteAccountPasswordInput == null)
				deleteAccountPasswordInput = FindComponentDeep<TMP_InputField>(
					deleteAccountPanel.transform, "DeleteAccountPasswordInput");

			if (deleteAccountConfirmButton == null)
				deleteAccountConfirmButton = FindButtonDeep(deleteAccountPanel.transform, "DeleteAccountConfirmButton");

			if (deleteAccountCancelButton == null)
				deleteAccountCancelButton = FindButtonDeep(deleteAccountPanel.transform, "DeleteAccountCancelButton");

			if (deleteAccountMessageText == null)
				deleteAccountMessageText = FindTmpDeep(deleteAccountPanel.transform, "DeleteAccountMessageText");
		}

		TryBindBottomButtons(box ?? root);
	}

	void OnMainMenuClicked()
	{
		AuthFlowController auth =
			FindFirstObjectByType<AuthFlowController>(FindObjectsInactive.Include);

		if (auth != null)
		{
			auth.RequestReturnToMainMenu();
			return;
		}

		Close();
		GameManager.instance?.ReturnToMainMenu();
	}

	void OnQuitClicked()
	{
		if (quitButton != null)
			quitButton.interactable = false;

		isOpen = false;
		HideDeleteAccountPanel();
		ResumeGameIfPausedBySettings();

		if (panel != null)
			panel.SetActive(false);

		GameQuitUtility.RequestQuit();
	}

	void OnDeleteAccountClicked()
	{
		if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
		{
			SetDeleteAccountMessage("로그인된 계정이 없습니다.");
			return;
		}

		if (deleteAccountPanel == null)
		{
			Debug.LogError(
				"[SettingsUI] DeleteAccountPanel이 없습니다. SettingPanel에 회원 탈퇴 확인 패널을 추가하고 Inspector에 연결하세요.");
			return;
		}

		deleteAccountPanel.SetActive(true);
		deleteAccountPanel.transform.SetAsLastSibling();
		SetDeleteAccountMessage("탈퇴 시 계정·닉네임·아이디가 삭제되며 복구할 수 없습니다.");
	}

	void HideDeleteAccountPanel()
	{
		if (deleteAccountPanel != null)
			deleteAccountPanel.SetActive(false);
	}

	void OnDeleteAccountConfirmClicked()
	{
		if (AuthManager.Instance == null)
		{
			SetDeleteAccountMessage("AuthManager가 씬에 없습니다.");
			return;
		}

		string password = AuthInputUtility.GetText(deleteAccountPasswordInput);
		if (string.IsNullOrEmpty(password))
		{
			SetDeleteAccountMessage("비밀번호를 입력하세요.");
			return;
		}

		AuthFlowController auth = FindFirstObjectByType<AuthFlowController>(FindObjectsInactive.Include);
		if (auth == null)
		{
			SetDeleteAccountMessage("AuthFlowController를 찾을 수 없습니다.");
			return;
		}

		HideDeleteAccountPanel();
		Close();

		auth.RequestDeleteAccount(password);
	}

	void SetDeleteAccountMessage(string message)
	{
		if (deleteAccountMessageText != null)
			deleteAccountMessageText.text = message ?? "";
	}

	void TryBindBottomButtons(Transform root)
	{
		if (root == null)
			return;

		foreach (Button button in root.GetComponentsInChildren<Button>(true))
		{
			if (button == null || button == closeButton)
				continue;

			TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
			if (label == null)
				continue;

			string text = label.text ?? string.Empty;

			if (mainMenuButton == null && (text.Contains("메인") || text.Contains("Main")))
				mainMenuButton = button;

			if (quitButton == null && (text.Contains("끝") || text.Contains("종료") || text.Contains("Quit")))
				quitButton = button;

			if (deleteAccountButton == null && (text.Contains("탈퇴") || text.Contains("Delete Account")))
				deleteAccountButton = button;
		}
	}

	void ApplyDefaultRowLabels()
	{
		if (screenModeLabel != null)
			screenModeLabel.text = "화면 모드 :";

		if (resolutionLabel != null)
			resolutionLabel.text = "해상도 :";

		if (bgmLabel != null)
			bgmLabel.text = "배경음악 :";

		if (sfxLabel != null)
			sfxLabel.text = "효과음 :";

		if (titleLabel != null && string.IsNullOrWhiteSpace(titleLabel.text))
			titleLabel.text = "설정";
	}

	void ApplyKoreanFont()
	{
		TmpKoreanFontUtility.ApplyFontToAll(
			koreanFont,
			titleLabel,
			screenModeLabel,
			resolutionLabel,
			bgmLabel,
			sfxLabel);

		if (screenModeDropdown?.captionText != null)
			TmpKoreanFontUtility.ApplyFont(screenModeDropdown.captionText, koreanFont);

		if (resolutionDropdown?.captionText != null)
			TmpKoreanFontUtility.ApplyFont(resolutionDropdown.captionText, koreanFont);
	}

	void ResolveKoreanFont()
	{
		if (koreanFont != null)
			return;

#if UNITY_EDITOR
		koreanFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpKoreanFontUtility.NeoDgmAssetPath);
#endif
	}

	static bool IsSettingsPanelName(string objectName)
	{
		return !string.IsNullOrEmpty(objectName)
			&& objectName.Contains("Setting", StringComparison.OrdinalIgnoreCase);
	}

	static Transform FindBoxPanel(Transform root)
	{
		if (root == null)
			return null;

		Transform box = root.Find("BoxPanel");
		return box != null ? box : root;
	}

	static Transform FindDeep(Transform root, string name)
	{
		if (root == null)
			return null;

		if (root.name == name)
			return root;

		foreach (Transform child in root)
		{
			Transform found = FindDeep(child, name);
			if (found != null)
				return found;
		}

		return null;
	}

	static TextMeshProUGUI FindTmpDeep(Transform root, string name)
	{
		if (root == null)
			return null;

		if (root.name == name && root.TryGetComponent(out TextMeshProUGUI direct))
			return direct;

		foreach (Transform child in root)
		{
			TextMeshProUGUI found = FindTmpDeep(child, name);
			if (found != null)
				return found;
		}

		return null;
	}

	static TMP_Dropdown FindDropdownDeep(Transform row, Transform root, string dropdownName)
	{
		if (row != null)
		{
			TMP_Dropdown inRow = FindComponentDeep<TMP_Dropdown>(row, dropdownName);
			if (inRow != null)
				return inRow;
		}

		return FindComponentDeep<TMP_Dropdown>(root, dropdownName);
	}

	static Slider FindSliderDeep(Transform row, string sliderName)
	{
		if (row == null)
			return null;

		return FindComponentDeep<Slider>(row, sliderName);
	}

	static Button FindButtonDeep(Transform root, string buttonName)
	{
		if (root == null)
			return null;

		return FindComponentDeep<Button>(root, buttonName);
	}

	static T FindComponentDeep<T>(Transform root, string objectName) where T : Component
	{
		if (root == null)
			return null;

		if (root.name == objectName && root.TryGetComponent(out T direct))
			return direct;

		foreach (Transform child in root)
		{
			T found = FindComponentDeep<T>(child, objectName);
			if (found != null)
				return found;
		}

		return null;
	}

	static void EnsureCloseButtonPressedSprite(GameObject closeBtnObject)
	{
		if (closeBtnObject == null)
			return;

		if (!closeBtnObject.TryGetComponent(out PixelButtonSpriteSwap swap))
			swap = closeBtnObject.AddComponent<PixelButtonSpriteSwap>();

		swap.Apply();
	}
}
