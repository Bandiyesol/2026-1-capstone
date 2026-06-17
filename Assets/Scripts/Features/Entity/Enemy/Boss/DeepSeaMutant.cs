using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 심해 괴수 보스 컨트롤러 (거리별 조류 흡입 + 외부 기절 탄막 조준 + 부하 링크 무적 페이즈 부모 클래스 상속)
/// </summary>
public class DeepSeaMutant : BossBase
{
    [Header("조류")]
    public GameObject currentObject;
    [SerializeField] float currentForce = 7f;
    [SerializeField] float currentRange = 6f;

    [Header("기절 탄막")]
    [SerializeField] int stunBulletIndex;
    [SerializeField] int bulletCount = 5;
    [SerializeField] float spawnRadius = 4f;
    [SerializeField] float bulletDelay = 0.25f;

    [Header("부하 소환")]
    [SerializeField] int summonEnemyIndex;
    [SerializeField] int summonCount = 4;
    [SerializeField] float summonInterval = 4f;

    // 페이즈 제어 플래그
    bool invinciblePhase;
    bool phaseTriggered;

    // 타이머 변수
    float summonTimer;

    // 탄막 추적 리스트
    List<GameObject> bullets = new List<GameObject>();

    // 최초 1회 초기화 여부 (풀 재사용 시 체력/페이즈 리셋 방지용)
    bool initialized;

    protected override void OnEnable()
    {
        // BossBase.OnEnable()은 항상 호출 (target, 컴포넌트 세팅 등 필요)
        base.OnEnable();

        // 최초 소환 시에만 페이즈/타이머 초기화
        // (이미 전투 중 재활성화될 경우 체력·페이즈 유지)
        invinciblePhase = false;
        phaseTriggered = false;
        summonTimer = 0f;
        bullets.Clear();
    }

    private void OnDisable()
    {
        bullets.Clear();
    }

    protected override void Update()
    {
        base.Update();

        if (target == null)
            return;

        ApplyCurrent();

        if (invinciblePhase)
        {
            summonTimer += Time.deltaTime;

            if (summonTimer >= summonInterval)
            {
                summonTimer = 0f;
                SummonMinions();
            }
        }
    }

    // ==========================================
    // 상시 조류 흡입 로직
    // ==========================================
    void ApplyCurrent()
    {
        Player player = target.GetComponent<Player>();

        if (player == null || player.isStunned)
        {
            currentObject.SetActive(false);
            return;
        }

        currentObject.SetActive(true);

        Vector2 dir = target.position - transform.position;
        float dist = dir.magnitude;

        if (dist > currentRange)
            return;

        dir.Normalize();

        float force = currentForce * (1f - dist / currentRange);
        player.externalVelocity += dir * force;
    }

    protected override void StartRandomPattern()
    {
        StartCoroutine(Pattern_StunShot());
    }

    // ==========================================
    // [패턴 1] 플레이어 주변 포위 기절 탄막 코루틴
    // ==========================================
    IEnumerator Pattern_StunShot()
    {
        isPatternPlaying = true;
        canMove = false;

        anim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.4f);

        for (int i = 0; i < bulletCount; i++)
        {
            Vector2 spawnPos =
                (Vector2)target.position +
                Random.insideUnitCircle * spawnRadius;

            Vector2 dir =
                ((Vector2)target.position - spawnPos).normalized;

            GameObject bullet =
                GameManager.instance.pool.GetBossBullet(stunBulletIndex);

            if (bullet == null)
                continue;

            bullet.transform.position = spawnPos;
            bullet.GetComponent<BossBullet>()?.Init(dir);
            bullet.SetActive(true);
            bullets.Add(bullet);

            yield return new WaitForSeconds(bulletDelay);
        }

        yield return new WaitForSeconds(0.4f);

        canMove = true;
        isPatternPlaying = false;
    }

    // ==========================================
    // 부하 몬스터 소환 로직
    // ==========================================
    void SummonMinions()
    {
        for (int i = 0; i < summonCount; i++)
        {
            GameObject enemy = GameManager.instance.pool.GetEnemy(summonEnemyIndex);

            if (enemy == null)
                continue;

            Vector2 offset = Random.insideUnitCircle * 2f;
            enemy.transform.position = (Vector2)transform.position + offset;
            enemy.SetActive(true);
        }
    }

    // ==========================================
    // 피격 이벤트 오버라이드 (페이즈 트리거)
    // ==========================================
    public override void TakeDamage(float damage)
    {
        if (invinciblePhase)
        {
            StartCoroutine(FlashInvincible(new Color(0.2f, 0.4f, 0.5f)));
            return;
        }

        base.TakeDamage(damage);

        if (!phaseTriggered && health <= maxHealth * 0.5f)
        {
            phaseTriggered = true;
            invinciblePhase = true;
            summonTimer = 0f;
            SummonMinions();
        }
    }

    // ==========================================
    // 소환수 사망 연동 — DeepSeaMinion이 직접 호출
    // ==========================================
    public void OnSummonDead()
    {
        if (!invinciblePhase)
            return;

        health -= maxHealth * 0.05f;

        if (health <= 0f)
        {
            Dead();
            return;
        }
    }

    // ==========================================
    // 사망 처리 오버라이드
    // ==========================================
    protected override void Dead()
    {
        ClearAll();
        base.Dead();
    }

    void ClearAll()
    {
        for (int i = 0; i < bullets.Count; i++)
        {
            if (bullets[i] != null)
                bullets[i].SetActive(false);
        }

        bullets.Clear();
    }
}