using UnityEngine;

// 웨이브 안에서 어떤 적을 몇 마리 낼지
[System.Serializable]
public class EnemySpawnInfo
{
    public int spawnDataIndex; // Spawner.spawnData 번호
    public int spawnCount;     // 이 적 몇 마리
}

// 웨이브 하나의 적 목록
[System.Serializable]
public class WaveData
{
    public EnemySpawnInfo[] enemies;
}

// 스테이지 하나의 웨이브 목록
[System.Serializable]
public class StageData
{
    public WaveData[] waves;
}
