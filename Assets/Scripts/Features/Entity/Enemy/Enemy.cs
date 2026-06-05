using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("데이터")]
    [SerializeField] EnemyData data;

    [Header("애니메이터")]
    public RuntimeAnimatorController[] animCon;
    public Rigidbody2D target;
    public WaveManager waveManager;

    [Header("적 스텟(건드리지 말 것)")]
    public float health;
    public float maxHealth;
    public float attackDamage;
    public float speed;

    bool isLive;
    bool isFrozen;
    bool hiddenInFog;
    float freezeTimer;
    bool isHitEffectRunning;

    Rigidbody2D rigid;
    Collider2D coll;
    Animator anim;
    SpriteRenderer spriter;
    Color originColor;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();
        originColor = spriter.color;
    }

    void OnEnable()
    {
        if (GameManager.instance != null && GameManager.instance.player != null)
            target = GameManager.instance.player.GetComponent<Rigidbody2D>();

        isLive = true;
        isFrozen = false;
        coll.enabled = true;
        rigid.simulated = true;
        spriter.sortingOrder = 2;
        ApplyData();
    }

    void ApplyData()
    {
        if (data == null)
            return;

        anim.runtimeAnimatorController = animCon[data.spriteType];
        speed = data.moveSpeed;
        maxHealth = data.maxHealth;
        health = maxHealth;
        attackDamage = data.attackDamage;
    }

    void FixedUpdate()
    {
        if (!GameManager.instance.isLive || !isLive)
            return;

        if (isFrozen)
        {
            freezeTimer -= Time.fixedDeltaTime;
            if (freezeTimer <= 0f)
                isFrozen = false;

            rigid.linearVelocity = Vector2.zero;
            return;
        }

        if (target == null)
            return;

        Vector2 dirVec = target.position - rigid.position;
        Vector2 nextVec = dirVec.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextVec);
        rigid.linearVelocity = Vector2.zero;
    }

    void LateUpdate()
    {
        if (!GameManager.instance.isLive || !isLive || target == null)
            return;

        spriter.flipX = target.position.x < rigid.position.x;

        if (hiddenInFog)
        {
            float dist = Vector2.Distance(rigid.position, target.position);
            float alpha = dist <= 2.2f ? 1f : 0.12f;
            Color c = spriter.color;
            c.a = alpha;
            spriter.color = c;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!isLive || health <= 0f)
            return;

        health -= damage;

        if (!isHitEffectRunning)
            StartCoroutine(HitFlashEffect());

        if (health <= 0f)
            Die();
    }

    IEnumerator HitFlashEffect()
    {
        isHitEffectRunning = true;
        spriter.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriter.color = originColor;
        isHitEffectRunning = false;
    }

    public void ApplyFreeze(float duration)
    {
        isFrozen = true;
        freezeTimer = Mathf.Max(freezeTimer, duration);
    }

    void Die()
    {
        isLive = false;
        coll.enabled = false;
        rigid.simulated = false;
        spriter.sortingOrder = 1;

        if (GameManager.instance != null)
            GameManager.instance.Kill++;

        // 코인 드랍
        if (CoinDropManager.Instance != null)
            CoinDropManager.Instance.TryDropFromEnemy(transform.position);

        // 상자 드랍 — 유니크 몬스터는 높은 등급 상자
        if (ChestDropManager.Instance != null)
        {
            if (data != null && data.isUnique)
                ChestDropManager.Instance.TryDropFromBoss(transform.position);
            else
                ChestDropManager.Instance.TryDropFromEnemy(transform.position);
        }

        waveManager?.OnEnemyDead();
        gameObject.SetActive(false);
    }

    public void KillInstantly()
    {
        if (!isLive)
            return;

        health = 0f;
        Die();
    }

    public void EnterFog()
    {
        hiddenInFog = true;
    }

    public void ExitFog()
    {
        hiddenInFog = false;
        Color c = spriter.color;
        c.a = 1f;
        spriter.color = c;
    }
}