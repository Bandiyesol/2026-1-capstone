using UnityEngine;

// 유사의 늪 기믹 (플레이어를 끌어당기고 중심부에서 지속 피해 및 감속 부여)
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

    // 현재 늪에 잡힌 플레이어 참조
    Player currentPlayer;

    // 늪에 머문 누적 시간
    float stayTime;

    // 중심 도달 여부 플래그
    bool inCenter;

    protected override void OnSpawn()
    {
        // 오브젝트 생성/풀링 스폰 시 플레이어 상태 안전하게 초기화
        RestorePlayer();

        stayTime = 0f;
        inCenter = false;
    }

    protected override void OnPlayerTrigger(Player player)
    {
        // 이미 다른 플레이어를 처리 중이라면 중복 등록 방지
        if (currentPlayer != null)
            return;

        // 플레이어 진입 등록
        currentPlayer = player;

        stayTime = 0f;
        inCenter = false;
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        Player player = collision.GetComponent<Player>();

        // 늪을 벗어난 오브젝트가 현재 붙잡아둔 플레이어가 맞는지 검증
        if (player == null || player != currentPlayer)
            return;

        // 탈출했으므로 속도 및 배율 원상 복구
        RestorePlayer();
    }

    protected override void Update()
    {
        // 부모 클래스의 기본 Update 로직 수행
        base.Update();

        // 늪에 플레이어가 없다면 연산 스킵
        if (currentPlayer == null)
            return;

        if (GameManager.instance == null || !GameManager.instance.isLive)
            return;

        // 늪 체류 시간 갱신
        stayTime += Time.deltaTime;

        Vector3 center = transform.position;
        Vector3 playerPos = currentPlayer.transform.position;

        // 중심점과 플레이어 사이의 거리 및 방향 벡터 계산
        Vector3 dir = center - playerPos;
        float dist = dir.magnitude;

        // [로직 수정] 머문 시간에 비례해 서서히 감소하는 최종 속도 배율 계산
        float currentTargetMultiplier = Mathf.Max(
            minSpeedMultiplier,
            startSpeedMultiplier - (stayTime * slowPerSecond)
        );

        // [에러 수정] player.speed/baseSpeed 대신 Player 스크립트의 이동 속도 멀티플라이어에 직접 대입
        currentPlayer.moveSpeedMultiplier = currentTargetMultiplier;

        // 시간에 따라 점점 강해지는 끌림 힘 계산
        float pullForce = Mathf.Min(
            maxPullForce,
            basePullForce + (stayTime * pullIncreasePerSecond)
        );

        // 중심부에 가까워질수록 빨려 들어가는 흡입 강도 팩터 가중치 연산
        float distanceFactor = 1f;
        if (dist > centerRadius)
        {
            distanceFactor = Mathf.Clamp01(
                1f + (1f / Mathf.Max(dist, 0.2f))
            );
        }

        // 아직 완전히 중심점에 정착하지 않았다면 중심 방향으로 외력(externalVelocity) 누적 주입
        if (!inCenter)
        {
            if (dist > 0.01f)
            {
                currentPlayer.externalVelocity +=
                    (Vector2)dir.normalized *
                    pullForce *
                    distanceFactor;
            }

            // 플레이어가 중심 반경 안으로 들어왔는지 체크
            if (dist <= centerRadius)
                inCenter = true;
        }
        else
        {
            // [에러 수정] 중심부 지속 피해를 PlayerStats의 방어/회피 파이프라인으로 안전하게 전달
            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.TakeDamage(damagePerSecond * Time.deltaTime);
            }
            else
            {
                GameManager.instance.Health -= damagePerSecond * Time.deltaTime; // 폴백용
            }

            // 탈출 판정: 중심 반경에 여유 오차범위(0.12f)를 더해 늪을 완전히 빠져나갔는지 체크
            if (dist > centerRadius + 0.12f)
                inCenter = false;
        }
    }

    void OnDisable()
    {
        // 오브젝트가 비활성화되거나 오브젝트 풀로 반환될 때 플레이어 상태 강제 롤백
        RestorePlayer();
    }

    void RestorePlayer()
    {
        if (currentPlayer == null)
            return;

        // [에러 수정] 탈출 시 플레이어의 이동 속도 배율을 기본값(1.0)으로 안전하게 청소
        currentPlayer.moveSpeedMultiplier = 1f;

        // 참조 해제 및 상태 초기화
        currentPlayer = null;
        stayTime = 0f;
        inCenter = false;
    }
}