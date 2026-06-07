using UnityEngine;

/// <summary>
/// 몬스터 사망 시 확률적으로 코인을 드랍합니다.
/// </summary>
public class CoinDropManager : MonoBehaviour
{
    public static CoinDropManager Instance { get; private set; }

    [SerializeField] CoinDropSettings settings;
    [SerializeField] PoolManager pool;
    [SerializeField] float scatterRadius = 0.35f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveSettings();
        if (pool == null && GameManager.instance != null)
            pool = GameManager.instance.pool;
    }

    void ResolveSettings()
    {
        if (settings != null)
            return;

        if (GameManager.instance != null && GameManager.instance.coinDropSettings != null)
            settings = GameManager.instance.coinDropSettings;

        if (settings == null)
            settings = Resources.Load<CoinDropSettings>("Data/CoinDropSettings");
    }

    /// <summary>일반 몬스터 사망 시 호출</summary>
    public void TryDropFromEnemy(Vector3 worldPosition)
    {
        TryDrop(worldPosition, isBoss: false);
    }

    /// <summary>보스 사망 시 호출</summary>
    public void TryDropFromBoss(Vector3 worldPosition)
    {
        TryDrop(worldPosition, isBoss: true);
    }

    void TryDrop(Vector3 worldPosition, bool isBoss)
    {
        if (settings == null || pool == null)
            return;

        if (Random.value > settings.GetDropChance(isBoss))
            return;

        CoinType type = settings.RollCoinType();
        SpawnCoin(type, worldPosition);
    }

    void SpawnCoin(CoinType type, Vector3 worldPosition)
    {
        GameObject coinObj = pool.GetCoin((int)type);
        if (coinObj == null)
            return;

        Vector2 offset = Random.insideUnitCircle * scatterRadius;
        coinObj.transform.position = worldPosition + new Vector3(offset.x, offset.y, 0f);

        DroppedCoin coin = coinObj.GetComponent<DroppedCoin>();
        if (coin != null)
            coin.Setup(type, settings);
    }
}
