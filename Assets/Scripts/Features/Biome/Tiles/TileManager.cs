using UnityEngine;

public class TileManager : MonoBehaviour
{
    public Transform player;     // 플레이어 위치 기준
    public Transform[] tiles;    // 4개의 타일맵

    public float tileSize = 50f; // 타일 한 변 길이 (중심 간 거리)

    void Update()
    {
        // 현재 플레이어 위치
        Vector3 playerPos = player.position;

        // 타일이 이 거리 이상 벗어나면 이동시킴
        float limit = tileSize;

        // 모든 타일 검사
        foreach (var t in tiles)
        {
            // 플레이어 ↔ 타일 위치 차이
            Vector3 diff = playerPos - t.position;

            // 이동할 값 (기본 0)
            Vector3 move = Vector3.zero;

            // =========================
            // X축 이동 판정
            // =========================

            // 플레이어가 타일보다 오른쪽으로 많이 이동했을 때
            if (diff.x > limit)
                move.x = tileSize * 2f;   // 오른쪽 끝으로 이동

            // 플레이어가 타일보다 왼쪽으로 많이 이동했을 때
            else if (diff.x < -limit)
                move.x = -tileSize * 2f;  // 왼쪽 끝으로 이동

            // =========================
            // Y축 이동 판정
            // =========================

            // 플레이어가 타일보다 위쪽으로 많이 이동했을 때
            if (diff.y > limit)
                move.y = tileSize * 2f;   // 위쪽 끝으로 이동

            // 플레이어가 타일보다 아래쪽으로 많이 이동했을 때
            else if (diff.y < -limit)
                move.y = -tileSize * 2f;  // 아래쪽 끝으로 이동

            // =========================
            // 실제 이동 적용
            // =========================

            // 이동이 필요할 때만 실행
            if (move != Vector3.zero)
            {
                // 타일을 반대편 끝으로 순간 이동
                t.position += move;
            }
        }
    }
}