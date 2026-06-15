using UnityEngine;
using UnityEngine.UI;

public class UHD : MonoBehaviour
{
    public enum InfoType { Coin, Stage, Time, Health, Kill }
    public InfoType type;

    Text myText;
    Slider mySlider;
    StageManager stageManager;
    int appliedStageIndex = -1;

    void Awake()
    {
        myText = GetComponent<Text>();
        mySlider = GetComponent<Slider>();
        stageManager = FindFirstObjectByType<StageManager>();
        if (type == InfoType.Health && mySlider != null)
        {
            mySlider.minValue = 0f;
            mySlider.maxValue = 1f;
            mySlider.wholeNumbers = false;
        }
        RefreshTextStyle(force: true);
    }

    void OnEnable()
    {
        if (type == InfoType.Health && PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged += RefreshHealthBar;
    }

    void OnDisable()
    {
        if (type == InfoType.Health && PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= RefreshHealthBar;
    }

    void LateUpdate()
    {
        RefreshTextStyle(force: false);

        if (GameManager.instance == null)
            return;

        switch (type)
        {
            case InfoType.Coin:
                if (myText != null)
                    myText.text = string.Format("{0:f0}", GameManager.instance.Coin);
                break;
            case InfoType.Stage:
                if (myText != null)
                {
                    if (stageManager != null)
                        myText.text = string.Format("{0} / {1}", stageManager.CurrentStage, stageManager.TotalStages);
                    else
                        myText.text = "1 / 1";
                }
                break;
            case InfoType.Time:
                if (myText != null)
                {
                    float playTime = GameManager.instance.gameTime;
                    int min = Mathf.FloorToInt(playTime / 60f);
                    int sec = Mathf.FloorToInt(playTime % 60f);
                    myText.text = string.Format("{0:D2} : {1:D2}", min, sec);
                }
                break;
            case InfoType.Health:
                RefreshHealthBar();
                break;
            case InfoType.Kill:
                if (myText != null)
                    myText.text = string.Format("{0}", GameManager.instance.Kill);
                break;
        }
    }

    void RefreshTextStyle(bool force)
    {
        if (myText == null)
            return;

        int stageIndex = stageManager != null ? stageManager.stageIndex : 0;
        if (!force && stageIndex == appliedStageIndex)
            return;

        appliedStageIndex = stageIndex;
        HudStatTextStyle.Apply(myText, stageIndex);
    }

    void RefreshHealthBar()
    {
        if (mySlider == null)
            return;

        float curHealth;
        float maxHp;
        if (PlayerStats.Instance != null)
        {
            curHealth = PlayerStats.Instance.CurrentHP;
            maxHp = PlayerStats.Instance.MaxHP;
        }
        else if (GameManager.instance != null)
        {
            curHealth = GameManager.instance.Health;
            maxHp = GameManager.instance.maxHealth;
        }
        else
        {
            return;
        }

        float ratio = maxHp > 0f ? Mathf.Clamp01(curHealth / maxHp) : 0f;
        mySlider.value = ratio;
    }
}
