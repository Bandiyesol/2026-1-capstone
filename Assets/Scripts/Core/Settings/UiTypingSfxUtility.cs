using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>UI 입력창 타이핑 효과음 연결.</summary>
public static class UiTypingSfxUtility
{
	static readonly HashSet<int> WiredInputIds = new HashSet<int>();

	public static void Wire(Component input)
	{
		if (input == null)
			return;

		int id = input.GetInstanceID();
		if (WiredInputIds.Contains(id))
			return;

		if (AuthInputUtility.TryGetTmpInputField(input, out TMP_InputField tmp))
		{
			tmp.onValueChanged.AddListener(OnTyped);
			WiredInputIds.Add(id);
			return;
		}

		if (AuthInputUtility.TryGetLegacyInputField(input, out InputField legacy))
		{
			legacy.onValueChanged.AddListener(OnTyped);
			WiredInputIds.Add(id);
		}
	}

	public static void WireAllInScene()
	{
		TMP_InputField[] tmpFields = Object.FindObjectsByType<TMP_InputField>(
			FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (TMP_InputField field in tmpFields)
			Wire(field);

		InputField[] legacyFields = Object.FindObjectsByType<InputField>(
			FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (InputField field in legacyFields)
			Wire(field);
	}

	static void OnTyped(string _)
	{
		GameAudio.PlayUiTextInput();
	}
}
