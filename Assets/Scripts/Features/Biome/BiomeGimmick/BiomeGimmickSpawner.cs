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

    [Header("기본 생성 반경")]
    public float spawnRadius = 15f;

    [Header("암석 전용 생성 반경")]
    public float fallingRockSpawnRadius = 8f;

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

    // 연속 생성 여부
    bool isSpawnSequence;

    // 마지막 스테이지
    int lastStageIndex = -1;

    void Update()
    {
        // 게임 종료 시 중단
        if (!GameManager.instance.isLive)
            return;

        // 기믹 없음
        if (gimmickIndexes == null || gimmickIndexes.Length == 0)
            return;

        // 웨이브 매니저 없음
        if (waveManager == null)
            return;

        // 웨이브 종료 시 중단
        if (waveManager.currentWave >=
            stageManager.stageDatas[stageManager.stageIndex].waves.Length)
            return;

        int wave = waveManager.currentWave;

        // 스테이지 변경 시 초기화
        if (stageManager != null && lastStageIndex != stageManager.stageIndex)
        {
            lastStageIndex = stageManager.stageIndex;
            timer = 0f;
        }

        // 현재 생성 간격 계산
        float currentInterval = Mathf.Max(
            minSpawnInterval,
            spawnInterval - wave * intervalDecreasePerWave
        );

        // 타이머 증가
        timer += Time.deltaTime;

        // 생성 시점 도달
        if (timer >= currentInterval)
        {
            timer = 0f;

            // 생성 개수 계산
            int spawnCount = Mathf.Min(
                maxSpawnCount,
                baseSpawnCount + wave * extraSpawnPerWave
            );

            // 연속 생성 시작
            if (!isSpawnSequence)
                StartCoroutine(SpawnSequence(spawnCount));
        }
    }

    void Spawn()
    {
        // 플레이어 위치
        Vector3 playerPos = GameManager.instance.player.transform.position;

        // 랜덤 기믹 선택
        int random = Random.Range(0, gimmickIndexes.Length);
        int gimmickIndex = gimmickIndexes[random];

        bool found = false;
        Vector3 spawnPos = Vector3.zero;

        for (int tryCount = 0; tryCount < 10; tryCount++)
        {
            // 기본 반경 사용
            float currentRadius = spawnRadius;

            // 암석이면 전용 반경 사용
            if (gimmickIndex == fallingRockIndex)
                currentRadius = fallingRockSpawnRadius;

            // 생성 위치 후보
            Vector2 offset = Random.insideUnitCircle * currentRadius;
            Vector3 candidate = playerPos + (Vector3)offset;

            // 낙석 최소 거리 제한
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

            // 기존 기믹과 거리 검사
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

            // 유효 위치 발견
            if (!overlapped)
            {
                spawnPos = candidate;
                found = true;
                break;
            }
        }

        // 실패 시 중단
        if (!found)
            return;

        // 풀에서 가져오기
        GameObject gimmick = GameManager.instance.pool.GetGimmick(gimmickIndex);

        // 부모 지정
        gimmick.transform.SetParent(transform);

        // 위치 설정
        gimmick.transform.position = spawnPos;
    }

    System.Collections.IEnumerator SpawnSequence(int spawnCount)
    {
        isSpawnSequence = true;

        for (int i = 0; i < spawnCount; i++)
        {
            Spawn();

            // 다음 생성까지 대기
            if (i < spawnCount - 1)
                yield return new WaitForSeconds(spawnStepDelay);
        }

        isSpawnSequence = false;
    }
}