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

    // --- 내부 상태 제어 변수 ---
    int aliveEnemyCount; // 현재 필드(또는 진행 중인 페이즈)에 생존해 있는 적의 총 수량 카운트
    bool isSpawning;     // 현재 코루틴 루프를 통해 몬스터들을 순차적으로 필드에 소환하는 중인지 나타내는 플래그
    bool started;        // 게임 시작 시 스테이지 가동 로직이 중복으로 실행되는 것을 차단하기 위한 플래그

    /// <summary>
    /// 스테이지를 처음부터 시작하기 위해 웨이브 인덱스를 초기화하고 첫 웨이브를 가동하는 메서드
    /// </summary>
    public void StartStage()
    {
        currentWave = 0; // 웨이브 번호 초기화
        StartWave();     // 첫 번째 웨이브 스폰 루틴 시동
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
        StopAllCoroutines(); // 가동 중인 스폰 및 대기 코루틴 전면 중지
        started = false;     // 실행 상태 플래그 초기화
        isSpawning = false;   // 소환 중 플래그 초기화
        aliveEnemyCount = 0; // 필드 생존 카운트 초기화
        currentWave = 0;     // 웨이브 인덱스 초기화
    }

    /// <summary>
    /// 현재 설정된 웨이브 스폰 코루틴을 안전하게 시동하는 래퍼 메서드
    /// </summary>
    void StartWave()
    {
        StartCoroutine(SpawnWave());
    }

    /// <summary>
    /// [메인 제어 루틴] 타이밍 버그가 수정된 안전한 스폰 제어 코루틴
    /// </summary>
    IEnumerator SpawnWave()
    {
        isSpawning = true;   // 몬스터 소환 프로세스 시작 설정 (중간 정산 버그 방지)
        aliveEnemyCount = 0; // 이번 웨이브용 생존 카운트 리셋

        // 현재 진행 중인 스테이지 인덱스와 웨이브 인덱스를 기반으로 ScriptableObject 등에서 웨이브 데이터 인출
        WaveData wave = stageManager.stageDatas[stageManager.stageIndex].waves[currentWave];

        // 🎯 [분기 1] 보스 웨이브인 경우
        if (wave.isBossWave)
        {
            // 1단계: 보스 등장 전, 함께 배치된 선결 잡몹(호위 부대) 스폰 및 전멸 대기
            if (wave.enemies != null && wave.enemies.Length > 0)
            {
                // 잡몹 스폰 코루틴의 완료를 대기
                yield return StartCoroutine(SpawnNormalWave(wave));
                // 소환된 잡몹들이 플레이어에게 전멸당해 카운트가 0이 될 때까지 명확히 대기 프레임 유지
                yield return new WaitUntil(() => aliveEnemyCount <= 0);
            }

            // 2단계: 선결 잡몹이 모두 처리된 후 최종 보스 본체(코어) 소환
            SpawnBossWave(wave);

            // 💡 중요: 코어 소환 직후 다음 웨이브 검사 로직으로 바로 넘어가지 않게 
            // 보스 본체가 스스로 사망을 보고(OnEnemyDead 호출)할 때까지 여기서 무한 대기합니다.
            yield return new WaitUntil(() => aliveEnemyCount <= 0);
        }
        // 🎯 [분기 2] 일반 잡몹 웨이브인 경우
        else
        {
            // 지정된 잡몹 배치 테이블에 맞춰 스폰 진행 후 코루틴 종료 대기
            yield return StartCoroutine(SpawnNormalWave(wave));
        }

        isSpawning = false; // 모든 소환 프로세스가 안전하게 종료되었음을 마킹

        // 모든 소환 및 1프레임 안착 대기가 완전히 끝난 시점에만 최종적으로 적 수량을 체크하여 다음 단계 전환 결정
        if (aliveEnemyCount <= 0)
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
        // 보스 스폰 인덱스 배열이 비어있다면 에러 방지를 위해 즉시 다음 웨이브/클리어 처리
        if (wave.bossSpawnIndexes == null || wave.bossSpawnIndexes.Length == 0)
        {
            NextWave();
            return;
        }

        // 등록된 보스 풀 중 무작위로 하나를 선정하여 스폰
        int rand = Random.Range(0, wave.bossSpawnIndexes.Length);
        SpawnEnemy(wave.bossSpawnIndexes[rand]);

        // 보스 본체 개체 카운트를 1로 확정 명시
        aliveEnemyCount = 1;
    }

    /// <summary>
    /// Spawner를 통해 실제 게임 오브젝트를 풀링 인출하고, 생성된 적에게 매니저 참조(this)를 주입하는 메서드
    /// </summary>
    void SpawnEnemy(int index)
    {
        GameObject enemy = spawner.Spawn(index);
        if (enemy == null) return; // 풀에 잔여 수량이 없거나 스폰 실패 시 예외 차단

        // [주입 1] 일반 몬스터 컴포넌트(Enemy)가 존재하면 웨이브 매니저 참조 전달
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
            enemyScript.waveManager = this;

        // [주입 2] 보스전 전용 컴포넌트(BossBase)가 존재하면 웨이브 매니저 참조 전달
        BossBase bossScript = enemy.GetComponent<BossBase>();
        if (bossScript != null)
            bossScript.waveManager = this;
    }

    /// <summary>
    /// 필드의 적(일반/보스)이 처치되었을 때 매니저에게 사망 사실을 알리는 콜백 통보 메서드
    /// </summary>
    public void OnEnemyDead()
    {
        aliveEnemyCount--; // 생존 수량 차감

        // ★핵심 조건: 현재 코루틴을 통해 스폰이 진행 중인 상태가 아니며, 필드의 적이 완전히 전멸했을 때만 다음 웨이브 가동
        if (!isSpawning && aliveEnemyCount <= 0)
            NextWave();
    }

    /// <summary>
    /// 현재 웨이브를 마치고 인덱스를 증가시키며, 스테이지 종료 여부를 판정하는 메서드
    /// </summary>
    void NextWave()
    {
        currentWave++; // 웨이브 카운트 전진

        // 현재 진행 중인 스테이지의 전체 웨이브 정보 로드
        StageData stage = stageManager.stageDatas[stageManager.stageIndex];

        // 만약 현재 웨이브가 해당 스테이지가 보유한 총 웨이브 수에 도달하거나 넘어섰다면 전환 종료 (StageManager에서 클리어 처리)
        if (currentWave >= stage.waves.Length)
            return;

        // 아직 잔여 웨이브가 남아있다면 설정된 딜레이(예: 1.5초) 후에 다음 웨이브를 개시하는 코루틴 실행
        StartCoroutine(StartWaveDelayed());
    }

    /// <summary>
    /// 웨이브 간의 정비 및 시각적 안정성을 위해 정해진 딜레이 타임만큼 프레임을 대기한 후 스폰을 재개하는 코루틴
    /// </summary>
    IEnumerator StartWaveDelayed()
    {
        yield return new WaitForSeconds(nextWaveDelay); // 웨이브 사이 대기 시간 적용
        StartWave();                                   // 다음 웨이브 루프 시동
    }
}