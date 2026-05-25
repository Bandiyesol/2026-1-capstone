using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("# Game Control")]
    public bool isLive;
    public float gameTime;
    public float maxGameTime = 2 * 10f;

    [Header("# Player Info")]
    public float Health;
    public float maxHealth = 100;
    public int Kill;
    public int Coin;

    [Header("# Game Object")]
    public PoolManager pool;
    public Player player;
    public Result uiResult;
    public WeaponSelectUI uiWeaponSelect;
    public RuneSelectUI uiRuneSelect;

    [Header("# Main Menu (비우면 이름으로 탐색)")]
    public GameObject mainMenuRoot;
    public GameObject gameplayHud;

    void Awake()
    {
        instance = this;

        if (uiWeaponSelect == null)
            uiWeaponSelect = FindFirstObjectByType<WeaponSelectUI>(FindObjectsInactive.Include);
        if (uiRuneSelect == null)
            uiRuneSelect = FindFirstObjectByType<RuneSelectUI>(FindObjectsInactive.Include);
    }

    /// <summary>스토리/메뉴 표시 전 무기·룬 선택 UI를 숨깁니다.</summary>
    public void HidePreGameSelectPanels()
    {
        if (uiWeaponSelect != null)
            uiWeaponSelect.gameObject.SetActive(false);
        if (uiRuneSelect != null)
            uiRuneSelect.gameObject.SetActive(false);
    }

    /// <summary>메인 메뉴 시작 버튼 — 오프닝 스토리 후 게임 진입.</summary>
    public void BeginGameFromMenu()
    {
        HidePreGameSelectPanels();

        MainStoryUI story = FindFirstObjectByType<MainStoryUI>(FindObjectsInactive.Include);
        if (story != null)
        {
            story.ShowThenStartGame();
            return;
        }

        Debug.LogWarning(
            "[GameManager] MainStoryUI가 씬에 없어 스토리를 건너뜁니다. " +
            "Canvas에 MainStoryUI를 추가하고 Panel에 MainStoryPanel을 연결하세요.");
        GameStart();
    }

    public void GameStart()
    {
        GameSessionReset.ResetAll(this);
        Health = maxHealth;

        if (uiWeaponSelect != null)
            uiWeaponSelect.Show();
        else if (uiRuneSelect != null)
            uiRuneSelect.Show();
        else
            Resume();
    }

    public void GameOver()
    {
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        isLive = false;
        yield return new WaitForSeconds(0.5f);
        uiResult.gameObject.SetActive(true);
        uiResult.Lose();
        Stop();
    }

    public void GameVictory()
    {
        StartCoroutine(GameVictoryRoutine());
    }

    IEnumerator GameVictoryRoutine()
    {
        isLive = false;
        yield return new WaitForSeconds(0.5f);
        uiResult.gameObject.SetActive(true);
        uiResult.Win();
        Stop();
    }

    public void GameRetry()
    {
        SceneManager.LoadScene("ProtoType_LTG");
    }

    /// <summary>게임 시작 화면(GameStart)으로 돌아갑니다.</summary>
    public void ReturnToMainMenu()
    {
        GameSessionReset.ResetAll(this);

        ResolveMainMenuReferences();

        if (uiWeaponSelect != null)
            uiWeaponSelect.gameObject.SetActive(false);
        if (uiRuneSelect != null)
            uiRuneSelect.gameObject.SetActive(false);
        if (uiResult != null)
            uiResult.gameObject.SetActive(false);

        CloseOverlayPanels();

        HideGameplayHud();
        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(true);
    }

    void ResolveMainMenuReferences()
    {
        if (mainMenuRoot == null)
        {
            GameObject found = GameObject.Find("GameStart");
            if (found != null)
                mainMenuRoot = found;
        }

        if (gameplayHud == null)
        {
            GameObject found = GameObject.Find("HUD");
            if (found != null)
                gameplayHud = found;
        }
    }

    static void CloseOverlayPanels()
    {
        StatusUI status = FindFirstObjectByType<StatusUI>(FindObjectsInactive.Include);
        if (status != null)
            status.Close();

        InventoryUI inventory = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if (inventory != null)
            inventory.Close();

        SettingsUI settings = FindFirstObjectByType<SettingsUI>(FindObjectsInactive.Include);
        if (settings != null)
            settings.Close();
    }

    void Update()
    {
        if (!isLive)
            return;

        gameTime += Time.deltaTime;
    }

    public void Stop()
    {
        isLive = false;
        Time.timeScale = 0;
    }

    /// <summary>설정/인벤토리 등 UI만 멈출 때 — timeScale 은 1 유지 (드롭다운·UI 애니메이션 동작).</summary>
    public void PauseOverlay()
    {
        isLive = false;
    }

    public void ResumeOverlay()
    {
        isLive = true;
    }

    public void ShowGameplayHud()
    {
        ResolveMainMenuReferences();

        if (gameplayHud != null)
            gameplayHud.SetActive(true);
    }

    public void HideGameplayHud()
    {
        ResolveMainMenuReferences();

        if (gameplayHud != null)
            gameplayHud.SetActive(false);
    }

    public void Resume()
    {
        isLive = true;
        Time.timeScale = 1;
        ShowGameplayHud();

        WaveManager wave = FindFirstObjectByType<WaveManager>();
        if (wave != null)
            wave.Begin();
    }

    public void AddCoin(int amount)
    {
        if (amount <= 0)
            return;

        Coin += amount;
    }
}
