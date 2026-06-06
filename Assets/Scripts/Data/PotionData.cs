using UnityEngine;

/// <summary>
/// 물약 하나의 데이터 SO.
/// 인스펙터에서 종류·이름·아이콘·설명·가격을 설정한다.
/// 실제 효과 실행은 PotionEffect.Use()가 담당.
/// </summary>
[CreateAssetMenu(fileName = "PotionData", menuName = "Item/PotionData")]
public class PotionData : ScriptableObject
{
    [Header("[ 기본 정보 ]")]
    public PotionType potionType;
    public string potionName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("[ 상점 ]")]
    [Tooltip("상점 구매 가격 (골드)")] public int price;
}
