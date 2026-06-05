using UnityEngine;

[CreateAssetMenu(fileName = "ChestDropSettings", menuName = "Game/Chest Drop Settings")]
public class ChestDropSettings : ScriptableObject
{
    [Header("상자 드랍 확률 (몬스터 처치 시)")]
    [Range(0f, 1f)]
    public float enemyDropChance = 1;

    [Range(0f, 1f)]
    public float bossDropChance = 0.25f;

    [Header("등급별 가중치 (드랍 성공 시, 높을수록 자주)")]
    public float normalWeight = 70f;
    public float rareWeight = 20f;
    public float uniqueWeight = 8f;
    public float legendaryWeight = 2f;

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
        float total = normalWeight + rareWeight + uniqueWeight + legendaryWeight;
        if (total <= 0f)
            return ChestGrade.Normal;

        float roll = Random.Range(0f, total);

        if (roll < normalWeight) return ChestGrade.Normal;
        roll -= normalWeight;
        if (roll < rareWeight) return ChestGrade.Rare;
        roll -= rareWeight;
        if (roll < uniqueWeight) return ChestGrade.Unique;

        return ChestGrade.Legendary;
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
