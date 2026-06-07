using UnityEngine;

// 용암 바이옴 지속 효과
// 용암 위 즉시 피해 +
// 이탈 시 플레이어 화상 시스템 호출
public class LavaEffect : BiomeEffect
{
    [Header("용암 초당 피해")]
    [SerializeField] float lavaDamagePerSecond = 8f;

    [Header("화상 설정")]
    [SerializeField] float burnDuration = 3f;

    [SerializeField]
    float burnTickDamage = 2f;

    [SerializeField]
    float burnTickInterval = 0.5f;

    [Header("화상 연출")]
    [SerializeField]
    float blinkSpeed = 10f;

    bool wasOnLava; // 직전 프레임 용암 여부

    protected override void ApplyEffect()
    {
        // 최초 진입 초기화
        wasOnLava = false;
    }

    protected override void RemoveEffect()
    {
        // 바이옴 벗어나면 색 복구
        if (player != null)
            player.ResetStatusTint();
    }

    protected override void EffectUpdate()
    {
        if (player == null)
            return;

        // 현재 용암 위 판정
        bool onLava = player.IsOnLava();

        // 용암 위에 있는 동안
        if (onLava)
        {
            // 방어 무시 지속 피해
            GameManager.instance.Health -= lavaDamagePerSecond * Time.deltaTime;
        }

        // 방금 용암에서 벗어났다면
        if (wasOnLava && !onLava)
        {
            // 플레이어 화상 시스템 호출
            player.ApplyBurn(burnDuration, burnTickDamage, burnTickInterval, blinkSpeed);
        }

        // 상태 저장
        wasOnLava = onLava;
    }
}