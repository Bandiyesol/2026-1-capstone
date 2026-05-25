using TMPro;
using UnityEngine;
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

	static bool TryGetTmpInputField(Component input, out TMP_InputField field)
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

	static bool TryGetLegacyInputField(Component input, out InputField field)
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
