using UnityEngine;

/// <summary>
/// 서브클래스별 룬 수치를 Effect·쿨타임 로직에서 일관되게 읽기 위한 접근자.
/// </summary>
public static class RuneDataAccess
{
	public static ActiveRuneData AsActive(RuneData data) => data as ActiveRuneData;

	public static SplitRuneData AsSplit(RuneData data) => data as SplitRuneData;

	public static RicochetRuneData AsRicochet(RuneData data) => data as RicochetRuneData;

	public static float GetDuration(RuneData data)
	{
		if (data is ActiveRuneData active) return active.duration;
		return 0f;
	}

	public static float GetSpeedMultiplier(RuneData data)
	{
		if (data is ActiveRuneData active) return active.speedMultiplier;
		return 0f;
	}

	public static float GetAffectedRange(RuneData data)
	{
		if (data is ActiveRuneData active) return active.affectedRange;
		return 0f;
	}

	public static float GetInterval(RuneData data)
	{
		if (data is RicochetRuneData ricochet) return ricochet.interval;
		if (data is ExplodeRuneData explode) return explode.interval;
		if (data is FreezeRuneData freeze) return freeze.interval;
		return 0f;
	}

	public static int GetSplitCount(RuneData data)
	{
		if (data is SplitRuneData split) return split.splitCount;
		return 0;
	}

	public static int GetBounceCount(RuneData data)
	{
		if (data is RicochetRuneData ricochet) return ricochet.bounceCount;
		return 0;
	}
}
