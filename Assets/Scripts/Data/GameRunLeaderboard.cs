using System;
using System.Collections.Generic;

/// <summary>클리어 기록 중 최단 플레이타임 순 랭킹.</summary>
public static class GameRunLeaderboard
{
	public const int MaxRankCount = 10;

	public static IReadOnlyList<GameRunRecord> GetTopClears(int count = MaxRankCount)
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
