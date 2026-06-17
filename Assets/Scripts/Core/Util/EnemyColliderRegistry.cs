using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 활성 Enemy 콜라이더 추적. 보스 탄막 등에서 FindGameObjectsWithTag 없이 충돌 무시 처리.
/// </summary>
public static class EnemyColliderRegistry
{
	static readonly List<Collider2D> ActiveColliders = new List<Collider2D>(128);

	public static void Register(Collider2D collider)
	{
		if (collider == null)
			return;

		for (int i = 0; i < ActiveColliders.Count; i++)
		{
			if (ActiveColliders[i] == collider)
				return;
		}

		ActiveColliders.Add(collider);
	}

	public static void Unregister(Collider2D collider)
	{
		if (collider == null)
			return;

		ActiveColliders.Remove(collider);
	}

	public static void IgnoreAllEnemies(Collider2D bulletCollider)
	{
		if (bulletCollider == null)
			return;

		for (int i = 0; i < ActiveColliders.Count; i++)
		{
			Collider2D enemyCollider = ActiveColliders[i];
			if (enemyCollider != null && enemyCollider.enabled)
				Physics2D.IgnoreCollision(bulletCollider, enemyCollider, true);
		}
	}
}
