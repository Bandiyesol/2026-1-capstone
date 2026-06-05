using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager instance;

    [Header("현재 스테이지")]
    public int stageIndex;

    [Header("스테이지 오브젝트")]
    public GameObject[] stages;

    /// <summary>
    /// stageDates -> 모든 웨이브 데이터를 넣는 데이터
    /// isBossWave -> 보스 웨이브인지 아닌지를 구분해 주는 플래그 변수
    /// bossSpawnIndexes -> Spawner 안의 보스로 취급하는 SpawnerData 배열 인덱스 번호를 여러 개 입력 시, 랜덤으로 한마리 소환
    /// enemies -> 소환할 일반 몬스터 설정
    /// enemies.spawnDataIndex -> 일반 몬스터에 해당하는 Spawner.spawnData 인덱스 입력 (첫번째 줄)
    /// enemies.spawnCount -> 등장할 몬스터 수
    /// </summary>
    [Header("스테이지 데이터")]
    public StageData[] stageDatas;

    public int CurrentStage => stageIndex + 1;

    public int TotalStages
    {
        get
        {
            if (stages != null && stages.Length > 0)
                return stages.Length;

            if (stageDatas != null && stageDatas.Length > 0)
                return stageDatas.Length;

            return 1;
        }
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        UpdateStage();
    }

    // 첫 스테이지 초기화
    public void ResetToFirstStage()
    {
        stageIndex = 0;
        UpdateStage();

        if (GameManager.instance?.player != null)
            GameManager.instance.player.transform.position = Vector3.zero;
    }

    // 현재 스테이지만 활성화
    void UpdateStage()
    {
        for (int i = 0; i < stages.Length; i++)
            stages[i].SetActive(i == stageIndex);
    }

    // 다음 스테이지 이동
    public bool NextStage()
    {
        // 마지막 스테이지 클리어
        if (stageIndex >= stages.Length - 1)
        {
            GameManager.instance.GameVictory();
            return false;
        }

        // 현재 비활성화
        stages[stageIndex].SetActive(false);

        // 인덱스 증가
        stageIndex++;

        // 다음 활성화
        stages[stageIndex].SetActive(true);

        // 플레이어 중앙 이동
        if (GameManager.instance?.player != null)
            GameManager.instance.player.transform.position = Vector3.zero;

        return true;
    }
}