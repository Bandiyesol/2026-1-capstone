using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class LevelUp : MonoBehaviour
{
    // 레벨업 패널 표시/숨김 제어용 루트
    RectTransform rect;
    // 선택 가능한 아이템 카드 목록
    Item[] items;

    void Awake()
    {
        // UI 컴포넌트 캐싱
        rect = GetComponent<RectTransform>();
        items = GetComponentsInChildren<Item>(true);
    }

    public void Show()
    {
        // 다음 선택지 생성 후 패널 오픈 + 게임 일시정지
        Next();
        rect.localScale = Vector3.one;
        GameManager.instance.Stop();
    }

    public void Hide()
    {
        // 패널 닫고 게임 재개
        rect.localScale = Vector3.zero; 
        GameManager.instance.Resume();
    }

    public void Select(int index)
    {
        // 외부에서 특정 아이템을 강제로 선택할 때 사용
        // items[index].OnClick();
    }

    void Next()
    {
        // 모든 아이템 먼저 숨김 처리
        foreach (Item item in items)
        {
            item.gameObject.SetActive(false);
        }

        // 중복 없이 랜덤 3개 인덱스 뽑기
        int[] ran = new int[3];
        while (true)
        {
            ran[0] = Random.Range(0, items.Length);
            ran[1] = Random.Range(0, items.Length);
            ran[2] = Random.Range(0, items.Length);

            if (ran[0]!=ran[1] && ran[1]!=ran[2] && ran[0]!=ran[2])
                break;
        }

        for (int index=0; index < ran.Length; index++)
        {
            Item ranItem = items[ran[index]];

            // 만렙 아이템은 후보에서 제외
            if (ranItem.level == ranItem.data.damages.Length)
            {
                continue;
            }
            else
            {
                // 선택 가능한 카드만 표시
                ranItem.gameObject.SetActive(true);   
            }
        }
    }
}
