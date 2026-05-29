using UnityEngine;

// 용암 바이옴 지속 효과 (용암 위 지속 피해 및 밖으로 벗어난 후 화상 상태이상 제어)
// ※ 환경 피해 기믹이므로 플레이어의 방어력 및 회피력을 완전히 무시하고 고정 피해를 입힙니다.
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
    [SerializeField] Color lavaTint = new Color(1f, 0.45f, 0.45f, 1f);

    [Header("화상 틱 간격")]
    [SerializeField] float burnTickInterval = 0.5f;

    // 화상 틱 타이머
    float burnTickTimer;

    // 화상 남은 시간
    float burnTimer;

    protected override void ApplyEffect()
    {
        // 바이옴 효과 최초 진입 시 타이머 초기화
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

        // 현재 플레이어가 용암 타일 위에 딛고 서 있는지 체크
        bool onLava = player.IsOnLava();

        // [구역 판정 1] 용암 위에 있는 상태
        if (onLava)
        {
            // [기획 수정] 방어력 및 회피 스탯을 무시하기 위해 GameManager의 Health를 다이렉트로 감산
            GameManager.instance.Health -= lavaDamagePerSecond * Time.deltaTime;

            // 화상 디버프 지속 타이머를 최대치로 실시간 갱신
            burnTimer = burnDuration;

            // 용암 위에서는 대미지 연출용 붉은 색상 고정 유지
            player.SetStatusTint(lavaTint);

            return;
        }

        // [구역 판정 2] 용암 밖으로 벗어난 화상(Burn) 잔여 상태
        if (burnTimer > 0f)
        {
            // 화상 유지 시간 및 내부 틱 타이머 감소
            burnTimer -= Time.deltaTime;
            burnTickTimer -= Time.deltaTime;

            // 설정된 틱 간격(burnTickInterval)마다 누적 피해 연산 실행
            if (burnTickTimer <= 0f)
            {
                // [기획 수정] 화상 디버프 고유 틱 대미지도 방어력을 무시하고 고정 수치로 차감
                GameManager.instance.Health -= burnDamagePerSecond;

                // 다음 틱 타이머 리셋
                burnTickTimer = burnTickInterval;
            }

            // 용암 밖 화상 상태일 때 플레이어 스프라이트가 빨간색으로 깜빡이는 핑퐁 연출 (유지)
            float blink = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            Color color = Color.Lerp(player.defaultTint, lavaTint, blink);
            player.SetStatusTint(color);
        }
        else
        {
            // 화상 지속 시간이 완전히 종료되면 틴트 컬러 기본값 복구
            player.ResetStatusTint();
        }
    }
}