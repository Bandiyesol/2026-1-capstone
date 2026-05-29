using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("�������� ������")]
    public StageManager stageManager;

    [Header("���� ������")]
    public Spawner spawner;

    [Header("���̺� ������ �ð�")]
    public float nextWaveDelay = 1.5f;

    [Header("���� �������� ��� �ð�")]
    public float nextStageDelay = 3f;

    // ���� ���̺� ��ȣ
    public int currentWave;

    // ���� ����ִ� �� ��
    int aliveEnemyCount;

    // ���� ���� ������
    bool isSpawning;
    // ���� ���۵� ������
    bool started;

    // �������� ����
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

    /// <summary>���� �޴��� ���ư� �� ���̺ꡤ���� �ڷ�ƾ�� �ʱ�ȭ�մϴ�.</summary>
    public void ResetForMainMenu()
    {
        StopAllCoroutines();
        started = false;
        isSpawning = false;
        aliveEnemyCount = 0;
        currentWave = 0;
    }

    // ���̺� ����
    void StartWave()
    {
        StartCoroutine(SpawnWave());
    }

    IEnumerator SpawnWave()
    {
        isSpawning = true;
        aliveEnemyCount = 0;

        // ���� ���̺� ������
        WaveData wave =
            stageManager.stageDatas[stageManager.stageIndex]
            .waves[currentWave];

        // ���� ���� ���� ť
        List<int> spawnQueue = new List<int>();

        // ���̺� �� ������ ť�� �ֱ�
        for (int i = 0; i < wave.enemies.Length; i++)
        {
            EnemySpawnInfo info = wave.enemies[i];

            for (int j = 0; j < info.spawnCount; j++)
            {
                spawnQueue.Add(info.spawnDataIndex);
                aliveEnemyCount++;
            }
        }

        // ��� �ڿ������� ����
        for (int i = spawnQueue.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);

            int temp = spawnQueue[i];
            spawnQueue[i] = spawnQueue[rand];
            spawnQueue[rand] = temp;
        }

        // ť ������� ����
        for (int i = 0; i < spawnQueue.Count; i++)
        {
            SpawnData data =
                spawner.GetSpawnData(spawnQueue[i]);

            GameObject enemy =
                spawner.Spawn(spawnQueue[i]);

            // ���� ���� ����Ʈ
            enemy.transform.position =
                spawner.GetRandomPoint().position;

            // �Ϲ� �� ����
            Enemy enemyScript =
                enemy.GetComponent<Enemy>();

            if (enemyScript != null)
            {
                enemyScript.waveManager = this;
            }

            // ���� ����
            BossBase bossScript =
                enemy.GetComponent<BossBase>();

            if (bossScript != null)
            {
                bossScript.waveManager = this;
            }

            // ������ ������ ���� ����
            yield return new WaitForSeconds(data.spawnTime);
        }

        // ���� ��
        isSpawning = false;

        // ������ �����µ� �̹� ���� ���� �׾��ٸ� ���� ���̺�
        if (aliveEnemyCount <= 0)
        {
            NextWave();
        }
    }

    // �� ��� �˸�
    public void OnEnemyDead()
    {
        aliveEnemyCount--;

        // ���� �� + ����ִ� �� ����
        if (!isSpawning && aliveEnemyCount <= 0)
        {
            NextWave();
        }
    }

    // ���� ���̺�
    void NextWave()
    {
        currentWave++;

        StageData stage =
            stageManager.stageDatas[stageManager.stageIndex];

        // ���� �������� ���̺� ����
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
        // ���� �������� ��ȯ �� ���
        yield return new WaitForSeconds(nextStageDelay);

        bool moved = stageManager.NextStage();

        // ���� �������� ������ ����
        if (moved)
        {
            StartStage();
        }
    }
}
