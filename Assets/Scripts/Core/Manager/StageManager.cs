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
        // 처음엔 그냥 인덱스 0만 켜고 나머지는 끄기
        for (int i = 0; i < stages.Length; i++)
            stages[i].SetActive(i == 0);
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
        {
            bool shouldBeActive = (i == stageIndex);
            // 이미 원하는 상태면 SetActive 호출 자체를 스킵
            if (stages[i].activeSelf != shouldBeActive)
                stages[i].SetActive(shouldBeActive);
        }
    }

    // 다음 스테이지 이동
    public bool NextStage()
    {
        Debug.Log($"NextStage 호출 - stageIndex: {stageIndex}, stages.Length: {stages.Length}");

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