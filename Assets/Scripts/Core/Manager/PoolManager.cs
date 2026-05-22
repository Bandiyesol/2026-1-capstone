using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [Header("적 프리팹")]
    public GameObject[] enemyPrefabs;

    [Header("보스 프리팹")]
    public GameObject[] bossPrefabs;

    [Header("투사체 프리팹")]
    public GameObject[] projectilePrefabs;

    [Header("보스 탄막 프리팹")]
    public GameObject[] bossBulletPrefabs;

    [Header("기믹 프리팹")]
    public GameObject[] gimmickPrefabs;

    // 적 풀
    List<GameObject>[] enemyPools;

    // 보스 풀
    List<GameObject>[] bossPools;

    // 플레이어 탄환 풀
    List<GameObject>[] projectilePools;

    // 보스 탄막 풀
    List<GameObject>[] bossBulletPools;

    // 기믹 풀
    List<GameObject>[] gimmickPools;

    void Awake()
    {
        enemyPools = CreatePools(enemyPrefabs.Length);
        bossPools = CreatePools(bossPrefabs.Length);
        projectilePools = CreatePools(projectilePrefabs.Length);
        bossBulletPools = CreatePools(bossBulletPrefabs.Length);
        gimmickPools = CreatePools(gimmickPrefabs.Length);
    }

    // 풀 생성
    List<GameObject>[] CreatePools(int count)
    {
        List<GameObject>[] pools = new List<GameObject>[count];

        for (int i = 0; i < count; i++)
        {
            pools[i] = new List<GameObject>();
        }

        return pools;
    }

    // 적 가져오기
    public GameObject GetEnemy(int index)
    {
        return GetFromPool(enemyPrefabs, enemyPools, index);
    }

    // 보스 가져오기
    public GameObject GetBoss(int index)
    {
        return GetFromPool(bossPrefabs, bossPools, index);
    }

    // 플레이어 탄환 가져오기
    public GameObject GetProjectile(int index)
    {
        return GetFromPool(projectilePrefabs, projectilePools, index);
    }

    // 보스 탄막 가져오기
    public GameObject GetBossBullet(int index)
    {
        return GetFromPool(bossBulletPrefabs, bossBulletPools, index);
    }

    // 기믹 가져오기
    public GameObject GetGimmick(int index)
    {
        return GetFromPool(gimmickPrefabs, gimmickPools, index);
    }

    // 공통 풀 처리
    GameObject GetFromPool
    (
        GameObject[] prefabs,
        List<GameObject>[] pools,
        int index
    )
    {
        GameObject select = null;

        // 비활성 오브젝트 재사용
        foreach (GameObject item in pools[index])
        {
            if (!item.activeSelf)
            {
                select = item;
                select.SetActive(true);
                break;
            }
        }

        // 없으면 생성
        if (select == null)
        {
            select = Instantiate(prefabs[index], transform);

            pools[index].Add(select);
        }

        return select;
    }
}