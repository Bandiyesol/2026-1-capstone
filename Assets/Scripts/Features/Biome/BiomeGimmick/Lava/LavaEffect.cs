using UnityEngine;

// 용암 바이옴 지속 효과
public class LavaEffect : BiomeEffect
{
    [Header("용암 초당 피해")]
    [SerializeField] float lavaDamagePerSecond = 8f;

    [Header("화상 지속 시간")]
    [SerializeField] float burnDuration = 3f;

    [Header("화상 초당 피해")]
    [SerializeField] float burnDamagePerSecond = 2f;

    [Header("깜빡임 속도")]
    [SerializeField] float blinkSpeed = 10f;

    [Header("용암 색")]
    [SerializeField]
    Color lavaTint =
        new Color(1f, 0.45f, 0.45f, 1f);

    [Header("화상 틱 간격")]
    [SerializeField] float burnTickInterval = 0.5f;

    // 화상 틱 타이머
    float burnTickTimer;

    // 화상 남은 시간
    float burnTimer;

    protected override void ApplyEffect()
    {
        burnTimer = 0f;
        burnTickTimer = burnTickInterval;
    }

    protected override void RemoveEffect()
    {
        if (player != null)
            player.ResetStatusTint();
    }

    protected override void EffectUpdate()
    {
        if (player == null)
            return;

        // 용암 위 여부
        bool onLava = player.IsOnLava();

        // 용암 위
        if (onLava)
        {
            // 용암 지속 피해
            GameManager.instance.Health -=
                lavaDamagePerSecond * Time.deltaTime;

            // 화상 시간 갱신
            burnTimer = burnDuration;

            // 용암 위에서는 계속 빨간색 유지
            player.SetStatusTint(lavaTint);

            return;
        }

        // 용암 밖 화상 상태
        if (burnTimer > 0f)
        {
            // 화상 시간 감소
            burnTimer -= Time.deltaTime;

            // 틱 타이머 감소
            burnTickTimer -= Time.deltaTime;

            // 일정 시간마다 화상 피해
            if (burnTickTimer <= 0f)
            {
                GameManager.instance.Health -=
                    burnDamagePerSecond;

                // 다음 틱 초기화
                burnTickTimer = burnTickInterval;
            }

            // 밖에서는 빨간색 깜빡임
            float blink =
                Mathf.PingPong(
                    Time.time * blinkSpeed,
                    1f
                );

            Color color =
                Color.Lerp(
                    player.defaultTint,
                    lavaTint,
                    blink
                );

            player.SetStatusTint(color);
        }
        else
        {
            // 완전 종료 시 원래 색 복구
            player.ResetStatusTint();
        }
    }
}