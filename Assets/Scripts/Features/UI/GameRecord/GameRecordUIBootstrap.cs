using UnityEngine;

/// <summary>씬에 GameRecordPanel + GameRecordUI가 있어야 합니다.</summary>
public static class GameRecordUIBootstrap
{
	public static GameRecordUI Ensure()
	{
		GameRecordUI existing = Object.FindFirstObjectByType<GameRecordUI>(FindObjectsInactive.Include);
		if (existing != null)
			return existing;

		GameObject panel = GameObject.Find("GameRecordPanel");
		if (panel == null)
		{
			Debug.LogError("[GameRecordUIBootstrap] GameRecordPanel이 없습니다. Canvas 아래에 배치하세요.");
			return null;
		}

		GameRecordUI ui = panel.GetComponent<GameRecordUI>();
		if (ui == null)
		{
			Debug.LogError("[GameRecordUIBootstrap] GameRecordPanel에 GameRecordUI가 없습니다.");
			return null;
		}

		return ui;
	}
}
