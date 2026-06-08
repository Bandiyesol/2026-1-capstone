#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// neodgm SDF 메인 아틀라스(2048)가 가득 찼을 때, 같은 neodgm.ttf로 보조 SDF에 글리프를 구웁니다.
/// Pretendard가 아닌 neodgm 본체 → 픽셀 스타일 동일.
/// </summary>
public static class TmpKoreanFallbackFontEditor
{
	public const string FallbackAssetPath = "Assets/Arts/UI/Fonts/neodgm Korean Fallback SDF.asset";
	const string NeoDgmTtfPath = "Assets/Arts/UI/Fonts/neodgm.ttf";

	public static TMP_FontAsset EnsureFallbackLinked(TMP_FontAsset primary, string glyphs)
	{
		if (primary == null || string.IsNullOrEmpty(glyphs))
			return null;

		string primaryMissing = TmpKoreanFontUtility.GetPrimaryMissingCharacters(primary, glyphs);
		if (string.IsNullOrEmpty(primaryMissing))
			return GetLinkedFallback(primary);

		TMP_FontAsset supplement = LoadOrCreateSupplementAsset();
		if (supplement == null)
			return null;

		TmpKoreanFontEditor.EnsureDynamicAtlas(supplement);
		SetClearDynamicDataOnBuild(supplement, false);
		TmpKoreanFontEditor.TryAddAllCharactersPublic(supplement, primaryMissing);
		supplement.ReadFontAssetDefinition();
		EnsureFontSubAssets(supplement);
		LinkSupplement(primary, supplement);

		EditorUtility.SetDirty(supplement);
		EditorUtility.SetDirty(primary);
		AssetDatabase.SaveAssets();

		return supplement;
	}

	public static TMP_FontAsset LoadOrCreateSupplementAsset()
	{
		var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackAssetPath);
		if (existing != null && IsNeoDgmSupplement(existing))
		{
			RepairSupplementAssetIfNeeded(existing);
			return existing;
		}

		if (existing != null)
			AssetDatabase.DeleteAsset(FallbackAssetPath);

		return CreateSupplementAsset();
	}

	static bool IsNeoDgmSupplement(TMP_FontAsset fontAsset)
	{
		if (fontAsset == null)
			return false;

		string family = fontAsset.faceInfo.familyName ?? string.Empty;
		return family.Contains("Neo") || family.Contains("Dunggeunmo") || family.Contains("neodgm");
	}

	static TMP_FontAsset CreateSupplementAsset()
	{
		Font source = AssetDatabase.LoadAssetAtPath<Font>(NeoDgmTtfPath);
		if (source == null)
		{
			Debug.LogError($"[TmpKoreanFont] neodgm.ttf를 찾을 수 없습니다: {NeoDgmTtfPath}");
			return null;
		}

		TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(
			source,
			48,
			2,
			GlyphRenderMode.SDFAA,
			512,
			512,
			AtlasPopulationMode.Dynamic,
			true);
		if (created == null)
			return null;

		created.name = "neodgm Korean Fallback SDF";
		SetClearDynamicDataOnBuild(created, false);

		AssetDatabase.CreateAsset(created, FallbackAssetPath);
		EnsureFontSubAssets(created);

		TmpKoreanFontEditor.TryAddAllCharactersPublic(created, TmpKoreanFontUtility.AccessoryPriorityGlyphs);
		created.ReadFontAssetDefinition();
		EnsureFontSubAssets(created);

		AssetDatabase.SaveAssets();
		Debug.Log($"[TmpKoreanFont] neodgm 보조 SDF 생성: {FallbackAssetPath}");
		return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackAssetPath);
	}

	public static void RepairSupplementAssetIfNeeded(TMP_FontAsset fontAsset)
	{
		if (fontAsset == null)
			return;

		bool needsRepair = fontAsset.material == null
		                   || fontAsset.atlasTextures == null
		                   || fontAsset.atlasTextures.Length == 0
		                   || fontAsset.atlasTextures[0] == null
		                   || GetClearDynamicDataOnBuild(fontAsset);

		SetClearDynamicDataOnBuild(fontAsset, false);

		if (needsRepair)
			EnsureFontSubAssets(fontAsset);

		string missing = TmpKoreanFontUtility.GetPrimaryMissingCharacters(
			fontAsset,
			TmpKoreanFontUtility.AccessoryPriorityGlyphs);
		if (!string.IsNullOrEmpty(missing))
			TmpKoreanFontEditor.TryAddAllCharactersPublic(fontAsset, missing);

		fontAsset.ReadFontAssetDefinition();
		EnsureFontSubAssets(fontAsset);
		EditorUtility.SetDirty(fontAsset);
		AssetDatabase.SaveAssets();

		if (needsRepair)
			Debug.Log("[TmpKoreanFont] neodgm 보조 SDF Material/Atlas를 복구했습니다.");
	}

	public static void RepairFallbackAssetIfNeeded(TMP_FontAsset fontAsset)
	{
		RepairSupplementAssetIfNeeded(fontAsset);
	}

	static void EnsureFontSubAssets(TMP_FontAsset fontAsset)
	{
		if (fontAsset == null)
			return;

		int atlasWidth = fontAsset.atlasWidth > 0 ? fontAsset.atlasWidth : 512;
		int atlasHeight = fontAsset.atlasHeight > 0 ? fontAsset.atlasHeight : 512;
		int atlasPadding = fontAsset.atlasPadding > 0 ? fontAsset.atlasPadding : 2;

		Texture2D texture = fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0
			? fontAsset.atlasTextures[0]
			: null;

		if (texture == null)
		{
			texture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.Alpha8, false);
			if (fontAsset.atlasTextures == null || fontAsset.atlasTextures.Length == 0)
				fontAsset.atlasTextures = new Texture2D[1];
			fontAsset.atlasTextures[0] = texture;
		}

		texture.name = fontAsset.name + " Atlas";
		TryEmbedSubAsset(texture, fontAsset);

		Material mat = fontAsset.material;
		if (mat == null)
		{
			mat = new Material(Shader.Find("TextMeshPro/Distance Field"));
			fontAsset.material = mat;
		}

		mat.name = fontAsset.name + " Material";
		mat.SetTexture(ShaderUtilities.ID_MainTex, texture);
		mat.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
		mat.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);
		mat.SetFloat(ShaderUtilities.ID_GradientScale, atlasPadding + 1f);
		mat.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.normalStyle);
		mat.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyle);

		TryEmbedSubAsset(mat, fontAsset);
	}

	static void TryEmbedSubAsset(Object subAsset, Object rootAsset)
	{
		if (subAsset == null || rootAsset == null || subAsset == rootAsset)
			return;

		if (IsEmbeddedInAsset(subAsset, rootAsset))
			return;

		AssetDatabase.AddObjectToAsset(subAsset, rootAsset);
	}

	static bool IsEmbeddedInAsset(Object subAsset, Object rootAsset)
	{
		if (AssetDatabase.IsSubAsset(subAsset))
			return true;

		string subPath = AssetDatabase.GetAssetPath(subAsset);
		string rootPath = AssetDatabase.GetAssetPath(rootAsset);
		return !string.IsNullOrEmpty(subPath)
		       && subPath == rootPath
		       && subAsset != rootAsset;
	}

	static bool GetClearDynamicDataOnBuild(TMP_FontAsset fontAsset)
	{
		var so = new SerializedObject(fontAsset);
		SerializedProperty prop = so.FindProperty("m_ClearDynamicDataOnBuild");
		return prop != null && prop.boolValue;
	}

	static void SetClearDynamicDataOnBuild(TMP_FontAsset fontAsset, bool value)
	{
		var so = new SerializedObject(fontAsset);
		SerializedProperty prop = so.FindProperty("m_ClearDynamicDataOnBuild");
		if (prop == null)
			return;

		prop.boolValue = value;
		so.ApplyModifiedProperties();
	}

	static void LinkSupplement(TMP_FontAsset primary, TMP_FontAsset supplement)
	{
		if (primary.fallbackFontAssetTable == null)
			primary.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();

		primary.fallbackFontAssetTable.RemoveAll(entry => entry == null || entry == supplement);

		for (int i = primary.fallbackFontAssetTable.Count - 1; i >= 0; i--)
		{
			TMP_FontAsset entry = primary.fallbackFontAssetTable[i];
			if (entry != null && entry != supplement && !IsNeoDgmSupplement(entry))
				primary.fallbackFontAssetTable.RemoveAt(i);
		}

		if (!primary.fallbackFontAssetTable.Contains(supplement))
			primary.fallbackFontAssetTable.Insert(0, supplement);
	}

	static TMP_FontAsset GetLinkedFallback(TMP_FontAsset primary)
	{
		if (primary?.fallbackFontAssetTable == null || primary.fallbackFontAssetTable.Count == 0)
			return null;

		return primary.fallbackFontAssetTable[0];
	}

	public static string GetPrimaryOnlyMissing(TMP_FontAsset primary, string glyphs)
	{
		return TmpKoreanFontUtility.GetPrimaryMissingCharacters(primary, glyphs);
	}

	[MenuItem("Tools/Game/Repair neodgm Korean Fallback Font")]
	public static void RepairFallbackFromMenu()
	{
		var primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpKoreanFontUtility.NeoDgmAssetPath);
		if (primary == null)
			return;

		TMP_FontAsset supplement = LoadOrCreateSupplementAsset();
		if (supplement == null)
			return;

		string glyphs = TmpKoreanFontEditor.CollectAccessoryGlyphsForEditor()
		                  + TmpKoreanFontUtility.AccessoryPriorityGlyphs;
		EnsureFallbackLinked(primary, glyphs);

		string stillMissing = TmpKoreanFontUtility.GetMissingCharacters(primary, glyphs);
		if (string.IsNullOrEmpty(stillMissing))
		{
			EditorUtility.DisplayDialog(
				"neodgm 보조 폰트",
				"누락 글리프를 neodgm 보조 SDF Fallback에 저장했습니다.",
				"확인");
		}
		else
		{
			EditorUtility.DisplayDialog(
				"neodgm 보조 폰트",
				$"아직 표시되지 않는 글자: {stillMissing}",
				"확인");
		}
	}
}
#endif
