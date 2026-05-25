using UnityEngine;

[CreateAssetMenu(fileName = "CoinDropSettings", menuName = "Game/Coin Drop Settings")]
public class CoinDropSettings : ScriptableObject
{
    [Header("드랍 확률")]
    [Range(0f, 1f)]
    [Tooltip("일반 몬스터 처치 시 코인이 나올 확률")]
    public float enemyDropChance = 0.35f;

    [Range(0f, 1f)]
    [Tooltip("보스 처치 시 코인이 나올 확률")]
    public float bossDropChance = 0.6f;

    [Header("종류별 드랍 가중치 (드랍 성공 시)")]
    [Tooltip("금 코인 가중치 — 낮을수록 희귀")]
    public float goldWeight = 5f;

    public float silverWeight = 20f;

    [Tooltip("동 코인 가중치 — 높을수록 자주 나옴")]
    public float bronzeWeight = 75f;

    [Header("코인 가치")]
    public int goldValue = 10;
    public int silverValue = 5;
    public int bronzeValue = 1;

    public int GetValue(CoinType type)
    {
        return type switch
        {
            CoinType.Gold => goldValue,
            CoinType.Silver => silverValue,
            _ => bronzeValue
        };
    }

    public float GetDropChance(bool isBoss) => isBoss ? bossDropChance : enemyDropChance;

    public CoinType RollCoinType()
    {
        float total = goldWeight + silverWeight + bronzeWeight;
        if (total <= 0f)
            return CoinType.Bronze;

        float roll = Random.Range(0f, total);

        if (roll < goldWeight)
            return CoinType.Gold;

        roll -= goldWeight;
        if (roll < silverWeight)
            return CoinType.Silver;

        return CoinType.Bronze;
    }
}
