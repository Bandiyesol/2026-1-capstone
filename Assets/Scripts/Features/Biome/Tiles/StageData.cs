using UnityEngine;

// 웨이브 내 일반 적 정보
[System.Serializable]
public class EnemySpawnInfo
{
    public int spawnDataIndex; // Spawner.spawnData 인덱스
    public int spawnCount;     // 소환 수
}

// 웨이브 1개 데이터
[System.Serializable]
public class WaveData
{
    [Header("보스 웨이브 여부")]
    public bool isBossWave;

    [Header("보스 후보 (랜덤 1마리)")]
    public int[] bossSpawnIndexes;

    [Header("일반 적")]
    public EnemySpawnInfo[] enemies;
}

// 스테이지 전체 데이터
[System.Serializable]
public class StageData
{
    public WaveData[] waves;
}