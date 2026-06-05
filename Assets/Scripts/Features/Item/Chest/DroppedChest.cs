using System.Collections;
using UnityEngine;

/// <summary>
/// 바닥에 떨어진 상자. 플레이어가 닿으면 Open 애니메이션 재생 후 무기 선택 UI를 띄웁니다.
/// Animator에 Open Trigger가 있으면 사용하고, 없으면 Open 상태 또는 기본 상태를 재생합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DroppedChest : MonoBehaviour
{
    const string OpenStateName = "Open";
    const string OpenTriggerName = "Open";

    [SerializeField] ChestGrade grade = ChestGrade.Normal;

    ChestDropSettings settings;
    Collider2D col;
    Animator anim;
    bool isOpening;
    bool openAnimationFinished;

    public ChestGrade Grade => grade;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        anim = GetComponent<Animator>();
    }

    public void Setup(ChestGrade chestGrade, ChestDropSettings dropSettings)
    {
        grade = chestGrade;
        settings = dropSettings;
        isOpening = false;
        openAnimationFinished = false;
        col.enabled = true;

        if (anim != null)
        {
            anim.updateMode = AnimatorUpdateMode.Normal;
            RestartDefaultAnimation();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpening || !GameManager.instance.isLive)
            return;

        if (other.GetComponent<Player>() == null)
            return;

        StartCoroutine(OpenSequence());
    }

    IEnumerator OpenSequence()
    {
        isOpening = true;
        col.enabled = false;
        GameManager.instance.isLive = false;

        if (anim != null)
        {
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            PlayOpenAnimation();
            yield return WaitForOpenAnimation();
            anim.updateMode = AnimatorUpdateMode.Normal;
        }
        else
        {
            float wait = settings != null ? settings.openAnimationFallbackSeconds : 0.6f;
            yield return new WaitForSecondsRealtime(wait);
        }

        // RewardRollService로 무기·악세사리·성물 후보 3개 뽑기
        if (RewardRollService.instance != null)
        {
            var candidates = RewardRollService.instance.Roll(grade);

            // 비활성화 상태 포함해서 RewardSelectUI 탐색
            RewardSelectUI rewardUi = RewardSelectUI.GetOrFind();

            if (rewardUi != null)
                rewardUi.Show(candidates);
            else
            {
                // RewardSelectUI 없으면 기존 WeaponSelectUI로 폴백
                WeaponSelectUI weaponUi = GameManager.instance.uiWeaponSelect;
                if (weaponUi != null)
                {
                    string[] pool = settings != null ? settings.GetWeaponPool(grade) : null;
                    weaponUi.ShowFromChest(grade, pool);
                }
                else
                {
                    Debug.LogWarning("[DroppedChest] RewardSelectUI, WeaponSelectUI 모두 없어 게임을 재개합니다.");
                    GameManager.instance.Resume();
                }
            }
        }
        else
        {
            // RewardRollService 없으면 기존 WeaponSelectUI로 폴백
            WeaponSelectUI weaponUi = GameManager.instance.uiWeaponSelect;
            if (weaponUi != null)
            {
                string[] pool = settings != null ? settings.GetWeaponPool(grade) : null;
                weaponUi.ShowFromChest(grade, pool);
            }
            else
            {
                Debug.LogWarning("[DroppedChest] RewardRollService가 없어 게임을 재개합니다.");
                GameManager.instance.Resume();
            }
        }

        gameObject.SetActive(false);
    }

    void RestartDefaultAnimation()
    {
        anim.Rebind();
        anim.Update(0f);
        anim.Play(0, 0, 0f);
    }

    void PlayOpenAnimation()
    {
        if (HasTrigger(OpenTriggerName))
        {
            anim.ResetTrigger(OpenTriggerName);
            anim.SetTrigger(OpenTriggerName);
            return;
        }

        if (TryPlayState(OpenStateName))
            return;

        anim.Play(0, 0, 0f);
    }

    bool HasTrigger(string triggerName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger && param.name == triggerName)
                return true;
        }

        return false;
    }

    bool TryPlayState(string stateName)
    {
        int hash = Animator.StringToHash(stateName);
        if (!anim.HasState(0, hash))
            return false;

        anim.Play(hash, 0, 0f);
        return true;
    }

    IEnumerator WaitForOpenAnimation()
    {
        openAnimationFinished = false;
        float fallback = settings != null ? settings.openAnimationFallbackSeconds : 0.6f;
        float elapsed = 0f;

        yield return null;

        while (elapsed < fallback + 2f)
        {
            if (openAnimationFinished)
                yield break;

            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            if (info.normalizedTime >= 0.99f && !anim.IsInTransition(0))
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    /// <summary>Animation Event에서 호출 (열림 클립 마지막 프레임)</summary>
    public void NotifyOpenFinished()
    {
        openAnimationFinished = true;
    }
}