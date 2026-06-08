using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// [임시 UI] 보상 선택창 — 무기·악세사리·성물 후보 3개 표시.
/// WeaponSelectUI와 동일한 구조 (Title + Detail).
/// 태경이(이태경)가 정식 UI로 교체 예정.
/// Title  → 이름만
/// Detail → 등급(색상) + \n + 설명/스탯
/// </summary>
public class RewardSelectUI : MonoBehaviour
{
    public static RewardSelectUI instance;

    [Header("[ 패널 ]")]
    public GameObject panel;

    [Header("[ 슬롯 3개 — WeaponSelectUI와 동일 구조 ]")]
    public Button[]          slotButtons;
    public Image[]           slotIcons;
    public TextMeshProUGUI[] slotTitles;   // 이름만
    public TextMeshProUGUI[] slotDetails;  // 등급(색상) + \n + 설명/스탯

    [Header("[ 폰트 ]")]
    [SerializeField] TMP_FontAsset koreanFont;

    List<RewardCandidate> currentCandidates;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (panel != null) panel.SetActive(false);
        EnsureReady();
        ChoiceSelectUILayout.Apply(transform);
        Debug.Log("[RewardSelectUI] Awake — instance 등록 완료");
    }

    void OnEnable()
    {
        if (instance == null) instance = this;
    }

    public static RewardSelectUI GetOrFind()
    {
        if (instance != null) return instance;
        RewardSelectUI found = FindAnyObjectByType<RewardSelectUI>(FindObjectsInactive.Include);
        if (found != null)
        {
            instance = found;
            found.EnsureReady();
            Debug.Log("[RewardSelectUI] GetOrFind — 비활성화 오브젝트에서 instance 등록");
        }
        return instance;
    }

    /// <summary>WeaponSelectUI 복제본 등에서 Btn0~2 슬롯을 자동 연결합니다.</summary>
    public void EnsureReady()
    {
        if (panel == null)
            panel = gameObject;

        if (slotButtons == null || slotButtons.Length == 0)
            slotButtons = FindChoiceButtons();

        if (slotButtons == null || slotButtons.Length == 0)
            return;

        slotTitles = BindTmpArray(slotTitles, "Title");
        slotDetails = BindTmpArray(slotDetails, "Detail");
        slotIcons = BindImageArray(slotIcons, "Icon");

        ResolveKoreanFont();
        ApplyKoreanFontToSlots();
        TmpKoreanFontUtility.EnsureAllAccessoryGlyphs(koreanFont);
    }

    Button[] FindChoiceButtons()
    {
        var buttons = new List<Button>();
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button == null)
                continue;

            string name = button.gameObject.name;
            if (name == "Btn0" || name == "Btn1" || name == "Btn2")
                buttons.Add(button);
        }

        buttons.Sort((a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));
        return buttons.ToArray();
    }

    TextMeshProUGUI[] BindTmpArray(TextMeshProUGUI[] current, string childName)
    {
        if (slotButtons == null || slotButtons.Length == 0)
            return current;

        var result = new TextMeshProUGUI[slotButtons.Length];
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (current != null && i < current.Length && current[i] != null)
            {
                result[i] = current[i];
                continue;
            }

            Transform child = slotButtons[i].transform.Find(childName);
            if (child != null)
                result[i] = child.GetComponent<TextMeshProUGUI>();
        }

        return result;
    }

    Image[] BindImageArray(Image[] current, string childName)
    {
        if (slotButtons == null || slotButtons.Length == 0)
            return current;

        var result = new Image[slotButtons.Length];
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (current != null && i < current.Length && current[i] != null)
            {
                result[i] = current[i];
                continue;
            }

            Transform child = slotButtons[i].transform.Find(childName);
            if (child != null)
                result[i] = child.GetComponent<Image>();
        }

        return result;
    }

    // ───────────────────────────────────────────
    //  외부 진입점 — DroppedChest에서 호출
    // ───────────────────────────────────────────

    public void Show(List<RewardCandidate> candidates)
    {
        currentCandidates = candidates;
        Time.timeScale = 0f;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        ChoiceSelectUILayout.Apply(transform);

        if (panel != null) panel.SetActive(true);

        ResolveKoreanFont();
        ApplyKoreanFontToSlots();
        EnsureCandidateGlyphs(candidates);

        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (i >= candidates.Count)
            {
                slotButtons[i].gameObject.SetActive(false);
                continue;
            }

            slotButtons[i].gameObject.SetActive(true);
            SetSlot(i, candidates[i]);

            int index = i;
            slotButtons[i].onClick.RemoveAllListeners();
            slotButtons[i].onClick.AddListener(() => OnSelect(index));
        }
    }

    // ───────────────────────────────────────────
    //  슬롯 세팅
    //  Title  → 이름만
    //  Detail → <color=#RRGGBB>등급</color>\n설명/스탯
    // ───────────────────────────────────────────

    void SetSlot(int i, RewardCandidate candidate)
    {
        string title, gradeText, desc;
        Sprite icon;
        string gradeColored;

        switch (candidate.type)
        {
            case RewardType.Weapon:
                WeaponInstance w = WeaponRewardService.CreateInstance(candidate.weaponId);
                icon      = WeaponRewardService.GetIcon(w);
                title     = WeaponRewardService.FormatTitle(w);
                gradeText = w?.info?.grade ?? "";
                desc      = WeaponRewardService.FormatStats(w);
                gradeColored = ChoiceGradeDisplay.FormatColored(gradeText);
                break;

            case RewardType.Accessory:
                AccessoryData acc = candidate.accessory;
                icon      = AccessoryIconResolver.Resolve(acc);
                title     = acc?.displayName ?? "";
                gradeText = acc?.grade.ToString() ?? "";
                desc      = acc?.description ?? "";
                gradeColored = ChoiceGradeDisplay.FormatColored(gradeText);
                break;

            case RewardType.Relic:
                RelicData relic = candidate.relic;
                icon      = relic?.icon;
                title     = relic?.relicName ?? "";
                gradeText = "성물";
                desc      = relic?.description ?? "";
                gradeColored = ChoiceGradeDisplay.FormatColored(gradeText, "FF3333");
                break;

            default:
                return;
        }

        // 아이콘
        if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
        {
            slotIcons[i].sprite  = icon;
            slotIcons[i].enabled = icon != null;
        }

        // Title — 이름만
        if (slotTitles != null && i < slotTitles.Length && slotTitles[i] != null)
        {
            slotTitles[i].text = title;
            TmpKoreanFontUtility.EnsureGlyphs(slotTitles[i], koreanFont, title);
        }

        // Detail — <color>등급</color>\n설명
        if (slotDetails != null && i < slotDetails.Length && slotDetails[i] != null)
        {
            string detail = string.IsNullOrEmpty(desc)
                ? gradeColored
                : $"{gradeColored}\n{desc}";
            slotDetails[i].text = detail;
            slotDetails[i].richText = true;
            TmpKoreanFontUtility.EnsureGlyphs(slotDetails[i], koreanFont, title + desc + gradeText);
        }
    }

    void ResolveKoreanFont()
    {
        koreanFont = TmpKoreanFontUtility.ResolveNeoDgmFont(koreanFont);
    }

    void ApplyKoreanFontToSlots()
    {
        if (koreanFont == null)
            return;

        if (slotTitles != null)
        {
            foreach (TextMeshProUGUI label in slotTitles)
                TmpKoreanFontUtility.ApplyFont(label, koreanFont);
        }

        if (slotDetails != null)
        {
            foreach (TextMeshProUGUI label in slotDetails)
                TmpKoreanFontUtility.ApplyFont(label, koreanFont);
        }
    }

    void EnsureCandidateGlyphs(List<RewardCandidate> candidates)
    {
        if (koreanFont == null || candidates == null)
            return;

        var sb = new System.Text.StringBuilder(512);
        foreach (RewardCandidate candidate in candidates)
        {
            if (candidate == null)
                continue;

            switch (candidate.type)
            {
                case RewardType.Accessory when candidate.accessory != null:
                    TmpKoreanFontUtility.AppendAccessoryText(sb, candidate.accessory);
                    break;
                case RewardType.Relic when candidate.relic != null:
                    if (!string.IsNullOrEmpty(candidate.relic.relicName))
                        sb.Append(candidate.relic.relicName);
                    if (!string.IsNullOrEmpty(candidate.relic.description))
                        sb.Append(candidate.relic.description);
                    break;
            }
        }

        TmpKoreanFontUtility.EnsureGlyphsInFont(koreanFont, sb.ToString(), "RewardSelectUI");
    }

    // ───────────────────────────────────────────
    //  선택 처리
    // ───────────────────────────────────────────

    void OnSelect(int index)
    {
        if (index >= currentCandidates.Count) return;
        RewardCandidate selected = currentCandidates[index];

        switch (selected.type)
        {
            case RewardType.Weapon:
                WeaponInstance w = WeaponRewardService.CreateInstance(selected.weaponId);
                if (w != null)
                {
                    WeaponInventory inv = FindFirstObjectByType<WeaponInventory>();
                    inv?.TryAdd(w);
                }
                break;

            case RewardType.Accessory:
                AccessoryManager.instance?.Add(selected.accessory);
                break;

            case RewardType.Relic:
                Debug.Log($"[RewardSelectUI] 성물 획득: {selected.relic?.relicName} (미구현)");
                break;
        }

        Hide();
    }

    void Hide()
    {
        Time.timeScale = 1f;
        GameManager.instance.Resume();
        if (panel != null) panel.SetActive(false);
        // 오브젝트 자체 비활성화 — 다른 UI 클릭 방해 방지
        gameObject.SetActive(false);
    }
}