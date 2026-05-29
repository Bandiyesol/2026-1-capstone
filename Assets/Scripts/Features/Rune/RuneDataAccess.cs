using UnityEngine;

// [RuneDataAccess.cs]
// 형변환이나 패턴 매칭을 통해 하위 타입의 룬 데이터에 안전하게 접근하는 유틸 클래스
public static class RuneDataAccess
{
	const float DefaultActiveDuration = 3f;
	const int DefaultBounceCount = 3;

	// 룬 종류별로 '지속 시간'을 가져옴 (설정값이 없으면 기본값 반환)
	public static float GetDuration(RuneData data) => data switch
	{
		ActiveRuneData a when a.duration > 0f => a.duration,
		GravityRuneData g when g.duration > 0f => g.duration,
		{ category: RuneCategory.Active } => DefaultActiveDuration,
		_ => 0f
	};

	// 룬 종류별 '속도 배율' 가져오기
	public static float GetSpeedMultiplier(RuneData data) => data switch
	{
		ActiveRuneData a when a.speedMultiplier > 0f => a.speedMultiplier,
		_ => 1f
	};

	// 룬 종류별 '적용 범위(반경)' 가져오기
	public static float GetAffectedRange(RuneData data) => data switch
	{
		ActiveRuneData a when a.affectedRange > 0f => a.affectedRange,
		_ => 0f
	};

	// 룬의 내부 재사용 대기시간(쿨타임) 가져오기
	public static float GetInterval(RuneData data) => data switch
	{
		RicochetRuneData r when r.interval > 0f => r.interval,
		ExplodeRuneData e when e.interval > 0f => e.interval,
		FreezeRuneData f when f.interval > 0f => f.interval,
		LogicRuneData l when l.interval > 0f => l.interval,
		_ => 0f
	};

	// 분열: 트리거 1회당 복제할 총알 수
	public static int GetSpawnsPerTrigger(RuneData data) => data switch
	{
		SplitRuneData s when s.spawnsPerTrigger > 0 => s.spawnsPerTrigger,
		{ runeType: RuneType.Split } => 3, // 기본 3갈래
		_ => 0
	};

	// 도탄: 최대 튕기는 횟수
	public static int GetBounceCount(RuneData data) => data switch
	{
		RicochetRuneData r when r.bounceCount > 0 => r.bounceCount,
		{ runeType: RuneType.Ricochet } => DefaultBounceCount,
		_ => 0
	};
}