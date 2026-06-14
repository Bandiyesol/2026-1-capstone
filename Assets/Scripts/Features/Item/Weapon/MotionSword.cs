using UnityEngine;

/// <summary>
/// 검 공격 애니메이션을 재생하는 근접 무기 모션입니다.
/// </summary>
public class MotionSword : MotionAnimatedMelee
{
	protected override string AttackStateName => "effect_sword";
}
