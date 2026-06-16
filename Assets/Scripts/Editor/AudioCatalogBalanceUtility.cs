#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>BGM·SFX 공통 RMS 측정 및 볼륨 보정.</summary>
public static class AudioCatalogBalanceUtility
{
	public const float MinRms = 0.0005f;
	public const float MinScale = 0.25f;
	public const float MaxScale = 3f;

	public static float MeasureClipRms(AudioClip clip)
	{
		if (clip == null || clip.samples <= 0)
			return 0f;

		int channels = Mathf.Max(1, clip.channels);
		int totalValues = clip.samples * channels;
		var data = new float[totalValues];

		if (!clip.GetData(data, 0))
		{
			Debug.LogWarning(
				$"[AudioCatalogBalanceUtility] RMS 측정 실패 — {clip.name}. Import Load Type을 Decompress On Load로 바꿔 보세요.");
			return 0f;
		}

		const int maxSamplesToScan = 44100 * 120 * 2;
		int stride = Mathf.Max(1, totalValues / maxSamplesToScan);

		double sumSquares = 0d;
		int count = 0;
		for (int i = 0; i < totalValues; i += stride)
		{
			float sample = data[i];
			sumSquares += sample * sample;
			count++;
		}

		return count > 0 ? Mathf.Sqrt((float)(sumSquares / count)) : 0f;
	}

	public static float ComputeTargetRms(IEnumerable<AudioClip> clips)
	{
		double sumRms = 0d;
		int count = 0;

		foreach (AudioClip clip in clips)
		{
			if (clip == null)
				continue;

			float rms = MeasureClipRms(clip);
			if (rms <= MinRms)
				continue;

			sumRms += rms;
			count++;
		}

		return count > 0 ? (float)(sumRms / count) : 1f;
	}

	public static float ScaleForClip(AudioClip clip, float targetRms)
	{
		if (clip == null || targetRms <= 0f)
			return 1f;

		float rms = MeasureClipRms(clip);
		if (rms <= MinRms)
			return 1f;

		return Mathf.Clamp(targetRms / rms, MinScale, MaxScale);
	}

	/// <summary>
	/// 신의 방패 체감 크기(rms×volumeScale)보다 큰 SFX만 줄입니다. 작은 소리는 키우지 않습니다.
	/// </summary>
	public static float ScaleForClipAttenuateToReference(
		AudioClip clip,
		float currentVolumeScale,
		AudioClip referenceClip,
		float referenceVolumeScale)
	{
		if (clip == null || referenceClip == null || referenceVolumeScale <= 0f)
			return currentVolumeScale;

		float refRms = MeasureClipRms(referenceClip);
		float clipRms = MeasureClipRms(clip);
		if (refRms <= MinRms || clipRms <= MinRms)
			return currentVolumeScale;

		float targetEffective = refRms * referenceVolumeScale;
		float currentEffective = clipRms * currentVolumeScale;
		if (currentEffective <= targetEffective)
			return currentVolumeScale;

		return Mathf.Clamp(targetEffective / clipRms, MinScale, MaxScale);
	}

	/// <summary>기준 클립의 현재 체감 크기(rms×volumeScale)에 다른 클립을 맞춥니다. (양방향 — 레거시)</summary>
	public static float ScaleForClipRelativeToReference(AudioClip clip, AudioClip referenceClip, float referenceVolumeScale)
	{
		if (clip == null || referenceClip == null || referenceVolumeScale <= 0f)
			return 1f;

		float refRms = MeasureClipRms(referenceClip);
		if (refRms <= MinRms)
			return 1f;

		float targetEffective = refRms * referenceVolumeScale;
		return ScaleForClip(clip, targetEffective);
	}
}
#endif
