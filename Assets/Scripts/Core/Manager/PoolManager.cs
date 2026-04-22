using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    // 풀링 대상 프리팹 목록(인덱스로 접근)
    public GameObject[] prefabs;
    // 프리팹별 비활성 오브젝트를 보관하는 풀 리스트
    List<GameObject>[] pools;

    void Awake()
    {
        // 프리팹 개수만큼 풀 컨테이너 생성
        pools = new List<GameObject>[prefabs.Length];

        for (int index = 0; index < pools.Length; index++)
        {
            pools[index] = new List<GameObject>();
        }
    }

    public GameObject Get(int index)
    {
        // 반환할 오브젝트(기존 재사용 or 신규 생성)
        GameObject select = null;

        // 선택한 풀에서 비활성 오브젝트를 찾아 재활성화
        foreach (GameObject item in pools[index])
        {
            if (!item.activeSelf)
            {
                select = item;
                select.SetActive(true);
                break;
            }
        }

        // 재사용 가능한 오브젝트가 없으면 새로 생성해서 풀에 등록
        if (!select)
        {
            select = Instantiate(prefabs[index], transform);
            pools[index].Add(select);
        }

        // 호출한 쪽에서 위치/회전/초기화를 이어서 설정
        return select;
    }
}
