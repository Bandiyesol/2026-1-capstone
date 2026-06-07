using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 상자를 열었을 때 무기·악세사리·성물 후보 3개를 뽑는 서비스.
/// DroppedChest에서 Roll(ChestGrade)를 호출하면 RewardCandidate 리스트를 반환한다.
/// </summary>
public class RewardRollService : MonoBehaviour
{
    public static RewardRollService instance;

    // ── 아이템 풀 ─────────────────────────────────────────
    [Header("[ 아이템 풀 ]")]
    [Tooltip("AccessoryData SO 전부 등록")]
    public List<AccessoryData> allAccessories = new List<AccessoryData>();

    [Tooltip("성물 데이터 전부 등록 (등급 없음, 고정)")]
    public List<RelicData> allRelics = new List<RelicData>();

    // ── 상자 등급별 아이템 종류 가중치 ───────────────────────
    [Header("[ 상자 등급별 종류 가중치 (무기 / 악세사리 / 성물) ]")]
    public ChestRewardWeight normalChestWeight   = ChestRewardWeight.Normal;
    public ChestRewardWeight rareChestWeight     = ChestRewardWeight.Rare;
    public ChestRewardWeight uniqueChestWeight   = ChestRewardWeight.Unique;
    public ChestRewardWeight legendaryChestWeight = ChestRewardWeight.Legendary;

    // ── 상자 등급별 아이템 등급 가중치 ───────────────────────
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

    /// <summary>Inspector 미등록 시 Resources 카탈로그에서 악세·성물 풀을 채웁니다.</summary>
    public void EnsureAccessoryPool()
    {
        if (allAccessories != null && allAccessories.Count > 0)
            return;

        RewardCatalogSettings catalog = RewardCatalogSettings.Load();
        if (catalog != null && catalog.allAccessories != null && catalog.allAccessories.Count > 0)
        {
            allAccessories = new List<AccessoryData>(catalog.allAccessories);
            allRelics = new List<RelicData>(catalog.allRelics);
            Debug.Log($"[RewardRollService] 카탈로그 로드 — 악세사리 {allAccessories.Count}개, 성물 {allRelics.Count}개");
            return;
        }

#if UNITY_EDITOR
        if (TryLoadEditorPools())
            return;
#endif

        Debug.LogWarning(
            "[RewardRollService] 악세사리 풀이 비어 있습니다. " +
            "Unity에서 Tools → Rebuild Reward Catalog를 실행하세요.");
    }

#if UNITY_EDITOR
    bool TryLoadEditorPools()
    {
        allAccessories = LoadEditorAssets<AccessoryData>("Assets/Data/Accessory");
        if (allAccessories.Count == 0)
            return false;

        allRelics = LoadEditorAssets<RelicData>("Assets/Data/Relic");
        Debug.Log($"[RewardRollService] 에디터 폴더 스캔 — 악세사리 {allAccessories.Count}개, 성물 {allRelics.Count}개");
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

    // ───────────────────────────────────────────────────────
    //  외부 진입점 — DroppedChest에서 호출
    // ───────────────────────────────────────────────────────

    /// <summary>
    /// 상자 등급을 받아서 후보 3개를 뽑아 반환한다.
    /// </summary>
    public List<RewardCandidate> Roll(ChestGrade chestGrade, int count = 3)
    {
        ChestRewardWeight typeWeight  = GetTypeWeight(chestGrade);
        ItemGradeWeight   gradeWeight = GetItemGradeWeight(chestGrade);

        List<RewardCandidate> result   = new List<RewardCandidate>();
        List<AccessoryData>   accPool  = new List<AccessoryData>(allAccessories);
        List<RelicData>       relicPool = new List<RelicData>(allRelics);

        // 무기 풀은 WeaponRewardService에서 가져옴
        List<string> weaponPool = WeaponRewardService.GetAllWeaponIds();

        for (int i = 0; i < count; i++)
        {
            RewardType type = RollRewardType(typeWeight, chestGrade);

            RewardCandidate candidate = type switch
            {
                RewardType.Weapon    => RollWeapon(weaponPool, gradeWeight),
                RewardType.Accessory => RollAccessory(accPool, gradeWeight),
                RewardType.Relic     => RollRelic(relicPool),
                _                    => null
            };

            if (candidate == null) continue;

            result.Add(candidate);

            // 중복 방지
            if (type == RewardType.Accessory && candidate.accessory != null)
                accPool.Remove(candidate.accessory);
            if (type == RewardType.Relic && candidate.relic != null)
                relicPool.Remove(candidate.relic);
        }

        return result;
    }

    // ── 아이템 종류 룰렛 ──────────────────────────────────
    RewardType RollRewardType(ChestRewardWeight w, ChestGrade grade)
    {
        // 성물은 Unique 이상 상자에서만 등장
        float relicWeight = (grade >= ChestGrade.Unique) ? w.relic : 0f;
        float total = w.weapon + w.accessory + relicWeight;
        float roll  = UnityEngine.Random.Range(0f, total);

        if (roll < w.weapon)                   return RewardType.Weapon;
        if (roll < w.weapon + w.accessory)     return RewardType.Accessory;
        return RewardType.Relic;
    }

    // ── 무기 뽑기 ────────────────────────────────────────
    RewardCandidate RollWeapon(List<string> pool, ItemGradeWeight gradeWeight)
    {
        if (pool == null || pool.Count == 0) return null;

        // 무기 등급은 WeaponInfo.grade 기반으로 필터링
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

    // ── 악세사리 뽑기 ─────────────────────────────────────
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

    // ── 성물 뽑기 ────────────────────────────────────────
    RewardCandidate RollRelic(List<RelicData> pool)
    {
        if (pool == null || pool.Count == 0) return null;

        RelicData picked = pool[UnityEngine.Random.Range(0, pool.Count)];

        return new RewardCandidate
        {
            type  = RewardType.Relic,
            relic = picked,
        };
    }

    // ── 아이템 등급 룰렛 ──────────────────────────────────
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

// ── 보상 종류 ─────────────────────────────────────────────
public enum RewardType
{
    Weapon,
    Accessory,
    Relic,
}

// ── 보상 후보 단일 데이터 ──────────────────────────────────
public class RewardCandidate
{
    public RewardType    type;
    public string        weaponId;   // RewardType.Weapon일 때
    public AccessoryData accessory;  // RewardType.Accessory일 때
    public RelicData     relic;      // RewardType.Relic일 때
}

// ── 상자 등급별 아이템 종류 가중치 ────────────────────────
[Serializable]
public struct ChestRewardWeight
{
    public float weapon;
    public float accessory;
    public float relic;

    public static ChestRewardWeight Normal    => new ChestRewardWeight { weapon = 50f, accessory = 50f, relic = 0f  };
    public static ChestRewardWeight Rare      => new ChestRewardWeight { weapon = 45f, accessory = 45f, relic = 10f };
    public static ChestRewardWeight Unique    => new ChestRewardWeight { weapon = 40f, accessory = 40f, relic = 20f };
    public static ChestRewardWeight Legendary => new ChestRewardWeight { weapon = 35f, accessory = 35f, relic = 30f };
}

// ── 상자 등급별 아이템 등급 가중치 ────────────────────────
[Serializable]
public struct ItemGradeWeight
{
    public float common;
    public float rare;
    public float unique;
    public float legendary;

    // 일반 상자 — Common 위주
    public static ItemGradeWeight ForNormalChest => new ItemGradeWeight
        { common = 65f, rare = 25f, unique = 8f,  legendary = 2f  };

    // 희귀 상자
    public static ItemGradeWeight ForRareChest => new ItemGradeWeight
        { common = 30f, rare = 45f, unique = 20f, legendary = 5f  };

    // 유니크 상자
    public static ItemGradeWeight ForUniqueChest => new ItemGradeWeight
        { common = 10f, rare = 30f, unique = 45f, legendary = 15f };

    // 전설 상자
    public static ItemGradeWeight ForLegendaryChest => new ItemGradeWeight
        { common = 0f,  rare = 15f, unique = 35f, legendary = 50f };
}
