using UnityEngine;

/// <summary>
/// 게임의 전체적인 스테이지 전개, 구역별 맵 오브젝트의 활성/비활성 스왑,
/// 그리고 스테이지 전환 시 플레이어 위치 초기화 등을 총괄하는 제어 매니저 클래스입니다.
/// </summary>
public class StageManager : MonoBehaviour
{
    // --- 싱글톤 인스턴스 ---
    public static StageManager instance; // 외부 어디서든 StageManager에 접근할 수 있도록 하는 글로벌 static 참조

    [Header("웨이브 매니저")]
    public WaveManager waveManager; // 현재 스테이지의 적 스폰 및 웨이브 흐름을 제어하는 WaveManager 참조

    [Header("현재 스테이지")]
    public int stageIndex; // 현재 진행 중인 스테이지의 배열 인덱스 (0부터 시작)

    [Header("스테이지 오브젝트")]
    public GameObject[] stages; // 각 스테이지에 해당하는 맵/환경 게임 오브젝트들을 담은 배열

    [Header("스테이지 데이터")]
    [Tooltip("모든 스테이지 데이터를 넣는 데이터")]
    public StageData[] stageDatas; // 각 스테이지별 웨이브 구성 및 잡몹/보스 정보가 들어있는 ScriptableObject 데이터 배열

    [Header("엔딩")]
    [Tooltip("이 스테이지 번호 클리어 시 게임 종료 (BossStageConfigurationEditor 자동 설정)")]
    public int endingAfterStageNumber; // 최종 엔딩을 트리거할 조건이 되는 스테이지 번호 마커

    /// <summary>
    /// 내부 인덱스(0-based)를 유저 인터페이스(UI)나 시각 표기용 숫자(1-based)로 변환하여 반환하는 프로퍼티
    /// </summary>
    public int CurrentStage => stageIndex + 1;

    /// <summary>
    /// 등록된 맵 오브젝트 또는 데이터 개수를 기반으로 전체 스테이지의 총합 수량을 안전하게 계산하여 반환하는 프로퍼티
    /// </summary>
    public int TotalStages
    {
        get
        {
            // 1. 맵 오브젝트 배열이 유효하다면 해당 배열의 길이를 우선적으로 반환
            if (stages != null && stages.Length > 0)
                return stages.Length;

            // 2. 맵 오브젝트가 없고 데이터 테이블만 존재한다면 데이터 개수를 반환
            if (stageDatas != null && stageDatas.Length > 0)
                return stageDatas.Length;

            // 3. 둘 다 비어있는 극단적인 예외 상황 시 시스템 에러 방지를 위해 기본값 1 반환
            return 1;
        }
    }

    void Awake()
    {
        // 씬 내에 유일한 스테이지 매니저 인스턴스를 싱글톤 변수에 정적 주입
        instance = this;
    }

    void Start()
    {
        // 게임 시작 시 최초 맵(인덱스 0)만 활성화하고, 나머지 대기 상태의 모든 스테이지 맵은 일괄 비활성화
        for (int i = 0; i < stages.Length; i++)
            stages[i].SetActive(i == 0);
    }

    /// <summary>
    /// 전체 게임을 리스타트하거나 메인 메뉴에서 재진입 시 스테이지를 처음(0번) 상태로 클린 리셋하는 메서드
    /// </summary>
    public void ResetToFirstStage()
    {
        stageIndex = 0; // 스테이지 인덱스를 처음으로 복구
        UpdateStage();  // 맵 활성화 상태를 인덱스에 맞춰 동기화 갱신

        // 싱글톤 GameManager를 거쳐 플레이어 객체의 존재 여부를 검증한 후 월드 중앙 좌표(0, 0, 0)로 강제 이동
        if (GameManager.instance?.player != null)
            GameManager.instance.player.transform.position = Vector3.zero;
    }

    /// <summary>
    /// 현재 설정된 stageIndex 값에 매칭되는 단 하나의 맵 오브젝트만 켜고 나머지는 끄는 동기화 메서드
    /// </summary>
    void UpdateStage()
    {
        for (int i = 0; i < stages.Length; i++)
        {
            // 루프 인덱스(i)가 현재 보스가 진행 중인 stageIndex와 일치하는지 여부 판정
            bool shouldBeActive = (i == stageIndex);

            // [최적화 연산 분기] 변경하고자 하는 목표 상태(shouldBeActive)가 이미 현재 상태(activeSelf)와 같다면, 
            // 유니티 엔진 내부 오버헤드와 가비지가 발생하는 SetActive() 호출 자체를 과감히 스킵하여 성능 낭비를 차단
            if (stages[i].activeSelf != shouldBeActive)
                stages[i].SetActive(shouldBeActive);
        }
    }

    /// <summary>
    /// 현재 구역의 모든 웨이브를 클리어했을 때 호출되어 다음 단계의 맵으로 전환을 처리하는 핵심 메서드
    /// </summary>
    /// <returns>다음 스테이지 이동에 성공하면 true, 마지막 스테이지여서 게임 승리로 끝나면 false를 반환</returns>
    public bool NextStage()
    {
        // 런타임 추적 및 디버깅을 위해 현재 인덱스 상태를 콘솔에 기록
        Debug.Log($"NextStage 호출 - stageIndex: {stageIndex}, stages.Length: {stages.Length}");

        // [최종 승리 조건 검사] 현재 스테이지가 보유한 전체 맵 리스트의 마지막 인덱스에 도달했는가?
        if (stageIndex >= stages.Length - 1)
        {
            // 마지막 구역까지 완벽히 클리어했으므로 GameManager를 통해 전역 게임 빅토리(승리 엔딩) 시퀀스 발동
            GameManager.instance.GameVictory();
            return false; // 더 이상 전진할 스테이지가 없으므로 실패(false) 리턴
        }

        // 1. 기존에 머무르던 현재 구역의 맵 오브젝트를 비활성화 처리
        stages[stageIndex].SetActive(false);

        // 2. 스테이지 관리 인덱스를 한 칸 전진
        stageIndex++;

        // 3. 새롭게 도달한 다음 구역의 맵 오브젝트를 필드에 활성화 처리
        stages[stageIndex].SetActive(true);

        // 4. 새로운 구역으로의 원활한 진입을 위해 플레이어 캐릭터의 좌표를 맵의 중앙 원점(0, 0, 0)으로 재정렬
        if (GameManager.instance?.player != null)
            GameManager.instance.player.transform.position = Vector3.zero;

        return true; // 성공적으로 스테이지 전환이 완료되었으므로 true 리턴
    }
}