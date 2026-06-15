using UnityEngine;

/// <summary>
/// 게임의 전체적인 스테이지 전개, 구역별 맵 오브젝트의 활성/비활성 스왑,
/// 그리고 스테이지 전환 시 플레이어 위치 초기화 등을 총괄하는 제어 매니저 클래스입니다.
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager instance;

    [Header("웨이브 매니저")]
    public WaveManager waveManager;

    [Header("현재 스테이지")]
    public int stageIndex;

    [Header("스테이지 오브젝트")]
    public GameObject[] stages;

    [Header("스테이지 데이터")]
    [Tooltip("모든 스테이지 데이터를 넣는 데이터")]
    public StageData[] stageDatas;

    [Header("엔딩")]
    [Tooltip("이 스테이지 번호 클리어 시 게임 종료 (BossStageConfigurationEditor 자동 설정)")]
    public int endingAfterStageNumber;

    public int CurrentStage => stageIndex + 1;

    public int TotalStages
    {
        get
        {
            if (stageDatas != null && stageDatas.Length > 0)
                return stageDatas.Length;

            if (stages != null && stages.Length > 0)
                return stages.Length;

            return 1;
        }
    }

    int MapCount => stages != null ? stages.Length : 0;

    int ResolveMapIndex(int index)
    {
        if (MapCount <= 0)
            return 0;

        if (index < MapCount)
            return index;

        return index % MapCount;
    }

    void Awake()
    {
        instance = this;
        EnsureStageMaps();
    }

    void EnsureStageMaps()
    {
        if (stages != null && stages.Length >= TotalStages)
            return;

        GameObject stagesRoot = GameObject.Find("Stages");
        if (stagesRoot == null)
            return;

        int childCount = stagesRoot.transform.childCount;
        if (childCount <= 0)
            return;

        var maps = new GameObject[childCount];
        for (int i = 0; i < childCount; i++)
            maps[i] = stagesRoot.transform.GetChild(i).gameObject;

        stages = maps;
    }

    void Start()
    {
        UpdateStage();
    }

    public void ResetToFirstStage()
    {
        stageIndex = 0;
        EnsureStageMaps();
        UpdateStage();

        if (GameManager.instance?.player != null)
            GameManager.instance.player.transform.position = Vector3.zero;
    }

    void UpdateStage()
    {
        if (MapCount <= 0)
            return;

        int activeMapIndex = ResolveMapIndex(stageIndex);
        for (int i = 0; i < MapCount; i++)
        {
            bool shouldBeActive = i == activeMapIndex;
            if (stages[i].activeSelf != shouldBeActive)
                stages[i].SetActive(shouldBeActive);
        }
    }

    public bool NextStage()
    {
        Debug.Log($"NextStage 호출 - stageIndex: {stageIndex}, TotalStages: {TotalStages}");

        if (stageIndex >= TotalStages - 1)
        {
            GameManager.instance.GameVictory();
            return false;
        }

        stageIndex++;
        UpdateStage();

        if (GameManager.instance?.player != null)
            GameManager.instance.player.transform.position = Vector3.zero;

        return true;
    }
}
