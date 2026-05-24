using UnityEngine;

// 공허 바이옴 효과
public class VoidBiomeEffect : BiomeEffect
{
    [Header("반전 지속 시간")]
    [SerializeField] float invertedDuration = 10f;

    [Header("정상 지속 시간")]
    [SerializeField] float normalDuration = 7.5f;

    [Header("조작 흔들림 강도")]
    [SerializeField] float inputJitter = 0.14f;

    [Header("카메라 흔들림 강도")]
    [SerializeField] float cameraShakeAmount = 0.045f;

    [Header("공허 색")]
    [SerializeField] Color voidTint = new Color(0.78f, 0.66f, 1f, 1f);

    // 상태 전환 타이머
    float timer;

    // 현재 반전 상태인지
    bool isInverted;

    // 축 반전 여부
    bool invertX;
    bool invertY;

    // 카메라 캐싱
    Camera cam;
    Vector3 cameraOrigin;

    protected override void ApplyEffect()
    {
        if (player == null)
            return;

        timer = 0f;

        cam = Camera.main;

        if (cam != null)
            cameraOrigin = cam.transform.localPosition;

        // 시작은 반전 상태
        EnterInvertedState();
    }

    protected override void RemoveEffect()
    {
        if (player == null)
            return;

        invertX = false;
        invertY = false;
        isInverted = false;

        // 입력 효과 제거
        player.inputModifier = Vector2.one;
        player.inputJitter = 0f;

        // 색 복구
        player.ResetStatusTint();

        // 카메라 복구
        if (cam != null)
            cam.transform.localPosition = cameraOrigin;
    }

    protected override void EffectUpdate()
    {
        if (player == null)
            return;

        timer += Time.deltaTime;

        // 반전 ↔ 정상 리듬 전환
        if (isInverted)
        {
            if (timer >= invertedDuration)
                EnterNormalState();
        }
        else
        {
            if (timer >= normalDuration)
                EnterInvertedState();
        }

        // 반전 상태일 때만 혼란 적용
        if (isInverted)
        {
            ApplyInputChaos();
            ApplyCameraShake();
        }
        else
        {
            RestoreCamera();
        }
    }

    // 반전 상태 진입
    void EnterInvertedState()
    {
        timer = 0f;
        isInverted = true;

        RandomizeDirection();

        player.SetStatusTint(voidTint);
    }

    // 정상 상태 진입
    void EnterNormalState()
    {
        timer = 0f;
        isInverted = false;

        invertX = false;
        invertY = false;

        player.inputModifier = Vector2.one;
        player.inputJitter = 0f;

        player.ResetStatusTint();
    }

    // 어느 축을 반전할지 결정
    void RandomizeDirection()
    {
        invertX = Random.value < 0.5f;
        invertY = Random.value < 0.5f;

        // 최소 하나는 반드시 반전
        if (!invertX && !invertY)
        {
            if (Random.value < 0.5f)
                invertX = true;
            else
                invertY = true;
        }
    }

    // 입력 혼란 적용
    void ApplyInputChaos()
    {
        player.inputModifier = new Vector2(
            invertX ? -1f : 1f,
            invertY ? -1f : 1f
        );

        player.inputJitter = inputJitter;
    }

    // 카메라 흔들림
    void ApplyCameraShake()
    {
        if (cam == null)
            return;

        Vector2 offset = Random.insideUnitCircle * cameraShakeAmount;

        cam.transform.localPosition =
            cameraOrigin + new Vector3(offset.x, offset.y, 0f);
    }

    // 카메라 복구
    void RestoreCamera()
    {
        if (cam == null)
            return;

        cam.transform.localPosition = cameraOrigin;
    }
}
