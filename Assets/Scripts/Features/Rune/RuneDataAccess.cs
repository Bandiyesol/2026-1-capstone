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
		VampireRuneData v when v.interval > 0f => v.interval,
		ExplodeRuneData e when e.interval > 0f => e.interval,
		FreezeRuneData f when f.interval > 0f => f.interval,
		ChainRuneData c when c.interval > 0f => c.interval,
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

	public static float GetFreezeRadius(RuneData data) => data switch
	{
		FreezeRuneData f when f.freezeRadius > 0f => f.freezeRadius,
		{ runeType: RuneType.Freeze } => 2f,
		_ => 0f
	};

	public static float GetFreezeDuration(RuneData data) => data switch
	{
		FreezeRuneData f when f.freezeDuration > 0f => f.freezeDuration,
		{ runeType: RuneType.Freeze } => 1f,
		_ => 0f
	};

	public static int GetChainCount(RuneData data) => data switch
	{
		ChainRuneData c when c.chainCount > 0 => c.chainCount,
		{ runeType: RuneType.Chain } => 3,
		_ => 0
	};

	public static float GetChainRadius(RuneData data) => data switch
	{
		{ valueA: > 0f } => data.valueA,
		{ runeType: RuneType.Chain } => 3f,
		_ => 0f
	};

	public static float GetExplodeRadius(RuneData data) => data switch
	{
		ExplodeRuneData e when e.explodeRadius > 0f => e.explodeRadius,
		{ valueA: > 0f } => data.valueA,
		{ runeType: RuneType.Explode } => 2.5f,
		_ => 0f
	};

	public static float GetLogicDistance(RuneData data) => data switch
	{
		LogicRuneData l when l.distance > 0f => l.distance,
		{ valueA: > 0f } => data.valueA,
		_ => 2f
	};

	public static float GetPullForce(RuneData data) => data switch
	{
		GravityRuneData g when g.pullForce > 0f => g.pullForce,
		{ valueB: > 0f } => data.valueB,
		{ runeType: RuneType.Gravity } => 5f,
		_ => 0f
	};

	public static float GetGravityRadius(RuneData data) => data switch
	{
		{ valueA: > 0f } => data.valueA,
		{ runeType: RuneType.Gravity } => 3f,
		_ => 0f
	};

	public static float GetGrowthDuration(RuneData data) => data switch
	{
		GrowthRuneData g when g.maxGrowthTime > 0f => g.maxGrowthTime,
		{ valueB: > 0f } => data.valueB,
		{ runeType: RuneType.Growth } => 3f,
		_ => 0f
	};

	public static float GetGrowthScaleRatio(RuneData data) => data switch
	{
		GrowthRuneData g when g.maxScaleRatio > 1f => g.maxScaleRatio,
		{ valueA: > 1f } => data.valueA,
		{ runeType: RuneType.Growth } => 2f,
		_ => 1f
	};

	public static float GetGrowthDamageRatio(RuneData data) => data switch
	{
		{ power: > 0f } => data.power,
		{ runeType: RuneType.Growth } => 1.5f,
		_ => 1f
	};
}
