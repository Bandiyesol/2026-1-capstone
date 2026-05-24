using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [Header("적 프리팹")]
    public GameObject[] enemyPrefabs;

    [Header("보스 프리팹")]
    public GameObject[] bossPrefabs;

    [Header("보스 탄막 프리팹")]
    public GameObject[] bossBulletPrefabs;

    [Header("기믹 프리팹")]
    public GameObject[] gimmickPrefabs;

    List<GameObject>[] enemyPools;
    List<GameObject>[] bossPools;
    List<GameObject>[] bossBulletPools;
    List<GameObject>[] gimmickPools;

    void Awake()
    {
        enemyPools = CreatePools(enemyPrefabs.Length);
        bossPools = CreatePools(bossPrefabs.Length);
        bossBulletPools = CreatePools(bossBulletPrefabs.Length);
        gimmickPools = CreatePools(gimmickPrefabs.Length);
    }

    List<GameObject>[] CreatePools(int count)
    {
        List<GameObject>[] pools = new List<GameObject>[count];

        for (int i = 0; i < count; i++)
            pools[i] = new List<GameObject>();

        return pools;
    }

    public GameObject GetEnemy(int index)
    {
        return GetFromPool(enemyPrefabs, enemyPools, index);
    }

    public GameObject GetBoss(int index)
    {
        return GetFromPool(bossPrefabs, bossPools, index);
    }

    public GameObject GetBossBullet(int index)
    {
        return GetFromPool(bossBulletPrefabs, bossBulletPools, index);
    }

    public GameObject GetGimmick(int index)
    {
        return GetFromPool(gimmickPrefabs, gimmickPools, index);
    }

    GameObject GetFromPool(GameObject[] prefabs, List<GameObject>[] pools, int index)
    {
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
}
