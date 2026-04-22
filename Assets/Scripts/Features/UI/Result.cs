using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Result : MonoBehaviour
{
    // 결과 타이틀 오브젝트 배열 (0: 패배, 1: 승리)
    public GameObject[] titles;

    public void Lose()
    {
        // 패배 타이틀 표시
        titles[0].SetActive(true);
    }

    public void Win()
    {
        // 승리 타이틀 표시
        titles[1].SetActive(true);
    }
    
}
