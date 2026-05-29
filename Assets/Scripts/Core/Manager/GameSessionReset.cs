using UnityEngine;

/// <summary>
/// 메인 메뉴(GameStart)로 돌아갈 때 플레이 상태를 초기화합니다.
/// </summary>
public static class GameSessionReset
{
	public static void ResetAll(GameManager game)
	{
		if (game == null)
			return;

		Time.timeScale = 1f;

		ResetWaveAndStage(game);
		ResetPool(game.pool);
		ResetWorldDropsAndMotions();
		ResetInventoriesAndRunes();
		ResetPlayer(game.player);
		ResetGameManagerStats(game);
	}

	static void ResetGameManagerStats(GameManager game)
	{
		game.isLive = false;
		game.gameTime = 0f;
		if (PlayerStats.Instance != null)
			PlayerStats.Instance.ResetRuntimeState();
		game.Health = game.maxHealth;
		game.Kill = 0;
		game.Coin = 0;
	}

	static void ResetWaveAndStage(GameManager game)
	{
		WaveManager wave = Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
		if (wave != null)
			wave.ResetForMainMenu();

		StageManager stage = Object.FindFirstObjectByType<StageManager>(FindObjectsInactive.Include);
		if (stage != null)
			stage.ResetToFirstStage();
	}

	static void ResetPool(PoolManager pool)
	{
		if (pool == null)
			return;

		pool.ReturnAllActiveToPool();
	}

	static void ResetWorldDropsAndMotions()
	{
		foreach (Motion motion in Object.FindObjectsByType<Motion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (motion != null)
				Object.Destroy(motion.gameObject);
		}

		foreach (DroppedCoin coin in Object.FindObjectsByType<DroppedCoin>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (coin != null)
				coin.gameObject.SetActive(false);
		}

		foreach (DroppedChest chest in Object.FindObjectsByType<DroppedChest>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (chest != null)
				chest.gameObject.SetActive(false);
		}

		foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (enemy != null)
				enemy.gameObject.SetActive(false);
		}

		foreach (BossBase boss in Object.FindObjectsByType<BossBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (boss != null)
				boss.gameObject.SetActive(false);
		}
	}

	static void ResetInventoriesAndRunes()
	{
		WeaponInventory weapon = Object.FindFirstObjectByType<WeaponInventory>(FindObjectsInactive.Include);
		if (weapon != null)
			weapon.Clear();

		if (AccessoryInventory.Instance != null)
			AccessoryInventory.Instance.Clear();

		PotionInventory potion = Object.FindFirstObjectByType<PotionInventory>(FindObjectsInactive.Include);
		if (potion != null)
			potion.Clear();

		if (RuneManager.instance != null)
			RuneManager.instance.ResetToInitial();
	}

	static void ResetPlayer(Player player)
	{
		if (player == null)
			return;

		player.ResetForMainMenu();

		if (PlayerStats.Instance != null)
			PlayerStats.Instance.ResetRuntimeState();
	}
}
