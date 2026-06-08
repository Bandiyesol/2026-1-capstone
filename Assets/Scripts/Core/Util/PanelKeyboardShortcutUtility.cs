using UnityEngine.InputSystem;

/// <summary>모달 UI에서 Esc / Enter 단축키 감지.</summary>
public static class PanelKeyboardShortcutUtility
{
	public static bool WasEscapeOrEnterPressedThisFrame()
	{
		Keyboard keyboard = Keyboard.current;
		if (keyboard == null)
			return false;

		return keyboard.escapeKey.wasPressedThisFrame
			|| keyboard.enterKey.wasPressedThisFrame
			|| keyboard.numpadEnterKey.wasPressedThisFrame;
	}
}
