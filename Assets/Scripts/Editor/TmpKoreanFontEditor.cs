#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

public static class TmpKoreanFontEditor
{
	const string StatusMenuPath = "Tools/Game/Add Status UI Korean Glyphs (neodgm)";
	const string StoryMenuPath = "Tools/Game/Add Story UI Korean Glyphs (neodgm)";
	const string AccessoryMenuPath = "Tools/Game/Add Accessory UI Korean Glyphs (neodgm)";

	const string AccessoryPriorityGlyphs = TmpKoreanFontUtility.AccessoryPriorityGlyphs;

	[MenuItem(StatusMenuPath)]
	static void AddStatusGlyphsToNeoDgm()
	{
		AddGlyphsToNeoDgm(TmpKoreanFontUtility.StatusUiGlyphs, "Status UI");
	}

	[MenuItem(StoryMenuPath)]
	static void AddStoryGlyphsToNeoDgm()
	{
		AddGlyphsToNeoDgm(TmpKoreanFontUtility.StoryUiGlyphs, "Story UI");
	}

	[MenuItem(AccessoryMenuPath)]
	public static void AddAccessoryGlyphsToNeoDgm()
	{
		AddGlyphsToNeoDgm(CollectAccessoryGlyphsForEditor(), "Accessory UI");
	}

	/// <summary>Data/Accessory 폴더 전체 문자 (에디터 전용).</summary>
	public static string CollectAccessoryGlyphsForEditor()
	{
		var sb = new System.Text.StringBuilder(16384);
		sb.Append(TmpKoreanFontUtility.AccessoryUiCommonGlyphs);

		foreach (AccessoryData data in RewardCatalogEditorUtility.LoadAllAccessories())
			TmpKoreanFontUtility.AppendAccessoryText(sb, data);

		return sb.ToString();
	}

	/// <summary>카탈로그 갱신 등 자동 호출용 — 로그만 간단히 출력.</summary>
	public static void AddAccessoryGlyphsToNeoDgmSilent()
	{
		AddGlyphsToNeoDgm(CollectAccessoryGlyphsForEditor(), "Accessory UI", quiet: true);
	}

	static void AddGlyphsToNeoDgm(string glyphs, string label, bool quiet = false)
	{
		var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpKoreanFontUtility.NeoDgmAssetPath);
		if (font == null)
		{
			Debug.LogError($"[TmpKoreanFont] 폰트를 찾을 수 없습니다: {TmpKoreanFontUtility.NeoDgmAssetPath}");
			return;
		}

		EnsureDynamicAtlas(font);
		EnsureSourceFontLinked(font);
		SetClearDynamicDataOnBuild(font, false);

		string allGlyphs = DedupeCharacters(glyphs);
		if (label == "Accessory UI")
			allGlyphs = DedupeCharacters(allGlyphs + AccessoryPriorityGlyphs);

		string beforeMissing = TmpKoreanFontUtility.GetPrimaryMissingCharacters(font, allGlyphs);
		bool ok = string.IsNullOrEmpty(beforeMissing) || TryAddAllCharacters(font, beforeMissing);
		font.ReadFontAssetDefinition();

		string afterPrimaryMissing = TmpKoreanFontUtility.GetPrimaryMissingCharacters(font, allGlyphs);
		if (!string.IsNullOrEmpty(afterPrimaryMissing))
			TmpKoreanFallbackFontEditor.EnsureFallbackLinked(font, afterPrimaryMissing);

		string afterAnyMissing = TmpKoreanFontUtility.GetMissingCharacters(font, allGlyphs);

		if (!string.IsNullOrEmpty(afterAnyMissing))
		{
			if (!quiet)
			{
				Debug.LogWarning(
					$"[TmpKoreanFont] {label} 표시 불가 글자: [{afterAnyMissing}] (primary 누락: [{afterPrimaryMissing}])");
			}
		}
		else if (!string.IsNullOrEmpty(afterPrimaryMissing))
		{
			if (!quiet)
			{
				Debug.Log(
					$"[TmpKoreanFont] {label} 메인 아틀라스는 가득 참. neodgm 보조 SDF Fallback에 저장: {afterPrimaryMissing}");
			}
		}
		else if (!quiet && !string.IsNullOrEmpty(beforeMissing))
		{
			Debug.Log($"[TmpKoreanFont] {label} neodgm SDF에 글리프 추가 완료. 이전 누락: {beforeMissing}");
		}
		else if (quiet && string.IsNullOrEmpty(afterAnyMissing) && !string.IsNullOrEmpty(beforeMissing))
		{
			Debug.Log($"[TmpKoreanFont] {label} neodgm SDF 글리프 반영 완료.");
		}

		if (!ok && !string.IsNullOrEmpty(afterPrimaryMissing) && string.IsNullOrEmpty(afterAnyMissing))
			ok = true;

		EditorUtility.SetDirty(font);
		PersistFontAsset(font);
		AssetDatabase.SaveAssets();
		if (!quiet)
			Debug.Log($"[TmpKoreanFont] neodgm SDF (Dynamic)에 {label} 한글을 반영했습니다.");
	}

	static bool TryAddAllCharacters(TMP_FontAsset font, string glyphs)
	{
		return TryAddAllCharactersPublic(font, glyphs);
	}

	public static void EnsureDynamicAtlas(TMP_FontAsset font)
	{
		var so = new SerializedObject(font);
		SerializedProperty mode = so.FindProperty("m_AtlasPopulationMode");
		if (mode == null)
			return;

		if (mode.intValue == 0)
		{
			mode.intValue = 1;
			so.ApplyModifiedProperties();
			Debug.Log("[TmpKoreanFont] neodgm SDF Atlas Population Mode → Dynamic 으로 변경했습니다.");
		}
	}

	public static bool TryAddAllCharactersPublic(TMP_FontAsset font, string glyphs)
	{
		if (font == null || string.IsNullOrEmpty(glyphs))
			return true;

		string unique = DedupeCharacters(glyphs);
		string missing = TmpKoreanFontUtility.GetPrimaryMissingCharacters(font, unique);
		if (string.IsNullOrEmpty(missing))
			return true;

		if (!font.TryAddCharacters(missing, out string batchMissing, true))
		{
			Debug.LogWarning($"[TmpKoreanFont] {font.name} TryAddCharacters 실패 — 아틀라스 공간 부족 가능: {missing}");
			return false;
		}

		if (!string.IsNullOrEmpty(batchMissing))
		{
			bool allOk = true;
			foreach (char c in batchMissing)
			{
				if (!font.TryAddCharacters(c.ToString(), out _, true))
					allOk = false;
			}

			if (!allOk)
			{
				Debug.LogWarning($"[TmpKoreanFont] {font.name} 일부 글리프 추가 실패: {batchMissing}");
				return false;
			}
		}

		font.ReadFontAssetDefinition();
		PersistFontAsset(font);
		return true;
	}

	public static void PersistFontAsset(TMP_FontAsset font)
	{
		if (font == null)
			return;

		if (font.material != null)
			EditorUtility.SetDirty(font.material);

		if (font.atlasTextures != null)
		{
			foreach (Texture2D atlas in font.atlasTextures)
			{
				if (atlas != null)
					EditorUtility.SetDirty(atlas);
			}
		}

		EditorUtility.SetDirty(font);
	}

	static string DedupeCharacters(string text)
	{
		if (string.IsNullOrEmpty(text))
			return text;

		var seen = new System.Collections.Generic.HashSet<char>();
		var sb = new System.Text.StringBuilder(text.Length);
		foreach (char c in text)
		{
			if (c == '\n' || c == '\r' || c == '\t')
				continue;

			if (seen.Add(c))
				sb.Append(c);
		}

		return sb.ToString();
	}

	[MenuItem(StatusMenuPath, true)]
	[MenuItem(StoryMenuPath, true)]
	[MenuItem(AccessoryMenuPath, true)]
	static bool ValidateAddGlyphs()
	{
		return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpKoreanFontUtility.NeoDgmAssetPath) != null;
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

	static void SetClearDynamicDataOnBuild(TMP_FontAsset font, bool value)
	{
		var so = new SerializedObject(font);
		SerializedProperty prop = so.FindProperty("m_ClearDynamicDataOnBuild");
		if (prop == null)
			return;

		prop.boolValue = value;
		so.ApplyModifiedProperties();
	}
}
#endif
