using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectRicochet : RuneEffect, ITriggerEffect
{
	const float RicochetMoveSpeedScale = 0.45f;
	const float BounceMoveSpeedRetention = 0.88f;
	const float MinMoveSpeed = 0.35f;

	int remainingBounces;
	Collider2D lastHitCollider;
	float ignoreHitUntil;
	bool useStraightTravel;
	readonly List<(Collider2D self, Collider2D other)> ignoredCollisions = new();

	public bool DestroyOnExecute => remainingBounces <= 0 && data != null && data.isDestroyed;
	public bool ProtectParent => false;

	/// <summary>도탄 후 반사 방향으로만 직진 (유도/파동 궤적 덮어쓰기 방지).</summary>
	public bool PreferStraightTravel => useStraightTravel;

	public override void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		base.InitEffect(instance, motion, runeData);
		RestoreIgnoredCollisions();
		remainingBounces = RuneDataAccess.GetBounceCount(data);
		lastHitCollider = null;
		ignoreHitUntil = 0f;
		useStraightTravel = false;

		if (weapon != null)
			weapon.movespeed = Mathf.Max(MinMoveSpeed, weapon.movespeed * RicochetMoveSpeedScale);
	}

	void OnDisable()
	{
		RestoreIgnoredCollisions();
	}

	void Update()
	{
		UpdateCooltime();
		if (ignoreHitUntil > 0f)
			ignoreHitUntil -= Time.deltaTime;
	}

	public void OnReflect(Collider2D collision)
	{
		TryReflect(collision);
	}

	/// <summary>충돌 시 반대 방향으로 튕김. 성공 여부 반환.</summary>
	public bool TryReflect(Collider2D collision)
	{
		if (!isReady || remainingBounces <= 0 || !TryGetDamageable(collision, out _))
			return false;

		if (collision == lastHitCollider && ignoreHitUntil > 0f)
			return false;

		Vector2 incident = transform.right.normalized;
		Vector2 reflectDir = -incident;

		float angle = Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0f, 0f, angle);

		float separation = Mathf.Max(1.6f, weapon.size * 1.25f);
		transform.position += (Vector3)(reflectDir * separation);

		lastHitCollider = collision;
		ignoreHitUntil = Mathf.Max(0.35f, RuneDataAccess.GetInterval(data));
		useStraightTravel = true;
		remainingBounces--;

		if (weapon != null)
			weapon.movespeed = Mathf.Max(MinMoveSpeed, weapon.movespeed * BounceMoveSpeedRetention);

		TemporarilyIgnoreCollider(collision, ignoreHitUntil);
		ResetCooltime();
		return true;
	}

	void TemporarilyIgnoreCollider(Collider2D other, float duration)
	{
		if (other == null || parentMotion == null || duration <= 0f)
			return;

		parentMotion.StartCoroutine(IgnoreColliderForSeconds(other, duration));
	}

	IEnumerator IgnoreColliderForSeconds(Collider2D other, float duration)
	{
		var localPairs = new List<(Collider2D self, Collider2D other)>();
		Collider2D[] selfColliders = parentMotion.GetComponentsInChildren<Collider2D>(true);
		for (int i = 0; i < selfColliders.Length; i++)
		{
			Collider2D self = selfColliders[i];
			if (self == null || other == null)
				continue;

			Physics2D.IgnoreCollision(self, other, true);
			var pair = (self, other);
			ignoredCollisions.Add(pair);
			localPairs.Add(pair);
		}

		yield return new WaitForSeconds(duration);

		for (int i = 0; i < localPairs.Count; i++)
		{
			(Collider2D self, Collider2D other) pair = localPairs[i];
			if (pair.self != null && pair.other != null)
				Physics2D.IgnoreCollision(pair.self, pair.other, false);
			ignoredCollisions.Remove(pair);
		}
	}

	void RestoreIgnoredCollisions()
	{
		for (int i = 0; i < ignoredCollisions.Count; i++)
		{
			(Collider2D self, Collider2D other) pair = ignoredCollisions[i];
			if (pair.self != null && pair.other != null)
				Physics2D.IgnoreCollision(pair.self, pair.other, false);
		}

		ignoredCollisions.Clear();
	}
}
