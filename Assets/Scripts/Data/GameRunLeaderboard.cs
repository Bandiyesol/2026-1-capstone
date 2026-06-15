using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>클리어 기록 중 최단 플레이타임 순 전역 랭킹.</summary>
public static class GameRunLeaderboard
{
	public const int MaxRankCount = 10;

	static readonly List<GameRunRecord> GlobalTopClears = new List<GameRunRecord>();
	static readonly Dictionary<string, GameRunRecord> GlobalRecordsById = new Dictionary<string, GameRunRecord>();
	static bool globalCacheReady;

	public static bool UsesGlobalCache => globalCacheReady;

	public static void ClearGlobalCache()
	{
		GlobalTopClears.Clear();
		GlobalRecordsById.Clear();
		globalCacheReady = false;
	}

	public static async Task RefreshGlobalAsync(int count = MaxRankCount)
	{
		GlobalTopClears.Clear();
		GlobalRecordsById.Clear();
		globalCacheReady = false;

		if (!await FirebaseBootstrap.EnsureInitializedAsync())
		{
			Debug.LogWarning("[GameRunLeaderboard] Firebase 미초기화 — 로컬 기록으로 랭킹을 표시합니다.");
			return;
		}

		if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
		{
			Debug.LogWarning("[GameRunLeaderboard] 로그인 필요 — 로컬 기록으로 랭킹을 표시합니다.");
			return;
		}

		try
		{
			var repository = new ClearLeaderboardRepository();
			IReadOnlyList<GameRunRecord> top = await repository.FetchTopClearsAsync(count);
			ApplyGlobalRecords(top);
			globalCacheReady = true;
			Debug.Log($"[GameRunLeaderboard] 전역 랭킹 {GlobalTopClears.Count}건 갱신");
		}
		catch (Exception exception)
		{
			Debug.LogWarning($"[GameRunLeaderboard] 전역 랭킹 로드 실패: {exception.Message}");
		}
	}

	public static async Task SubmitClearAsync(GameRunRecord record)
	{
		if (record == null || !record.cleared)
			return;

		if (!await FirebaseBootstrap.EnsureInitializedAsync())
			return;

		if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
			return;

		try
		{
			var repository = new ClearLeaderboardRepository();
			await repository.SubmitClearAsync(record);
		}
		catch (Exception exception)
		{
			Debug.LogWarning($"[GameRunLeaderboard] 전역 랭킹 등록 실패: {exception.Message}");
		}
	}

	static void ApplyGlobalRecords(IReadOnlyList<GameRunRecord> records)
	{
		GlobalTopClears.Clear();
		GlobalRecordsById.Clear();

		if (records == null)
			return;

		foreach (GameRunRecord record in records)
		{
			if (record == null || !record.cleared)
				continue;

			GlobalTopClears.Add(record);
			if (!string.IsNullOrEmpty(record.id))
				GlobalRecordsById[record.id] = record;
		}
	}

	public static IReadOnlyList<GameRunRecord> GetTopClears(int count = MaxRankCount)
	{
		if (globalCacheReady && GlobalTopClears.Count > 0)
		{
			if (count <= 0 || GlobalTopClears.Count <= count)
				return GlobalTopClears;

			return GlobalTopClears.GetRange(0, count);
		}

		return GetLocalTopClears(count);
	}

	static IReadOnlyList<GameRunRecord> GetLocalTopClears(int count)
	{
		var cleared = new List<GameRunRecord>();

		foreach (GameRunRecord record in GameRunRecordStore.LoadAll())
		{
			if (record != null && record.cleared)
				cleared.Add(record);
		}

		cleared.Sort(CompareByClearTime);

		if (count <= 0 || cleared.Count <= count)
			return cleared;

		return cleared.GetRange(0, count);
	}

	public static GameRunRecord FindRecordById(string recordId)
	{
		if (string.IsNullOrEmpty(recordId))
			return null;

		if (GlobalRecordsById.TryGetValue(recordId, out GameRunRecord global))
			return global;

		return GameRunRecordStore.FindById(recordId);
	}

	public static async Task<GameRunRecord> FindRecordByIdAsync(string recordId)
	{
		GameRunRecord cached = FindRecordById(recordId);
		if (cached != null)
			return cached;

		if (!await FirebaseBootstrap.EnsureInitializedAsync())
			return null;

		if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
			return null;

		try
		{
			var repository = new ClearLeaderboardRepository();
			GameRunRecord remote = await repository.FindByIdAsync(recordId);
			if (remote != null && !string.IsNullOrEmpty(remote.id))
				GlobalRecordsById[remote.id] = remote;
			return remote;
		}
		catch (Exception exception)
		{
			Debug.LogWarning($"[GameRunLeaderboard] 기록 조회 실패: {exception.Message}");
			return null;
		}
	}

	public static int CompareByClearTime(GameRunRecord a, GameRunRecord b)
	{
		if (a == null && b == null)
			return 0;
		if (a == null)
			return 1;
		if (b == null)
			return -1;

		int byTime = a.playTimeSeconds.CompareTo(b.playTimeSeconds);
		if (byTime != 0)
			return byTime;

		int byKills = b.killCount.CompareTo(a.killCount);
		if (byKills != 0)
			return byKills;

		return string.Compare(a.playedAt, b.playedAt, StringComparison.Ordinal);
	}

	public static string FormatRankLine(int rank, GameRunRecord record)
	{
		if (record == null)
			return $"{rank}.  —";

		string name = UserAccountDisplay.ResolveRecordDisplayName(record);
		return $"{rank}.  {name}  {FormatPlayTime(record.playTimeSeconds)}";
	}

	public static string FormatPlayTime(float seconds)
	{
		int total = Math.Max(0, (int)Math.Floor(seconds));
		int minutes = total / 60;
		int remain = total % 60;
		return $"{minutes:D2}:{remain:D2}";
	}
}
