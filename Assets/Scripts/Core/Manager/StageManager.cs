using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("현재 스테이지 번호")]
    public int stageIndex;

    [Header("스테이지 오브젝트들")]
    public GameObject[] stages;

    [Header("스테이지별 웨이브 데이터")]
    public StageData[] stageDatas;

    [Header("클리어 / 엔딩 (테스트)")]
    [Tooltip("몇 번째 스테이지(1부터) 클리어 시 엔딩. 0이면 stages 배열의 마지막 스테이지")]
    [Min(0)]
    public int endingAfterStageNumber = 3;

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

    void Start()
    {
        UpdateStage();
    }

    public void ResetToFirstStage()
    {
        stageIndex = 0;
        UpdateStage();

        if (GameManager.instance != null && GameManager.instance.player != null)
            GameManager.instance.player.transform.position = Vector3.zero;
    }

    // 현재 스테이지만 켜기
    void UpdateStage()
    {
        for (int i = 0; i < stages.Length; i++)
        {
            stages[i].SetActive(i == stageIndex);
        }
    }

    // 다음 스테이지 이동
    public bool NextStage()
    {
        int clearingIndex = stageIndex;

        if (stageIndex >= GetFinalStageIndex())
        {
            if (GameRunSessionTracker.IsActive)
                GameRunSessionTracker.CommitStage(clearingIndex, stageCleared: true);

            GameManager.instance.GameVictory();
            return false;
        }

        if (GameRunSessionTracker.IsActive)
            GameRunSessionTracker.CommitStage(clearingIndex, stageCleared: true);

        // 현재 스테이지 끄기
        stages[stageIndex].SetActive(false);

        // 다음 스테이지로 이동
        stageIndex++;

        // 다음 스테이지 활성화
        stages[stageIndex].SetActive(true);

        // 플레이어 위치 초기화
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            GameManager.instance.player.transform.position = Vector3.zero;
        }

        if (GameManager.instance != null)
            GameManager.instance.RefreshBossBriefingForCurrentStage();

        if (GameRunSessionTracker.IsActive)
            GameRunSessionTracker.MarkStageStart(stageIndex);

        return true;
    }

    /// <summary>0-based. 이 인덱스 스테이지 클리어 시 GameVictory.</summary>
    public int GetFinalStageIndex()
    {
        int lastInArray = TotalStages - 1;
        if (lastInArray < 0)
            return 0;

        if (endingAfterStageNumber <= 0)
            return lastInArray;

        int configured = endingAfterStageNumber - 1;
        return Mathf.Clamp(configured, 0, lastInArray);
    }
}
