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
    bool isDead;

    /// <summary>마지막으로 쓰러진 보스의 월드 좌표 (엔딩 연출용).</summary>
    public static Vector3? LastDeathWorldPosition { get; private set; }

    /// <summary>현재 스테이지에서 마지막으로 쓰러진 적의 월드 좌표 (엔딩 연출 폴백).</summary>
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
        {
            target = GameManager.instance.player.transform;
        }

        if (data != null)
        {
            maxHealth = data.maxHealth;
            health = maxHealth;
            moveSpeed = data.moveSpeed;
            attackDamage = data.attackDamage;
            defense = data.damageReduction;
            patternCooldown = data.patternCooldown;
        }

        canMove = true;
        isPatternPlaying = false;
        isDead = false;
        patternTimer = 0f;

        if (spriter != null) spriter.enabled = true;
        if (col != null) col.enabled = true;

        rigid.linearVelocity = Vector2.zero;
    }

    protected virtual void Update()
    {
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
        {
            Dead();
        }
    }

    protected virtual void Dead()
    {
        if (isDead) return;
        isDead = true;

        LastDeathWorldPosition = transform.position;
        RecordEnemyDeath(transform.position);

        if (GameManager.instance != null)
            GameManager.instance.Kill++;

        if (CoinDropManager.Instance != null)
            CoinDropManager.Instance.TryDropFromBoss(transform.position);

        if (ChestDropManager.Instance != null)
            ChestDropManager.Instance.TryDropFromBoss(transform.position);

        waveManager?.OnEnemyDead();

        rigid.linearVelocity = Vector2.zero;
        canMove = false;

        if (spriter != null) spriter.enabled = false;
        if (col != null) col.enabled = false;

        // 풀 매니저를 사용하는 코루틴 실행
        StartCoroutine(SpawnPortalRoutine());
    }

    // [수정] 대기 후 PoolManager에서 마법진을 활성화하는 코루틴
    private IEnumerator SpawnPortalRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);

        if (PoolManager.Instance != null)
        {
            // PoolManager에서 기믹 오브젝트 풀을 통해 마법진을 꺼내옴 (자동 SetActive(true) 처리됨)
            GameObject portal = PoolManager.Instance.GetGimmick(portalGimmickIndex);

            if (portal != null)
            {
                // 보스가 사망한 현재 위치로 마법진 순간이동
                portal.transform.position = transform.position;
            }
        }
        else
        {
            Debug.LogWarning("PoolManager 인스턴스를 찾을 수 없습니다.");
        }

        // 보스 오브젝트 비활성화 (풀로 반환 가능한 상태가 됨)
        gameObject.SetActive(false);
    }
}