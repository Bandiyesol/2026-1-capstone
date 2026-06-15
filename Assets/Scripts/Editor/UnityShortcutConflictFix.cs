#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

/// <summary>
/// Unity Default 단축키 프로필은 읽기 전용이라, S 키 충돌 시 사용자 프로필을 만들어 수정합니다.
/// </summary>
[InitializeOnLoad]
static class UnityShortcutConflictFix
{
	const string ShowToolSettingsShortcut = "Overlays/Show Tool Settings";
	const string GridPaintingSelectShortcut = "Grid Painting/Select";
	const string WritableProfileId = "TheLastRune";
	const string AppliedPrefKey = "TheLastRune.ShortcutConflictFix.v2";

	static UnityShortcutConflictFix()
	{
		EditorApplication.delayCall += RunAutoFix;
	}

	static void RunAutoFix()
	{
		if (EditorPrefs.GetBool(AppliedPrefKey, false))
			return;

		try
		{
			if (TryFixSKeyConflict())
				EditorPrefs.SetBool(AppliedPrefKey, true);
		}
		catch (Exception ex)
		{
			Debug.LogWarning(
				"[Shortcut Fix] 자동 수정 실패: " + ex.Message +
				"\nEdit > Shortcuts 에서 'Overlays/Show Tool Settings' 의 S 할당을 Shift+S 등으로 변경해 주세요.");
		}
	}

	[MenuItem("Tools/Fix Unity S Key Shortcut Conflict")]
	static void FixFromMenu()
	{
		EditorPrefs.DeleteKey(AppliedPrefKey);

		try
		{
			if (TryFixSKeyConflict())
			{
				EditorPrefs.SetBool(AppliedPrefKey, true);
				EditorUtility.DisplayDialog(
					"Shortcut Conflict Fixed",
					"단축키 프로필 '" + WritableProfileId + "' 를 사용하도록 전환했습니다.\n" +
					"'Overlays/Show Tool Settings' 를 S → Shift+S 로 변경했습니다.",
					"OK");
			}
			else
			{
				EditorUtility.DisplayDialog(
					"Shortcut Conflict",
					"S 키 충돌이 감지되지 않았습니다.\n" +
					"이미 해결되었거나 Edit > Shortcuts 에서 직접 확인해 주세요.",
					"OK");
			}
		}
		catch (Exception ex)
		{
			EditorUtility.DisplayDialog(
				"Shortcut Conflict Fix Failed",
				ex.Message + "\n\nEdit > Shortcuts 에서 수동으로 변경해 주세요.",
				"OK");
		}
	}

	static bool TryFixSKeyConflict()
	{
		IShortcutManager manager = ShortcutManager.instance;
		if (manager == null)
			return false;

		ShortcutBinding toolSettingsBinding = manager.GetShortcutBinding(ShowToolSettingsShortcut);
		ShortcutBinding gridSelectBinding = manager.GetShortcutBinding(GridPaintingSelectShortcut);

		if (!BindingUsesPlainKey(toolSettingsBinding, KeyCode.S)
			|| !BindingUsesPlainKey(gridSelectBinding, KeyCode.S))
			return false;

		if (!EnsureWritableActiveProfile(manager))
			return false;

		manager.RebindShortcut(
			ShowToolSettingsShortcut,
			new ShortcutBinding(new KeyCombination(KeyCode.S, ShortcutModifiers.Shift)));

		Debug.Log(
			"[Shortcut Fix] 프로필 '" + WritableProfileId + "' 에서 " +
			"'Overlays/Show Tool Settings' 를 S → Shift+S 로 변경했습니다.");
		return true;
	}

	static bool EnsureWritableActiveProfile(IShortcutManager manager)
	{
		if (!manager.IsProfileReadOnly(manager.activeProfileId))
			return true;

		bool profileExists = false;
		foreach (string profileId in manager.GetAvailableProfileIds())
		{
			if (profileId == WritableProfileId)
			{
				profileExists = true;
				break;
			}
		}

		if (!profileExists)
			manager.CreateProfile(WritableProfileId);

		manager.activeProfileId = WritableProfileId;
		return !manager.IsProfileReadOnly(manager.activeProfileId);
	}

	static bool BindingUsesPlainKey(ShortcutBinding binding, KeyCode key)
	{
		if (binding.keyCombinationSequence == null)
			return false;

		foreach (KeyCombination combo in binding.keyCombinationSequence)
		{
			if (combo.keyCode == key && combo.modifiers == ShortcutModifiers.None)
				return true;
		}

		return false;
	}
}
#endif
