using System.Collections.Generic;
using UnityEngine;

// [RuneEffect.cs] 인터페이스 정의
public interface IActiveDriver  { bool isFinished { get; } void UpdateMovement(); }
public interface IStateEffect   { bool isFinished { get; } void UpdateState(); }
public interface ILogicEffect   { void UpdateLogic(); }
public interface ITriggerEffect { bool ProtectParent { get; } bool DestroyOnExecute { get; } void OnReflect(Collider2D collision); }
public interface IFinalEffect   { void OnFinalExecute(); }

// 모든 룬 이펙트 스크립트의 최상위 부모 클래스
public abstract class RuneEffect : MonoBehaviour
{
	protected WeaponInstance weapon;
	protected Motion parentMotion;
	public RuneData data { get; protected set; }
	public float currentCooltime { get; protected set; } = 0f;

	public bool isReady          => currentCooltime <= 0f;
	public virtual bool isFinished     => true;
	public virtual bool ManualCollision => false;

	public virtual void InitEffect(WeaponInstance instance, Motion motion, RuneData runeData)
	{
		weapon        = instance;
		parentMotion  = motion;
		data          = runeData;
		currentCooltime = 0f;
	}

	/// <summary>플레이 중이며, 이 룬이 현재 로드아웃에 장착되어 있을 때만 true.</summary>
	public bool ShouldRunEffect()
	{
		if (GameManager.instance == null || !GameManager.instance.isLive)
			return false;

		if (data == null || parentMotion == null || parentMotion.instance == null)
			return false;

		return RuneManager.instance != null && RuneManager.instance.ContainsRune(data);
	}

	// 홍식 버전 채택 — CooldownMultiplier 반영 (특수 물약 버프 연동)
	public void ResetCooltime()
	{
		float interval   = RuneDataAccess.GetInterval(data);
		float multiplier = RuneManager.instance != null ? RuneManager.instance.CooldownMultiplier : 1f;
		currentCooltime  = interval * multiplier;
	}

	protected void UpdateCooltime()
	{
		if (currentCooltime > 0f) currentCooltime -= Time.deltaTime;
	}

	/// <summary>근접·오브 등 제자리 무기 — 액티브 룬 기본 전진 속도</summary>
	const float DefaultStationaryActiveMoveSpeed = 5f;

	/// <summary>애니메이션 근접 등 제자리 무기 여부</summary>
	protected bool IsStationaryWeapon()
	{
		if (weapon?.info == null) return false;

		return weapon.info.type is "Sword" or "Hammer" or "Sickle" or "Whip" or "Orb";
	}

	protected bool IsOrbWeapon()
		=> weapon?.info != null && weapon.info.type == "Orb";

	/// <summary>
	/// 액티브 룬 전진 속도.
	/// 투사체(movespeed &gt; 0)는 무기 movespeed 그대로, 제자리 무기는 DefaultStationaryActiveMoveSpeed.
	/// </summary>
	protected float GetActiveMoveSpeed()
	{
		if (weapon == null)
			return DefaultStationaryActiveMoveSpeed;

		if (IsStationaryWeapon() || weapon.movespeed <= 0.01f)
			return DefaultStationaryActiveMoveSpeed;

		return weapon.movespeed;
	}

	protected float GetActiveTurnSpeed()
	{
		if (IsStationaryWeapon())
			return Mathf.Max(120f, RuneDataAccess.GetSpeedMultiplier(data) * 90f);

		return Mathf.Max(180f, RuneDataAccess.GetSpeedMultiplier(data) * 140f);
	}

	protected static bool TryGetDamageable(Collider2D collider, out IDamageable damageable)
	{
		damageable = null;
		if (collider == null) return false;

		damageable = collider.GetComponent<IDamageable>()
			?? collider.GetComponentInParent<IDamageable>()
			?? collider.GetComponentInChildren<IDamageable>();

		return damageable != null;
	}

	static readonly List<Collider2D> FindEnemyScratch = new List<Collider2D>(32);

	protected static void CollectEnemyColliders(Vector2 center, float radius, List<Collider2D> results)
	{
		results.Clear();
		if (radius <= 0f)
			return;

		using PhysicsQuery2D.OverlapCircleScope query = PhysicsQuery2D.OverlapCircle(center, radius);
		for (int i = 0; i < query.Count; i++)
		{
			Collider2D hit = query.Get(i);
			if (hit == null)
				continue;

			if (hit.CompareTag("Enemy") || TryGetDamageable(hit, out _))
				results.Add(hit);
		}
	}

	protected static IReadOnlyList<Collider2D> FindEnemyColliders(Vector2 center, float radius)
	{
		CollectEnemyColliders(center, radius, FindEnemyScratch);
		return FindEnemyScratch;
	}
}