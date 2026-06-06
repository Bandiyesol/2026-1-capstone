using System.Collections;
using UnityEngine;

/// <summary>
/// 악세사리 특수 효과 실행 클래스.
/// 단순 스탯형은 AccessoryManager에서 처리하고,
/// 특수 로직이 필요한 효과는 이 클래스에서 구현한다.
/// </summary>
public class AccessoryEffect : MonoBehaviour
{
    public static AccessoryEffect instance;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    /// <summary>
    /// 특수 효과를 활성화한다.
    /// AccessoryManager.Add()에서 effectType이 None이 아닐 때 호출.
    /// </summary>
    public void Activate(AccessoryEffectType effectType, AccessoryData data)
    {
        switch (effectType)
        {
            case AccessoryEffectType.AutoHeal:
                StartCoroutine(AutoHealRoutine());
                break;

            default:
                Debug.Log($"[AccessoryEffect] '{effectType}' 효과는 아직 구현되지 않았습니다.");
                break;
        }
    }

    // ───────────────────────────────────────────
    //  구현된 특수 효과
    // ───────────────────────────────────────────

    /// <summary>약초 꾸러미 : 5초마다 체력 2 자동 회복</summary>
    IEnumerator AutoHealRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.Heal(2f);
        }
    }
}
