using UnityEngine;

[CreateAssetMenu(fileName = "ChestDropSettings", menuName = "Game/Chest Drop Settings")]
public class ChestDropSettings : ScriptableObject
{
    [Header("상자 드랍 확률 (몬스터 처치 시)")]
    [Range(0f, 1f)]
    public float enemyDropChance = 0.08f;

    [Range(0f, 1f)]
    public float bossDropChance = 0.25f;

    [Header("등급별 가중치 (추후 확장용 — 현재는 일반 상자만)")]
    public float normalWeight = 100f;
    public float rareWeight = 0f;
    public float uniqueWeight = 0f;
    public float legendaryWeight = 0f;

    [Header("등급별 무기 ID 풀 (WeaponInfo.json id)")]
    public string[] normalWeaponIds = { "SWORD_001", "BOW_001", "ORB_001" };
    public string[] rareWeaponIds = { "SWORD_001", "BOW_001" };
    public string[] uniqueWeaponIds = { "BOW_001" };
    public string[] legendaryWeaponIds = { "ORB_001" };

    [Header("열림 애니 대기 (Animation Event 없을 때 백업)")]
    [Tooltip("Animator Open 클립 길이와 맞추세요")]
    public float openAnimationFallbackSeconds = 0.6f;

    public float GetDropChance(bool isBoss) => isBoss ? bossDropChance : enemyDropChance;

    public ChestGrade RollGrade()
    {
        // 현재 무기는 WeaponInfo.json 등급(Common)만 사용합니다.
        return ChestGrade.Normal;
    }

    public string[] GetWeaponPool(ChestGrade grade)
    {
        return grade switch
        {
            ChestGrade.Rare => rareWeaponIds,
            ChestGrade.Unique => uniqueWeaponIds,
            ChestGrade.Legendary => legendaryWeaponIds,
            _ => normalWeaponIds
        };
    }

    public static string GetGradeLabel(ChestGrade grade)
    {
        return grade switch
        {
            ChestGrade.Rare => "희귀",
            ChestGrade.Unique => "유니크",
            ChestGrade.Legendary => "전설",
            _ => "일반"
        };
    }
}
