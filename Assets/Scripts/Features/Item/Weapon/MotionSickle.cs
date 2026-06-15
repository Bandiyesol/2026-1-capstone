using UnityEngine;

/// <summary>
/// 플레이어 앞쪽을 반원으로 베는 낫 애니메이션 모션입니다.
/// </summary>
public class MotionSickle : MotionAnimatedMelee
{
	protected override string AttackStateName => "effect_sickle";
}