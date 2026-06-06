using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Result : MonoBehaviour
{
    // 결과 타이틀 오브젝트 배열 (0: 패배, 1: 승리)
    public GameObject[] titles;

    public void Lose()
    {
        ShowDefeatInterstitial();
    }

    public void Win()
    {
        // 승리 타이틀 표시
        titles[0].SetActive(false);
        if (titles.Length > 1)
            titles[1].SetActive(true);
    }

    /// <summary>패배 연출 — Title Over만 표시하고 Victory는 숨깁니다.</summary>
    public void ShowDefeatInterstitial()
    {
        if (titles.Length > 1)
            titles[1].SetActive(false);

        titles[0].SetActive(true);
    }

    public void ResetTitles()
    {
        if (titles == null)
            return;

        for (int i = 0; i < titles.Length; i++)
        {
            if (titles[i] != null)
                titles[i].SetActive(false);
        }
    }
}
