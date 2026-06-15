using UnityEngine;

/// <summary>룬 선택 카드에 표시할 간단 한국어 설명.</summary>
public static class RuneDescriptionDefaults
{
	public static string GetDescription(RuneData rune)
	{
		if (rune == null || rune.runeType == RuneType.None)
			return string.Empty;

		return rune.runeType switch
		{
			RuneType.Orbit => "원 궤도로 이동하며 공격",
			RuneType.Wave => "물결 궤도로 이동하며 공격",
			RuneType.Spiral => "나선 궤도로 이동하며 공격",
			RuneType.Homing => "가장 가까운 적을 추적하며 공격",

			RuneType.Split => $"충돌 시 {N(RuneDataAccess.GetSpawnsPerTrigger(rune))}개로 분열",
			RuneType.Ricochet => $"충돌 시 {N(RuneDataAccess.GetBounceCount(rune))}번 튕김",
			RuneType.Vampire => "충돌 데미지로 스탯 회복",
			RuneType.Freeze => $"충돌 시 주변 적 {Sec(RuneDataAccess.GetFreezeDuration(rune))} 빙결",
			RuneType.Chain => $"충돌 시 주변 적 {N(RuneDataAccess.GetChainCount(rune))}명 연쇄 피해",
			RuneType.Explode => $"충돌 시 반경 {N(RuneDataAccess.GetExplodeRadius(rune))} 폭발",

			RuneType.Recursion => "소멸 시 동일 탄환 재생성",

			RuneType.Gravity => "블랙홀 방향으로 적 끌어당김",
			RuneType.Growth => "크기·데미지 점점 증폭",

			RuneType.Blink => $"{Sec(RuneDataAccess.GetInterval(rune))}마다 {N(RuneDataAccess.GetLogicDistance(rune))} 워프",

			RuneType.Boing => "화면 끝에서 반대편으로 이동",

			_ => string.Empty,
		};
	}

	public static string GetBrief(RuneType type)
	{
		if (type == RuneType.None)
			return string.Empty;

		var stub = ScriptableObject.CreateInstance<RuneData>();
		stub.runeType = type;
		return GetDescription(stub);
	}

	static string Sec(float seconds) => $"{N(seconds)}초";

	static string N(float value)
	{
		if (Mathf.Approximately(value % 1f, 0f))
			return ((int)value).ToString();
		return value.ToString("0.#");
	}

	static string N(int value) => value.ToString();
}
