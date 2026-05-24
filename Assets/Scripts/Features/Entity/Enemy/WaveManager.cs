using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("스테이지 관리자")]
    public StageManager stageManager;

    [Header("스폰 데이터")]
    public Spawner spawner;

    [Header("웨이브 딜레이 시간")]
    public float nextWaveDelay = 1.5f;

    [Header("다음 스테이지 대기 시간")]
    public float nextStageDelay = 3f;

    // 현재 웨이브 번호
    public int currentWave;

    // 현재 살아있는 적 수
    int aliveEnemyCount;

    // 현재 스폰 중인지
    bool isSpawning;
    // 게임 시작된 것인지
    bool started;

    // 스테이지 시작
    public void StartStage()
    {
        currentWave = 0;
        StartWave();
    }
    public void Begin()
    {
        if (started)
            return;

        started = true;
        StartStage();
    }

    // 웨이브 시작
    void StartWave()
    {
        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        isSpawning = true;
        aliveEnemyCount = 0;

        // 현재 웨이브 데이터
        WaveData wave =
            stageManager.stageDatas[stageManager.stageIndex]
            .waves[currentWave];

        // 실제 스폰 순서 큐
        List<int> spawnQueue = new List<int>();

        // 웨이브 적 정보를 큐에 넣기
        for (int i = 0; i < wave.enemies.Length; i++)
        {
            EnemySpawnInfo info = wave.enemies[i];

            for (int j = 0; j < info.spawnCount; j++)
            {
                spawnQueue.Add(info.spawnDataIndex);
                aliveEnemyCount++;
            }
        }

        // 섞어서 자연스럽게 등장
        for (int i = spawnQueue.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);

            int temp = spawnQueue[i];
            spawnQueue[i] = spawnQueue[rand];
            spawnQueue[rand] = temp;
        }

        // 큐 순서대로 스폰
        for (int i = 0; i < spawnQueue.Count; i++)
        {
            SpawnData data =
                spawner.GetSpawnData(spawnQueue[i]);

            GameObject enemy =
                spawner.Spawn(spawnQueue[i]);

            // 랜덤 스폰 포인트
            enemy.transform.position =
                spawner.GetRandomPoint().position;

            // 일반 적 연결
            Enemy enemyScript =
                enemy.GetComponent<Enemy>();

            if (enemyScript != null)
            {
                enemyScript.waveManager = this;
            }

            // 보스 연결
            BossBase bossScript =
                enemy.GetComponent<BossBase>();

            if (bossScript != null)
            {
                bossScript.waveManager = this;
            }

            // 적마다 지정된 스폰 간격
            yield return new WaitForSeconds(data.spawnTime);
        }

        // 스폰 끝
        isSpawning = false;

        // 스폰이 끝났는데 이미 적이 전부 죽었다면 다음 웨이브
        if (aliveEnemyCount <= 0)
        {
            NextWave();
        }
    }

    // 적 사망 알림
    public void OnEnemyDead()
    {
        aliveEnemyCount--;

        // 스폰 끝 + 살아있는 적 없음
        if (!isSpawning && aliveEnemyCount <= 0)
        {
            NextWave();
        }
    }

    // 다음 웨이브
    void NextWave()
    {
        currentWave++;

        StageData stage =
            stageManager.stageDatas[stageManager.stageIndex];

        // 현재 스테이지 웨이브 종료
        if (currentWave >= stage.waves.Length)
        {
            StartCoroutine(NextStageDelayed());
            return;
        }

        StartCoroutine(StartWaveDelayed());
    }
    IEnumerator StartWaveDelayed()
    {
        yield return new WaitForSeconds(nextWaveDelay);
        StartWave();
    }
    IEnumerator NextStageDelayed()
    {
        // 다음 스테이지 전환 전 대기
        yield return new WaitForSeconds(nextStageDelay);

        bool moved = stageManager.NextStage();

        // 다음 스테이지 있으면 시작
        if (moved)
        {
            StartStage();
        }
    }
}
