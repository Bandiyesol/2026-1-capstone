using UnityEngine;
using System.Collections;

// 오래 남는 거대 암석
public class GiantRock : BiomeGimmick
{
    [Header("암석 체력")]
    [SerializeField] float maxHealth = 200f;
    [SerializeField] float health = 200f;

    [Header("경고 시간")]
    [SerializeField] float warningTime = 1f;

    [Header("경고 표시")]
    [SerializeField] GameObject warningCircle;

    [Header("실제 암석 오브젝트")]
    [SerializeField] GameObject rock;

    [Header("낙하 데미지 콜라이더")]
    [SerializeField] Collider2D damageColl;

    // 실제 벽 충돌 콜라이더
    Collider2D bodyColl;

    // 암석 리지드바디
    Rigidbody2D rockRigid;

    // 암석 생존 여부
    bool dead;
    // 총알 피격 가능 여부
    bool canTakeBulletDamage;
    // 낙하 중 즉사 여부
    bool fallingDanger;

    protected override void Awake()
    {
        // 체력
        health = maxHealth;

        // 부모 초기화
        base.Awake();

        // 부모 콜라이더 사용
        bodyColl = GetComponent<Collider2D>();

        // 부모 리지드바디 사용
        rockRigid = GetComponent<Rigidbody2D>();
    }

    protected override void Update()
    {
        // 부모 Update 실행
        base.Update();
    }

    protected override void OnSpawn()
    {
        StopAllCoroutines();

        // 낙하 데미지 충돌 끄기
        if (damageColl != null)
            damageColl.enabled = false;

        // 처음엔 총알 무시
        canTakeBulletDamage = false;

        dead = false;

        // 부모 Trigger 끄기
        DisableCollider();

        // 경고 원 켜기
        if (warningCircle != null)
            warningCircle.SetActive(true);

        // 암석 숨기기
        if (rock != null)
            rock.SetActive(false);

        // 벽 충돌 끄기
        if (bodyColl != null)
            bodyColl.enabled = false;

        // 낙하 데미지 충돌 켜기
        fallingDanger = false;

        // 리지드바디 초기화
        if (rockRigid != null)
        {
            rockRigid.bodyType = RigidbodyType2D.Dynamic;
            rockRigid.simulated = true;

            rockRigid.linearVelocity = Vector2.zero;
            rockRigid.angularVelocity = 0f;
        }

        StartCoroutine(FallRoutine());
    }

    IEnumerator FallRoutine()
    {
        // 경고 시간 대기
        yield return new WaitForSeconds(warningTime);

        // 경고 원 숨기기
        if (warningCircle != null)
            warningCircle.SetActive(false);

        // 암석 등장
        if (rock != null)
            rock.SetActive(true);

        // 이제부터 즉사 가능
        fallingDanger = true;

        // 벽 충돌 활성화
        if (bodyColl != null)
            bodyColl.enabled = true;

        // 낙하 데미지 충돌 활성화
        if (damageColl != null)
            damageColl.enabled = true;
    }

    // 충돌 처리
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (dead)
            return;

        // 낙하 데미지 처리
        if (fallingDanger)
        {
            // 플레이어 즉사
            if (collision.CompareTag("Player"))
            {
                Player player =
                    collision.GetComponent<Player>();

                if (player != null)
                {
                    GameManager.instance.Health = 0f;
                    player.PlayerDead();
                }

                return;
            }

            // 적 즉사
            if (collision.CompareTag("Enemy"))
            {
                // 보스 제외
                BossBase boss =
                    collision.GetComponent<BossBase>();

                if (boss != null)
                    return;

                Enemy enemy =
                    collision.GetComponent<Enemy>();

                if (enemy != null)
                    enemy.KillInstantly();

                return;
            }
        }

        // 아직 피격 불가 상태
        if (!canTakeBulletDamage)
            return;

        // 총알만 처리
        if (!collision.CompareTag("Bullet"))
            return;

        float dmg = 0f;

        // 룬 총알 우선
        BulletRune br =
            collision.GetComponentInParent<BulletRune>();

        if (br != null)
        {
            dmg = br.damage;

            // 💡 추가된 부분: 룬 총알을 즉시 비활성화하여 관통을 막습니다.
            br.gameObject.SetActive(false);
        }
        else
        {
            Bullet bullet =
                collision.GetComponentInParent<Bullet>();

            if (bullet == null)
                return;

            dmg = bullet.damage;

            // 💡 추가된 부분: 일반 총알을 즉시 비활성화하여 관통을 막습니다.
            bullet.gameObject.SetActive(false);
        }

        // 체력 감소
        TakeDamage(dmg);
    }

    // 데미지 처리
    void TakeDamage(float damage)
    {
        if (dead)
            return;

        health -= damage;

        // 체력 다 닳으면 파괴
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

        // 오브젝트 비활성화
        gameObject.SetActive(false);
    }

    // 애니메이터 이벤트
    public void EnableBulletDamage()
    {
        canTakeBulletDamage = true;
    }
    public void EndFall()
    {
        // 착지 완료
        fallingDanger = false;

        // 낙하 데미지 판정 제거
        DisableDamageCollider();

        // 완전 고정
        if (rockRigid != null)
        {
            rockRigid.linearVelocity = Vector2.zero;
            rockRigid.angularVelocity = 0f;

            // 절대 안 밀리게 고정
            rockRigid.bodyType = RigidbodyType2D.Static;
        }
    }

    // 낙하 데미지 제거
    public void DisableDamageCollider()
    {
        if (damageColl != null)
            damageColl.enabled = false;
    }

    // 부모 추상 함수
    protected override void OnPlayerTrigger(Player player)
    {
        // 사용 안 함
    }

    void OnDisable()
    {
        dead = false;

        // 풀 재사용 초기화
        if (rockRigid != null)
        {
            health = maxHealth;

            rockRigid.bodyType = RigidbodyType2D.Dynamic;
            rockRigid.simulated = false;

            rockRigid.linearVelocity = Vector2.zero;
            rockRigid.angularVelocity = 0f;
        }
    }
}