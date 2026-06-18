using UnityEngine;

/// <summary>적 재배치를 프레임당 상한으로 제한해 대량 이동 시 물리 스파이크를 방지합니다.</summary>
public static class EnemyRepositionBudget
{
    const int MaxPerFixedUpdate = 10;

    static int lastFixedFrame = -1;
    static int used;

    public static bool TryConsume()
    {
        int frame = Time.frameCount;
        if (frame != lastFixedFrame)
        {
            lastFixedFrame = frame;
            used = 0;
        }

        if (used >= MaxPerFixedUpdate)
            return false;

        used++;
        return true;
    }
}
