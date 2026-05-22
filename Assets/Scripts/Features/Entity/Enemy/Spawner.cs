using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("스폰 포인트")]
    public Transform[] spawnPoint;

    [Header("스폰 데이터")]
    public SpawnData[] spawnData;

    // PoolManager
    PoolManager pool;

    void Awake()
    {
        spawnPoint = GetComponentsInChildren<Transform>();
    }

    void Start()
    {
        pool = GameManager.instance.pool;
    }

    // 랜덤 위치 반환
    public Transform GetRandomPoint()
    {
        return spawnPoint[
            Random.Range(1, spawnPoint.Length)
        ];
    }

    // 스폰 데이터 반환
    public SpawnData GetSpawnData(int index)
    {
        index = Mathf.Clamp
        (
            index,
            0,
            spawnData.Length - 1
        );

        return spawnData[index];
    }

    // 적 또는 보스 스폰
    public GameObject Spawn(int index)
    {
        // 데이터 가져오기
        SpawnData data = GetSpawnData(index);

        // 위치 가져오기
        Transform point = GetRandomPoint();

        GameObject obj;

        // 보스
        if (data.isBoss)
        {
            obj = pool.GetBoss(data.prefabIndex);
        }
        // 일반 적
        else
        {
            obj = pool.GetEnemy(data.prefabIndex);
        }

        // 위치 설정
        obj.transform.position = point.position;

        // 회전 초기화
        obj.transform.rotation = Quaternion.identity;

        return obj;
    }
}

[System.Serializable]
public class SpawnData
{
    [Header("타입")]
    // 보스 여부
    public bool isBoss;

    [Header("스폰")]
    // 스폰 간격
    public float spawnTime;
    // 프리팹 번호
    public int prefabIndex;

    /*[Header("보스 연출")]
    // 등장 연출 사용 여부
    public bool useBossIntro;
    // 스폰 이펙트
    public GameObject spawnEffect;
    // 보스 BGM
    public AudioClip bossBgm;*/
}