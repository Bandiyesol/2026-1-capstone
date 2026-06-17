using UnityEngine;

public class Scaner : MonoBehaviour
{
    // 주변 적 탐지 반경
    public float scanRange;
    // 탐지 대상 레이어 마스크
    public LayerMask targetLayer;
    // 가장 가까운 대상(없으면 null)
    public Transform nearestTarget;

    Transform scannerTransform;

    void Awake()
    {
        scannerTransform = transform;
    }

    void FixedUpdate()
    {
        using PhysicsQuery2D.OverlapCircleScope query = PhysicsQuery2D.OverlapCircle(
            scannerTransform.position, scanRange, targetLayer);

        nearestTarget = GetNearest(query, scannerTransform.position);
    }

    static Transform GetNearest(PhysicsQuery2D.OverlapCircleScope query, Vector3 myPos)
    {
        Transform result = null;
        float diff = 100f;

        for (int i = 0; i < query.Count; i++)
        {
            Collider2D hit = query.Get(i);
            if (hit == null)
                continue;

            Transform targetTransform = hit.transform;
            float curDiff = Vector3.Distance(myPos, targetTransform.position);
            if (curDiff < diff)
            {
                diff = curDiff;
                result = targetTransform;
            }
        }

        return result;
    }
}
