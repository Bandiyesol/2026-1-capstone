using System.Collections;
using UnityEngine;

public class BossBase : MonoBehaviour, IDamageable
{
    [HideInInspector]
    public WaveManager waveManager;

    public BossData data;

    [Header("마법진 풀링 설정")]
    [Tooltip("PoolManager의 GimmickPrefabs 배열에서 마법진이 위치한 인덱스 번호")]
    [SerializeField] private int portalGimmickIndex = 13;
    [SerializeField] private float spawnDelay = 2.0f;

    [Header("보스 스텟(소환될 때 자동으로 설정)")]
    public float health;
    public float maxHealth;
    protected float moveSpeed;
    protected float attackDamage;
    public float AttackDamage => attackDamage;
    protected float defense;

    protected Transform target;
    protected bool canMove = true;
    protected bool isPatternPlaying;
    protected float patternCooldown;
    protected float patternTimer;
    protected bool isDead;

    // 체력/스탯 초기화가 필요한지 여부 — ResetBoss() 호출 시 true, OnEnable()에서 초기화 후 false
    bool needsReset = true;

    public static Vector3? LastDeathWorldPosition { get; private set; }
    public static Vector3? LastEnemyDeathWorldPosition { get; private set; }

    public static void RecordEnemyDeath(Vector3 worldPosition)
    {
        LastEnemyDeathWorldPosition = worldPosition;
    }

    public static void ClearLastDeathPosition()
    {
        LastDeathWorldPosition = null;
        LastEnemyDeathWorldPosition = null;
    }

    protected Rigidbody2D rigid;
    protected SpriteRenderer spriter;
    protected Animator anim;
    protected Collider2D col;

    protected virtual void Start() { }

    protected virtual void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    protected virtual void OnEnable()
    {
        if (GameManager.instance != null)
            target = GameManager.instance.player.transform;

        // needsReset이 true일 때만 스탯 초기화 (전투 중 의도치 않은 재활성화 시 체력 유지)
        if (needsReset && data != null)
        {
            maxHealth = data.maxHealth;
            health = maxHealth;
            moveSpeed = data.moveSpeed;
            attackDamage = data.attackDamage;
            defense = data.damageReduction;
            patternCooldown = data.patternCooldown;
            needsReset = false;
        }

        canMove = true;
        isPatternPlaying = false;
        isDead = false;
        patternTimer = 0f;

        if (spriter != null) spriter.enabled = true;
        if (col != null) col.enabled = true;

        rigid.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// 풀에서 꺼내기 직전 PoolManager에서 호출 — 다음 OnEnable()에서 체력/스탯을 풀피로 초기화하도록 예약
    /// </summary>
    public void ResetBoss()
    {
        needsReset = true;
    }

    protected virtual void Update()
    {
        if (!GameManager.instance.isLive)
            return;

        if (isDead || isPatternPlaying) return;

        patternTimer += Time.deltaTime;

        if (patternTimer >= patternCooldown)
        {
            patternTimer = 0f;
            StartRandomPattern();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (!GameManager.instance.isLive)
            return;

        if (isDead) return;

        if (!canMove)
        {
            rigid.linearVelocity = Vector2.zero;
            anim.SetInteger("Moving", 0);
            return;
        }

        if (target == null) return;

        Vector2 dir = ((Vector2)target.position - rigid.position).normalized;
        rigid.MovePosition(rigid.position + dir * moveSpeed * Time.fixedDeltaTime);

        anim.SetInteger("Moving", 1);
        spriter.flipX = target.position.x < transform.position.x;
    }

    protected virtual void StartRandomPattern() { }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;

        float finalDamage = damage * (1f - defense);
        health -= finalDamage;

        if (health <= 0)
            Dead();
    }

    protected virtual void Dead()
    {
        if (isDead) return;
        isDead = true;

        // 다음 판에서 풀에서 꺼낼 때 풀피로 시작하도록 예약
        needsReset = true;

        LastDeathWorldPosition = transform.position;
        RecordEnemyDeath(transform.position);

        if (GameManager.instance != null)
            GameManager.instance.Kill++;

        if (CoinDropManager.Instance != null)
            CoinDropManager.Instance.TryDropFromBoss(transform.position);

        if (ChestDropManager.Instance != null)
            ChestDropManager.Instance.TryDropFromBoss(transform.position);

        waveManager?.OnEnemyDead();

        AccessoryEffect.instance?.NotifyBossDead();

        rigid.linearVelocity = Vector2.zero;
        canMove = false;

        if (spriter != null) spriter.enabled = false;
        if (col != null) col.enabled = false;

        Vector3 spawnPosition = transform.position;
        MonoBehaviour coroutineHost = GameManager.instance != null ? GameManager.instance : this;
        coroutineHost.StartCoroutine(SpawnPortalRoutine(spawnPosition));
    }

    private IEnumerator SpawnPortalRoutine(Vector3 spawnPosition)
    {
        yield return new WaitForSeconds(spawnDelay);

        if (PoolManager.Instance != null)
            StageClearSpawnUtility.SpawnPortalAndShopkeeper(spawnPosition, portalGimmickIndex);
        else
            Debug.LogWarning("PoolManager 인스턴스를 찾을 수 없습니다.");

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    protected IEnumerator FlashInvincible(Color flashColor)
    {
        if (spriter == null) yield break;
        spriter.color = flashColor;
        yield return new WaitForSeconds(0.15f);
        spriter.color = Color.white;
    }
}