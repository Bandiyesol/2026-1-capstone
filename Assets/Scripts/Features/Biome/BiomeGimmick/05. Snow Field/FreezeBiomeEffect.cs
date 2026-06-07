using UnityEngine;

// 동결 바이옴 효과 (추위에 지속 노출 시 지연 후 이동속도 감속 및 색상 변경)
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

    // 추위 노출 누적 시간
    float exposureTime;

    // 모닥불 등 따뜻한 구역(WarmZone) 체류 여부
    bool inWarmZone;

    protected override void ApplyEffect()
    {
        if (player == null)
            return;

        // 바이옴 효과 최초 진입 시 상태 초기화
        exposureTime = 0f;
        inWarmZone = false;
    }

    protected override void RemoveEffect()
    {
        if (player == null)
            return;

        // [에러 수정] 효과가 전면 해제될 때 플레이어 이동 속도 배율을 기본값(1.0)으로 정상화
        player.moveSpeedMultiplier = 1f;

        // 동결 전용 틴트 컬러 제거 및 원래 색상 복구
        player.ResetStatusTint();
    }

    protected override void EffectUpdate()
    {
        if (player == null)
            return;

        // [구역 판정] 현재 따뜻한 곳에 있는지 여부에 따른 추위 수치 증감 처리
        if (inWarmZone)
        {
            // 따뜻한 구역: 초당 설정된 회복 속도만큼 추위 게이지 차감
            exposureTime -= recoverPerSecond * Time.deltaTime;
            exposureTime = Mathf.Max(0f, exposureTime); // 음수 누적 방지
        }
        else
        {
            // 추운 구역: 시간에 따라 추위 지속 누적
            exposureTime += Time.deltaTime;
        }

        // [지연 검사] 아직 감속이 시작되는 타임라인(exposureDelay)에 도달하지 않은 경우
        if (exposureTime < exposureDelay)
        {
            // [에러 수정] 감속 전에는 속도 배율을 정상(1.0)으로 유지
            player.moveSpeedMultiplier = 1f;
            return;
        }

        // 지연 시간을 뺀 순수 동결 진행 시간 계산
        float freezeTime = exposureTime - exposureDelay;

        // [디버프 연산] 초당 감속량에 맞춰 떨어지는 배율 계산 (최소 속도 한계치 적용)
        float targetMultiplier = Mathf.Max(minSpeedMultiplier, 1f - (freezeTime * slowPerSecond));

        // [에러 수정] 계산된 동결 속도 배율을 플레이어 스크립트에 실시간 주입
        player.moveSpeedMultiplier = targetMultiplier;
    }

    // 외부 캠프파이어, 열원 오브젝트 등에서 트리거 진입 시 호출
    public void EnterWarmZone()
    {
        inWarmZone = true;
    }

    // 따뜻한 구역을 벗어날 때 호출
    public void ExitWarmZone()
    {
        inWarmZone = false;
    }
}