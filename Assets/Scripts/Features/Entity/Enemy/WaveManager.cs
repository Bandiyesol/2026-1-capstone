using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지별 웨이브 전개 및 일반/보스 몬스터 스폰을 총괄하는 웨이브 매니저
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("참조")]
    public StageManager stageManager; // 스테이지 데이터 및 인덱스 관리 컴포넌트
    public Spawner spawner;           // 오브젝트 풀링 기반의 실질적인 몬스터 생성기

    [Header("딜레이")]
    public float nextWaveDelay = 1.5f;   // 하나의 웨이브 클리어 후 다음 웨이브 개시까지의 딜레이
    public float nextStageDelay = 3f;    // 스테이지 완전 클리어 시 다음 단계 전환 딜레이

    [Header("상태")]
    public int currentWave; // 현재 진행 중인 웨이브 번호 (인덱스)

    int aliveEnemyCount; // 현재 필드(또는 해당 페이즈)에 살아있는 적의 총합 카운트
    bool isSpawning;     // 현재 코루틴을 통해 몬스터들을 순차 소환하고 있는 중인지 여부
    bool started;        // 스테이지 중복 실행을 막기 위한 가동 확인 플래그

    public void StartStage()
    {
        currentWave = 0;
        StartWave();
    }

    public void Begin()
    {
        if (started) return;
        started = true;
        StartStage();
    }

    public void ResetForMainMenu()
    {
        StopAllCoroutines();
        started = false;
        isSpawning = false;
        aliveEnemyCount = 0;
        currentWave = 0;
    }

    void StartWave()
    {
        StartCoroutine(SpawnWave());
    }

    /// <summary>
    /// [메인 제어 루틴] 타이밍 버그가 수정된 안전한 스폰 제어 코루틴
    /// </summary>
    IEnumerator SpawnWave()
    {
        isSpawning = true;
        aliveEnemyCount = 0;

        WaveData wave = stageManager.stageDatas[stageManager.stageIndex].waves[currentWave];

        // 🎯 [분기 1] 보스 웨이브인 경우
        // WaveManager.cs 의 SpawnWave 코루틴 중 보스 웨이브 부분
        if (wave.isBossWave)
        {
            // 1단계: 선결 잡몹 스폰 및 전멸 대기
            if (wave.enemies != null && wave.enemies.Length > 0)
            {
                yield return StartCoroutine(SpawnNormalWave(wave));
                // 잡몹들이 다 죽을 때까지 명확히 대기
                yield return new WaitUntil(() => aliveEnemyCount <= 0);
            }

            // 2단계: 최종 보스 본체(코어) 소환
            SpawnBossWave(wave);

            // 💡 중요: 코어 소환 직후 다음 웨이브 검사 로직으로 바로 넘어가지 않게 
            // 코어가 스스로 죽음을 보고할 때까지 여기서 무한 대기합니다.
            yield return new WaitUntil(() => aliveEnemyCount <= 0);
        }
        // 🎯 [분기 2] 일반 잡몹 웨이브인 경우
        else
        {
            yield return StartCoroutine(SpawnNormalWave(wave));
        }

        isSpawning = false;

        // 모든 소환 및 1프레임 안착 대기가 완전히 끝난 시점에만 적 수량을 체크합니다.
        if (aliveEnemyCount <= 0)
            NextWave();
    }

    IEnumerator SpawnNormalWave(WaveData wave)
    {
        List<int> spawnQueue = new List<int>();

        for (int i = 0; i < wave.enemies.Length; i++)
        {
            EnemySpawnInfo info = wave.enemies[i];
            for (int j = 0; j < info.spawnCount; j++)
            {
                spawnQueue.Add(info.spawnDataIndex);
                aliveEnemyCount++;
            }
        }

        for (int i = spawnQueue.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            int temp = spawnQueue[i];
            spawnQueue[i] = spawnQueue[rand];
            spawnQueue[rand] = temp;
        }

        for (int i = 0; i < spawnQueue.Count; i++)
        {
            SpawnEnemy(spawnQueue[i]);
            SpawnData data = spawner.GetSpawnData(spawnQueue[i]);
            yield return new WaitForSeconds(data.spawnTime);
        }
    }

    void SpawnBossWave(WaveData wave)
    {
        if (wave.bossSpawnIndexes == null || wave.bossSpawnIndexes.Length == 0)
        {
            NextWave();
            return;
        }

        int rand = Random.Range(0, wave.bossSpawnIndexes.Length);
        SpawnEnemy(wave.bossSpawnIndexes[rand]);

        // 보스 개체 카운트 설정
        aliveEnemyCount = 1;
    }

    void SpawnEnemy(int index)
    {
        GameObject enemy = spawner.Spawn(index);
        if (enemy == null) return;

        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
            enemyScript.waveManager = this;

        BossBase bossScript = enemy.GetComponent<BossBase>();
        if (bossScript != null)
            bossScript.waveManager = this;
    }

    public void OnEnemyDead()
    {
        aliveEnemyCount--;

        if (!isSpawning && aliveEnemyCount <= 0)
            NextWave();
    }

    void NextWave()
    {
        currentWave++;

        StageData stage = stageManager.stageDatas[stageManager.stageIndex];

        if (currentWave >= stage.waves.Length)
            return;

        StartCoroutine(StartWaveDelayed());
    }

	IEnumerator StartWaveDelayed()
	{
		yield return new WaitForSeconds(nextWaveDelay);

		if (currentWave > 0)
		{
			RuneSelectUI runeSelect = GameManager.instance != null
				? GameManager.instance.uiRuneSelect
				: null;
			if (runeSelect == null)
				runeSelect = FindFirstObjectByType<RuneSelectUI>(FindObjectsInactive.Include);

			if (runeSelect != null && RuneManager.instance != null)
			{
				bool confirmed = false;
				runeSelect.gameObject.SetActive(true);
				runeSelect.transform.SetAsLastSibling();
				runeSelect.ShowBetweenWaves(() => confirmed = true);
				yield return new WaitUntil(() => confirmed);
			}
		}

		StartWave();
	}
}