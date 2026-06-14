using UnityEngine;

/// <summary>
/// 내려찍기 애니메이션을 재생하는 망치 모션입니다.
/// 클립에서는 망치 타격과 바닥 균열 이펙트를 함께 키프레임으로 넣습니다.
/// </summary>
public class MotionHammer : MotionAnimatedMelee
{
	protected override string AttackStateName => "effect_hammer";
}
