using UnityEngine;

// 화상 보스 탄막
public class BurnBossBullet : BossBullet
{
    [Header("화상 설정")]
    [SerializeField] float burnDuration = 3f;       // 화상 지속 시간
    [SerializeField] float burnTickDamage = 2f;     // 틱 피해
    [SerializeField] float burnTickInterval = 0.5f; // 틱 간격

    [Header("화상 연출")]
    [SerializeField] float blinkSpeed = 10f; // 깜빡임 속도

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 플레이어만 처리
        if (!collision.gameObject.CompareTag("Player"))
            return;

        // 플레이어 참조 획득
        Player player =
            collision.gameObject.GetComponent<Player>();

        // 없으면 종료
        if (player == null)
            return;

        // 플레이어 내부 화상 시스템 호출
        player.ApplyBurn(burnDuration, burnTickDamage, burnTickInterval, blinkSpeed);

        // 탄 반환
        gameObject.SetActive(false);
    }

    protected override void OnEnable()
    {
        // 부모 초기화 실행
        base.OnEnable();
    }
}