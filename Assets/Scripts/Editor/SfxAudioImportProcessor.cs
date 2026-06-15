#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>SFX를 2D·즉시 재생에 맞게 Import 설정합니다.</summary>
public class SfxAudioImportProcessor : AssetPostprocessor
{
	const string SfxRoot = "Assets/Arts/Audio/SFX/";

	void OnPreprocessAudio()
	{
		if (!assetPath.StartsWith(SfxRoot) || assetPath.Contains("/_Misplaced/"))
			return;

		ApplyLowLatencySettings((AudioImporter)assetImporter);
	}

	public static void ApplyLowLatencySettings(AudioImporter importer)
	{
		if (importer == null)
			return;

		importer.forceToMono = true;
		importer.loadInBackground = false;
		importer.ambisonic = false;

		AudioImporterSampleSettings settings = importer.defaultSampleSettings;
		settings.loadType = AudioClipLoadType.DecompressOnLoad;
		settings.compressionFormat = AudioCompressionFormat.PCM;
		settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
		settings.preloadAudioData = true;
		importer.defaultSampleSettings = settings;

		ApplyPlatformSampleSettings(importer, "Standalone");
		ApplyPlatformSampleSettings(importer, "WebGL");
	}

	static void ApplyPlatformSampleSettings(AudioImporter importer, string platform)
	{
		if (!importer.ContainsSampleSettingsOverride(platform))
			return;

		AudioImporterSampleSettings settings = importer.GetOverrideSampleSettings(platform);
		settings.loadType = AudioClipLoadType.DecompressOnLoad;
		settings.compressionFormat = AudioCompressionFormat.PCM;
		settings.preloadAudioData = true;
		importer.SetOverrideSampleSettings(platform, settings);
	}

	[MenuItem("Tools/Game/Reimport Sfx For Low Latency")]
	public static void ReimportAllFromMenu()
	{
		if (!AssetDatabase.IsValidFolder("Assets/Arts/Audio/SFX"))
		{
			Debug.LogWarning("[SfxAudioImportProcessor] SFX 폴더가 없습니다.");
			return;
		}

		string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Arts/Audio/SFX" });
		int count = 0;

		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (path.Contains("/_Misplaced/"))
				continue;

			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
			count++;
		}

		Debug.Log($"[SfxAudioImportProcessor] SFX {count}개 재임포트 — Decompress On Load + PCM + Preload");
	}
}
#endif
