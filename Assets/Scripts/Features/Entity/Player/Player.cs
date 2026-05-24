using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class Player : MonoBehaviour
{
    [Header("이동 관련")]
    public Vector2 inputVec;
    public Vector2 inputModifier = Vector2.one;
    public float inputJitter;

    [Header("물리/속도")]
    public float speed = 5f;
    public float baseSpeed = 5f;
    [HideInInspector]
    public Vector2 externalVelocity;

    [Header("탐색/환경")]
    public Scaner scaner;
    public LayerMask groundMask;

    [Header("스프라이트 틴트")]
    public Color defaultTint = Color.white;
    Color currentTint;

    Rigidbody2D rigid;
    public SpriteRenderer spriter;
    Animator anim;

    bool isStunned;
    bool isDead;

    public Vector2 lastTravelDirection = Vector2.right;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        scaner = GetComponent<Scaner>();

        defaultTint = spriter.color;
        currentTint = defaultTint;
        speed = baseSpeed;
    }

    void Update()
    {
        if (GameManager.instance != null && GameManager.instance.Health <= 0f)
            PlayerDead();
    }

    void FixedUpdate()
    {
        if (isStunned)
        {
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        if (GameManager.instance == null || !GameManager.instance.isLive)
            return;

        Vector2 inputVelocity = inputVec * speed;

        if (inputVelocity.sqrMagnitude > 1e-10f)
            lastTravelDirection = inputVelocity.normalized;

        rigid.linearVelocity = inputVelocity + externalVelocity;
        externalVelocity = Vector2.zero;
    }

    void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        input = Vector2.Scale(input, inputModifier);

        if (inputJitter > 0f)
            input += Random.insideUnitCircle * inputJitter;

        inputVec = Vector2.ClampMagnitude(input, 1f);
    }

    void LateUpdate()
    {
        if (GameManager.instance == null || !GameManager.instance.isLive)
            return;

        anim.SetFloat("Speed", inputVec.magnitude);

        if (inputVec.x != 0f)
            spriter.flipX = inputVec.x < 0;
    }

    public Vector2 GetWorldPosition() => rigid.position;

    public Vector2 GetFacingDirection()
    {
        const float velEps = 0.06f;
        if (rigid.linearVelocity.sqrMagnitude > velEps * velEps)
            return rigid.linearVelocity.normalized;

        if (inputVec.sqrMagnitude > 0.01f)
            return inputVec.normalized;

        if (lastTravelDirection.sqrMagnitude > 1e-6f)
            return lastTravelDirection;

        return spriter.flipX ? Vector2.left : Vector2.right;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (GameManager.instance == null || !GameManager.instance.isLive)
            return;

        float damage = 0f;

        Enemy enemy = collision.collider.GetComponent<Enemy>();
        if (enemy != null)
            damage = enemy.attackDamage;

        BossBase boss = collision.collider.GetComponent<BossBase>();
        if (boss != null)
            damage = boss.AttackDamage;

        if (damage <= 0f)
            return;

        GameManager.instance.Health -= damage * Time.deltaTime;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("BossBullet"))
            return;

        BossBullet bullet = collision.gameObject.GetComponent<BossBullet>();
        if (bullet != null)
            GameManager.instance.Health -= bullet.damage;

        collision.gameObject.SetActive(false);
    }

    public void PlayerDead()
    {
        if (isDead)
            return;

        isDead = true;

        for (int index = 2; index < transform.childCount; index++)
            transform.GetChild(index).gameObject.SetActive(false);

        anim.SetTrigger("Dead");
        GameManager.instance.GameOver();
    }

    public void Stun(float time)
    {
        StopAllCoroutines();
        StartCoroutine(StunRoutine(time));
    }

    IEnumerator StunRoutine(float time)
    {
        isStunned = true;
        yield return new WaitForSeconds(time);
        isStunned = false;
    }

    public void SetStatusTint(Color tint)
    {
        currentTint = tint;
        spriter.color = currentTint;
    }

    public void ResetStatusTint()
    {
        currentTint = defaultTint;
        spriter.color = defaultTint;
    }

    public bool IsOnLava()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(rigid.position, 0.15f, groundMask);
        if (hits == null)
            return false;

        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.CompareTag("Lava"))
                return true;
        }

        return false;
    }
}
