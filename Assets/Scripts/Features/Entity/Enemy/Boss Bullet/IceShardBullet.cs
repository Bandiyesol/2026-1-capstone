using UnityEngine;

// 빙결 파편 탄막
public class IceShardBullet : BossBullet
{
    [Header("빙결 슬로우")]
    public float slowDuration = 1f;       // 슬로우 지속 시간
    public float slowMultiplier = 0.7f;   // 이동속도 배율

    protected override void OnEnable()
    {
        base.OnEnable(); // 부모 초기화 호출

        // 풀링 재사용 대비
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // -----------------------------
        // 플레이어 피격
        // -----------------------------
        if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>();

            if (player != null)
            {
                // [방어 시스템 연동] 다이렉트 체력 차감 대신 방어력/회피율 스탯 적용
                if (PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.TakeDamage(damage);
                }
                else
                {
                    GameManager.instance.Health -= damage; // 폴백
                }

                // 빙결 슬로우 적용 (Player.cs의 코루틴으로 안전하게 전달됨)
                player.ApplyIceSlow(slowMultiplier, slowDuration);
            }

            // 풀로 반환
            gameObject.SetActive(false);
            return;
        }

        // -----------------------------
        // 플레이어 공격에 맞으면 제거
        // Motion 기반 무기 전체 대응
        // (검, 활, 오브 등)
        // -----------------------------
        if (collision.gameObject.GetComponent<Motion>() != null)
        {
            gameObject.SetActive(false);
            return;
        }
    }
}