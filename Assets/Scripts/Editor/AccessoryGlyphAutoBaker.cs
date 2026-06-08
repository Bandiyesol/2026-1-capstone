#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 에디터 시작·플레이 종료 시 neodgm SDF + neodgm 보조 Fallback SDF에 글리프를 반영합니다.
/// </summary>
[InitializeOnLoad]
static class AccessoryGlyphAutoBaker
{
	static AccessoryGlyphAutoBaker()
	{
		EditorApplication.delayCall += TryBakeIfNeeded;
		EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
	}

	static void OnPlayModeStateChanged(PlayModeStateChange state)
	{
		if (state == PlayModeStateChange.ExitingPlayMode)
			EditorApplication.delayCall += TryBakeIfNeeded;

		if (state == PlayModeStateChange.EnteredPlayMode)
			TmpKoreanFontUtility.ResetRuntimeGlyphCache();
	}

	static void TryBakeIfNeeded()
	{
		if (Application.isPlaying)
			return;

		RewardCatalogSettingsBuilder.SyncCatalogIfStalePublic();

		var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpKoreanFontUtility.NeoDgmAssetPath);
		if (font == null)
			return;

		string glyphs = TmpKoreanFontEditor.CollectAccessoryGlyphsForEditor()
		                + TmpKoreanFontUtility.AccessoryPriorityGlyphs;
		string missingBefore = TmpKoreanFontUtility.GetMissingCharacters(font, glyphs);
		if (string.IsNullOrEmpty(missingBefore))
			return;

		TmpKoreanFontEditor.AddAccessoryGlyphsToNeoDgmSilent();

		font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpKoreanFontUtility.NeoDgmAssetPath);
		string missingAfter = TmpKoreanFontUtility.GetMissingCharacters(font, glyphs);
		string missingPrimary = TmpKoreanFontUtility.GetPrimaryMissingCharacters(font, glyphs);

		if (!string.IsNullOrEmpty(missingAfter))
		{
			Debug.LogError(
				$"[TmpKoreanFont] 글리프 저장 실패: {missingAfter}\n" +
				"Tools > Game > Repair neodgm Korean Fallback Font 를 실행하세요.");
			return;
		}

		if (!string.IsNullOrEmpty(missingPrimary))
		{
			Debug.Log(
				$"[TmpKoreanFont] 메인 neodgm SDF 아틀라스는 가득 참. " +
				$"neodgm 보조 Fallback에 저장됨: {missingPrimary}");
		}
		else if (!string.IsNullOrEmpty(missingBefore))
		{
			Debug.Log($"[TmpKoreanFont] neodgm SDF 아틀라스 저장 완료: {missingBefore}");
		}
	}
}
#endif
