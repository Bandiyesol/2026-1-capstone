using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("데이터")]
    // 적 데이터
    [SerializeField] EnemyData data;

    [Header("애니메이터")]
    // 애니메이터 목록
    public RuntimeAnimatorController[] animCon;
    // 플레이어 Rigidbody
    public Rigidbody2D target;
    // WaveManager
    public WaveManager waveManager;

    [Header("적 스텟(건드리지 말 것)")]
    // 현재 체력
    public float health;
    // 최대 체력
    public float maxHealth;
    // 공격력
    public float attackDamage;
    // 이동 속도
    public float speed;

    // 생존 여부
    bool isLive;
    // 빙결 여부
    bool isFrozen;
    // 은신 여부
    bool hiddenInFog;
    // 빙결 시간
    float freezeTimer;
    // Rigidbody
    Rigidbody2D rigid;
    // Collider
    Collider2D coll;
    // Animator
    Animator anim;
    // SpriteRenderer
    SpriteRenderer spriter;
    // 원래 색상
    Color originColor;

    [Header("넉백")]
    // 넉백 시간
    [SerializeField] float knockbackDuration = 0.09f;
    // 넉백 속도
    [SerializeField] float knockbackSpeed = 4.5f;
    // 최소 속도 기준
    [SerializeField]
    float knockbackVelocityMinSqr = 0.25f;

    // 넉백 타이머
    float knockbackTimer;
    // 넉백 방향
    Vector2 knockbackDir;

    void Awake()
    {
        // 컴포넌트 캐싱
        rigid = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();

        originColor = spriter.color;
    }

    void OnEnable()
    {
        // 플레이어 가져오기
        if (GameManager.instance != null
            && GameManager.instance.player != null)
        {
            target = GameManager.instance.player.GetComponent<Rigidbody2D>();
        }

        // 상태 초기화
        isLive = true;
        isFrozen = false;

        // 컴포넌트 복구
        coll.enabled = true;
        rigid.simulated = true;

        // 정렬 순서
        spriter.sortingOrder = 2;

        // 넉백 초기화
        knockbackTimer = 0f;

        // 데이터 적용
        ApplyData();
    }

    // 데이터 적용
    void ApplyData()
    {
        // 데이터 없음
        if (data == null)
            return;

        // 애니메이터 적용
        anim.runtimeAnimatorController = animCon[data.spriteType];

        // 스탯 적용
        speed = data.moveSpeed;

        maxHealth = data.maxHealth;
        health = maxHealth;

        // 공격력 적용
        attackDamage = data.attackDamage;
    }

    void FixedUpdate()
    {
        // 게임 정지
        if (!GameManager.instance.isLive)
            return;

        // 사망 상태
        if (!isLive)
            return;

        // 빙결 상태
        if (isFrozen)
        {
            freezeTimer -= Time.fixedDeltaTime;

            if (freezeTimer <= 0f)
                isFrozen = false;

            rigid.linearVelocity = Vector2.zero;

            return;
        }

        // 넉백 상태
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;

            rigid.MovePosition
            (
                rigid.position +
                knockbackDir *
                (knockbackSpeed * Time.fixedDeltaTime)
            );

            rigid.linearVelocity = Vector2.zero;

            return;
        }

        // 방향 계산
        Vector2 dirVec =
            target.position - rigid.position;

        // 이동 계산
        Vector2 nextVec =
            dirVec.normalized *
            speed *
            Time.fixedDeltaTime;

        // 이동
        rigid.MovePosition
        (
            rigid.position + nextVec
        );

        rigid.linearVelocity = Vector2.zero;
    }

    void LateUpdate()
    {
        // 게임 정지
        if (!GameManager.instance.isLive)
            return;

        // 사망 상태
        if (!isLive)
            return;

        // 방향 반전
        spriter.flipX =
            target.position.x < rigid.position.x;

        // 안개 은신 처리
        if (hiddenInFog)
        {
            float dist =
                Vector2.Distance
                (
                    rigid.position,
                    target.position
                );

            // 가까우면 보임
            float alpha =
                dist <= 2.2f ? 1f : 0.12f;

            Color c = spriter.color;
            c.a = alpha;

            spriter.color = c;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 총알만 처리
        if (!collision.CompareTag("Bullet"))
            return;

        // 죽은 상태
        if (!isLive)
            return;

        float dmg = 0f;

        bool isMeleeHit = false;

        // BulletRune 우선
        BulletRune br =
            collision.GetComponent<BulletRune>();

        if (br != null)
        {
            dmg = br.damage;

            isMeleeHit = br.isMelee;
        }
        else
        {
            // 일반 Bullet
            Bullet bullet =
                collision.GetComponent<Bullet>();

            if (bullet == null)
                return;

            dmg = bullet.damage;

            isMeleeHit = bullet.isMelee;
        }

        // 체력 감소
        health -= dmg;

        // 원거리 넉백
        if (!isMeleeHit)
        {
            knockbackTimer = knockbackDuration;

            knockbackDir =
                GetKnockbackDirection(collision);
        }

        // 사망 체크
        if (health <= 0)
        {
            isLive = false;

            coll.enabled = false;

            rigid.simulated = false;

            spriter.sortingOrder = 1;

            Dead();
        }
    }

    // 넉백 방향 계산
    Vector2 GetKnockbackDirection
    (
        Collider2D bulletCollider
    )
    {
        // 탄속 기준
        if
        (
            bulletCollider.TryGetComponent
            (
                out Rigidbody2D brb
            )
            &&
            brb.linearVelocity.sqrMagnitude >
            knockbackVelocityMinSqr
        )
        {
            return brb.linearVelocity.normalized;
        }

        // 충돌 지점 기준
        Vector2 closest =
            bulletCollider.ClosestPoint(rigid.position);

        Vector2 awayFromHit =
            rigid.position - closest;

        if (awayFromHit.sqrMagnitude > 1e-4f)
            return awayFromHit.normalized;

        // 탄환 위치 기준
        Vector2 awayFromBullet =
            rigid.position -
            (Vector2)bulletCollider.transform.position;

        if (awayFromBullet.sqrMagnitude > 1e-4f)
            return awayFromBullet.normalized;

        // 플레이어 기준
        if (target != null)
        {
            return
                (rigid.position - target.position)
                .normalized;
        }

        return Vector2.right;
    }

    // 빙결 적용
    public void ApplyFreeze(float duration)
    {
        isFrozen = true;

        freezeTimer =
            Mathf.Max(freezeTimer, duration);
    }

    // 사망 처리
    void Dead()
    {
        // 웨이브 알림
        waveManager?.OnEnemyDead();

        // 경험치 처리
        GameManager.instance.GetLevelUp();

        // 풀 반환
        gameObject.SetActive(false);
    }
    // 강제 즉사
    public void KillInstantly()
    {
        if (!isLive)
            return;

        health = 0f;

        isLive = false;

        coll.enabled = false;

        rigid.simulated = false;

        spriter.sortingOrder = 1;

        Dead();
    }

    // 안개 은신 시작
    public void EnterFog()
    {
        hiddenInFog = true;
    }

    // 안개 은신 종료
    public void ExitFog()
    {
        hiddenInFog = false;

        // 원래 색 복구
        Color c = spriter.color;
        c.a = 1f;

        spriter.color = c;
    }
}