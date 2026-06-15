using UnityEngine;

/// <summary>인게임 보스 체력 HUD용 — 코어/다중 유닛/단일 보스 패턴을 통합 집계합니다.</summary>
public static class BossHealthHudResolver
{
	public struct Snapshot
	{
		public float Current;
		public float Max;
		public string DisplayName;
		public bool IsValid;
	}

	public static Snapshot Resolve()
	{
		if (TryResolveFrostWolf(out Snapshot frost))
			return frost;

		if (TryResolveLavaTyrano(out Snapshot lava))
			return lava;

		if (TryResolveVolcanoPumpkin(out Snapshot pumpkin))
			return pumpkin;

		if (TryResolveGenericBossBase(out Snapshot generic))
			return generic;

		return default;
	}

	static bool TryResolveFrostWolf(out Snapshot snap)
	{
		snap = default;
		foreach (FrostWolfCore core in Object.FindObjectsByType<FrostWolfCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
		{
			if (core == null || !core.isActiveAndEnabled)
				continue;

			if (core.maxHealth <= 0f || core.currentHealth <= 0f)
				continue;

			snap.Current = core.currentHealth;
			snap.Max = core.maxHealth;
			snap.DisplayName = ResolveDisplayName();
			snap.IsValid = true;
			return true;
		}

		return false;
	}

	static bool TryResolveLavaTyrano(out Snapshot snap)
	{
		snap = default;
		foreach (LavaTyranoCore core in Object.FindObjectsByType<LavaTyranoCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
		{
			if (core == null || !core.isActiveAndEnabled || core.units == null || core.units.Count == 0)
				continue;

			float current = 0f;
			float max = 0f;
			int alive = 0;

			for (int i = 0; i < core.units.Count; i++)
			{
				LavaTyranoUnit unit = core.units[i];
				if (unit == null || !unit.gameObject.activeInHierarchy || unit.health <= 0f)
					continue;

				current += unit.health;
				max += unit.maxHealth;
				alive++;
			}

			if (alive == 0 || max <= 0f)
				continue;

			snap.Current = current;
			snap.Max = max;
			snap.DisplayName = ResolveDisplayName();
			snap.IsValid = true;
			return true;
		}

		return false;
	}

	static bool TryResolveVolcanoPumpkin(out Snapshot snap)
	{
		snap = default;
		VolcanoPumpkinUnit[] units = Object.FindObjectsByType<VolcanoPumpkinUnit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		if (units == null || units.Length == 0)
			return false;

		float current = 0f;
		float max = 0f;
		int alive = 0;

		for (int i = 0; i < units.Length; i++)
		{
			VolcanoPumpkinUnit unit = units[i];
			if (unit == null || !unit.gameObject.activeInHierarchy || unit.health <= 0f)
				continue;

			current += unit.health;
			max += unit.maxHealth;
			alive++;
		}

		if (alive == 0 || max <= 0f)
			return false;

		snap.Current = current;
		snap.Max = max;
		snap.DisplayName = ResolveDisplayName();
		snap.IsValid = true;
		return true;
	}

	static bool TryResolveGenericBossBase(out Snapshot snap)
	{
		snap = default;
		BossBase[] bosses = Object.FindObjectsByType<BossBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		if (bosses == null || bosses.Length == 0)
			return false;

		float current = 0f;
		float max = 0f;
		int alive = 0;

		for (int i = 0; i < bosses.Length; i++)
		{
			BossBase boss = bosses[i];
			if (boss == null || !boss.gameObject.activeInHierarchy || boss.health <= 0f)
				continue;

			if (boss is FrostWolfBoss or LavaTyranoUnit or VolcanoPumpkinUnit)
				continue;

			if (boss.waveManager == null)
				continue;

			current += boss.health;
			max += boss.maxHealth;
			alive++;
		}

		if (alive == 0 || max <= 0f)
			return false;

		snap.Current = current;
		snap.Max = max;
		snap.DisplayName = ResolveDisplayName();
		snap.IsValid = true;
		return true;
	}

	static string ResolveDisplayName()
	{
		if (BossBriefingRuntime.HasBrief && !string.IsNullOrEmpty(BossBriefingRuntime.DisplayName))
			return BossBriefingRuntime.DisplayName;

		StageManager stage = StageManager.instance;
		if (stage != null)
			return BossBriefingRuntime.GetBossDisplayName(stage.stageIndex);

		return "보스";
	}
}
