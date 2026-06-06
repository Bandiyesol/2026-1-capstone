using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>GameRecordPanel이 없으면 Canvas 아래에 런타임 생성합니다.</summary>
public static class GameRecordUIBootstrap
{
	public static GameRecordUI Ensure()
	{
		GameRecordUI existing = Object.FindFirstObjectByType<GameRecordUI>(FindObjectsInactive.Include);
		if (existing != null)
			return existing;

		GameObject panel = GameObject.Find("GameRecordPanel");
		if (panel == null)
			panel = BuildPanel();

		GameRecordUI ui = panel.GetComponent<GameRecordUI>();
		if (ui == null)
			ui = panel.AddComponent<GameRecordUI>();

		return ui;
	}

	static GameObject BuildPanel()
	{
		Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
		if (canvas == null)
		{
			Debug.LogError("[GameRecordUIBootstrap] Canvas를 찾지 못했습니다.");
			return null;
		}

		GameObject panel = CreateUiObject("GameRecordPanel", canvas.transform);
		RectTransform panelRect = panel.GetComponent<RectTransform>();
		StretchFull(panelRect);

		Image dim = panel.AddComponent<Image>();
		dim.color = new Color(0f, 0f, 0f, 0.72f);
		dim.raycastTarget = true;

		GameObject window = CreateUiObject("Window", panel.transform);
		RectTransform windowRect = window.GetComponent<RectTransform>();
		windowRect.anchorMin = new Vector2(0.08f, 0.08f);
		windowRect.anchorMax = new Vector2(0.92f, 0.92f);
		windowRect.offsetMin = Vector2.zero;
		windowRect.offsetMax = Vector2.zero;
		Image windowBg = window.AddComponent<Image>();
		windowBg.color = new Color(0.12f, 0.12f, 0.16f, 0.96f);

		GameObject title = CreateText("Title", window.transform, "플레이 기록", 36f, TextAlignmentOptions.Center);
		RectTransform titleRect = title.GetComponent<RectTransform>();
		titleRect.anchorMin = new Vector2(0f, 1f);
		titleRect.anchorMax = new Vector2(1f, 1f);
		titleRect.pivot = new Vector2(0.5f, 1f);
		titleRect.sizeDelta = new Vector2(0f, 56f);
		titleRect.anchoredPosition = new Vector2(0f, -12f);

		GameObject scrollRoot = CreateUiObject("RecordScrollView", window.transform);
		RectTransform scrollRect = scrollRoot.GetComponent<RectTransform>();
		scrollRect.anchorMin = new Vector2(0.03f, 0.12f);
		scrollRect.anchorMax = new Vector2(0.97f, 0.88f);
		scrollRect.offsetMin = Vector2.zero;
		scrollRect.offsetMax = Vector2.zero;

		ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
		scroll.horizontal = false;
		scroll.vertical = true;
		scroll.movementType = ScrollRect.MovementType.Clamped;
		scroll.scrollSensitivity = ScrollRectContentUtility.DefaultListScrollSensitivity;

		GameObject viewport = CreateUiObject("Viewport", scrollRoot.transform);
		StretchFull(viewport.GetComponent<RectTransform>());
		viewport.AddComponent<RectMask2D>();
		Image viewportImage = viewport.AddComponent<Image>();
		viewportImage.color = new Color(0f, 0f, 0f, 0.15f);

		GameObject content = CreateUiObject("Content", viewport.transform);
		RectTransform contentRect = content.GetComponent<RectTransform>();
		contentRect.anchorMin = new Vector2(0f, 1f);
		contentRect.anchorMax = new Vector2(1f, 1f);
		contentRect.pivot = new Vector2(0.5f, 1f);
		contentRect.anchoredPosition = Vector2.zero;
		contentRect.sizeDelta = new Vector2(0f, 0f);

		VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
		layout.childAlignment = TextAnchor.UpperCenter;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = false;
		layout.spacing = 10f;
		layout.padding = new RectOffset(8, 8, 8, 8);

		ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		scroll.viewport = viewport.GetComponent<RectTransform>();
		scroll.content = contentRect;

		GameObject confirm = CreateButton("ConfirmButton", window.transform, "확인");
		RectTransform confirmRect = confirm.GetComponent<RectTransform>();
		confirmRect.anchorMin = new Vector2(0.35f, 0.02f);
		confirmRect.anchorMax = new Vector2(0.65f, 0.08f);
		confirmRect.offsetMin = Vector2.zero;
		confirmRect.offsetMax = Vector2.zero;

		GameObject rowTemplate = BuildRowTemplate(window.transform);
		rowTemplate.SetActive(false);

		panel.SetActive(false);
		return panel;
	}

	static GameObject BuildRowTemplate(Transform parent)
	{
		GameObject row = CreateUiObject("RecordRowTemplate", parent);
		RectTransform rowRect = row.GetComponent<RectTransform>();
		rowRect.sizeDelta = new Vector2(0f, 120f);

		VerticalLayoutGroup rowLayout = row.AddComponent<VerticalLayoutGroup>();
		rowLayout.childControlWidth = true;
		rowLayout.childControlHeight = true;
		rowLayout.childForceExpandWidth = true;
		rowLayout.childForceExpandHeight = false;
		rowLayout.spacing = 0f;

		ContentSizeFitter rowFitter = row.AddComponent<ContentSizeFitter>();
		rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		Image rowBg = row.AddComponent<Image>();
		rowBg.color = new Color(0.3f, 0.3f, 0.3f, 0.35f);

		GameObject summary = CreateUiObject("Summary", row.transform);
		LayoutElement summaryLayout = summary.AddComponent<LayoutElement>();
		summaryLayout.minHeight = 108f;
		summaryLayout.preferredHeight = 108f;

		HorizontalLayoutGroup summaryH = summary.AddComponent<HorizontalLayoutGroup>();
		summaryH.padding = new RectOffset(10, 10, 8, 8);
		summaryH.spacing = 10f;
		summaryH.childAlignment = TextAnchor.MiddleLeft;
		summaryH.childControlWidth = true;
		summaryH.childControlHeight = true;
		summaryH.childForceExpandWidth = false;
		summaryH.childForceExpandHeight = true;

		GameObject timeBlock = CreateUiObject("TimeBlock", summary.transform);
		timeBlock.AddComponent<LayoutElement>().preferredWidth = 150f;
		VerticalLayoutGroup timeV = timeBlock.AddComponent<VerticalLayoutGroup>();
		timeV.childAlignment = TextAnchor.UpperLeft;
		timeV.spacing = 4f;
		GameObject date = CreateText("DateText", timeBlock.transform, "2026-01-01", 20f, TextAlignmentOptions.TopLeft);
		GameObject playTime = CreateText("PlayTimeText", timeBlock.transform, "00:00", 18f, TextAlignmentOptions.TopLeft);

		GameObject portrait = CreateUiObject("Portrait", summary.transform);
		portrait.AddComponent<LayoutElement>().preferredWidth = 72f;
		Image portraitImage = portrait.AddComponent<Image>();
		portraitImage.color = new Color(1f, 1f, 1f, 0.85f);
		RectTransform portraitRect = portrait.GetComponent<RectTransform>();
		portraitRect.sizeDelta = new Vector2(72f, 72f);

		GameObject summaryText = CreateText("SummaryText", summary.transform, "요약", 18f, TextAlignmentOptions.TopLeft);
		summaryText.AddComponent<LayoutElement>().flexibleWidth = 1f;

		GameObject expandButton = CreateButton("ExpandButton", summary.transform, "▼");
		expandButton.AddComponent<LayoutElement>().preferredWidth = 56f;
		TextMeshProUGUI expandLabel = expandButton.GetComponentInChildren<TextMeshProUGUI>();
		if (expandLabel != null)
			expandLabel.fontSize = 26f;

		GameObject detailPanel = CreateUiObject("DetailPanel", row.transform);
		detailPanel.SetActive(false);
		LayoutElement detailLayout = detailPanel.AddComponent<LayoutElement>();
		detailLayout.minHeight = 160f;
		detailLayout.preferredHeight = 160f;
		Image detailBg = detailPanel.AddComponent<Image>();
		detailBg.color = new Color(0f, 0f, 0f, 0.22f);

		GameObject detailText = CreateText("DetailText", detailPanel.transform, "상세", 20f, TextAlignmentOptions.TopLeft);
		StretchFull(detailText.GetComponent<RectTransform>());
		RectTransform detailTextRect = detailText.GetComponent<RectTransform>();
		detailTextRect.offsetMin = new Vector2(16f, 12f);
		detailTextRect.offsetMax = new Vector2(-16f, -12f);

		GameRecordRowView view = row.AddComponent<GameRecordRowView>();
		view.ConfigureReferences(
			rowBg,
			date.GetComponent<TextMeshProUGUI>(),
			playTime.GetComponent<TextMeshProUGUI>(),
			summaryText.GetComponent<TextMeshProUGUI>(),
			detailText.GetComponent<TextMeshProUGUI>(),
			portraitImage,
			expandButton.GetComponent<Button>(),
			expandButton.GetComponent<RectTransform>(),
			detailPanel);

		return row;
	}

	static GameObject CreateUiObject(string name, Transform parent)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		return go;
	}

	static GameObject CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
	{
		GameObject go = CreateUiObject(name, parent);
		TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
		tmp.text = text;
		tmp.fontSize = size;
		tmp.alignment = align;
		tmp.color = Color.white;
		tmp.textWrappingMode = TextWrappingModes.Normal;
		TmpKoreanFontUtility.ApplyFont(tmp, TmpKoreanFontUtility.ResolveNeoDgmFont(null));
		return go;
	}

	static GameObject CreateButton(string name, Transform parent, string label)
	{
		GameObject go = CreateUiObject(name, parent);
		Image image = go.AddComponent<Image>();
		image.color = new Color(0.22f, 0.24f, 0.32f, 1f);
		Button button = go.AddComponent<Button>();
		button.targetGraphic = image;

		GameObject textGo = CreateText("Text", go.transform, label, 24f, TextAlignmentOptions.Center);
		StretchFull(textGo.GetComponent<RectTransform>());
		return go;
	}

	static void StretchFull(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}
}
