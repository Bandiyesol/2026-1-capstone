using UnityEngine;

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

    bool HasValidStageMaps()
    {
        if (stages == null || stages.Length == 0)
            return false;

        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i] == null)
                return false;
        }

        return true;
    }

    void EnsureStageMaps()
    {
        if (HasValidStageMaps())
            return;

        Transform stagesRoot = null;
        if (waveManager != null)
            stagesRoot = waveManager.transform;
        else
        {
            GameObject found = GameObject.Find("Stages");
            if (found != null)
                stagesRoot = found.transform;
        }

        if (stagesRoot == null || stagesRoot.childCount <= 0)
        {
            Debug.LogWarning("[StageManager] stages를 찾지 못했습니다. Inspector의 stages 또는 Stages 오브젝트를 확인하세요.");
            return;
        }

        int childCount = stagesRoot.childCount;
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
            if (stages[i] == null)
                continue;

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

        PotionEffect.instance?.OnStageChanged();

        stageIndex++;
        UpdateStage();

        if (GameManager.instance?.player != null)
            GameManager.instance.player.transform.position = Vector3.zero;

        return true;
    }
}
