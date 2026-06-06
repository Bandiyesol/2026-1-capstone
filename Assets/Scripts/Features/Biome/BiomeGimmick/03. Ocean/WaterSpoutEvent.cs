using UnityEngine;

// 물기둥 오브젝트의 콜라이더 충돌 처리
public class WaterSpoutEvent : MonoBehaviour
{
    WaterSpout parent;

    void Awake()
    {
        // 부모 WaterSpout 찾기
        parent = GetComponentInParent<WaterSpout>();
    }

    // 물기둥 콜라이더 충돌 감지 (진입)
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (parent != null)
        {
            // 부모의 메서드 이름을 OnWaterspoutEnter로 호출해야 함
            parent.OnWaterspoutEnter(collision);
        }
    }

    // 물기둥 콜라이더 충돌 감지 (퇴장)
    void OnTriggerExit2D(Collider2D collision)
    {
        if (parent != null)
        {
            // 부모의 메서드 이름을 OnWaterspoutExit로 호출해야 함
            parent.OnWaterspoutExit(collision);
        }
    }
}