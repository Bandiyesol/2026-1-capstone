using UnityEngine;

// WarmZone 범위 감지 전용
public class WarmZoneRange : MonoBehaviour
{
    // 부모 WarmZone
    WarmZoneGimmick warmZone;

    void Awake()
    {
        // 부모 스크립트 가져오기
        warmZone =
            GetComponentInParent<WarmZoneGimmick>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어 아니면 무시
        if (!collision.CompareTag("Player"))
            return;

        // 따뜻한 구역 진입
        warmZone.EnterPlayer();
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        // 플레이어 아니면 무시
        if (!collision.CompareTag("Player"))
            return;

        // 따뜻한 구역 이탈
        warmZone.ExitPlayer();
    }
}