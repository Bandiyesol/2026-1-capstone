using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>플레이 기록 목록 — 롤 전적처럼 행 확장(아코디언).</summary>
public class GameRecordUI : MonoBehaviour
{
	[SerializeField] GameObject panel;
	[SerializeField] ScrollRect recordScrollRect;
	[SerializeField] RectTransform listContent;
	[SerializeField] GameRecordRowView rowTemplate;
	[SerializeField] Button confirmButton;
	[SerializeField] TextMeshProUGUI titleText;
	[SerializeField] TMP_FontAsset koreanFont;

	[Header("스크롤")]
	[Tooltip("RecordScrollView의 Scroll Sensitivity. 클수록 휠 한 번에 더 많이 내려갑니다.")]
	[SerializeField] float scrollSensitivity = ScrollRectContentUtility.DefaultListScrollSensitivity;

	readonly List<GameRecordRowView> rows = new List<GameRecordRowView>();

	bool isOpen;
	bool singleRunOnly;
	string expandRecordId;
	Action onConfirm;

	public bool IsOpen => isOpen;

	void Awake()
	{
		EnsureReferences();

		if (panel != null && !isOpen)
			panel.SetActive(false);

		if (confirmButton != null)
		{
			confirmButton.onClick.RemoveAllListeners();
			confirmButton.onClick.AddListener(OnConfirmClicked);
		}
	}

	public void Show(string autoExpandRecordId, Action confirmCallback, bool singleRunOnly = false)
	{
		EnsureReferences();
		expandRecordId = autoExpandRecordId;
		this.singleRunOnly = singleRunOnly;
		onConfirm = confirmCallback;

		if (panel == null)
		{
			Debug.LogError("[GameRecordUI] GameRecordPanel을 찾지 못했습니다.");
			return;
		}

		isOpen = true;
		Time.timeScale = 0f;
		panel.SetActive(true);
		panel.transform.SetAsLastSibling();

		ApplyTitle();
		ApplyRecordScrollSettings();
		EnsureListContentLayout();
		RebuildList();
		StartCoroutine(ScrollAfterLayout());
	}

	public void Hide()
	{
		isOpen = false;

		if (panel != null)
			panel.SetActive(false);
	}

	void Update()
	{
		if (!isOpen || panel == null || !panel.activeInHierarchy)
			return;

		if (!PanelKeyboardShortcutUtility.WasEscapeOrEnterPressedThisFrame())
			return;

		TryInvokeConfirm();
	}

	public bool TryHandleConfirmShortcut()
	{
		if (!isOpen || panel == null || !panel.activeInHierarchy)
			return false;

		if (!PanelKeyboardShortcutUtility.WasEscapeOrEnterPressedThisFrame())
			return false;

		TryInvokeConfirm();
		return true;
	}

	void TryInvokeConfirm()
	{
		if (confirmButton != null && confirmButton.isActiveAndEnabled && confirmButton.interactable)
		{
			confirmButton.onClick.Invoke();
			return;
		}

		OnConfirmClicked();
	}

	void ApplyTitle()
	{
		if (titleText == null)
			return;

		titleText.text = "플레이 기록";
		titleText.fontSize = 32f;
		TmpKoreanFontUtility.ApplyFont(titleText, koreanFont);
	}

	void RebuildList()
	{
		ClearRows();

		if (rowTemplate == null || listContent == null)
			return;

		IReadOnlyList<GameRunRecord> records = ResolveRecordsForDisplay();
		if (records.Count == 0)
		{
			CreateEmptyLabel();
			return;
		}

		GameRecordRowView expandTarget = null;

		foreach (GameRunRecord record in records)
		{
			if (record == null)
				continue;

			GameRecordRowView row = Instantiate(rowTemplate, listContent);
			row.gameObject.SetActive(true);
			row.Bind(record, koreanFont, OnRowToggleRequested);
			row.ApplyFullWidthLayout();
			rows.Add(row);

			if (!string.IsNullOrEmpty(expandRecordId) && record.id == expandRecordId)
				expandTarget = row;
		}

		if (expandTarget != null)
			OnRowToggleRequested(expandTarget);

		LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
	}

	IReadOnlyList<GameRunRecord> ResolveRecordsForDisplay()
	{
		if (!singleRunOnly)
			return GameRunRecordStore.LoadAll();

		GameRunRecord record = !string.IsNullOrEmpty(expandRecordId)
			? GameRunRecordStore.FindById(expandRecordId)
			: null;

		if (record == null)
		{
			IReadOnlyList<GameRunRecord> all = GameRunRecordStore.LoadAll();
			if (all.Count > 0)
				record = all[0];
		}

		if (record == null)
			return System.Array.Empty<GameRunRecord>();

		return new[] { record };
	}

	void CreateEmptyLabel()
	{
		GameObject labelGo = new GameObject("EmptyLabel", typeof(RectTransform));
		labelGo.transform.SetParent(listContent, false);

		RectTransform rect = labelGo.GetComponent<RectTransform>();
		rect.sizeDelta = new Vector2(0f, 80f);

		TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
		tmp.text = "저장된 기록이 없습니다.";
		tmp.fontSize = 32f;
		tmp.alignment = TextAlignmentOptions.Center;
		TmpKoreanFontUtility.ApplyFont(tmp, koreanFont);
	}

	void OnRowToggleRequested(GameRecordRowView target)
	{
		bool willExpand = !target.IsExpanded;

		foreach (GameRecordRowView row in rows)
			row.SetExpanded(row == target && willExpand, notify: false);

		if (listContent != null)
			LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);

		if (willExpand && target != null)
			target.ApplyFullWidthLayout();
	}

	void OnConfirmClicked()
	{
		Hide();
		onConfirm?.Invoke();
		onConfirm = null;
	}

	void ClearRows()
	{
		foreach (GameRecordRowView row in rows)
		{
			if (row != null)
				Destroy(row.gameObject);
		}

		rows.Clear();

		if (listContent == null)
			return;

		for (int i = listContent.childCount - 1; i >= 0; i--)
			Destroy(listContent.GetChild(i).gameObject);
	}

	IEnumerator ScrollAfterLayout()
	{
		yield return null;
		EnsureListContentLayout();
		foreach (GameRecordRowView row in rows)
			row.ApplyFullWidthLayout();

		if (listContent != null)
			LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
	}

	void ApplyRecordScrollSettings()
	{
		if (recordScrollRect == null)
			return;

		ScrollRectContentUtility.ApplyVerticalOnlyScroll(recordScrollRect, scrollSensitivity);
	}

	void EnsureListContentLayout()
	{
		if (listContent == null)
			return;

		listContent.anchorMin = new Vector2(0f, 1f);
		listContent.anchorMax = new Vector2(1f, 1f);
		listContent.pivot = new Vector2(0.5f, 1f);
		listContent.offsetMin = new Vector2(0f, listContent.offsetMin.y);
		listContent.offsetMax = new Vector2(0f, listContent.offsetMax.y);

		VerticalLayoutGroup vertical = listContent.GetComponent<VerticalLayoutGroup>();
		if (vertical == null)
			vertical = listContent.gameObject.AddComponent<VerticalLayoutGroup>();

		vertical.childAlignment = TextAnchor.UpperCenter;
		vertical.childControlWidth = true;
		vertical.childControlHeight = true;
		vertical.childForceExpandWidth = true;
		vertical.childForceExpandHeight = false;
		vertical.spacing = 10f;
		vertical.padding = new RectOffset(4, 4, 4, 4);

		ContentSizeFitter fitter = listContent.GetComponent<ContentSizeFitter>();
		if (fitter == null)
			fitter = listContent.gameObject.AddComponent<ContentSizeFitter>();
		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
	}

	void EnsureReferences()
	{
		if (panel == null)
		{
			GameObject found = GameObject.Find("GameRecordPanel");
			if (found != null)
				panel = found;
		}

		if (panel != null && recordScrollRect == null)
		{
			Transform scroll = panel.transform.Find("Window/RecordScrollView");
			if (scroll == null)
				scroll = panel.transform.Find("RecordScrollView");
			if (scroll != null)
				recordScrollRect = scroll.GetComponent<ScrollRect>();
		}

		if (panel != null && listContent == null)
		{
			Transform content = panel.transform.Find("Window/RecordScrollView/Viewport/Content");
			if (content == null)
				content = panel.transform.Find("RecordScrollView/Viewport/Content");
			if (content != null)
				listContent = content as RectTransform;
		}

		ApplyRecordScrollSettings();

		if (panel != null && rowTemplate == null)
		{
			Transform template = panel.transform.Find("Window/RecordRowTemplate");
			if (template == null)
				template = panel.transform.Find("RecordRowTemplate");
			if (template != null)
				rowTemplate = template.GetComponent<GameRecordRowView>();
		}

		if (panel != null && confirmButton == null)
		{
			Transform confirm = panel.transform.Find("Window/ConfirmButton");
			if (confirm == null)
				confirm = panel.transform.Find("ConfirmButton");
			if (confirm != null)
				confirmButton = confirm.GetComponent<Button>();
		}

		if (panel != null && titleText == null)
		{
			Transform title = panel.transform.Find("Window/Title");
			if (title == null)
				title = panel.transform.Find("Title");
			if (title != null)
				titleText = title.GetComponent<TextMeshProUGUI>();
		}

		koreanFont = TmpKoreanFontUtility.ResolveNeoDgmFont(koreanFont);
	}
}
