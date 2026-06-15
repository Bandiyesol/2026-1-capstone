/// <summary>Spawner.spawnData 인덱스 — 씬 hex 직렬화 깨짐(36→603979776) 복구.</summary>
public static class BossSpawnDataIndexUtility
{
	public static int Normalize(int index)
	{
		if (index >= 0 && index < 256)
			return index;

		// 36(0x24)이 0x24000000(603979776)으로 읽힌 패턴
		int fromHighByte = (index >> 24) & 0xFF;
		if (fromHighByte > 0)
			return fromHighByte;

		return index & 0xFF;
	}

	public static int[] SanitizeArray(int[] rawIndexes)
	{
		if (rawIndexes == null || rawIndexes.Length == 0)
			return rawIndexes;

		var sanitized = new int[rawIndexes.Length];
		for (int i = 0; i < rawIndexes.Length; i++)
			sanitized[i] = Normalize(rawIndexes[i]);

		return sanitized;
	}

	public static bool IsPlausibleSpawnIndex(int index, int spawnDataLength)
	{
		index = Normalize(index);
		return index >= 0 && index < spawnDataLength;
	}

	public static int ResolveFromWaveManager(Spawner spawner)
	{
		WaveManager wave = UnityEngine.Object.FindFirstObjectByType<WaveManager>(
			UnityEngine.FindObjectsInactive.Include);
		if (wave == null || wave.SelectedBossSpawnDataIndex < 0)
			return -1;

		int index = Normalize(wave.SelectedBossSpawnDataIndex);
		if (spawner == null || spawner.spawnData == null)
			return index;

		return index < spawner.spawnData.Length ? index : -1;
	}
}
