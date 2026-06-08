using System;
using System.Collections.Generic;

/// <summary>한 판 플레이 요약 — PlayerPrefs에 JSON으로 저장합니다.</summary>
[Serializable]
public class GameRunRecord
{
	public string id;
	public string playedAt;
	public float playTimeSeconds;
	public bool cleared;
	public int killCount;
	public int coinCount;
	public int stageReached;
	public string bossName;
	public string characterLabel;
	public string characterId;
	public string playerNickname;
	public GameRunStageRecord[] stageRecords;
	public string[] weaponNames;
	public string[] accessoryNames;
	public string[] runeNames;
	public string[] weaponSpriteIds;

	public static GameRunRecord CreateNew()
	{
		return new GameRunRecord
		{
			id = Guid.NewGuid().ToString("N"),
			playedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
			weaponNames = Array.Empty<string>(),
			accessoryNames = Array.Empty<string>(),
			runeNames = Array.Empty<string>(),
			weaponSpriteIds = Array.Empty<string>(),
			characterId = GameCharacterCatalog.DefaultCharacterId,
			stageRecords = CreateEmptyStageRecords(),
		};
	}

	static GameRunStageRecord[] CreateEmptyStageRecords()
	{
		var records = new GameRunStageRecord[GameRunSessionTracker.MaxStages];
		for (int i = 0; i < records.Length; i++)
			records[i] = GameRunStageRecord.Empty(i + 1);
		return records;
	}
}

[Serializable]
public class GameRunRecordList
{
	public List<GameRunRecord> records = new List<GameRunRecord>();
}
