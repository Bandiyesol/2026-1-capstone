using UnityEngine;

// 바이옴 기믹 공통 생성기
public class BiomeGimmickSpawner : MonoBehaviour
{
    [Header("웨이브 매니저")]
    public WaveManager waveManager;

    [Header("스테이지 매니저")]
    public StageManager stageManager;

    [Header("PoolManager 기믹 인덱스들")]
    public int[] gimmickIndexes;

    [Header("기본 생성 간격")]
    public float spawnInterval = 4f;

    [Header("최소 생성 간격")]
    public float minSpawnInterval = 1.2f;

    [Header("웨이브당 생성 간격 감소량")]
    public float intervalDecreasePerWave = 0.2f;

    [Header("플레이어 주변 생성 반경")]
    public float spawnRadius = 15f;

    [Header("낙석 기믹 인덱스")]
    public int fallingRockIndex = 0;
    [Header("낙석 최소 거리")]
    public float fallingRockMinDistance = 2f;
    [Header("낙석 근접 생성 확률")]
    [Range(0f, 1f)]
    public float fallingRockNearChance = 0.25f;

    [Header("기믹 최소 간격")]
    public float minSpawnDistance = 3f;

    [Header("기본 생성 개수")]
    public int baseSpawnCount = 1;

    [Header("웨이브당 추가 생성 개수")]
    public int extraSpawnPerWave = 1;

    [Header("최대 생성 개수")]
    public int maxSpawnCount = 4;

    [Header("연속 생성 간격")]
    public float spawnStepDelay = 0.25f;

    // 생성 타이머
    float timer;

    // 연속 생성 중인지
    bool isSpawnSequence;

    // 마지막 스테이지 번호
    int lastStageIndex = -1;

    void Update()
    {
        // 게임 정지 상태면 중단
        if (!GameManager.instance.isLive)
            return;

        // 기믹 없으면 중단
        if (gimmickIndexes == null || gimmickIndexes.Length == 0)
            return;

        // 웨이브 매니저 없으면 중단
        if (waveManager == null)
            return;

        // 현재 스테이지 웨이브가 끝났으면 생성 중단
        if (waveManager.currentWave >=
            stageManager.stageDatas[stageManager.stageIndex].waves.Length)
            return;

        // 현재 웨이브
        int wave = waveManager.currentWave;

        // 스테이지가 바뀌면 스폰 상태 초기화
        if (stageManager != null && lastStageIndex != stageManager.stageIndex)
        {
            lastStageIndex = stageManager.stageIndex;

            // 스폰 타이머 초기화
            timer = 0f;
        }

        // 웨이브가 진행될수록 생성 간격 감소
        float currentInterval = Mathf.Max(
            minSpawnInterval,
            spawnInterval - wave * intervalDecreasePerWave
        );

        // 시간 누적
        timer += Time.deltaTime;

        // 생성 시간 도달
        if (timer >= currentInterval)
        {
            timer = 0f;

            // 웨이브가 진행될수록 생성 개수 증가
            int spawnCount = Mathf.Min(
                maxSpawnCount,
                baseSpawnCount + wave * extraSpawnPerWave
            );

            // 개수만큼 생성
            if (!isSpawnSequence)
                StartCoroutine(SpawnSequence(spawnCount));
        }
    }

    void Spawn()
    {
        // 플레이어 위치
        Vector3 playerPos = GameManager.instance.player.transform.position;

        // 배열 중 하나 랜덤 선택
        int random = Random.Range(0, gimmickIndexes.Length);
        int gimmickIndex = gimmickIndexes[random];

        bool found = false;
        Vector3 spawnPos = Vector3.zero;

        // 너무 가까우면 다시 뽑기
        for (int tryCount = 0; tryCount < 10; tryCount++)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = playerPos + (Vector3)offset;

            // 낙석이면 가끔만 플레이어 바로 위 허용
            if (gimmickIndex == fallingRockIndex)
            {
                bool allowNear = Random.value < fallingRockNearChance;

                if (!allowNear &&
                    Vector3.Distance(candidate, playerPos) < fallingRockMinDistance)
                {
                    continue;
                }
            }

            bool overlapped = false;

            // 현재 스포너 자식들과 거리 검사
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);

                if (!child.gameObject.activeSelf)
                    continue;

                if (Vector3.Distance(candidate, child.position) < minSpawnDistance)
                {
                    overlapped = true;
                    break;
                }
            }

            // 괜찮은 위치 찾음
            if (!overlapped)
            {
                spawnPos = candidate;
                found = true;
                break;
            }
        }

        // 위치 못 찾았으면 이번 생성 생략
        if (!found)
            return;

        // 풀에서 기믹 가져오기
        GameObject gimmick = GameManager.instance.pool.GetGimmick(gimmickIndex);

        // 현재 스테이지 스포너 자식으로 설정
        gimmick.transform.SetParent(transform);

        // 위치 지정
        gimmick.transform.position = spawnPos;
    }
    System.Collections.IEnumerator SpawnSequence(int spawnCount)
    {
        isSpawnSequence = true;

        for (int i = 0; i < spawnCount; i++)
        {
            Spawn();

            // 마지막이 아니면 잠깐 대기
            if (i < spawnCount - 1)
                yield return new WaitForSeconds(spawnStepDelay);
        }

        isSpawnSequence = false;
    }
}
