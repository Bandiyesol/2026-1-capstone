using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메인 메뉴 → 게임 시작 시 표시되는 오프닝 스토리. 스크롤 + Skip.
/// </summary>
public class MainStoryUI : MonoBehaviour
{
	[Header("# 패널")]
	[SerializeField] GameObject panel;
	[SerializeField] ScrollRect scrollRect;

	[Header("# 텍스트")]
	[SerializeField] TextMeshProUGUI titleText;
	[SerializeField] TextMeshProUGUI storyText;
	[TextArea(12, 24)]
	[SerializeField] string storyBodyOverride;

	[Header("# 버튼")]
	[SerializeField] Button skipButton;

	[Header("# 폰트 (선택)")]
	[SerializeField] TMP_FontAsset koreanFont;

	[Header("# 동작")]
	[SerializeField] bool hideGameStartWhileOpen = true;

	bool isOpen;

	public TMP_FontAsset KoreanFont => koreanFont;

	void Awake()
	{
		EnsureReferences();
		WireSkipButton();

		// 패널이 처음 비활성일 때 Awake가 Show 직후에 돌면 다시 꺼지는 문제 방지 (isOpen 확인).
		if (!isOpen && panel != null)
			panel.SetActive(false);
	}

	void OnDestroy()
	{
		if (skipButton != null)
			skipButton.onClick.RemoveListener(Skip);
	}

	void Update()
	{
		if (!isOpen || panel == null || !panel.activeInHierarchy)
			return;

		if (!PanelKeyboardShortcutUtility.WasEscapeOrEnterPressedThisFrame())
			return;

		TryInvokeSkip();
	}

	/// <summary>스토리 표시 후 완료 시 보스 알리미 → 무기/룬 플로우(<see cref="GameManager.GameStart"/>).</summary>
	public void ShowThenStartGame()
	{
		if (isOpen)
			return;

		EnsureReferences();
		WireSkipButton();
		ApplyStoryText();
		PrepareWorldPaused();

		if (panel == null)
		{
			Debug.LogError(
				"[MainStoryUI] panel이 없습니다. MainStoryPanel에 MainStoryUI를 붙이지 말고, " +
				"항상 켜져 있는 Canvas(또는 UI 루트)에 붙인 뒤 Panel 필드에 MainStoryPanel을 연결하세요.",
				this);
			return;
		}

		isOpen = true;
		panel.SetActive(true);
		panel.transform.SetAsLastSibling();

		RefreshStoryScroll();
	}

	void RefreshStoryScroll()
	{
		if (scrollRect == null)
			return;

		EnsureScrollLayout();
		ScrollRectContentUtility.RefreshAndScrollToTop(scrollRect);
		StartCoroutine(ScrollToTopAfterLayout());
	}

	IEnumerator ScrollToTopAfterLayout()
	{
		yield return null;
		if (scrollRect != null)
			ScrollRectContentUtility.RefreshAndScrollToTop(scrollRect);
	}

	void EnsureScrollLayout()
	{
		if (scrollRect == null || scrollRect.content == null)
			return;

		ScrollRectContentUtility.ApplyVerticalOnlyScroll(scrollRect);
		ScrollRectContentUtility.ApplyTopDownContentDefaults(scrollRect.content);

		if (storyText != null)
			ScrollRectContentUtility.ApplyTopDownTextDefaults(storyText);
	}

	public void Skip()
	{
		if (!isOpen)
			return;

		FinishAndStartGame();
	}

	void TryInvokeSkip()
	{
		if (skipButton != null && skipButton.isActiveAndEnabled && skipButton.interactable)
		{
			skipButton.onClick.Invoke();
			return;
		}

		Skip();
	}

	void FinishAndStartGame()
	{
		isOpen = false;

		if (panel != null)
			panel.SetActive(false);

		// GameStart 메뉴는 다시 켜지 않음 → 무기 선택 → 룬 선택으로 이어짐.
		if (GameManager.instance != null)
			GameManager.instance.GameStart();
	}

	void PrepareWorldPaused()
	{
		SettingsUI settings = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
		settings?.Close();

		if (GameManager.instance == null)
			return;

		GameManager.instance.isLive = false;
		GameManager.instance.HidePreGameSelectPanels();

		GameManager.instance.HideGameplayHud();

		if (hideGameStartWhileOpen && GameManager.instance.mainMenuRoot != null)
			GameManager.instance.mainMenuRoot.SetActive(false);
	}

	void ApplyStoryText()
	{
		if (titleText != null)
			titleText.text = "메인 스토리";

		if (storyText == null)
			return;

		string body = string.IsNullOrWhiteSpace(storyBodyOverride)
			? MainStoryDefaults.OpeningStory
			: storyBodyOverride;

		storyText.text = body;
		TmpKoreanFontUtility.ApplyFont(storyText, koreanFont);
		TmpKoreanFontUtility.EnsureGlyphs(storyText, koreanFont, body);
		ScrollRectContentUtility.ApplyTopDownTextDefaults(storyText);

		if (titleText != null)
		{
			TmpKoreanFontUtility.ApplyFont(titleText, koreanFont);
			TmpKoreanFontUtility.EnsureGlyphs(titleText, koreanFont, titleText.text);
		}
	}

	void EnsureReferences()
	{
		if (panel == null)
		{
			Transform found = transform.Find("MainStoryPanel");
			if (found != null)
				panel = found.gameObject;
		}

		if (panel == null)
			panel = gameObject;

		if (scrollRect == null && panel != null)
			scrollRect = panel.GetComponentInChildren<ScrollRect>(true);

		if (storyText == null && panel != null)
		{
			Transform content = panel.transform.Find("StoryScrollView/Viewport/Content/StoryText");
			if (content == null)
				content = FindDeep(panel.transform, "StoryText");

			if (content != null)
				storyText = content.GetComponent<TextMeshProUGUI>();
		}

		if (titleText == null && panel != null)
		{
			Transform title = FindDeep(panel.transform, "StoryTitle");
			if (title != null)
				titleText = title.GetComponent<TextMeshProUGUI>();
		}

		if (skipButton == null && panel != null)
		{
			Transform skip = FindDeep(panel.transform, "SkipButton");
			if (skip != null)
				skipButton = skip.GetComponent<Button>();
		}
	}

	void WireSkipButton()
	{
		if (skipButton == null)
			return;

		// Inspector 에 다른 OnClick(메뉴 복귀 등)이 있으면 스킵 후 루프가 납니다.
		skipButton.onClick.RemoveAllListeners();
		skipButton.onClick.AddListener(Skip);
	}

	static Transform FindDeep(Transform root, string childName)
	{
		if (root == null)
			return null;

		if (root.name == childName)
			return root;

		for (int i = 0; i < root.childCount; i++)
		{
			Transform found = FindDeep(root.GetChild(i), childName);
			if (found != null)
				return found;
		}

		return null;
	}
}
