using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 등급 가중치를 바탕으로 악세사리 후보 3개를 뽑는 서비스.
/// RewardRollService(추후 구현)가 이 결과를 받아서 선택 UI에 전달한다.
/// </summary>
public class AccessoryRewardService : MonoBehaviour
{
    public static AccessoryRewardService instance;

    [Header("[ 전체 악세사리 풀 ]")]
    [Tooltip("Data/Accessory 폴더의 AccessoryData SO를 전부 등록")]
    public List<AccessoryData> allAccessories = new List<AccessoryData>();

    [Header("[ 등급 가중치 ]")]
    public DropGradeWeight normalWeight  = DropGradeWeight.Normal;
    public DropGradeWeight uniqueWeight  = DropGradeWeight.Unique;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    /// <summary>
    /// 후보를 count개 뽑아서 반환.
    /// isUnique = true면 유니크 몬스터 가중치 적용.
    /// </summary>
    public List<AccessoryData> Roll(bool isUnique, int count = 3)
    {
        DropGradeWeight weight = isUnique ? uniqueWeight : normalWeight;
        List<AccessoryData> result   = new List<AccessoryData>();
        List<AccessoryData> usedPool = new List<AccessoryData>(allAccessories);

        for (int i = 0; i < count; i++)
        {
            AccessoryGrade grade = RollGrade(weight);
            List<AccessoryData> pool = usedPool.FindAll(a => a.grade == grade);

            if (pool.Count == 0) pool = usedPool;
            if (pool.Count == 0) break;

            AccessoryData picked = pool[Random.Range(0, pool.Count)];
            result.Add(picked);
            usedPool.Remove(picked); // 중복 방지
        }

        return result;
    }

    AccessoryGrade RollGrade(DropGradeWeight w)
    {
        float total = w.common + w.rare + w.unique + w.legendary;
        float roll  = Random.Range(0f, total);

        if (roll < w.common)                          return AccessoryGrade.Common;
        if (roll < w.common + w.rare)                 return AccessoryGrade.Rare;
        if (roll < w.common + w.rare + w.unique)      return AccessoryGrade.Unique;
        return AccessoryGrade.Legendary;
    }
}
