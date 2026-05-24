using UnityEngine;

public class Reposition : MonoBehaviour
{
    public float minSpawn = 4f;   // 플레이어 최소 거리
    public float maxSpawn = 8f;   // 플레이어 최대 거리

    void OnTriggerExit2D(Collider2D other)
    {
        // Enemy만 처리
        if (!other.CompareTag("Enemy"))
            return;

        // Enemy 콜라이더 꺼져있으면 무시
        if (!other.enabled)
            return;

        Transform enemy = other.transform;
        Vector3 playerPos = GameManager.instance.player.transform.position;
        Camera cam = Camera.main;

        Vector3 newPos;

        int safety = 0;

        // =========================
        // 카메라 밖 위치 찾기
        // =========================
        do
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(minSpawn, maxSpawn);

            newPos = playerPos + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0
            );

            safety++;
            if (safety > 20) break;

        } while (IsInsideCamera(newPos, cam));

        // 위치 이동
        enemy.position = newPos;
    }

    // 카메라 안 여부 체크
    bool IsInsideCamera(Vector3 pos, Camera cam)
    {
        Vector3 viewPos = cam.WorldToViewportPoint(pos);

        return viewPos.x > 0f && viewPos.x < 1f &&
               viewPos.y > 0f && viewPos.y < 1f;
    }
}
