using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 HUD — 현재 장착 룬 순서를 읽기 전용으로 표시합니다.
/// 룬 순서 선택 UI와 비슷한 레이아웃이지만 순서 변경은 불가합니다.
/// </summary>
public class RuneLoadoutViewUI : MonoBehaviour
{
	[Header("# 패널")]
	[SerializeField] GameObject panel;
	[SerializeField] Button closeButton;

	[Header("# 슬롯 표시")]
	[SerializeField] Image[] slotIcons;
	[SerializeField] TextMeshProUGUI[] slotTitles;
	[SerializeField] TextMeshProUGUI titleLabel;
	[SerializeField] TextMeshProUGUI infoLabel;

	[Header("# 폰트")]
	[SerializeField] TMP_FontAsset koreanFont;

	const float InfoFontSize = 30f;
	const float InfoBottomOffsetY = 168f;

	bool isOpen;
	bool initialized;
	bool pausedByPanel;

	public bool IsOpen => isOpen && gameObject.activeInHierarchy;

	void Awake()
	{
		EnsureInitialized();
	}

	void OnEnable()
	{
		EnsureInitialized();
	}

	void OnDestroy()
	{
		ResumeGameIfPaused();
		if (closeButton != null)
			closeButton.onClick.RemoveListener(Close);
	}

	public void Toggle()
	{
		EnsureInitialized();
		if (isOpen)
			Close();
		else
			Open();
	}

	public void Open()
	{
		EnsureInitialized();
		if (panel == null)
			panel = gameObject;

		isOpen = true;
		gameObject.SetActive(true);
		panel.SetActive(true);
		GameAudio.PlayPanelOpen();
		OverlayPanelUILayout.Apply(panel.transform);
		ChoiceSelectUILayout.Apply(panel.transform);
		PrepareReadOnlyPanel();
		ApplyKoreanFontToPanel();
		Refresh();
		PauseGameIfLive();
	}

	public void Close()
	{
		isOpen = false;
		if (panel != null)
			panel.SetActive(false);
		gameObject.SetActive(false);
		ResumeGameIfPaused();
	}

	public bool TryHandleEscape()
	{
		if (!IsOpen)
			return false;

		if (closeButton != null)
			closeButton.onClick.Invoke();
		else
			Close();

		return true;
	}

	void EnsureInitialized()
	{
		if (initialized)
			return;

		initialized = true;
		AutoBindReferences();

		if (panel != null && !isOpen)
		{
			panel.SetActive(false);
			gameObject.SetActive(false);
		}

		if (closeButton != null)
			closeButton.onClick.AddListener(Close);

		koreanFont = TmpKoreanFontUtility.ResolveNeoDgmFont(koreanFont);
	}

	void AutoBindReferences()
	{
		if (panel == null || panel == gameObject)
		{
			Transform childPanel = transform.Find("LoadoutPanel");
			panel = childPanel != null ? childPanel.gameObject : gameObject;
		}

		Transform box = OverlayPanelUILayout.FindBoxPanel(panel.transform);
		if (box == null)
			return;

		if (closeButton == null)
		{
			Transform close = box.Find("CloseBtn") ?? box.Find("Close");
			if (close != null)
				closeButton = close.GetComponent<Button>();
		}

		if (titleLabel == null)
		{
			foreach (TextMeshProUGUI tmp in box.GetComponentsInChildren<TextMeshProUGUI>(true))
			{
				if (tmp.transform.name.StartsWith("Btn"))
					continue;

				titleLabel = tmp;
				break;
			}
		}

		if (infoLabel == null)
		{
			Transform info = panel.transform.Find("InfoText");
			if (info != null)
				infoLabel = info.GetComponent<TextMeshProUGUI>();
		}

		BindSlotArray(ref slotIcons, "Icon");
		BindSlotArray(ref slotTitles, "Title");
	}

	void BindSlotArray<T>(ref T[] array, string childName) where T : Component
	{
		if (array != null && array.Length > 0)
			return;

		Transform box = OverlayPanelUILayout.FindBoxPanel(panel.transform);
		if (box == null)
			return;

		var result = new T[3];
		for (int i = 0; i < 3; i++)
		{
			Transform btn = box.Find($"Btn{i}");
			if (btn == null)
				continue;

			Transform child = btn.Find(childName);
			if (child != null)
				result[i] = child.GetComponent<T>();

			if (btn.TryGetComponent(out Button button))
				button.interactable = false;
		}

		array = result;
	}

	void PrepareReadOnlyPanel()
	{
		if (panel == null)
			return;

		RemoveReadOnlyControls();
		EnsureInfoLabel();
		ApplyInfoLabelLayout();

		Transform box = OverlayPanelUILayout.FindBoxPanel(panel.transform);
		if (box == null)
			return;

		for (int i = 0; i < 3; i++)
		{
			Transform btn = box.Find($"Btn{i}");
			if (btn == null)
				continue;

			if (btn.TryGetComponent(out Button button))
			{
				button.interactable = false;
				button.onClick.RemoveAllListeners();

				ColorBlock colors = button.colors;
				colors.normalColor = Color.white;
				colors.highlightedColor = Color.white;
				colors.pressedColor = Color.white;
				colors.selectedColor = Color.white;
				colors.disabledColor = Color.white;
				button.colors = colors;
			}

			if (btn.TryGetComponent(out Image cardImage))
			{
				Color color = cardImage.color;
				color.a = 1f;
				cardImage.color = color;
			}

			foreach (Image image in btn.GetComponentsInChildren<Image>(true))
			{
				if (image == null)
					continue;

				Color color = image.color;
				color.a = 1f;
				image.color = color;
			}
		}
	}

	void RemoveReadOnlyControls()
	{
		if (panel == null)
			return;

		foreach (Transform child in panel.GetComponentsInChildren<Transform>(true))
		{
			if (child == null)
				continue;

			string name = child.name;
			if (name == "StartButton" || name == "WarningText")
			{
				if (Application.isPlaying)
					Destroy(child.gameObject);
				else
					child.gameObject.SetActive(false);
			}
		}

		foreach (Button button in panel.GetComponentsInChildren<Button>(true))
		{
			if (button == null)
				continue;

			Transform label = button.transform.Find("Label");
			if (label != null && label.TryGetComponent(out TextMeshProUGUI tmp)
			    && tmp.text.Equals("Start", System.StringComparison.OrdinalIgnoreCase))
			{
				if (Application.isPlaying)
					Destroy(button.gameObject);
				else
					button.gameObject.SetActive(false);
			}
		}
	}

	void EnsureInfoLabel()
	{
		if (infoLabel != null)
			return;

		Transform info = panel.transform.Find("InfoText");
		if (info != null)
			infoLabel = info.GetComponent<TextMeshProUGUI>();
	}

	void ApplyInfoLabelLayout()
	{
		EnsureInfoLabel();
		if (infoLabel == null)
			return;

		infoLabel.fontSize = InfoFontSize;
		infoLabel.alignment = TextAlignmentOptions.Center;

		if (infoLabel.transform is RectTransform rect)
		{
			rect.anchorMin = new Vector2(0.5f, 0f);
			rect.anchorMax = new Vector2(0.5f, 0f);
			rect.pivot = new Vector2(0.5f, 0f);
			rect.anchoredPosition = new Vector2(0f, InfoBottomOffsetY);
			rect.sizeDelta = new Vector2(900f, 56f);
		}
	}

	void ApplyKoreanFontToPanel()
	{
		koreanFont = TmpKoreanFontUtility.ResolveNeoDgmFont(koreanFont);
		if (panel == null || koreanFont == null)
			return;

		foreach (TextMeshProUGUI tmp in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
		{
			if (tmp == null || !tmp.gameObject.activeInHierarchy)
				continue;

			if (tmp.transform.name == "WarningText")
			{
				tmp.gameObject.SetActive(false);
				continue;
			}

			TmpKoreanFontUtility.ApplyFont(tmp, koreanFont);
			if (!string.IsNullOrEmpty(tmp.text))
				TmpKoreanFontUtility.EnsureGlyphs(tmp, koreanFont, tmp.text);
		}
	}

	void Refresh()
	{
		if (titleLabel != null)
		{
			titleLabel.text = "내 룬";
			TmpKoreanFontUtility.ApplyFont(titleLabel, koreanFont);
			TmpKoreanFontUtility.EnsureGlyphs(titleLabel, koreanFont, titleLabel.text);
		}

		if (infoLabel != null)
		{
			infoLabel.text = "현재 장착 순서입니다.";
			ApplyInfoLabelLayout();
			TmpKoreanFontUtility.ApplyFont(infoLabel, koreanFont);
			TmpKoreanFontUtility.EnsureGlyphs(infoLabel, koreanFont, infoLabel.text);
		}

		if (RuneManager.instance == null)
			return;

		for (int i = 0; i < 3; i++)
		{
			RuneData rune = RuneManager.instance.GetSlot(i);
			string title = rune != null ? RuneRewardService.FormatType(rune) : "Empty";
			string detail = rune != null ? RuneRewardService.FormatDescription(rune) : string.Empty;

			if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
			{
				if (rune != null)
					RuneCategoryDisplay.ApplyChoiceIcon(slotIcons[i], rune);
				else
				{
					slotIcons[i].sprite = null;
					slotIcons[i].enabled = false;
				}
			}

			if (slotTitles != null && i < slotTitles.Length && slotTitles[i] != null)
			{
				slotTitles[i].text = title;
				TmpKoreanFontUtility.ApplyFont(slotTitles[i], koreanFont);
				TmpKoreanFontUtility.EnsureGlyphs(slotTitles[i], koreanFont, title);
			}

			ApplySlotDetail(i, detail);
		}
	}

	void ApplySlotDetail(int index, string detailText)
	{
		Transform box = OverlayPanelUILayout.FindBoxPanel(panel.transform);
		if (box == null)
			return;

		Transform btn = box.Find($"Btn{index}");
		if (btn == null)
			return;

		Transform detail = btn.Find("Detail");
		if (detail != null && detail.TryGetComponent(out TextMeshProUGUI detailTmp))
		{
			detailTmp.text = detailText;
			detailTmp.richText = false;
			detailTmp.color = Color.black;
			TmpKoreanFontUtility.ApplyFont(detailTmp, koreanFont);
			TmpKoreanFontUtility.EnsureGlyphs(detailTmp, koreanFont, detailText);
		}
	}

	void PauseGameIfLive()
	{
		if (GameManager.instance == null || !GameManager.instance.isLive)
			return;

		GameManager.instance.PauseForOverlayPanel();
		pausedByPanel = true;
	}

	void ResumeGameIfPaused()
	{
		if (!pausedByPanel || GameManager.instance == null)
			return;

		pausedByPanel = false;
		GameManager.instance.ResumeGameplayFromOverlay();
	}
}
