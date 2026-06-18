using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("# Game Control")]
    public bool isLive;
    public float gameTime;
    public float maxGameTime = 2 * 10f;

    [Header("# Player Info")]
    public float Health;
    public float maxHealth = 100;
    public int Kill;
    public int Coin;

    [Header("# Game Object")]
    public PoolManager pool;
    public Player player;
    public Result uiResult;
    public WeaponSelectUI uiWeaponSelect;
    public RuneSelectUI uiRuneSelect;

    [Header("# 드랍 확률 (ScriptableObject)")]
    [Tooltip("Assets/Resources/Data/ChestDropSettings — 몬스터 처치 시 상자 드랍 확률")]
    public ChestDropSettings chestDropSettings;
    [Tooltip("Assets/Resources/Data/CoinDropSettings — 몬스터 처치 시 코인 드랍 확률")]
    public CoinDropSettings coinDropSettings;
    [Tooltip("Assets/Resources/Data/StageClearSpawnSettings — 보스 처치 후 상점 주인 등장 확률")]
    public StageClearSpawnSettings stageClearSpawnSettings;

    [Header("# Boss briefing (스토리 → 보스 알리미 → 무기·룬)")]
    [Tooltip("비우면 스크립트 기본 문구(BossBriefingDefaults) 사용")]
    public StageBossBriefDatabase bossBriefDatabase;
    [Tooltip("비우면 씬에서 BossAlarmUI 탐색")]
    public BossAlarmUI bossAlarmUI;

    [Tooltip("스테이지 순서대로 보스 프리팹 — HUD 툴팁 초상용")]
    public GameObject[] bossPortraitPrefabs;

    Sprite[] cachedBossPortraits;

    [Header("# Main Menu (비우면 이름으로 탐색)")]
    public GameObject mainMenuRoot;
    public GameObject gameplayHud;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (player == null)
            player = FindFirstObjectByType<Player>(FindObjectsInactive.Include);
        if (pool == null)
            pool = FindFirstObjectByType<PoolManager>(FindObjectsInactive.Include);
        if (uiResult == null)
            uiResult = FindFirstObjectByType<Result>(FindObjectsInactive.Include);
        if (uiWeaponSelect == null)
            uiWeaponSelect = FindFirstObjectByType<WeaponSelectUI>(FindObjectsInactive.Include);
        if (uiRuneSelect == null)
            uiRuneSelect = FindFirstObjectByType<RuneSelectUI>(FindObjectsInactive.Include);
        if (bossAlarmUI == null)
            bossAlarmUI = FindFirstObjectByType<BossAlarmUI>(FindObjectsInactive.Include);

        EnsureBossPortraitPrefabsFromPool();
        RebuildBossPortraitCache();
        ResolveDropSettings();
        RewardSystemBootstrap.EnsureRewardSystem();
        WireGameResultButton();
        RuneLoadoutHudRuntimeSetup.Ensure();
    }

    void ResolveDropSettings()
    {
        if (chestDropSettings == null)
            chestDropSettings = Resources.Load<ChestDropSettings>("Data/ChestDropSettings");
        if (coinDropSettings == null)
            coinDropSettings = Resources.Load<CoinDropSettings>("Data/CoinDropSettings");
        if (stageClearSpawnSettings == null)
            stageClearSpawnSettings = Resources.Load<StageClearSpawnSettings>("Data/StageClearSpawnSettings");

        StageClearSpawnSettings.ClearCache();
    }

    void WireGameResultButton()
    {
        if (uiResult == null)
            return;

        Transform retry = uiResult.transform.Find("ButtonRetry");
        if (retry == null)
            return;

        Button button = retry.GetComponent<Button>();
        if (button == null)
            return;

        UiClickSfxUtility.Rewire(button, OnGameResultContinueToRecord);

        TextMeshProUGUI label = retry.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.text = "기록 보기";
    }

    void EnsureBossPortraitPrefabsFromPool()
    {
        if (pool?.bossPrefabs == null || pool.bossPrefabs.Length == 0)
            return;

        bossPortraitPrefabs = pool.bossPrefabs;
    }

    void RebuildBossPortraitCache()
    {
        if (pool?.bossPrefabs == null || pool.bossPrefabs.Length == 0)
        {
            cachedBossPortraits = null;
            return;
        }

        cachedBossPortraits = new Sprite[pool.bossPrefabs.Length];
        for (int i = 0; i < pool.bossPrefabs.Length; i++)
        {
            cachedBossPortraits[i] = BossBriefPortraitResolver.FromPrefab(pool.bossPrefabs[i]);
            if (cachedBossPortraits[i] == null && pool.bossPrefabs[i] != null)
            {
                Debug.LogWarning(
                    $"[GameManager] 보스 초상 없음 — pool.bossPrefabs[{i}] ({pool.bossPrefabs[i].name}). " +
                    "프리팹에 SpriteRenderer가 있는지 확인하세요.");
            }
            else if (pool.bossPrefabs[i] == null)
            {
                Debug.LogError(
                    $"[GameManager] pool.bossPrefabs[{i}]가 null입니다. " +
                    "Tools → Game → Setup 7-Stage Boss Waves 를 실행하세요.");
            }
        }
    }

    public void SyncBossBriefingPrefabs()
    {
        EnsureBossPortraitPrefabsFromPool();
        if (cachedBossPortraits == null
            || pool?.bossPrefabs == null
            || cachedBossPortraits.Length != pool.bossPrefabs.Length)
        {
            RebuildBossPortraitCache();
        }
    }

    public Sprite GetCachedBossPortrait(int stageIndex)
    {
        if (cachedBossPortraits == null
            || stageIndex < 0
            || stageIndex >= cachedBossPortraits.Length)
        {
            return null;
        }

        return cachedBossPortraits[stageIndex];
    }

    /// <summary>스토리/메뉴 표시 전 무기·룬 선택 UI를 숨깁니다.</summary>
    public void HidePreGameSelectPanels()
    {
        if (uiWeaponSelect != null)
            uiWeaponSelect.gameObject.SetActive(false);
        if (uiRuneSelect != null)
            uiRuneSelect.gameObject.SetActive(false);

        if (bossAlarmUI == null)
            bossAlarmUI = FindFirstObjectByType<BossAlarmUI>(FindObjectsInactive.Include);
        bossAlarmUI?.Hide();
    }

    /// <summary>메인 메뉴 시작 버튼 — 오프닝 스토리 후 게임 진입.</summary>
    public void BeginGameFromMenu()
    {
        HidePreGameSelectPanels();

        MainStoryUI story = FindFirstObjectByType<MainStoryUI>(FindObjectsInactive.Include);
        if (story != null)
        {
            story.ShowThenStartGame();
            return;
        }

        Debug.LogWarning(
            "[GameManager] MainStoryUI가 씬에 없어 스토리를 건너뜁니다. " +
            "Canvas에 MainStoryUI를 추가하고 Panel에 MainStoryPanel을 연결하세요.");
        GameStart();
    }

	public void GameStart()
	{
		defeatHandled = false;
		GameSessionReset.ResetAll(this);
		SyncHealthFromPlayerStats();
		PlayBgmForCurrentStage();

		RefreshBossBriefingForCurrentStage(rerollBoss: true);

		ShowBossAlarmThen(() =>
		{
			RefreshBossBriefingHudTip();
			OpenWeaponOrRuneSelect();
		});
	}

	/// <summary>마법진 진입 — 스테이지 기록 → 다음 스테이지 → 보스 알리미 → 룬(2·3스테이지) 또는 순서만(4+) → 웨이브.</summary>
	public void AdvanceStageViaPortal()
	{
		if (portalAdvanceRunning)
			return;

		StartCoroutine(AdvanceStageViaPortalRoutine());
	}

	bool portalAdvanceRunning;

	IEnumerator AdvanceStageViaPortalRoutine()
	{
		portalAdvanceRunning = true;
		// OnTriggerEnter2D 중 DestroyImmediate 불가 → 다음 프레임에 정리·전환
		yield return null;
		portalAdvanceRunning = false;
		AdvanceStageViaPortalInternal();
	}

	void AdvanceStageViaPortalInternal()
	{
		StageManager stage = StageManager.instance;
		if (stage == null)
			return;

		DismissBlockingRewardOverlays();
		isLive = false;
		FreezePlayerMovement();
		PoolManager.Instance?.ReturnAllActiveMotions();
		PoolManager.Instance?.PurgeAllMotionRuneEffects();
		PoolManager.Instance?.ReturnStageClearGimmicks();
		PoolManager.Instance?.ReturnActiveEnemiesAndBosses();
		PoolManager.Instance?.ReturnActiveFieldDrops();

		WaveManager wave = stage.waveManager != null
			? stage.waveManager
			: FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
		wave?.AbortStageTransition();

		int clearedStageIndex = stage.stageIndex;
		if (GameRunSessionTracker.IsActive)
			GameRunSessionTracker.CommitStage(clearedStageIndex, stageCleared: true);

		bool isFinalStageClear = clearedStageIndex >= stage.TotalStages - 1;
		if (isFinalStageClear)
			GameAudio.PlayStageClear();

		bool moved = stage.NextStage();
		if (!moved)
			return;

		PlayBgmForCurrentStage();

		ShopUI shop = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
		if (shop != null)
			shop.Close();

		if (GameRunSessionTracker.IsActive)
			GameRunSessionTracker.OnStageAdvanced(stage.stageIndex);

		ShowBossAlarmForStageTransition(() =>
		{
			ContinueAfterStagePortal(stage.stageIndex);
		});
	}

	void ContinueAfterStagePortal(int stageIndex)
	{
		if (ShouldOfferRunePickAfterPortal(stageIndex))
		{
			ShowRunePickThenOrder(StartCurrentStageWaves);
			return;
		}

		ShowRuneOrderOnly(StartCurrentStageWaves);
	}

	/// <summary>스테이지 2·3 진입 시에만 추가 룬 1개 (시작 시 1개 + 최대 3슬롯).</summary>
	public static bool ShouldOfferRunePickAfterPortal(int stageIndex)
	{
		if (RuneManager.instance != null && RuneManager.instance.IsFull)
			return false;

		return stageIndex >= 1 && stageIndex <= 2;
	}

	void ShowRunePickThenOrder(System.Action onComplete)
	{
		if (uiRuneSelect == null)
			uiRuneSelect = FindFirstObjectByType<RuneSelectUI>(FindObjectsInactive.Include);

		if (uiRuneSelect == null)
		{
			ResumeThen(onComplete);
			return;
		}

		uiRuneSelect.ShowForStageTransition(onComplete);
	}

	void ShowRuneOrderOnly(System.Action onComplete)
	{
		if (uiRuneSelect == null)
			uiRuneSelect = FindFirstObjectByType<RuneSelectUI>(FindObjectsInactive.Include);

		if (uiRuneSelect == null
		    || RuneManager.instance == null
		    || RuneManager.instance.GetFilledSlotCount() <= 0)
		{
			ResumeThen(onComplete);
			return;
		}

		uiRuneSelect.ShowOrderOnlyForStageTransition(onComplete);
	}

	void ResumeThen(System.Action onComplete)
	{
		ResumeGameplayFromOverlay();
		onComplete?.Invoke();
	}

	void StartCurrentStageWaves()
	{
		ResumeGameplayFromOverlay();
		PlayBgmForCurrentStage();

		WaveManager wave = FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
		if (wave != null)
			wave.StartStage();
	}

	static void PlayBgmForCurrentStage()
	{
		StageManager stage = StageManager.instance;
		if (stage == null)
			return;

		GameAudioSettings.Instance?.PlayStageBgm(stage.stageIndex);
	}

	static void PlayMainMenuBgm()
	{
		GameAudio.PlayMainMenu();
	}

	/// <summary>상자·무기 보상·룬 선택 등 오버레이가 열려 있으면 포털 진입을 막습니다.</summary>
	public bool CanUseStagePortal()
	{
		RewardSelectUI reward = RewardSelectUI.GetOrFind();
		if (reward != null && reward.IsPanelOpen)
			return false;

		if (uiWeaponSelect != null && uiWeaponSelect.IsBlockingFromChest)
			return false;

		if (uiRuneSelect != null && uiRuneSelect.IsPanelOpen)
			return false;

		RuneLoadoutViewUI runeLoadout = FindFirstObjectByType<RuneLoadoutViewUI>(FindObjectsInactive.Include);
		if (runeLoadout != null && runeLoadout.IsOpen)
			return false;

		return true;
	}

	/// <summary>F12 상점 — 런 시작 후(무기·룬 선택 완료)이며 플레이 중일 때만. 이미 열린 상점은 닫기 가능.</summary>
	public bool CanUseShopHotkey()
	{
		if (!GameRunSessionTracker.IsActive)
			return false;

		ShopUI shop = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
		if (shop != null && shop.IsPanelOpen)
			return true;

		return isLive;
	}

	void DismissBlockingRewardOverlays()
	{
		RewardSelectUI reward = RewardSelectUI.GetOrFind();
		if (reward != null && reward.IsPanelOpen)
			reward.ForceCloseWithoutResume();

		if (uiWeaponSelect != null && uiWeaponSelect.IsBlockingFromChest)
			uiWeaponSelect.ForceCloseChestWithoutResume();
	}

	void EnsureGameplayBgm()
	{
		ResolveMainMenuReferences();
		if (mainMenuRoot != null && mainMenuRoot.activeSelf)
			return;

		ShopUI shop = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
		if (shop != null && shop.IsPanelOpen)
			return;

		StageManager stage = StageManager.instance;
		if (stage == null)
			return;

		GameAudio.EnsureStageBgm(stage.stageIndex);
	}

	public void SyncHealthFromPlayerStats()
	{
		if (PlayerStats.Instance == null)
		{
			Health = maxHealth;
			return;
		}

		Health = PlayerStats.Instance.CurrentHP;
		maxHealth = PlayerStats.Instance.MaxHP;
	}

	/// <summary>스테이지 전환 후 보스 알리미 → 웨이브 시작 등 후속 동작.</summary>
	public void ShowBossAlarmForStageTransition(System.Action onContinue)
	{
		RefreshBossBriefingForCurrentStage(rerollBoss: true);
		ShowBossAlarmThen(() =>
		{
			RefreshBossBriefingHudTip();
			onContinue?.Invoke();
		});
	}

	void ShowBossAlarmThen(System.Action onContinue)
	{
		if (bossAlarmUI == null)
			bossAlarmUI = FindFirstObjectByType<BossAlarmUI>(FindObjectsInactive.Include);

		if (bossAlarmUI != null && BossBriefingRuntime.HasBrief)
		{
			bool resumeAfter = isLive;
			isLive = false;
			FreezePlayerMovement();
			bossAlarmUI.Show(() =>
			{
				if (resumeAfter)
					isLive = true;
				onContinue?.Invoke();
			});
			return;
		}

		onContinue?.Invoke();
	}

    void OpenWeaponOrRuneSelect()
    {
        if (RuneManager.instance != null)
            RuneManager.instance.ClearAll();

        if (uiWeaponSelect == null)
            uiWeaponSelect = FindFirstObjectByType<WeaponSelectUI>(FindObjectsInactive.Include);
        if (uiRuneSelect == null)
            uiRuneSelect = FindFirstObjectByType<RuneSelectUI>(FindObjectsInactive.Include);

        if (uiWeaponSelect != null)
        {
            uiWeaponSelect.gameObject.SetActive(true);
            uiWeaponSelect.transform.SetAsLastSibling();
            uiWeaponSelect.Show();
            return;
        }

        if (uiRuneSelect != null)
        {
            uiRuneSelect.gameObject.SetActive(true);
            uiRuneSelect.transform.SetAsLastSibling();
            uiRuneSelect.ShowForGameStart();
            return;
        }

        Debug.LogWarning("[GameManager] WeaponSelectUI / RuneSelectUI를 찾지 못해 바로 플레이를 시작합니다.");
        Resume();
    }

    /// <summary>스테이지 전환 후 HUD 보스 툴팁이 다음 보스를 가리키도록 갱신합니다.</summary>
    public void RefreshBossBriefingForCurrentStage(bool rerollBoss = false)
    {
        SyncBossBriefingPrefabs();

        StageManager stage = FindFirstObjectByType<StageManager>(FindObjectsInactive.Include);
        if (stage == null)
            return;

        WaveManager wave = FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
        if (wave != null)
        {
            if (rerollBoss)
                wave.RerollBossForStage(stage.stageIndex);
            else
                wave.PrepareBossForStage(stage.stageIndex);
        }

        int bossSpawnIndex = wave != null ? wave.SelectedBossSpawnDataIndex : -1;
        ApplyBossBriefing(stage.stageIndex, bossSpawnIndex, wave?.spawner);
    }

    /// <summary>보스가 실제로 스폰된 뒤 알리미·HUD가 해당 보스 정보를 표시하도록 갱신합니다.</summary>
    public void RefreshBossBriefingForBossSpawn(int bossSpawnDataIndex)
    {
        SyncBossBriefingPrefabs();

        StageManager stage = FindFirstObjectByType<StageManager>(FindObjectsInactive.Include);
        if (stage == null)
            return;

        WaveManager wave = FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
        ApplyBossBriefing(stage.stageIndex, bossSpawnDataIndex, wave?.spawner);
    }

    void ApplyBossBriefing(int stageIndex, int bossSpawnDataIndex, Spawner spawner)
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<Spawner>(FindObjectsInactive.Include);

        BossBriefingRuntime.ApplyFromBossSelection(
            stageIndex,
            spawner,
            bossSpawnDataIndex,
            bossBriefDatabase,
            bossPortraitPrefabs);
        RefreshBossBriefingHudTip();
    }

    void RefreshBossBriefingHudTip()
    {
        BossBriefingHudTip tip = FindFirstObjectByType<BossBriefingHudTip>(FindObjectsInactive.Include);
        if (tip != null)
            tip.RefreshVisibility();
    }

    bool defeatHandled;

    public void GameOver()
    {
        if (defeatHandled)
            return;

        defeatHandled = true;
        GameAudio.PlayDeath();
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        isLive = false;
        AccessoryEffect.instance?.ClearCombatTransientEffects();
        RuneEffect.InvalidateGameplaySession();
        PoolManager.Instance?.ReturnAllActiveMotions();
        PoolManager.Instance?.PurgeAllMotionRuneEffects();
        yield return new WaitForSeconds(0.5f);

        if (uiResult != null)
        {
            uiResult.gameObject.SetActive(true);
            uiResult.ResetTitles();
            uiResult.ShowDefeatInterstitial();
        }

        Stop();
    }

    public void GameVictory()
    {
        StartCoroutine(GameVictoryRoutine());
    }

    IEnumerator GameVictoryRoutine()
    {
        isLive = false;
        yield return new WaitForSeconds(0.5f);

        EndingSequenceController ending = GetComponent<EndingSequenceController>();
        if (ending == null)
            ending = FindFirstObjectByType<EndingSequenceController>(FindObjectsInactive.Include);

        if (ending != null)
        {
            Vector3 origin = ResolveEndingFlashOrigin();
            ending.PlayFromBoss(origin);
            yield break;
        }

        ShowGameRecordAfterRun(cleared: true);
    }

    static Vector3 ResolveEndingFlashOrigin()
    {
        if (BossBase.LastDeathWorldPosition is Vector3 bossPosition)
            return bossPosition;

        if (BossBase.LastEnemyDeathWorldPosition is Vector3 enemyPosition)
            return enemyPosition;

        if (instance != null && instance.player != null)
            return instance.player.transform.position;

        return Vector3.zero;
    }

    public void GameRetry()
    {
        Time.timeScale = 1f;
        RuneEffect.InvalidateGameplaySession();
        if (player != null)
            player.ResetForMainMenu();

        SceneManager.LoadScene("ProtoType_LTG");
    }

    /// <summary>패배 화면(Title Over)에서 기록창으로 이동합니다.</summary>
    public void OnGameResultContinueToRecord()
    {
        if (uiResult != null)
            uiResult.gameObject.SetActive(false);

        ShowGameRecordAfterRun(cleared: false);
    }

    /// <summary>플레이 기록을 저장한 뒤 기록창을 엽니다.</summary>
    public void ShowGameRecordAfterRun(bool cleared)
    {
        _ = SaveRunRecordAndShowAsync(cleared);
    }

    async System.Threading.Tasks.Task SaveRunRecordAndShowAsync(bool cleared)
    {
        await UserAccountDisplay.RefreshAsync();

        GameRunRecord record = GameRunSnapshotBuilder.Build(cleared);
        GameRunRecordStore.Save(record);
        await GameRunLeaderboard.SubmitClearAsync(record);
        await GameRunLeaderboard.RefreshGlobalAsync();
        RefreshMainMenuLeaderboard();

        GameRecordUI ui = GameRecordUIBootstrap.Ensure();
        if (ui == null)
        {
            ReturnToMainMenu();
            return;
        }

        ui.Show(record.id, ReturnToMainMenu, singleRunOnly: true);
    }

    /// <summary>메인 메뉴에서 기록만 열 때 — 확인 시 메뉴 유지.</summary>
    public void ShowGameRecordFromMenu()
    {
        GameRecordUI ui = GameRecordUIBootstrap.Ensure();
        if (ui == null)
            return;

        ui.Show(null, () =>
        {
            ui.Hide();
            Time.timeScale = 1f;
            RefreshMainMenuLeaderboard();
        });
    }

    /// <summary>랭킹·기록에서 특정 판 상세(기록 UI)를 엽니다.</summary>
    public void ShowGameRecordDetail(string recordId)
    {
        if (string.IsNullOrEmpty(recordId))
            return;

        GameRecordUI ui = GameRecordUIBootstrap.Ensure();
        if (ui == null)
            return;

        ui.Show(recordId, () =>
        {
            ui.Hide();
            Time.timeScale = 1f;
            RefreshMainMenuLeaderboard();
        }, singleRunOnly: true);
    }

    public static async System.Threading.Tasks.Task RefreshMainMenuLeaderboardAsync()
    {
        await GameRunLeaderboard.RefreshGlobalAsync();
        MainMenuLeaderboardView view = Object.FindFirstObjectByType<MainMenuLeaderboardView>(FindObjectsInactive.Include);
        view?.Refresh();
    }

    public static void RefreshMainMenuLeaderboard()
    {
        MainMenuLeaderboardView view = Object.FindFirstObjectByType<MainMenuLeaderboardView>(FindObjectsInactive.Include);
        view?.Refresh();
    }

    /// <summary>게임 시작 화면(GameStart)으로 돌아갑니다.</summary>
    public void ReturnToMainMenu()
    {
        GameSessionReset.ResetAll(this);

        defeatHandled = false;

        ResolveMainMenuReferences();

        if (uiWeaponSelect != null)
            uiWeaponSelect.gameObject.SetActive(false);
        if (uiRuneSelect != null)
            uiRuneSelect.gameObject.SetActive(false);
        if (uiResult != null)
            uiResult.gameObject.SetActive(false);

        GameRecordUI recordUi = FindFirstObjectByType<GameRecordUI>(FindObjectsInactive.Include);
        recordUi?.Hide();

        if (bossAlarmUI == null)
            bossAlarmUI = FindFirstObjectByType<BossAlarmUI>(FindObjectsInactive.Include);
        bossAlarmUI?.Hide();
        BossBriefingRuntime.Clear();
        RefreshBossBriefingHudTip();

        CloseOverlayPanels();
        DismissBlockingRewardOverlays();

        EndingStoryUI endingStory = FindFirstObjectByType<EndingStoryUI>(FindObjectsInactive.Include);
        endingStory?.ForceClose();

        HideGameplayHud();
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);

        PlayMainMenuBgm();
        RefreshMainMenuLeaderboard();
    }

    void ResolveMainMenuReferences()
    {
        if (mainMenuRoot == null)
        {
            GameObject found = GameObject.Find("GameStart");
            if (found != null)
                mainMenuRoot = found;
        }

        if (gameplayHud == null)
        {
            GameObject found = GameObject.Find("HUD");
            if (found != null)
                gameplayHud = found;
        }
    }

    public static void CloseOverlayPanels()
    {
        StatusUI status = FindFirstObjectByType<StatusUI>(FindObjectsInactive.Include);
        if (status != null)
            status.Close();

        InventoryUI inventory = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if (inventory != null)
            inventory.Close();

        ShopUI shop = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        if (shop != null)
            shop.Close();

		SettingsUI settings = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
		if (settings != null)
			settings.CloseWithoutResumingGameplay();

		RuneLoadoutViewUI runeLoadout = FindFirstObjectByType<RuneLoadoutViewUI>(FindObjectsInactive.Include);
		if (runeLoadout != null)
			runeLoadout.Close();
	}

    void Update()
    {
        if (!isLive)
            return;

        gameTime += Time.deltaTime;
    }

    public void Stop()
    {
        isLive = false;
        AccessoryEffect.instance?.ClearCombatTransientEffects();
        Time.timeScale = 0;
    }

    /// <summary>설정/인벤토리 등 UI만 멈출 때 — timeScale 은 1 유지 (드롭다운·UI 애니메이션 동작).</summary>
    public void PauseOverlay()
    {
        isLive = false;
    }

    public void ResumeOverlay()
    {
        isLive = true;
    }

    public static void FreezePlayerMovement()
    {
        if (instance?.player == null)
            return;

        Player player = instance.player;
        player.inputVec = Vector2.zero;

        if (player.TryGetComponent(out Rigidbody2D rigid))
            rigid.linearVelocity = Vector2.zero;
    }

    /// <summary>상점·인벤·스테이터스 등 — 월드·기록·웨이브는 그대로 둡니다.</summary>
    public void PauseForOverlayPanel()
    {
        if (!isLive)
            return;

        PauseOverlay();
        FreezePlayerMovement();
    }

    /// <summary>오버레이 패널을 닫은 뒤 게임만 재개합니다.</summary>
    public void ResumeGameplayFromOverlay()
    {
        isLive = true;
        Time.timeScale = 1f;
        ShowGameplayHud();
        EnsureGameplayBgm();
    }

    /// <summary>룬 선택 후 첫 플레이 진입 — 기록 추적·웨이브 시작.</summary>
    public void BeginGameplaySession()
    {
        isLive = true;
        Time.timeScale = 1f;
        ShowGameplayHud();
        PlayBgmForCurrentStage();

        if (!GameRunSessionTracker.IsActive)
            GameRunSessionTracker.BeginRun();

        SyncBossBriefingPrefabs();

        WaveManager wave = FindFirstObjectByType<WaveManager>();
        if (wave != null)
            wave.Begin();
    }

    public void ShowGameplayHud()
    {
        ResolveMainMenuReferences();

        if (gameplayHud != null)
            gameplayHud.SetActive(true);

        RefreshBossBriefingHudTip();
    }

    public void HideGameplayHud()
    {
        ResolveMainMenuReferences();

        if (gameplayHud != null)
            gameplayHud.SetActive(false);
    }

    /// <summary>룬 선택 직후 등 — <see cref="BeginGameplaySession"/> 과 동일.</summary>
    public void Resume()
    {
        BeginGameplaySession();
    }

	public void AddCoin(int amount)
	{
		if (amount <= 0)
			return;

		Coin += amount;
		GameRunSessionTracker.AddCoinsEarned(amount);
	}

	public bool TrySpendCoin(int amount)
	{
		if (amount <= 0)
			return true;

		if (Coin < amount)
			return false;

		Coin -= amount;
		GameRunSessionTracker.AddCoinsSpent(amount);
		return true;
	}
}
