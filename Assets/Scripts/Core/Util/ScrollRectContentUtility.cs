using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>ScrollRect + TMP 본문 높이 갱신 및 맨 위로 스크롤.</summary>
public static class ScrollRectContentUtility
{
	/// <summary>마우스 휠·트랙패드 스크롤 속도. Unity 기본값 1은 목록 UI에서 너무 느린 경우가 많습니다.</summary>
	public const float DefaultListScrollSensitivity = 32f;

	/// <summary>세로만 스크롤, 가로 잠금.</summary>
	public static void ApplyVerticalOnlyScroll(ScrollRect scrollRect, float scrollSensitivity = DefaultListScrollSensitivity)
	{
		if (scrollRect == null)
			return;

		scrollRect.horizontal = false;
		scrollRect.vertical = true;
		scrollRect.scrollSensitivity = Mathf.Max(1f, scrollSensitivity);
		scrollRect.horizontalNormalizedPosition = 0f;

		DisableHorizontalScrollbar(scrollRect);

		if (scrollRect.content != null)
		{
			ClampContentWidthToViewport(scrollRect);
		}
	}

	/// <summary>텍스트 반영 후 Content 높이를 다시 계산하고 스크롤을 맨 위(1)로 둡니다.</summary>
	public static void RefreshAndScrollToTop(ScrollRect scrollRect)
	{
		if (scrollRect == null || scrollRect.content == null)
			return;

		ApplyVerticalOnlyScroll(scrollRect);

		RectTransform content = scrollRect.content;
		RebuildContent(content);
		ClampContentWidthToViewport(scrollRect);
		scrollRect.horizontalNormalizedPosition = 0f;
		scrollRect.verticalNormalizedPosition = 1f;
	}

	static void DisableHorizontalScrollbar(ScrollRect scrollRect)
	{
		if (scrollRect.horizontalScrollbar != null)
		{
			scrollRect.horizontalScrollbar.gameObject.SetActive(false);
			scrollRect.horizontalScrollbar = null;
		}

		Transform horizontalBar = scrollRect.transform.Find("Scrollbar Horizontal");
		if (horizontalBar != null)
			horizontalBar.gameObject.SetActive(false);
	}

	static void ClampContentWidthToViewport(ScrollRect scrollRect)
	{
		RectTransform viewport = scrollRect.viewport;
		RectTransform content = scrollRect.content;
		if (viewport == null || content == null)
			return;

		content.anchorMin = new Vector2(0f, 1f);
		content.anchorMax = new Vector2(1f, 1f);
		content.pivot = new Vector2(0.5f, 1f);
		content.offsetMin = new Vector2(0f, content.offsetMin.y);
		content.offsetMax = new Vector2(0f, content.offsetMax.y);

		float width = viewport.rect.width;
		if (width > 0f)
			content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
	}

	public static void RebuildContent(RectTransform content)
	{
		if (content == null)
			return;

		foreach (TextMeshProUGUI tmp in content.GetComponentsInChildren<TextMeshProUGUI>(true))
			tmp.ForceMeshUpdate();

		Canvas.ForceUpdateCanvases();
		LayoutRebuilder.ForceRebuildLayoutImmediate(content);
	}

	/// <summary>Content 기본값: 위에서 아래로 스크롤 (상단 잘림 방지).</summary>
	public static void ApplyTopDownContentDefaults(RectTransform content)
	{
		if (content == null)
			return;

		content.anchorMin = new Vector2(0f, 1f);
		content.anchorMax = new Vector2(1f, 1f);
		content.pivot = new Vector2(0.5f, 1f);
		content.anchoredPosition = Vector2.zero;

		ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
		if (fitter == null)
			fitter = content.gameObject.AddComponent<ContentSizeFitter>();

		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
	}

	/// <summary>스크롤 안 TMP 한 줄: 가로만 늘고 높이는 글자 수에 맞춤.</summary>
	public static void ApplyTopDownTextDefaults(TextMeshProUGUI tmp)
	{
		if (tmp == null)
			return;

		RectTransform rt = tmp.rectTransform;
		rt.anchorMin = new Vector2(0f, 1f);
		rt.anchorMax = new Vector2(1f, 1f);
		rt.pivot = new Vector2(0.5f, 1f);
		rt.anchoredPosition = Vector2.zero;

		ContentSizeFitter fitter = tmp.GetComponent<ContentSizeFitter>();
		if (fitter == null)
			fitter = tmp.gameObject.AddComponent<ContentSizeFitter>();

		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		tmp.textWrappingMode = TextWrappingModes.Normal;
		tmp.overflowMode = TextOverflowModes.Overflow;
	}

	/// <summary>
	/// 플로팅 툴팁 패널용 — 앵커는 건드리지 않고 VLG+CSF만 추가 (상단 스트레치로 붙는 문제 방지).
	/// </summary>
	public static void ApplyFloatingPanelStack(RectTransform panel, float spacing = 6f, float width = 360f)
	{
		if (panel == null)
			return;

		ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
		if (fitter == null)
			fitter = panel.gameObject.AddComponent<ContentSizeFitter>();

		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
		if (layout == null)
			layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();

		layout.childAlignment = TextAnchor.UpperLeft;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = false;
		layout.spacing = spacing;
		layout.padding = new RectOffset(16, 16, 12, 12);

		if (width > 0f)
			panel.sizeDelta = new Vector2(width, panel.sizeDelta.y);
	}

	/// <summary>Content 안에 TMP가 여러 개일 때(특징 + 패턴) 세로로 쌓기.</summary>
	public static void ApplyVerticalStackContent(RectTransform content, float spacing = 16f)
	{
		ApplyTopDownContentDefaults(content);

		VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
		if (layout == null)
			layout = content.gameObject.AddComponent<VerticalLayoutGroup>();

		layout.childAlignment = TextAnchor.UpperLeft;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = false;
		layout.spacing = spacing;
		layout.padding = new RectOffset(20, 20, 16, 24);
	}
}
