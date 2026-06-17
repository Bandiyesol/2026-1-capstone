using System.Collections.Generic;
using UnityEngine;

public class EffectChain : RuneEffect, ITriggerEffect
{
	static readonly List<Collider2D> chainColliderScratch = new List<Collider2D>(32);

	public bool DestroyOnExecute => data != null && data.isDestroyed;
	public bool ProtectParent => false;


	private void Update() => UpdateCooltime();


	public void OnReflect(Collider2D collision)
	{
		if (!isReady || !TryGetDamageable(collision, out _))
			return;

		int chainCount = RuneDataAccess.GetChainCount(data);
		float radius = RuneDataAccess.GetChainRadius(data);
		if (chainCount <= 0 || radius <= 0f)
			return;

		float chainDamage = DamageCalculator.CalculateBaseDamage(weapon, data);
		CollectEnemyColliders(collision.transform.position, radius, chainColliderScratch);
		Vector3 hitPosition = collision.transform.position;

		chainColliderScratch.Sort((a, b) =>
		{
			float distA = (a.transform.position - hitPosition).sqrMagnitude;
			float distB = (b.transform.position - hitPosition).sqrMagnitude;
			return distA.CompareTo(distB);
		});

		List<IDamageable> targets = new();
		List<Vector3> targetPositions = new();
		for (int i = 0; i < chainColliderScratch.Count; i++)
		{
			Collider2D enemyCollider = chainColliderScratch[i];
			if (enemyCollider == collision) continue;

			if (!TryGetDamageable(enemyCollider, out IDamageable damageable) || targets.Contains(damageable)) continue;

			targets.Add(damageable);
			targetPositions.Add(enemyCollider.transform.position);
			if (targets.Count >= chainCount) break;
		}

		foreach (IDamageable target in targets)
			target.TakeDamage(chainDamage);

		DrawChain(hitPosition, targetPositions);
		ResetCooltime();
	}

	void DrawChain(Vector3 startPosition, List<Vector3> targetPositions)
	{
		if (targetPositions.Count == 0)
			return;

		GameObject visual = new GameObject("ChainRuneVisual");
		LineRenderer line = visual.AddComponent<LineRenderer>();
		line.positionCount = targetPositions.Count + 1;
		line.useWorldSpace = true;
		line.startWidth = 0.08f;
		line.endWidth = 0.02f;
		line.material = ChainLineMaterialCache.Shared;
		line.startColor = new Color(0.45f, 0.85f, 1f, 1f);
		line.endColor = new Color(0.85f, 1f, 1f, 0.15f);
		line.SetPosition(0, startPosition);

		for (int i = 0; i < targetPositions.Count; i++)
			line.SetPosition(i + 1, targetPositions[i]);

		Destroy(visual, 0.15f);
	}
}
