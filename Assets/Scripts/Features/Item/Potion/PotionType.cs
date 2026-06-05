/// <summary>
/// 물약 종류 정의.
/// 상점 구매 확정 시 PotionEffect.Use(PotionType)으로 호출.
/// </summary>
public enum PotionType
{
    /// <summary>최대 체력의 30% 즉시 회복</summary>
    HealthRestore,

    /// <summary>10초간 공격력 +50%, 공격 속도 +20%</summary>
    AttackBuff,

    /// <summary>10초간 피해 감소 50%</summary>
    DefenseBuff,

    /// <summary>15초간 이동 속도 +40%, 회피율 +20%</summary>
    SpeedBuff,

    /// <summary>10초간 룬 쿨타임 절반 (발동 확률 2배 효과)</summary>
    RuneBuff,
}
