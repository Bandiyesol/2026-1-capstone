using UnityEngine;
using System.Collections;

// 오래 남는 거대 암석
public class GiantRock : BiomeGimmick, IDamageable
{
    [Header("암석 체력")]
    [SerializeField] float maxHealth = 200f;
    [SerializeField] float health = 200f;

    [Header("경고 시간")]
    [SerializeField] float warningTime = 1f;

    [Header("낙하 데미지")]
    [SerializeField] float fallDamage = 80f;

    [Header("경고 표시")]
    [SerializeField] GameObject warningCircle;

    [Header("실제 암석")]
    [SerializeField] GameObject rock;

    [Header("낙하 판정")]
    [SerializeField] Collider2D damageColl;

    // 본체 충돌
    Collider2D bodyColl;

    // 물리 제어
    Rigidbody2D rockRigid;

    // 파괴 여부
    bool dead;

    // 총알 피격 가능
    bool canTakeBulletDamage;

    // 낙하 위험 상태
    bool fallingDanger;

    protected override void Awake()
    {
        // 체력 초기화
        health = maxHealth;

        // 부모 초기화
        base.Awake();

        // 컴포넌트 캐싱
        bodyColl = GetComponent<Collider2D>();
        rockRigid = GetComponent<Rigidbody2D>();
    }

    protected override void Update()
    {
        // 부모 처리
        base.Update();
    }

    protected override void OnSpawn()
    {
        // 코루틴 정리
        StopAllCoroutines();

        // 상태 초기화
        dead = false;
        canTakeBulletDamage = false;
        fallingDanger = false;

        // 낙하 판정 끔
        if (damageColl != null)
            damageColl.enabled = false;

        // 부모 충돌 끔
        DisableCollider();

        // 경고 표시
        if (warningCircle != null)
            warningCircle.SetActive(true);

        // 암석 숨김
        if (rock != null)
            rock.SetActive(false);

        // 본체 충돌 끔
        if (bodyColl != null)
            bodyColl.enabled = false;

        // 물리 초기화
        if (rockRigid != null)
        {
            rockRigid.bodyType = RigidbodyType2D.Dynamic;
            rockRigid.simulated = true;
            rockRigid.linearVelocity = Vector2.zero;
            rockRigid.angularVelocity = 0f;
        }

        // 낙하 시작
        StartCoroutine(FallRoutine());
    }

    IEnumerator FallRoutine()
    {
        // 경고 대기
        yield return new WaitForSeconds(warningTime);

        // 경고 제거
        if (warningCircle != null)
            warningCircle.SetActive(false);

        // 암석 등장
        if (rock != null)
            rock.SetActive(true);

        // 낙하 시작
        fallingDanger = true;

        // 충돌 활성화
        if (bodyColl != null)
            bodyColl.enabled = true;

        if (damageColl != null)
            damageColl.enabled = true;
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        // 부모 처리
        base.OnTriggerEnter2D(collision);

        // 종료 상태
        if (dead || !fallingDanger)
            return;

        // 플레이어 충돌
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();

            if (player != null)
            {
                // [방어 시스템 연동] 무조건 깎이던 연산에서 스탯 방어 필터링으로 보완
                if (PlayerStats.Instance != null)
                {
                    PlayerStats.Instance.TakeDamage(fallDamage);
                }
                else
                {
                    GameManager.instance.Health -= fallDamage;
                }

                // 밖으로 밀기 연출 유지
                PushOut(collision);
            }

            return;
        }

        // 적 충돌 (기존 로직 100% 동일하게 유지)
        if (collision.CompareTag("Enemy"))
        {
            // 보스 제외
            BossBase boss = collision.GetComponent<BossBase>();

            if (boss != null)
                return;

            Enemy enemy = collision.GetComponent<Enemy>();

            if (enemy != null)
            {
                // 데미지
                enemy.TakeDamage(fallDamage);

                // 밖으로 밀기
                PushOut(collision);
            }

            return;
        }
    }

    // 암석 밖으로 밀기 (기존 물리 로직 유지)
    void PushOut(Collider2D targetColl)
    {
        if (targetColl == null || bodyColl == null)
            return;

        Vector2 targetPos = targetColl.transform.position;

        // 가장 가까운 표면
        Vector2 closest = bodyColl.ClosestPoint(targetPos);

        // 중심 기준 방향
        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;

        // 중심 겹침 대비
        if (dir == Vector2.zero)
            dir = Vector2.up;

        // 콜라이더 밖으로 이동
        targetColl.transform.position = closest + dir * 0.5f;
    }

    // 피격 처리
    public void TakeDamage(float damage)
    {
        if (dead || !canTakeBulletDamage)
            return;

        // 체력 감소
        health -= damage;

        // 파괴
        if (health <= 0f)
            BreakRock();
    }

    // 암석 파괴
    void BreakRock()
    {
        dead = true;

        // 충돌 제거
        if (bodyColl != null)
            bodyColl.enabled = false;

        if (damageColl != null)
            damageColl.enabled = false;

        // 비활성화
        gameObject.SetActive(false);
    }

    // 총알 허용
    public void EnableBulletDamage()
    {
        canTakeBulletDamage = true;
    }

    // 낙하 종료
    public void EndFall()
    {
        // 위험 종료
        fallingDanger = false;

        // 낙하 판정 제거
        DisableDamageCollider();

        // 완전 고정
        if (rockRigid != null)
        {
            rockRigid.linearVelocity = Vector2.zero;
            rockRigid.angularVelocity = 0f;
            rockRigid.bodyType = RigidbodyType2D.Static;
        }
    }

    // 낙하 판정 제거
    public void DisableDamageCollider()
    {
        if (damageColl != null)
            damageColl.enabled = false;
    }

    protected override void OnPlayerTrigger(Player player)
    {
        // 미사용
    }

    void OnDisable()
    {
        // 상태 초기화
        dead = false;
        health = maxHealth;

        if (rockRigid != null)
        {
            rockRigid.bodyType = RigidbodyType2D.Dynamic;
            rockRigid.simulated = false;
            rockRigid.linearVelocity = Vector2.zero;
            rockRigid.angularVelocity = 0f;
        }
    }
}