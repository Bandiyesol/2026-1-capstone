using System.Collections.Generic;
using UnityEngine;

/// <summary>악세사리 적중/AoE/이펙트가 한 프레임에 폭주하지 않도록 예산을 둡니다.</summary>
public static class AccessoryCombatBudget
{
    const int MaxNotifyPerFrame = 48;
    const int MaxAoEPerFrame = 8;
    const int MaxFxSpawnPerFrame = 12;

    static int budgetFrame = -1;
    static int notifyUsed;
    static int aoeUsed;
    static int fxUsed;
    static readonly HashSet<int> enemiesNotifiedThisFrame = new HashSet<int>();

    static void SyncFrame()
    {
        int frame = Time.frameCount;
        if (frame == budgetFrame)
            return;

        budgetFrame = frame;
        notifyUsed = 0;
        aoeUsed = 0;
        fxUsed = 0;
        enemiesNotifiedThisFrame.Clear();
    }

    /// <summary>같은 적에 대한 NotifyEnemyHit 중복·프레임당 총 호출 상한.</summary>
    public static bool TryBeginEnemyHit(Enemy enemy)
    {
        if (enemy == null)
            return false;

        SyncFrame();
        if (notifyUsed >= MaxNotifyPerFrame)
            return false;

        if (!enemiesNotifiedThisFrame.Add(enemy.GetInstanceID()))
            return false;

        notifyUsed++;
        return true;
    }

    public static bool TryAoEProc()
    {
        SyncFrame();
        if (aoeUsed >= MaxAoEPerFrame)
            return false;

        aoeUsed++;
        return true;
    }

    public static bool TrySpawnFx()
    {
        SyncFrame();
        if (fxUsed >= MaxFxSpawnPerFrame)
            return false;

        fxUsed++;
        return true;
    }
}
