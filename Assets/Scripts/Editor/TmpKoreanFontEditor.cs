#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

public static class TmpKoreanFontEditor
{
	const string MenuPath = "Tools/Game/Add Status UI Korean Glyphs (neodgm)";

	[MenuItem(MenuPath)]
	static void AddStatusGlyphsToNeoDgm()
	{
		var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpKoreanFontUtility.NeoDgmAssetPath);
		if (font == null)
		{
			Debug.LogError($"[TmpKoreanFont] 폰트를 찾을 수 없습니다: {TmpKoreanFontUtility.NeoDgmAssetPath}");
			return;
		}

		EnsureDynamicAtlas(font);
		EnsureSourceFontLinked(font);

		string beforeMissing = TmpKoreanFontUtility.GetMissingCharacters(font, TmpKoreanFontUtility.StatusUiGlyphs);
		bool ok = TmpKoreanFontUtility.TryAddStringCharacters(font, TmpKoreanFontUtility.StatusUiGlyphs);
		string afterMissing = TmpKoreanFontUtility.GetMissingCharacters(font, TmpKoreanFontUtility.StatusUiGlyphs);

		if (!ok || !string.IsNullOrEmpty(afterMissing))
		{
			Debug.LogWarning(
				$"[TmpKoreanFont] 글리프 추가 실패 또는 누락: [{afterMissing}] (추가 전: [{beforeMissing}])\n" +
				"Window > TextMeshPro > Font Asset Creator에서 neodgm SDF를 열고 Character Set에 한글을 포함해 Generate Font Atlas 하세요.");
		}
		else if (!string.IsNullOrEmpty(beforeMissing))
		{
			Debug.Log($"[TmpKoreanFont] 추가 완료. 이전 누락: {beforeMissing}");
		}

		EditorUtility.SetDirty(font);
		AssetDatabase.SaveAssets();
		Debug.Log("[TmpKoreanFont] neodgm SDF (Dynamic)에 Status UI 한글을 반영했습니다.");
	}

	[MenuItem(MenuPath, true)]
	static bool ValidateAddStatusGlyphs()
	{
		return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpKoreanFontUtility.NeoDgmAssetPath) != null;
	}

	static void EnsureDynamicAtlas(TMP_FontAsset font)
	{
		var so = new SerializedObject(font);
		SerializedProperty mode = so.FindProperty("m_AtlasPopulationMode");
		if (mode == null)
			return;

		// 0 = Static (TryAddCharacters 불가), 1 = Dynamic
		if (mode.intValue == 0)
		{
			mode.intValue = 1;
			so.ApplyModifiedProperties();
			Debug.Log("[TmpKoreanFont] neodgm SDF Atlas Population Mode → Dynamic 으로 변경했습니다.");
		}
	}

	static void EnsureSourceFontLinked(TMP_FontAsset font)
	{
		var so = new SerializedObject(font);
		SerializedProperty source = so.FindProperty("m_SourceFontFile");
		if (source == null || source.objectReferenceValue != null)
			return;

		var ttf = AssetDatabase.LoadAssetAtPath<Font>("Assets/Arts/UI/Fonts/neodgm.ttf");
		if (ttf == null)
		{
			Debug.LogWarning("[TmpKoreanFont] neodgm.ttf를 찾을 수 없습니다.");
			return;
		}

		source.objectReferenceValue = ttf;
		so.ApplyModifiedProperties();
		Debug.Log("[TmpKoreanFont] neodgm SDF에 소스 폰트(neodgm.ttf)를 연결했습니다.");
	}
}
#endif
