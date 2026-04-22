using UnityEngine;

public class Enemy : MonoBehaviour
{
    // 이동 속도
    public float speed;
    // 현재 체력
    public float health;
    // 최대 체력
    public float maxHealth;
    // 타입별 애니메이터 컨트롤러
    public RuntimeAnimatorController[] animCon;
    // 추적 대상(플레이어 리지드바디)
    public Rigidbody2D target;
    // 생존 상태 플래그
    bool isLive;
    bool isFrozen;
    float freezeTimer;

    // 자주 사용하는 컴포넌트 캐싱
    Rigidbody2D rigid;
    Collider2D coll;
    Animator anim;
    SpriteRenderer spriter;

    [Header("Knockback")]
    [SerializeField] float knockbackDuration = 0.09f;
    [SerializeField] float knockbackSpeed = 4.5f;
    // 탄환 속도가 너무 작을 때 넉백 방향 계산 기준값
    [SerializeField, Tooltip("탄환 속도가 너무 작을 때 넉백 방향 계산 최소 제곱속도")]
    float knockbackVelocityMinSqr = 0.25f;

    float knockbackTimer;
    Vector2 knockbackDir;

    void Awake()
    {
        // 런타임 중 자주 접근하는 컴포넌트는 미리 참조 저장
        rigid = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        // 게임이 멈췄거나 이미 죽은 적은 이동 처리하지 않음
        if (!GameManager.instance.isLive)
            return;

        if (!isLive)
            return;
        if (isFrozen)
{
        freezeTimer -= Time.fixedDeltaTime;
        if (freezeTimer <= 0f) isFrozen = false;
        rigid.linearVelocity = Vector2.zero;
        return;
}
        // 넉백 시간 동안은 플레이어 추적 대신 넉백 이동 우선
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            rigid.MovePosition(rigid.position + knockbackDir * (knockbackSpeed * Time.fixedDeltaTime));
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        // 기본 AI: 플레이어를 향해 일정 속도로 이동
        Vector2 dirVec = target.position - rigid.position;
        Vector2 nextVec = dirVec.normalized * speed * Time.fixedDeltaTime;
        rigid.MovePosition(rigid.position + nextVec);
        rigid.linearVelocity = Vector2.zero;
    }

    void LateUpdate()
    {
        // 타겟 기준으로 좌우 반전해 바라보는 방향 표현
        if (!GameManager.instance.isLive)
            return;
            
        if (!isLive)
            return;

        spriter.flipX = target.position.x < rigid.position.x;
    }

    void OnEnable()
    {
        // 풀에서 재활성화될 때 상태를 전투 가능 상태로 초기화
        if (GameManager.instance != null && GameManager.instance.player != null)
        {
            target = GameManager.instance.player.GetComponent<Rigidbody2D>();
            isLive = true;
            coll.enabled = true;
            rigid.simulated = true;
            spriter.sortingOrder = 2;
            health = maxHealth;
        }

        knockbackTimer = 0f;
    }

    public void Init(SpawnData data)
    {
        // 스폰 데이터에 맞춰 외형/능력치 초기화
        anim.runtimeAnimatorController = animCon[data.spriteType];
        speed = data.speed;
        maxHealth = data.health;
        health = data.health;
    }

 void OnTriggerEnter2D(Collider2D collision)
{
    if (!collision.CompareTag("Bullet") || !isLive)
        return;

    // BulletRune 먼저 시도, 없으면 기존 Bullet 시도
    float dmg = 0f;
    bool isMeleeHit = false;

    BulletRune br = collision.GetComponent<BulletRune>();
    if (br != null)
    {
        dmg = br.damage;
        isMeleeHit = br.isMelee;
    }
    else
    {
        Bullet bullet = collision.GetComponent<Bullet>();
        if (bullet == null) return;
        dmg = bullet.damage;
        isMeleeHit = bullet.isMelee;
    }

    health -= dmg;

    if (!isMeleeHit)
    {
        knockbackTimer = knockbackDuration;
        knockbackDir = GetKnockbackDirection(collision);
    }

    if (health > 0)
    {
        // 피격 생존
    }
    else
    {
        isLive = false;
        coll.enabled = false;
        rigid.simulated = false;
        spriter.sortingOrder = 1;
        Dead();
        GameManager.instance.Kill++;
        GameManager.instance.Coin++;
    }
}

    Vector2 GetKnockbackDirection(Collider2D bulletCollider)
    {
        // 1순위: 탄환 속도가 충분하면 그 진행 방향 사용
        if (bulletCollider.TryGetComponent(out Rigidbody2D brb) &&
            brb.linearVelocity.sqrMagnitude > knockbackVelocityMinSqr)
            return brb.linearVelocity.normalized;

        // 2순위: 충돌 지점에서 적 중심으로 향하는 벡터 사용
        Vector2 closest = bulletCollider.ClosestPoint(rigid.position);
        Vector2 awayFromHit = rigid.position - closest;
        if (awayFromHit.sqrMagnitude > 1e-4f)
            return awayFromHit.normalized;

        // 3순위: 탄환 중심점 반대 방향 사용
        Vector2 awayFromBullet = rigid.position - (Vector2)bulletCollider.transform.position;
        if (awayFromBullet.sqrMagnitude > 1e-4f)
            return awayFromBullet.normalized;

        // 마지막 보정: 플레이어 반대 방향
        if (target != null)
            return (rigid.position - target.position).normalized;

        // 완전 예외 상황 기본값
        return Vector2.right;
    }
    
    public void ApplyFreeze(float duration)
{
    isFrozen = true;
    freezeTimer = Mathf.Max(freezeTimer, duration);
}
    void Dead()
    {
        // 사망 시 레벨업 체크 후 오브젝트 풀로 반납
        GameManager.instance.GetLevelUp();
        gameObject.SetActive(false);
    }
}

