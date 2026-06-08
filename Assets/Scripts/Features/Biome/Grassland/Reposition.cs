using UnityEngine;

[DefaultExecutionOrder(50)]
public class GrasslandReposition : MonoBehaviour
{
    // 트리거 감지용 콜라이더
    Collider2D coll;
    // 물리 좌표 이동용 리지드바디(없으면 transform 이동)
    Rigidbody2D rb;

    [Header("1. 재배치 타이밍 (체크 거리)")]
    [Tooltip("적이 플레이어와 이 거리보다 멀어지면 재배치를 발동합니다.")]
    [SerializeField] float enemyFarFromPlayerDistance = 16.0f;

    [Header("2. 재배치 스폰 위치 (멀리 배치하기)")]
    [Tooltip("카메라 화면 밖으로 나간 뒤, 추가로 더 멀리 밀어서 스폰시킬 거리 (원래 값: 1.25f)")]
    [SerializeField] float pastScreenEdge = 5.0f; // ◀ 이 값을 키울수록 화면 훨씬 밖에서 나타납니다.

    [Tooltip("스폰 지점을 좌우로 흩뿌리는 너비 (원래 값: 1.5f)")]
    [SerializeField] float lateralPastScreen = 4.0f; // ◀ 거리가 멀어진 만큼 좌우 범위도 넓혀줍니다.

    [Tooltip("null이면 Camera.main")]
    [SerializeField] Camera gameplayCamera;

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

        // 플레이어와 설정한 거리보다 멀어지면 재배치 실행
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
            // 직교 카메라 뷰 경계를 기준으로 화면이 끝나는 딱 딱 맞는 거리 계산
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 c = cam.transform.position;
            float minX = c.x - halfW, maxX = c.x + halfW;
            float minY = c.y - halfH, maxY = c.y + halfH;

            forwardDist = DistanceAlongFacingToExitCamera(anchor, facing, minX, maxX, minY, maxY);
            // 계산된 화면 끝 거리 정보에 '추가로 밀어낼 거리(pastScreenEdge)'를 더함
            forwardDist += Random.Range(pastScreenEdge * 0.8f, pastScreenEdge * 1.2f);
        }
        else
            // 카메라가 없을 때: 멀리 배치하기 위해 기본 폰 범위를 크게 상향 (원래 값: 15f~18f)
            forwardDist = Random.Range(22f, 26f);

        // 전방 거리와 좌우 흩뿌림(lateral)을 조합하여 최종 좌표 생성
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
        if (GameManager.instance == null || GameManager.instance.player == null)
            return;

        float tileSize = 28f;
        Vector3 playerPos = GameManager.instance.player.transform.position;
        Vector3 myPos = transform.position;

        float gridX = Mathf.Round((playerPos.x - myPos.x) / tileSize);
        float gridY = Mathf.Round((playerPos.y - myPos.y) / tileSize);

        if (Mathf.Abs(gridX) >= 1f || Mathf.Abs(gridY) >= 1f)
        {
            Vector3 newPos = myPos + new Vector3(gridX * tileSize, gridY * tileSize, 0);
            transform.position = newPos;
        }
    }

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

        return 16f;
    }
}