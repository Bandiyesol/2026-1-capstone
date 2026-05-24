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
        spawnPoint = GetComponentsInChildren<Transform>();
    }

    void Start()
    {
        pool = GameManager.instance.pool;
    }

    public Transform GetRandomPoint()
    {
        return spawnPoint[Random.Range(1, spawnPoint.Length)];
    }

    public SpawnData GetSpawnData(int index)
    {
        index = Mathf.Clamp(index, 0, spawnData.Length - 1);
        return spawnData[index];
    }

    public GameObject Spawn(int index)
    {
        SpawnData data = GetSpawnData(index);
        Transform point = GetRandomPoint();

        GameObject obj = data.isBoss
            ? pool.GetBoss(data.prefabIndex)
            : pool.GetEnemy(data.prefabIndex);

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
    public float spawnTime;
    public int prefabIndex;
}
