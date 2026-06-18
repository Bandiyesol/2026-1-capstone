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
    public float nextWaveDelay = 1.5f;   // 하나의 웨이브 클리어 후 다음 웨이브 개시까지의 대기 시간
    public float nextStageDelay = 3f;    // 스테이지 완전 클리어 시 다음 단계 전환 대기 시간

    [Header("상태")]
    public int currentWave; // 현재 진행 중인 웨이브 번호 (배열 인덱스 기준, 0부터 시작)

    [Header("바이옴 기믹 스포너들")]
    public BiomeGimmickSpawner[] gimmickSpawners;

    // --- 내부 상태 제어 변수 ---
    int aliveEnemyCount; // 현재 필드(또는 진행 중인 페이즈)에 생존해 있는 적의 총 수량 카운트
    bool isSpawning;     // 현재 코루틴 루프를 통해 몬스터들을 순차적으로 필드에 소환하는 중인지 나타내는 플래그
    bool started;        // 게임 시작 시 스테이지 가동 로직이 중복으로 실행되는 것을 차단하기 위한 플래그
    int preparedBossStageIndex = -1;
    int bossRollGeneration;
    bool bossPhaseCleared;
    bool isTransitioningWave;
    Coroutine spawnWaveCoroutine;
    int bossSpawnCommittedWave = -1;

    /// <summary>스테이지 시작·보스 알리미와 동일한 보스 후보 중 하나를 미리 선택합니다.</summary>
    public int SelectedBossSpawnDataIndex { get; private set; } = -1;

    /// <summary>
    /// 스테이지를 처음부터 시작하기 위해 웨이브 인덱스를 초기화하고 첫 웨이브를 가동하는 메서드
    /// </summary>
    public void StartStage()
    {
        StopSpawnWaveCoroutine();
        StopAllCoroutines();
        isSpawning = false;
        aliveEnemyCount = 0;
        bossPhaseCleared = false;
        isTransitioningWave = false;
        bossSpawnCommittedWave = -1;

        if (stageManager != null)
            PrepareBossForStage(stageManager.stageIndex);

        // [변경] FindObjectsOfType을 지우고, 인스펙터로 연결된 배열을 바로 사용!
        if (gimmickSpawners != null)
        {
            foreach (var spawner in gimmickSpawners)
            {
                if (spawner != null) // 혹시 모를 null 체크
                    spawner.ResetSpawner();
            }
        }

        currentWave = 0; // 웨이브 번호 초기화
        StartWave();     // 첫 번째 웨이브 스폰 루틴 시동
    }

    void StopSpawnWaveCoroutine()
    {
        if (spawnWaveCoroutine == null)
            return;

        StopCoroutine(spawnWaveCoroutine);
        spawnWaveCoroutine = null;
    }

    /// <summary>포털 전환 직전 — 이전 스테이지 웨이브 코루틴을 정리합니다.</summary>
    public void AbortStageTransition()
    {
        StopSpawnWaveCoroutine();
        StopAllCoroutines();
        isSpawning = false;
        aliveEnemyCount = 0;
        bossPhaseCleared = false;
        isTransitioningWave = false;
        bossSpawnCommittedWave = -1;
    }

    public void PrepareBossForStage(int stageIndex, bool forceReroll = false)
    {
        if (!forceReroll && preparedBossStageIndex == stageIndex && SelectedBossSpawnDataIndex >= 0)
            return;

        preparedBossStageIndex = stageIndex;
        SelectedBossSpawnDataIndex = -1;

        if (stageManager == null || stageManager.stageDatas == null
            || stageIndex < 0 || stageIndex >= stageManager.stageDatas.Length)
        {
            return;
        }

        WaveData bossWave = FindFirstBossWave(stageManager.stageDatas[stageIndex]);
        if (bossWave?.bossSpawnIndexes == null || bossWave.bossSpawnIndexes.Length == 0)
            return;

        int[] candidates = BossSpawnDataIndexUtility.SanitizeArray(bossWave.bossSpawnIndexes);
        if (candidates == null || candidates.Length == 0)
        {
            Debug.LogError($"[WaveManager] 스테이지 {stageIndex + 1} 보스 spawn 인덱스가 유효하지 않습니다.");
            return;
        }

        SelectedBossSpawnDataIndex = PickRandomBossSpawnIndex(candidates);
        LogBossRoll(stageIndex, candidates, SelectedBossSpawnDataIndex);
    }

    int PickRandomBossSpawnIndex(int[] bossSpawnIndexes)
    {
        if (bossSpawnIndexes == null || bossSpawnIndexes.Length == 0)
            return -1;

        if (bossSpawnIndexes.Length == 1)
            return bossSpawnIndexes[0];

        int pick = Random.Range(0, bossSpawnIndexes.Length);
        if (bossRollGeneration > 0)
            pick = (pick + bossRollGeneration) % bossSpawnIndexes.Length;

        return bossSpawnIndexes[pick];
    }

    /// <summary>새 판·스테이지 진입 시 이전 선택을 버리고 보스를 다시 뽑습니다.</summary>
    public void RerollBossForStage(int stageIndex)
    {
        preparedBossStageIndex = -1;
        SelectedBossSpawnDataIndex = -1;
        PrepareBossForStage(stageIndex, forceReroll: true);
    }

    void LogBossRoll(int stageIndex, int[] candidates, int selectedSpawnIndex)
    {
        string bossName = ResolveBossNameForLog(selectedSpawnIndex);
        string candidateText = candidates != null ? string.Join(", ", candidates) : "—";
        Debug.Log(
            $"[WaveManager] 스테이지 {stageIndex + 1} 보스 선택 — spawnData[{selectedSpawnIndex}] ({bossName}), " +
            $"후보 [{candidateText}], rollGen={bossRollGeneration}");
    }

    string ResolveBossNameForLog(int spawnDataIndex)
    {
        spawnDataIndex = BossSpawnDataIndexUtility.Normalize(spawnDataIndex);
        if (spawner == null || spawnDataIndex < 0 || spawnDataIndex >= spawner.spawnData.Length)
            return "unknown";

        SpawnData data = spawner.GetSpawnData(spawnDataIndex);
        if (!data.isBoss || GameManager.instance?.pool?.bossPrefabs == null)
            return "unknown";

        GameObject[] bossPrefabs = GameManager.instance.pool.bossPrefabs;
        if (data.prefabIndex < 0 || data.prefabIndex >= bossPrefabs.Length || bossPrefabs[data.prefabIndex] == null)
            return "unknown";

        return bossPrefabs[data.prefabIndex].name;
    }

    static WaveData FindFirstBossWave(StageData stageData)
    {
        if (stageData?.waves == null)
            return null;

        for (int i = 0; i < stageData.waves.Length; i++)
        {
            if (stageData.waves[i].isBossWave)
                return stageData.waves[i];
        }

        return null;
    }

    /// <summary>
    /// 외부(UI 또는 게임 제어 스크립트)에서 스테이지 시작을 트리거하는 메인 진입 메서드
    /// </summary>
    public void Begin()
    {
        // 이미 스테이지가 기동 중이라면 중복 실행 방지를 위해 차단
        if (started) return;
        started = true;
        StartStage();
    }

    /// <summary>
    /// 게임 도중 메인 메뉴로 나가거나 게임을 완전 리셋할 때 상태 플래그 및 카운트를 클리어하는 메서드
    /// </summary>
    public void ResetForMainMenu()
    {
        StopSpawnWaveCoroutine();
        StopAllCoroutines(); // 가동 중인 스폰 및 대기 코루틴 전면 중지
        started = false;     // 실행 상태 플래그 초기화
        isSpawning = false;   // 소환 중 플래그 초기화
        aliveEnemyCount = 0; // 필드 생존 카운트 초기화
        currentWave = 0;     // 웨이브 인덱스 초기화
        preparedBossStageIndex = -1;
        SelectedBossSpawnDataIndex = -1;
        bossRollGeneration++;
        bossPhaseCleared = false;
        isTransitioningWave = false;
        bossSpawnCommittedWave = -1;
    }

    public bool IsBossPhaseCleared => bossPhaseCleared;

    /// <summary>
    /// 현재 설정된 웨이브 스폰 코루틴을 안전하게 시동하는 래퍼 메서드
    /// </summary>
    void StartWave()
    {
        if (bossPhaseCleared || stageManager == null || stageManager.stageDatas == null)
            return;

        StageData stageData = stageManager.stageDatas[stageManager.stageIndex];
        if (stageData?.waves == null || currentWave >= stageData.waves.Length)
            return;

        StopSpawnWaveCoroutine();
        spawnWaveCoroutine = StartCoroutine(SpawnWaveTracked());
    }

    IEnumerator SpawnWaveTracked()
    {
        yield return SpawnWave();
        spawnWaveCoroutine = null;
    }

    /// <summary>
    /// [메인 제어 루틴] 타이밍 버그가 수정된 안전한 스폰 제어 코루틴
    /// </summary>
    /// <summary>
    /// [메인 제어 루틴] 타이밍 버그가 수정된 안전한 스폰 제어 코루틴
    /// </summary>
    IEnumerator SpawnWave()
    {
        if (bossPhaseCleared)
            yield break;

        isSpawning = true;
        aliveEnemyCount = 0;

        var stageData = stageManager.stageDatas[stageManager.stageIndex];
        if (stageData.waves == null || currentWave >= stageData.waves.Length)
            yield break;

        WaveData wave = stageData.waves[currentWave];

        // 🎯 [분기 1] 보스 웨이브인 경우
        if (wave.isBossWave)
        {
            if (wave.enemies != null && wave.enemies.Length > 0)
            {
                yield return StartCoroutine(SpawnNormalWave(wave));
                Debug.Log($"[WaveManager] 선결 잡몹 스폰 완료, aliveEnemyCount={aliveEnemyCount}");
                yield return new WaitUntil(() => aliveEnemyCount <= 0);
                Debug.Log("[WaveManager] 선결 잡몹 전멸 확인, 보스 스폰 진행");
            }

            SpawnBossWave(wave);
            Debug.Log("[WaveManager] SpawnBossWave 호출됨");

            // 여기서 보스가 죽을 때까지 코루틴이 대기합니다.
            yield return new WaitUntil(() => aliveEnemyCount <= 0);

            // =========================================================
            // [여기에 추가] 보스가 죽어서 대기가 풀리면 즉시 보스 스테이지 클리어 처리!
            // =========================================================
            NotifyBossStageCleared();
        }
        // 🎯 [분기 2] 일반 잡몹 웨이브인 경우
        else
        {
            yield return StartCoroutine(SpawnNormalWave(wave));
        }

        isSpawning = false;

        // 보스 웨이브는 OnEnemyDead에서만 다음 단계로 넘깁니다.
        if (!wave.isBossWave && aliveEnemyCount <= 0)
            NextWave();
    }

    /// <summary>
    /// 웨이브 데이터를 기반으로 몬스터 풀을 구성하고, 무작위로 섞은(Shuffle) 뒤 순차 스폰하는 코루틴
    /// </summary>
    IEnumerator SpawnNormalWave(WaveData wave)
    {
        // 이번 페이즈에 스폰될 몬스터들의 SpawnData 인덱스(ID)들을 담을 임시 큐(리스트)
        List<int> spawnQueue = new List<int>();

        // 1. 데이터에 지정된 종류와 마리수만큼 인덱스를 반복 등록하고, 총 생존 카운트를 선제 누적
        for (int i = 0; i < wave.enemies.Length; i++)
        {
            EnemySpawnInfo info = wave.enemies[i];
            for (int j = 0; j < info.spawnCount; j++)
            {
                spawnQueue.Add(info.spawnDataIndex); // 몬스터 ID 등록
                aliveEnemyCount++;                  // 필드 생존 목표 수량 증가
            }
        }

        // 2. 피셔-예이츠(Fisher-Yates) 셔플 알고리즘을 사용하여 소환될 몬스터들의 순서를 무작위로 혼합
        for (int i = spawnQueue.Count - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            int temp = spawnQueue[i];
            spawnQueue[i] = spawnQueue[rand];
            spawnQueue[rand] = temp;
        }

        // 3. 완전히 섞인 큐를 순회하며 실시간으로 몬스터를 스폰하고, 각 개체별 지정된 개별 스폰 주기를 대기
        for (int i = 0; i < spawnQueue.Count; i++)
        {
            SpawnEnemy(spawnQueue[i]); // 실질적인 생성 및 매니저 링크 주입

            // Spawner를 통해 해당 몬스터의 고유 간격(Time)을 인출하여 대기 텀 적용
            SpawnData data = spawner.GetSpawnData(spawnQueue[i]);
            yield return new WaitForSeconds(data.spawnTime);
        }
    }

    /// <summary>
    /// 보스 배열 중 임의의 보스 하나를 선정하여 필드에 생성하고 카운트를 잡는 메서드
    /// </summary>
    void SpawnBossWave(WaveData wave)
    {
        if (bossSpawnCommittedWave == currentWave)
        {
            Debug.LogWarning($"[WaveManager] Wave {currentWave} 보스가 이미 스폰됨 — 중복 SpawnBossWave 무시");
            return;
        }

        if (HasActiveStageBoss())
        {
            Debug.LogWarning("[WaveManager] 필드에 활성 보스가 남아 있어 중복 스폰을 차단합니다.");
            aliveEnemyCount = 1;
            bossSpawnCommittedWave = currentWave;
            return;
        }

        // 보스 등장 전 호위 몬스터·소환 잔여물 정리 (카운트 불일치 시 타 바이옴 몬스터 겹침 방지)
        PoolManager.Instance?.ReturnActiveEnemiesAndBosses();

        // 보스 스폰 인덱스 배열이 비어있다면 에러 방지를 위해 즉시 다음 웨이브/클리어 처리
        if (wave.bossSpawnIndexes == null || wave.bossSpawnIndexes.Length == 0)
        {
            NextWave();
            return;
        }

        int[] candidates = BossSpawnDataIndexUtility.SanitizeArray(wave.bossSpawnIndexes);

        int spawnIndex = BossSpawnDataIndexUtility.Normalize(SelectedBossSpawnDataIndex);
        if (spawnIndex < 0 || System.Array.IndexOf(candidates, spawnIndex) < 0)
        {
            spawnIndex = PickRandomBossSpawnIndex(candidates);
            SelectedBossSpawnDataIndex = spawnIndex;
        }

        bossSpawnCommittedWave = currentWave;
        SpawnEnemy(spawnIndex);
        GameManager.instance?.RefreshBossBriefingForBossSpawn(spawnIndex);

        // 보스 본체 개체 카운트를 1로 확정 명시
        aliveEnemyCount = 1;
    }

    bool HasActiveStageBoss()
    {
        BossBase[] bosses = Object.FindObjectsByType<BossBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < bosses.Length; i++)
        {
            BossBase boss = bosses[i];
            if (boss == null || !boss.gameObject.activeInHierarchy || boss.health <= 0f)
                continue;

            if (boss.waveManager != this)
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Spawner를 통해 실제 게임 오브젝트를 풀링 인출하고, 생성된 적에게 매니저 참조(this)를 주입하는 메서드
    /// </summary>
    void SpawnEnemy(int index)
    {
        GameObject enemy = spawner.Spawn(index);
        if (enemy == null) return; // 풀에 잔여 수량이 없거나 스폰 실패 시 예외 차단

        // [악세사리 훅] 신기한 화살 — 보스 스폰 알림
        BossBase boss = enemy.GetComponent<BossBase>();
        if (boss != null)
            AccessoryEffect.instance?.NotifyBossSpawn(enemy.transform);

        // [주입 1] 일반 몬스터 컴포넌트(Enemy)가 존재하면 웨이브 매니저 참조 전달
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
            enemyScript.waveManager = this;

        // [주입 2] 보스전 전용 컴포넌트(BossBase)가 존재하면 웨이브 매니저 참조 전달
        BossBase bossScript = enemy.GetComponent<BossBase>();
        if (bossScript != null)
            bossScript.waveManager = this;

        // [주입 3] 멀티 파트 보스 코어 (루트에 BossBase 없음)
        InjectBossCoreWaveManager(enemy);
    }

    void InjectBossCoreWaveManager(GameObject enemy)
    {
        FrostWolfCore frostCore = enemy.GetComponent<FrostWolfCore>();
        if (frostCore != null)
            frostCore.waveManager = this;

        VolcanoPumpkinCore volcanoCore = enemy.GetComponent<VolcanoPumpkinCore>();
        if (volcanoCore != null)
            volcanoCore.waveManager = this;

        LavaTyranoCore tyranoCore = enemy.GetComponent<LavaTyranoCore>();
        if (tyranoCore != null)
            tyranoCore.waveManager = this;
    }

    /// <summary>
    /// 필드의 적(일반/보스)이 처치되었을 때 매니저에게 사망 사실을 알리는 콜백 통보 메서드
    /// </summary>
    public void OnEnemyDead()
    {
        if (bossPhaseCleared)
            return;

        aliveEnemyCount--; // 생존 수량 차감

        // ★핵심 조건: 현재 코루틴을 통해 스폰이 진행 중인 상태가 아니며, 필드의 적이 완전히 전멸했을 때만 다음 웨이브 가동
        if (!isSpawning && aliveEnemyCount <= 0)
            NextWave();
    }

    /// <summary>보스 클리어 후 소환 잔여물·추가 웨이브를 차단합니다.</summary>
    public void NotifyBossStageCleared()
    {
        if (bossPhaseCleared)
            return;

        bossPhaseCleared = true;
        isTransitioningWave = false;
        StopSpawnWaveCoroutine();
        StopAllCoroutines();
        isSpawning = false;
        aliveEnemyCount = 0;
        bossSpawnCommittedWave = -1;

        if (stageManager != null && stageManager.stageDatas != null
            && stageManager.stageIndex >= 0 && stageManager.stageIndex < stageManager.stageDatas.Length)
        {
            currentWave = stageManager.stageDatas[stageManager.stageIndex].waves.Length;
        }

        PoolManager.Instance?.ReturnActiveEnemiesAndBosses();
    }

    /// <summary>
    /// 현재 웨이브를 마치고 인덱스를 증가시키며, 스테이지 종료 여부를 판정하는 메서드
    /// </summary>
    void NextWave()
    {
        if (bossPhaseCleared || isTransitioningWave)
            return;

        if (stageManager == null || stageManager.stageDatas == null)
            return;

        StageData stage = stageManager.stageDatas[stageManager.stageIndex];
        if (stage.waves == null || currentWave >= stage.waves.Length)
            return;

        isTransitioningWave = true;
        currentWave++;

        if (currentWave >= stage.waves.Length)
        {
            PoolManager.Instance?.ReturnActiveEnemiesAndBosses();
            isTransitioningWave = false;
            return;
        }

        StartCoroutine(StartWaveDelayed());
    }

    /// <summary>
    /// 웨이브 간의 정비 및 시각적 안정성을 위해 정해진 딜레이 타임만큼 프레임을 대기한 후 스폰을 재개하는 코루틴
    /// </summary>
    IEnumerator StartWaveDelayed()
    {
        yield return new WaitForSeconds(nextWaveDelay);
        isTransitioningWave = false;
        StartWave();
    }
}