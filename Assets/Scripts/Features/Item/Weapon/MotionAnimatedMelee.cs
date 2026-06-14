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

	/// <summary>
	/// 액티브 룬이 없으면 무기가 향한 방향(로컬 오른쪽)으로 전진합니다.
	/// 액티브 룬이 있으면 룬이 이동을 제어합니다.
	/// </summary>
	protected override void UpdateMovement()
	{
		base.UpdateMovement();

		// IActiveDriver 룬이 이동을 제어 중이면 추가 이동 없음
		if (currentActiveRune is IActiveDriver)
			return;

		// 향한 방향(로컬 X축 = 스폰 시 rotation 기준 정면)으로 전진
		transform.Translate(Vector3.right * instance.movespeed * Time.deltaTime, Space.Self);
	}

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

	protected override bool ActuallyDestroy()
	{
		if (!base.ActuallyDestroy())
			return false;

		return isFinished || life <= 0f;
	}

	public override void ResetForPool()
	{
		base.ResetForPool();
		isFinished = false;
		animationStarted = false;

		if (spriteRenderer != null)
			spriteRenderer.enabled = true;

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
