using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class KeyRebindUI : MonoBehaviour
{
	[SerializeField] private string actionName;
	[SerializeField] private string bindingName;
	[SerializeField] private TextMeshProUGUI bindingKeyName;

	private int bindingIndex;
	private InputAction _targetAction;
	private InputActionRebindingExtensions.RebindingOperation _rebindOperation;


	void Start()
	{
		if (KeyBindManager.Instance == null) return;

		_targetAction = KeyBindManager.Instance.Controls.FindAction(actionName);

		if (_targetAction == null) return;

		for (int i = 0; i < _targetAction.bindings.Count; i++)
		{
			if (_targetAction.bindings[i].isPartOfComposite && _targetAction.bindings[i].name.Equals(bindingName, System.StringComparison.OrdinalIgnoreCase))
			{
				bindingIndex = i;
				UpdateUI();
			}
		}		
	}

	public void StartRebinding()
	{
		if (_targetAction == null) return;

		bool indexFound = false;

		for (int i = 0; i < _targetAction.bindings.Count; i++)
		{
			if (_targetAction.bindings[i].isPartOfComposite && _targetAction.bindings[i].name.Equals(bindingName, System.StringComparison.OrdinalIgnoreCase))
			{
				bindingIndex = i;
				indexFound = true;
				break;
			}
		}

		if (!indexFound) return;

		_targetAction.Disable();
		bindingKeyName.text = "?";
		_rebindOperation = _targetAction.PerformInteractiveRebinding(bindingIndex)
		.WithControlsExcluding("<Mouse>/leftButton")
		.OnComplete(operation => FinishRebinding())
		.Start();
	}

	private void FinishRebinding()
	{
		_rebindOperation.Dispose();
		_targetAction.Enable();

		KeyBindManager.Instance.SaveSetting();
		UpdateUI();
	}

	public void UpdateUI()
	{
		bindingKeyName.text = InputControlPath.ToHumanReadableString(
			_targetAction.bindings[bindingIndex].effectivePath,
			InputControlPath.HumanReadableStringOptions.OmitDevice
		);
	}
}