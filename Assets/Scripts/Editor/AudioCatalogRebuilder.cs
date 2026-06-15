#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>BGM·SFX 카탈로그를 한 번에 빌드하고 동일 RMS 기준으로 볼륨을 맞춥니다.</summary>
public static class AudioCatalogRebuilder
{
	[MenuItem("Tools/Game/Rebuild All Audio Catalogs")]
	public static void RebuildAllFromMenu()
	{
		BgmCatalogBuilder.RebuildCatalog(force: true, applyBalance: false);
		SfxCatalogBuilder.RebuildCatalog(applyBalance: false);
		RebalanceAllCatalogs(saveAssets: true);
	}

	[MenuItem("Tools/Game/Rebalance All Audio Volumes")]
	public static void RebalanceAllFromMenu()
	{
		RebalanceAllCatalogs(saveAssets: true);
	}

	public static void RebalanceAllCatalogs(bool saveAssets)
	{
		BgmCatalog bgm = AssetDatabase.LoadAssetAtPath<BgmCatalog>("Assets/Resources/Data/BgmCatalog.asset");
		SfxCatalog sfx = AssetDatabase.LoadAssetAtPath<SfxCatalog>("Assets/Resources/Data/SfxCatalog.asset");

		if (bgm == null && sfx == null)
		{
			Debug.LogWarning("[AudioCatalogRebuilder] BgmCatalog / SfxCatalog가 없습니다.");
			return;
		}

		var clips = new List<AudioClip>();
		if (bgm != null)
			clips.AddRange(BgmCatalogBuilder.CollectClips(bgm));
		if (sfx != null)
			clips.AddRange(SfxCatalogBuilder.CollectClips(sfx));

		AudioMixSettings mix = AssetDatabase.LoadAssetAtPath<AudioMixSettings>("Assets/Resources/Data/AudioMixSettings.asset")
			?? AudioMixSettings.Load();

		float targetRms = AudioCatalogBalanceUtility.ComputeTargetRms(clips);
		float bgmTargetRms = targetRms * mix.bgmMasterMixScale;
		float sfxTargetRms = targetRms * mix.sfxMasterMixScale;

		if (bgm != null)
			BgmCatalogBuilder.ApplyVolumeBalance(bgm, bgmTargetRms);
		if (sfx != null)
			SfxCatalogBuilder.ApplyVolumeBalance(sfx, sfxTargetRms);

		if (saveAssets)
			AssetDatabase.SaveAssets();

		Debug.Log(
			$"[AudioCatalogRebuilder] 볼륨 보정 — BGM RMS {bgmTargetRms:F4} (x{mix.bgmMasterMixScale:F2}), " +
			$"SFX RMS {sfxTargetRms:F4} (x{mix.sfxMasterMixScale:F2})");
	}
}
#endif
