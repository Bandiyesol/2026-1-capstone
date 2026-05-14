using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// <summary>
// 룬 선택 UI — 스테이지 시작 전 룬 3개를 선택하고 순서를 정하는 창
// 슬롯 클릭 → 선택 상태 → 룬 목록 클릭 → 장착
// 슬롯끼리 클릭 → 스왑
// </summary>
public class RuneSelectUI : MonoBehaviour
{
	[Header("# 슬롯 UI")]
	public Button[] slotButtons;
	public Image[] slotIcons;
	public TextMeshProUGUI[] slotNames;
	public Image[] slotHighlights;

	[Header("# 룬 목록 UI")]
	public Button[] runeButtons;
	public Image[] runeIcons;
	public TextMeshProUGUI[] runeNames;
	public Image[] runeDimOverlays;

	[Header("# 기타 UI")]
	public TextMeshProUGUI descText;
	public TextMeshProUGUI warningText;
	public Button startButton;
	public Button clearSlotButton;

	[Header("# 룬 데이터")]
	public RuneData[] allRunes;

	int selectedSlotIndex = -1;
	Color highlightColor = new Color(1f, 0.8f, 0f, 1f);
	Color normalColor = new Color(1f, 1f, 1f, 0f);



	void Start()
	{
		for (int i = 0; i < slotButtons.Length; i++)
		{
			int idx = i;
			slotButtons[i].onClick.AddListener(() => OnSlotClicked(idx));
		}

		for (int i = 0; i < runeButtons.Length; i++)
		{
			int idx = i;
			runeButtons[i].onClick.AddListener(() => OnRuneClicked(idx));
		}

		if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
		if (clearSlotButton != null) clearSlotButton.onClick.AddListener(OnClearSlotClicked);

		RefreshAll();
	}


	// ─────────────────────────────────────────────────────
	// 슬롯 클릭
	// ─────────────────────────────────────────────────────
	void OnSlotClicked(int slotIndex)
	{
		if (selectedSlotIndex == slotIndex)
		{
			selectedSlotIndex = -1;
			RefreshSlotHighlights();
			return;
		}

		if (selectedSlotIndex != -1)
		{
			RuneManager.instance.SwapSlots(selectedSlotIndex, slotIndex);
			selectedSlotIndex = -1;
			RefreshAll();
			return;
		}

		selectedSlotIndex = slotIndex;
		RefreshSlotHighlights();

		RuneData current = RuneManager.instance.GetSlot(slotIndex);
		if (current != null) ShowDesc(current);
	}


	// ─────────────────────────────────────────────────────
	// 룬 목록 클릭
	// ─────────────────────────────────────────────────────
	void OnRuneClicked(int runeIndex)
	{
		if (runeIndex >= allRunes.Length) return;
		RuneData rune = allRunes[runeIndex];
		ShowDesc(rune);

		if (selectedSlotIndex != -1)
		{
			RuneManager.instance.SetRune(selectedSlotIndex, rune);
			selectedSlotIndex = -1;
			RefreshAll();
		}
	}


	// ─────────────────────────────────────────────────────
	// 슬롯 장착 해제
	// ─────────────────────────────────────────────────────
	void OnClearSlotClicked()
	{
		if (selectedSlotIndex == -1) return;
		RuneManager.instance.ClearSlot(selectedSlotIndex);
		selectedSlotIndex = -1;
		RefreshAll();
	}


	// ─────────────────────────────────────────────────────
	// 게임 시작
	// ─────────────────────────────────────────────────────
	public void OnStartClicked()
	{
		gameObject.SetActive(false);
		GameManager.instance.Resume();
	}


	public void OnClearAllClicked()
	{
		RuneManager.instance.ClearAll();
		selectedSlotIndex = -1;
		RefreshAll();
	}


	// ─────────────────────────────────────────────────────
	// UI 갱신
	// ─────────────────────────────────────────────────────
	void RefreshAll()
	{
		RefreshSlots();
		RefreshRuneList();
		RefreshSlotHighlights();
		RefreshWarning();
	}


	void RefreshSlots()
	{
		for (int i = 0; i < slotButtons.Length; i++)
		{
			RuneData rune = RuneManager.instance.GetSlot(i);

			if (rune != null)
			{
				if (slotIcons[i] != null)
				{
					slotIcons[i].sprite = rune.runeIcon;
					slotIcons[i].enabled = rune.runeIcon != null;
					slotIcons[i].color = Color.white;
				}

				if (slotNames[i] != null) slotNames[i].text = rune.runeName;
			}

			else
			{
				if (slotIcons[i] != null)
				{
					slotIcons[i].sprite = null;
					slotIcons[i].enabled = false;
					slotIcons[i].color = new Color(1f, 1f, 1f, 0f);
				}

				if (slotNames[i] != null) slotNames[i].text = "Empty";
			}
		}
	}


	void RefreshRuneList()
	{
		var active = RuneManager.instance.GetActiveRunes();

		for (int i = 0; i < runeButtons.Length; i++)
		{
			if (i >= allRunes.Length)
			{
				runeButtons[i].gameObject.SetActive(false);
				continue;
			}

			RuneData rune = allRunes[i];
			runeButtons[i].gameObject.SetActive(true);

			Image btnImage = runeButtons[i].GetComponent<Image>();
			if (btnImage != null) btnImage.color = Color.white;

			if (runeIcons[i] != null)
			{
				runeIcons[i].sprite = rune.runeIcon;
				runeIcons[i].enabled = rune.runeIcon != null;
			}

			if (runeNames[i] != null)runeNames[i].text = rune.runeName;

			if (runeDimOverlays[i] != null)
			{
				bool isEquipped = active.Contains(rune);
				runeDimOverlays[i].enabled = isEquipped;
			}
		}
	}


	void RefreshSlotHighlights()
	{
		for (int i = 0; i < slotButtons.Length; i++)
		{
			Image btnImage = slotButtons[i].GetComponent<Image>();
			if (btnImage != null)
			{
				btnImage.color = (i == selectedSlotIndex)
					? new Color(1f, 0.8f, 0f, 1f)
					: Color.white;
			}

			ColorBlock cb = slotButtons[i].colors;
			cb.normalColor = Color.white;
			cb.highlightedColor = Color.white;
			cb.pressedColor = Color.white;
			cb.selectedColor = Color.white;
			slotButtons[i].colors = cb;
		}
	}


	void RefreshWarning()
	{
		if (warningText == null) return;

		bool isValid = RuneManager.instance.IsCurrentCombinationValid;
		warningText.text = isValid ? string.Empty : RuneManager.instance.CurrentWarningMessage;
		warningText.gameObject.SetActive(!RuneManager.instance.IsCurrentCombinationValid);
	}


	void ShowDesc(RuneData rune)
	{
		if (descText == null) return;
		descText.text = $"<b>{rune.runeName}</b>\n{rune.runeDescription}";
	}


	// ─────────────────────────────────────────────────────
	// 외부에서 열기/닫기
	// ─────────────────────────────────────────────────────
	public void Show()
	{
		gameObject.SetActive(true);
		selectedSlotIndex = -1;
		RefreshAll();
		GameManager.instance.Stop();
	}


	public void Hide()
	{
		gameObject.SetActive(false);
		GameManager.instance.Resume();
	}
}
