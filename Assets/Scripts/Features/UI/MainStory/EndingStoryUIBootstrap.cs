using UnityEngine;

/// <summary>
/// EndingStoryPanel이 없으면 MainStoryPanel을 복제해 생성합니다.
/// </summary>
public static class EndingStoryUIBootstrap
{
	public static EndingStoryUI EnsureEndingStoryUI()
	{
		EndingStoryUI existing = Object.FindFirstObjectByType<EndingStoryUI>(FindObjectsInactive.Include);
		if (existing != null)
			return existing;

		GameObject endingPanel = GameObject.Find("EndingStoryPanel");
		if (endingPanel == null)
		{
			GameObject mainPanel = GameObject.Find("MainStoryPanel");
			if (mainPanel == null)
			{
				Debug.LogWarning("[EndingStoryUIBootstrap] MainStoryPanel / EndingStoryPanel을 찾지 못했습니다.");
				return null;
			}

			endingPanel = Object.Instantiate(mainPanel, mainPanel.transform.parent);
			endingPanel.name = "EndingStoryPanel";
			endingPanel.SetActive(false);

			MainStoryUI mainStoryOnClone = endingPanel.GetComponent<MainStoryUI>();
			if (mainStoryOnClone != null)
				Object.Destroy(mainStoryOnClone);
		}

		Transform host = endingPanel.transform.parent != null
			? endingPanel.transform.parent
			: endingPanel.transform;

		MainStoryUI mainStoryHost = host.GetComponent<MainStoryUI>();
		if (mainStoryHost != null)
			host = mainStoryHost.transform;

		EndingStoryUI created = host.GetComponent<EndingStoryUI>();
		if (created == null)
			created = host.gameObject.AddComponent<EndingStoryUI>();

		Debug.Log("[EndingStoryUIBootstrap] EndingStoryPanel + EndingStoryUI를 준비했습니다.");
		return created;
	}
}
