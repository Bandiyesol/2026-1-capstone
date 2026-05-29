using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화면·해상도·볼륨 설정 저장(PlayerPrefs) 및 적용.
/// </summary>
public static class GameSettings
{
	const string KeyScreenMode = "Settings_ScreenMode";
	const string KeyResolutionIndex = "Settings_ResolutionIndex";
	const string KeyBgmVolume = "Settings_BgmVolume";
	const string KeySfxVolume = "Settings_SfxVolume";

	static readonly string[] ScreenModeLabels =
	{
		"창 모드",
		"전체 화면",
		"테두리 없는 전체 화면",
	};

	static ResolutionOption[] resolutionOptions;
	static bool loaded;

	public static int ScreenModeIndex { get; private set; }
	public static int ResolutionIndex { get; private set; }
	public static float BgmVolume { get; private set; } = 1f;
	public static float SfxVolume { get; private set; } = 1f;

	public static IReadOnlyList<string> ScreenModeOptions => ScreenModeLabels;

	public static void EnsureLoaded()
	{
		if (loaded)
			return;

		Load();
	}

	public static void Load()
	{
		ScreenModeIndex = PlayerPrefs.GetInt(KeyScreenMode, GetDefaultScreenModeIndex());
		ResolutionIndex = PlayerPrefs.GetInt(KeyResolutionIndex, GetDefaultResolutionIndex());
		BgmVolume = PlayerPrefs.GetFloat(KeyBgmVolume, 1f);
		SfxVolume = PlayerPrefs.GetFloat(KeySfxVolume, 1f);

		ClampValues();
		loaded = true;
	}

	public static void Save()
	{
		PlayerPrefs.SetInt(KeyScreenMode, ScreenModeIndex);
		PlayerPrefs.SetInt(KeyResolutionIndex, ResolutionIndex);
		PlayerPrefs.SetFloat(KeyBgmVolume, BgmVolume);
		PlayerPrefs.SetFloat(KeySfxVolume, SfxVolume);
		PlayerPrefs.Save();
	}

	public static void SetScreenModeIndex(int index)
	{
		EnsureLoaded();
		ScreenModeIndex = Mathf.Clamp(index, 0, ScreenModeLabels.Length - 1);
		Save();
		ApplyDisplay();
	}

	public static void SetResolutionIndex(int index)
	{
		EnsureLoaded();
		ResolutionIndex = Mathf.Clamp(index, 0, Mathf.Max(0, GetResolutionOptions().Length - 1));
		Save();
		ApplyDisplay();
	}

	public static void SetBgmVolume(float volume)
	{
		EnsureLoaded();
		BgmVolume = Mathf.Clamp01(volume);
		Save();
		ApplyAudio();
	}

	public static void SetSfxVolume(float volume)
	{
		EnsureLoaded();
		SfxVolume = Mathf.Clamp01(volume);
		Save();
		ApplyAudio();
	}

	public static void ApplyAll()
	{
		EnsureLoaded();
		ApplyDisplay();
		ApplyAudio();
	}

	public static void ApplyDisplay()
	{
		EnsureLoaded();
		RebuildResolutionOptionsIfNeeded();

		FullScreenMode mode = ToFullScreenMode(ScreenModeIndex);
		ResolutionOption option = GetCurrentResolutionOption();
		Screen.fullScreenMode = mode;
		Screen.SetResolution(option.Width, option.Height, mode);

#if UNITY_EDITOR
		Debug.Log($"[GameSettings] 화면 적용: {ScreenModeLabels[ScreenModeIndex]}, {option.Label} (에디터 Game 뷰 크기는 안 바뀔 수 있음 — 빌드에서 확인)");
#endif
	}

	public static void ApplyAudio()
	{
		EnsureLoaded();

		if (GameAudioSettings.Instance != null)
			GameAudioSettings.Instance.ApplyVolumes();
		else
			GameAudioSettings.ApplyVolumesToSceneSources();
	}

	public static ResolutionOption[] GetResolutionOptions()
	{
		RebuildResolutionOptionsIfNeeded();
		return resolutionOptions ?? Array.Empty<ResolutionOption>();
	}

	public static ResolutionOption GetCurrentResolutionOption()
	{
		ResolutionOption[] options = GetResolutionOptions();
		if (options.Length == 0)
			return new ResolutionOption(Screen.width, Screen.height, $"{Screen.width} x {Screen.height}");

		int index = Mathf.Clamp(ResolutionIndex, 0, options.Length - 1);
		return options[index];
	}

	static void RebuildResolutionOptionsIfNeeded()
	{
		if (resolutionOptions != null && resolutionOptions.Length > 0)
			return;

		Resolution[] raw = Screen.resolutions;
		if (raw == null || raw.Length == 0)
		{
			resolutionOptions = new[]
			{
				new ResolutionOption(Screen.width, Screen.height, $"{Screen.width} x {Screen.height}"),
			};
			return;
		}

		var unique = new Dictionary<string, ResolutionOption>();
		foreach (Resolution resolution in raw)
		{
			string label = $"{resolution.width} x {resolution.height}";
			if (!unique.ContainsKey(label))
				unique[label] = new ResolutionOption(resolution.width, resolution.height, label);
		}

		var list = new List<ResolutionOption>(unique.Values);
		list.Sort((a, b) =>
		{
			int byWidth = a.Width.CompareTo(b.Width);
			return byWidth != 0 ? byWidth : a.Height.CompareTo(b.Height);
		});

		resolutionOptions = list.ToArray();
	}

	static int GetDefaultScreenModeIndex()
	{
		return Screen.fullScreenMode switch
		{
			FullScreenMode.ExclusiveFullScreen => 1,
			FullScreenMode.FullScreenWindow => 2,
			_ => 0,
		};
	}

	static int GetDefaultResolutionIndex()
	{
		RebuildResolutionOptionsIfNeeded();
		string current = $"{Screen.width} x {Screen.height}";

		for (int i = 0; i < resolutionOptions.Length; i++)
		{
			if (resolutionOptions[i].Label == current)
				return i;
		}

		return Mathf.Max(0, resolutionOptions.Length - 1);
	}

	static void ClampValues()
	{
		ScreenModeIndex = Mathf.Clamp(ScreenModeIndex, 0, ScreenModeLabels.Length - 1);
		BgmVolume = Mathf.Clamp01(BgmVolume);
		SfxVolume = Mathf.Clamp01(SfxVolume);

		RebuildResolutionOptionsIfNeeded();
		ResolutionIndex = Mathf.Clamp(ResolutionIndex, 0, Mathf.Max(0, resolutionOptions.Length - 1));
	}

	static FullScreenMode ToFullScreenMode(int index)
	{
		return index switch
		{
			1 => FullScreenMode.ExclusiveFullScreen,
			2 => FullScreenMode.FullScreenWindow,
			_ => FullScreenMode.Windowed,
		};
	}

	public readonly struct ResolutionOption
	{
		public int Width { get; }
		public int Height { get; }
		public string Label { get; }

		public ResolutionOption(int width, int height, string label)
		{
			Width = width;
			Height = height;
			Label = label;
		}
	}
}
