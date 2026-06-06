using UnityEngine;

/// <summary>
/// 바닥에 떨어진 코인. 플레이어가 닿으면 GameManager.Coin에 가치를 더합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DroppedCoin : MonoBehaviour
{
    [SerializeField] CoinType coinType = CoinType.Bronze;

    CoinDropSettings settings;
    Animator anim;
    bool collected;

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

        RestartDefaultAnimation();
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

        int value = settings != null ? settings.GetValue(coinType) : GetDefaultValue(coinType);
        GameManager.instance.AddCoin(value);

        gameObject.SetActive(false);
    }

    static int GetDefaultValue(CoinType type)
    {
        return type switch
        {
            CoinType.Gold => 10,
            CoinType.Silver => 5,
            _ => 1,
        };
    }
}
