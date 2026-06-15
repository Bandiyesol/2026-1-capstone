using UnityEngine;

/// <summary>
/// 심연의 군주 촉수 — 적이 닿으면 데미지 적용
/// TentacleEffect 프리팹에 붙여서 사용
/// </summary>
public class TentacleDamage : MonoBehaviour
{
    [Tooltip("틱 피해")]          public float damagePerTick   = 100f;
    [Tooltip("틱 간격(초)")]      public float tickInterval    = 0.3f;
    [Tooltip("흡혈 비율 (최대체력 %)")]public float lifeStealRatio = 0.01f;

    void OnTriggerStay2D(Collider2D other)
    {
        Enemy e = other.GetComponent<Enemy>();
        if (e == null || !e.IsLive) return;

        // 틱 간격으로 데미지 적용
        e.TakeDamage(damagePerTick * Time.deltaTime / tickInterval);

        // 흡혈
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.Heal(PlayerStats.Instance.MaxHP * lifeStealRatio * Time.deltaTime / tickInterval);
    }
}
