using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 시작 시 무기 후보 3개 중 1개 선택. 인벤 UI 없이도 동작하는 최소 선택창.
/// </summary>
public class WeaponSelectUI : MonoBehaviour
{
	[Header("# 후보 풀 (WeaponInfo id)")]
	[SerializeField] string[] weaponIdPool = { "SWORD_001", "BOW_001", "ORB_001" };

	[Header("# UI")]
	[SerializeField] Button[] choiceButtons;
	[SerializeField] TextMeshProUGUI titleLabel;
	[SerializeField] WeaponInventory inventory;

	[Header("# 선택 버튼별 (순서 = Btn 0,1,2)")]
	[SerializeField] TextMeshProUGUI[] choiceTitleLabels;
	[SerializeField] TextMeshProUGUI[] choiceDetailLabels;
	[SerializeField] Image[] choiceIcons;

	[Header("# 레거시 — 비우면 choiceTitle/Detail 사용")]
	[SerializeField] TextMeshProUGUI[] choiceLabels;

	readonly List<WeaponInstance> currentCandidates = new List<WeaponInstance>();
	bool openedFromChest;


	void Awake()
	{
		if (inventory == null)
			inventory = FindFirstObjectByType<WeaponInventory>();

		BindChoiceUiFromButtons();
	}

	void BindChoiceUiFromButtons()
	{
		if (choiceButtons == null || choiceButtons.Length == 0)
			return;

		choiceTitleLabels = BindTmpArray(choiceTitleLabels, "Title");
		choiceDetailLabels = BindTmpArray(choiceDetailLabels, "Detail");
		choiceIcons = BindImageArray(choiceIcons, "Icon");
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
		rect.pivot = new Vector2(0.5f, 1f);
		rect.anchoredPosition = new Vector2(0f, -8f);
		rect.sizeDelta = new Vector2(64f, 64f);

		var image = go.GetComponent<Image>();
		image.raycastTarget = false;
		image.preserveAspect = true;
		return go.transform;
	}

	public void Show()
	{
		openedFromChest = false;
		gameObject.SetActive(true);
		RollCandidates(weaponIdPool);

		if (titleLabel != null)
			titleLabel.text = "시작 무기 선택";

		GameManager.instance.Stop();
	}

	/// <summary>상자 보상용 — 등급에 맞는 무기 풀에서 후보를 뽑습니다.</summary>
	public void ShowFromChest(ChestGrade grade, string[] weaponPoolOverride = null)
	{
		openedFromChest = true;
		gameObject.SetActive(true);
		RollCandidates(weaponPoolOverride ?? weaponIdPool);

		if (titleLabel != null)
			titleLabel.text = $"{ChestDropSettings.GetGradeLabel(grade)} 상자 보상";

		GameManager.instance.Stop();
	}

	void RollCandidates(IReadOnlyList<string> pool)
	{
		currentCandidates.Clear();
		currentCandidates.AddRange(WeaponRewardService.RollCandidates(pool, 3));

		for (int i = 0; i < choiceButtons.Length; i++)
		{
			bool hasChoice = i < currentCandidates.Count;
			choiceButtons[i].gameObject.SetActive(hasChoice);
			choiceButtons[i].interactable = hasChoice;

			if (!hasChoice)
				continue;

			WeaponInstance weapon = currentCandidates[i];
			ApplyChoicePreview(i, weapon);
		}
	}

	void ApplyChoicePreview(int index, WeaponInstance weapon)
	{
		if (choiceTitleLabels != null && index < choiceTitleLabels.Length && choiceTitleLabels[index] != null)
			choiceTitleLabels[index].text = WeaponRewardService.FormatTitle(weapon);

		if (choiceDetailLabels != null && index < choiceDetailLabels.Length && choiceDetailLabels[index] != null)
			choiceDetailLabels[index].text = WeaponRewardService.FormatStats(weapon);

		if (choiceLabels != null && index < choiceLabels.Length && choiceLabels[index] != null
		    && (choiceTitleLabels == null || choiceTitleLabels.Length == 0)
		    && (choiceDetailLabels == null || choiceDetailLabels.Length == 0))
			choiceLabels[index].text = WeaponRewardService.FormatPreview(weapon);

		if (choiceIcons == null || index >= choiceIcons.Length || choiceIcons[index] == null)
			return;

		Image image = choiceIcons[index];
		Sprite icon = WeaponRewardService.GetIcon(weapon);
		image.sprite = icon;
		image.enabled = icon != null;
		image.gameObject.SetActive(icon != null);

		if (icon == null)
			Debug.LogWarning($"[WeaponSelectUI] 아이콘 없음: {weapon?.info?.spriteId} (버튼 {index})");
	}

	public void OnPickWeapon(int index)
	{
		if (index < 0 || index >= currentCandidates.Count) return;

		if (inventory != null)
		{
			if (!inventory.TryAdd(currentCandidates[index]))
				Debug.LogWarning("[WeaponSelectUI] 인벤토리에 무기를 넣지 못했습니다.");
		}
		else
			Debug.LogError("[WeaponSelectUI] WeaponInventory가 없습니다.");

		currentCandidates.Clear();
		HideAndContinue();
	}

	void HideAndContinue()
	{
		gameObject.SetActive(false);

		if (openedFromChest)
		{
			openedFromChest = false;
			GameManager.instance.ResumeGameplayFromOverlay();
			return;
		}

		if (GameManager.instance.uiRuneSelect != null)
			GameManager.instance.uiRuneSelect.Show();
		else
			GameManager.instance.Resume();
	}
}
