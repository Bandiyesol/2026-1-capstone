#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Assets/Arts/Audio/SFX 파일명으로 SfxCatalog를 채웁니다.</summary>
public static class SfxCatalogBuilder
{
	const string SfxFolder = "Assets/Arts/Audio/SFX";
	const string CatalogPath = "Assets/Resources/Data/SfxCatalog.asset";

	static readonly (string fileName, SfxId id)[] ClipMap =
	{
		("클릭", SfxId.UiClick),
		("패널 오픈 소리", SfxId.PanelOpen),
		("상점 재진열", SfxId.ShopReroll),
		("아이템 구매, 보상 선택", SfxId.ItemPurchaseReward),
		("코인 획득", SfxId.CoinPickup),
		("상자 여는 소리", SfxId.ChestOpen),
		("플레이어가 맞을 때", SfxId.PlayerHit),
		("몬스터가 맞을 때", SfxId.EnemyHit),
		("몬스터가 죽을 때", SfxId.EnemyDeath),
		("소드", SfxId.WeaponSword),
		("낫", SfxId.WeaponScythe),
		("망치", SfxId.WeaponHammer),
		("총", SfxId.WeaponGun),
		("활", SfxId.WeaponBow),
		("채찍", SfxId.WeaponWhip),
		("부메랑", SfxId.WeaponBoomerang),
		("오브", SfxId.WeaponOrb),
		("마도서", SfxId.WeaponGrimoire),
		("스태프", SfxId.WeaponStaff),
	};

	[InitializeOnLoadMethod]
	static void EnsureOnLoad()
	{
		EditorApplication.delayCall += EnsureCatalogExists;
	}

	public static void RebuildCatalog(bool applyBalance = true)
	{
		EnsureDirectory("Assets/Resources/Data");

		SfxCatalog catalog = AssetDatabase.LoadAssetAtPath<SfxCatalog>(CatalogPath);
		if (catalog == null)
		{
			catalog = ScriptableObject.CreateInstance<SfxCatalog>();
			AssetDatabase.CreateAsset(catalog, CatalogPath);
		}

		Dictionary<string, AudioClip> clipsByName = LoadClipsByBaseName();
		var entries = new List<SfxCatalog.Entry>();

		foreach ((string fileName, SfxId id) in ClipMap)
		{
			clipsByName.TryGetValue(fileName, out AudioClip clip);
			if (clip == null)
				Debug.LogWarning($"[SfxCatalogBuilder] SFX 클립을 찾지 못했습니다: {SfxFolder}/**/{fileName}");

			entries.Add(new SfxCatalog.Entry
			{
				id = id,
				clip = clip,
				volumeScale = 1f,
			});
		}

		catalog.SetEntries(entries.ToArray());
		EditorUtility.SetDirty(catalog);
		SfxCatalog.SetCached(catalog);

		if (applyBalance)
			AudioCatalogRebuilder.RebalanceAllCatalogs(saveAssets: false);

		Debug.Log($"[SfxCatalogBuilder] SfxCatalog 갱신 — {CountAssigned(catalog)}/{ClipMap.Length}개 클립 연결");
	}

	public static void ApplyVolumeBalance(SfxCatalog catalog, float targetRms)
	{
		if (catalog?.entries == null)
			return;

		for (int i = 0; i < catalog.entries.Length; i++)
		{
			SfxCatalog.Entry entry = catalog.entries[i];
			entry.volumeScale = AudioCatalogBalanceUtility.ScaleForClip(entry.clip, targetRms);
			catalog.entries[i] = entry;
		}

		EditorUtility.SetDirty(catalog);
	}

	public static IEnumerable<AudioClip> CollectClips(SfxCatalog catalog)
	{
		if (catalog?.entries == null)
			yield break;

		foreach (SfxCatalog.Entry entry in catalog.entries)
		{
			if (entry.clip != null)
				yield return entry.clip;
		}
	}

	static void EnsureCatalogExists()
	{
		if (Application.isPlaying)
			return;

		if (!File.Exists(CatalogPath))
		{
			RebuildCatalog(applyBalance: true);
			return;
		}

		SfxCatalog catalog = AssetDatabase.LoadAssetAtPath<SfxCatalog>(CatalogPath);
		if (catalog == null || catalog.entries == null || catalog.entries.Length < ClipMap.Length)
			RebuildCatalog(applyBalance: true);
	}

	static Dictionary<string, AudioClip> LoadClipsByBaseName()
	{
		var map = new Dictionary<string, AudioClip>();
		if (!AssetDatabase.IsValidFolder(SfxFolder))
			return map;

		string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { SfxFolder });
		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (path.Contains("/_Misplaced/"))
				continue;

			AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
			if (clip == null)
				continue;

			string baseName = Path.GetFileNameWithoutExtension(path);
			if (!map.ContainsKey(baseName))
				map.Add(baseName, clip);
		}

		return map;
	}

	static int CountAssigned(SfxCatalog catalog)
	{
		int count = 0;
		if (catalog?.entries == null)
			return count;

		foreach (SfxCatalog.Entry entry in catalog.entries)
		{
			if (entry.clip != null)
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
