using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>메인 메뉴 상시 표시 클리어 랭킹(1~10위). 행 클릭 시 플레이 기록 상세.</summary>
public class MainMenuLeaderboardView : MonoBehaviour
{
	[SerializeField] TextMeshProUGUI titleText;
	[SerializeField] TextMeshProUGUI subtitleText;
	[SerializeField] Button[] rankButtons;
	[SerializeField] TextMeshProUGUI[] rankLabels;
	[SerializeField] TMP_FontAsset koreanFont;

	readonly string[] recordIds = new string[GameRunLeaderboard.MaxRankCount];

	Transform menuRoot;

	public void BindMenuRoot(Transform gameStartRoot)
	{
		menuRoot = gameStartRoot;
		SyncVisibilityWithMenu();
	}

	public void Configure(
		TextMeshProUGUI title,
		TextMeshProUGUI subtitle,
		Button[] buttons,
		TextMeshProUGUI[] labels)
	{
		titleText = title;
		subtitleText = subtitle;
		rankButtons = buttons;
		rankLabels = labels;
	}

	void Awake()
	{
		koreanFont = TmpKoreanFontUtility.ResolveNeoDgmFont(koreanFont);
		WireRowButtons();
		ApplyStaticLabels();
	}

	void OnEnable()
	{
		Refresh();
	}

	void SyncVisibilityWithMenu()
	{
		if (menuRoot == null)
			return;

		bool menuVisible = menuRoot.gameObject.activeInHierarchy;
		if (gameObject.activeSelf != menuVisible)
			gameObject.SetActive(menuVisible);
	}

	public void Refresh()
	{
		koreanFont = TmpKoreanFontUtility.ResolveNeoDgmFont(koreanFont);
		WireRowButtons();

		IReadOnlyList<GameRunRecord> top = GameRunLeaderboard.GetTopClears(GameRunLeaderboard.MaxRankCount);

		for (int i = 0; i < GameRunLeaderboard.MaxRankCount; i++)
		{
			int rank = i + 1;
			GameRunRecord record = i < top.Count ? top[i] : null;
			recordIds[i] = record?.id;

			if (rankLabels != null && i < rankLabels.Length && rankLabels[i] != null)
			{
				string line = GameRunLeaderboard.FormatRankLine(rank, record);
				rankLabels[i].text = line;
				TmpKoreanFontUtility.ApplyFont(rankLabels[i], koreanFont);
				TmpKoreanFontUtility.EnsureGlyphs(rankLabels[i], koreanFont, line);
			}

			if (rankButtons != null && i < rankButtons.Length && rankButtons[i] != null)
				rankButtons[i].interactable = record != null;
		}
	}

	void ApplyStaticLabels()
	{
		if (titleText != null)
		{
			titleText.text = "클리어 랭킹";
			TmpKoreanFontUtility.ApplyFont(titleText, koreanFont);
		}

		if (subtitleText != null)
		{
			subtitleText.text = "닉네임 · 최단 플레이타임 · 탭 상세";
			TmpKoreanFontUtility.ApplyFont(subtitleText, koreanFont);
		}
	}

	void WireRowButtons()
	{
		if (rankButtons == null)
			return;

		for (int i = 0; i < rankButtons.Length; i++)
		{
			if (rankButtons[i] == null)
				continue;

			int index = i;
			rankButtons[i].onClick.RemoveAllListeners();
			rankButtons[i].onClick.AddListener(() => OnRankRowClicked(index));
		}
	}

	void OnRankRowClicked(int index)
	{
		if (index < 0 || index >= recordIds.Length)
			return;

		string recordId = recordIds[index];
		if (string.IsNullOrEmpty(recordId))
			return;

		if (GameManager.instance != null)
		{
			GameManager.instance.ShowGameRecordDetail(recordId);
			return;
		}

		GameRecordUI ui = GameRecordUIBootstrap.Ensure();
		if (ui == null)
			return;

		ui.Show(recordId, () =>
		{
			ui.Hide();
			Time.timeScale = 1f;
			Refresh();
		}, singleRunOnly: true);
	}
}
