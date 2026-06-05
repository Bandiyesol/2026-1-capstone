using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("참조")]
    public StageManager stageManager;
    public Spawner spawner;

    [Header("딜레이")]
    public float nextWaveDelay = 1.5f;
    public float nextStageDelay = 3f;

    [Header("상태")]
    public int currentWave;

    int aliveEnemyCount; // 생존 적 수
    bool isSpawning;     // 소환 중 여부
    bool started;        // 시작 여부

    // 스테이지 시작
    public void StartStage()
    {
        currentWave = 0;
        StartWave();
    }

    // 최초 시작
    public void Begin()
    {
        if (started) return;

        started = true;
        StartStage();
    }

    // 메인메뉴 복귀용 초기화
    public void ResetForMainMenu()
    {
        StopAllCoroutines();

        started = false;
        isSpawning = false;
        aliveEnemyCount = 0;
        currentWave = 0;
    }

    // 웨이브 시작
    void StartWave()
    {
        StartCoroutine(SpawnWave());
    }

    // 웨이브 소환 루프
    IEnumerator SpawnWave()
    {
        isSpawning = true;
        aliveEnemyCount = 0;

        // 현재 웨이브 로드
        WaveData wave =
            stageManager.stageDatas[stageManager.stageIndex]
            .waves[currentWave];

        // 보스 웨이브
        if (wave.isBossWave)
        {
            SpawnBossWave(wave);
        }
        // 일반 웨이브
        else
        {
            yield return StartCoroutine(SpawnNormalWave(wave));
        }

        isSpawning = false;

        // 즉시 종료 체크
        if (aliveEnemyCount <= 0)
            NextWave();
    }

    // 일반 적 웨이브
    IEnumerator SpawnNormalWave(WaveData wave)
    {
        List<int> spawnQueue = new List<int>();

        // 소환 큐 생성
        for (int i = 0; i < wave.enemies.Length; i++)
        {
            EnemySpawnInfo info = wave.enemies[i];

            for (int j = 0; j < info.spawnCount; j++)
            {
                spawnQueue.Add(info.spawnDataIndex);
                aliveEnemyCount++;
            }
        }

        // 랜덤 셔플
        for (int i = spawnQueue.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);

            int temp = spawnQueue[i];
            spawnQueue[i] = spawnQueue[rand];
            spawnQueue[rand] = temp;
        }

        // 순차 소환
        for (int i = 0; i < spawnQueue.Count; i++)
        {
            SpawnEnemy(spawnQueue[i]);

            SpawnData data =
                spawner.GetSpawnData(spawnQueue[i]);

            yield return new WaitForSeconds(data.spawnTime);
        }
    }

    // 보스 웨이브
    void SpawnBossWave(WaveData wave)
    {
        // 후보 없으면 스킵
        if (wave.bossSpawnIndexes == null ||
            wave.bossSpawnIndexes.Length == 0)
        {
            NextWave();
            return;
        }

        // 랜덤 보스 선택
        int rand =
            Random.Range(0, wave.bossSpawnIndexes.Length);

        SpawnEnemy(wave.bossSpawnIndexes[rand]);

        aliveEnemyCount = 1;
    }

    // 실제 적 생성
    void SpawnEnemy(int index)
    {
        GameObject enemy = spawner.Spawn(index);

        if (enemy == null) return;

        // 일반 몹 연결
        Enemy enemyScript =
            enemy.GetComponent<Enemy>();

        if (enemyScript != null)
            enemyScript.waveManager = this;

        // 보스 연결
        BossBase bossScript =
            enemy.GetComponent<BossBase>();

        if (bossScript != null)
            bossScript.waveManager = this;
    }

    // 적 사망 콜백
    public void OnEnemyDead()
    {
        aliveEnemyCount--;

        // 소환 완료 + 전멸
        if (!isSpawning && aliveEnemyCount <= 0)
            NextWave();
    }

    // 다음 웨이브
    void NextWave()
    {
        currentWave++;

        StageData stage =
            stageManager.stageDatas[stageManager.stageIndex];

        // 스테이지 종료 시 아무것도 안 함 (포탈 활성화는 다른 곳에서 처리)
        if (currentWave >= stage.waves.Length)
            return;

        StartCoroutine(StartWaveDelayed());
    }

    // 웨이브 딜레이
    IEnumerator StartWaveDelayed()
    {
        yield return new WaitForSeconds(nextWaveDelay);
        StartWave();
    }
}