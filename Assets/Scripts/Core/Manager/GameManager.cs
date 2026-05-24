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

    public void Resume()
    {
        isLive = true;
        Time.timeScale = 1;

        WaveManager wave = FindFirstObjectByType<WaveManager>();
        if (wave != null)
            wave.Begin();
    }
}
