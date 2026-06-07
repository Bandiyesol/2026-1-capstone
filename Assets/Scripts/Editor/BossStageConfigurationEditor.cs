#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 7스테이지 보스 웨이브·풀·스폰·클리어 조건을 설정합니다. BossData 스탯은 변경하지 않습니다.
/// </summary>
public static class BossStageConfigurationEditor
{
	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";

	static readonly string[] BossPrefabPaths =
	{
		"Assets/Prefabs/Characters/Boss/HeavenEyeBoss.prefab",
		"Assets/Prefabs/Characters/Boss/UndergroundDrillerBoss.prefab",
		"Assets/Prefabs/Characters/Boss/StormDragonBoss.prefab",
		"Assets/Prefabs/Characters/Boss/LavaEarthDragon.prefab",
		"Assets/Prefabs/Characters/Boss/IceGiant.prefab",
		"Assets/Prefabs/Characters/Boss/DesertGuardianBoss.prefab",
		"Assets/Prefabs/Characters/Boss/AbyssalPredator.prefab",
	};

	[MenuItem("Tools/Game/Setup 7-Stage Boss Waves")]
	public static void SetupFromMenu()
	{
		if (Application.isPlaying)
		{
			EditorUtility.DisplayDialog("보스 스테이지 설정", "플레이 모드에서는 실행할 수 없습니다.", "확인");
			return;
		}

		Scene scene = EditorSceneManager.GetActiveScene();
		if (scene.path != ScenePath)
			scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

		if (!ApplyConfiguration())
		{
			EditorUtility.DisplayDialog("보스 스테이지 설정", "StageManager / Spawner / PoolManager를 찾지 못했습니다.", "확인");
			return;
		}

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		EditorUtility.DisplayDialog(
			"보스 스테이지 설정",
			"7스테이지 보스 웨이브·풀·스폰·클리어(7스테이지) 설정을 적용했습니다.",
			"확인");
	}

	public static bool ApplyConfiguration()
	{
		StageManager stageManager = Object.FindFirstObjectByType<StageManager>(FindObjectsInactive.Include);
		Spawner spawner = Object.FindFirstObjectByType<Spawner>(FindObjectsInactive.Include);
		PoolManager pool = Object.FindFirstObjectByType<PoolManager>(FindObjectsInactive.Include);

		if (stageManager == null || spawner == null || pool == null)
			return false;

		ApplyPoolBossPrefabs(pool);
		ApplySpawnerBossEntries(spawner);
		ApplyStageWaves(stageManager);
		stageManager.endingAfterStageNumber = 7;

		GameManager gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
		if (gameManager != null)
		{
			gameManager.bossPortraitPrefabs = pool.bossPrefabs;
			EditorUtility.SetDirty(gameManager);
		}

		EditorUtility.SetDirty(stageManager);
		EditorUtility.SetDirty(spawner);
		EditorUtility.SetDirty(pool);
		LogBossConfiguration(pool, spawner, stageManager);
		return true;
	}

	static void LogBossConfiguration(PoolManager pool, Spawner spawner, StageManager stageManager)
	{
		for (int i = 0; i < BossPrefabPaths.Length; i++)
		{
			GameObject prefab = pool.bossPrefabs != null && i < pool.bossPrefabs.Length
				? pool.bossPrefabs[i]
				: null;
			int spawnDataIndex = 2 + i;
			bool hasSpawnEntry = spawner.spawnData != null && spawnDataIndex < spawner.spawnData.Length;
			int prefabIndex = hasSpawnEntry ? spawner.spawnData[spawnDataIndex].prefabIndex : -1;

			if (prefab == null)
			{
				Debug.LogError(
					$"[BossStageSetup] pool.bossPrefabs[{i}] ({BossPrefabPaths[i]}) 가 비어 있습니다.");
			}

			if (!hasSpawnEntry || !spawner.spawnData[spawnDataIndex].isBoss || prefabIndex != i)
			{
				Debug.LogError(
					$"[BossStageSetup] spawner.spawnData[{spawnDataIndex}] 보스 설정 불일치 " +
                    $"(expected prefabIndex={i}).");
			}
		}
	}

	static void ApplyPoolBossPrefabs(PoolManager pool)
	{
		var prefabs = new GameObject[BossPrefabPaths.Length];
		for (int i = 0; i < BossPrefabPaths.Length; i++)
			prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPaths[i]);

		pool.bossPrefabs = prefabs;
	}

	static void ApplySpawnerBossEntries(Spawner spawner)
	{
		int enemyCount = 2;
		var entries = new SpawnData[enemyCount + BossPrefabPaths.Length];

		for (int i = 0; i < enemyCount; i++)
		{
			if (spawner.spawnData != null && i < spawner.spawnData.Length)
				entries[i] = spawner.spawnData[i];
			else
				entries[i] = new SpawnData { isBoss = false, spawnTime = i == 0 ? 0.7f : 0.2f, prefabIndex = i };
		}

		for (int stage = 0; stage < BossPrefabPaths.Length; stage++)
		{
			entries[enemyCount + stage] = new SpawnData
			{
				isBoss = true,
				spawnTime = 0.2f,
				prefabIndex = stage,
			};
		}

		spawner.spawnData = entries;
	}

	static void ApplyStageWaves(StageManager stageManager)
	{
		if (stageManager.stageDatas == null || stageManager.stageDatas.Length < BossPrefabPaths.Length)
			return;

		const int lastWaveIndex = 4;
		const int enemySpawnIndex = 0;

		for (int stage = 0; stage < BossPrefabPaths.Length; stage++)
		{
			StageData stageData = stageManager.stageDatas[stage];
			if (stageData?.waves == null || stageData.waves.Length <= lastWaveIndex)
				continue;

			int bossSpawnDataIndex = 2 + stage;

			for (int wave = 0; wave < stageData.waves.Length; wave++)
			{
				WaveData waveData = stageData.waves[wave];
				if (wave == lastWaveIndex)
				{
					waveData.isBossWave = true;
					waveData.bossSpawnIndexes = new[] { bossSpawnDataIndex };
					waveData.enemies = new EnemySpawnInfo[0];
				}
				else if (wave == 0)
				{
					waveData.isBossWave = false;
					waveData.bossSpawnIndexes = null;
					waveData.enemies = new[] { new EnemySpawnInfo { spawnDataIndex = enemySpawnIndex, spawnCount = 1 } };
				}
				else if (wave == 1)
				{
					waveData.isBossWave = false;
					waveData.enemies = new[] { new EnemySpawnInfo { spawnDataIndex = enemySpawnIndex, spawnCount = 5 } };
				}
				else if (wave == 2)
				{
					waveData.isBossWave = false;
					waveData.enemies = new[] { new EnemySpawnInfo { spawnDataIndex = enemySpawnIndex, spawnCount = 10 } };
				}
				else
				{
					waveData.isBossWave = false;
					waveData.enemies = new[]
					{
						new EnemySpawnInfo { spawnDataIndex = enemySpawnIndex, spawnCount = 5 },
						new EnemySpawnInfo { spawnDataIndex = 1, spawnCount = 5 },
					};
				}
			}
		}
	}
}
#endif
