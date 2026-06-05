using UnityEngine;

// 플레이어를 붙잡아 익사시키는 잠수 함정
public class DiveTrap : BiomeGimmick
{
    [Header("익사 피해")]
    [SerializeField] float damage = 5f;
    [SerializeField] float tickInterval = 0.5f;

    [Header("잠수 시간")]
    [SerializeField] float diveDuration = 5f;

    // 붙잡힌 플레이어
    Player attachedPlayer;

    // 피해 타이머
    float tickTimer;

    // 잠수 타이머
    float diveTimer;

    // 생성한 보스
    DrownedSpiritBoss ownerBoss;

    // 포획 여부
    bool captured;

    // 생성한 보스 등록
    public void SetOwner(DrownedSpiritBoss boss)
    {
        ownerBoss = boss;
    }

    // 활성화 초기화
    protected override void OnSpawn()
    {
        attachedPlayer = null;

        tickTimer = 0f;
        diveTimer = 0f;

        captured = false;
    }

    protected override void Update()
    {
        // 포획 중
        if (attachedPlayer != null)
        {
            // 플레이어 위치 고정
            attachedPlayer.transform.position =
                transform.position;

            // 타이머 진행
            tickTimer += Time.deltaTime;
            diveTimer -= Time.deltaTime;

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

            // 잠수 종료
            if (diveTimer <= 0f)
            {
                gameObject.SetActive(false);
            }

            return;
        }

        // 포획 전에는 기본 수명 사용
        base.Update();
    }

    protected override void OnPlayerTrigger(Player player)
    {
        // 이미 포획 중
        if (attachedPlayer != null)
            return;

        attachedPlayer = player;

        captured = true;

        // 잠수 시간 시작
        diveTimer = diveDuration;

        // lifeTime보다 잠수 시간이 우선
        currentLifeTime = diveDuration + 1f;

        // 플레이어 숨김
        SpriteRenderer sr =
            player.GetComponent<SpriteRenderer>();

        if (sr != null)
            sr.enabled = false;

        // 보스 알림
        ownerBoss?.OnDiveCapture(player);
    }

    void OnDisable()
    {
        // 포획 종료
        if (captured &&
            attachedPlayer != null)
        {
            ownerBoss?.OnDiveEnd(
                attachedPlayer
            );
        }

        attachedPlayer = null;

        tickTimer = 0f;
        diveTimer = 0f;

        captured = false;
    }
}