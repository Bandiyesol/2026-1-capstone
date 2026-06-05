using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    // 다른 스크립트에서 접근 가능한 싱글톤 인스턴스
    public static PoolManager Instance { get; private set; }

    [Header("프리팹 배열 설정")]
    public GameObject[] enemyPrefabs;
    public GameObject[] bossPrefabs;
    public GameObject[] bossBulletPrefabs;
    public GameObject[] gimmickPrefabs; // 마법진(포탈) 등 오브젝트 포함
    public GameObject[] coinPrefabs;
    public GameObject[] chestPrefabs;

    // 오브젝트 풀을 관리할 리스트 배열
    List<GameObject>[] enemyPools;
    List<GameObject>[] bossPools;
    List<GameObject>[] bossBulletPools;
    List<GameObject>[] gimmickPools;
    List<GameObject>[] coinPools;
    List<GameObject>[] chestPools;

    void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 각 프리팹 개수에 맞춰 오브젝트 풀(리스트 배열) 생성
        enemyPools = CreatePools(enemyPrefabs.Length);
        bossPools = CreatePools(bossPrefabs.Length);
        bossBulletPools = CreatePools(bossBulletPrefabs.Length);
        gimmickPools = CreatePools(gimmickPrefabs.Length);
        coinPools = CreatePools(coinPrefabs != null ? coinPrefabs.Length : 0);
        chestPools = CreatePools(chestPrefabs != null ? chestPrefabs.Length : 0);
    }

    // 지정된 개수만큼 빈 리스트(풀) 배열을 생성하는 메서드
    List<GameObject>[] CreatePools(int count)
    {
        List<GameObject>[] pools = new List<GameObject>[count];
        for (int i = 0; i < count; i++)
            pools[i] = new List<GameObject>();
        return pools;
    }

    #region 오브젝트 풀 가져오기 메서드들 (Get)
    public GameObject GetEnemy(int index) => GetFromPool(enemyPrefabs, enemyPools, index);
    public GameObject GetBoss(int index) => GetFromPool(bossPrefabs, bossPools, index);
    public GameObject GetBossBullet(int index) => GetFromPool(bossBulletPrefabs, bossBulletPools, index);
    public GameObject GetGimmick(int index) => GetFromPool(gimmickPrefabs, gimmickPools, index);

    public GameObject GetCoin(int index)
    {
        if (coinPrefabs == null || index < 0 || index >= coinPrefabs.Length) return null;
        return GetFromPool(coinPrefabs, coinPools, index);
    }

    public GameObject GetChest(int index)
    {
        if (chestPrefabs == null || index < 0 || index >= chestPrefabs.Length) return null;
        return GetFromPool(chestPrefabs, chestPools, index);
    }
    #endregion

    // 풀에서 비활성화된 오브젝트를 찾거나, 없으면 새로 생성해서 반환하는 핵심 로직
    GameObject GetFromPool(GameObject[] prefabs, List<GameObject>[] pools, int index)
    {
        GameObject select = null;

        // 1. 기존 풀에 쉬고 있는(비활성화) 오브젝트가 있다면 재사용
        foreach (GameObject item in pools[index])
        {
            if (!item.activeSelf)
            {
                select = item;
                select.SetActive(true); // 활성화하여 반환 준비
                break;
            }
        }

        // 2. 쉴 수 있는 오브젝트가 없다면 새로 생성(Instantiate) 후 풀에 추가
        if (select == null)
        {
            select = Instantiate(prefabs[index], transform);
            pools[index].Add(select);
        }

        return select;
    }

    // 현재 씬에서 활성화된 모든 풀링 오브젝트를 전부 비활성화(정리)
    public void ReturnAllActiveToPool()
    {
        ReturnAllActiveInPools(enemyPools);
        ReturnAllActiveInPools(bossPools);
        ReturnAllActiveInPools(bossBulletPools);
        ReturnAllActiveInPools(gimmickPools); // 다음 스테이지 이동 시 마법진도 여기서 자동 정리
        ReturnAllActiveInPools(coinPools);
        ReturnAllActiveInPools(chestPools);
    }

    // 하나의 풀 배열 내부에 켜져 있는 모든 오브젝트를 끄는 내부 메서드
    static void ReturnAllActiveInPools(List<GameObject>[] pools)
    {
        if (pools == null) return;

        foreach (List<GameObject> pool in pools)
        {
            if (pool == null) continue;

            foreach (GameObject item in pool)
            {
                if (item != null && item.activeSelf)
                    item.SetActive(false); // 오브젝트 비활성화 (풀로 반환)
            }
        }
    }
}