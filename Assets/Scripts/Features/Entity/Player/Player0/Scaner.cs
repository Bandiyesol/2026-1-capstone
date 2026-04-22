using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scaner : MonoBehaviour
{
    // 주변 적 탐지 반경
    public float scanRange;
    // 탐지 대상 레이어 마스크
    public LayerMask targetLayer;
    // 현재 프레임에 탐지된 대상 목록
    public RaycastHit2D[] targets;
    // 가장 가까운 대상(없으면 null)
    public Transform nearestTarget;

    void FixedUpdate()
    {
        // 원형 범위 탐지로 적 목록 갱신
        targets = Physics2D.CircleCastAll(transform.position, scanRange, Vector2.zero, 0, targetLayer);
        // 무기 조준용 최근접 대상 계산
        nearestTarget = GetNearest();
    }

    Transform GetNearest()
    {
        // 가장 가까운 타겟을 찾는 단순 선형 탐색
        Transform result = null;
        float diff = 100;

        foreach (RaycastHit2D target in targets)
        {
            Vector3 myPos = transform.position;
            Vector3 targetPos = target.transform.position;
            float curDiff = Vector3.Distance(myPos, targetPos);
            // 현재까지 최소 거리보다 작으면 갱신
            if (curDiff < diff)
            {
                diff = curDiff;
                result = target.transform;
            }
        }
        return result;
    }
}
