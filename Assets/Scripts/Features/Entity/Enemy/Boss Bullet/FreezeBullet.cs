using UnityEngine;

// 빙결 탄막
public class FreezeBullet : BossBullet
{
    [Header("빙결")]
    [SerializeField] float slowDuration = 2f;      // 슬로우 지속시간
    [SerializeField] float slowMultiplier = 0.7f;  // 이동속도 배율

    [Range(0f, 1f)]
    [SerializeField] float freezeChance = 0.3f;    // 빙결 확률

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 플레이어 충돌
        if (collision.collider.CompareTag("Player"))
        {
            // 피해 적용
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.TakeDamage(damage);

            // 확률적으로 빙결 적용
            if (Random.value <= freezeChance)
            {
                Player player = collision.collider.GetComponent<Player>();

                if (player != null)
                {
                    player.ApplyIceSlow(
                        slowDuration,
                        slowMultiplier
                    );
                }
            }

            gameObject.SetActive(false);
            return;
        }

        // 다른 오브젝트 충돌 시 제거
        gameObject.SetActive(false);
    }
}