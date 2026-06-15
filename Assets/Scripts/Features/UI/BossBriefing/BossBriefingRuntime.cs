using UnityEngine;

/// <summary>현재 스테이지 보스 브리핑 — 보스 알리미 패널과 HUD 툴팁이 동일 데이터를 읽습니다.</summary>
public static class BossBriefingRuntime
{
	public static string DisplayName { get; private set; }
	public static string TraitsSummary { get; private set; }
	public static string PatternsHint { get; private set; }
	public static string TraitsHudShort { get; private set; }
	public static string PatternsHudShort { get; private set; }
	public static Sprite Portrait { get; private set; }
	public static int StageIndex { get; private set; }
	public static int SelectedBossSpawnDataIndex { get; private set; } = -1;
	public static bool HasBrief { get; private set; }

	static readonly string[] committedBossNames = new string[GameRunSessionTracker.MaxStages];
	static GameObject activeBossPrefab;
	static GameObject[] portraitPrefabOverride;

	public static void ClearSession()
	{
		for (int i = 0; i < committedBossNames.Length; i++)
			committedBossNames[i] = null;
	}

	public static void Clear()
	{
		HasBrief = false;
		StageIndex = 0;
		SelectedBossSpawnDataIndex = -1;
		activeBossPrefab = null;
		portraitPrefabOverride = null;
		DisplayName = string.Empty;
		TraitsSummary = string.Empty;
		PatternsHint = string.Empty;
		TraitsHudShort = string.Empty;
		PatternsHudShort = string.Empty;
		Portrait = null;
	}

	/// <summary>기록 저장용 — 이미 확정된 보스 이름을 우선 반환합니다.</summary>
	public static string GetBossDisplayName(int stageIndex)
	{
		if (stageIndex >= 0 && stageIndex < committedBossNames.Length
		    && !string.IsNullOrEmpty(committedBossNames[stageIndex]))
		{
			return committedBossNames[stageIndex];
		}

		if (stageIndex == StageIndex && HasBrief && !string.IsNullOrEmpty(DisplayName))
			return DisplayName;

		GameManager game = GameManager.instance;
		if (game != null && game.bossBriefDatabase != null
		    && game.bossBriefDatabase.TryGetBriefing(stageIndex, out BossBriefProfile profile))
		{
			if (profile != null && !string.IsNullOrEmpty(profile.displayName))
				return profile.displayName;
		}

		if (BossBriefingDefaults.TryGet(stageIndex, out BossBriefingDefaults.Entry entry))
			return entry.displayName;

		return "—";
	}

	public static void ApplyStage(int stageIndex, StageBossBriefDatabase database, GameObject[] portraitPrefabFallback = null)
	{
		ApplyFromBossSelection(stageIndex, null, -1, database, portraitPrefabFallback);
	}

	public static void ApplyFromBossSelection(
		int stageIndex,
		Spawner spawner,
		int bossSpawnDataIndex,
		StageBossBriefDatabase database,
		GameObject[] portraitPrefabFallback = null)
	{
		Clear();
		StageIndex = stageIndex;
		bossSpawnDataIndex = BossSpawnDataIndexUtility.Normalize(bossSpawnDataIndex);
		SelectedBossSpawnDataIndex = bossSpawnDataIndex;
		portraitPrefabOverride = portraitPrefabFallback;
		activeBossPrefab = ResolveBossPrefab(spawner, bossSpawnDataIndex, portraitPrefabFallback);

		if (activeBossPrefab != null && BossBriefingCatalog.TryGet(activeBossPrefab, out BossBriefingCatalog.Entry catalogEntry))
		{
			ApplyFromCatalogEntry(catalogEntry, activeBossPrefab);
			Debug.Log(
				$"[BossBriefing] 스테이지 {stageIndex + 1} — 카탈로그 '{DisplayName}' " +
				$"(spawnData[{bossSpawnDataIndex}], prefab={activeBossPrefab.name})");
		}
		else if (database != null && database.TryGetBriefing(stageIndex, out BossBriefProfile profile) && profile != null)
		{
			Debug.LogWarning(
				$"[BossBriefing] 스테이지 {stageIndex + 1} — spawnData[{bossSpawnDataIndex}] 프리팹을 못 찾아 " +
				$"StageBossBriefDatabase 폴백 '{profile.displayName}' 사용");
			ApplyFromProfile(profile, portraitPrefabFallback);
		}
		else if (BossBriefingDefaults.TryGet(stageIndex, out BossBriefingDefaults.Entry defaultsEntry))
		{
			Debug.LogWarning(
				$"[BossBriefing] 스테이지 {stageIndex + 1} — spawnData[{bossSpawnDataIndex}] 프리팹을 못 찾아 " +
				$"기본값 '{defaultsEntry.displayName}' 사용 (랜덤 보스와 다를 수 있음)");
			ApplyFromDefaultsEntry(defaultsEntry, portraitPrefabFallback);
		}

		if (HasBrief && stageIndex >= 0 && stageIndex < committedBossNames.Length)
			committedBossNames[stageIndex] = DisplayName;

		RefreshPortrait();
	}

	public static Sprite GetPortrait()
	{
		if (Portrait == null && HasBrief)
			RefreshPortrait();

		return Portrait;
	}

	static void RefreshPortrait()
	{
		if (!HasBrief)
		{
			Portrait = null;
			return;
		}

		if (activeBossPrefab != null)
		{
			Portrait = BossBriefPortraitResolver.FromPrefab(activeBossPrefab);
			if (Portrait != null)
				return;
		}

		if (SelectedBossSpawnDataIndex >= 0 && GameManager.instance != null)
		{
			Spawner spawner = UnityEngine.Object.FindFirstObjectByType<Spawner>(UnityEngine.FindObjectsInactive.Include);
			GameObject prefab = ResolveBossPrefab(spawner, SelectedBossSpawnDataIndex, portraitPrefabOverride);
			if (prefab != null)
			{
				Portrait = BossBriefPortraitResolver.FromPrefab(prefab);
				if (Portrait != null)
					return;
			}
		}

		Portrait = BossBriefPortraitResolver.Resolve(StageIndex, portraitPrefabOverride);

		if (Portrait == null)
		{
			Debug.LogWarning(
				$"[BossBriefing] 스테이지 {StageIndex} 보스 초상을 찾지 못했습니다. " +
				"PoolManager → Boss Prefabs 또는 보스 프리팹 SpriteRenderer를 확인하세요.");
		}
	}

	static GameObject ResolveBossPrefab(Spawner spawner, int spawnDataIndex, GameObject[] portraitPrefabFallback)
	{
		spawnDataIndex = BossSpawnDataIndexUtility.Normalize(spawnDataIndex);
		if (spawnDataIndex < 0)
			spawnDataIndex = BossSpawnDataIndexUtility.ResolveFromWaveManager(spawner);

		if (spawner != null && BossSpawnDataIndexUtility.IsPlausibleSpawnIndex(spawnDataIndex, spawner.spawnData.Length))
		{
			SpawnData data = spawner.GetSpawnData(spawnDataIndex);
			if (data.isBoss)
			{
				GameManager game = GameManager.instance;
				GameObject[] bossPrefabs = portraitPrefabFallback;
				if (bossPrefabs == null && game != null)
					bossPrefabs = game.bossPortraitPrefabs;
				if (bossPrefabs == null && game?.pool != null)
					bossPrefabs = game.pool.bossPrefabs;

				if (bossPrefabs != null && data.prefabIndex >= 0 && data.prefabIndex < bossPrefabs.Length)
					return bossPrefabs[data.prefabIndex];
			}
		}

		// stageIndex를 bossPrefabs[stageIndex]로 쓰면 랜덤 보스와 무관하게 첫 보스(펌킨킹)로 고정되므로 사용하지 않음
		return null;
	}

	static void ApplyFromCatalogEntry(BossBriefingCatalog.Entry entry, GameObject bossPrefab)
	{
		DisplayName = entry.displayName;
		PatternsHint = entry.patterns;
		PatternsHudShort = string.IsNullOrWhiteSpace(entry.patternsHud) ? entry.patterns : entry.patternsHud;
		activeBossPrefab = bossPrefab;
		ApplyStatsFromBossData(
			bossPrefab,
			entry.traits,
			entry.traitsHud);
		HasBrief = true;
	}

	static void ApplyFromProfile(BossBriefProfile profile, GameObject[] portraitPrefabFallback)
	{
		DisplayName = profile.displayName;
		PatternsHint = profile.patternsHint;
		PatternsHudShort = string.IsNullOrWhiteSpace(profile.patternsHudShort)
			? profile.patternsHint
			: profile.patternsHudShort;
		Portrait = profile.portrait;
		ApplyStatsFromBossData(
			activeBossPrefab,
			profile.traitsSummary,
			profile.traitsHudShort);
		HasBrief = true;
	}

	static void ApplyFromDefaultsEntry(BossBriefingDefaults.Entry e, GameObject[] portraitPrefabFallback)
	{
		DisplayName = e.displayName;
		PatternsHint = e.patterns;
		PatternsHudShort = string.IsNullOrWhiteSpace(e.patternsHud) ? e.patterns : e.patternsHud;
		ApplyStatsFromBossData(
			activeBossPrefab,
			e.traits,
			e.traitsHud);
		HasBrief = true;
	}

	static void ApplyStatsFromBossData(GameObject bossPrefab, string traitsDescription, string traitsHudFallback)
	{
		BossData data = BossDataDisplayUtility.ResolveFromPrefab(bossPrefab);
		string statsFull = BossDataDisplayUtility.FormatStatsLine(data, includeMelee: true);
		string statsHud = BossDataDisplayUtility.FormatStatsLine(data, includeMelee: false);

		TraitsSummary = BossDataDisplayUtility.CombineStatsAndDescription(statsFull, traitsDescription);

		if (!string.IsNullOrWhiteSpace(statsHud))
			TraitsHudShort = statsHud;
		else if (!string.IsNullOrWhiteSpace(traitsHudFallback))
			TraitsHudShort = traitsHudFallback;
		else
			TraitsHudShort = traitsDescription;
	}
}
