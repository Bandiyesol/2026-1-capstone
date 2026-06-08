using UnityEngine;

// 기절 전용 탄막
public class StunBossBullet : BossBullet
{
    [Header("기절 설정")]
    [SerializeField] float stunChance = 0.65f;
    [SerializeField] float stunDuration = 1.5f;

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 플레이어만 판정
        if (!collision.collider.CompareTag("Player"))
            return;

        Player player =
            collision.collider.GetComponent<Player>();

        if (player == null)
            return;

        // 확률 기절
        if (Random.value <= stunChance)
        {
            player.Stun(stunDuration);
        }

        // 탄 제거
        gameObject.SetActive(false);
    }
}