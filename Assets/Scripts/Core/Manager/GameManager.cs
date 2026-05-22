using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // 다른 스크립트에서 공용으로 접근하는 싱글턴 인스턴스
    public static GameManager instance;
    [Header("# Game Control")]
    // 실제 게임 진행 여부(일시정지/종료 시 false)
    public bool isLive;
    // 플레이 시간 누적값
    public float gameTime;
    // 최대 플레이 시간(현재는 승리 조건에서 직접 사용되지는 않음)
    public float maxGameTime = 2 * 10f;
    [Header("# Player Info")]
    // 플레이어 현재 체력
    public float Health;
    // 플레이어 최대 체력
    public float maxHealth = 100;
    // 누적 처치 수
    public int Kill;
    // 누적 코인 수
    public int Coin;
    [Header("# Game Object")]
    // 오브젝트 풀 매니저
    public PoolManager pool;
    // 플레이어 참조
    public Player player;
    // 레벨업 UI
    public LevelUp uiLevelUp;
    // 결과 UI(승/패)
    public Result uiResult;
    // 무기 선택 UI (게임 시작, 룬 선택 전)
    public WeaponSelectUI uiWeaponSelect;
    // 룬 선택 UI
    public RuneSelectUI uiRuneSelect;

    void Awake()
    {
        instance = this;

        if (uiWeaponSelect == null)
            uiWeaponSelect = FindFirstObjectByType<WeaponSelectUI>(FindObjectsInactive.Include);
        if (uiRuneSelect == null)
            uiRuneSelect = FindFirstObjectByType<RuneSelectUI>(FindObjectsInactive.Include);
    }

public void GameStart()
{
    Health = maxHealth;
    uiLevelUp.Select(1);

    if (uiWeaponSelect != null)
        uiWeaponSelect.Show();
    else if (uiRuneSelect != null)
        uiRuneSelect.Show();
    else
        Resume();
}

    public void GameOver()
    {
        // 패배 연출은 코루틴으로 처리
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        // 추가 입력/로직 중단
        isLive = false;

        // 사망 애니메이션/연출 대기
        yield return new WaitForSeconds(0.5f);

        // 결과 UI 활성화 후 패배 상태 표시
        uiResult.gameObject.SetActive(true);
        uiResult.Lose();
        Stop();
    }

    public void GameVictory()
    {
        // 승리 연출은 코루틴으로 처리
        StartCoroutine(GameVictoryRoutine());
    }

    IEnumerator GameVictoryRoutine()
    {
        // 전투 로직 정지
        isLive = false;

        // 연출 대기 후 결과 표시
        yield return new WaitForSeconds(0.5f);

        uiResult.gameObject.SetActive(true);
        uiResult.Win();
        Stop();
    }
    
    public void GameRetry()
    {
        // 현재 씬 재로딩으로 게임 리셋
        SceneManager.LoadScene("ProtoType_LTG");
    }

    void Update()
    {
        // 정지 상태에서는 타이머/승리 검사 중단
        if (!isLive)
            return;
        gameTime += Time.deltaTime;

        // 처치 수 50 달성 시 즉시 승리
        if (Kill == 50)
            GameVictory();
    }

    // 처치 조건을 만족하면 레벨업 선택 UI 오픈
public void GetLevelUp()
{
    if (!isLive)
        return;

    // 10킬 단위로 레벨업 기회 제공 (0킬 제외)
    if (Kill % 10 == 0 && Kill > 0)
    {
        uiLevelUp.Show();
    }
}

    public void Stop()
    {
        // 게임 상태 정지 + 전역 시간 정지
        isLive = false;
        Time.timeScale = 0;
    }

    public void Resume()
    {
        // 게임 상태 재개 + 전역 시간 복구
        isLive = true;
        Time.timeScale = 1;
    }
}

