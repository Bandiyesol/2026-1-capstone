using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

/// <summary>클리어 기록을 Firestore에 저장·조회합니다. 모든 계정이 공유하는 전역 랭킹용.</summary>
public class ClearLeaderboardRepository
{
	const string CollectionName = "clearLeaderboard";

	FirebaseFirestore database;

	FirebaseFirestore Database => database ??= FirebaseFirestore.DefaultInstance;

	public async Task SubmitClearAsync(GameRunRecord record)
	{
		if (record == null || !record.cleared || string.IsNullOrEmpty(record.id))
			return;

		if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
		{
			Debug.LogWarning("[ClearLeaderboardRepository] 로그인 상태가 아니어서 전역 랭킹에 등록하지 않습니다.");
			return;
		}

		string userId = AuthManager.Instance.CurrentUser?.UserId;
		if (string.IsNullOrEmpty(userId))
			return;

		var entry = new ClearLeaderboardEntry
		{
			UserId = userId,
			RecordId = record.id,
			PlayerNickname = record.playerNickname ?? string.Empty,
			PlayTimeSeconds = record.playTimeSeconds,
			KillCount = record.killCount,
			PlayedAt = record.playedAt ?? string.Empty,
			Cleared = true,
			RecordJson = JsonUtility.ToJson(record),
		};

		DocumentReference doc = Database.Collection(CollectionName).Document(record.id);
		await doc.SetAsync(entry).AwaitOnMainThread();
		Debug.Log($"[ClearLeaderboardRepository] 전역 랭킹 등록: {record.id}, {record.playTimeSeconds:F0}s");
	}

	public async Task<IReadOnlyList<GameRunRecord>> FetchTopClearsAsync(int count)
	{
		if (count <= 0)
			return Array.Empty<GameRunRecord>();

		if (FirestoreRestClient.UseRestInPlayerBuild)
			return await FirestoreRestClient.FetchTopClearsAsync(count);

		Query query = Database.Collection(CollectionName)
			.OrderBy("playTimeSeconds")
			.Limit(count);

		QuerySnapshot snapshot = await query.GetSnapshotAsync().AwaitOnMainThread();
		var records = new List<GameRunRecord>(snapshot.Count);

		foreach (DocumentSnapshot document in snapshot.Documents)
		{
			if (!document.Exists)
				continue;

			GameRunRecord record = DeserializeRecord(document);
			if (record != null && record.cleared)
				records.Add(record);
		}

		records.Sort(GameRunLeaderboard.CompareByClearTime);
		return records;
	}

	public async Task<GameRunRecord> FindByIdAsync(string recordId)
	{
		if (string.IsNullOrEmpty(recordId))
			return null;

		if (FirestoreRestClient.UseRestInPlayerBuild)
			return await FirestoreRestClient.FindClearRecordByIdAsync(recordId);

		DocumentSnapshot snapshot = await Database.Collection(CollectionName).Document(recordId).GetSnapshotAsync().AwaitOnMainThread();
		if (!snapshot.Exists)
			return null;

		return DeserializeRecord(snapshot);
	}

	static GameRunRecord DeserializeRecord(DocumentSnapshot document)
	{
		try
		{
			ClearLeaderboardEntry entry = document.ConvertTo<ClearLeaderboardEntry>();
			if (entry == null)
				return null;

			if (!string.IsNullOrEmpty(entry.RecordJson))
			{
				GameRunRecord fromJson = JsonUtility.FromJson<GameRunRecord>(entry.RecordJson);
				if (fromJson != null)
					return fromJson;
			}

			return new GameRunRecord
			{
				id = string.IsNullOrEmpty(entry.RecordId) ? document.Id : entry.RecordId,
				playerNickname = entry.PlayerNickname,
				playTimeSeconds = (float)entry.PlayTimeSeconds,
				killCount = entry.KillCount,
				playedAt = entry.PlayedAt,
				cleared = entry.Cleared,
				weaponNames = Array.Empty<string>(),
				accessoryNames = Array.Empty<string>(),
				runeNames = Array.Empty<string>(),
				weaponSpriteIds = Array.Empty<string>(),
				stageRecords = GameRunRecord.CreateNew().stageRecords,
			};
		}
		catch (Exception exception)
		{
			Debug.LogWarning($"[ClearLeaderboardRepository] 기록 역직렬화 실패 ({document.Id}): {exception.Message}");
			return null;
		}
	}

	[FirestoreData]
	public class ClearLeaderboardEntry
	{
		[FirestoreProperty("userId")] public string UserId { get; set; }
		[FirestoreProperty("recordId")] public string RecordId { get; set; }
		[FirestoreProperty("playerNickname")] public string PlayerNickname { get; set; }
		[FirestoreProperty("playTimeSeconds")] public double PlayTimeSeconds { get; set; }
		[FirestoreProperty("killCount")] public int KillCount { get; set; }
		[FirestoreProperty("playedAt")] public string PlayedAt { get; set; }
		[FirestoreProperty("cleared")] public bool Cleared { get; set; }
		[FirestoreProperty("recordJson")] public string RecordJson { get; set; }
	}
}
