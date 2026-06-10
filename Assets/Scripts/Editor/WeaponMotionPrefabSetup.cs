#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Resources/Prefabs/Motions 에 없는 무기 Motion 프리팹을 effect_bow 템플릿으로 생성합니다.
/// Unity 메뉴: Tools → Weapon → Create Missing Motion Prefabs
/// </summary>
public static class WeaponMotionPrefabSetup
{
	const string Folder = "Assets/Resources/Prefabs/Motions";
	const string BowTemplatePath = Folder + "/effect_bow.prefab";

	static readonly (string prefabName, Type motionType)[] MissingPrefabs =
	{
		("effect_gun", typeof(MotionGun)),
		("effect_hammer", typeof(MotionHammer)),
		("effect_sickle", typeof(MotionSickle)),
		("effect_whip", typeof(MotionWhip)),
		("effect_boomerang", typeof(MotionBoomerang)),
		("effect_staff", typeof(MotionStaff)),
		("effect_grimore", typeof(MotionGrimore)),
	};

	[MenuItem("Tools/Weapon/Create Missing Motion Prefabs")]
	[MenuItem("Window/The Last Rune/Weapon/Create Missing Motion Prefabs")]
	public static void CreateMissingMotionPrefabs()
	{
		GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(BowTemplatePath);
		if (template == null)
		{
			Debug.LogError($"[WeaponMotionPrefabSetup] 템플릿을 찾을 수 없습니다: {BowTemplatePath}");
			return;
		}

		EnsureFolderExists(Folder);

		int created = 0;
		int skipped = 0;

		foreach ((string prefabName, Type motionType) in MissingPrefabs)
		{
			string path = $"{Folder}/{prefabName}.prefab";
			if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
			{
				skipped++;
				continue;
			}

			if (TryCreatePrefab(template, path, prefabName, motionType))
				created++;
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log($"[WeaponMotionPrefabSetup] 생성 {created}개, 스킵 {skipped}개. 경로: {Folder}");
	}

	[MenuItem("Tools/Weapon/Validate Motion Prefabs")]
	[MenuItem("Window/The Last Rune/Weapon/Validate Motion Prefabs")]
	public static void ValidateMotionPrefabs()
	{
		var missing = new List<string>();
		foreach (WeaponInfo info in WeaponManager.Instance != null
			         ? WeaponManager.Instance.GetAllWeaponInfos()
			         : LoadWeaponInfosFromResources())
		{
			if (string.IsNullOrWhiteSpace(info.motionId)) continue;
			string path = $"{Folder}/{info.motionId}.prefab";
			if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
				missing.Add($"{info.id} → {info.motionId}");
		}

		if (missing.Count == 0)
			Debug.Log("[WeaponMotionPrefabSetup] WeaponInfo.json에 필요한 Motion 프리팹이 모두 있습니다.");
		else
			Debug.LogWarning("[WeaponMotionPrefabSetup] 누락 프리팹:\n- " + string.Join("\n- ", missing));
	}

	static IEnumerable<WeaponInfo> LoadWeaponInfosFromResources()
	{
		TextAsset infoJson = Resources.Load<TextAsset>("Data/WeaponInfo");
		if (infoJson == null) yield break;

		WeaponDataLoader loader = JsonUtility.FromJson<WeaponDataLoader>(infoJson.text);
		if (loader?.info == null) yield break;

		foreach (WeaponInfo info in loader.info)
			yield return info;
	}

	static bool TryCreatePrefab(GameObject template, string path, string prefabName, Type motionType)
	{
		GameObject instance = PrefabUtility.InstantiatePrefab(template) as GameObject;
		if (instance == null)
		{
			Debug.LogError($"[WeaponMotionPrefabSetup] 템플릿 인스턴스 생성 실패: {prefabName}");
			return false;
		}

		instance.name = prefabName;

		foreach (Motion motion in instance.GetComponents<Motion>())
			UnityEngine.Object.DestroyImmediate(motion);

		if (instance.GetComponent(motionType) == null)
			instance.AddComponent(motionType);

		GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
		UnityEngine.Object.DestroyImmediate(instance);

		if (prefab == null)
		{
			Debug.LogError($"[WeaponMotionPrefabSetup] 저장 실패: {path}");
			return false;
		}

		Debug.Log($"[WeaponMotionPrefabSetup] {path} ({motionType.Name})");
		return true;
	}

	static void EnsureFolderExists(string folder)
	{
		if (AssetDatabase.IsValidFolder(folder)) return;

		const string parent = "Assets/Resources/Prefabs";
		if (!AssetDatabase.IsValidFolder(parent))
			AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
		if (!AssetDatabase.IsValidFolder(folder))
			AssetDatabase.CreateFolder(parent, "Motions");
	}
}
#endif
