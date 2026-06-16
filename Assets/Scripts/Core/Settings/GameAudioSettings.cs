using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배경음·효과음 AudioSource에 GameSettings 볼륨을 적용합니다.
/// 씬에 빈 오브젝트를 만들고 BGM/SFX 소스를 연결하세요.
/// </summary>
[DefaultExecutionOrder(500)]
public class GameAudioSettings : MonoBehaviour
{
	public static GameAudioSettings Instance { get; private set; }

	[SerializeField] AudioSource bgmSource;
	[SerializeField] AudioSource sfxSource;
	[SerializeField] BgmCatalog bgmCatalog;
	[SerializeField] SfxCatalog sfxCatalog;
	[SerializeField] AudioMixSettings mixSettings;

	const int SfxPoolSize = 8;
	const int SfxLoopSlotCount = 6;
	const int BgmSourcePriority = 0;
	const int SfxSourcePriority = 256;
	const float BgmWatchInterval = 0.5f;

	AudioSource[] sfxPool;
	int sfxPoolIndex;
	AudioSource[] sfxLoopPool;
	readonly Dictionary<SfxId, AudioSource> activeLoopById = new Dictionary<SfxId, AudioSource>();
	bool sfxLoopSourcesConfigured;
	Dictionary<SfxId, SfxCatalog.Entry> sfxEntryCache;
	bool sfxSourcesConfigured;
	float nextBgmWatchTime;

	enum BgmMode
	{
		None,
		MainMenu,
		Stage,
		Shop,
		StageClear,
		Death,
	}

	BgmMode currentBgmMode = BgmMode.None;
	int currentStageIndex = -1;
	float currentTrackVolumeScale = 1f;
	bool shopBgmActive;
	BgmMode bgmBeforeShop = BgmMode.None;
	int stageIndexBeforeShop = -1;
	readonly Dictionary<int, float> enemyHitSfxTimes = new Dictionary<int, float>();
	float lastPlayerHitSfxTime = -999f;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
		ResolveAudioSources();
		EnsureMixSettings();
		EnsureBgmCatalog();
		EnsureSfxCatalog();
		ConfigureSfxSources();
		ConfigureSfxLoopSources();
		ConfigureBgmSource();
		BuildSfxCache();
		PreloadSfxClips();
		PreloadBgmStingers();
		GameSettings.EnsureLoaded();
		ApplyVolumes();
	}

	void Start()
	{
		ConfigureBgmSource();
		PlayMainMenuBgm();
		UiClickSfxUtility.WireAllInScene();
		UiTypingSfxUtility.WireAllInScene();
	}

	void Update()
	{
		if (Time.unscaledTime < nextBgmWatchTime)
			return;

		nextBgmWatchTime = Time.unscaledTime + BgmWatchInterval;
		TryRestoreLoopBgmIfNeeded();
	}

	void OnDestroy()
	{
		StopAllSfxLoops();
		if (Instance == this)
			Instance = null;
	}

#if UNITY_EDITOR
	void OnValidate()
	{
		if (Application.isPlaying && Instance == this)
			ApplyVolumes();
	}
#endif

	static AudioMixSettings ResolveMixSettings()
	{
		if (Instance != null && Instance.mixSettings != null)
			return Instance.mixSettings;

		return AudioMixSettings.Load();
	}

	void EnsureMixSettings()
	{
		if (mixSettings == null)
			mixSettings = AudioMixSettings.Load();
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

			if (sfxSource == null && source != bgmSource && IsSfxName(name))
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

			if (sfxSource == null && source != bgmSource && IsSfxName(name))
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

		if (sfxSource != null)
		{
			sfxSource.volume = GameSettings.SfxVolume;
			if (sfxPool != null)
			{
				foreach (AudioSource source in sfxPool)
				{
					if (source != null)
						source.volume = GameSettings.SfxVolume;
				}
			}

			RefreshLoopVolumes();
		}

		ApplyBgmVolume();
	}

	void ApplyBgmVolume()
	{
		if (bgmSource == null)
			return;

		bgmSource.volume = GetEffectiveBgmVolume();
	}

	public float GetTrackVolumeScale() => currentTrackVolumeScale;

	public static float GetEffectiveBgmVolume()
	{
		AudioMixSettings mix = ResolveMixSettings();
		float scale = Instance != null ? Instance.currentTrackVolumeScale : 1f;
		return GameSettings.BgmVolume * scale * mix.bgmMasterMixScale;
	}

	public static float GetEffectiveSfxVolume(float trackScale = 1f)
	{
		AudioMixSettings mix = ResolveMixSettings();
		return GameSettings.SfxVolume * trackScale * mix.sfxMasterMixScale;
	}

	/// <summary>씬 안 BGM/SFX 이름 AudioSource에 볼륨을 적용합니다 (싱글톤 없어도 동작).</summary>
	public static void ApplyVolumesToSceneSources(AudioSource preferredBgm = null, AudioSource preferredSfx = null)
	{
		GameSettings.EnsureLoaded();

		bool appliedBgm = false;
		bool appliedSfx = false;

		if (preferredBgm != null)
		{
			preferredBgm.volume = GetEffectiveBgmVolume();
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
				source.volume = GetEffectiveBgmVolume();
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
		sfxPool = null;
		sfxPoolIndex = 0;
		sfxLoopPool = null;
		sfxLoopSourcesConfigured = false;
		activeLoopById.Clear();
		sfxSourcesConfigured = false;
		ResolveAudioSources();
		ConfigureSfxSources();
		ConfigureSfxLoopSources();
		ConfigureBgmSource();
		ApplyVolumes();
	}

	void ConfigureSfxSources()
	{
		if (sfxSourcesConfigured)
			return;

		if (sfxSource == null)
			return;

		if (sfxSource == bgmSource)
		{
			Debug.LogWarning("[GameAudioSettings] SFX와 BGM AudioSource가 같습니다. BGM 전용 소스와 SFX 전용 소스를 분리하세요.");
			return;
		}

		AudioSource[] existing = sfxSource.GetComponents<AudioSource>();
		sfxPool = new AudioSource[Mathf.Max(SfxPoolSize, existing.Length)];
		int count = 0;

		for (int i = 0; i < existing.Length && count < SfxPoolSize; i++)
		{
			if (existing[i] == bgmSource)
				continue;

			ApplySfxSourceSettings(existing[i]);
			sfxPool[count++] = existing[i];
		}

		while (count < SfxPoolSize)
		{
			AudioSource extra = sfxSource.gameObject.AddComponent<AudioSource>();
			ApplySfxSourceSettings(extra);
			extra.volume = sfxSource.volume;
			sfxPool[count++] = extra;
		}

		sfxPoolIndex = 0;
		sfxSourcesConfigured = true;
	}

	void ConfigureSfxLoopSources()
	{
		if (sfxLoopSourcesConfigured)
			return;

		if (sfxSource == null)
			return;

		if (sfxSource == bgmSource)
			return;

		sfxLoopPool = new AudioSource[SfxLoopSlotCount];
		for (int i = 0; i < SfxLoopSlotCount; i++)
		{
			AudioSource loopSource = sfxSource.gameObject.AddComponent<AudioSource>();
			ApplySfxSourceSettings(loopSource);
			loopSource.loop = true;
			loopSource.volume = sfxSource.volume;
			sfxLoopPool[i] = loopSource;
		}

		sfxLoopSourcesConfigured = true;
	}

	AudioSource GetFreeLoopSource()
	{
		if (sfxLoopPool == null || sfxLoopPool.Length == 0)
			return null;

		foreach (AudioSource source in sfxLoopPool)
		{
			if (source == null || source.isPlaying)
				continue;

			return source;
		}

		return sfxLoopPool[0];
	}

	public void PlaySfxLoop(SfxId id)
	{
		if (activeLoopById.ContainsKey(id))
			return;

		if (sfxEntryCache == null || sfxEntryCache.Count == 0)
			BuildSfxCache();

		if (sfxEntryCache == null || !sfxEntryCache.TryGetValue(id, out SfxCatalog.Entry entry) || entry.clip == null)
		{
			EnsureSfxCatalog();
			BuildSfxCache();
			if (sfxCatalog == null || !sfxCatalog.TryGet(id, out entry) || entry.clip == null)
				return;
		}

		ConfigureSfxLoopSources();
		AudioSource source = GetFreeLoopSource();
		if (source == null)
			return;

		source.clip = entry.clip;
		source.volume = GetEffectiveSfxVolume(entry.volumeScale);
		source.loop = true;
		source.Play();
		activeLoopById[id] = source;
	}

	public void PlaySfxOnce(SfxId id, float volumeMultiplier = 1f)
	{
		StopSfxLoop(id);

		if (sfxEntryCache == null || sfxEntryCache.Count == 0)
			BuildSfxCache();

		if (sfxEntryCache == null || !sfxEntryCache.TryGetValue(id, out SfxCatalog.Entry entry) || entry.clip == null)
		{
			EnsureSfxCatalog();
			BuildSfxCache();
			if (sfxCatalog == null || !sfxCatalog.TryGet(id, out entry) || entry.clip == null)
				return;
		}

		ConfigureSfxLoopSources();
		AudioSource source = GetFreeLoopSource();
		if (source == null && sfxLoopPool != null && sfxLoopPool.Length > 0)
			source = sfxLoopPool[0];
		if (source == null)
			return;

		float scale = entry.volumeScale * Mathf.Max(0.05f, volumeMultiplier);
		source.Stop();
		source.loop = false;
		source.clip = entry.clip;
		GameSettings.EnsureLoaded();
		source.volume = GameSettings.SfxVolume;
		EnsureMixSettings();
		source.PlayOneShot(entry.clip, scale * mixSettings.sfxMasterMixScale);
	}

	public void StopSfxLoop(SfxId id)
	{
		if (!activeLoopById.TryGetValue(id, out AudioSource source))
			return;

		if (source != null)
		{
			source.Stop();
			source.clip = null;
		}

		activeLoopById.Remove(id);
	}

	public void StopAllSfxLoops()
	{
		foreach (KeyValuePair<SfxId, AudioSource> pair in activeLoopById)
		{
			if (pair.Value == null)
				continue;

			pair.Value.Stop();
			pair.Value.clip = null;
		}

		activeLoopById.Clear();
	}

	void RefreshLoopVolumes()
	{
		if (activeLoopById.Count == 0)
			return;

		if (sfxEntryCache == null || sfxEntryCache.Count == 0)
			BuildSfxCache();

		foreach (KeyValuePair<SfxId, AudioSource> pair in activeLoopById)
		{
			if (pair.Value == null)
				continue;

			if (sfxEntryCache != null && sfxEntryCache.TryGetValue(pair.Key, out SfxCatalog.Entry entry))
				pair.Value.volume = GetEffectiveSfxVolume(entry.volumeScale);
		}
	}

	void ApplySfxSourceSettings(AudioSource source)
	{
		if (source == null)
			return;

		source.playOnAwake = false;
		source.loop = false;
		source.spatialBlend = 0f;
		source.priority = SfxSourcePriority;
		source.dopplerLevel = 0f;
		source.reverbZoneMix = 0f;
	}

	void ConfigureBgmSource()
	{
		ResolveAudioSources();
		if (bgmSource == null)
			return;

		bgmSource.playOnAwake = false;
		bgmSource.spatialBlend = 0f;
		bgmSource.priority = BgmSourcePriority;
		bgmSource.dopplerLevel = 0f;
		bgmSource.reverbZoneMix = 0f;
	}

	bool IsLoopingBgmMode(BgmMode mode)
	{
		return mode == BgmMode.MainMenu || mode == BgmMode.Stage || mode == BgmMode.Shop;
	}

	void TryRestoreLoopBgmIfNeeded()
	{
		if (!IsLoopingBgmMode(currentBgmMode))
			return;

		ResolveAudioSources();
		if (bgmSource == null || bgmSource.clip == null || bgmSource.isPlaying)
			return;

		bgmSource.loop = true;
		ApplyBgmVolume();
		bgmSource.Play();
	}

	void BuildSfxCache()
	{
		EnsureSfxCatalog();
		sfxEntryCache = new Dictionary<SfxId, SfxCatalog.Entry>();
		if (sfxCatalog?.entries == null)
			return;

		foreach (SfxCatalog.Entry entry in sfxCatalog.entries)
		{
			if (!sfxEntryCache.ContainsKey(entry.id))
				sfxEntryCache.Add(entry.id, entry);
		}
	}

	void PreloadBgmStingers()
	{
		EnsureBgmCatalog();
		if (bgmCatalog == null)
			return;

		TryPreloadClip(bgmCatalog.stageClearClip);
		TryPreloadClip(bgmCatalog.deathClip);
	}

	static void TryPreloadClip(AudioClip clip)
	{
		if (clip == null || clip.loadState == AudioDataLoadState.Loaded)
			return;

		clip.LoadAudioData();
	}

	void PreloadSfxClips()
	{
		if (sfxCatalog?.entries == null)
			return;

		foreach (SfxCatalog.Entry entry in sfxCatalog.entries)
			TryPreloadClip(entry.clip);
	}

	AudioSource GetNextSfxSource()
	{
		if (sfxPool == null || sfxPool.Length == 0)
			return sfxSource;

		for (int i = 0; i < sfxPool.Length; i++)
		{
			int index = (sfxPoolIndex + i) % sfxPool.Length;
			AudioSource candidate = sfxPool[index];
			if (candidate == null || candidate.isPlaying)
				continue;

			sfxPoolIndex = (index + 1) % sfxPool.Length;
			return candidate;
		}

		AudioSource source = sfxPool[sfxPoolIndex];
		sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Length;
		return source;
	}

	void PlaySfxClip(AudioClip clip, float volumeScale = 1f)
	{
		if (clip == null)
			return;

		if (sfxSource == null)
			ResolveAudioSources();

		ConfigureSfxSources();
		ConfigureSfxLoopSources();

		AudioSource source = GetNextSfxSource();
		if (source == null)
			return;

		EnsureMixSettings();
		// source.volume = SfxVolume — PlayOneShot 배율에는 믹스·클립 보정만 적용
		source.PlayOneShot(clip, volumeScale * mixSettings.sfxMasterMixScale);
	}

	void PlayBgmStinger(AudioClip clip, BgmMode mode, float volumeScale)
	{
		if (clip == null)
			return;

		ResolveAudioSources();
		if (bgmSource == null)
			return;

		shopBgmActive = false;
		currentBgmMode = mode;
		currentStageIndex = -1;
		currentTrackVolumeScale = volumeScale;

		bgmSource.loop = false;
		bgmSource.playOnAwake = false;
		bgmSource.spatialBlend = 0f;
		bgmSource.clip = clip;
		ApplyBgmVolume();
		bgmSource.Play();
	}

	public void PlaySfx(SfxId id, float volumeMultiplier = 1f)
	{
		if (sfxEntryCache == null || sfxEntryCache.Count == 0)
			BuildSfxCache();

		if (sfxEntryCache == null || !sfxEntryCache.TryGetValue(id, out SfxCatalog.Entry entry) || entry.clip == null)
		{
			EnsureSfxCatalog();
			BuildSfxCache();
			if (sfxCatalog == null || !sfxCatalog.TryGet(id, out entry) || entry.clip == null)
				return;
		}

		float scale = entry.volumeScale * Mathf.Max(0.05f, volumeMultiplier);
		PlaySfxClip(entry.clip, scale);
	}

	/// <summary>효과음 1회 재생 시 GameSettings 볼륨을 반영합니다.</summary>
	public void PlaySfxOneShot(AudioClip clip)
	{
		PlaySfxClip(clip, 1f);
	}

	public void PlayWeaponSfx(string weaponType)
	{
		PlaySfx(SfxCatalog.WeaponTypeToSfxId(weaponType));
	}

	public void PlayPlayerHitSfx()
	{
		EnsureMixSettings();
		float now = Time.time;
		if (now - lastPlayerHitSfxTime < mixSettings.playerHitSfxCooldown)
			return;

		lastPlayerHitSfxTime = now;
		PlaySfx(SfxId.PlayerHit);
	}

	public void PlayEnemyHitSfx(GameObject target)
	{
		if (target == null)
			return;

		if (target.GetComponent<Player>() != null || target.GetComponentInParent<Player>() != null)
			return;

		int id = target.GetInstanceID();
		float now = Time.time;
		EnsureMixSettings();
		if (enemyHitSfxTimes.TryGetValue(id, out float lastTime) && now - lastTime < mixSettings.enemyHitSfxCooldown)
			return;

		enemyHitSfxTimes[id] = now;
		PlaySfx(SfxId.EnemyHit);
	}

	/// <summary>최종 클리어(7스테이지 마법진 진입) 연출음 — BGM 채널 1회 재생.</summary>
	public void PlayStageClearStinger()
	{
		EnsureBgmCatalog();
		if (bgmCatalog == null || bgmCatalog.stageClearClip == null)
			return;

		PlayBgmStinger(bgmCatalog.stageClearClip, BgmMode.StageClear, bgmCatalog.GetStageClearVolumeScale());
	}

	/// <summary>사망 짧은 연출음 — BGM 채널 1회 재생.</summary>
	public void PlayDeathStinger()
	{
		EnsureBgmCatalog();
		if (bgmCatalog == null || bgmCatalog.deathClip == null)
			return;

		PlayBgmStinger(bgmCatalog.deathClip, BgmMode.Death, bgmCatalog.GetDeathVolumeScale());
	}

	public void PlayMainMenuBgm()
	{
		EnsureBgmCatalog();
		PlayBgmClip(bgmCatalog != null ? bgmCatalog.mainMenuClip : null, BgmMode.MainMenu, -1);
	}

	/// <summary>스테이지/상점 BGM 재생 중이어도 메인 메뉴 BGM으로 강제 전환.</summary>
	public void TransitionToMainMenuBgm()
	{
		shopBgmActive = false;
		EnsureBgmCatalog();
		ResolveAudioSources();

		if (bgmSource != null)
		{
			bgmSource.Stop();
			currentBgmMode = BgmMode.None;
			currentStageIndex = -1;
		}

		PlayMainMenuBgm();
	}

	/// <summary>스테이지 BGM이 멈춘 경우 같은 스테이지 트랙을 다시 재생합니다.</summary>
	public void EnsureStageBgmPlaying(int stageIndex)
	{
		if (shopBgmActive && currentBgmMode == BgmMode.Shop)
			return;

		EnsureBgmCatalog();
		ResolveAudioSources();
		if (bgmSource == null)
			return;

		if ((currentBgmMode == BgmMode.Death || currentBgmMode == BgmMode.StageClear) && bgmSource.isPlaying)
			return;

		AudioClip expected = bgmCatalog != null ? bgmCatalog.GetStageClip(stageIndex) : null;
		if (expected == null)
			return;

		if (currentBgmMode == BgmMode.Stage
		    && currentStageIndex == stageIndex
		    && bgmSource.clip == expected
		    && bgmSource.isPlaying)
			return;

		PlayStageBgm(stageIndex);
	}

	public void PlayStageBgm(int stageIndex)
	{
		EnsureBgmCatalog();
		AudioClip clip = bgmCatalog != null ? bgmCatalog.GetStageClip(stageIndex) : null;
		PlayBgmClip(clip, BgmMode.Stage, stageIndex);
	}

	/// <summary>상점 열릴 때 — 스테이지 BGM을 멈추고 상점 BGM으로 교체 (단일 소스, 겹침 없음).</summary>
	public void EnterShopBgm()
	{
		EnsureBgmCatalog();
		if (bgmCatalog == null || bgmCatalog.shopClip == null)
			return;

		if (!shopBgmActive)
		{
			bgmBeforeShop = currentBgmMode;
			stageIndexBeforeShop = currentStageIndex;
		}

		shopBgmActive = true;
		PlayBgmClip(bgmCatalog.shopClip, BgmMode.Shop, -1);
	}

	/// <summary>상점 닫을 때 — 이전 BGM(보통 스테이지)으로 복귀.</summary>
	public void ExitShopBgm()
	{
		if (!shopBgmActive)
			return;

		shopBgmActive = false;

		if (currentBgmMode != BgmMode.Shop)
			return;

		switch (bgmBeforeShop)
		{
			case BgmMode.MainMenu:
				PlayMainMenuBgm();
				break;
			case BgmMode.Stage:
				PlayStageBgm(stageIndexBeforeShop);
				break;
			default:
				StageManager stage = StageManager.instance;
				if (stage != null)
					PlayStageBgm(stage.stageIndex);
				break;
		}
	}

	void EnsureBgmCatalog()
	{
		if (bgmCatalog == null)
			bgmCatalog = BgmCatalog.Load();
	}

	void EnsureSfxCatalog()
	{
		if (sfxCatalog == null)
			sfxCatalog = SfxCatalog.Load();
	}

	void PlayBgmClip(AudioClip clip, BgmMode mode, int stageIndex)
	{
		if (clip == null)
		{
			Debug.LogWarning($"[GameAudioSettings] BGM 클립이 없습니다 (mode={mode}, stage={stageIndex}). Tools → Rebuild Bgm Catalog 실행.");
			return;
		}

		ResolveAudioSources();
		if (bgmSource == null)
			return;

		ConfigureBgmSource();

		if (mode != BgmMode.Shop)
			shopBgmActive = false;

		if (currentBgmMode == mode
		    && currentStageIndex == stageIndex
		    && bgmSource.clip == clip
		    && bgmSource.isPlaying)
			return;

		currentBgmMode = mode;
		currentStageIndex = stageIndex;
		currentTrackVolumeScale = ResolveVolumeScale(mode, stageIndex);

		bgmSource.loop = true;
		bgmSource.playOnAwake = false;
		bgmSource.spatialBlend = 0f;
		bgmSource.clip = clip;
		ApplyBgmVolume();
		bgmSource.Play();
	}

	float ResolveVolumeScale(BgmMode mode, int stageIndex)
	{
		switch (mode)
		{
			case BgmMode.MainMenu:
				return bgmCatalog.GetMainMenuVolumeScale();
			case BgmMode.Shop:
				return bgmCatalog.GetShopVolumeScale();
			case BgmMode.Stage:
				return bgmCatalog.GetStageVolumeScale(stageIndex);
			case BgmMode.StageClear:
				return bgmCatalog.GetStageClearVolumeScale();
			case BgmMode.Death:
				return bgmCatalog.GetDeathVolumeScale();
			default:
				return 1f;
		}
	}
}
