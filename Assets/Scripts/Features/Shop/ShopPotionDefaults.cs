using UnityEngine;

/// <summary>PotionData SO가 없을 때 상점·인벤 기본 물약 정의.</summary>
public static class ShopPotionDefaults
{
	public static string GetDisplayName(PotionType type) => type switch
	{
		PotionType.HealthRestore => "회복 물약",
		PotionType.AttackBuff => "공격 강화 물약",
		PotionType.DefenseBuff => "방어 강화 물약",
		PotionType.SpeedBuff => "신속 물약",
		PotionType.RuneBuff => "룬 가속 물약",
		_ => type.ToString()
	};

	public static string GetDescription(PotionType type) => type switch
	{
		PotionType.HealthRestore => "최대 HP의 30%를 즉시 회복합니다.",
		PotionType.AttackBuff => "10초간 공격력 +50%, 공격 속도 +20%",
		PotionType.DefenseBuff => "10초간 피해 감소 +50%",
		PotionType.SpeedBuff => "15초간 이동 속도 +40%, 회피 +20%",
		PotionType.RuneBuff => "10초간 룬 쿨타임 절반",
		_ => string.Empty
	};

	public static int GetDefaultPrice(PotionType type) => type switch
	{
		PotionType.HealthRestore => 25,
		PotionType.RuneBuff => 45,
		_ => 35
	};
}
