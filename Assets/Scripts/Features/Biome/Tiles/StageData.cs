using UnityEngine;

// 웨이브 내 일반 적 정보
[System.Serializable]
public class EnemySpawnInfo
{
    [Tooltip("일반 몬스터에 해당하는 spawner.spawndata 인덱스 입력 (첫번째 줄)")]
    public int spawnDataIndex; // Spawner.spawnData 인덱스

    [Tooltip("등장할 몬스터 수")]
    public int spawnCount;     // 소환 수
}

// 웨이브 1개 데이터
[System.Serializable]
public class WaveData
{
    [Header("보스 웨이브 여부")]
    [Tooltip("보스 웨이브인지 아닌지를 구분해 주는 플래그 변수")]
    public bool isBossWave;

    [Header("보스 후보 (랜덤 1마리)")]
    [Tooltip("spawner 안의 보스로 취급하는 spawnerdata 배열 인덱스 번호를 여러 개 입력 시, 랜덤으로 한마리 소환")]
    public int[] bossSpawnIndexes;

    [Header("일반 적")]
    [Tooltip("소환할 일반 몬스터 설정")]
    public EnemySpawnInfo[] enemies;
}

// 스테이지 전체 데이터
[System.Serializable]
public class StageData
{
    [Tooltip("모든 웨이브 데이터를 넣는 데이터")]
    public WaveData[] waves;
}