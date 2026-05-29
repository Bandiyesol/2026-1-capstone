using UnityEngine;

// 유사 기믹
public class Quicksand : BiomeGimmick
{
    [Header("기본 끌림")]
    [SerializeField] float basePullForce = 0.35f;

    [Header("최대 끌림")]
    [SerializeField] float maxPullForce = 1f;

    [Header("초당 끌림 증가")]
    [SerializeField] float pullIncreasePerSecond = 0.2f;

    [Header("처음 이동속도 배율")]
    [SerializeField] float startSpeedMultiplier = 0.9f;

    [Header("최소 이동속도 배율")]
    [SerializeField] float minSpeedMultiplier = 0.3f;

    [Header("초당 감속량")]
    [SerializeField] float slowPerSecond = 0.1f;

    [Header("중심 반경")]
    [SerializeField] float centerRadius = 0.8f;

    [Header("초당 피해")]
    [SerializeField] float damagePerSecond = 2f;

    // 현재 플레이어
    Player currentPlayer;

    // 머문 시간
    float stayTime;

    // 원래 속도
    float baseSpeed;

    // 중심 여부
    bool inCenter;

    protected override void OnSpawn()
    {
        currentPlayer = null;
        stayTime = 0f;
        inCenter = false;
    }

    protected override void OnPlayerTrigger(Player player)
    {
        if (currentPlayer != null)
            return;

        currentPlayer = player;

        baseSpeed = player.speed;

        stayTime = 0f;
        inCenter = false;
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        Player player = collision.GetComponent<Player>();

        if (player == null || player != currentPlayer)
            return;

        // 이동속도 복구
        player.speed = baseSpeed;

        currentPlayer = null;
        stayTime = 0f;
        inCenter = false;
    }

    protected override void Update()
    {
        // 부모 Update 실행
        base.Update();

        if (currentPlayer == null)
            return;

        if (!GameManager.instance.isLive)
            return;

        // 체류 시간 증가
        stayTime += Time.deltaTime;

        Vector3 center = transform.position;
        Vector3 playerPos = currentPlayer.transform.position;

        Vector3 dir = center - playerPos;

        float dist = dir.magnitude;

        // 점점 느려짐
        float speedMultiplier = Mathf.Max(
            minSpeedMultiplier,
            startSpeedMultiplier - stayTime * slowPerSecond
        );

        currentPlayer.speed =
            baseSpeed * speedMultiplier;

        // 점점 강해지는 끌림
        float pullForce = Mathf.Min(
            maxPullForce,
            basePullForce +
            stayTime * pullIncreasePerSecond
        );

        // 중심 가까울수록 더 강함
        float distanceFactor = 1f;

        if (dist > centerRadius)
        {
            distanceFactor =
                Mathf.Clamp01(
                    1f + (1f / Mathf.Max(dist, 0.2f))
                );
        }

        // 중심 밖이면 끌림
        if (!inCenter)
        {
            if (dist > 0.01f)
            {
                currentPlayer.externalVelocity +=
                    (Vector2)dir.normalized *
                    pullForce *
                    distanceFactor;
            }

            // 중심 도달
            if (dist <= centerRadius)
                inCenter = true;
        }
        else
        {
            // 중심 지속 피해
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.TakeDamage(
                    damagePerSecond * Time.deltaTime,
                    applyIFrames: false,
                    PlayerDamageKind.PerSecondFrame
                );
            }

            if (GameManager.instance.Health <= 0f)
                currentPlayer.PlayerDead();

            // 탈출 시 다시 끌림
            if (dist > centerRadius + 0.12f)
                inCenter = false;
        }
    }
}