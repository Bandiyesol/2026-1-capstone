using UnityEngine;

/// <summary>
/// 검처럼 애니메이션 재생이 끝나면 사라지는 근접 무기 공통 모션입니다.
/// </summary>
public abstract class MotionAnimatedMelee : Motion
{
	Animator animationCtrl;
	SpriteRenderer spriteRenderer;
	bool isFinished;
	bool animationStarted;

	protected abstract string AttackStateName { get; }

	protected override void OnStartMotion()
	{
		animationCtrl = GetComponent<Animator>();
		spriteRenderer = GetComponent<SpriteRenderer>();
		isFinished = false;
		animationStarted = false;
		// SetActive(true) 이전에 Animator.Play()를 호출하면 Unity 경고가 발생하므로
		// 첫 번째 Update에서 재생을 시작한다.
	}

	protected override float GetDefaultTime() => instance.spawntime;

	protected override bool ShouldDestroyOnHit() => false;

	protected override void Update()
	{
		// 오브젝트가 활성화된 첫 프레임에 애니메이션을 시작한다.
		if (!animationStarted)
		{
			animationStarted = true;
			RestartAttackAnimation();
		}

		base.Update();

		if (IsDestroyed || isFinished || animationCtrl == null || animationCtrl.runtimeAnimatorController == null)
			return;

		AnimatorStateInfo info = animationCtrl.GetCurrentAnimatorStateInfo(0);
		if (info.normalizedTime >= 1f && !animationCtrl.IsInTransition(0))
			OnAnimationFinished();
	}

	/// <summary>
	/// Animation Event에서 직접 호출해도 되고, 이벤트가 없으면 normalizedTime으로 자동 호출됩니다.
	/// </summary>
	public void OnAnimationFinished()
	{
		if (isFinished)
			return;

		if (currentActiveRune is IActiveDriver driver && !driver.isFinished)
		{
			RestartAttackAnimation();
			return;
		}

		isFinished = true;
		HideVisual();
		RequestDestroy(DestroyReason.WeaponLogic);
	}

	void RestartAttackAnimation()
	{
		if (animationCtrl == null || animationCtrl.runtimeAnimatorController == null)
			return;

		int hash = Animator.StringToHash(AttackStateName);
		if (animationCtrl.HasState(0, hash))
			animationCtrl.Play(hash, 0, 0f);
		else
			animationCtrl.Play(0, 0, 0f);
	}

	void HideVisual()
	{
		if (spriteRenderer != null)
			spriteRenderer.enabled = false;

		foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>(true))
		{
			trail.Clear();
			trail.emitting = false;
			trail.enabled = false;
		}
	}

	protected override bool CanDestroyNow(DestroyReason reason)
	{
		// Explode 등 트리거 룬은 애니메이션 중에도 즉시 파괴 허용
		if (reason == DestroyReason.TriggerRune)
			return true;

		return isFinished || life <= 0f;
	}

	public override void ResetForPool()
	{
		base.ResetForPool();
		isFinished = false;
		animationStarted = false;

		foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>(true))
		{
			trail.Clear();
			trail.emitting = true;
			trail.enabled = true;
		}

		if (animationCtrl != null)
		{
			animationCtrl.Rebind();
			animationCtrl.Update(0f);
		}
	}
}
