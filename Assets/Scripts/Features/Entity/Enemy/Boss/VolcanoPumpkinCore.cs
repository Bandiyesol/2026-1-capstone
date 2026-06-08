using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 화산 호박 소대 전체 관리 코어 (자식 리더 원격 제어 + 소대원 순간이동 강제 동기화 시스템)
/// </summary>
public class VolcanoPumpkinCore : MonoBehaviour
{
    [Header("리더 (자식 오브젝트)")]
    [SerializeField] VolcanoPumpkinUnit leader;

    [Header("최종 보상")]
    [SerializeField] int portalGimmickIndex = 13;

    [HideInInspector]
    public WaveManager waveManager;

    readonly List<VolcanoPumpkinUnit> units = new List<VolcanoPumpkinUnit>();
    readonly List<GameObject> summonedEnemies = new List<GameObject>();
    readonly List<GameObject> spawnedBullets = new List<GameObject>();

    Vector2 lastDeathPosition;
    bool cleared;

    void OnEnable()
    {
        if (waveManager == null)
            waveManager = StageManager.instance.waveManager;

        units.Clear();
        summonedEnemies.Clear();
        spawnedBullets.Clear();
        cleared = false;
        lastDeathPosition = Vector2.zero;

        if (leader != null)
        {
            leader.SetCore(this);
            leader.gameObject.SetActive(true);
        }
    }

    // -------------------------------------------------------
    // 기능 복구: 소대원 재배치 및 동기화
    // -------------------------------------------------------
    public void RepositionFollowers(Vector2 leaderPos, float spacing, bool flipX)
    {
        float dir = flipX ? 1f : -1f;
        int followerIndex = 1;
        for (int i = 1; i < units.Count; i++)
        {
            VolcanoPumpkinUnit unit = units[i];
            if (unit == null) continue;

            Vector2 newPos = leaderPos + Vector2.right * dir * spacing * followerIndex;
            unit.transform.position = newPos;
            unit.ResetPositionHistory();
            followerIndex++;
        }
    }

    public void BroadcastFire()
    {
        for (int i = 1; i < units.Count; i++)
        {
            if (units[i] != null)
                units[i].TriggerFire();
        }
    }

    public bool IsLeaderAlive()
    {
        foreach (VolcanoPumpkinUnit unit in units)
        {
            if (unit != null && unit.isLeader)
                return true;
        }
        return false;
    }

    // -------------------------------------------------------
    // 기능 복구: 등록 및 제거 로직
    // -------------------------------------------------------
    public void RegisterUnit(VolcanoPumpkinUnit unit)
    {
        if (!units.Contains(unit)) units.Add(unit);
    }

    public void RegisterBullet(GameObject bullet)
    {
        if (bullet != null && !spawnedBullets.Contains(bullet))
            spawnedBullets.Add(bullet);
    }

    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy != null && !summonedEnemies.Contains(enemy))
            summonedEnemies.Add(enemy);
    }

    public void UnregisterUnit(VolcanoPumpkinUnit unit, bool isLeader)
    {
        if (unit == null) return;

        lastDeathPosition = unit.transform.position;
        units.Remove(unit);
        units.RemoveAll(x => x == null);

        if (isLeader && units.Count > 0)
        {
            units[0].PromoteToLeader();
            units[0].ReassignFollowers(units);
        }
        CheckAllDead();
    }

    // -------------------------------------------------------
    // 핵심 수정: 웨이브 매니저 전멸 보고 및 보상 호출
    // -------------------------------------------------------
    void CheckAllDead()
    {
        if (cleared || units.Count > 0) return;

        cleared = true;
        SpawnRewards();

        // 💡 보스 전체가 전멸했을 때만 WaveManager에게 보고합니다.
        if (waveManager != null)
        {
            waveManager.OnEnemyDead();
        }
    }

    void SpawnRewards()
    {
        foreach (GameObject bullet in spawnedBullets) if (bullet != null) bullet.SetActive(false);
        spawnedBullets.Clear();

        foreach (GameObject enemy in summonedEnemies) if (enemy != null) enemy.SetActive(false);
        summonedEnemies.Clear();

        if (CoinDropManager.Instance != null) CoinDropManager.Instance.TryDropFromBoss(lastDeathPosition);
        if (ChestDropManager.Instance != null) ChestDropManager.Instance.TryDropFromBoss(lastDeathPosition);

        if (PoolManager.Instance != null)
        {
            GameObject portal = PoolManager.Instance.GetGimmick(portalGimmickIndex);
            if (portal != null) portal.transform.position = lastDeathPosition;
        }

        gameObject.SetActive(false);
    }
}