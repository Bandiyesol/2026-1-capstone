#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AccessoryCatalogAssetPostprocessor : AssetPostprocessor

{

	const string AccessoryFolder = "Assets/Data/Accessory/";



	static void OnPostprocessAllAssets(

		string[] importedAssets,

		string[] deletedAssets,

		string[] movedAssets,

		string[] movedFromAssetPaths)

	{

		if (!TouchesAccessoryFolder(importedAssets)

		    && !TouchesAccessoryFolder(deletedAssets)

		    && !TouchesAccessoryFolder(movedAssets)

		    && !TouchesAccessoryFolder(movedFromAssetPaths))

			return;



		EditorApplication.delayCall += () =>

		{

			if (Application.isPlaying)

				return;



			RewardCatalogSettingsBuilder.RebuildFromMenu();

		};

	}



	static bool TouchesAccessoryFolder(string[] paths)

	{

		if (paths == null || paths.Length == 0)

			return false;



		return paths.Any(path => path != null && path.Replace('\\', '/').StartsWith(AccessoryFolder));

	}

}

#endif

