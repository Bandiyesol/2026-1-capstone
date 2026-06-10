using UnityEngine;

/// <summary>
/// ??? ???? ???? ??????? ?????? ????? ?????? ?????.
/// ??????? ???? ????? ???? ??? ????????.
/// </summary>
public class MotionSword : Motion
{
    const string AttackStateName = "motion_sword";

    Animator animationCtrl;
    SpriteRenderer spriteRenderer;
    bool isFinished;

    protected override void OnStartMotion()
    {
        animationCtrl = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        RestartAttackAnimation();
    }

    protected override float GetDefaultTime() => instance.spawntime;

    protected override bool ShouldDestroyOnHit() => false;

    protected override void Update()
    {
        base.Update();

        if (isFinished || animationCtrl == null)
            return;

        AnimatorStateInfo info = animationCtrl.GetCurrentAnimatorStateInfo(0);
        if (info.normalizedTime >= 1f && !animationCtrl.IsInTransition(0))
            OnAnimationFinished();
    }

    /// <summary>
    /// ????? ??????? ????(Animation Event)???? ????? ???? ??????? ???? ??????.
    /// </summary>
    public void OnAnimationFinished()
    {
        if (isFinished)
            return;

        if (currentActiveRune != null && currentActiveRune is IActiveDriver driver && !driver.isFinished)
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
        if (animationCtrl == null)
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
