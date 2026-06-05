using UnityEngine;

// 플레이어를 끌어당기고 중심 도달 시 무작위 위치로 워프시키는 블랙홀 기믹
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

        // 처음엔 약하게 시작 (기존 수치 보존)
        currentPullForce = startPullForce;
    }

    protected override void Update()
    {
        base.Update();

        if (!active || !pulling)
            return;

        // [안전장치 추가] 주체 플레이어가 비어있을 경우 예외 발생을 방지하기 위해 런타임 자동 캐싱
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null) return;
        }

        // 블랙홀 중심부와 플레이어 사이의 거리 및 방향 계산
        Vector3 center = transform.position;
        Vector3 dir = center - player.transform.position;
        float distance = dir.magnitude;

        // 시간에 따라 흡입력을 서서히 증가시킴 (최대치 한계 적용)
        currentPullForce = Mathf.Min(currentPullForce + pullIncreasePerSecond * Time.deltaTime, maxPullForce);

        if (distance > 0.01f)
        {
            // 플레이어 외력(externalVelocity)에 블랙홀 중심 방향 힘 누적 주입
            player.externalVelocity += (Vector2)dir.normalized * currentPullForce;
        }

        // [중심부 진입 판정] 설정한 반지름보다 안쪽으로 들어온 경우
        if (distance < centerRadius)
        {
            centerTimer += Time.deltaTime;

            // 지연 시간을 충족하면 다른 좌표로 텔레포트 실행
            if (centerTimer >= teleportDelay)
            {
                TeleportPlayer();

                // 텔레포트 성공 후 타이머 및 흡입 강도 리셋
                centerTimer = 0f;
                currentPullForce = startPullForce;
            }
        }
        else
        {
            // 중심 영역에서 벗어나면 타이머 초기화
            centerTimer = 0f;
        }
    }

    // 플레이어 트리거 진입하면 흡입 시작
    protected override void OnPlayerTrigger(Player player)
    {
        this.player = player;
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

        float maxDistance = Mathf.Max(teleportMinDistance, teleportMaxDistance);

        // 무작위 방향 벡터 추출
        Vector2 dir = Random.insideUnitCircle.normalized;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;

        // 최소~최대 사거리 사이의 무작위 거리 연산
        float dist = Random.Range(teleportMinDistance, maxDistance);
        Vector3 targetPosition = center + (Vector3)(dir * dist);

        // 플레이어 위치 강제 이동
        player.transform.position = targetPosition;
    }
}