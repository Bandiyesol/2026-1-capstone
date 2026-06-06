using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>기록 목록 한 줄 + 아코디언 상세 패널.</summary>
public class GameRecordRowView : MonoBehaviour
{
	const float SummaryMinHeight = 140f;
	const float TimeBlockWidth = 210f;
	const float TimeLineHeight = 34f;
	const float PortraitSize = 84f;
	const float SummaryColumnSpacing = 10f;
	const float DetailMinHeight = 340f;
	const float StageStripHeight = 208f;
	const float StageColumnMinWidth = 68f;
	const float StageColumnHeight = 208f;
	const float StageCardPadding = 8f;
	const float StageCardLineSpacing = 2f;
	const float DetailHeaderHeight = 118f;
	const float PlayTimeFontSize = 26f;
	const float DateFontSize = 26f;
	const float DetailHeaderFontSize = 24f;
	const float StageCardFontSize = 20f;
	const float SummaryFontSize = 22f;

	[SerializeField] Image backgroundImage;
	[SerializeField] TextMeshProUGUI dateText;
	[SerializeField] TextMeshProUGUI playTimeText;
	[SerializeField] TextMeshProUGUI summaryText;
	[SerializeField] TextMeshProUGUI detailText;
	[SerializeField] Image portraitImage;
	[SerializeField] Button expandButton;
	[SerializeField] RectTransform expandArrow;
	[SerializeField] GameObject detailPanel;
	[SerializeField] RectTransform stageStripContent;

	[Header("# 레이아웃 (비우면 자동 보정)")]
	[SerializeField] RectTransform summaryRow;
	[SerializeField] RectTransform timeBlock;

	static readonly Color WinTint = new Color(0.28f, 0.48f, 0.82f, 0.48f);
	static readonly Color LoseTint = new Color(0.72f, 0.22f, 0.24f, 0.48f);
	static readonly Color StageUnreached = new Color(0.18f, 0.18f, 0.22f, 0.55f);
	static readonly Color StageReached = new Color(0.24f, 0.28f, 0.36f, 0.75f);
	static readonly Color StageCleared = new Color(0.2f, 0.36f, 0.55f, 0.85f);

	GameRunRecord record;
	bool isExpanded;
	bool detailStripBuilt;
	TMP_FontAsset boundFont;
	System.Action<GameRecordRowView> onToggle;

	public string RecordId => record?.id;
	public bool IsExpanded => isExpanded;

	public void ConfigureReferences(
		Image background,
		TextMeshProUGUI date,
		TextMeshProUGUI playTime,
		TextMeshProUGUI summary,
		TextMeshProUGUI detail,
		Image portrait,
		Button expand,
		RectTransform arrow,
		GameObject detailRoot)
	{
		backgroundImage = background;
		dateText = date;
		playTimeText = playTime;
		summaryText = summary;
		detailText = detail;
		portraitImage = portrait;
		expandButton = expand;
		expandArrow = arrow;
		detailPanel = detailRoot;
	}

	public void Bind(
		GameRunRecord data,
		TMP_FontAsset font,
		System.Action<GameRecordRowView> toggleCallback)
	{
		record = data;
		boundFont = font;
		onToggle = toggleCallback;

		NormalizeCharacterDisplay(record);

		ResolveLayoutReferences();
		DestroyDetailChildrenImmediate();
		ResetDetailStrip();

		if (dateText != null)
		{
			dateText.gameObject.SetActive(true);
			dateText.text = data.playedAt;
			dateText.fontSize = DateFontSize;
			dateText.alignment = TextAlignmentOptions.TopLeft;
			TmpKoreanFontUtility.ApplyFont(dateText, font);
		}

		if (playTimeText != null)
		{
			playTimeText.text = $"플레이 타임 {FormatPlayTime(data.playTimeSeconds)}";
			playTimeText.fontSize = PlayTimeFontSize;
			playTimeText.richText = false;
			TmpKoreanFontUtility.ApplyFont(playTimeText, font);
		}

		ApplySummaryLayout();
		ApplyFullWidthLayout();
		AnchorExpandButtonToRow();

		if (backgroundImage != null)
			backgroundImage.color = data.cleared ? WinTint : LoseTint;

		if (summaryText != null)
		{
			summaryText.text = BuildSummary(data);
			summaryText.fontSize = SummaryFontSize;
			TmpKoreanFontUtility.ApplyFont(summaryText, font);
			TmpKoreanFontUtility.EnsureGlyphs(summaryText, font, summaryText.text);
		}

		if (detailText != null)
			SuppressDetailText();
		ApplyPortrait(data);
		SetExpanded(false, notify: false);

		if (expandButton != null)
		{
			expandButton.onClick.RemoveAllListeners();
			expandButton.onClick.AddListener(OnExpandClicked);
		}
	}

	public void SetExpanded(bool expanded, bool notify = true)
	{
		isExpanded = expanded;

		if (detailPanel != null)
			detailPanel.SetActive(expanded);

		if (expanded)
		{
			ResolveLayoutReferences();
			ApplyFullWidthLayout();
			ApplyDetailPanelFullWidth();

			if (record != null && boundFont != null)
				EnsureDetailStripBuilt(record, boundFont);
		}

		if (expandArrow != null)
			expandArrow.localRotation = Quaternion.Euler(0f, 0f, expanded ? 180f : 0f);

		if (notify)
			LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
	}

	void EnsureDetailStripBuilt(GameRunRecord data, TMP_FontAsset font)
	{
		Transform scroll = detailPanel != null ? detailPanel.transform.Find("StageScrollView") : null;
		if (detailStripBuilt && scroll != null)
		{
			Transform content = scroll.Find("Viewport/Content");
			if (content != null)
				stageStripContent = content as RectTransform;

			scroll.gameObject.SetActive(true);
			ApplyDetailPanelFullWidth();
			ApplyDetailTypography(data, font);
			LayoutStageCardsEdgeToEdge();
			return;
		}

		BuildStageDetailStrip(data, font);
		detailStripBuilt = true;
	}

	void ApplyDetailTypography(GameRunRecord data, TMP_FontAsset font)
	{
		if (detailPanel == null || data == null)
			return;

		Transform header = detailPanel.transform.Find("DetailHeader/HeaderText");
		if (header != null && header.TryGetComponent(out TextMeshProUGUI headerTmp))
		{
			headerTmp.fontSize = DetailHeaderFontSize;
			TmpKoreanFontUtility.ApplyFont(headerTmp, font);
		}

		if (stageStripContent == null)
			return;

		for (int i = 0; i < stageStripContent.childCount; i++)
		{
			Transform stage = stageStripContent.GetChild(i);
			foreach (TextMeshProUGUI stageTmp in stage.GetComponentsInChildren<TextMeshProUGUI>(true))
			{
				stageTmp.fontSize = StageCardFontSize;
				TmpKoreanFontUtility.ApplyFont(stageTmp, font);
				TmpKoreanFontUtility.EnsureGlyphs(stageTmp, font, stageTmp.text);
			}
		}
	}

	void ResetDetailStrip()
	{
		detailStripBuilt = false;
		stageStripContent = null;
	}

	void DestroyDetailChildrenImmediate()
	{
		if (detailPanel == null)
			return;

		for (int i = detailPanel.transform.childCount - 1; i >= 0; i--)
		{
			Transform child = detailPanel.transform.GetChild(i);
			if (child.name == "DetailHeader" || child.name == "StageScrollView")
				DestroyImmediate(child.gameObject);
		}

		stageStripContent = null;
	}

	void OnExpandClicked()
	{
		onToggle?.Invoke(this);
	}

	static void NormalizeCharacterDisplay(GameRunRecord data)
	{
		if (data == null)
			return;

		if (string.IsNullOrEmpty(data.characterId))
			data.characterId = GameCharacterCatalog.DefaultCharacterId;

		if (string.IsNullOrEmpty(data.characterLabel)
		    || data.characterLabel.Contains("Basic character")
		    || data.characterLabel.Contains("v1_"))
		{
			data.characterLabel = GameCharacterCatalog.GetDisplayName(data.characterId);
		}
	}

	void ResolveLayoutReferences()
	{
		if (summaryRow == null)
		{
			Transform summary = transform.Find("Summary");
			if (summary != null)
				summaryRow = summary as RectTransform;
		}

		if (timeBlock == null && summaryRow != null)
		{
			Transform block = summaryRow.Find("TimeBlock");
			if (block != null)
				timeBlock = block as RectTransform;
		}
	}

	void ApplySummaryLayout()
	{
		ApplyRowVerticalLayout();
		StretchHorizontally(transform as RectTransform);

		LayoutElement rootLayout = GetComponent<LayoutElement>();
		if (rootLayout == null)
			rootLayout = gameObject.AddComponent<LayoutElement>();
		rootLayout.minHeight = SummaryMinHeight;
		rootLayout.flexibleWidth = 1f;
		rootLayout.minWidth = 0f;

		if (summaryRow != null)
		{
			StretchHorizontally(summaryRow);

			LayoutElement summaryLayout = summaryRow.GetComponent<LayoutElement>();
			if (summaryLayout == null)
				summaryLayout = summaryRow.gameObject.AddComponent<LayoutElement>();
			summaryLayout.minHeight = SummaryMinHeight;
			summaryLayout.preferredHeight = SummaryMinHeight;
			summaryLayout.flexibleWidth = 1f;
			summaryLayout.minWidth = 0f;

			HorizontalLayoutGroup horizontal = summaryRow.GetComponent<HorizontalLayoutGroup>();
			if (horizontal != null)
			{
				horizontal.childAlignment = TextAnchor.UpperLeft;
				horizontal.padding = new RectOffset(12, 72, 10, 10);
				horizontal.spacing = SummaryColumnSpacing;
				horizontal.childControlWidth = true;
				horizontal.childControlHeight = true;
				horizontal.childForceExpandWidth = false;
				horizontal.childForceExpandHeight = false;
			}

			RemovePortraitLeadingGap();
			EnsureSummaryChildOrder();
			ApplyFixedWidth(timeBlock, TimeBlockWidth);
			ApplyPortraitLayout();
			ApplyFlexibleWidth(summaryText, 1f);
			ApplyTimeBlockManualLayout();
			AnchorExpandButtonToRow();
		}

		if (detailPanel != null)
			ApplyDetailPanelFullWidth();
	}

	void ApplyRowVerticalLayout()
	{
		VerticalLayoutGroup vertical = GetComponent<VerticalLayoutGroup>();
		if (vertical == null)
			return;

		vertical.childControlWidth = true;
		vertical.childControlHeight = true;
		vertical.childForceExpandWidth = true;
		vertical.childForceExpandHeight = false;
	}

	void ApplyDetailPanelFullWidth()
	{
		if (detailPanel == null)
			return;

		DisableDetailPanelLayoutGroups();

		RectTransform rect = detailPanel.transform as RectTransform;
		RectTransform row = transform as RectTransform;

		rect.anchorMin = new Vector2(0f, 1f);
		rect.anchorMax = new Vector2(1f, 1f);
		rect.pivot = new Vector2(0.5f, 1f);
		rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
		rect.offsetMax = new Vector2(0f, rect.offsetMax.y);

		LayoutElement layout = detailPanel.GetComponent<LayoutElement>();
		if (layout == null)
			layout = detailPanel.AddComponent<LayoutElement>();
		layout.minHeight = DetailMinHeight;
		layout.preferredHeight = DetailMinHeight;
		layout.flexibleWidth = 1f;

		Canvas.ForceUpdateCanvases();
		float width = row != null ? row.rect.width : 0f;
		if (width < 50f && transform.parent is RectTransform parentRow)
			width = parentRow.rect.width;

		if (width > 50f)
		{
			layout.minWidth = width;
			layout.preferredWidth = width;
			rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
		}
		else
		{
			layout.minWidth = 600f;
			layout.preferredWidth = 600f;
		}
	}

	public void ApplyFullWidthLayout()
	{
		StretchHorizontally(transform as RectTransform);

		if (summaryRow != null)
			StretchHorizontally(summaryRow);

		if (detailPanel != null)
			StretchHorizontally(detailPanel.transform as RectTransform);
	}

	static void StretchHorizontally(RectTransform rect)
	{
		if (rect == null)
			return;

		float minY = rect.anchorMin.y;
		float maxY = rect.anchorMax.y;
		rect.anchorMin = new Vector2(0f, minY);
		rect.anchorMax = new Vector2(1f, maxY);
		rect.pivot = new Vector2(0.5f, rect.pivot.y);
		rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
		rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
	}

	static void ApplyFixedWidth(RectTransform rect, float width)
	{
		if (rect == null)
			return;

		LayoutElement layout = rect.GetComponent<LayoutElement>();
		if (layout == null)
			layout = rect.gameObject.AddComponent<LayoutElement>();
		layout.minWidth = width;
		layout.preferredWidth = width;
		layout.flexibleWidth = 0f;
	}

	static void ApplyFlexibleWidth(Component target, float flexibleWidth)
	{
		if (target == null)
			return;

		RectTransform rect = target.transform as RectTransform;
		if (rect == null)
			return;

		LayoutElement layout = rect.GetComponent<LayoutElement>();
		if (layout == null)
			layout = rect.gameObject.AddComponent<LayoutElement>();
		layout.flexibleWidth = flexibleWidth;
		layout.minWidth = 120f;
	}

	static void ApplyTextBlockLayout(TextMeshProUGUI text, float minHeight, float fontSize)
	{
		if (text == null)
			return;

		text.fontSize = fontSize;
		text.textWrappingMode = TextWrappingModes.Normal;
		text.overflowMode = TextOverflowModes.Overflow;

		LayoutElement layout = text.GetComponent<LayoutElement>();
		if (layout != null)
			Destroy(layout);
	}

	void ApplyTimeBlockManualLayout()
	{
		if (timeBlock == null)
			return;

		LayoutElement blockLayout = timeBlock.GetComponent<LayoutElement>();
		if (blockLayout == null)
			blockLayout = timeBlock.gameObject.AddComponent<LayoutElement>();
		blockLayout.minWidth = TimeBlockWidth;
		blockLayout.preferredWidth = TimeBlockWidth;
		blockLayout.minHeight = TimeLineHeight * 2f + 6f;
		blockLayout.preferredHeight = TimeLineHeight * 2f + 6f;
		blockLayout.flexibleWidth = 0f;
		blockLayout.flexibleHeight = 0f;

		VerticalLayoutGroup vertical = timeBlock.GetComponent<VerticalLayoutGroup>();
		if (vertical == null)
			vertical = timeBlock.gameObject.AddComponent<VerticalLayoutGroup>();

		vertical.padding = new RectOffset(0, 0, 0, 0);
		vertical.spacing = 4f;
		vertical.childAlignment = TextAnchor.UpperLeft;
		vertical.childControlWidth = true;
		vertical.childControlHeight = true;
		vertical.childForceExpandWidth = true;
		vertical.childForceExpandHeight = false;

		if (dateText != null)
		{
			dateText.transform.SetParent(timeBlock, false);
			ConfigureTimeLineText(dateText, DateFontSize);
			ApplyTimeLineLayoutElement(dateText, TimeLineHeight);
		}

		if (playTimeText != null)
		{
			playTimeText.transform.SetParent(timeBlock, false);
			ConfigureTimeLineText(playTimeText, PlayTimeFontSize, centerHorizontally: true);
			ApplyTimeLineLayoutElement(playTimeText, TimeLineHeight);
		}

		if (dateText != null)
			dateText.transform.SetAsFirstSibling();
		if (playTimeText != null)
			playTimeText.transform.SetAsLastSibling();
	}

	void EnsureSummaryChildOrder()
	{
		if (summaryRow == null)
			return;

		int index = 0;

		if (timeBlock != null)
			timeBlock.SetSiblingIndex(index++);

		if (portraitImage != null)
			portraitImage.rectTransform.SetSiblingIndex(index++);

		if (summaryText != null)
			summaryText.rectTransform.SetAsLastSibling();
	}

	void RemovePortraitLeadingGap()
	{
		if (summaryRow == null)
			return;

		Transform gap = summaryRow.Find("PortraitLeadingGap");
		if (gap != null)
			Destroy(gap.gameObject);
	}

	void ApplyPortraitLayout()
	{
		if (portraitImage == null)
			return;

		RectTransform portraitRect = portraitImage.rectTransform;
		LayoutElement layout = portraitRect.GetComponent<LayoutElement>();
		if (layout == null)
			layout = portraitRect.gameObject.AddComponent<LayoutElement>();

		layout.ignoreLayout = false;
		layout.minWidth = PortraitSize;
		layout.preferredWidth = PortraitSize;
		layout.flexibleWidth = 0f;
		layout.minHeight = PortraitSize;
		layout.preferredHeight = PortraitSize;
		layout.flexibleHeight = 0f;

		portraitRect.sizeDelta = new Vector2(PortraitSize, PortraitSize);
	}

	static void ConfigureTimeLineText(TextMeshProUGUI text, float fontSize, bool centerHorizontally = false)
	{
		if (text == null)
			return;

		text.fontSize = fontSize;
		text.alignment = centerHorizontally ? TextAlignmentOptions.Top : TextAlignmentOptions.TopLeft;
		text.horizontalAlignment = centerHorizontally
			? HorizontalAlignmentOptions.Center
			: HorizontalAlignmentOptions.Left;
		text.textWrappingMode = TextWrappingModes.NoWrap;
		text.overflowMode = TextOverflowModes.Overflow;
		text.margin = Vector4.zero;
	}

	static void ApplyTimeLineLayoutElement(TextMeshProUGUI text, float height)
	{
		if (text == null)
			return;

		LayoutElement layout = text.GetComponent<LayoutElement>();
		if (layout == null)
			layout = text.gameObject.AddComponent<LayoutElement>();

		layout.minHeight = height;
		layout.preferredHeight = height;
		layout.flexibleHeight = 0f;
		layout.minWidth = 0f;
		layout.flexibleWidth = 0f;
	}

	void AnchorExpandButtonToRow()
	{
		if (expandButton == null)
			return;

		Transform spacer = summaryRow != null ? summaryRow.Find("ExpandSpacer") : null;
		if (spacer != null)
			spacer.gameObject.SetActive(false);

		RectTransform expandRect = expandButton.transform as RectTransform;
		expandRect.SetParent(transform, false);

		LayoutElement expandLayout = expandRect.GetComponent<LayoutElement>();
		if (expandLayout == null)
			expandLayout = expandRect.gameObject.AddComponent<LayoutElement>();
		expandLayout.ignoreLayout = true;

		expandRect.anchorMin = new Vector2(1f, 1f);
		expandRect.anchorMax = new Vector2(1f, 1f);
		expandRect.pivot = new Vector2(1f, 1f);
		expandRect.sizeDelta = new Vector2(56f, 56f);
		expandRect.anchoredPosition = new Vector2(-12f, -(SummaryMinHeight * 0.5f));

		Image buttonImage = expandButton.GetComponent<Image>();
		if (buttonImage != null)
			buttonImage.color = new Color(0.93f, 0.93f, 0.95f, 1f);

		expandButton.gameObject.SetActive(true);
		expandRect.SetAsLastSibling();

	}

	void SuppressDetailText()
	{
		if (detailText == null)
			return;

		detailText.gameObject.SetActive(false);
		Destroy(detailText.gameObject);
		detailText = null;
	}

	void ApplyPortrait(GameRunRecord data)
	{
		if (portraitImage == null)
			return;

		string characterId = string.IsNullOrEmpty(data.characterId)
			? GameCharacterCatalog.DefaultCharacterId
			: data.characterId;

		Sprite sprite = GameCharacterCatalog.GetPortrait(characterId, GameManager.instance?.player);
		portraitImage.sprite = sprite;
		portraitImage.enabled = sprite != null;
		portraitImage.preserveAspect = true;
		portraitImage.color = Color.white;
	}

	void BuildStageDetailStrip(GameRunRecord data, TMP_FontAsset font)
	{
		if (detailPanel == null)
			return;

		ApplyDetailPanelFullWidth();
		SuppressDetailText();
		DisableDetailPanelLayoutGroups();

		Transform oldHeader = detailPanel.transform.Find("DetailHeader");
		if (oldHeader != null)
			DestroyImmediate(oldHeader.gameObject);

		Transform oldScroll = detailPanel.transform.Find("StageScrollView");
		if (oldScroll != null)
			DestroyImmediate(oldScroll.gameObject);

		stageStripContent = null;

		GameObject header = CreateDetailHeader(data, font, detailPanel.transform as RectTransform);
		RectTransform headerRect = header.GetComponent<RectTransform>();
		headerRect.anchorMin = new Vector2(0f, 1f);
		headerRect.anchorMax = new Vector2(1f, 1f);
		headerRect.pivot = new Vector2(0.5f, 1f);
		headerRect.sizeDelta = new Vector2(0f, DetailHeaderHeight);
		headerRect.anchoredPosition = Vector2.zero;

		RectTransform content = EnsureStageStripContent();
		RectTransform scrollRoot = content.parent.parent as RectTransform;
		if (scrollRoot != null)
		{
			scrollRoot.anchorMin = new Vector2(0f, 0f);
			scrollRoot.anchorMax = new Vector2(1f, 1f);
			scrollRoot.pivot = new Vector2(0.5f, 0.5f);
			scrollRoot.offsetMin = new Vector2(8f, 8f);
			scrollRoot.offsetMax = new Vector2(-8f, -(DetailHeaderHeight + 12f));
		}

		ClearChildren(content);

		VerticalLayoutGroup verticalOnContent = content.GetComponent<VerticalLayoutGroup>();
		if (verticalOnContent != null)
			Destroy(verticalOnContent);

		HorizontalLayoutGroup row = content.GetComponent<HorizontalLayoutGroup>();
		if (row == null)
			row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
		row.spacing = 6f;
		row.childAlignment = TextAnchor.UpperCenter;
		row.childControlWidth = true;
		row.childControlHeight = true;
			row.childForceExpandWidth = true;
			row.childForceExpandHeight = false;
			row.padding = new RectOffset(0, 0, 0, 0);

		GameRunStageRecord[] stages = data.stageRecords;
		if (stages == null || stages.Length == 0)
			stages = GameRunRecord.CreateNew().stageRecords;

		for (int i = 0; i < GameRunSessionTracker.MaxStages; i++)
		{
			GameRunStageRecord stage = i < stages.Length ? stages[i] : GameRunStageRecord.Empty(i + 1);
			CreateStageColumn(content, stage, font);
		}

		ApplyDetailPanelFullWidth();
		LayoutStageCardsEdgeToEdge();
	}

	void DisableDetailPanelLayoutGroups()
	{
		if (detailPanel == null)
			return;

		VerticalLayoutGroup vertical = detailPanel.GetComponent<VerticalLayoutGroup>();
		if (vertical != null)
			vertical.enabled = false;

		HorizontalLayoutGroup horizontal = detailPanel.GetComponent<HorizontalLayoutGroup>();
		if (horizontal != null)
			horizontal.enabled = false;

		ContentSizeFitter fitter = detailPanel.GetComponent<ContentSizeFitter>();
		if (fitter != null)
			fitter.enabled = false;
	}

	GameObject CreateDetailHeader(GameRunRecord data, TMP_FontAsset font, RectTransform parent)
	{
		GameObject header = new GameObject("DetailHeader", typeof(RectTransform));
		header.transform.SetParent(parent, false);

		string headerText =
			$"<b>상세 기록</b>  ·  캐릭터: {data.characterLabel}\n" +
			$"플레이 타임 {FormatPlayTime(data.playTimeSeconds)}  ·  누적 처치 {GetRunTotalKills(data)}  ·  누적 골드 {GetRunTotalGold(data)}\n" +
			$"무기: {FormatItemList(data.weaponNames)}  ·  악세서리: {FormatItemList(data.accessoryNames)}  ·  룬: {FormatItemList(data.runeNames)}";

		TextMeshProUGUI tmp = CreateTmp("HeaderText", header.transform, headerText, DetailHeaderFontSize, TextAlignmentOptions.TopLeft);
		tmp.textWrappingMode = TextWrappingModes.Normal;
		tmp.overflowMode = TextOverflowModes.Overflow;
		RectTransform textRect = tmp.rectTransform;
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.offsetMin = new Vector2(12f, 6f);
		textRect.offsetMax = new Vector2(-12f, -6f);
		TmpKoreanFontUtility.ApplyFont(tmp, font);
		TmpKoreanFontUtility.EnsureGlyphs(tmp, font, headerText);

		return header;
	}

	void CreateStageColumn(RectTransform parent, GameRunStageRecord stage, TMP_FontAsset font)
	{
		GameObject column = new GameObject($"Stage{stage.stageNumber}", typeof(RectTransform));
		column.transform.SetParent(parent, false);

		RectTransform columnRect = column.GetComponent<RectTransform>();
		columnRect.sizeDelta = new Vector2(0f, StageColumnHeight);

		LayoutElement columnLayout = column.AddComponent<LayoutElement>();
		columnLayout.flexibleWidth = 1f;
		columnLayout.minWidth = StageColumnMinWidth;
		columnLayout.minHeight = StageColumnHeight;
		columnLayout.preferredHeight = StageColumnHeight;

		Image bg = column.AddComponent<Image>();
		if (!stage.reached)
			bg.color = StageUnreached;
		else if (stage.cleared)
			bg.color = StageCleared;
		else
			bg.color = StageReached;

		int pad = Mathf.RoundToInt(StageCardPadding);
		string status = !stage.reached
			? "미도달"
			: stage.cleared ? "클리어" : "사망";

		string body =
			$"<b>{stage.stageNumber}스테이지</b>\n" +
			$"{status}\n" +
			$"플레이 타임 {FormatPlayTime(stage.playTimeSeconds)}\n" +
			$"처치 {stage.killCount}\n" +
			$"획득 골드 {stage.coinCount}\n" +
			$"룬 {FormatStageRunes(stage.runeNames)}\n" +
			$"보스 {ResolveStageBossName(stage)}";

		GameObject textRoot = new GameObject("StageCardText", typeof(RectTransform));
		textRoot.transform.SetParent(column.transform, false);
		RectTransform textRect = textRoot.GetComponent<RectTransform>();
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.offsetMin = new Vector2(pad, pad);
		textRect.offsetMax = new Vector2(-pad, -pad);

		TextMeshProUGUI tmp = textRoot.AddComponent<TextMeshProUGUI>();
		tmp.text = body;
		tmp.fontSize = StageCardFontSize;
		tmp.alignment = TextAlignmentOptions.TopLeft;
		tmp.color = Color.white;
		tmp.richText = true;
		tmp.textWrappingMode = TextWrappingModes.Normal;
		tmp.overflowMode = TextOverflowModes.Overflow;
		tmp.lineSpacing = StageCardLineSpacing;
		tmp.paragraphSpacing = 0f;
		tmp.margin = new Vector4(0f, 2f, 0f, 0f);
		tmp.enableAutoSizing = false;
		tmp.extraPadding = true;

		TmpKoreanFontUtility.ApplyFont(tmp, font);
		TmpKoreanFontUtility.EnsureGlyphs(tmp, font, body);
	}

	RectTransform EnsureStageStripContent()
	{
		stageStripContent = null;

		Transform existing = detailPanel.transform.Find("StageScrollView");
		if (existing != null)
			DestroyImmediate(existing.gameObject);

		GameObject scrollRoot = new GameObject("StageScrollView", typeof(RectTransform));
		scrollRoot.transform.SetParent(detailPanel.transform, false);
		RectTransform scrollRootRect = scrollRoot.GetComponent<RectTransform>();
		scrollRootRect.anchorMin = new Vector2(0f, 0f);
		scrollRootRect.anchorMax = new Vector2(1f, 1f);
		scrollRootRect.offsetMin = new Vector2(8f, 8f);
		scrollRootRect.offsetMax = new Vector2(-8f, -(DetailHeaderHeight + 8f));

		ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
		scroll.horizontal = false;
		scroll.vertical = false;
		scroll.movementType = ScrollRect.MovementType.Clamped;

		GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
		viewport.transform.SetParent(scrollRoot.transform, false);
		StretchFull(viewport.GetComponent<RectTransform>());
		viewport.AddComponent<RectMask2D>();
		Image viewportImage = viewport.AddComponent<Image>();
		viewportImage.color = new Color(0f, 0f, 0f, 0.12f);

		GameObject content = new GameObject("Content", typeof(RectTransform));
		content.transform.SetParent(viewport.transform, false);
		RectTransform contentRect = content.GetComponent<RectTransform>();
		StretchFull(contentRect);

		scroll.viewport = viewport.GetComponent<RectTransform>();
		scroll.content = contentRect;

		stageStripContent = contentRect;
		return stageStripContent;
	}

	static TextMeshProUGUI CreateTmp(
		string name,
		Transform parent,
		string text,
		float fontSize,
		TextAlignmentOptions alignment)
	{
		GameObject go = new GameObject(name, typeof(RectTransform));
		go.transform.SetParent(parent, false);
		TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
		tmp.text = text;
		tmp.fontSize = fontSize;
		tmp.alignment = alignment;
		tmp.color = Color.white;
		tmp.textWrappingMode = TextWrappingModes.Normal;
		tmp.richText = true;
		return tmp;
	}

	static void ClearChildren(RectTransform parent)
	{
		if (parent == null)
			return;

		for (int i = parent.childCount - 1; i >= 0; i--)
			Destroy(parent.GetChild(i).gameObject);
	}

	void LayoutStageCardsEdgeToEdge()
	{
		if (stageStripContent == null || detailPanel == null)
			return;

		Transform scrollTransform = detailPanel.transform.Find("StageScrollView");
		if (scrollTransform == null)
			return;

		HorizontalLayoutGroup row = stageStripContent.GetComponent<HorizontalLayoutGroup>();
		if (row != null)
		{
			row.spacing = 4f;
			row.padding = new RectOffset(0, 0, 0, 0);
			row.childAlignment = TextAnchor.UpperCenter;
			row.childControlWidth = true;
			row.childControlHeight = true;
			row.childForceExpandWidth = true;
			row.childForceExpandHeight = false;
		}

		ContentSizeFitter fitter = stageStripContent.GetComponent<ContentSizeFitter>();
		if (fitter != null)
			DestroyImmediate(fitter);

		StretchFull(stageStripContent);
		stageStripContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, StageStripHeight);

		float stripWidth = scrollTransform is RectTransform scrollRect
			? scrollRect.rect.width
			: 0f;
		if (stripWidth < 50f && detailPanel.transform is RectTransform detailRect)
			stripWidth = detailRect.rect.width - 16f;

		float spacing = row != null ? row.spacing : 4f;
		float columnWidth = stripWidth > 50f
			? Mathf.Max(StageColumnMinWidth, (stripWidth - spacing * (GameRunSessionTracker.MaxStages - 1)) / GameRunSessionTracker.MaxStages)
			: StageColumnMinWidth;

		for (int i = 0; i < stageStripContent.childCount; i++)
		{
			if (!stageStripContent.GetChild(i).TryGetComponent(out LayoutElement columnLayout))
				continue;

			columnLayout.flexibleWidth = 1f;
			columnLayout.flexibleHeight = 0f;
			columnLayout.minWidth = columnWidth;
			columnLayout.preferredWidth = columnWidth;
			columnLayout.minHeight = StageColumnHeight;
			columnLayout.preferredHeight = StageColumnHeight;
		}

		if (scrollTransform.TryGetComponent(out ScrollRect scroll))
		{
			scroll.horizontal = false;
			scroll.vertical = false;
			scroll.content = stageStripContent;
		}

		Canvas.ForceUpdateCanvases();
		LayoutRebuilder.ForceRebuildLayoutImmediate(stageStripContent);

		Transform viewport = scrollTransform.Find("Viewport");
		if (viewport is RectTransform viewportRect)
			LayoutRebuilder.ForceRebuildLayoutImmediate(viewportRect);
	}

	static void StretchFull(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	static string BuildSummary(GameRunRecord data)
	{
		var sb = new StringBuilder();
		sb.AppendLine(data.cleared ? "클리어" : "실패");
		sb.AppendLine($"캐릭터: {data.characterLabel}");
		sb.AppendLine($"도달 {data.stageReached}스테이지 · 처치 {GetRunTotalKills(data)} · 누적 골드 {GetRunTotalGold(data)}");
		sb.AppendLine($"무기 {FormatItemList(data.weaponNames)}");
		sb.Append($"룬 {FormatItemList(data.runeNames)}");
		return sb.ToString();
	}

	static string FormatItemList(string[] values)
	{
		return InventoryDisplayService.FormatStackedNames(values);
	}

	static int GetRunTotalGold(GameRunRecord data)
	{
		if (data == null)
			return 0;

		int fromStages = 0;
		if (data.stageRecords != null)
		{
			foreach (GameRunStageRecord stage in data.stageRecords)
			{
				if (stage != null && stage.reached)
					fromStages += Mathf.Max(0, stage.coinCount);
			}
		}

		return Mathf.Max(data.coinCount, fromStages);
	}

	static int GetRunTotalKills(GameRunRecord data)
	{
		if (data == null)
			return 0;

		int fromStages = 0;
		if (data.stageRecords != null)
		{
			foreach (GameRunStageRecord stage in data.stageRecords)
			{
				if (stage != null && stage.reached)
					fromStages += Mathf.Max(0, stage.killCount);
			}
		}

		return Mathf.Max(data.killCount, fromStages);
	}

	static string FormatStageRunes(string[] runeNames)
	{
		if (runeNames == null || runeNames.Length == 0)
			return "—";

		return string.Join(", ", runeNames);
	}

	static string ResolveStageBossName(GameRunStageRecord stage)
	{
		if (stage == null || !stage.reached)
			return "—";

		int index = stage.stageNumber - 1;
		if (index >= 0 && index < GameRunSessionTracker.MaxStages)
			return BossBriefingRuntime.GetBossDisplayName(index);

		return string.IsNullOrEmpty(stage.bossName) ? "—" : stage.bossName;
	}

	static string FormatPlayTime(float seconds)
	{
		int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
		int minutes = total / 60;
		int remain = total % 60;
		return $"{minutes:D2}:{remain:D2}";
	}
}
