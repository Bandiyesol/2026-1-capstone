using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem; // 새 Input System 네임스페이스 추가

public class Spawner : MonoBehaviour
{
    // 자식 트랜스폼들(0번은 자기 자신, 1번부터 실제 스폰 포인트)
    public Transform[] spawnPoint;
    // 시간대별/레벨별 스폰 설정 데이터
    public SpawnData[] spawnData;
    // 현재 게임 시간 기준 스폰 단계
    int level;
    // 마지막 스폰 이후 경과 시간
    float timer;

    void Awake()
    {
        // 자식 오브젝트를 스폰 포인트 배열로 캐싱
        spawnPoint = GetComponentsInChildren<Transform>();
    }
    void Update()
    {
        // 게임 정지 상태에서는 스폰 중단
        if (!GameManager.instance.isLive)
            return;
            
        timer += Time.deltaTime;
        // 10초마다 레벨 상승, 배열 범위를 넘지 않도록 clamp
        level = Mathf.Min(Mathf.FloorToInt(GameManager.instance.gameTime / 10f), spawnData.Length - 1);

        // 레벨별 스폰 주기를 넘기면 적 생성
        if (timer > spawnData[level].spawnTime)
        {
            timer = 0;
            Spawn();
        }
    }

    void Spawn()
    {
        // 0번 풀(적 프리팹)에서 가져와 랜덤 스폰 포인트에 배치
        GameObject enemy = GameManager.instance.pool.Get(0);
        enemy.transform.position = spawnPoint[Random.Range(1,spawnPoint.Length)].position;
        // 현재 레벨 데이터로 적 능력치 초기화
        enemy.GetComponent<Enemy>().Init(spawnData[level]);
    }
}

[System.Serializable]
public class SpawnData
{
    // 스폰 간격(초)
    public float spawnTime;
    // 사용할 스프라이트/애니메이터 타입
    public int spriteType;
    // 적 체력
    public int health;
    // 적 이동 속도
    public float speed;
}