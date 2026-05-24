using UnityEngine;

public class BlackHoleGimmick : BiomeGimmick
{
    [Header("초기 흡입 강도")]
    [SerializeField] float startPullForce = 0.15f;

    [Header("최대 흡입 강도")]
    [SerializeField] float maxPullForce = 0.6f;

    [Header("빠른 흡입 강도 증가")]
    [SerializeField] float pullIncreasePerSecond = 0.1f;

    [Header("중심 범위 반경")]
    [SerializeField] float centerRadius = 0.7f;

    [Header("텔레포트 지연 시간")]
    [SerializeField] float teleportDelay = 1.1f;

    [Header("텔레포트 최소 거리")]
    [SerializeField] float teleportMinDistance = 2f;

    [Header("텔레포트 최대 거리")]
    [SerializeField] float teleportMaxDistance = 4f;

    // 플레이어 캐시
    Player player;

    // 중심 체크 시간
    float centerTimer;

    // 현재 흡입력
    float currentPullForce;

    // 현재 상태
    bool pulling;

    // 활성화 상태
    bool active;

    protected override void OnSpawn()
    {
        active = true;
        pulling = false;
        centerTimer = 0f;

        // 처음엔 약하게 시작
        currentPullForce = startPullForce;

        if (GameManager.instance != null)
            player = GameManager.instance.player;
    }

    protected override void Update()
    {
        // 부모 Update 실행
        base.Update();

        if (!active)
            return;

        if (!pulling)
            return;

        if (player == null)
            return;

        if (!GameManager.instance.isLive)
            return;

        Vector3 center = transform.position;
        Vector3 playerPos = player.transform.position;

        float distance = Vector3.Distance(playerPos, center);

        // 거리 기반 흡입력 감소 (멀수록 약해짐)
        float distanceMultiplier = Mathf.Clamp01(1f - (distance / (teleportMaxDistance * 1.5f)));

        // 현재 흡입력 증가 (거리 기반으로 제한)
        currentPullForce = Mathf.Min(
            maxPullForce * distanceMultiplier,
            currentPullForce + pullIncreasePerSecond * Time.deltaTime
        );

        // 중심 방향
        Vector3 dir = (center - playerPos).normalized;

        // 블랙홀 흡입력 추가
        player.externalVelocity +=
            (Vector2)dir * currentPullForce;

        // 중심 근처 체크
        if (distance < centerRadius)
        {
            centerTimer += Time.deltaTime;

            if (centerTimer >= teleportDelay)
            {
                TeleportPlayer();

                // 텔레포트 후 초기화
                centerTimer = 0f;
                currentPullForce = startPullForce;
            }
        }
        else
        {
            centerTimer = 0f;
        }
    }

    // 플레이어 트리거 진입하면 흡입 시작
    protected override void OnPlayerTrigger(Player player)
    {
        pulling = true;
    }

    // 플레이어 트리거 이탈하면 흡입 중단
    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        pulling = false;
        centerTimer = 0f;
    }

    // 플레이어 중심 위치로 텔레포트
    void TeleportPlayer()
    {
        if (player == null)
            return;

        Vector3 center = transform.position;

        float maxDistance =
            Mathf.Max(teleportMinDistance, teleportMaxDistance);

        Vector2 dir = Random.insideUnitCircle.normalized;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;

        float dist = Random.Range(
            teleportMinDistance,
            maxDistance
        );

        Vector2 offset = dir * dist;

        player.transform.position =
            center + (Vector3)offset;
    }

    void OnDisable()
    {
        active = false;
        pulling = false;
        centerTimer = 0f;
    }
}