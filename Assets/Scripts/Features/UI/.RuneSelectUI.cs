 using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 룬 선택 UI — 스테이지 시작 전 룬 3개를 선택하고 순서를 정하는 창
/// 슬롯 클릭 → 선택 상태 → 룬 목록 클릭 → 장착
/// 슬롯끼리 클릭 → 스왑
/// </summary>
public class RuneSelectUI : MonoBehaviour
{
    [Header("# 슬롯 UI")]
    // 슬롯 3개 버튼
    public Button[] slotButtons;
    // 슬롯 아이콘 이미지
    public Image[] slotIcons;
    // 슬롯 이름 텍스트
    public TextMeshProUGUI[] slotNames;
    // 슬롯 선택 표시 (하이라이트 테두리 등)
    public Image[] slotHighlights;

    [Header("# 룬 목록 UI")]
    // 룬 목록 버튼들 (15개)
    public Button[] runeButtons;
    // 룬 아이콘 이미지
    public Image[] runeIcons;
    // 룬 이름 텍스트
    public TextMeshProUGUI[] runeNames;
    // 룬 장착 여부 표시 (어두워지는 효과)
    public Image[] runeDimOverlays;

    [Header("# 기타 UI")]
    // 선택된 룬 설명 텍스트
    public TextMeshProUGUI descText;
    // 런타임 에러 경고 텍스트
    public TextMeshProUGUI warningText;
    // 게임 시작 버튼
    public Button startButton;
    // 장착 해제 버튼
    public Button clearSlotButton;

    [Header("# 룬 데이터")]
    // 15개 룬 데이터 (인스펙터에서 연결)
    public RuneData[] allRunes;

    // 현재 선택된 슬롯 인덱스 (-1이면 선택 없음)
    int selectedSlotIndex = -1;
    // 슬롯 선택 색상
    Color highlightColor = new Color(1f, 0.8f, 0f, 1f);   // 노란색 테두리
Color normalColor   = new Color(1f, 1f, 1f, 0f);       // 투명

    void Start()
    {
        // 슬롯 버튼 이벤트 등록
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int idx = i;
            slotButtons[i].onClick.AddListener(() => OnSlotClicked(idx));
        }

        // 룬 버튼 이벤트 등록
        for (int i = 0; i < runeButtons.Length; i++)
        {
            int idx = i;
            runeButtons[i].onClick.AddListener(() => OnRuneClicked(idx));
        }

        // 시작 버튼
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        // 장착 해제 버튼
        if (clearSlotButton != null)
            clearSlotButton.onClick.AddListener(OnClearSlotClicked);

        // 초기 UI 갱신
        RefreshAll();
    }

    // ─────────────────────────────────────────────────────
    // 슬롯 클릭
    // ─────────────────────────────────────────────────────
    void OnSlotClicked(int slotIndex)
    {
        // 이미 선택된 슬롯 클릭 → 선택 해제
        if (selectedSlotIndex == slotIndex)
        {
            selectedSlotIndex = -1;
            RefreshSlotHighlights();
            return;
        }

        // 다른 슬롯이 이미 선택된 상태 → 스왑
        if (selectedSlotIndex != -1)
        {
            RuneManager.instance.SwapSlots(selectedSlotIndex, slotIndex);
            selectedSlotIndex = -1;
            RefreshAll();
            return;
        }

        // 슬롯 선택
        selectedSlotIndex = slotIndex;
        RefreshSlotHighlights();

        // 선택된 슬롯의 룬 설명 표시
        RuneData current = RuneManager.instance.GetSlot(slotIndex);
        if (current != null)
            ShowDesc(current);
    }

    // ─────────────────────────────────────────────────────
    // 룬 목록 클릭
    // ─────────────────────────────────────────────────────
    void OnRuneClicked(int runeIndex)
    {
        if (runeIndex >= allRunes.Length) return;
        RuneData rune = allRunes[runeIndex];

        // 설명 표시
        ShowDesc(rune);

        // 슬롯이 선택된 상태면 장착
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
                // ✅ 아이콘 있을 때 불투명하게
                slotIcons[i].color = Color.white;
            }
            if (slotNames[i] != null)
                slotNames[i].text = rune.runeName;
        }
        else
        {
            if (slotIcons[i] != null)
            {
                slotIcons[i].sprite = null;
                slotIcons[i].enabled = false;
                // ✅ 룬 없을 때 완전 투명
                slotIcons[i].color = new Color(1f, 1f, 1f, 0f);
            }
            if (slotNames[i] != null)
                slotNames[i].text = "Empty";
        }
    }
}

void RefreshRuneList()
{
    var active = RuneManager.instance.GetActiveRunes();

    for (int i = 0; i < runeButtons.Length; i++)
    {
        if (i >= allRunes.Length) break;
        RuneData rune = allRunes[i];

        // 룬 버튼 Image 흰색 박스
        Image btnImage = runeButtons[i].GetComponent<Image>();
        if (btnImage != null)
            btnImage.color = Color.white;

        if (runeIcons[i] != null)
        {
            runeIcons[i].sprite  = rune.runeIcon;
            runeIcons[i].enabled = rune.runeIcon != null;
        }

        if (runeNames[i] != null)
            runeNames[i].text = rune.runeName;

        if (runeDimOverlays[i] != null)
            runeDimOverlays[i].enabled = false;
    }
}

void RefreshSlotHighlights()
{
    for (int i = 0; i < slotButtons.Length; i++)
    {
        // 버튼 Image 색상 설정
        Image btnImage = slotButtons[i].GetComponent<Image>();
        if (btnImage != null)
        {
            btnImage.color = (i == selectedSlotIndex)
                ? new Color(1f, 0.8f, 0f, 1f)   // 선택 → 노란색
                : Color.white;                    // 기본 → 흰색 박스
        }

        ColorBlock cb = slotButtons[i].colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = Color.white;
        cb.pressedColor     = Color.white;
        cb.selectedColor    = Color.white;
        slotButtons[i].colors = cb;
    }
}

    void RefreshWarning()
    {
        if (warningText == null) return;
        warningText.text = RuneManager.instance.CurrentWarningMessage;
        warningText.gameObject.SetActive(!RuneManager.instance.IsCurrentCombinationValid);
    }

    void ShowDesc(RuneData rune)
    {
        if (descText == null) return;
        descText.text = $"<b>{rune.runeName}</b>\n{rune.runeDesc}";
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
