using UnityEngine;

/// <summary>
/// 몬스터 사망 시 등급별 상자를 드랍합니다.
/// </summary>
public class ChestDropManager : MonoBehaviour
{
    public static ChestDropManager Instance { get; private set; }

    [SerializeField] ChestDropSettings settings;
    [SerializeField] PoolManager pool;
    [SerializeField] float scatterRadius = 0.4f;

    public ChestDropSettings Settings => settings;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveSettings();
        ResolvePool();
    }

    void ResolveSettings()
    {
        if (settings != null)
            return;

        if (GameManager.instance != null && GameManager.instance.chestDropSettings != null)
            settings = GameManager.instance.chestDropSettings;

        if (settings == null)
            settings = Resources.Load<ChestDropSettings>("Data/ChestDropSettings");
    }

    void ResolvePool()
    {
        if (pool == null && GameManager.instance != null)
            pool = GameManager.instance.pool;
    }

    public void TryDropFromEnemy(Vector3 worldPosition)
    {
        TryDrop(worldPosition, isBoss: false);
    }

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

        ChestGrade grade = settings.RollGrade();
        SpawnChest(grade, worldPosition);
    }

    void SpawnChest(ChestGrade grade, Vector3 worldPosition)
    {
        GameObject chestObj = pool.GetChest((int)grade);
        if (chestObj == null)
            return;

        Vector2 offset = Random.insideUnitCircle * scatterRadius;
        chestObj.transform.position = worldPosition + new Vector3(offset.x, offset.y, 0f);

        DroppedChest chest = chestObj.GetComponent<DroppedChest>();
        if (chest != null)
            chest.Setup(grade, settings);
    }
}
