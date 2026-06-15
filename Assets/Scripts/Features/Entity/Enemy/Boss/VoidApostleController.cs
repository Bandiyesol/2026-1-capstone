using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공허의 사도(보스 분신) 제어 클래스
/// - 메인 보스의 능력치를 동기화 및 스케일링하여 성질이 다른 3가지 AI 패턴 구동
/// - 유도탄 발사(Seed), 범위 장판(Wave), 벽 충돌 기절 메커니즘을 가진 조준 돌진(Advent) 제어
/// </summary>
public class VoidApostleController : BossBase
{
    public enum ApostleType { Seed, Wave, Advent }

    [Header("분신 설정")]
    [SerializeField] private ApostleType apostleType;
    [Tooltip("재앙의 씨앗 패턴 유도탄 풀링 인덱스")]
    [SerializeField] private int bulletIndex = 2;

    [Header("재앙의 파동(Wave) 패턴 설정")]
    [SerializeField] private float waveAttackRange = 3.5f;   // 장판 패턴 진입 사거리
    [SerializeField] private GameObject warningCircle;       // 범위 예고 장판 오브젝트
    [SerializeField] private float warningTime = 1.2f;       // 폭발 전 대기 시간
    [SerializeField] private int waveAOEIndex = 3;           // 광역 폭발 프리패브 풀링 인덱스

    [Header("재앙의 강림(Advent) 대시 패턴 설정")]
    [SerializeField] private float dashWarningTime = 0.8f;   // 돌진 전 선딜레이 시간
    [SerializeField] private float dashSpeedMultiplier = 3.5f; // 돌진 속도 배율
    [SerializeField] private float maxDashDuration = 0.4f;   // 최대 돌진 시간 (벽 미충돌 시 제한)
    [SerializeField] private float dashKnockbackForce = 12f; // 플레이어 충돌 시 넉백 힘
    [SerializeField] private float wallStunDuration = 1.0f;  // 벽 충돌 시 기절 지속 시간
    [SerializeField] private LayerMask wallLayer;            // 벽 판정 레이어 마스크

    private List<GameObject> spawnedBullets; // 상위 보스 사망 시 일괄 제거를 위한 탄막 참조 리스트
    private Coroutine patternCoroutine;      // 현재 구동 중인 AI 패턴 코루틴 핸들

    private bool isDashing = false;          // 돌진 물리 주입 상태 플래그
    private bool isPatternStunned = false;   // 벽 충돌 기절 상태 플래그
    private Vector2 currentDashDir = Vector2.zero; // 확정된 록온 돌진 방향 벡터

    public bool IsDead => isDead; // 외부 참조용 사망 플래그 프로퍼티

    /// <summary>
    /// 풀에서 인출될 때 실행되는 초기화 메서드 (스탯 동기화 및 AI 시작)
    /// </summary>
    public void Init(ApostleType type, BossData bossData, Transform targetTransform, List<GameObject> bulletList)
    {
        apostleType = type;
        data = bossData;
        target = targetTransform;
        spawnedBullets = bulletList;

        // 재사용을 위한 상태 리셋
        isDead = false;
        canMove = true;
        isDashing = false;
        isPatternStunned = false;
        currentDashDir = Vector2.zero;

        // 원본 보스 스탯 기반 밸런싱 세팅 (체력 20%, 공격력 50%, 기동성 110%)
        if (data != null)
        {
            maxHealth = data.maxHealth * 0.2f;
            health = maxHealth;
            attackDamage = data.attackDamage * 0.5f;
            moveSpeed = data.moveSpeed * 1.1f;
            defense = data.damageReduction;
        }

        // 컴포넌트 캐싱 및 예외 방지 구조 안정화
        if (rigid == null) rigid = GetComponent<Rigidbody2D>();
        if (spriter == null) spriter = GetComponent<SpriteRenderer>();
        if (col == null) col = GetComponent<Collider2D>();

        if (spriter != null) spriter.enabled = true;
        if (col != null) col.enabled = true;
        if (warningCircle != null) warningCircle.SetActive(false);

        // 이전 구동 코루틴 안전 종료 후 신규 패턴 분기 가동
        if (patternCoroutine != null) StopCoroutine(patternCoroutine);

        switch (apostleType)
        {
            case ApostleType.Seed:
                patternCoroutine = StartCoroutine(Pattern_SeedOfCalamity());
                break;
            case ApostleType.Wave:
                patternCoroutine = StartCoroutine(Pattern_WaveOfCalamity());
                break;
            case ApostleType.Advent:
                patternCoroutine = StartCoroutine(Pattern_AdventOfCalamity());
                break;
        }

        gameObject.SetActive(true);
    }

    protected override void Update()
    {
        if (GameManager.instance == null || !GameManager.instance.isLive) return;
        if (isDead || target == null) return;

        // 대시 및 기절 상태가 아닐 때만 타겟 방향으로 좌우 스프라이트 반전
        if (!isPatternStunned && spriter != null)
        {
            spriter.flipX = target.position.x < transform.position.x;
        }

        // 기절 그로기 상태일 때 주기 함수를 사용하여 스프라이트 고속 점멸 연출
        if (isPatternStunned && spriter != null)
        {
            spriter.enabled = (Mathf.FloorToInt(Time.time * 15f) % 2 == 0);
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (GameManager.instance == null || !GameManager.instance.isLive) return;
        if (isDead || target == null) return;

        // 돌진 활성화 상태일 시 정해진 방향 벡터로 고속 물리 속도 강제 제어
        if (isDashing)
        {
            if (rigid != null)
                rigid.linearVelocity = currentDashDir * (moveSpeed * dashSpeedMultiplier);
            return;
        }

        // 이동 불가 혹은 기절 상태일 시 물리 관성 제로 고정 브레이크
        if (!canMove || isPatternStunned)
        {
            if (rigid != null)
                rigid.linearVelocity = Vector2.zero;
            return;
        }

        FollowTarget(); // 평시 상태 타겟 등속 추적
    }

    private void FollowTarget()
    {
        if (rigid == null) return;
        Vector2 dir = ((Vector2)target.position - rigid.position).normalized;
        rigid.MovePosition(rigid.position + dir * moveSpeed * Time.fixedDeltaTime);
    }

    // ============================================================
    // [패턴 1] 재앙의 씨앗 : 일정 거리 무빙 후 유도탄 3연사 루틴
    // ============================================================
    private IEnumerator Pattern_SeedOfCalamity()
    {
        while (!isDead && target != null)
        {
            yield return new WaitForSeconds(Random.Range(3f, 5f)); // 추적 무빙 주기 대기
            if (isDead || target == null) yield break;

            canMove = false; // 사격을 위한 제자리 고정
            if (rigid != null) rigid.linearVelocity = Vector2.zero;

            for (int i = 0; i < 3; i++)
            {
                if (target == null || isDead) break;

                Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
                FireApostleBullet(dir); // 타겟 조준 사출

                yield return new WaitForSeconds(0.2f); // 연사 간격 딜레이
            }

            canMove = true; // 사격 완수 후 추적 복구
        }
    }

    // ============================================================
    // [패턴 2] 재앙의 파동 : 사거리 진입 시 예고 범위 전출 후 광역 폭발
    // ============================================================
    private IEnumerator Pattern_WaveOfCalamity()
    {
        while (!isDead && target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);

            if (distance <= waveAttackRange) // 범위 진입 검증
            {
                canMove = false;
                if (rigid != null) rigid.linearVelocity = Vector2.zero;

                if (warningCircle != null) warningCircle.SetActive(true); // 경고 장판 활성화

                yield return new WaitForSeconds(warningTime); // 차징 대기
                if (isDead) yield break;

                if (warningCircle != null) warningCircle.SetActive(false);

                // 풀매니저를 통해 사도 중심점에 광역 폭발 장판 생성 및 관리 등록
                if (PoolManager.Instance != null)
                {
                    GameObject aoeBlast = PoolManager.Instance.GetBossBullet(waveAOEIndex);
                    if (aoeBlast != null)
                    {
                        aoeBlast.transform.position = transform.position;
                        if (spawnedBullets != null) spawnedBullets.Add(aoeBlast);
                    }
                }

                yield return new WaitForSeconds(0.7f); // 후경직 딜레이
                canMove = true;

                yield return new WaitForSeconds(3.0f); // 패턴 자체 내부 쿨타임
            }

            yield return new WaitForSeconds(0.2f); // 조건 미달 시 프레임 과부하 방지 숨고르기
        }
    }

    // ============================================================
    // [패턴 3] 재앙의 강림 : 3연속 플레이어 조준 타격 돌진 및 벽 충돌 기절 기믹
    // ============================================================
    private IEnumerator Pattern_AdventOfCalamity()
    {
        while (!isDead && target != null)
        {
            yield return new WaitForSeconds(5f); // 대접 패턴 구동 대기 주기
            if (isDead || target == null) yield break;

            // 3연속 콤보 대시 실행 루프
            for (int i = 0; i < 3; i++)
            {
                if (isDead || target == null) yield break;

                canMove = false;
                isDashing = false;
                isPatternStunned = false;
                if (rigid != null) rigid.linearVelocity = Vector2.zero;

                // [단계 1] 조준: 타겟 방향 록온 및 방향 정규화 후 시선 방향 고정
                Vector2 targetLastPos = target.position;
                currentDashDir = (targetLastPos - (Vector2)transform.position).normalized;

                if (spriter != null)
                    spriter.flipX = targetLastPos.x < transform.position.x;

                // 경고 선딜레이 프레임 타임라인 대기
                float warningTimer = 0f;
                while (warningTimer < dashWarningTime)
                {
                    if (isDead || target == null) yield break;
                    warningTimer += Time.deltaTime;
                    yield return null;
                }

                // [단계 2] 돌진: 물리 엔진 주입 활성화 및 시간제 제한 루프 구동
                isDashing = true;
                float dashTimer = 0f;

                while (dashTimer < maxDashDuration && isDashing)
                {
                    if (isDead) yield break;
                    dashTimer += Time.deltaTime;
                    yield return null;
                }

                // 1회 대시 프로세스 종료 및 속도 감속 브레이크
                isDashing = false;
                if (rigid != null) rigid.linearVelocity = Vector2.zero;

                // [단계 3] 그로기: 벽 충돌 판정 수신으로 플래그 변동 시 기절 루틴 진입
                if (isPatternStunned)
                {
                    yield return new WaitForSeconds(wallStunDuration); // 무방비 대기 효과

                    isPatternStunned = false;
                    if (spriter != null) spriter.enabled = true; // 점멸 버그 방지를 위한 렌더러 원상 복구
                }

                yield return new WaitForSeconds(0.4f); // 다음 연쇄 대시 간 연출 딜레이
            }

            canMove = true; // 3연격 시퀀스 종료 후 기본 AI 추적 복구
        }
    }

    private void FireApostleBullet(Vector2 direction)
    {
        if (PoolManager.Instance == null) return;

        GameObject bulletObj = PoolManager.Instance.GetBossBullet(bulletIndex);
        if (bulletObj == null) return;

        bulletObj.transform.position = transform.position;

        // 삼각함수 아크탄젠트를 활용하여 투사체 진행 방향 각도 회전 동기화
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bulletObj.transform.rotation = Quaternion.Euler(0, 0, angle);

        BossBullet bullet = bulletObj.GetComponent<BossBullet>();
        if (bullet != null) bullet.Init(direction);

        if (spawnedBullets != null)
            spawnedBullets.Add(bulletObj);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || !isDashing) return; // 사망 상태 혹은 돌진 폭주 중이 아닐 시 연산 차단

        // 플레이어 충돌 시 처리 (데미지 직접 감산 및 사방 방사형 기반 넉백 주입)
        if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null)
            {
                PlayerStats.ApplyDamage(attackDamage);

                Vector2 knockDir = (collision.transform.position - transform.position).normalized;
                player.ApplyKnockback(knockDir, dashKnockbackForce);
            }
            return; // 플레이어 관통 규칙: 플레이어 처박기 성공 시에도 돌진은 벽에 닿을 때까지 지속
        }

        // 비트마스크 레이어 OR 태그 매칭을 통한 물리 벽 충돌 검증
        if (((1 << collision.gameObject.layer) & wallLayer) != 0 || collision.gameObject.CompareTag("Wall"))
        {
            isDashing = false;         // 돌진 상태 종료
            isPatternStunned = true;   // 기절 그로기 모드 돌입 트리거
            if (rigid != null)
                rigid.linearVelocity = Vector2.zero; // 물리 관성 강제 소거
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // 타일 콜라이더 벽면 미끄러짐 비벼짐 현상으로 인한 충돌 엔터 유실 케이스 예방 가드 포인터
        if (isDashing) OnCollisionEnter2D(collision);
    }

    protected override void Dead()
    {
        if (isDead) return;
        isDead = true;

        if (patternCoroutine != null) StopCoroutine(patternCoroutine);
        if (warningCircle != null) warningCircle.SetActive(false);

        isDashing = false;
        isPatternStunned = false;

        if (rigid != null) rigid.linearVelocity = Vector2.zero;
        canMove = false;

        // [풀링 안정화] 점멸 연출 도중 비활성화되어 리사이클 인출 시 투명 인간이 되는 엔진 유실 버그 가드 처리
        if (spriter != null) spriter.enabled = true;
        if (spriter != null) spriter.enabled = false;
        if (col != null) col.enabled = false;

        gameObject.SetActive(false); // 오브젝트 풀 반환
    }
}