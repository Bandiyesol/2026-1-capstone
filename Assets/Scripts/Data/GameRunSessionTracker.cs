using System.Collections.Generic;
using UnityEngine;

/// <summary>현재 플레이 판의 스테이지별 통계를 누적합니다.</summary>
public static class GameRunSessionTracker
{
	public const int MaxStages = 7;

	static bool active;
	static string characterId;
	static GameRunStageRecord[] stages;
	static int trackedStageIndex;
	static readonly int[] stageStartKills = new int[MaxStages];
	static readonly int[] stageStartCoinsEarned = new int[MaxStages];
	static readonly float[] stageStartTimes = new float[MaxStages];
	static int totalCoinsEarned;
	static int totalCoinsSpent;

	public static bool IsActive => active;
	public static string CharacterId => characterId;
	public static int TotalCoinsEarned => totalCoinsEarned;

	public static void AddCoinsEarned(int amount)
	{
		if (amount <= 0)
			return;

		totalCoinsEarned += amount;
	}

	public static void AddCoinsSpent(int amount)
	{
		if (amount <= 0)
			return;

		totalCoinsSpent += amount;
	}

	public static void BeginRun()
	{
		active = true;
		totalCoinsEarned = 0;
		totalCoinsSpent = 0;
		characterId = GameCharacterCatalog.ResolveCharacterId(GameManager.instance?.player);
		stages = new GameRunStageRecord[MaxStages];

		for (int i = 0; i < MaxStages; i++)
		{
			stages[i] = GameRunStageRecord.Empty(i + 1);
			stageStartKills[i] = 0;
			stageStartCoinsEarned[i] = 0;
			stageStartTimes[i] = 0f;
		}

		int startIndex = ResolveCurrentStageIndex();
		trackedStageIndex = startIndex;
		MarkStageStart(startIndex);
	}

	/// <summary>일시정지 해제(상점·인벤 등) 시 — 이미 진행 중인 런의 스테이지 기록은 유지합니다.</summary>
	public static void ResumeRun()
	{
		if (!active || stages == null)
			return;

		trackedStageIndex = ResolveCurrentStageIndex();
	}

	public static void Reset()
	{
		active = false;
		stages = null;
		totalCoinsEarned = 0;
		totalCoinsSpent = 0;
		characterId = GameCharacterCatalog.DefaultCharacterId;
	}

	public static GameRunStageRecord[] GetStageSnapshots()
	{
		if (stages == null)
		{
			var empty = new GameRunStageRecord[MaxStages];
			for (int i = 0; i < MaxStages; i++)
				empty[i] = GameRunStageRecord.Empty(i + 1);
			return empty;
		}

		var copy = new GameRunStageRecord[MaxStages];
		for (int i = 0; i < MaxStages; i++)
			copy[i] = stages[i] ?? GameRunStageRecord.Empty(i + 1);

		return copy;
	}

	public static void MarkStageStart(int stageIndex)
	{
		if (!active)
			return;

		int index = Mathf.Clamp(stageIndex, 0, MaxStages - 1);

		trackedStageIndex = index;
		CaptureStageStartMarkers(index);
	}

	static void CaptureStageStartMarkers(int index)
	{
		GameManager game = GameManager.instance;
		stageStartKills[index] = game != null ? game.Kill : 0;
		stageStartCoinsEarned[index] = totalCoinsEarned;
		stageStartTimes[index] = game != null ? game.gameTime : 0f;
	}

	public static void CommitStage(int stageIndex, bool stageCleared)
	{
		trackedStageIndex = Mathf.Clamp(stageIndex, 0, MaxStages - 1);
		CommitCurrentStage(stageCleared);
	}

	public static void CommitCurrentStage(bool stageCleared)
	{
		if (!active || stages == null)
			return;

		int index = Mathf.Clamp(trackedStageIndex, 0, MaxStages - 1);
		GameManager game = GameManager.instance;

		int kills = game != null ? Mathf.Max(0, game.Kill - stageStartKills[index]) : 0;
		int coins = Mathf.Max(0, totalCoinsEarned - stageStartCoinsEarned[index]);
		float playTime = game != null ? Mathf.Max(0f, game.gameTime - stageStartTimes[index]) : 0f;

		stages[index] = new GameRunStageRecord
		{
			stageNumber = index + 1,
			reached = true,
			cleared = stageCleared,
			killCount = kills,
			coinCount = coins,
			playTimeSeconds = playTime,
			bossName = BossBriefingRuntime.GetBossDisplayName(index),
			runeNames = CaptureCurrentRuneNames(),
		};
	}

	static string[] CaptureCurrentRuneNames()
	{
		if (RuneManager.instance == null)
			return System.Array.Empty<string>();

		var names = new List<string>();
		for (int i = 0; i < RuneManager.instance.SlotCount_; i++)
		{
			RuneData rune = RuneManager.instance.GetSlot(i);
			if (rune == null)
				continue;

			names.Add(string.IsNullOrEmpty(rune.runeName) ? rune.name : rune.runeName);
		}

		return names.ToArray();
	}

	public static void OnStageAdvanced(int newStageIndex)
	{
		MarkStageStart(newStageIndex);
	}

	public static void FinalizeRun(bool runCleared)
	{
		if (!active || stages == null)
			return;

		int deathOrClearIndex = ResolveCurrentStageIndex();

		if (!runCleared)
		{
			for (int i = 0; i < deathOrClearIndex; i++)
				EnsurePriorStageMarkedCleared(i);

			if (!IsStageCommitted(deathOrClearIndex))
				CommitStage(deathOrClearIndex, stageCleared: false);
		}
		else
		{
			for (int i = 0; i < deathOrClearIndex; i++)
				EnsurePriorStageMarkedCleared(i);

			if (!IsStageCommitted(deathOrClearIndex))
				CommitStage(deathOrClearIndex, stageCleared: true);
		}

		ReconcileTotalCoinsEarned();
		active = false;
	}

	/// <summary>스테이지별 획득 골드(가치 합)를 모두 더한 총 누적 골드.</summary>
	public static int GetTotalGoldEarnedForRecord()
	{
		int fromStages = SumStageGoldEarned();
		int tracked = Mathf.Max(fromStages, totalCoinsEarned);

		GameManager game = GameManager.instance;
		if (game == null)
			return tracked;

		int walletPlusSpent = Mathf.Max(0, game.Coin) + totalCoinsSpent;
		return Mathf.Max(tracked, walletPlusSpent);
	}

	public static int SumStageGoldEarned()
	{
		if (stages == null)
			return totalCoinsEarned;

		int sum = 0;
		for (int i = 0; i < MaxStages; i++)
		{
			GameRunStageRecord record = stages[i];
			if (record != null && record.reached)
				sum += Mathf.Max(0, record.coinCount);
		}

		return sum;
	}

	static void ReconcileTotalCoinsEarned()
	{
		int fromStages = SumStageGoldEarned();
		if (fromStages > totalCoinsEarned)
			totalCoinsEarned = fromStages;
	}

	static int ResolveCurrentStageIndex()
	{
		StageManager stageManager = Object.FindFirstObjectByType<StageManager>(FindObjectsInactive.Include);
		if (stageManager != null)
			return Mathf.Clamp(stageManager.stageIndex, 0, MaxStages - 1);

		return Mathf.Clamp(trackedStageIndex, 0, MaxStages - 1);
	}

	static bool IsStageCommitted(int index)
	{
		if (stages == null || index < 0 || index >= MaxStages)
			return false;

		GameRunStageRecord record = stages[index];
		return record != null && record.reached && HasMeaningfulStageStats(record);
	}

	static void EnsurePriorStageMarkedCleared(int index)
	{
		if (!IsStageCommitted(index))
			return;

		GameRunStageRecord record = stages[index];
		if (record == null || record.cleared)
			return;

		record.cleared = true;
		stages[index] = record;
	}

	static bool HasMeaningfulStageStats(GameRunStageRecord record)
	{
		if (record == null)
			return false;

		return record.killCount > 0
			|| record.coinCount > 0
			|| record.playTimeSeconds > 0.05f;
	}
}
