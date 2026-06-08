using UnityEngine;

/// <summary>씬에 EndingStoryPanel + EndingStoryUI가 있어야 합니다.</summary>
public static class EndingStoryUIBootstrap
{
	public static EndingStoryUI EnsureEndingStoryUI()
	{
		EndingStoryUI existing = Object.FindFirstObjectByType<EndingStoryUI>(FindObjectsInactive.Include);
		if (existing != null)
			return existing;

		if (GameObject.Find("EndingStoryPanel") == null)
		{
			Debug.LogWarning("[EndingStoryUIBootstrap] EndingStoryPanel이 없습니다.");
			return null;
		}

		Debug.LogWarning("[EndingStoryUIBootstrap] EndingStoryUI 컴포넌트를 씬에 추가하세요.");
		return null;
	}
}
