using UnityEngine;

public class HomingBossBullet : BossBullet
{
    [Header("유도 설정")]

    // 추적 지속 시간
    [SerializeField] float homingDuration = 1.5f;

    // 초당 회전 속도
    [SerializeField] float rotateSpeed = 360f;

    // 플레이어
    Transform target;

    // 추적 타이머
    float homingTimer;

    protected override void OnEnable()
    {
        // 부모 초기화
        base.OnEnable();

        // 타이머 초기화
        homingTimer = 0f;

        // 플레이어 저장
        if (GameManager.instance != null)
            target = GameManager.instance.player.transform;
    }

    protected override void Update()
    {
        // 추적 시간 동안만 유도
        if (target != null &&
            homingTimer < homingDuration)
        {
            // 목표 방향 계산
            Vector2 targetDir =
                ((Vector2)target.position -
                (Vector2)transform.position).normalized;

            // 현재 각도
            float currentAngle =
                Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;

            // 목표 각도
            float targetAngle =
                Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

            // 자연스럽게 회전
            float newAngle =
                Mathf.MoveTowardsAngle(
                    currentAngle,
                    targetAngle,
                    rotateSpeed * Time.deltaTime
                );

            // 회전값을 방향 벡터로 변환
            moveDir = new Vector2(
                Mathf.Cos(newAngle * Mathf.Deg2Rad),
                Mathf.Sin(newAngle * Mathf.Deg2Rad)
            ).normalized;

            // 탄 회전 적용
            transform.rotation =
                Quaternion.Euler(0, 0, newAngle);

            // 타이머 증가
            homingTimer += Time.deltaTime;
        }

        // 부모 이동 처리
        base.Update();
    }

    public override void Init(Vector2 dir)
    {
        // 부모 초기화
        base.Init(dir);

        // 추적 타이머 초기화
        homingTimer = 0f;
    }
}