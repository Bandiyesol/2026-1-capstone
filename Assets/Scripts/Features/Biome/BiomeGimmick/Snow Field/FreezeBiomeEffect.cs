using UnityEngine;

// 동결 바이옴 효과
public class FreezeBiomeEffect : BiomeEffect
{
    [Header("감속 시작 시간")]
    [SerializeField] float exposureDelay = 2f;

    [Header("초당 감속량")]
    [SerializeField] float slowPerSecond = 0.35f;

    [Header("최소 속도 배율")]
    [SerializeField] float minSpeedMultiplier = 0.45f;

    [Header("회복 속도")]
    [SerializeField] float recoverPerSecond = 1.5f;

    [Header("빙결 색상")]
    [SerializeField]
    Color freezeTint =
        new Color(0.72f, 0.9f, 1f, 1f);

    // 추위 노출 시간
    float exposureTime;

    // 따뜻한 구역 여부
    bool inWarmZone;

    protected override void ApplyEffect()
    {
        if (player == null)
            return;

        // 초기화
        exposureTime = 0f;
        inWarmZone = false;
    }

    protected override void RemoveEffect()
    {
        if (player == null)
            return;

        // 속도 복구
        player.speed = player.baseSpeed;

        // 색상 복구
        player.ResetStatusTint();
    }

    protected override void EffectUpdate()
    {
        if (player == null)
            return;

        // 따뜻한 곳이면 회복
        if (inWarmZone)
        {
            exposureTime -=
                recoverPerSecond * Time.deltaTime;

            // 음수 방지
            exposureTime =
                Mathf.Max(0f, exposureTime);
        }
        else
        {
            // 추위 누적
            exposureTime += Time.deltaTime;
        }

        // 아직 감속 전
        if (exposureTime < exposureDelay)
        {
            player.speed = player.baseSpeed;

            player.ResetStatusTint();

            return;
        }

        // 빙결 색상 적용
        player.SetStatusTint(freezeTint);

        // 실제 빙결 시간
        float freezeTime =
            exposureTime - exposureDelay;

        // 감속 계산
        float multiplier = Mathf.Max(
            minSpeedMultiplier,
            1f - freezeTime * slowPerSecond
        );

        // 속도 적용
        player.speed =
            player.baseSpeed * multiplier;
    }

    // WarmZone 진입
    public void EnterWarmZone()
    {
        inWarmZone = true;
    }

    // WarmZone 이탈
    public void ExitWarmZone()
    {
        inWarmZone = false;
    }
}