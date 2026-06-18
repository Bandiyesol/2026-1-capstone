using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 서로 다른 FirebaseCppApp 버전 DLL이 같이 있으면 네이티브 크래시가 납니다. 빌드 전에 하나만 남깁니다.
/// </summary>
public sealed class FirebaseDuplicatePluginCleanup : IPreprocessBuildWithReport
{
	const string PluginsRelative = "Assets/Plugins/x86_64";

	public int callbackOrder => 0;

	[MenuItem("Tools/Game/Prune Duplicate Firebase Native DLLs")]
	public static void PruneFromMenu()
	{
		int removed = PruneDuplicates();
		if (removed == 0)
			Debug.Log("[FirebaseDuplicatePluginCleanup] 중복 FirebaseCppApp DLL 없음.");
		else
			Debug.Log($"[FirebaseDuplicatePluginCleanup] {removed}개 중복 DLL 제거 완료. 다시 빌드하세요.");
	}

	public void OnPreprocessBuild(BuildReport report)
	{
		PruneDuplicates();
	}

	static int PruneDuplicates()
	{
		string pluginsDir = Path.Combine(Application.dataPath, "Plugins", "x86_64");
		if (!Directory.Exists(pluginsDir))
			return 0;

		var appDlls = Directory.GetFiles(pluginsDir, "FirebaseCppApp-*.dll", SearchOption.TopDirectoryOnly)
			.Select(path => new FileInfo(path))
			.ToList();

		if (appDlls.Count <= 1)
			return 0;

		FileInfo keep = appDlls.FirstOrDefault(info => info.Name.Contains("13_10_0", StringComparison.Ordinal))
		              ?? appDlls.OrderByDescending(info => info.LastWriteTimeUtc).First();
		int removed = 0;

		for (int i = 1; i < appDlls.Count; i++)
		{
			FileInfo stale = appDlls[i];
			string assetPath = ToAssetPath(stale.FullName);
			Debug.LogWarning(
				$"[FirebaseDuplicatePluginCleanup] 중복 Firebase 네이티브 DLL 제거: {stale.Name} (유지: {keep.Name})");

			if (!string.IsNullOrEmpty(assetPath))
				AssetDatabase.DeleteAsset(assetPath);
			else
				File.Delete(stale.FullName);

			string metaPath = stale.FullName + ".meta";
			if (File.Exists(metaPath))
				File.Delete(metaPath);

			removed++;
		}

		if (removed > 0)
			AssetDatabase.Refresh();

		return removed;
	}

	static string ToAssetPath(string fullPath)
	{
		fullPath = fullPath.Replace('\\', '/');
		string dataPath = Application.dataPath.Replace('\\', '/');
		if (!fullPath.StartsWith(dataPath))
			return null;

		return "Assets" + fullPath.Substring(dataPath.Length);
	}
}
