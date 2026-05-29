using UnityEngine;

public class BossBase : MonoBehaviour, IDamageable
{
    // 웨이브 매니저
    [HideInInspector]
    public WaveManager waveManager;

    // 보스 데이터
    public BossData data;

    // 현재 체력
    public float health;
    // 최대 체력
    public float maxHealth;
    // 이동 속도
    protected float moveSpeed;
    // 공격력
    protected float attackDamage;
    // 공격력 반환
    public float AttackDamage => attackDamage;
    // 방어력
    protected float defense;

    // 플레이어
    protected Transform target;
    // 이동 가능 여부
    protected bool canMove = true;
    // 패턴 실행 여부
    protected bool isPatternPlaying;
    // 패턴 쿨타임
    protected float patternCooldown;
    // 패턴 타이머
    protected float patternTimer;
    // 이미 죽었는지
    bool isDead;

    // 리지드바디
    protected Rigidbody2D rigid;
    // 스프라이트
    protected SpriteRenderer spriter;
    // 애니메이터
    protected Animator anim;

    protected virtual void Start()
    {

    }

    protected virtual void Awake()
    {
        // 컴포넌트 저장
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    protected virtual void OnEnable()
    {
        // 플레이어 저장
        if (GameManager.instance != null)
        {
            target =
                GameManager.instance.player.transform;
        }

        // 데이터 적용
        if (data != null)
        {
            // 체력
            maxHealth = data.maxHealth;
            health = maxHealth;

            // 이동 속도
            moveSpeed = data.moveSpeed;

            // 공격력
            attackDamage = data.attackDamage;

            // 방어력
            defense = data.damageReduction;

            // 패턴 쿨타임
            patternCooldown =
                data.patternCooldown;
        }

        // 상태 초기화
        canMove = true;
        isPatternPlaying = false;
        isDead = false;

        // 타이머 초기화
        patternTimer = 0f;

        // 이동 정지
        rigid.linearVelocity = Vector2.zero;
    }

    protected virtual void Update()
    {
        // 패턴 중이면 대기
        if (isPatternPlaying)
            return;

        // 타이머 증가
        patternTimer += Time.deltaTime;

        // 패턴 발동
        if (patternTimer >= patternCooldown)
        {
            patternTimer = 0f;

            StartRandomPattern();
        }
    }

    protected virtual void FixedUpdate()
    {
        // 이동 불가
        if (!canMove)
        {
            rigid.linearVelocity = Vector2.zero;
            anim.SetInteger("Moving", 0);
            return;
        }

        // 플레이어 없음
        if (target == null)
            return;

        // 플레이어 추적
        Vector2 dir =
            ((Vector2)target.position -
            rigid.position).normalized;

        rigid.MovePosition(
            rigid.position +
            dir * moveSpeed * Time.fixedDeltaTime
        );

        // 애니메이션 작동
        anim.SetInteger("Moving", 1);

        // 좌우 반전
        spriter.flipX =
            target.position.x < transform.position.x;
    }

    // 랜덤 패턴
    protected virtual void StartRandomPattern()
    {

    }

    // 데미지 처리
    public virtual void TakeDamage(float damage)
    {
        // 방어력 적용
        float finalDamage =
            damage * (1f - defense);

        // 체력 감소
        health -= finalDamage;

        // 사망
        if (health <= 0)
        {
            Dead();
        }
    }

    // 사망 처리
    protected virtual void Dead()
    {
        // 중복 방지
        if (isDead)
            return;

        isDead = true;

        if (GameManager.instance != null)
            GameManager.instance.Kill++;

        if (CoinDropManager.Instance != null)
            CoinDropManager.Instance.TryDropFromBoss(transform.position);

        if (ChestDropManager.Instance != null)
            ChestDropManager.Instance.TryDropFromBoss(transform.position);

        // 웨이브 알림
        waveManager?.OnEnemyDead();

        // 이동 정지
        rigid.linearVelocity = Vector2.zero;

        // 비활성화
        gameObject.SetActive(false);
    }

}