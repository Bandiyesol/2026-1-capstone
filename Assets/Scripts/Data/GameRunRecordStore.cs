using System.Collections.Generic;
using UnityEngine;

/// <summary>플레이 기록을 PlayerPrefs에 저장·로드합니다.</summary>
public static class GameRunRecordStore
{
	const string KeyPrefix = "GameRunRecords_v1_";
	const int MaxRecords = 50;

	public static void Save(GameRunRecord record)
	{
		if (record == null || string.IsNullOrEmpty(record.id))
			return;

		GameRunRecordList list = LoadList();
		list.records.RemoveAll(r => r != null && r.id == record.id);
		list.records.Insert(0, record);

		if (list.records.Count > MaxRecords)
			list.records.RemoveRange(MaxRecords, list.records.Count - MaxRecords);

		string json = JsonUtility.ToJson(list);
		PlayerPrefs.SetString(GetKey(), json);
		PlayerPrefs.Save();
	}

	public static IReadOnlyList<GameRunRecord> LoadAll()
	{
		return LoadList().records;
	}

	public static GameRunRecord FindById(string recordId)
	{
		if (string.IsNullOrEmpty(recordId))
			return null;

		foreach (GameRunRecord record in LoadList().records)
		{
			if (record != null && record.id == recordId)
				return record;
		}

		return null;
	}

	static GameRunRecordList LoadList()
	{
		string key = GetKey();
		if (!PlayerPrefs.HasKey(key))
			return new GameRunRecordList();

		string json = PlayerPrefs.GetString(key);
		if (string.IsNullOrEmpty(json))
			return new GameRunRecordList();

		GameRunRecordList list = JsonUtility.FromJson<GameRunRecordList>(json);
		return list ?? new GameRunRecordList();
	}

	static string GetKey()
	{
		string userId = "local";
		if (AuthManager.Instance != null && AuthManager.Instance.CurrentUser != null)
			userId = AuthManager.Instance.CurrentUser.UserId ?? userId;

		return KeyPrefix + userId;
	}
}
