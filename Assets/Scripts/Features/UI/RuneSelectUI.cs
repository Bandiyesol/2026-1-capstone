using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 룬 선택 UI — 무기 선택과 동일한 3카드 후보 화면 → 선택 후 내 룬(순서 변경) 화면.
/// 라운드당 1개만 획득, 최대 3개.
/// </summary>
public class RuneSelectUI : MonoBehaviour
{
    const int ChoiceCount = 3;

    enum UiPhase
    {
        Pick,
        Loadout,
    }

    [Header("# 패널 (비우면 자동 탐색/생성)")]
    [SerializeField] GameObject choicePanelRoot;
    [SerializeField] GameObject loadoutPanelRoot;
    [Tooltip("켜면 무기 선택 UI와 같은 고정 크기(1520x960)로 덮어씁니다. 씬에서 직접 배치한 경우 끄세요.")]
    [SerializeField] bool useAutoLayout;

    [Header("# 소지 슬롯 (순서 변경)")]
    [SerializeField] Button[] slotButtons;
    [SerializeField] Image[] slotIcons;
    [SerializeField] TextMeshProUGUI[] slotNames;

    [Header("# 후보 룬 (Btn 0,1,2)")]
    [SerializeField] Button[] choiceButtons;
    [SerializeField] Image[] choiceIcons;
    [SerializeField] TextMeshProUGUI[] choiceTitleLabels;
    [SerializeField] TextMeshProUGUI[] choiceDetailLabels;

    [Header("# 기타 UI")]
    [SerializeField] TextMeshProUGUI titleLabel;
    [SerializeField] TextMeshProUGUI loadoutTitleLabel;
    [SerializeField] TextMeshProUGUI warningText;
    [SerializeField] Button confirmButton;
    [SerializeField] Button startButton;

    [Header("# 룬 데이터")]
    [Tooltip("비어 있으면 Rune Catalog에서 자동 로드")]
    public RuneData[] allRunes;
    public RuneCatalog runeCatalog;

    readonly List<RuneData> currentCandidates = new List<RuneData>();
    UiPhase currentPhase;
    int selectedSlotIndex = -1;
    bool pickedThisRound;
    bool reorderOnlyMode;
    Action onConfirmed;
    bool beginSessionOnConfirm;
    TMP_FontAsset koreanFont;
    bool choiceUiBuilt;
    TextMeshProUGUI runtimeLoadoutTitle;

    void Awake()
    {
        koreanFont = TmpKoreanFontUtility.ResolveNeoDgmFont(null);
        EnsureUiStructure();
        BindChoiceUiFromButtons();
        ApplyKoreanFont();
    }

    void Start()
    {
        EnsureAllRunesLoaded();
        WireButtons();
    }

    void WireButtons()
    {
        if (slotButtons != null)
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] == null)
                    continue;

                int idx = i;
                slotButtons[i] = RewireButton(slotButtons[i], () => OnSlotClicked(idx));
            }
        }

        if (choiceButtons != null)
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] == null)
                    continue;

                int idx = i;
                choiceButtons[i] = RewireButton(choiceButtons[i], () => OnPickRune(idx));
            }
        }

        if (confirmButton != null)
            confirmButton = RewireButton(confirmButton, OnConfirmClicked);

        if (startButton != null && startButton != confirmButton)
            startButton = RewireButton(startButton, OnConfirmClicked);
    }

    /// <summary>
    /// WeaponSelectUI 복제 시 Inspector에 남는 OnPickWeapon persistent 이벤트를 제거합니다.
    /// RemoveAllListeners()만으로는 persistent 호출이 지워지지 않습니다.
    /// </summary>
    static Button RewireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return null;

        if (button.onClick.GetPersistentEventCount() > 0)
            button = ReplaceButtonComponent(button);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        return button;
    }

    static Button ReplaceButtonComponent(Button button)
    {
        GameObject go = button.gameObject;
        Graphic targetGraphic = button.targetGraphic;
        Selectable.Transition transition = button.transition;
        ColorBlock colors = button.colors;
        SpriteState spriteState = button.spriteState;
        Navigation navigation = button.navigation;
        bool interactable = button.interactable;

        if (Application.isPlaying)
            Destroy(button);
        else
            DestroyImmediate(button);

        Button fresh = go.AddComponent<Button>();
        fresh.targetGraphic = targetGraphic;
        fresh.transition = transition;
        fresh.colors = colors;
        fresh.spriteState = spriteState;
        fresh.navigation = navigation;
        fresh.interactable = interactable;
        return fresh;
    }

    void EnsureUiStructure()
    {
        if (useAutoLayout)
            EnsureFullScreenRoot();

        HideLegacyRuneListGroup();

        if (choicePanelRoot == null)
            choicePanelRoot = FindChoicePanelObject(transform);

        if (choicePanelRoot == null)
            TryCloneChoicePanelFromWeapon();

        if (loadoutPanelRoot == null)
        {
            Transform slotGroup = transform.Find("SlotGroup");
            if (slotGroup != null)
                loadoutPanelRoot = slotGroup.gameObject;
        }

        HideClearSlotButton();
        HideSequencePanelIfPresent();
    }

    void HideSequencePanelIfPresent()
    {
        Transform sequence = transform.Find("SequencePanel");
        if (sequence != null)
            sequence.gameObject.SetActive(false);
    }

    void EnsureFullScreenRoot()
    {
        if (transform is not RectTransform rect)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    void HideLegacyRuneListGroup()
    {
        Transform legacy = transform.Find("RuneListGroup");
        if (legacy != null)
            legacy.gameObject.SetActive(false);

        Transform desc = transform.Find("DescText");
        if (desc != null)
            desc.gameObject.SetActive(false);
    }

    void HideClearSlotButton()
    {
        Transform clear = transform.Find("ClearSlotButton");
        if (clear != null)
            clear.gameObject.SetActive(false);
    }

    static GameObject FindChoicePanelObject(Transform root)
    {
        Transform choicePanel = root.Find("ChoicePanel");
        if (choicePanel != null)
            return choicePanel.gameObject;

        Transform panel = FindChoiceButtonPanel(root);
        if (panel == null)
            return null;

        if (panel.parent != null && panel.parent.name == "ContentPanel")
            return panel.parent.gameObject;

        return panel.gameObject;
    }

    static Transform FindChoiceButtonPanel(Transform root)
    {
        Transform choicePanel = root.Find("ChoicePanel");
        if (choicePanel != null)
        {
            Transform box = FindBoxPanelUnder(choicePanel);
            if (box != null)
                return box;
        }

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "LoadoutPanel" || t.name == "SlotGroup")
                continue;

            if (t.name != "Btn0")
                continue;

            Transform parent = t.parent;
            if (parent != null && parent.Find("Btn1") != null && parent.Find("Btn2") != null)
                return parent;
        }

        return null;
    }

    static Transform FindBoxPanelUnder(Transform panelRoot)
    {
        Transform box = panelRoot.Find("BoxPanel");
        if (box != null)
            return box;

        foreach (Transform child in panelRoot)
        {
            if (child.Find("Btn0") != null && child.Find("Btn1") != null && child.Find("Btn2") != null)
                return child;
        }

        return null;
    }

    void TryCloneChoicePanelFromWeapon()
    {
        if (choiceUiBuilt)
            return;

        var weaponUi = FindFirstObjectByType<WeaponSelectUI>(FindObjectsInactive.Include);
        if (weaponUi == null)
            return;

        Transform template = weaponUi.transform.Find("ContentPanel");
        if (template == null)
            return;

        var clone = Instantiate(template.gameObject, transform);
        clone.name = "ChoicePanel";
        clone.transform.SetAsFirstSibling();
        choicePanelRoot = clone;
        choiceUiBuilt = true;
    }

    void ApplyKoreanFont()
    {
        ApplyFont(titleLabel);
        ApplyFont(warningText);

        if (slotNames != null)
        {
            foreach (TextMeshProUGUI label in slotNames)
                ApplyFont(label);
        }

        if (choiceTitleLabels != null)
        {
            foreach (TextMeshProUGUI label in choiceTitleLabels)
                ApplyFont(label);
        }

        if (choiceDetailLabels != null)
        {
            foreach (TextMeshProUGUI label in choiceDetailLabels)
                ApplyFont(label);
        }
    }

    void ApplyFont(TextMeshProUGUI label)
    {
        if (label == null)
            return;

        TmpKoreanFontUtility.ApplyFont(label, koreanFont);
    }

    void BindChoiceUiFromButtons()
    {
        ResolveChoiceButtons();

        if (choiceButtons == null || choiceButtons.Length == 0)
            return;

        choiceDetailLabels = BindTmpArray(choiceDetailLabels, "Detail");
        choiceTitleLabels = BindTmpArray(choiceTitleLabels, "Title");
        choiceIcons = BindImageArray(choiceIcons, "Icon");

        if (titleLabel == null)
            titleLabel = FindPanelTitleLabel();
    }

    void ResolveChoiceButtons()
    {
        if (choiceButtons != null && choiceButtons.Length == ChoiceCount
            && choiceButtons[0] != null && choiceButtons[0].name == "Btn0")
            return;

        Transform panel = FindChoiceButtonPanel(transform);
        if (panel == null)
            return;

        choiceButtons = new[]
        {
            panel.Find("Btn0")?.GetComponent<Button>(),
            panel.Find("Btn1")?.GetComponent<Button>(),
            panel.Find("Btn2")?.GetComponent<Button>(),
        };
    }

    TextMeshProUGUI FindPanelTitleLabel()
    {
        Transform panel = FindChoiceButtonPanel(transform);
        if (panel == null)
            return null;

        foreach (Transform child in panel)
        {
            if (child.name.StartsWith("Btn"))
                continue;

            TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
                return tmp;
        }

        return null;
    }

    TextMeshProUGUI[] BindTmpArray(TextMeshProUGUI[] current, string childName)
    {
        var result = new TextMeshProUGUI[choiceButtons.Length];
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (current != null && i < current.Length && current[i] != null)
            {
                result[i] = current[i];
                continue;
            }

            if (choiceButtons[i] == null)
                continue;

            Transform child = choiceButtons[i].transform.Find(childName);
            if (child != null)
                result[i] = child.GetComponent<TextMeshProUGUI>();
        }

        return result;
    }

    Image[] BindImageArray(Image[] current, string childName)
    {
        var result = new Image[choiceButtons.Length];
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (current != null && i < current.Length && current[i] != null)
            {
                result[i] = current[i];
                continue;
            }

            if (choiceButtons[i] == null)
                continue;

            Transform child = choiceButtons[i].transform.Find(childName);
            if (child == null)
                child = CreateIconChild(choiceButtons[i].transform);

            Image image = child.GetComponent<Image>();
            if (image == null)
                image = child.gameObject.AddComponent<Image>();

            image.raycastTarget = false;
            image.preserveAspect = true;
            result[i] = image;
        }

        return result;
    }

    static Transform CreateIconChild(Transform parent)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -200f);
        rect.sizeDelta = new Vector2(84f, 84f);

        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        return go.transform;
    }

    public void Show()
    {
        ShowForGameStart();
    }

    public void ShowForGameStart()
    {
        int owned = RuneManager.instance != null ? RuneManager.instance.GetFilledSlotCount() : 0;
        OpenPanel(
            beginSession: true,
            onConfirmed: null,
            pickTitle: $"룬 선택 ({Mathf.Min(owned + 1, RuneValidator.MaxSlots)}/{RuneValidator.MaxSlots})",
            loadoutTitle: "내 룬");
    }

    public void ShowBetweenWaves(Action onConfirmed)
    {
        int owned = RuneManager.instance != null ? RuneManager.instance.GetFilledSlotCount() : 0;
        bool full = owned >= RuneValidator.MaxSlots;
        OpenPanel(
            beginSession: false,
            onConfirmed: onConfirmed,
            pickTitle: $"룬 선택 ({owned + 1}/{RuneValidator.MaxSlots})",
            loadoutTitle: full ? "룬 순서 변경" : "내 룬");
    }

    void OpenPanel(bool beginSession, Action onConfirmed, string pickTitle, string loadoutTitle)
    {
        EnsureUiStructure();
        EnsureAllRunesLoaded();
        WireButtons();

        this.onConfirmed = onConfirmed;
        beginSessionOnConfirm = beginSession;
        selectedSlotIndex = -1;
        pickedThisRound = false;
        reorderOnlyMode = !beginSession && RuneManager.instance != null && RuneManager.instance.IsFull;

        gameObject.SetActive(true);
        ApplyKoreanFont();

        pendingPickTitle = pickTitle;
        pendingLoadoutTitle = loadoutTitle;

        if (reorderOnlyMode)
            EnterLoadoutPhase();
        else
            EnterPickPhase();

        GameManager.instance.Stop();
    }

    string pendingPickTitle;
    string pendingLoadoutTitle;

    void EnterPickPhase()
    {
        currentPhase = UiPhase.Pick;
        HideLoadoutTitle();
        SetChoicePanelVisible(true);
        SetLoadoutPanelVisible(false);
        RollCandidates();
        RefreshChoices();

        if (titleLabel != null)
            titleLabel.text = pendingPickTitle;

        if (useAutoLayout)
            ApplyAutoLayout();
    }

    void ApplyAutoLayout()
    {
        if (choicePanelRoot != null)
            ChoiceSelectUILayout.Apply(choicePanelRoot.transform);
        else
            ChoiceSelectUILayout.Apply(transform);
    }

    void EnterLoadoutPhase()
    {
        currentPhase = UiPhase.Loadout;
        SetChoicePanelVisible(false);
        SetLoadoutPanelVisible(true);
        selectedSlotIndex = -1;
        if (useAutoLayout)
            LayoutLoadoutPanel();
        RefreshAll();
        SetLoadoutTitle(pendingLoadoutTitle);
    }

    void SetLoadoutTitle(string text)
    {
        if (loadoutTitleLabel != null)
        {
            loadoutTitleLabel.text = text;
            loadoutTitleLabel.gameObject.SetActive(true);
            ApplyFont(loadoutTitleLabel);
            return;
        }

        if (titleLabel != null
            && (choicePanelRoot == null || !titleLabel.transform.IsChildOf(choicePanelRoot.transform)))
        {
            titleLabel.text = text;
            titleLabel.gameObject.SetActive(true);
            return;
        }

        EnsureRuntimeLoadoutTitle(text);
    }

    void EnsureRuntimeLoadoutTitle(string text)
    {
        if (runtimeLoadoutTitle == null)
        {
            var go = new GameObject("LoadoutTitle", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -120f);
            rect.sizeDelta = new Vector2(720f, 72f);

            runtimeLoadoutTitle = go.AddComponent<TextMeshProUGUI>();
            runtimeLoadoutTitle.fontSize = 34f;
            runtimeLoadoutTitle.alignment = TextAlignmentOptions.Center;
        }

        runtimeLoadoutTitle.text = text;
        runtimeLoadoutTitle.gameObject.SetActive(true);
        ApplyFont(runtimeLoadoutTitle);
    }

    void HideLoadoutTitle()
    {
        if (loadoutTitleLabel != null)
            loadoutTitleLabel.gameObject.SetActive(false);

        if (runtimeLoadoutTitle != null)
            runtimeLoadoutTitle.gameObject.SetActive(false);
    }

    void LayoutLoadoutPanel()
    {
        if (loadoutPanelRoot != null && loadoutPanelRoot.transform is RectTransform slotRect)
        {
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(0f, 40f);
        }

        Button confirm = confirmButton != null ? confirmButton : startButton;
        if (confirm != null && confirm.transform is RectTransform confirmRect)
        {
            confirmRect.anchorMin = new Vector2(0.5f, 0f);
            confirmRect.anchorMax = new Vector2(0.5f, 0f);
            confirmRect.pivot = new Vector2(0.5f, 0.5f);
            confirmRect.anchoredPosition = new Vector2(0f, 80f);
        }

        if (warningText != null && warningText.transform is RectTransform warnRect)
        {
            warnRect.anchorMin = new Vector2(0.5f, 0f);
            warnRect.anchorMax = new Vector2(0.5f, 0f);
            warnRect.pivot = new Vector2(0.5f, 0f);
            warnRect.anchoredPosition = new Vector2(0f, 150f);
        }
    }

    void SetChoicePanelVisible(bool visible)
    {
        if (choicePanelRoot != null)
        {
            choicePanelRoot.SetActive(visible);
            return;
        }

        if (choiceButtons == null)
            return;

        foreach (Button button in choiceButtons)
        {
            if (button != null)
                button.transform.parent.gameObject.SetActive(visible);
        }
    }

    void SetLoadoutPanelVisible(bool visible)
    {
        if (loadoutPanelRoot != null)
            loadoutPanelRoot.SetActive(visible);

        if (warningText != null)
            warningText.gameObject.SetActive(visible);

        Button confirm = confirmButton != null ? confirmButton : startButton;
        if (confirm != null)
            confirm.gameObject.SetActive(visible);
    }

    void RollCandidates()
    {
        currentCandidates.Clear();
        var owned = RuneManager.instance != null
            ? RuneManager.instance.GetActiveRunes()
            : new List<RuneData>();

        currentCandidates.AddRange(
            RuneRewardService.RollCandidates(allRunes, ChoiceCount, owned));
    }

    void OnSlotClicked(int slotIndex)
    {
        if (currentPhase != UiPhase.Loadout || RuneManager.instance == null)
            return;

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
    }

    public void OnPickRune(int index)
    {
        if (currentPhase != UiPhase.Pick || pickedThisRound || RuneManager.instance == null)
            return;

        if (index < 0 || index >= currentCandidates.Count)
            return;

        RuneData rune = currentCandidates[index];
        if (!RuneManager.instance.TryAddRune(rune))
        {
            ShowWarning("룬을 추가하지 못했습니다.");
            return;
        }

        pickedThisRound = true;
        EnterLoadoutPhase();
    }

    void OnConfirmClicked()
    {
        if (currentPhase != UiPhase.Loadout)
            return;

        if (!reorderOnlyMode && !pickedThisRound)
            return;

        HideAndContinue();
    }

    public void OnStartClicked()
    {
        OnConfirmClicked();
    }

    void HideAndContinue()
    {
        gameObject.SetActive(false);
        onConfirmed?.Invoke();
        onConfirmed = null;

        if (beginSessionOnConfirm)
            GameManager.instance.BeginGameplaySession();
        else
            GameManager.instance.ResumeGameplayFromOverlay();
    }

    void RefreshAll()
    {
        RefreshSlots();
        RefreshWarning();
        RefreshConfirmButton();
    }

    void RefreshSlots()
    {
        if (slotButtons == null || RuneManager.instance == null)
            return;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            RuneData rune = RuneManager.instance.GetSlot(i);
            string label = rune != null ? RuneRewardService.FormatType(rune) : "Empty";

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

            if (slotNames != null && i < slotNames.Length && slotNames[i] != null)
                slotNames[i].text = label;
        }

        RefreshSlotHighlights();
    }

    void RefreshChoices()
    {
        if (choiceButtons == null)
            return;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool hasChoice = i < currentCandidates.Count;
            if (choiceButtons[i] == null)
                continue;

            choiceButtons[i].gameObject.SetActive(hasChoice);
            choiceButtons[i].interactable = hasChoice && !pickedThisRound;

            if (!hasChoice)
                continue;

            ApplyChoicePreview(i, currentCandidates[i]);
        }
    }

    void ApplyChoicePreview(int index, RuneData rune)
    {
        if (choiceTitleLabels != null && index < choiceTitleLabels.Length && choiceTitleLabels[index] != null)
        {
            string name = RuneRewardService.FormatTitle(rune);
            choiceTitleLabels[index].text = name;
            choiceTitleLabels[index].richText = false;
            choiceTitleLabels[index].color = Color.black;
            TmpKoreanFontUtility.EnsureGlyphs(choiceTitleLabels[index], koreanFont, name);
        }

        if (choiceDetailLabels != null && index < choiceDetailLabels.Length && choiceDetailLabels[index] != null)
        {
            string description = RuneRewardService.FormatDescription(rune);
            choiceDetailLabels[index].text = description;
            choiceDetailLabels[index].richText = false;
            choiceDetailLabels[index].color = Color.black;
            TmpKoreanFontUtility.EnsureGlyphs(choiceDetailLabels[index], koreanFont, description);
        }

        if (choiceIcons != null && index < choiceIcons.Length && choiceIcons[index] != null)
            RuneCategoryDisplay.ApplyChoiceIcon(choiceIcons[index], rune);
    }

    void RefreshSlotHighlights()
    {
        if (slotButtons == null)
            return;

        for (int i = 0; i < slotButtons.Length; i++)
        {
            Image btnImage = slotButtons[i].GetComponent<Image>();
            if (btnImage == null)
                continue;

            btnImage.color = i == selectedSlotIndex
                ? new Color(1f, 0.8f, 0f, 1f)
                : Color.white;
        }
    }

    void RefreshConfirmButton()
    {
        Button confirm = confirmButton != null ? confirmButton : startButton;
        if (confirm == null)
            return;

        confirm.interactable = reorderOnlyMode || pickedThisRound;
    }

    void RefreshWarning()
    {
        if (warningText == null || RuneManager.instance == null)
            return;

        if (!RuneManager.instance.IsCurrentCombinationValid)
        {
            warningText.text = RuneManager.instance.CurrentWarningMessage;
            warningText.gameObject.SetActive(true);
            TmpKoreanFontUtility.EnsureGlyphs(warningText, koreanFont, warningText.text);
            return;
        }

        if (reorderOnlyMode)
            warningText.text = "슬롯 2개를 눌러 순서를 바꾼 뒤 Start를 누르세요.";
        else
            warningText.text = "슬롯 2개를 눌러 순서를 바꿀 수 있습니다. Start로 진행하세요.";

        warningText.gameObject.SetActive(true);
        TmpKoreanFontUtility.EnsureGlyphs(warningText, koreanFont, warningText.text);
    }

    void ShowWarning(string message)
    {
        if (warningText == null)
            return;

        warningText.text = message;
        warningText.gameObject.SetActive(true);
        TmpKoreanFontUtility.EnsureGlyphs(warningText, koreanFont, message);
    }

    void EnsureAllRunesLoaded()
    {
        if (runeCatalog == null)
            runeCatalog = Resources.Load<RuneCatalog>("Data/RuneCatalog");

        if (runeCatalog != null && runeCatalog.runes != null && runeCatalog.runes.Length > 0)
        {
            allRunes = FilterValidRunes(runeCatalog.runes);
            return;
        }

        if (allRunes != null && allRunes.Length > 0)
            allRunes = FilterValidRunes(allRunes);
    }

    static RuneData[] FilterValidRunes(RuneData[] source)
    {
        if (source == null || source.Length == 0)
            return Array.Empty<RuneData>();

        var list = new List<RuneData>(source.Length);
        foreach (RuneData rune in source)
        {
            if (rune != null)
                list.Add(rune);
        }

        return list.ToArray();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        onConfirmed = null;
        GameManager.instance.ResumeGameplayFromOverlay();
    }
}
