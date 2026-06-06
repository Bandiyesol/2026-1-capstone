using UnityEngine;

/// <summary>보스 프리팹 SpriteRenderer → UI 초상 스프라이트.</summary>
public static class BossBriefPortraitResolver
{
	public static Sprite Resolve(int stageIndex, GameObject[] explicitPrefabs = null)
	{
		GameObject prefab = ResolvePrefab(stageIndex, explicitPrefabs);
		return FromPrefab(prefab);
	}

	public static GameObject ResolvePrefab(int stageIndex, GameObject[] explicitPrefabs = null)
	{
		if (explicitPrefabs != null && stageIndex >= 0 && stageIndex < explicitPrefabs.Length)
		{
			GameObject p = explicitPrefabs[stageIndex];
			if (p != null)
				return p;
		}

		GameManager gm = GameManager.instance;
		if (gm == null)
			return null;

		if (gm.bossPortraitPrefabs != null && stageIndex >= 0 && stageIndex < gm.bossPortraitPrefabs.Length)
		{
			GameObject p = gm.bossPortraitPrefabs[stageIndex];
			if (p != null)
				return p;
		}

		if (gm.pool != null && gm.pool.bossPrefabs != null
		    && stageIndex >= 0 && stageIndex < gm.pool.bossPrefabs.Length)
		{
			return gm.pool.bossPrefabs[stageIndex];
		}

		return null;
	}

	public static Sprite FromPrefab(GameObject prefab)
	{
		if (prefab == null)
			return null;

		SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
		if (renderer != null && renderer.sprite != null)
			return renderer.sprite;

		renderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
		return renderer != null ? renderer.sprite : null;
	}
}
