using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class AuthInputUtility
{
	public static string GetText(Component input)
	{
		if (TryGetTmpInputField(input, out TMP_InputField tmp))
			return tmp.text;

		if (TryGetLegacyInputField(input, out InputField legacy))
			return legacy.text;

		return string.Empty;
	}

	public static void SetInteractable(Component input, bool interactable)
	{
		if (TryGetTmpInputField(input, out TMP_InputField tmp))
			tmp.interactable = interactable;
		else if (TryGetLegacyInputField(input, out InputField legacy))
			legacy.interactable = interactable;
	}

	public static void Clear(Component input)
	{
		if (TryGetTmpInputField(input, out TMP_InputField tmp))
			tmp.text = string.Empty;
		else if (TryGetLegacyInputField(input, out InputField legacy))
			legacy.text = string.Empty;
	}

	public static void ClearAll(params Component[] inputs)
	{
		if (inputs == null)
			return;

		foreach (Component input in inputs)
			Clear(input);
	}

	public static Selectable GetSelectable(Component input)
	{
		if (input == null)
			return null;

		if (input is Selectable selectable)
			return selectable;

		if (TryGetTmpInputField(input, out TMP_InputField tmp))
			return tmp;

		if (TryGetLegacyInputField(input, out InputField legacy))
			return legacy;

		return input.GetComponentInChildren<Selectable>(true);
	}

	public static void Focus(Component input)
	{
		Selectable selectable = GetSelectable(input);
		if (selectable == null)
			return;

		EventSystem eventSystem = EventSystem.current;
		if (eventSystem != null)
			eventSystem.SetSelectedGameObject(selectable.gameObject);

		if (TryGetTmpInputField(input, out TMP_InputField tmp))
		{
			tmp.ActivateInputField();
			tmp.caretPosition = tmp.text?.Length ?? 0;
			return;
		}

		if (TryGetLegacyInputField(input, out InputField legacy))
			legacy.ActivateInputField();
	}

	public static bool IsFocused(Component input)
	{
		if (input == null || EventSystem.current == null)
			return false;

		GameObject selected = EventSystem.current.currentSelectedGameObject;
		if (selected == null)
			return false;

		Selectable selectable = GetSelectable(input);
		return selectable != null && selected == selectable.gameObject;
	}

	public static bool TryGetTmpInputField(Component input, out TMP_InputField field)
	{
		field = null;
		if (input == null)
			return false;

		if (input is TMP_InputField tmp)
		{
			field = tmp;
			return true;
		}

		field = input.GetComponent<TMP_InputField>();
		if (field != null)
			return true;

		field = input.GetComponentInChildren<TMP_InputField>(true);
		return field != null;
	}

	public static bool TryGetLegacyInputField(Component input, out InputField field)
	{
		field = null;
		if (input == null)
			return false;

		if (input is InputField legacy)
		{
			field = legacy;
			return true;
		}

		field = input.GetComponent<InputField>();
		if (field != null)
			return true;

		field = input.GetComponentInChildren<InputField>(true);
		return field != null;
	}
}
