using UnityEngine;

[CreateAssetMenu(fileName = "ChestDropSettings", menuName = "Game/Chest Drop Settings")]
public class ChestDropSettings : ScriptableObject
{
    [Header("상자 드랍 확률 (몬스터 처치 시)")]
    [Range(0f, 1f)]
    [Tooltip("일반 몬스터 처치 시 상자가 나올 확률 (0~1). 예: 0.08 = 8%")]
    public float enemyDropChance = 0.08f;

    [Range(0f, 1f)]
    [Tooltip("보스/유니크 몬스터 처치 시 상자가 나올 확률 (0~1). 예: 0.25 = 25%")]
    public float bossDropChance = 0.25f;

    [Header("드롭되는 상자 등급 가중치")]
    [Min(0f)] public float normalWeight = 70f;
    [Min(0f)] public float rareWeight = 20f;
    [Min(0f)] public float uniqueWeight = 8f;
    [Min(0f)] public float legendaryWeight = 2f;

    [Header("상자 등급별 보상 종류 가중치 (무기 / 악세서리 / 성물)")]
    public ChestRewardWeight normalChestReward = ChestRewardWeight.Normal;
    public ChestRewardWeight rareChestReward = ChestRewardWeight.Rare;
    public ChestRewardWeight uniqueChestReward = ChestRewardWeight.Unique;
    public ChestRewardWeight legendaryChestReward = ChestRewardWeight.Legendary;

    [Header("상자 등급별 보상 아이템 등급 가중치")]
    public ItemGradeWeight normalItemGrade = ItemGradeWeight.ForNormalChest;
    public ItemGradeWeight rareItemGrade = ItemGradeWeight.ForRareChest;
    public ItemGradeWeight uniqueItemGrade = ItemGradeWeight.ForUniqueChest;
    public ItemGradeWeight legendaryItemGrade = ItemGradeWeight.ForLegendaryChest;

    [Header("등급별 무기 ID 풀 (WeaponInfo.json id)")]
    public string[] normalWeaponIds = { "SWORD_001", "BOW_001", "ORB_001" };
    public string[] rareWeaponIds = { "SWORD_001", "BOW_001" };
    public string[] uniqueWeaponIds = { "BOW_001" };
    public string[] legendaryWeaponIds = { "ORB_001" };

    [Header("열림 애니 대기 (Animation Event 없을 때 백업)")]
    [Tooltip("Animator Open 클립 길이와 맞추세요")]
    public float openAnimationFallbackSeconds = 0.6f;

    public float GetDropChance(bool isBoss) => isBoss ? bossDropChance : enemyDropChance;

    public float GetTotalChestGradeWeight() =>
        normalWeight + rareWeight + uniqueWeight + legendaryWeight;

    public ChestGrade RollGrade()
    {
        return GradeWeightRollUtility.Roll(
            normalWeight, rareWeight, uniqueWeight, legendaryWeight,
            ChestGrade.Normal, ChestGrade.Rare, ChestGrade.Unique, ChestGrade.Legendary);
    }

    public ChestRewardWeight GetRewardTypeWeight(ChestGrade grade) => grade switch
    {
        ChestGrade.Rare => rareChestReward,
        ChestGrade.Unique => uniqueChestReward,
        ChestGrade.Legendary => legendaryChestReward,
        _ => normalChestReward
    };

    public ItemGradeWeight GetItemGradeWeight(ChestGrade grade) => grade switch
    {
        ChestGrade.Rare => rareItemGrade,
        ChestGrade.Unique => uniqueItemGrade,
        ChestGrade.Legendary => legendaryItemGrade,
        _ => normalItemGrade
    };

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

    static ChestDropSettings cached;

    public static ChestDropSettings Load()
    {
        if (cached == null)
            cached = Resources.Load<ChestDropSettings>("Data/ChestDropSettings");

        return cached;
    }
}
