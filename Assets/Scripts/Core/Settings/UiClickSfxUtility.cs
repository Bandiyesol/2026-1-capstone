using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>UI 버튼 클릭 효과음(클릭.wav) 연결.</summary>
public static class UiClickSfxUtility
{
	static readonly HashSet<int> WiredButtonIds = new HashSet<int>();

	public static void Wire(Button button)
	{
		if (button == null)
			return;

		int id = button.GetInstanceID();
		if (WiredButtonIds.Contains(id))
			return;

		WiredButtonIds.Add(id);
		button.onClick.AddListener(GameAudio.PlayUiClick);
	}

	public static void Rewire(Button button, UnityAction action)
	{
		if (button == null)
			return;

		button.onClick.RemoveAllListeners();
		button.onClick.AddListener(GameAudio.PlayUiClick);
		if (action != null)
			button.onClick.AddListener(action);

		WiredButtonIds.Add(button.GetInstanceID());
	}

	public static void WireAllInScene()
	{
		Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (Button button in buttons)
			Wire(button);
	}
}
