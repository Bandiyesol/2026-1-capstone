#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 7바이옴 스테이지 웨이브·몬스터·보스(랜덤) 풀·스폰 설정.
/// </summary>
public static class BossStageConfigurationEditor
{
	const string ScenePath = "Assets/Scenes/ProtoType_LTG.unity";
	const string EnemyPrefabRoot = "Assets/Prefabs/Characters/Enemy";
	const string BossPrefabRoot = "Assets/Prefabs/Characters/Boss";
	const int EnemyCount = 36;
	const int WaveCount = 5;
	const int BossWaveIndex = 4;

	static readonly string[] ExtraBossPoolPrefabs =
	{
		$"{BossPrefabRoot}/LavaTyrano.prefab",
	};

	static readonly string[] BossMinionPrefabPaths =
	{
		$"{EnemyPrefabRoot}/Enemy_PumpkinKing.prefab",
		$"{EnemyPrefabRoot}/Enemy_CaveRex.prefab",
		$"{EnemyPrefabRoot}/Enemy_DeepSeaMutant.prefab",
		$"{EnemyPrefabRoot}/Enemy_VolcanoPumpkin.prefab",
		$"{EnemyPrefabRoot}/Enemy_LavaEarthDragon.prefab",
		$"{EnemyPrefabRoot}/Enemy_FrostWolfBoss.prefab",
		$"{EnemyPrefabRoot}/Enemy_IceGiantBoss_A.prefab",
		$"{EnemyPrefabRoot}/Enemy_IceGiantBoss_B.prefab",
		$"{EnemyPrefabRoot}/Enemy_IceGiantBoss_C.prefab",
		$"{EnemyPrefabRoot}/Enemy_IceGiantBoss_D.prefab",
		$"{EnemyPrefabRoot}/Enemy_IceGiantBoss_E.prefab",
		$"{EnemyPrefabRoot}/Enemy_UndeadGuard.prefab",
		$"{EnemyPrefabRoot}/Enemy_ImmortalUndeadBoss_A.prefab",
		$"{EnemyPrefabRoot}/Enemy_ImmortalUndeadBoss_B.prefab",
		$"{EnemyPrefabRoot}/Enemy_ImmortalUndeadBoss_C.prefab",
	};

	struct BiomeStageConfig
	{
		public int enemyStart;
		public int enemyEnd;
		public int[] eliteEnemyNumbers;
		public string[] bossPrefabPaths;
	}

	static readonly BiomeStageConfig[] Biomes =
	{
		new BiomeStageConfig
		{
			enemyStart = 1, enemyEnd = 2,
			bossPrefabPaths = new[]
			{
				$"{BossPrefabRoot}/PumpkinKing.prefab",
				$"{BossPrefabRoot}/HeavenEyeBoss.prefab",
			},
		},
		new BiomeStageConfig
		{
			enemyStart = 3, enemyEnd = 4,
			bossPrefabPaths = new[]
			{
				$"{BossPrefabRoot}/UndergroundDrillerBoss.prefab",
				$"{BossPrefabRoot}/CaveRex.prefab",
			},
		},
		new BiomeStageConfig
		{
			enemyStart = 5, enemyEnd = 7,
			bossPrefabPaths = new[]
			{
				$"{BossPrefabRoot}/DeepSeaMutant.prefab",
				$"{BossPrefabRoot}/DrownedSpiritBoss.prefab",
				$"{BossPrefabRoot}/StormDragonBoss.prefab",
			},
		},
		new BiomeStageConfig
		{
			enemyStart = 8, enemyEnd = 10, eliteEnemyNumbers = new[] { 10 },
			bossPrefabPaths = new[]
			{
				$"{BossPrefabRoot}/LavaTyrano Core.prefab",
				$"{BossPrefabRoot}/VolcanoPumpkin Core.prefab",
				$"{BossPrefabRoot}/LavaEarthDragon.prefab",
			},
		},
		new BiomeStageConfig
		{
			enemyStart = 11, enemyEnd = 14, eliteEnemyNumbers = new[] { 14 },
			bossPrefabPaths = new[]
			{
				$"{BossPrefabRoot}/FrostWolfBoss Core.prefab",
				$"{BossPrefabRoot}/IceGiant.prefab",
			},
		},
		new BiomeStageConfig
		{
			enemyStart = 15, enemyEnd = 21, eliteEnemyNumbers = new[] { 20, 21 },
			bossPrefabPaths = new[]
			{
				$"{BossPrefabRoot}/DesertGuardianBoss.prefab",
				$"{BossPrefabRoot}/ImmortalUndeadBoss.prefab",
			},
		},
		new BiomeStageConfig
		{
			enemyStart = 22, enemyEnd = 36, eliteEnemyNumbers = new[] { 32, 33, 34, 35, 36 },
			bossPrefabPaths = new[]
			{
				$"{BossPrefabRoot}/AbyssalPredator.prefab",
				$"{BossPrefabRoot}/VoidCalamityBoss.prefab",
			},
		},
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
			"7바이옴 몬스터·보스(랜덤) 웨이브 설정을 적용했습니다.\n" +
			"스테이지 시작 시 보스 알리미는 선택된 보스 정보를 표시합니다.",
			"확인");
	}

	public static void ApplyFromCommandLine()
	{
		Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
		if (!ApplyConfiguration())
		{
			Debug.LogError("[BossStageSetup] ApplyConfiguration failed.");
			EditorApplication.Exit(1);
			return;
		}

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
		Debug.Log("[BossStageSetup] Scene saved.");
		EditorApplication.Exit(0);
	}

	public static bool ApplyConfiguration()
	{
		StageManager stageManager = Object.FindFirstObjectByType<StageManager>(FindObjectsInactive.Include);
		Spawner spawner = Object.FindFirstObjectByType<Spawner>(FindObjectsInactive.Include);
		PoolManager pool = Object.FindFirstObjectByType<PoolManager>(FindObjectsInactive.Include);

		if (stageManager == null || spawner == null || pool == null)
			return false;

		ApplyPoolEnemyPrefabs(pool);
		var bossPathToPoolIndex = ApplyPoolBossPrefabs(pool);
		var bossPathToSpawnIndex = ApplySpawnerEntries(spawner, bossPathToPoolIndex);
		ApplyStageWaves(stageManager, spawner, bossPathToSpawnIndex);
		ApplyStageMapReferences(stageManager);
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
		return true;
	}

	static void ApplyPoolEnemyPrefabs(PoolManager pool)
	{
		var prefabs = new List<GameObject>(EnemyCount + BossMinionPrefabPaths.Length);
		for (int i = 0; i < EnemyCount; i++)
		{
			string path = $"{EnemyPrefabRoot}/Enemy{(i + 1):00}.prefab";
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (prefab == null)
				Debug.LogWarning($"[BossStageSetup] Enemy 프리팹 없음: {path}");
			prefabs.Add(prefab);
		}

		for (int i = 0; i < BossMinionPrefabPaths.Length; i++)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossMinionPrefabPaths[i]);
			if (prefab == null)
				Debug.LogWarning($"[BossStageSetup] 보스 minion 프리팹 없음: {BossMinionPrefabPaths[i]}");
			prefabs.Add(prefab);
		}

		pool.enemyPrefabs = prefabs.ToArray();
	}

	static Dictionary<string, int> ApplyPoolBossPrefabs(PoolManager pool)
	{
		var uniquePaths = new List<string>();
		for (int i = 0; i < Biomes.Length; i++)
		{
			string[] paths = Biomes[i].bossPrefabPaths;
			for (int j = 0; j < paths.Length; j++)
			{
				if (!uniquePaths.Contains(paths[j]))
					uniquePaths.Add(paths[j]);
			}
		}

		for (int i = 0; i < ExtraBossPoolPrefabs.Length; i++)
		{
			if (!uniquePaths.Contains(ExtraBossPoolPrefabs[i]))
				uniquePaths.Add(ExtraBossPoolPrefabs[i]);
		}

		var pathToIndex = new Dictionary<string, int>();
		var prefabs = new GameObject[uniquePaths.Count];
		for (int i = 0; i < uniquePaths.Count; i++)
		{
			prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(uniquePaths[i]);
			pathToIndex[uniquePaths[i]] = i;
			if (prefabs[i] == null)
				Debug.LogError($"[BossStageSetup] Boss 프리팹 없음: {uniquePaths[i]}");
		}

		pool.bossPrefabs = prefabs;
		return pathToIndex;
	}

	static Dictionary<string, int> ApplySpawnerEntries(Spawner spawner, Dictionary<string, int> bossPathToPoolIndex)
	{
		var entries = new List<SpawnData>();
		for (int i = 0; i < EnemyCount; i++)
		{
			entries.Add(new SpawnData
			{
				isBoss = false,
				spawnTime = 0.2f,
				prefabIndex = i,
			});
		}

		var bossPathToSpawnIndex = new Dictionary<string, int>();
		foreach (KeyValuePair<string, int> pair in bossPathToPoolIndex)
		{
			bossPathToSpawnIndex[pair.Key] = entries.Count;
			entries.Add(new SpawnData
			{
				isBoss = true,
				spawnTime = 0.2f,
				prefabIndex = pair.Value,
			});
		}

		spawner.spawnData = entries.ToArray();
		return bossPathToSpawnIndex;
	}

	static void ApplyStageWaves(
		StageManager stageManager,
		Spawner spawner,
		Dictionary<string, int> bossPathToSpawnIndex)
	{
		if (stageManager.stageDatas == null || stageManager.stageDatas.Length < Biomes.Length)
		{
			stageManager.stageDatas = new StageData[Biomes.Length];
			for (int i = 0; i < Biomes.Length; i++)
				stageManager.stageDatas[i] = new StageData();
		}

		for (int stage = 0; stage < Biomes.Length; stage++)
		{
			BiomeStageConfig biome = Biomes[stage];
			int typeA = biome.enemyStart - 1;
			int typeB = biome.enemyEnd - 1;
			if (typeB < typeA)
				typeB = typeA;

			int eliteSpawnIndex = ResolveEliteSpawnIndex(biome);

			if (stageManager.stageDatas[stage] == null)
				stageManager.stageDatas[stage] = new StageData();

			stageManager.stageDatas[stage].waves = new WaveData[WaveCount];
			for (int wave = 0; wave < WaveCount; wave++)
			{
				WaveData waveData = new WaveData();
				if (wave == BossWaveIndex)
				{
					waveData.isBossWave = true;
					waveData.bossSpawnIndexes = BuildBossSpawnIndexes(biome, bossPathToSpawnIndex);
					waveData.enemies = BuildBossPreWaveEnemies(typeA, typeB, eliteSpawnIndex);
				}
				else
				{
					waveData.isBossWave = false;
					waveData.bossSpawnIndexes = null;
					waveData.enemies = BuildNormalWaveEnemies(wave, typeA, typeB, eliteSpawnIndex);
				}

				stageManager.stageDatas[stage].waves[wave] = waveData;
			}
		}
	}

	static int ResolveEliteSpawnIndex(BiomeStageConfig biome)
	{
		if (biome.eliteEnemyNumbers == null || biome.eliteEnemyNumbers.Length == 0)
			return -1;

		return biome.eliteEnemyNumbers[0] - 1;
	}

	static int[] BuildBossSpawnIndexes(BiomeStageConfig biome, Dictionary<string, int> bossPathToSpawnIndex)
	{
		var indexes = new List<int>();
		for (int i = 0; i < biome.bossPrefabPaths.Length; i++)
		{
			if (bossPathToSpawnIndex.TryGetValue(biome.bossPrefabPaths[i], out int spawnIndex))
				indexes.Add(spawnIndex);
			else
				Debug.LogError($"[BossStageSetup] 보스 spawnData 없음: {biome.bossPrefabPaths[i]}");
		}

		return indexes.ToArray();
	}

	static EnemySpawnInfo[] BuildNormalWaveEnemies(int wave, int typeA, int typeB, int eliteSpawnIndex)
	{
		switch (wave)
		{
			case 0:
				return new[] { new EnemySpawnInfo { spawnDataIndex = typeA, spawnCount = 1 } };
			case 1:
				return new[] { new EnemySpawnInfo { spawnDataIndex = typeA, spawnCount = 5 } };
			case 2:
				return new[] { new EnemySpawnInfo { spawnDataIndex = typeA, spawnCount = 10 } };
			case 3:
				if (eliteSpawnIndex >= 0)
				{
					return new[]
					{
						new EnemySpawnInfo { spawnDataIndex = typeA, spawnCount = 5 },
						new EnemySpawnInfo { spawnDataIndex = eliteSpawnIndex, spawnCount = 3 },
					};
				}

				return new[]
				{
					new EnemySpawnInfo { spawnDataIndex = typeA, spawnCount = 5 },
					new EnemySpawnInfo { spawnDataIndex = typeB, spawnCount = 5 },
				};
			default:
				return new[] { new EnemySpawnInfo { spawnDataIndex = typeA, spawnCount = 1 } };
		}
	}

	static EnemySpawnInfo[] BuildBossPreWaveEnemies(int typeA, int typeB, int eliteSpawnIndex)
	{
		if (eliteSpawnIndex >= 0)
		{
			return new[]
			{
				new EnemySpawnInfo { spawnDataIndex = typeA, spawnCount = 5 },
				new EnemySpawnInfo { spawnDataIndex = eliteSpawnIndex, spawnCount = 5 },
			};
		}

		return new[]
		{
			new EnemySpawnInfo { spawnDataIndex = typeA, spawnCount = 5 },
			new EnemySpawnInfo { spawnDataIndex = typeB, spawnCount = 5 },
		};
	}

	static void ApplyStageMapReferences(StageManager stageManager)
	{
		GameObject stagesRoot = GameObject.Find("Stages");
		if (stagesRoot == null)
		{
			Debug.LogWarning("[BossStageSetup] Stages 루트를 찾지 못했습니다.");
			return;
		}

		var maps = new GameObject[stagesRoot.transform.childCount];
		for (int i = 0; i < maps.Length; i++)
			maps[i] = stagesRoot.transform.GetChild(i).gameObject;

		stageManager.stages = maps;
	}
}
#endif
