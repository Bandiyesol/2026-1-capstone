using UnityEngine;

/// <summary>
/// 성물 데이터 SO.
/// 성물은 등급 개념 없이 고정 효과를 가진다.
/// Unique 이상 상자에서만 낮은 확률로 등장.
/// 실제 효과는 추후 RelicEffect에서 구현.
/// </summary>
[CreateAssetMenu(fileName = "RelicData", menuName = "Item/RelicData")]
public class RelicData : ScriptableObject
{
    [Header("[ 기본 정보 ]")]
    public string relicName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("[ 상점 ]")]
    public int price;
}
