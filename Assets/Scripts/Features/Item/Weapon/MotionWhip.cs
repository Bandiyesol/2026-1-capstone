using UnityEngine;

/// <summary>
/// 구불거리는 선이 뻗었다 돌아오는 채찍 애니메이션 모션입니다.
/// </summary>
public class MotionWhip : MotionAnimatedMelee
{
	Vector3 desiredScale;

	protected override string AttackStateName => "effect_whip";

	protected override void OnStartMotion()
	{
		base.OnStartMotion();
		desiredScale = transform.localScale;
	}

	protected override void Update()
	{
		base.Update();

		// 채찍 애니메이션이 스케일을 되돌리므로 근접 범위 보정값을 매 프레임 유지합니다.
		if (!IsDestroyed && transform.localScale != desiredScale)
			transform.localScale = desiredScale;
	}

	public override void ResetForPool()
	{
		base.ResetForPool();
		desiredScale = Vector3.one;
	}
}