using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 상자를 열었을 때 무기·악세사리 후보 3개를 뽑는 서비스.
/// DroppedChest에서 Roll(ChestGrade)를 호출하면 RewardCandidate 리스트를 반환한다.
/// </summary>
public class RewardRollService : MonoBehaviour
{
    public static RewardRollService instance;

    [Header("[ 아이템 풀 ]")]
    [Tooltip("AccessoryData SO 전부 등록")]
    public List<AccessoryData> allAccessories = new List<AccessoryData>();

    [Header("[ 상자 등급별 종류 가중치 (무기 / 악세사리) ]")]
    public ChestRewardWeight normalChestWeight   = ChestRewardWeight.Normal;
    public ChestRewardWeight rareChestWeight     = ChestRewardWeight.Rare;
    public ChestRewardWeight uniqueChestWeight   = ChestRewardWeight.Unique;
    public ChestRewardWeight legendaryChestWeight = ChestRewardWeight.Legendary;

    [Header("[ 상자 등급별 아이템 등급 가중치 ]")]
    public ItemGradeWeight normalItemGrade    = ItemGradeWeight.ForNormalChest;
    public ItemGradeWeight rareItemGrade      = ItemGradeWeight.ForRareChest;
    public ItemGradeWeight uniqueItemGrade    = ItemGradeWeight.ForUniqueChest;
    public ItemGradeWeight legendaryItemGrade = ItemGradeWeight.ForLegendaryChest;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        EnsureAccessoryPool();
    }

    /// <summary>Inspector 미등록 시 Resources 카탈로그에서 악세사리 풀을 채웁니다.</summary>
    public void EnsureAccessoryPool()
    {
        if (allAccessories != null && allAccessories.Count > 0)
            return;

        RewardCatalogSettings catalog = RewardCatalogSettings.Load();
        if (catalog != null && catalog.allAccessories != null && catalog.allAccessories.Count > 0)
        {
            allAccessories = new List<AccessoryData>(catalog.allAccessories);
            Debug.Log($"[RewardRollService] 카탈로그 로드 — 악세사리 {allAccessories.Count}개");
            return;
        }

#if UNITY_EDITOR
        if (TryLoadEditorPool())
            return;
#endif

        Debug.LogWarning(
            "[RewardRollService] 악세사리 풀이 비어 있습니다. " +
            "Unity에서 Tools → Rebuild Reward Catalog를 실행하세요.");
    }

#if UNITY_EDITOR
    bool TryLoadEditorPool()
    {
        allAccessories = LoadEditorAssets<AccessoryData>("Assets/Data/Accessory");
        if (allAccessories.Count == 0)
            return false;

        Debug.Log($"[RewardRollService] 에디터 폴더 스캔 — 악세사리 {allAccessories.Count}개");
        return true;
    }

    static List<T> LoadEditorAssets<T>(string folder) where T : UnityEngine.Object
    {
        var result = new List<T>();
        if (!AssetDatabase.IsValidFolder(folder))
            return result;

        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                result.Add(asset);
        }

        result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return result;
    }
#endif

    public List<RewardCandidate> Roll(ChestGrade chestGrade, int count = 3)
    {
        ChestRewardWeight typeWeight  = GetTypeWeight(chestGrade);
        ItemGradeWeight   gradeWeight = GetItemGradeWeight(chestGrade);

        List<RewardCandidate> result  = new List<RewardCandidate>(count);
        List<AccessoryData> accPool   = new List<AccessoryData>(allAccessories);
        List<string> weaponPool = new List<string>(WeaponRewardService.GetAllWeaponIds());

        int attempts = 0;
        const int maxAttempts = 48;

        while (result.Count < count && attempts < maxAttempts)
        {
            attempts++;

            RewardCandidate candidate = RollCandidate(
                typeWeight,
                gradeWeight,
                weaponPool,
                accPool);

            if (candidate == null)
                continue;

            result.Add(candidate);
            ConsumeCandidateFromPools(candidate, weaponPool, accPool);
        }

        while (result.Count < count)
        {
            RewardCandidate fallback = RollFallbackCandidate(gradeWeight, weaponPool, accPool);
            if (fallback == null)
                break;

            result.Add(fallback);
            ConsumeCandidateFromPools(fallback, weaponPool, accPool);
        }

        return result;
    }

    RewardCandidate RollCandidate(
        ChestRewardWeight typeWeight,
        ItemGradeWeight gradeWeight,
        List<string> weaponPool,
        List<AccessoryData> accPool)
    {
        RewardType type = RollRewardType(typeWeight);

        RewardCandidate candidate = type switch
        {
            RewardType.Weapon    => RollWeapon(weaponPool, gradeWeight),
            RewardType.Accessory => RollAccessory(accPool, gradeWeight),
            _                    => null
        };

        if (candidate != null)
            return candidate;

        return RollWeapon(weaponPool, gradeWeight)
            ?? RollAccessory(accPool, gradeWeight);
    }

    RewardCandidate RollFallbackCandidate(
        ItemGradeWeight gradeWeight,
        List<string> weaponPool,
        List<AccessoryData> accPool)
    {
        RewardCandidate candidate = RollWeapon(weaponPool, gradeWeight)
            ?? RollAccessory(accPool, gradeWeight);

        if (candidate != null)
            return candidate;

        List<string> allWeapons = WeaponRewardService.GetAllWeaponIds();
        if (allWeapons != null && allWeapons.Count > 0)
        {
            return new RewardCandidate
            {
                type = RewardType.Weapon,
                weaponId = allWeapons[UnityEngine.Random.Range(0, allWeapons.Count)],
            };
        }

        if (allAccessories != null && allAccessories.Count > 0)
        {
            return new RewardCandidate
            {
                type = RewardType.Accessory,
                accessory = allAccessories[UnityEngine.Random.Range(0, allAccessories.Count)],
            };
        }

        return null;
    }

    static void ConsumeCandidateFromPools(
        RewardCandidate candidate,
        List<string> weaponPool,
        List<AccessoryData> accPool)
    {
        if (candidate == null)
            return;

        switch (candidate.type)
        {
            case RewardType.Weapon:
                if (!string.IsNullOrEmpty(candidate.weaponId))
                    weaponPool.Remove(candidate.weaponId);
                break;
            case RewardType.Accessory:
                if (candidate.accessory != null)
                    accPool.Remove(candidate.accessory);
                break;
        }
    }

    RewardType RollRewardType(ChestRewardWeight w)
    {
        float total = w.weapon + w.accessory;
        if (total <= 0f)
            return RewardType.Weapon;

        float roll = UnityEngine.Random.Range(0f, total);
        return roll < w.weapon ? RewardType.Weapon : RewardType.Accessory;
    }

    RewardCandidate RollWeapon(List<string> pool, ItemGradeWeight gradeWeight)
    {
        if (pool == null || pool.Count == 0) return null;

        string targetGrade = RollItemGradeString(gradeWeight);
        List<string> filtered = pool.FindAll(id =>
            WeaponRewardService.GetWeaponGrade(id) == targetGrade);

        if (filtered.Count == 0) filtered = pool;
        if (filtered.Count == 0) return null;

        string weaponId = filtered[UnityEngine.Random.Range(0, filtered.Count)];
        pool.Remove(weaponId);

        return new RewardCandidate
        {
            type     = RewardType.Weapon,
            weaponId = weaponId,
        };
    }

    RewardCandidate RollAccessory(List<AccessoryData> pool, ItemGradeWeight gradeWeight)
    {
        if (pool == null || pool.Count == 0) return null;

        AccessoryGrade targetGrade = RollAccessoryGrade(gradeWeight);
        List<AccessoryData> filtered = pool.FindAll(a => a.grade == targetGrade);

        if (filtered.Count == 0) filtered = pool;
        if (filtered.Count == 0) return null;

        AccessoryData picked = filtered[UnityEngine.Random.Range(0, filtered.Count)];

        return new RewardCandidate
        {
            type      = RewardType.Accessory,
            accessory = picked,
        };
    }

    AccessoryGrade RollAccessoryGrade(ItemGradeWeight w)
    {
        float total = w.common + w.rare + w.unique + w.legendary;
        float roll  = UnityEngine.Random.Range(0f, total);

        if (roll < w.common)                          return AccessoryGrade.Common;
        if (roll < w.common + w.rare)                 return AccessoryGrade.Rare;
        if (roll < w.common + w.rare + w.unique)      return AccessoryGrade.Unique;
        return AccessoryGrade.Legendary;
    }

    string RollItemGradeString(ItemGradeWeight w)
    {
        return RollAccessoryGrade(w) switch
        {
            AccessoryGrade.Common    => "Common",
            AccessoryGrade.Rare      => "Rare",
            AccessoryGrade.Unique    => "Unique",
            AccessoryGrade.Legendary => "Legendary",
            _                        => "Common"
        };
    }

    ChestRewardWeight GetTypeWeight(ChestGrade grade)
    {
        ChestDropSettings settings = ResolveSettings();
        if (settings != null)
            return settings.GetRewardTypeWeight(grade);

        return grade switch
        {
            ChestGrade.Rare => rareChestWeight,
            ChestGrade.Unique => uniqueChestWeight,
            ChestGrade.Legendary => legendaryChestWeight,
            _ => normalChestWeight
        };
    }

    ItemGradeWeight GetItemGradeWeight(ChestGrade grade)
    {
        ChestDropSettings settings = ResolveSettings();
        if (settings != null)
            return settings.GetItemGradeWeight(grade);

        return grade switch
        {
            ChestGrade.Rare => rareItemGrade,
            ChestGrade.Unique => uniqueItemGrade,
            ChestGrade.Legendary => legendaryItemGrade,
            _ => normalItemGrade
        };
    }

    static ChestDropSettings ResolveSettings()
    {
        if (ChestDropManager.Instance != null && ChestDropManager.Instance.Settings != null)
            return ChestDropManager.Instance.Settings;

        if (GameManager.instance != null && GameManager.instance.chestDropSettings != null)
            return GameManager.instance.chestDropSettings;

        return ChestDropSettings.Load();
    }
}

public enum RewardType
{
    Weapon,
    Accessory,
}

public class RewardCandidate
{
    public RewardType    type;
    public string        weaponId;
    public AccessoryData accessory;
}

[Serializable]
public struct ChestRewardWeight
{
    public float weapon;
    public float accessory;

    public static ChestRewardWeight Normal    => new ChestRewardWeight { weapon = 50f, accessory = 50f };
    public static ChestRewardWeight Rare      => new ChestRewardWeight { weapon = 45f, accessory = 55f };
    public static ChestRewardWeight Unique    => new ChestRewardWeight { weapon = 40f, accessory = 60f };
    public static ChestRewardWeight Legendary => new ChestRewardWeight { weapon = 35f, accessory = 65f };
}

[Serializable]
public struct ItemGradeWeight
{
    public float common;
    public float rare;
    public float unique;
    public float legendary;

    public static ItemGradeWeight ForNormalChest => new ItemGradeWeight
        { common = 65f, rare = 25f, unique = 8f,  legendary = 2f  };

    public static ItemGradeWeight ForRareChest => new ItemGradeWeight
        { common = 30f, rare = 45f, unique = 20f, legendary = 5f  };

    public static ItemGradeWeight ForUniqueChest => new ItemGradeWeight
        { common = 10f, rare = 30f, unique = 45f, legendary = 15f };

    public static ItemGradeWeight ForLegendaryChest => new ItemGradeWeight
        { common = 0f,  rare = 15f, unique = 35f, legendary = 50f };
}
