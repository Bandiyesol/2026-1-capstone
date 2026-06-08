using UnityEngine;

/// <summary>보스 프리팹 SpriteRenderer → UI 초상 스프라이트.</summary>
public static class BossBriefPortraitResolver
{
	public static Sprite Resolve(int stageIndex, GameObject[] explicitPrefabs = null)
	{
		GameManager gm = GameManager.instance;
		if (gm != null)
		{
			Sprite cached = gm.GetCachedBossPortrait(stageIndex);
			if (cached != null)
				return cached;
		}

		GameObject prefab = ResolvePrefab(stageIndex, explicitPrefabs);
		return FromPrefab(prefab);
	}

	public static GameObject ResolvePrefab(int stageIndex, GameObject[] explicitPrefabs = null)
	{
		if (TryGetPrefabAt(explicitPrefabs, stageIndex, out GameObject explicitPrefab))
			return explicitPrefab;

		GameManager gm = GameManager.instance;
		if (gm == null)
			return null;

		if (TryGetPrefabAt(gm.bossPortraitPrefabs, stageIndex, out GameObject portraitPrefab))
			return portraitPrefab;

		if (gm.pool != null && TryGetPrefabAt(gm.pool.bossPrefabs, stageIndex, out GameObject poolPrefab))
			return poolPrefab;

		return null;
	}

	public static Sprite FromPrefab(GameObject prefab)
	{
		if (prefab == null)
			return null;

		SpriteRenderer[] renderers = prefab.GetComponentsInChildren<SpriteRenderer>(true);
		for (int i = 0; i < renderers.Length; i++)
		{
			SpriteRenderer renderer = renderers[i];
			if (renderer != null && renderer.sprite != null)
				return renderer.sprite;
		}

		return null;
	}

	static bool TryGetPrefabAt(GameObject[] prefabs, int stageIndex, out GameObject prefab)
	{
		prefab = null;
		if (prefabs == null || stageIndex < 0 || stageIndex >= prefabs.Length)
			return false;

		prefab = prefabs[stageIndex];
		return prefab != null;
	}
}
