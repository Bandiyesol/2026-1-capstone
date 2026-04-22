using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptble Object/ItemData")]
public class ItemData : ScriptableObject
{
    // 아이템 무기 타입 분류
    public enum ItemType { Melee, Range }

    [Header("# Main Info")]
    // 분류 타입
    public ItemType itemType;
    // 무기/아이템 고유 ID
    public int itemId;
    // 아이템 이름
    public string itemName;
    [TextArea]
    // UI 설명 포맷 문자열
    public string itemDesc;
    // 아이템 아이콘 이미지
    public Sprite itemIcon;

    [Header("# Level Data")]
    // 기본 공격력
    public float baseDamage;
    // 기본 개수/관통 수치
    public int baseCount;
    // 레벨별 공격력 증가 배율 배열
    public float[] damages;
    // 레벨별 개수 증가 배열
    public int[] counts;

    [Header("# Weapon")]
    // 실제 생성되는 발사체 프리팹
    public GameObject projectile;
}
