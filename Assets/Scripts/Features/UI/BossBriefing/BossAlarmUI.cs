using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 시작 직후 전면 보스 알리미. 이어하기: 무기 선택 → 룬 선택.
/// Hierarchy 예: BossAlarmPanel(비활성) / Title, Body, Portrait, ContinueButton
/// </summary>
public class BossAlarmUI : MonoBehaviour
{
	[Header("# 패널")]
	[SerializeField] GameObject panel;
	[SerializeField] ScrollRect scrollRect;

	[Header("# 내용 (스크롤 밖)")]
	[SerializeField] TextMeshProUGUI titleText;
	[SerializeField] Image portraitImage;

	[Header("# 내용 (스크롤 안 — StoryScrollView와 동일 구조 권장)")]
	[SerializeField] TextMeshProUGUI traitsText;
	[SerializeField] TextMeshProUGUI patternsText;

	[Header("# 버튼")]
	[SerializeField] Button continueButton;

	[Header("# 폰트 (선택)")]
	[SerializeField] TMP_FontAsset koreanFont;

	Action onContinue;
	bool isOpen;

	void Awake()
	{
		EnsureReferences();
		WireContinue();

		if (!isOpen && panel != null)
			panel.SetActive(false);
	}

	void OnDestroy()
	{
		if (continueButton != null)
			continueButton.onClick.RemoveListener(OnContinueClicked);
	}

	void Update()
	{
		if (!isOpen || panel == null || !panel.activeInHierarchy)
			return;

		if (!PanelKeyboardShortcutUtility.WasEscapeOrEnterPressedThisFrame())
			return;

		TryInvokeContinue();
	}

	/// <summary>현재 <see cref="BossBriefingRuntime"/> 내용으로 전면 표시 후, 확인 시 콜백.</summary>
	public void Show(Action onContinueCallback)
	{
		if (isOpen)
			return;

		if (!BossBriefingRuntime.HasBrief)
		{
			onContinueCallback?.Invoke();
			return;
		}

		EnsureReferences();
		WireContinue();
		ApplyTexts();

		isOpen = true;
		onContinue = onContinueCallback;

		if (panel != null)
		{
			panel.SetActive(true);
			panel.transform.SetAsLastSibling();
		}

		RefreshBossScroll();
	}

	void RefreshBossScroll()
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
		ScrollRectContentUtility.ApplyVerticalStackContent(scrollRect.content);

		if (traitsText != null)
			ScrollRectContentUtility.ApplyTopDownTextDefaults(traitsText);

		if (patternsText != null)
			ScrollRectContentUtility.ApplyTopDownTextDefaults(patternsText);
	}

	public void Hide()
	{
		isOpen = false;
		onContinue = null;

		if (panel != null)
			panel.SetActive(false);
	}

	void OnContinueClicked()
	{
		if (!isOpen)
			return;

		// Hide()가 onContinue를 null로 만들기 때문에, 반드시 먼저 보관합니다.
		Action cb = onContinue;
		Hide();
		cb?.Invoke();
	}

	void TryInvokeContinue()
	{
		if (continueButton != null && continueButton.isActiveAndEnabled && continueButton.interactable)
		{
			continueButton.onClick.Invoke();
			return;
		}

		OnContinueClicked();
	}

	void ApplyTexts()
	{
		if (titleText != null)
		{
			titleText.text = $"이번 스테이지 보스 — {BossBriefingRuntime.DisplayName}";
			TmpKoreanFontUtility.ApplyFont(titleText, koreanFont);
			TmpKoreanFontUtility.EnsureGlyphs(titleText, koreanFont, titleText.text);
		}

		if (traitsText != null)
		{
			traitsText.text = $"<b>특징</b>\n{BossBriefingRuntime.TraitsSummary}";
			TmpKoreanFontUtility.ApplyFont(traitsText, koreanFont);
			TmpKoreanFontUtility.EnsureGlyphs(traitsText, koreanFont, traitsText.text);
			ScrollRectContentUtility.ApplyTopDownTextDefaults(traitsText);
		}

		if (patternsText != null)
		{
			patternsText.text = $"<b>패턴·룬</b>\n{BossBriefingRuntime.PatternsHint}";
			TmpKoreanFontUtility.ApplyFont(patternsText, koreanFont);
			TmpKoreanFontUtility.EnsureGlyphs(patternsText, koreanFont, patternsText.text);
			ScrollRectContentUtility.ApplyTopDownTextDefaults(patternsText);
		}

		BossBriefPortraitView.Apply(portraitImage);
	}

	void EnsureReferences()
	{
		if (panel == null)
		{
			Transform t = transform.Find("BossAlarmPanel");
			if (t != null)
				panel = t.gameObject;
		}

		if (panel == null)
			panel = gameObject;

		if (scrollRect == null && panel != null)
			scrollRect = panel.GetComponentInChildren<ScrollRect>(true);

		if (titleText == null && panel != null)
			titleText = FindDeepTMP(panel.transform, "BossAlarmTitle");

		if (traitsText == null && panel != null)
		{
			Transform t = panel.transform.Find("BossAlarmScrollView/Viewport/Content/BossAlarmTraits");
			if (t == null)
				t = FindDeep(panel.transform, "BossAlarmTraits");
			if (t != null)
				traitsText = t.GetComponent<TextMeshProUGUI>();
		}

		if (patternsText == null && panel != null)
		{
			Transform t = panel.transform.Find("BossAlarmScrollView/Viewport/Content/BossAlarmPatterns");
			if (t == null)
				t = FindDeep(panel.transform, "BossAlarmPatterns");
			if (t != null)
				patternsText = t.GetComponent<TextMeshProUGUI>();
		}

		if (portraitImage == null && panel != null)
		{
			Transform img = FindDeep(panel.transform, "BossAlarmPortrait");
			if (img != null)
				portraitImage = img.GetComponent<Image>();
		}

		if (continueButton == null && panel != null)
		{
			Transform b = FindDeep(panel.transform, "BossAlarmContinueButton");
			if (b != null)
				continueButton = b.GetComponent<Button>();
		}
	}

	void WireContinue()
	{
		if (continueButton == null)
			return;

		continueButton.onClick.RemoveAllListeners();
		continueButton.onClick.AddListener(OnContinueClicked);
	}

	static TextMeshProUGUI FindDeepTMP(Transform root, string childName)
	{
		Transform t = FindDeep(root, childName);
		return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
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
