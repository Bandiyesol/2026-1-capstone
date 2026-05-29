using UnityEngine;

/// <summary>
/// 배경음·효과음 AudioSource에 GameSettings 볼륨을 적용합니다.
/// 씬에 빈 오브젝트를 만들고 BGM/SFX 소스를 연결하세요.
/// </summary>
public class GameAudioSettings : MonoBehaviour
{
	public static GameAudioSettings Instance { get; private set; }

	[SerializeField] AudioSource bgmSource;
	[SerializeField] AudioSource sfxSource;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		ResolveAudioSources();
		GameSettings.EnsureLoaded();
		ApplyVolumes();
	}

	void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}

	void ResolveAudioSources()
	{
		TryResolveFromChildren();
		TryResolveFromScene();
	}

	void TryResolveFromChildren()
	{
		AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);

		foreach (AudioSource source in sources)
		{
			if (source == null)
				continue;

			string name = source.gameObject.name;

			if (bgmSource == null && IsBgmName(name))
				bgmSource = source;

			if (sfxSource == null && IsSfxName(name))
				sfxSource = source;
		}
	}

	void TryResolveFromScene()
	{
		AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);

		foreach (AudioSource source in sources)
		{
			if (source == null)
				continue;

			string name = source.gameObject.name;

			if (bgmSource == null && IsBgmName(name))
				bgmSource = source;

			if (sfxSource == null && IsSfxName(name))
				sfxSource = source;
		}
	}

	public static bool IsBgmName(string objectName)
	{
		if (string.IsNullOrEmpty(objectName))
			return false;

		return objectName.Contains("BGM", System.StringComparison.OrdinalIgnoreCase)
			|| objectName.Contains("Bgm", System.StringComparison.OrdinalIgnoreCase)
			|| objectName.Contains("Background", System.StringComparison.OrdinalIgnoreCase)
			|| objectName.Contains("Music", System.StringComparison.OrdinalIgnoreCase)
			|| objectName.Contains("배경", System.StringComparison.OrdinalIgnoreCase);
	}

	public static bool IsSfxName(string objectName)
	{
		if (string.IsNullOrEmpty(objectName))
			return false;

		return objectName.Contains("SFX", System.StringComparison.OrdinalIgnoreCase)
			|| objectName.Contains("Sfx")
			|| objectName.Contains("Effect");
	}

	public void ApplyVolumes()
	{
		ResolveAudioSources();
		GameSettings.EnsureLoaded();
		ApplyVolumesToSceneSources(bgmSource, sfxSource);
	}

	/// <summary>씬 안 BGM/SFX 이름 AudioSource에 볼륨을 적용합니다 (싱글톤 없어도 동작).</summary>
	public static void ApplyVolumesToSceneSources(AudioSource preferredBgm = null, AudioSource preferredSfx = null)
	{
		GameSettings.EnsureLoaded();

		bool appliedBgm = false;
		bool appliedSfx = false;

		if (preferredBgm != null)
		{
			preferredBgm.volume = GameSettings.BgmVolume;
			appliedBgm = true;
		}

		if (preferredSfx != null)
		{
			preferredSfx.volume = GameSettings.SfxVolume;
			appliedSfx = true;
		}

		AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);

		foreach (AudioSource source in sources)
		{
			if (source == null)
				continue;

			string name = source.gameObject.name;

			if (!appliedBgm && IsBgmName(name))
			{
				source.volume = GameSettings.BgmVolume;
				appliedBgm = true;
			}

			if (!appliedSfx && IsSfxName(name))
			{
				source.volume = GameSettings.SfxVolume;
				appliedSfx = true;
			}
		}

#if UNITY_EDITOR
		if (!appliedBgm)
			Debug.LogWarning("[GameAudioSettings] BGM AudioSource를 찾지 못했습니다. 오브젝트 이름에 BGM 또는 Music을 넣으세요.");
#endif
	}

	/// <summary>런타임에 BGM 소스를 다시 찾습니다 (Inspector 비어 있을 때).</summary>
	public void RefreshSources()
	{
		bgmSource = null;
		sfxSource = null;
		ResolveAudioSources();
		ApplyVolumes();
	}

	/// <summary>효과음 1회 재생 시 GameSettings 볼륨을 반영합니다.</summary>
	public void PlaySfxOneShot(AudioClip clip)
	{
		if (clip == null)
			return;

		ResolveAudioSources();

		if (sfxSource == null)
			return;

		sfxSource.PlayOneShot(clip, GameSettings.SfxVolume);
	}
}
