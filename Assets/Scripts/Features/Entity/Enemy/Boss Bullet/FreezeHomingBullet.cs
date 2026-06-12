using UnityEngine;

// 빙결 유도 탄막 (재앙의 씨앗 패턴용)
public class FreezeHomingBullet : BossBullet
{
    [Header("빙결 설정")]
    [SerializeField] float slowDuration = 2f;      // 슬로우 지속시간
    [SerializeField] float slowMultiplier = 0.7f;  // 이동속도 배율
    [Range(0f, 1f)]
    [SerializeField] float freezeChance = 0.3f;    // 빙결 확률

    [Header("유도 설정")]
    [SerializeField] float homingDuration = 1.5f;  // 추적 지속 시간
    [SerializeField] float rotateSpeed = 360f;     // 초당 회전 속도
    [SerializeField] float homingStartDelay;       // 대기 시간

    Transform target;      // 플레이어 Target
    float homingTimer;     // 추적 타이머

    protected override void OnEnable()
    {
        // 부모 초기화 및 타이머 초기화 (HomingBossBullet 구조 반영)
        base.OnEnable();
        homingTimer = 0f;

        // 플레이어 트랜스폼 캐싱
        if (GameManager.instance != null && GameManager.instance.player != null)
            target = GameManager.instance.player.transform;
    }

    protected override void Update()
    {
        // 대기 시간 카운트
        homingTimer += Time.deltaTime;

        // 대기 시간이 지나고 유도 가능 시간 안에 있을 때만 플레이어 추적
        if (target != null &&
            homingTimer >= homingStartDelay &&
            homingTimer <= homingStartDelay + homingDuration)
        {
            // 플레이어 방향 계산
            Vector2 targetDir =
                ((Vector2)target.position - (Vector2)transform.position).normalized;

            // 현재 탄막의 이동 각도
            float currentAngle =
                Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;

            // 플레이어를 향한 목표 각도
            float targetAngle =
                Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

            // rotateSpeed에 맞춰 부드럽게 회전 연산
            float newAngle =
                Mathf.MoveTowardsAngle(
                    currentAngle,
                    targetAngle,
                    rotateSpeed * Time.deltaTime
                );

            // 회전된 새로운 이동 방향 갱신
            moveDir = new Vector2(
                Mathf.Cos(newAngle * Mathf.Deg2Rad),
                Mathf.Sin(newAngle * Mathf.Deg2Rad)
            ).normalized;

            // 오브젝트 스프라이트 회전 반영
            transform.rotation = Quaternion.Euler(0, 0, newAngle);
        }

        // 부모(BossBullet)의 기본 등속 이동 및 수명 타이머 로직 실행
        base.Update();
    }

    public override void Init(Vector2 dir)
    {
        // 부모 초기화 및 추적 타이머 초기화
        base.Init(dir);
        homingTimer = 0f;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 플레이어 충돌 처리 (FreezeBullet 로직 반영)
        if (collision.collider.CompareTag("Player"))
        {
            // 피해 적용
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.TakeDamage(damage);

            // 확률적으로 빙결(이속 감소) 디버프 적용
            if (Random.value <= freezeChance)
            {
                Player player = collision.collider.GetComponent<Player>();

                if (player != null)
                {
                    player.ApplyIceSlow(
                        slowDuration,
                        slowMultiplier
                    );
                }
            }

            // 피격 후 탄막 비활성화 (풀 반환)
            gameObject.SetActive(false);
            return;
        }

        // 플레이어 외의 벽이나 다른 오브젝트 충돌 시에도 제거
        gameObject.SetActive(false);
    }
}