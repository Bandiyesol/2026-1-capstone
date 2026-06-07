using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("스폰 포인트")]
    public Transform[] spawnPoint;

    [Header("스폰 데이터")]
    public SpawnData[] spawnData;

    PoolManager pool;

    void Awake()
    {
        if (spawnPoint == null || spawnPoint.Length == 0)
            spawnPoint = GetComponentsInChildren<Transform>();
    }

    void Start()
    {
        ResolvePool();
    }

    void ResolvePool()
    {
        if (pool != null)
            return;

        if (GameManager.instance != null)
            pool = GameManager.instance.pool;

        if (pool == null)
            pool = PoolManager.Instance;
    }

    public Transform GetRandomPoint()
    {
        if (spawnPoint == null || spawnPoint.Length == 0)
        {
            Debug.LogError("[Spawner] spawnPoint가 비어 있습니다.");
            return transform;
        }

        int index = spawnPoint.Length == 1 ? 0 : Random.Range(0, spawnPoint.Length);
        Transform point = spawnPoint[index];
        if (point != null)
            return point;

        Debug.LogWarning($"[Spawner] spawnPoint[{index}]가 null입니다. Spawner 위치를 사용합니다.");
        return transform;
    }

    public SpawnData GetSpawnData(int index)
    {
        index = Mathf.Clamp(index, 0, spawnData.Length - 1);
        return spawnData[index];
    }

    public GameObject Spawn(int index)
    {
        if (spawnData == null || spawnData.Length == 0)
        {
            Debug.LogError("[Spawner] spawnData가 비어 있습니다.");
            return null;
        }

        ResolvePool();
        if (pool == null)
        {
            Debug.LogError("[Spawner] PoolManager를 찾지 못했습니다.");
            return null;
        }

        SpawnData data = GetSpawnData(index);
        Transform point = GetRandomPoint();

        GameObject obj = data.isBoss
            ? pool.GetBoss(data.prefabIndex)
            : pool.GetEnemy(data.prefabIndex);

        if (obj == null)
        {
            Debug.LogError(
                $"[Spawner] 소환 실패 — spawnData[{index}] " +
                $"(isBoss={data.isBoss}, prefabIndex={data.prefabIndex}). " +
                "PoolManager 프리팹 배열을 확인하세요.");
            return null;
        }

        obj.transform.position = point.position;
        obj.transform.rotation = Quaternion.identity;

        return obj;
    }
}

[System.Serializable]
public class SpawnData
{
    [Header("타입")]
    public bool isBoss;

    [Header("스폰")]
    public float spawnTime = 0.2f;
    public int prefabIndex;
}
