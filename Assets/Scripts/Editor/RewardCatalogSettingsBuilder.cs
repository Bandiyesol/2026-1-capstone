#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Data/Accessory 의 SO를 Resources 카탈로그에 등록합니다.
/// Unity를 열면 카탈로그가 없을 때 자동 생성됩니다.
/// </summary>
[InitializeOnLoad]
public static class RewardCatalogSettingsBuilder
{
	const string CatalogResourcePath = "Assets/Resources/Data/RewardCatalogSettings.asset";

	static RewardCatalogSettingsBuilder()
	{
		EditorApplication.delayCall += EnsureCatalogExists;
	}

	[MenuItem("Tools/Rebuild Reward Catalog")]
	public static void RebuildFromMenu()
	{
		RebuildCatalog(force: true);
	}

	static void EnsureCatalogExists()
	{
		if (Application.isPlaying)
			return;

		if (!File.Exists(CatalogResourcePath))
		{
			RebuildCatalog(force: false);
			return;
		}

		SyncCatalogIfStale();
	}

	static void SyncCatalogIfStale()
	{
		SyncCatalogIfStalePublic();
	}

	public static void SyncCatalogIfStalePublic()
	{
		int folderCount = RewardCatalogEditorUtility.LoadAllAccessories().Count;
		RewardCatalogSettings catalog = AssetDatabase.LoadAssetAtPath<RewardCatalogSettings>(CatalogResourcePath);
		int catalogCount = catalog?.allAccessories?.Count ?? 0;

		if (folderCount > 0 && folderCount != catalogCount)
			RebuildCatalog(force: true);
	}

	static void RebuildCatalog(bool force)
	{
		EnsureDirectory("Assets/Resources/Data");

		RewardCatalogSettings catalog = AssetDatabase.LoadAssetAtPath<RewardCatalogSettings>(CatalogResourcePath);
		if (catalog == null)
		{
			catalog = ScriptableObject.CreateInstance<RewardCatalogSettings>();
			AssetDatabase.CreateAsset(catalog, CatalogResourcePath);
		}

		catalog.allAccessories = RewardCatalogEditorUtility.LoadAllAccessories();
		catalog.allRelics = RewardCatalogEditorUtility.LoadAllRelics();

		EditorUtility.SetDirty(catalog);
		AssetDatabase.SaveAssets();
		RewardCatalogSettings.SetCached(catalog);

		Debug.Log(
			$"[RewardCatalogSettingsBuilder] 카탈로그 {(force ? "재생성" : "생성")} 완료 — " +
			$"악세사리 {catalog.allAccessories.Count}개, 성물 {catalog.allRelics.Count}개");

		TmpKoreanFontEditor.AddAccessoryGlyphsToNeoDgmSilent();
	}

	static void EnsureDirectory(string path)
	{
		if (AssetDatabase.IsValidFolder(path))
			return;

		string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
		string folderName = Path.GetFileName(path);
		if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
			EnsureDirectory(parent);

		AssetDatabase.CreateFolder(parent, folderName);
	}
}
#endif
