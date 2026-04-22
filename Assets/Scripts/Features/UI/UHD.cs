using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UHD : MonoBehaviour
{
    // 이 UI가 어떤 정보를 표시하는지 구분
    public enum InfoType { Coin, Kill, Time, Health }
    public InfoType type;

    // 텍스트형 UI일 때 사용
    Text myText;
    // 슬라이더형 UI(체력바)일 때 사용
    Slider mySlider;

    void Awake()
    {
        // 한 컴포넌트에서 텍스트/슬라이더 둘 다 대응하기 위해 캐싱
        myText = GetComponent<Text>();
        mySlider = GetComponent<Slider>();
    }

    void LateUpdate()
    {
        // 타입별로 HUD 값 갱신
        switch (type) {
            case InfoType.Coin:
                // 코인 수 표시
                myText.text = string.Format("{0:f0}",GameManager.instance.Coin);
                break;
            case InfoType.Kill:
                // 킬 카운트/목표 표시
                myText.text = string.Format("{0:f0} / 50",GameManager.instance.Kill);
                break;
            case InfoType.Time:
                // 누적 시간을 mm:ss 형식으로 변환
                float playTime = GameManager.instance.gameTime;
                int min = Mathf.FloorToInt(playTime / 60f);
                int sec = Mathf.FloorToInt(playTime % 60f);
                myText.text = string.Format("{0:D2} : {1:D2}", min, sec);
                break;
            case InfoType.Health:
                // 현재 체력 비율을 슬라이더로 표시
                float curHealth = GameManager.instance.Health;
                float maxHealth = GameManager.instance.maxHealth;
                mySlider.value = curHealth / maxHealth;
                break;
        }
    }
}



