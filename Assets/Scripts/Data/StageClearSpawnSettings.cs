using UnityEngine;

[CreateAssetMenu(
	fileName = "StageClearSpawnSettings",
	menuName = "Game/Stage Clear Spawn Settings")]
public class StageClearSpawnSettings : ScriptableObject
{
	[Header("보스 처치 후 스폰")]
	[Range(0f, 1f)]
	[Tooltip("보스 처치 후 마법진 옆에 상점 주인이 등장할 확률 (0~1). 예: 0.5 = 50%")]
	public float shopkeeperSpawnChance = 0.5f;

	static StageClearSpawnSettings cached;

	public static StageClearSpawnSettings Instance
	{
		get
		{
			if (cached != null)
				return cached;

			if (GameManager.instance != null && GameManager.instance.stageClearSpawnSettings != null)
				cached = GameManager.instance.stageClearSpawnSettings;
			else
				cached = Resources.Load<StageClearSpawnSettings>("Data/StageClearSpawnSettings");

			return cached;
		}
	}

	public static void ClearCache()
	{
		cached = null;
	}

	public bool RollShopkeeperSpawn()
	{
		return Random.value < Mathf.Clamp01(shopkeeperSpawnChance);
	}

	public static bool TryRollShopkeeperSpawn()
	{
		StageClearSpawnSettings settings = Instance;
		float chance = settings != null ? settings.shopkeeperSpawnChance : 0.5f;
		return Random.value < Mathf.Clamp01(chance);
	}
}
