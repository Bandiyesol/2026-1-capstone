using UnityEngine;

/// <summary>
/// 구불거리는 선이 뻗었다 돌아오는 채찍 애니메이션 모션입니다.
/// </summary>
public class MotionWhip : MotionAnimatedMelee
{
    protected override string AttackStateName => "effect_whip";

    protected override void OnStartMotion()
    {
        base.OnStartMotion();
        Debug.Log($"[채찍] size={instance.size}, scale={transform.localScale}");
    }
}