using UnityEngine;

public class Reposition : MonoBehaviour
{
    public float minSpawn = 10f;
    public float maxSpawn = 20f;

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        if (!other.enabled)
            return;

        GameManager gm = GameManager.instance;
        Player player = gm != null ? gm.player : null;
        if (player == null)
            return;

        Transform enemy = other.transform;
        Vector3 playerPos = player.transform.position;
        Camera cam = Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("[Reposition] MainCamera 태그를 가진 카메라가 씬에 없습니다!");
            return;
        }

        Vector3 newPos;
        int safety = 0;

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

        enemy.position = newPos;
    }

    bool IsInsideCamera(Vector3 pos, Camera cam)
    {
        Vector3 viewPos = cam.WorldToViewportPoint(pos);

        return viewPos.x > 0f && viewPos.x < 1f &&
               viewPos.y > 0f && viewPos.y < 1f;
    }
}
