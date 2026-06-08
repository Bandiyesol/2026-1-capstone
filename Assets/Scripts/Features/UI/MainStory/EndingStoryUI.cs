using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 최종 스테이지 클리어 후 엔딩 스토리. MainStoryPanel과 동일 구조의 EndingStoryPanel 사용.
/// Skip → 플레이 기록 → 메인 화면.
/// </summary>
public class EndingStoryUI : MonoBehaviour
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

	bool isOpen;

	public TMP_FontAsset KoreanFont => koreanFont;

	void Awake()
	{
		EnsureReferences();
		WireSkipButton();

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

	public bool IsOpen => isOpen;

	public void Show()
	{
		if (isOpen)
			return;

		EnsureReferences();
		WireSkipButton();
		ApplyStoryText();
		PreparePresentation();

		if (panel == null)
		{
			Debug.LogError(
				"[EndingStoryUI] EndingStoryPanel을 찾지 못했습니다. MainStoryPanel을 복제해 EndingStoryPanel로 이름을 바꾸고 연결하세요.",
				this);
			return;
		}

		isOpen = true;
		panel.SetActive(true);
		panel.transform.SetAsLastSibling();
		RefreshStoryScroll();
	}

	public void ForceClose()
	{
		isOpen = false;

		if (panel != null)
			panel.SetActive(false);
	}

	public void Skip()
	{
		if (!isOpen)
			return;

		FinishAndShowRecord();
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

	void FinishAndShowRecord()
	{
		isOpen = false;

		if (panel != null)
			panel.SetActive(false);

		if (GameManager.instance != null)
			GameManager.instance.ShowGameRecordAfterRun(cleared: true);
	}

	void PreparePresentation()
	{
		if (GameManager.instance == null)
			return;

		GameManager.instance.isLive = false;
		GameManager.instance.Stop();
		GameManager.instance.HideGameplayHud();
		GameManager.CloseOverlayPanels();
	}

	void ApplyStoryText()
	{
		ResolveKoreanFont();

		if (titleText != null)
			titleText.text = EndingStoryDefaults.Title;

		if (storyText == null)
			return;

		string body = string.IsNullOrWhiteSpace(storyBodyOverride)
			? EndingStoryDefaults.StoryBody
			: storyBodyOverride;

		string allText = EndingStoryDefaults.Title + body;
		TmpKoreanFontUtility.EnsureGlyphsInFont(koreanFont, allText, "EndingStoryUI");

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

	void ResolveKoreanFont()
	{
		if (koreanFont != null)
			return;

		MainStoryUI mainStory = GetComponent<MainStoryUI>();
		if (mainStory != null)
			koreanFont = mainStory.KoreanFont;

		koreanFont = TmpKoreanFontUtility.ResolveNeoDgmFont(koreanFont);
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

	void EnsureReferences()
	{
		if (panel == null)
		{
			Transform found = transform.Find("EndingStoryPanel");
			if (found != null)
				panel = found.gameObject;
		}

		if (panel == null)
		{
			GameObject scenePanel = GameObject.Find("EndingStoryPanel");
			if (scenePanel != null)
				panel = scenePanel;
		}

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
