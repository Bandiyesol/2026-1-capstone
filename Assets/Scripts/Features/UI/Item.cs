using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    // 이 UI 카드가 참조하는 아이템 데이터
    public ItemData data;
    // 현재 강화 레벨
    public int level;
    // 연결된 무기 인스턴스(없으면 첫 클릭 시 생성)
    // public Weapon weapon;

    // 카드 UI 구성 요소
    Image icon;
    Text textLevel;
    Text textName;
    Text textDesc;

    void Awake()
    {
        // 아이콘/텍스트 레퍼런스 캐싱 및 기본 표시값 세팅
        Image[] icons = GetComponentsInChildren<Image>();
        icon = icons[1];
        icon.sprite = data.itemIcon;

        Text[] texts = GetComponentsInChildren<Text>();
        textLevel = texts[0];
        textName = texts[1];
        textDesc = texts[2];
        textName.text = data.itemName;
    }

    void OnEnable()
    {
        // 카드가 표시될 때 현재 레벨/설명 문구 갱신
        textLevel.text = "Lv." + level;
        switch (data.itemType) {
            case ItemData.ItemType.Melee:
            case ItemData.ItemType.Range:
                // 포맷 문자열에 현재 레벨 기준 수치 삽입
                textDesc.text = string.Format(data.itemDesc, data.damages[level] * 100, data.counts[level]);
                break;
        }
    }

    public void OnClick()
    {
        // 첫 선택 시 무기 오브젝트를 동적으로 생성
        // if (weapon == null)
        // {
        //     GameObject newWeapon = new GameObject();
        //     weapon = newWeapon.AddComponent<Weapon>();
        //     weapon.Init(data);
        // }

        // 다음 레벨 적용 스탯 계산 시작값(기본치)
        float nextDamage = data.baseDamage;
        int nextCount = data.baseCount;

        // level 0은 기본값, level 1부터 배열 강화치 적용
        int statIndex = level - 1;
        if (statIndex >= 0)
        {
            if (data.damages != null && statIndex < data.damages.Length)
                nextDamage += data.baseDamage * data.damages[statIndex];
            if (data.counts != null && statIndex < data.counts.Length)
                nextCount += data.counts[statIndex];
        }

        // weapon.LevelUp(nextDamage, nextCount);

        // 강화 레벨 증가
        level++;

        // 배열 길이를 기준으로 최대 레벨 계산 후 버튼 잠금
        int maxLevel = 1 + Mathf.Max(
            data.damages != null ? data.damages.Length : 0,
            data.counts != null ? data.counts.Length : 0
        );
        if (level >= maxLevel) {
            GetComponent<Button>().interactable = false;
        }
    }
}
