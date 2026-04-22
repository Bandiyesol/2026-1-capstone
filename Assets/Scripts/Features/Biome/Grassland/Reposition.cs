using UnityEngine;

[DefaultExecutionOrder(50)]
public class Reposition : MonoBehaviour
{
    // 트리거 감지용 콜라이더
    Collider2D coll;
    // 물리 좌표 이동용 리지드바디(없으면 transform 이동)
    Rigidbody2D rb;

    /// <summary>
    /// 플레이어 자식 Area 박스가 14×14(반변 7)일 때, 대각선 안쪽 최대 거리 ≈ 9.9.
    /// 트리거 Exit 대신 거리로 판정할 때는 그보다 약간 크게 잡는다.
    /// </summary>
    [SerializeField] float enemyFarFromPlayerDistance = 10.5f;

    [Tooltip("null이면 Camera.main")]
    [SerializeField] Camera gameplayCamera;

    [Tooltip("뷰 박스 밖으로 나간 뒤, 그만큼 더 밀어서 스폰 (월드 유닛)")]
    [SerializeField] float pastScreenEdge = 1.25f;

    [Tooltip("스폰 지점을 좌우로 살짝 흩뿌림 (전방 벡터에 수직)")]
    [SerializeField] float lateralPastScreen = 1.5f;

    void Awake()
    {
        // 컴포넌트 참조 캐싱
        coll = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        // 적은 트리거 누락 가능성이 있어 거리 기반 재배치를 병행
        if (!CompareTag("Enemy") || !isActiveAndEnabled || !coll.enabled)
            return;

        Player player = GameManager.instance != null ? GameManager.instance.player : null;
        if (player == null)
            return;

        Vector2 p = player.GetWorldPosition();
        Vector2 me = rb != null ? rb.position : (Vector2)transform.position;
        float limit = enemyFarFromPlayerDistance;
        // 플레이어와 너무 멀어지면 카메라 앞쪽으로 재배치
        if ((p - me).sqrMagnitude > limit * limit)
            RepositionEnemy();
    }

    // 타일맵(Ground)은 기존 방식대로 Area 트리거 이탈 시 재배치
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Area"))
            return;

        if (CompareTag("Ground"))
            RepositionGround();
    }

    private void RepositionEnemy()
    {
        // 비활성/비충돌 상태 적은 재배치하지 않음
        if (!coll.enabled)
            return;

        Player player = GameManager.instance != null ? GameManager.instance.player : null;
        if (player == null)
            return;

        // 플레이어 진행 방향을 기준으로 전방/측면 오프셋 계산
        Vector2 anchor = player.GetWorldPosition();
        Vector2 facing = player.GetFacingDirection();
        Vector2 side = new Vector2(-facing.y, facing.x);

        Camera cam = gameplayCamera != null ? gameplayCamera : Camera.main;
        float forwardDist;
        if (cam != null && cam.orthographic)
        {
            // 직교 카메라 뷰 경계를 기준으로 화면 밖 거리 계산
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 c = cam.transform.position;
            float minX = c.x - halfW, maxX = c.x + halfW;
            float minY = c.y - halfH, maxY = c.y + halfH;
            forwardDist = DistanceAlongFacingToExitCamera(anchor, facing, minX, maxX, minY, maxY);
            forwardDist += Random.Range(pastScreenEdge * 0.7f, pastScreenEdge * 1.4f);
        }
        else
            // 카메라 정보가 없으면 보수적 랜덤 거리 사용
            forwardDist = Random.Range(10f, 14f);

        // 전방 + 측면 랜덤 오프셋으로 스폰 위치 분산
        float lateral = Random.Range(-lateralPastScreen, lateralPastScreen);
        Vector2 offset = facing * forwardDist + side * lateral;
        Vector3 newPos = anchor + offset;
        newPos.z = transform.position.z;

        if (rb != null)
            rb.MovePosition(newPos);
        else
            transform.position = newPos;
    }

    private void RepositionGround()
    {
        // 플레이어를 중심으로 타일 단위 재배치
        if (GameManager.instance == null || GameManager.instance.player == null)
            return;

        float tileSize = 28f;

        Vector3 playerPos = GameManager.instance.player.transform.position;
        Vector3 myPos = transform.position;

        // 그리드 좌표 차이를 타일 크기로 환산
        float gridX = Mathf.Round((playerPos.x - myPos.x) / tileSize);
        float gridY = Mathf.Round((playerPos.y - myPos.y) / tileSize);

        // 한 칸 이상 벗어나면 해당 칸 수만큼 이동
        if (Mathf.Abs(gridX) >= 1f || Mathf.Abs(gridY) >= 1f)
        {
            Vector3 newPos = myPos + new Vector3(gridX * tileSize, gridY * tileSize, 0);
            transform.position = newPos;
        }
    }

    /// <summary>직교 카메라 뷰 직사각형 안에서 출발해, facing 방향으로 나갈 때 첫 번째 경계까지의 거리.</summary>
    static float DistanceAlongFacingToExitCamera(Vector2 origin, Vector2 facing, float minX, float maxX, float minY, float maxY)
    {
        if (facing.sqrMagnitude < 1e-8f)
            facing = Vector2.right;
        facing = facing.normalized;

        bool inside = origin.x >= minX && origin.x <= maxX && origin.y >= minY && origin.y <= maxY;
        float tExit = float.MaxValue;

        if (facing.x > 1e-6f)
            tExit = Mathf.Min(tExit, (maxX - origin.x) / facing.x);
        else if (facing.x < -1e-6f)
            tExit = Mathf.Min(tExit, (minX - origin.x) / facing.x);

        if (facing.y > 1e-6f)
            tExit = Mathf.Min(tExit, (maxY - origin.y) / facing.y);
        else if (facing.y < -1e-6f)
            tExit = Mathf.Min(tExit, (minY - origin.y) / facing.y);

        if (inside && tExit > 0f && tExit < 1e6f)
            return tExit;

        return 12f;
    }
}
