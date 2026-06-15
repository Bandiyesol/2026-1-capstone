using UnityEngine;

/// <summary>멀티 파트 보스 코어 공통 — 마법진 스폰 + WaveManager 클리어 보고.</summary>
public static class BossStageClearUtility
{
	public static PoolManager ResolvePool()
	{
		if (PoolManager.Instance != null)
			return PoolManager.Instance;

		if (GameManager.instance != null && GameManager.instance.pool != null)
			return GameManager.instance.pool;

		return Object.FindFirstObjectByType<PoolManager>(FindObjectsInactive.Include);
	}

	public static WaveManager ResolveWaveManager(WaveManager preferred)
	{
		if (preferred != null)
			return preferred;

		if (StageManager.instance != null && StageManager.instance.waveManager != null)
			return StageManager.instance.waveManager;

		return Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
	}

	public static void CompleteStage(Vector2 deathPosition, int portalGimmickIndex, WaveManager waveManager)
	{
		if (ResolvePool() == null)
		{
			Debug.LogWarning("[BossStageClear] PoolManager를 찾지 못해 마법진을 스폰하지 못했습니다.");
			return;
		}

		WaveManager resolvedWave = ResolveWaveManager(waveManager);
		resolvedWave?.NotifyBossStageCleared();

		StageClearSpawnUtility.SpawnPortalAndShopkeeper(deathPosition, portalGimmickIndex);
	}
}
