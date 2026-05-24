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
	[SerializeField] TextMeshProUGUI[] choiceLabels;
	[SerializeField] WeaponInventory inventory;

	readonly List<WeaponInstance> currentCandidates = new List<WeaponInstance>();


	void Awake()
	{
		if (inventory == null)
			inventory = FindFirstObjectByType<WeaponInventory>();
	}

	public void Show()
	{
		gameObject.SetActive(true);
		RollCandidates();
		GameManager.instance.Stop();
	}

	void RollCandidates()
	{
		currentCandidates.Clear();
		currentCandidates.AddRange(WeaponRewardService.RollCandidates(weaponIdPool, 3));

		for (int i = 0; i < choiceButtons.Length; i++)
		{
			bool hasChoice = i < currentCandidates.Count;
			choiceButtons[i].gameObject.SetActive(hasChoice);
			choiceButtons[i].interactable = hasChoice;

			if (choiceLabels != null && i < choiceLabels.Length && hasChoice)
				choiceLabels[i].text = WeaponRewardService.FormatPreview(currentCandidates[i]);
		}
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

		if (GameManager.instance.uiRuneSelect != null)
			GameManager.instance.uiRuneSelect.Show();
		else
			GameManager.instance.Resume();
	}
}
