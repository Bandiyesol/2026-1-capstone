using UnityEngine;

// stone의 애니메이터 이벤트 콜백 및 충돌 감지
public class FallingRockEvent : MonoBehaviour
{
    FallingRock parent;

    void Awake()
    {
        // 부모 FallingRock 찾기
        parent = GetComponentInParent<FallingRock>();
    }

    // 애니메이터 종료 프레임에서 호출
    public void EndFall()
    {
        if (parent != null)
            parent.EndFall();
    }

    // stone 콜라이더 충돌 감지 (이것이 중요!)
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (parent != null)
            parent.OnStoneCollision(collision);
    }
}
