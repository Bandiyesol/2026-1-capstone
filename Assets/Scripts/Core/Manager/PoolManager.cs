using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [Header("프리팹 배열 설정")]
    public GameObject[] enemyPrefabs;
    public GameObject[] bossPrefabs;
    public GameObject[] bossBulletPrefabs;
    public GameObject[] gimmickPrefabs;
    public GameObject[] coinPrefabs;
    public GameObject[] chestPrefabs;
    public GameObject[] motionPrefabs;

    List<GameObject>[] enemyPools;
    List<GameObject>[] bossPools;
    List<GameObject>[] bossBulletPools;
    List<GameObject>[] gimmickPools;
    List<GameObject>[] coinPools;
    List<GameObject>[] chestPools;

    readonly Dictionary<string, List<Motion>> motionPools = new Dictionary<string, List<Motion>>();
    readonly Dictionary<string, GameObject> motionPrefabById = new Dictionary<string, GameObject>();

    public const int MaxActiveMotions = 96;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        enemyPools = CreatePools(enemyPrefabs != null ? enemyPrefabs.Length : 0);
        bossPools = CreatePools(bossPrefabs != null ? bossPrefabs.Length : 0);
        bossBulletPools = CreatePools(bossBulletPrefabs != null ? bossBulletPrefabs.Length : 0);
        gimmickPools = CreatePools(gimmickPrefabs != null ? gimmickPrefabs.Length : 0);
        coinPools = CreatePools(coinPrefabs != null ? coinPrefabs.Length : 0);
        chestPools = CreatePools(chestPrefabs != null ? chestPrefabs.Length : 0);

        EnsureShopkeeperGimmickPrefab();
        InitializeMotionPools();
    }

    void EnsureShopkeeperGimmickPrefab()
    {
        if (FindShopkeeperGimmickIndex() >= 0)
            return;

        GameObject prefab = Resources.Load<GameObject>("Prefabs/Gimmick/ShopkeeperNpc");
        if (prefab == null)
            return;

        RegisterGimmickPrefab(prefab);
        Debug.Log("[PoolManager] ShopkeeperNpc를 Resources에서 gimmickPrefabs에 자동 등록했습니다.");
    }

    public void RegisterGimmickPrefab(GameObject prefab)
    {
        if (prefab == null || gimmickPrefabs == null)
            return;

        for (int i = 0; i < gimmickPrefabs.Length; i++)
        {
            if (gimmickPrefabs[i] == prefab)
                return;
        }

        int oldLength = gimmickPrefabs.Length;
        var expanded = new GameObject[oldLength + 1];
        for (int i = 0; i < oldLength; i++)
            expanded[i] = gimmickPrefabs[i];
        expanded[oldLength] = prefab;
        gimmickPrefabs = expanded;
        EnsurePoolCapacity(ref gimmickPools, gimmickPrefabs);
    }

    void InitializeMotionPools()
    {
        motionPools.Clear();
        motionPrefabById.Clear();

        if (motionPrefabs == null || motionPrefabs.Length == 0)
            motionPrefabs = Resources.LoadAll<GameObject>("Prefabs/Motions");

        if (motionPrefabs == null || motionPrefabs.Length == 0)
        {
            Debug.LogWarning("[PoolManager] motionPrefabs가 비어 있습니다. Assets/Resources/Prefabs/Motions 를 확인하세요.");
            return;
        }

        foreach (GameObject prefab in motionPrefabs)
        {
            if (prefab == null || motionPrefabById.ContainsKey(prefab.name))
                continue;

            motionPrefabById[prefab.name] = prefab;
            motionPools[prefab.name] = new List<Motion>();
        }

        Debug.Log($"[PoolManager] Motion 풀 등록: {motionPrefabById.Count}개");
    }

    List<GameObject>[] CreatePools(int count)
    {
        List<GameObject>[] pools = new List<GameObject>[count];
        for (int i = 0; i < count; i++)
            pools[i] = new List<GameObject>();
        return pools;
    }

    #region 오브젝트 풀 가져오기 메서드들 (Get)
    public GameObject GetEnemy(int index)
    {
        EnsurePoolCapacity(ref enemyPools, enemyPrefabs);
        return GetFromPool(enemyPrefabs, enemyPools, index, "Enemy");
    }

    public GameObject GetBoss(int index)
    {
        EnsurePoolCapacity(ref bossPools, bossPrefabs);
        return GetBossFromPool(index);
    }

    // 보스 전용 — SetActive 전에 ResetBoss() 호출하여 OnEnable()에서 체력 초기화 보장
    GameObject GetBossFromPool(int index)
    {
        if (bossPrefabs == null || bossPools == null
            || index < 0 || index >= bossPrefabs.Length || index >= bossPools.Length)
        {
            Debug.LogError($"[PoolManager] Boss index {index} 범위 초과.");
            return null;
        }

        if (bossPrefabs[index] == null)
        {
            Debug.LogError($"[PoolManager] bossPrefabs[{index}]가 null입니다.");
            return null;
        }

        GameObject select = null;

        foreach (GameObject item in bossPools[index])
        {
            if (item != null && !item.activeSelf)
            {
                select = item;
                break;
            }
        }

        if (select == null)
        {
            select = Instantiate(bossPrefabs[index], transform);
            bossPools[index].Add(select);
        }

        // ★ SetActive(true) 전에 ResetBoss() 호출 → OnEnable()에서 풀피로 초기화됨
        BossBase boss = select.GetComponent<BossBase>();
        if (boss != null)
            boss.ResetBoss();

        select.SetActive(true);

        return select;
    }

    public GameObject GetBossBullet(int index)
    {
        EnsurePoolCapacity(ref bossBulletPools, bossBulletPrefabs);
        return GetFromPool(bossBulletPrefabs, bossBulletPools, index, "BossBullet");
    }

    public GameObject GetGimmick(int index)
    {
        int resolved = ResolveGimmickIndex(index);
        if (resolved < 0) return null;
        return GetFromPool(gimmickPrefabs, gimmickPools, resolved, "Gimmick");
    }

    public bool IsStagePortalGimmickIndex(int index)
    {
        if (gimmickPrefabs == null || index < 0 || index >= gimmickPrefabs.Length)
            return false;

        GameObject prefab = gimmickPrefabs[index];
        return prefab != null && prefab.GetComponent<StagePortal>() != null;
    }

    public int FindStagePortalGimmickIndex()
    {
        if (gimmickPrefabs == null) return -1;

        for (int i = 0; i < gimmickPrefabs.Length; i++)
        {
            GameObject prefab = gimmickPrefabs[i];
            if (prefab != null && prefab.GetComponent<StagePortal>() != null)
                return i;
        }

        return -1;
    }

    public int FindShopkeeperGimmickIndex()
    {
        if (gimmickPrefabs == null) return -1;

        for (int i = 0; i < gimmickPrefabs.Length; i++)
        {
            GameObject prefab = gimmickPrefabs[i];
            if (prefab != null && prefab.GetComponent<ShopkeeperNpc>() != null)
                return i;
        }

        return -1;
    }

    int ResolveGimmickIndex(int index)
    {
        if (gimmickPrefabs == null || gimmickPrefabs.Length == 0)
        {
            Debug.LogWarning("[PoolManager] gimmickPrefabs가 비어 있습니다.");
            return -1;
        }

        if (index >= 0 && index < gimmickPrefabs.Length && gimmickPrefabs[index] != null)
            return index;

        int portalIndex = FindStagePortalGimmickIndex();
        if (portalIndex >= 0)
        {
            Debug.LogWarning(
                $"[PoolManager] gimmickPrefabs[{index}]를 사용할 수 없어 Stage Portal(index {portalIndex})로 대체합니다.");
            return portalIndex;
        }

        Debug.LogError(
            $"[PoolManager] gimmickPrefabs[{index}]가 범위를 벗어났고 Stage Portal 프리팹도 없습니다. " +
            "PoolManager.gimmickPrefabs에 Stage Portal.prefab을 추가하세요.");
        return -1;
    }

    public GameObject GetCoin(int index)
    {
        if (coinPrefabs == null || index < 0 || index >= coinPrefabs.Length) return null;
        return GetFromPool(coinPrefabs, coinPools, index, "Coin");
    }

    public GameObject GetChest(int index)
    {
        if (chestPrefabs == null || index < 0 || index >= chestPrefabs.Length) return null;
        return GetFromPool(chestPrefabs, chestPools, index, "Chest");
    }

    public Motion SpawnMotion(string motionId, Vector3 position, Quaternion rotation, bool activateImmediately = true)
    {
        if (string.IsNullOrEmpty(motionId) || !motionPrefabById.TryGetValue(motionId, out GameObject prefab))
        {
            Debug.LogWarning($"[PoolManager] Motion 프리팹 없음: {motionId}");
            return null;
        }

        if (!motionPools.TryGetValue(motionId, out List<Motion> pool))
        {
            pool = new List<Motion>();
            motionPools[motionId] = pool;
        }

        Motion motion = null;
        foreach (Motion candidate in pool)
        {
            if (candidate != null && !candidate.gameObject.activeSelf)
            {
                motion = candidate;
                break;
            }
        }

        if (motion == null && !CanSpawnMotion(1))
            return null;

        if (motion == null)
        {
            GameObject created = Instantiate(prefab, transform);
            created.name = prefab.name;
            created.SetActive(false);
            motion = created.GetComponent<Motion>();
            if (motion == null)
            {
                Debug.LogError($"[PoolManager] Motion 컴포넌트 없음: {prefab.name}");
                Destroy(created);
                return null;
            }

            pool.Add(motion);
        }

        motion.ResetForPool();
        motion.transform.SetPositionAndRotation(position, rotation);
        if (activateImmediately)
            motion.gameObject.SetActive(true);
        return motion;
    }

    public int GetActiveMotionCount()
    {
        int count = 0;
        foreach (List<Motion> pool in motionPools.Values)
        {
            foreach (Motion motion in pool)
            {
                if (motion != null && motion.gameObject.activeSelf)
                    count++;
            }
        }

        return count;
    }

    public int GetRemainingMotionBudget() => Mathf.Max(0, MaxActiveMotions - GetActiveMotionCount());
    public bool CanSpawnMotion(int count = 1) => GetRemainingMotionBudget() >= count;

    public void ReleaseMotion(Motion motion)
    {
        if (motion == null)
            return;

        motion.ResetForPool();
        motion.gameObject.SetActive(false);
    }

    public void ReturnAllActiveMotions()
    {
        foreach (List<Motion> pool in motionPools.Values)
        {
            foreach (Motion motion in pool)
            {
                if (motion != null && motion.gameObject.activeSelf)
                    ReleaseMotion(motion);
            }
        }
    }

    public void PurgeAllMotionRuneEffects()
    {
        foreach (List<Motion> pool in motionPools.Values)
        {
            foreach (Motion motion in pool)
            {
                if (motion != null)
                    motion.ForceClearRuneEffects();
            }
        }
    }
    #endregion

    void EnsurePoolCapacity(ref List<GameObject>[] pools, GameObject[] prefabs)
    {
        if (prefabs == null)
            return;

        if (pools != null && pools.Length == prefabs.Length)
            return;

        List<GameObject>[] resized = CreatePools(prefabs.Length);
        if (pools != null)
        {
            int copyCount = Mathf.Min(pools.Length, resized.Length);
            for (int i = 0; i < copyCount; i++)
                resized[i] = pools[i];
        }

        pools = resized;
    }

    GameObject GetFromPool(GameObject[] prefabs, List<GameObject>[] pools, int index, string label)
    {
        if (prefabs == null || pools == null || index < 0 || index >= prefabs.Length || index >= pools.Length)
        {
            Debug.LogError(
                $"[PoolManager] {label} index {index} 범위 초과 " +
                $"(prefabs={(prefabs != null ? prefabs.Length : 0)}, pools={(pools != null ? pools.Length : 0)}).");
            return null;
        }

        if (prefabs[index] == null)
        {
            Debug.LogError($"[PoolManager] {label} prefabs[{index}]가 null입니다. Inspector에서 프리팹을 연결하세요.");
            return null;
        }

        GameObject select = null;

        foreach (GameObject item in pools[index])
        {
            if (!item.activeSelf)
            {
                select = item;
                select.SetActive(true);
                break;
            }
        }

        if (select == null)
        {
            select = Instantiate(prefabs[index], transform);
            pools[index].Add(select);
        }

        return select;
    }

    public void ReturnAllActiveToPool()
    {
        ReturnAllActiveInPools(enemyPools);
        ReturnAllActiveInPools(bossPools);
        ReturnAllActiveInPools(bossBulletPools);
        ReturnAllActiveInPools(gimmickPools);
        ReturnAllActiveInPools(coinPools);
        ReturnAllActiveInPools(chestPools);
        ReturnAllActiveMotions();
    }

    public void ReturnStageClearGimmicks()
    {
        if (gimmickPools == null)
            return;

        foreach (List<GameObject> pool in gimmickPools)
        {
            if (pool == null)
                continue;

            foreach (GameObject item in pool)
            {
                if (item == null || !item.activeSelf)
                    continue;

                if (item.GetComponent<StagePortal>() != null || item.GetComponent<ShopkeeperNpc>() != null)
                    item.SetActive(false);
            }
        }
    }

    public void ReturnActiveEnemiesAndBosses()
    {
        ReturnAllActiveInPools(enemyPools);
        ReturnAllActiveInPools(bossPools);
        ReturnAllActiveInPools(bossBulletPools);
    }

    public void ReturnActiveFieldDrops()
    {
        ReturnAllActiveInPools(coinPools);
        ReturnAllActiveInPools(chestPools);
    }

    static void ReturnAllActiveInPools(List<GameObject>[] pools)
    {
        if (pools == null) return;

        foreach (List<GameObject> pool in pools)
        {
            if (pool == null) continue;

            foreach (GameObject item in pool)
            {
                if (item != null && item.activeSelf)
                    item.SetActive(false);
            }
        }
    }
}