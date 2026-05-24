using UnityEngine;

/// <summary>
/// 서브클래스 SO 필드를 Effect에서 읽기 위한 접근자.
/// 타입마다 필드가 달라서 switch/type-pattern 사용 (enum switch보다 서브클래스와 1:1).
/// </summary>
public static class RuneDataAccess
{
	const float DefaultActiveDuration = 3f;
	const int DefaultBounceCount = 3;

	public static float GetDuration(RuneData data) => data switch
	{
		ActiveRuneData a when a.duration > 0f => a.duration,
		GravityRuneData g when g.duration > 0f => g.duration,
		{ category: RuneCategory.Active } => DefaultActiveDuration,
		_ => 0f
	};

	public static float GetSpeedMultiplier(RuneData data) => data switch
	{
		ActiveRuneData a when a.speedMultiplier > 0f => a.speedMultiplier,
		_ => 1f
	};

	public static float GetAffectedRange(RuneData data) => data switch
	{
		ActiveRuneData a when a.affectedRange > 0f => a.affectedRange,
		_ => 0f
	};

	public static float GetInterval(RuneData data) => data switch
	{
		RicochetRuneData r when r.interval > 0f => r.interval,
		ExplodeRuneData e when e.interval > 0f => e.interval,
		FreezeRuneData f when f.interval > 0f => f.interval,
		LogicRuneData l when l.interval > 0f => l.interval,
		_ => 0f
	};

	/// <summary>분열: 충돌·쿨마다 한 번에 생성할 발 수. 부모는 생존 동안 반복.</summary>
	public static int GetSpawnsPerTrigger(RuneData data) => data switch
	{
		SplitRuneData s when s.spawnsPerTrigger > 0 => s.spawnsPerTrigger,
		{ runeType: RuneType.Split } => 3,
		_ => 0
	};

	public static int GetBounceCount(RuneData data) => data switch
	{
		RicochetRuneData r when r.bounceCount > 0 => r.bounceCount,
		{ runeType: RuneType.Ricochet } => DefaultBounceCount,
		_ => 0
	};
}
