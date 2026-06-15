#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>에디터 플레이 중 Resources 카탈로그 없이 Data 폴더 SO를 직접 읽습니다.</summary>
public static class RewardCatalogEditorUtility
{
	const string AccessoryFolder = "Assets/Data/Accessory";

	public static List<AccessoryData> LoadAllAccessories()
	{
		return LoadAssets<AccessoryData>(AccessoryFolder);
	}

	static List<T> LoadAssets<T>(string folder) where T : Object
	{
		var result = new List<T>();
		if (!AssetDatabase.IsValidFolder(folder))
			return result;

		string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			T asset = AssetDatabase.LoadAssetAtPath<T>(path);
			if (asset != null)
				result.Add(asset);
		}

		result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
		return result;
	}
}
#endif
