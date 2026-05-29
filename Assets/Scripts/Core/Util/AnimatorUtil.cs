using UnityEngine;

public static class AnimatorUtil
{
    /// <summary>Animator Controller의 기본(Entry) 상태를 처음부터 재생합니다.</summary>
    public static void PlayDefaultState(Animator animator, int layer = 0)
    {
        if (animator == null)
            return;

        animator.Play(0, layer, 0f);
    }

    /// <summary>풀 재사용 시 애니메이션을 초기화한 뒤 기본 상태를 재생합니다.</summary>
    public static void RestartDefaultState(Animator animator, int layer = 0)
    {
        if (animator == null)
            return;

        animator.Rebind();
        animator.Update(0f);
        PlayDefaultState(animator, layer);
    }

    public static bool TryPlayState(Animator animator, string stateName, int layer = 0, float normalizedTime = 0f)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
            return false;

        int hash = Animator.StringToHash(stateName);
        if (!animator.HasState(layer, hash))
            return false;

        animator.Play(hash, layer, normalizedTime);
        return true;
    }

    public static void PlayStateOrDefault(Animator animator, string stateName, int layer = 0, float normalizedTime = 0f)
    {
        if (!TryPlayState(animator, stateName, layer, normalizedTime))
            PlayDefaultState(animator, layer);
    }

    public static bool HasTrigger(Animator animator, string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName))
            return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger && param.name == triggerName)
                return true;
        }

        return false;
    }
}
