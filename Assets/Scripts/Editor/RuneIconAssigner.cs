#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Vol 6 Ui Expansion Pack / Runes 스프라이트를 RuneData.runeIcon에 연결합니다.</summary>
public static class RuneIconAssigner
{
	const string RuneArtRoot = "Assets/Arts/UI/Vol 6 Ui Expansion Pack/Runes";

	static readonly Dictionary<RuneType, string> IconByType = new()
	{
		{ RuneType.Orbit, "Runes_01_03.png" },
		{ RuneType.Wave, "Runes_02_03.png" },
		{ RuneType.Spiral, "Runes_03_03.png" },
		{ RuneType.Homing, "Runes_04_03.png" },
		{ RuneType.Split, "Runes_05_03.png" },
		{ RuneType.Ricochet, "Runes_06_03.png" },
		{ RuneType.Vampire, "Runes_07_03.png" },
		{ RuneType.Freeze, "Runes_08_03.png" },
		{ RuneType.Chain, "Runes_09_03.png" },
		{ RuneType.Explode, "Runes_10_03.png" },
		{ RuneType.Recursion, "Runes_11_03.png" },
		{ RuneType.Gravity, "Runes_12_03.png" },
		{ RuneType.Growth, "Runes_13_03.png" },
		{ RuneType.Blink, "Runes_14_03.png" },
		{ RuneType.Boing, "Runes_15_03.png" },
	};

	[MenuItem("Tools/Rune/Assign Vol 6 Rune Icons")]
	[MenuItem("Window/The Last Rune/Rune/Assign Vol 6 Rune Icons")]
	public static void AssignFromMenu()
	{
		int count = AssignAll();
		AssetDatabase.SaveAssets();
		EditorUtility.DisplayDialog(
			"룬 아이콘",
			$"Vol 6 Runes 아이콘 {count}개를 RuneData에 연결했습니다.",
			"확인");
	}

	public static int AssignAll()
	{
		string[] guids = AssetDatabase.FindAssets("t:RuneData", new[] { RunePaths.DataFolder });
		int count = 0;

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (path.Contains("RuneCatalog"))
				continue;

			var rune = AssetDatabase.LoadAssetAtPath<RuneData>(path);
			if (rune == null || rune.runeType == RuneType.None)
				continue;

			if (!IconByType.TryGetValue(rune.runeType, out string fileName))
				continue;

			Sprite sprite = LoadFirstSprite($"{RuneArtRoot}/{fileName}");
			if (sprite == null)
			{
				Debug.LogWarning($"[RuneIconAssigner] 스프라이트 없음: {fileName} ({rune.runeName})");
				continue;
			}

			rune.runeIcon = sprite;
			EditorUtility.SetDirty(rune);
			count++;
		}

		Debug.Log($"[RuneIconAssigner] 아이콘 연결 완료: {count}개");
		return count;
	}

	static Sprite LoadFirstSprite(string assetPath)
	{
		Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
		for (int i = 0; i < assets.Length; i++)
		{
			if (assets[i] is Sprite sprite)
				return sprite;
		}

		return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
	}
}
#endif
