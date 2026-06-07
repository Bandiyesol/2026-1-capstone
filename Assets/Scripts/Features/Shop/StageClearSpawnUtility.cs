using UnityEngine;

/// <summary>스테이지 클리어 시 마법진·상점 주인을 보스 처치 위치에 스폰합니다.</summary>
public static class StageClearSpawnUtility
{
	const float GapBetweenPortalAndShopkeeper = 2.0f;

	public static void SpawnPortalAndShopkeeper(Vector3 position, int portalGimmickIndex, Vector2? shopkeeperOffset = null)
	{
		if (PoolManager.Instance == null)
		{
			Debug.LogWarning("[StageClearSpawn] PoolManager를 찾지 못했습니다.");
			return;
		}

		GameObject portal = PoolManager.Instance.GetGimmick(portalGimmickIndex);
		if (portal != null)
		{
			portal.transform.position = position;
		}
		else
		{
			Debug.LogWarning(
				$"[StageClearSpawn] Stage Portal 기믹을 가져오지 못했습니다 (index={portalGimmickIndex}).");
		}

		int shopkeeperIndex = PoolManager.Instance.FindShopkeeperGimmickIndex();
		if (shopkeeperIndex < 0)
			return;

		if (!StageClearSpawnSettings.TryRollShopkeeperSpawn())
			return;

		GameObject shopkeeper = PoolManager.Instance.GetGimmick(shopkeeperIndex);
		if (shopkeeper == null)
		{
			Debug.LogWarning("[StageClearSpawn] ShopkeeperNpc 기믹을 가져오지 못했습니다.");
			return;
		}

		Vector2 offset = shopkeeperOffset ?? ResolveShopkeeperOffset(portal, shopkeeper);
		shopkeeper.transform.position = position + (Vector3)offset;

		if (GameManager.instance?.player != null)
			shopkeeper.transform.localScale = GameManager.instance.player.transform.localScale;
	}

	static Vector2 ResolveShopkeeperOffset(GameObject portal, GameObject shopkeeper)
	{
		float portalRadius = 0.6f;
		if (portal != null && portal.TryGetComponent(out CircleCollider2D portalCollider))
			portalRadius = portalCollider.radius * Mathf.Max(portal.transform.lossyScale.x, 0.01f);

		float keeperHalfWidth = 0.35f;
		if (shopkeeper != null && shopkeeper.TryGetComponent(out BoxCollider2D keeperCollider))
		{
			float scale = Mathf.Max(shopkeeper.transform.lossyScale.x, 0.01f);
			keeperHalfWidth = keeperCollider.size.x * scale * 0.5f;
		}

		float distance = portalRadius + keeperHalfWidth + GapBetweenPortalAndShopkeeper;
		return new Vector2(distance, 0f);
	}
}
