using UnityEngine;

public class Reposition : MonoBehaviour
{
    // (여기에 minSpawn, maxSpawn 등 기존에 있던 변수들이 선언되어 있어야 합니다)
    public float minSpawn = 10f; // 예시 (원래 수치로 변경하세요)
    public float maxSpawn = 20f; // 예시 (원래 수치로 변경하세요)

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

        // 핵심 방어 코드
        if (cam == null)
        {
            Debug.LogWarning("[Reposition] MainCamera 태그를 가진 카메라가 씬에 없습니다!");
            return;
        }

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