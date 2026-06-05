using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 악세사리 하나의 데이터 SO.
/// InventoryDisplayService가 읽는 필드명(displayName, grade, accessoryType, icon, description)과 호환.
/// 실제 스탯 적용은 List<StatModifier>로 처리.
/// </summary>
[CreateAssetMenu(fileName = "AccessoryData", menuName = "Item/AccessoryData")]
public class AccessoryData : ScriptableObject
{
    [Header("[ 기본 정보 ]")]
    [Tooltip("인벤토리 UI에 표시될 이름")] public string displayName;
    public AccessoryGrade grade;
    [Tooltip("악세사리 분류 (공격/방어/유틸 등)")] public string accessoryType;
    [TextArea] public string description;
    public Sprite icon;

    [Header("[ 스탯 수정값 ]")]
    [Tooltip("획득 즉시 PlayerStats에 영구 적용")]
    public List<StatModifier> modifiers = new List<StatModifier>();

    [Header("[ 특수 효과 ]")]
    [Tooltip("단순 스탯형이면 None으로 두면 됨")]
    public AccessoryEffectType effectType = AccessoryEffectType.None;

    // InventoryDisplayService 호환용 — grade를 string으로 반환
    public string GradeString => grade.ToString();
}
