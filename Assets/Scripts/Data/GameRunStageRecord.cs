using System;

/// <summary>한 스테이지(1~7) 플레이 요약.</summary>
[Serializable]
public class GameRunStageRecord
{
	public int stageNumber;
	public bool reached;
	public bool cleared;
	public int killCount;
	public int coinCount;
	public float playTimeSeconds;
	public string bossName;
	public string[] runeNames;

	public static GameRunStageRecord Empty(int stageNumber)
	{
		return new GameRunStageRecord
		{
			stageNumber = stageNumber,
			reached = false,
			cleared = false,
			bossName = "—",
			runeNames = System.Array.Empty<string>(),
		};
	}
}
