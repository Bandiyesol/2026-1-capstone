using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("현재 스테이지 번호")]
    public int stageIndex;

    [Header("스테이지 오브젝트들")]
    public GameObject[] stages;

    [Header("스테이지별 웨이브 데이터")]
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
        // 마지막 스테이지라면 현재 스테이지 유지한 채 클리어
        if (stageIndex >= stages.Length - 1)
        {
            GameManager.instance.GameVictory();
            return false;
        }

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

        return true;
    }
}
