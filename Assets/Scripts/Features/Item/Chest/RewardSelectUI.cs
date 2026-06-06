using System.Collections.Generic;
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

    List<RewardCandidate> currentCandidates;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (panel != null) panel.SetActive(false);
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
            Debug.Log("[RewardSelectUI] GetOrFind — 비활성화 오브젝트에서 instance 등록");
        }
        return instance;
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

        if (panel != null) panel.SetActive(true);

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
        string gradeHex;

        switch (candidate.type)
        {
            case RewardType.Weapon:
                WeaponInstance w = WeaponRewardService.CreateInstance(candidate.weaponId);
                icon      = WeaponRewardService.GetIcon(w);
                title     = WeaponRewardService.FormatTitle(w);
                gradeText = w?.info?.grade ?? "";
                desc      = WeaponRewardService.FormatStats(w);
                gradeHex  = GradeHex(w?.info?.grade);
                break;

            case RewardType.Accessory:
                AccessoryData acc = candidate.accessory;
                icon      = acc?.icon;
                title     = acc?.displayName ?? "";
                gradeText = acc?.grade.ToString() ?? "";
                desc      = acc?.description ?? "";
                gradeHex  = GradeHex(acc?.grade.ToString());
                break;

            case RewardType.Relic:
                RelicData relic = candidate.relic;
                icon      = relic?.icon;
                title     = relic?.relicName ?? "";
                gradeText = "성물";
                desc      = relic?.description ?? "";
                gradeHex  = "FF3333"; // 빨간색
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
            slotTitles[i].text = title;

        // Detail — <color>등급</color>\n설명
        if (slotDetails != null && i < slotDetails.Length && slotDetails[i] != null)
        {
            slotDetails[i].text = $"<color=#{gradeHex}>{gradeText}</color>\n{desc}";
            slotDetails[i].richText = true;
        }
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

    // ───────────────────────────────────────────
    //  등급별 색상 (Hex)
    // ───────────────────────────────────────────

    string GradeHex(string grade) => grade switch
    {
        "Common"    or "일반"   => "FFFFFF", // 흰색
        "Rare"      or "희귀"   => "4D99FF", // 파랑
        "Unique"    or "유니크" => "B24DFF", // 보라
        "Legendary" or "전설"   => "FF9900", // 주황
        _                       => "FFFFFF"
    };
}