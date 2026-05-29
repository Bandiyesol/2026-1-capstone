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

    [Header("코인 드랍 프리팹 (0=동, 1=은, 2=금)")]
    public GameObject[] coinPrefabs;

    [Header("상자 드랍 프리팹 (0=일반, 1=희귀, 2=유니크, 3=전설)")]
    public GameObject[] chestPrefabs;

    List<GameObject>[] enemyPools;
    List<GameObject>[] bossPools;
    List<GameObject>[] bossBulletPools;
    List<GameObject>[] gimmickPools;
    List<GameObject>[] coinPools;
    List<GameObject>[] chestPools;

    void Awake()
    {
        enemyPools = CreatePools(enemyPrefabs.Length);
        bossPools = CreatePools(bossPrefabs.Length);
        bossBulletPools = CreatePools(bossBulletPrefabs.Length);
        gimmickPools = CreatePools(gimmickPrefabs.Length);
        coinPools = CreatePools(coinPrefabs != null ? coinPrefabs.Length : 0);
        chestPools = CreatePools(chestPrefabs != null ? chestPrefabs.Length : 0);
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

    public GameObject GetCoin(int index)
    {
        if (coinPrefabs == null || index < 0 || index >= coinPrefabs.Length)
            return null;

        return GetFromPool(coinPrefabs, coinPools, index);
    }

    public GameObject GetChest(int index)
    {
        if (chestPrefabs == null || index < 0 || index >= chestPrefabs.Length)
            return null;

        return GetFromPool(chestPrefabs, chestPools, index);
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

    public void ReturnAllActiveToPool()
    {
        ReturnAllActiveInPools(enemyPools);
        ReturnAllActiveInPools(bossPools);
        ReturnAllActiveInPools(bossBulletPools);
        ReturnAllActiveInPools(gimmickPools);
        ReturnAllActiveInPools(coinPools);
        ReturnAllActiveInPools(chestPools);
    }

    static void ReturnAllActiveInPools(List<GameObject>[] pools)
    {
        if (pools == null)
            return;

        foreach (List<GameObject> pool in pools)
        {
            if (pool == null)
                continue;

            foreach (GameObject item in pool)
            {
                if (item != null && item.activeSelf)
                    item.SetActive(false);
            }
        }
    }
}
