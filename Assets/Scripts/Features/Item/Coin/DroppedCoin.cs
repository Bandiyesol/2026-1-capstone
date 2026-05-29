using UnityEngine;

/// <summary>
/// 바닥에 떨어진 코인. 플레이어가 닿으면 GameManager.Coin에 가치를 더합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DroppedCoin : MonoBehaviour
{
    [SerializeField] CoinType coinType = CoinType.Bronze;
    [SerializeField] float magnetMaxSpeed = 10f;
    [SerializeField] float magnetAcceleration = 30f;

    CoinDropSettings settings;
    Animator anim;
    bool collected;
    float currentMagnetSpeed;

    public CoinType Type => coinType;

    void Awake()
    {
        anim = GetComponent<Animator>();
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    public void Setup(CoinType type, CoinDropSettings dropSettings)
    {
        coinType = type;
        settings = dropSettings;
        collected = false;
        currentMagnetSpeed = 0f;

        RestartDefaultAnimation();
    }

    void Update()
    {
        if (collected || GameManager.instance == null || !GameManager.instance.isLive)
            return;

        Player player = GameManager.instance.player;
        PlayerStats stats = PlayerStats.Instance;
        if (player == null || stats == null)
            return;

        Vector3 playerPosition = player.transform.position;
        Vector3 coinPosition = transform.position;
        float distance = Vector2.Distance(playerPosition, coinPosition);

        if (distance > stats.MagnetRange)
        {
            currentMagnetSpeed = 0f;
            return;
        }

        currentMagnetSpeed = Mathf.MoveTowards(
            currentMagnetSpeed,
            magnetMaxSpeed,
            magnetAcceleration * Time.deltaTime
        );

        transform.position = Vector2.MoveTowards(
            coinPosition,
            playerPosition,
            currentMagnetSpeed * Time.deltaTime
        );
    }

    void RestartDefaultAnimation()
    {
        if (anim == null)
            return;

        anim.Rebind();
        anim.Update(0f);
        anim.Play(0, 0, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !GameManager.instance.isLive)
            return;

        if (other.GetComponent<Player>() == null)
            return;

        Collect();
    }

    void Collect()
    {
        collected = true;

        int value = settings != null ? settings.GetValue(coinType) : GetDefaultValue();
        GameManager.instance.AddCoin(value);

        gameObject.SetActive(false);
    }

    static int GetDefaultValue()
    {
        return 1;
    }
}
