using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 로그인·회원가입·비밀번호 찾기 폼: Tab → 다음 입력칸, Enter → 확인 버튼.
/// </summary>
public class AuthFormKeyboardNavigation : MonoBehaviour
{
	public struct FormConfig
	{
		public GameObject panel;
		public Component[] inputs;
		public Button submitButton;
	}

	FormConfig[] forms;
	Func<bool> isBlocked;

	public void Initialize(FormConfig[] formConfigs, Func<bool> isBlockedCallback)
	{
		isBlocked = isBlockedCallback;

		if (formConfigs == null)
			return;

		var list = new List<FormConfig>();
		foreach (FormConfig form in formConfigs)
		{
			if (form.panel == null)
				continue;

			list.Add(form);
			WireForm(form);
		}

		forms = list.ToArray();
	}

	public void RegisterForm(FormConfig form)
	{
		if (form.panel == null)
			return;

		if (forms == null || forms.Length == 0)
		{
			forms = new[] { form };
			WireForm(form);
			return;
		}

		for (int i = 0; i < forms.Length; i++)
		{
			if (forms[i].panel != form.panel)
				continue;

			forms[i] = form;
			WireForm(form);
			return;
		}

		var list = new List<FormConfig>(forms) { form };
		forms = list.ToArray();
		WireForm(form);
	}

	void Update()
	{
		if (isBlocked != null && isBlocked())
			return;

		Keyboard keyboard = Keyboard.current;
		if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame)
			return;

		if (!TryGetActiveForm(out FormConfig form))
			return;

		if (!TryGetFocusedInputIndex(form.inputs, out int index))
			return;

		bool reverse = keyboard.shiftKey.isPressed;
		int nextIndex = reverse ? index - 1 : index + 1;
		if (nextIndex < 0)
			nextIndex = form.inputs.Length - 1;
		else if (nextIndex >= form.inputs.Length)
			nextIndex = 0;

		RemoveTrailingTab(form.inputs[index]);
		AuthInputUtility.Focus(form.inputs[nextIndex]);
	}

	void WireForm(FormConfig form)
	{
		if (form.inputs == null || form.inputs.Length == 0)
			return;

		WireNavigationChain(form.inputs);

		foreach (Component input in form.inputs)
		{
			if (AuthInputUtility.TryGetTmpInputField(input, out TMP_InputField tmp))
			{
				tmp.lineType = TMP_InputField.LineType.SingleLine;
				tmp.onSubmit.RemoveAllListeners();
				tmp.onSubmit.AddListener(_ => TrySubmit(form));
				continue;
			}

			if (AuthInputUtility.TryGetLegacyInputField(input, out InputField legacy))
			{
				legacy.lineType = InputField.LineType.SingleLine;
				legacy.onEndEdit.RemoveAllListeners();
				legacy.onEndEdit.AddListener(_ =>
				{
					if (WasEnterSubmitted())
						TrySubmit(form);
				});
			}
		}
	}

	static void WireNavigationChain(Component[] inputs)
	{
		var selectables = new Selectable[inputs.Length];
		for (int i = 0; i < inputs.Length; i++)
			selectables[i] = AuthInputUtility.GetSelectable(inputs[i]);

		for (int i = 0; i < selectables.Length; i++)
		{
			Selectable current = selectables[i];
			if (current == null)
				continue;

			Selectable previous = i > 0 ? selectables[i - 1] : selectables[selectables.Length - 1];
			Selectable next = i + 1 < selectables.Length ? selectables[i + 1] : selectables[0];

			Navigation nav = current.navigation;
			nav.mode = Navigation.Mode.Explicit;
			nav.selectOnUp = previous;
			nav.selectOnDown = next;
			nav.selectOnLeft = previous;
			nav.selectOnRight = next;
			current.navigation = nav;
		}
	}

	void TrySubmit(FormConfig form)
	{
		if (isBlocked != null && isBlocked())
			return;

		if (form.panel == null || !form.panel.activeInHierarchy)
			return;

		if (form.submitButton == null || !form.submitButton.interactable)
			return;

		form.submitButton.onClick.Invoke();
	}

	bool TryGetActiveForm(out FormConfig form)
	{
		form = default;

		if (forms == null)
			return false;

		foreach (FormConfig candidate in forms)
		{
			if (candidate.panel != null && candidate.panel.activeInHierarchy)
			{
				form = candidate;
				return true;
			}
		}

		return false;
	}

	static bool TryGetFocusedInputIndex(Component[] inputs, out int index)
	{
		index = -1;

		if (inputs == null || EventSystem.current == null)
			return false;

		for (int i = 0; i < inputs.Length; i++)
		{
			if (AuthInputUtility.IsFocused(inputs[i]))
			{
				index = i;
				return true;
			}
		}

		return false;
	}

	static void RemoveTrailingTab(Component input)
	{
		if (AuthInputUtility.TryGetTmpInputField(input, out TMP_InputField tmp))
		{
			if (tmp.text.EndsWith("\t"))
				tmp.text = tmp.text.TrimEnd('\t');
			return;
		}

		if (AuthInputUtility.TryGetLegacyInputField(input, out InputField legacy)
		    && legacy.text.EndsWith("\t"))
		{
			legacy.text = legacy.text.TrimEnd('\t');
		}
	}

	static bool WasEnterSubmitted()
	{
		Keyboard keyboard = Keyboard.current;
		if (keyboard != null)
		{
			if (keyboard.enterKey.wasReleasedThisFrame
			    || keyboard.numpadEnterKey.wasReleasedThisFrame)
				return true;
		}

		return Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter);
	}
}
