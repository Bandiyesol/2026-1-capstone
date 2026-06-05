using System.Collections;
using UnityEngine;

// 얼음 내려찍기 범위 공격
public class IceSmashAOE : BossBullet
{
    [Header("빙결")]
    public float slowDuration = 3f;       // 감속 지속시간
    public float slowMultiplier = 0.4f;   // 감속 배율

    [Header("넉백")]
    public float knockbackForce = 15f;    // 넉백 힘

    SpriteRenderer spriter; // 렌더러 캐싱
    Animator anim;          // 애니메이터 캐싱
    bool canDamage;         // 타격 가능 여부

    void Awake()
    {
        // 컴포넌트 캐싱
        col = GetComponent<Collider2D>();
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        StopAllCoroutines();
        canDamage = true;

        // 스프라이트 표시
        if (spriter != null)
            spriter.enabled = true;

        // 물리 초기화
        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.angularVelocity = 0f;
            rigid.Sleep();
            rigid.WakeUp();
        }

        // 충돌 초기 비활성
        if (col != null)
            col.enabled = false;

        // 애니메이션 재시작
        if (anim != null)
        {
            anim.Rebind();                        // 상태 완전 초기화
            anim.Update(0f);                      // 즉시 반영
            anim.Play("IceGiant_IceSmash", 0, 0f); // 첫 프레임부터 재생
        }

        // 충돌 활성 타이밍
        StartCoroutine(ActivateRoutine());

        // 안전 종료
        StartCoroutine(LifeRoutine());
    }

    protected override void Update()
    {
        timer += Time.deltaTime;

        if (timer >= lifeTime)
            gameObject.SetActive(false);
    }

    void OnDisable()
    {
        // 코루틴 정리
        StopAllCoroutines();

        // 타격 차단
        canDamage = false;

        // 충돌 끄기
        if (col != null)
            col.enabled = false;

        // 물리 정지
        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.angularVelocity = 0f;
            rigid.Sleep();
        }
    }

    // 애니메이션 이벤트 종료
    public void OnAnimationEnd()
    {
        Debug.Log("AOE End Called");
        gameObject.SetActive(false);
    }

    IEnumerator ActivateRoutine()
    {
        // 한 물리 프레임 대기
        yield return new WaitForFixedUpdate();

        // 충돌 활성
        if (col != null)
            col.enabled = true;
    }

    IEnumerator LifeRoutine()
    {
        // 혹시 이벤트 누락 대비
        yield return new WaitForSeconds(lifeTime);

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    // 플레이어 1회만 타격
    public void DisablePlayerCollision()
    {
        canDamage = false;

        if (col != null)
            col.enabled = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 타격 불가면 종료
        if (!canDamage) return;

        // 플레이어만 처리
        if (!collision.gameObject.CompareTag("Player")) return;

        Player player = collision.gameObject.GetComponent<Player>();

        if (player != null)
        {
            // 넉백 방향 계산
            Vector2 dir =
                (collision.transform.position - transform.position).normalized;

            // 상태이상 적용
            player.ApplyKnockback(dir, knockbackForce);
            player.ApplyIceSlow(slowMultiplier, slowDuration);

            // 데미지 적용
            GameManager.instance.Health -= damage;

            // 중복 방지
            DisablePlayerCollision();
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            OnCollisionEnter2D(collision);
    }
}