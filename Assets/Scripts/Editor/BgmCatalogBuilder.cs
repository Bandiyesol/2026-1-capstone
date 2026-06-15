#if UNITY_EDITOR

using System.Collections.Generic;

using System.IO;

using UnityEditor;

using UnityEngine;



/// <summary>Assets/Arts/Audio/BGM 파일명으로 BgmCatalog를 채웁니다.</summary>

[InitializeOnLoad]

public static class BgmCatalogBuilder

{

	const string BgmFolder = "Assets/Arts/Audio/BGM";

	const string CatalogPath = "Assets/Resources/Data/BgmCatalog.asset";



	static readonly string MainMenuFileName = "배경음악";

	static readonly string ShopFileName = "상점";

	static readonly string StageClearFileName = "클리어 했을 때";

	static readonly string DeathFileName = "사망 했을 때";

	static readonly string[] StageFileNames =

	{

		"숲",

		"동굴",

		"바다",

		"용암",

		"설원",

		"사막",

		"보이드",

	};



	static BgmCatalogBuilder()

	{

		EditorApplication.delayCall += EnsureCatalogExists;

	}



	[MenuItem("Tools/Game/Rebuild Bgm Catalog")]

	public static void RebuildFromMenu()

	{

		RebuildCatalog(force: true, applyBalance: true);

	}



	[MenuItem("Tools/Game/Rebalance Bgm Volumes")]

	public static void RebalanceVolumesFromMenu()

	{

		AudioCatalogRebuilder.RebalanceAllFromMenu();

	}



	static void EnsureCatalogExists()

	{

		if (Application.isPlaying)

			return;



		if (!File.Exists(CatalogPath))

		{

			RebuildCatalog(force: false, applyBalance: true);

			return;

		}



		BgmCatalog catalog = AssetDatabase.LoadAssetAtPath<BgmCatalog>(CatalogPath);

		if (catalog == null || catalog.mainMenuClip == null || catalog.shopClip == null

		    || catalog.stageClearClip == null || catalog.deathClip == null || !HasAllStageClips(catalog))

			RebuildCatalog(force: true, applyBalance: true);

	}



	static bool HasAllStageClips(BgmCatalog catalog)

	{

		if (catalog.stageClips == null || catalog.stageClips.Length < BgmCatalog.StageCount)

			return false;



		for (int i = 0; i < BgmCatalog.StageCount; i++)

		{

			if (catalog.stageClips[i] == null)

				return false;

		}



		return true;

	}



	public static void RebuildCatalog(bool force, bool applyBalance = true)

	{

		EnsureDirectory("Assets/Resources/Data");



		BgmCatalog catalog = AssetDatabase.LoadAssetAtPath<BgmCatalog>(CatalogPath);

		if (catalog == null)

		{

			catalog = ScriptableObject.CreateInstance<BgmCatalog>();

			AssetDatabase.CreateAsset(catalog, CatalogPath);

		}



		Dictionary<string, AudioClip> clipsByName = LoadClipsByBaseName();



		catalog.mainMenuClip = FindClip(clipsByName, MainMenuFileName);

		catalog.shopClip = FindClip(clipsByName, ShopFileName);

		catalog.stageClearClip = FindClip(clipsByName, StageClearFileName);

		catalog.deathClip = FindClip(clipsByName, DeathFileName);



		if (catalog.stageClips == null || catalog.stageClips.Length != BgmCatalog.StageCount)

			catalog.stageClips = new AudioClip[BgmCatalog.StageCount];



		for (int i = 0; i < StageFileNames.Length; i++)

			catalog.stageClips[i] = FindClip(clipsByName, StageFileNames[i]);



		EditorUtility.SetDirty(catalog);

		BgmCatalog.SetCached(catalog);



		if (applyBalance)

			AudioCatalogRebuilder.RebalanceAllCatalogs(saveAssets: true);

		else

			AssetDatabase.SaveAssets();



		Debug.Log(

			$"[BgmCatalogBuilder] 카탈로그 {(force ? "재생성" : "생성")} 완료 — " +

			$"메인={(catalog.mainMenuClip != null ? catalog.mainMenuClip.name : "없음")}, " +

			$"상점={(catalog.shopClip != null ? catalog.shopClip.name : "없음")}, " +

			$"클리어={(catalog.stageClearClip != null ? catalog.stageClearClip.name : "없음")}, " +

			$"사망={(catalog.deathClip != null ? catalog.deathClip.name : "없음")}, " +

			$"스테이지={CountAssignedStages(catalog)}/{BgmCatalog.StageCount}");

	}



	public static void ApplyVolumeBalance(BgmCatalog catalog, float targetRms)

	{

		if (catalog == null)

			return;



		catalog.mainMenuVolumeScale = AudioCatalogBalanceUtility.ScaleForClip(catalog.mainMenuClip, targetRms);

		catalog.shopVolumeScale = AudioCatalogBalanceUtility.ScaleForClip(catalog.shopClip, targetRms);

		catalog.stageClearVolumeScale = AudioCatalogBalanceUtility.ScaleForClip(catalog.stageClearClip, targetRms);

		catalog.deathVolumeScale = AudioCatalogBalanceUtility.ScaleForClip(catalog.deathClip, targetRms);



		if (catalog.stageVolumeScales == null || catalog.stageVolumeScales.Length != BgmCatalog.StageCount)

			catalog.stageVolumeScales = new float[BgmCatalog.StageCount];



		for (int i = 0; i < BgmCatalog.StageCount; i++)

		{

			AudioClip clip = catalog.stageClips != null ? catalog.stageClips[i] : null;

			catalog.stageVolumeScales[i] = AudioCatalogBalanceUtility.ScaleForClip(clip, targetRms);

		}



		EditorUtility.SetDirty(catalog);

	}



	public static IEnumerable<AudioClip> CollectClips(BgmCatalog catalog)

	{

		if (catalog == null)

			yield break;



		if (catalog.mainMenuClip != null) yield return catalog.mainMenuClip;

		if (catalog.shopClip != null) yield return catalog.shopClip;

		if (catalog.stageClearClip != null) yield return catalog.stageClearClip;

		if (catalog.deathClip != null) yield return catalog.deathClip;



		if (catalog.stageClips == null)

			yield break;



		foreach (AudioClip clip in catalog.stageClips)

		{

			if (clip != null)

				yield return clip;

		}

	}



	static Dictionary<string, AudioClip> LoadClipsByBaseName()

	{

		var map = new Dictionary<string, AudioClip>();



		if (!AssetDatabase.IsValidFolder(BgmFolder))

			return map;



		string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { BgmFolder });

		foreach (string guid in guids)

		{

			string path = AssetDatabase.GUIDToAssetPath(guid);

			AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

			if (clip == null)

				continue;



			string baseName = Path.GetFileNameWithoutExtension(path);

			if (!map.ContainsKey(baseName))

				map.Add(baseName, clip);

		}



		return map;

	}



	static AudioClip FindClip(Dictionary<string, AudioClip> map, string baseName)

	{

		if (map.TryGetValue(baseName, out AudioClip clip))

			return clip;



		Debug.LogWarning($"[BgmCatalogBuilder] BGM 클립을 찾지 못했습니다: {BgmFolder}/{baseName}");

		return null;

	}



	static int CountAssignedStages(BgmCatalog catalog)

	{

		int count = 0;

		if (catalog.stageClips == null)

			return count;



		foreach (AudioClip clip in catalog.stageClips)

		{

			if (clip != null)

				count++;

		}



		return count;

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


