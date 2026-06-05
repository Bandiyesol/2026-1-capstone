using UnityEngine;

// 플레이어에게 붙어 도트딜을 주며, 플레이어 공격으로 파괴 가능한 기믹
// 무기 시스템과 연동하기 위해 IDamageable 인터페이스를 구현합니다.
public class CursedBubble : BiomeGimmick, IDamageable
{
    [Header("[ 도트딜 설정 ]")]
    [SerializeField] float damage = 3f;          // 틱당 피해
    [SerializeField] float tickInterval = 0.5f;  // 피해 간격
    [SerializeField] float attachDelay = 1f;     // 부착 후 도트딜 시작 대기 시간

    [Header("[ 체력 설정 ]")]
    [SerializeField] float maxHealth = 3f;       // 플레이어 공격을 견딜 수 있는 횟수
    private float currentHealth;                 // 현재 체력

    // 붙은 플레이어
    Player attachedPlayer;

    // 도트 타이머
    float tickTimer;

    // 부착 대기 타이머
    float attachTimer;

    // 도트 시작 여부
    bool damageStarted;

    protected override void OnEnable()
    {
        base.OnEnable();

        // 상태 초기화
        attachedPlayer = null;

        tickTimer = 0f;
        attachTimer = 0f;

        damageStarted = false;

        // 체력 초기화
        currentHealth = maxHealth;
    }

    protected override void Update()
    {
        // 부모 수명 처리
        base.Update();

        if (attachedPlayer == null)
            return;

        // 플레이어 따라가기
        transform.position =
            attachedPlayer.transform.position;

        // 부착 후 대기 시간
        if (!damageStarted)
        {
            attachTimer += Time.deltaTime;

            if (attachTimer >= attachDelay)
            {
                damageStarted = true;
                tickTimer = 0f;
            }

            return;
        }

        // 도트 타이머
        tickTimer += Time.deltaTime;

        // 주기 피해
        if (tickTimer >= tickInterval)
        {
            tickTimer = 0f;

            // 방어 무시 피해
            PlayerStats.Instance.TakeDamage(
                damage +
                PlayerStats.Instance.Defense
            );
        }
    }

    // 플레이어 접촉 시 부착
    protected override void OnPlayerTrigger(Player player)
    {
        // 최초 1회만 부착
        if (attachedPlayer == null)
        {
            attachedPlayer = player;

            attachTimer = 0f;
            tickTimer = 0f;
            damageStarted = false;
        }
    }

    // 플레이어 공격으로 피해
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0f)
        {
            gameObject.SetActive(false);
        }
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        // 부모 플레이어 감지 처리
        base.OnTriggerEnter2D(collision);

        Motion weaponMotion =
            collision.GetComponent<Motion>();

        if (weaponMotion != null &&
            weaponMotion.instance != null &&
            weaponMotion.instance.info != null)
        {
            string weaponType =
                weaponMotion.instance.info.type;

            bool isExempt =
                weaponType == "Sword" ||
                weaponType == "Hammer" ||
                weaponType == "Scythe" ||
                weaponType == "Orb" ||
                weaponType == "Grimoire";

            // 예외 무기 제외하고 투사체 제거
            if (!isExempt)
            {
                collision.gameObject.SetActive(false);
            }
        }
    }
}