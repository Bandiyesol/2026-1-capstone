using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 분신 제어 클래스: 공허의 사도 분신 (VoidApostleController)
/// 부모 클래스(BossBase)를 상속받으며, 3가지 고유 행동 패턴(씨앗 유도탄, 파동 장판, 강림 돌진)을 수행합니다.
/// 특히 '재앙의 강림' 패턴은 조준선 연출, 초고속 돌진, 플레이어 관통, 벽 충돌 기절(그로기) 메커니즘을 가집니다.
/// </summary>
public class VoidApostleController : BossBase
{
    // 분신이 가질 수 있는 3가지 고유 행동 패턴 타입 정의
    public enum ApostleType { Seed, Wave, Advent }

    [Header("분신 설정")]
    [SerializeField] private ApostleType apostleType; // 현재 분신 오브젝트에 할당된 고유 행동 타입
    [Tooltip("재앙의 씨앗(Seed) 패턴: 오브젝트 풀에서 꺼내어 발사할 유도 탄환의 인덱스")]
    [SerializeField] private int bulletIndex = 2;

    [Header("재앙의 파동(Wave) 패턴 설정")]
    [Tooltip("재앙의 파동 장판 공격을 발동하기 위한 플레이어와의 최소 진입 사거리")]
    [SerializeField] private float waveAttackRange = 3.5f;
    [Tooltip("공격 직전 플레이어에게 폭발 범위를 시각적으로 보여줄 자식 오브젝트 경고원(Telegraph)")]
    [SerializeField] private GameObject warningCircle;
    [Tooltip("폭발이 일어나기 전 경고 장판을 노출하고 대기하는 선딜레이 시간")]
    [SerializeField] private float warningTime = 1.2f;
    [Tooltip("오브젝트 풀에서 소환할 화상 유발 전용 광역 폭발 프리패브(VoidWaveAOE) 인덱스")]
    [SerializeField] private int waveAOEIndex = 3;

    [Header("재앙의 강림(Advent) 대시 패턴 설정")]
    [Tooltip("돌진 전 조준선을 켜고 자리에 멈춰 대기하는 선딜레이 시간")]
    [SerializeField] private float dashWarningTime = 0.8f;
    [Tooltip("돌진 속도 배율 (분신의 기본 이동속도 * 배율)")]
    [SerializeField] private float dashSpeedMultiplier = 3.5f;
    [Tooltip("돌진 최대 지속 시간 (벽에 안 부딪혔을 때 대시를 강제 종료할 타이머)")]
    [SerializeField] private float maxDashDuration = 0.4f;
    [Tooltip("돌진 중 플레이어 충돌 시 플레이어를 밀어내는 넉백 힘의 세기")]
    [SerializeField] private float dashKnockbackForce = 12f;
    [Tooltip("벽 충돌 시 어지러움(그로기) 상태로 묶여있을 기절 지속 시간")]
    [SerializeField] private float wallStunDuration = 1.0f;
    [Tooltip("물리적 벽으로 판정하여 부딪힐 레이어 마스크 설정 (인스펙터 설정 필수)")]
    [SerializeField] private LayerMask wallLayer;

    // 현재 이 사도가 소환한 탄환 및 장판들을 추적하는 외부 공유 리스트 (보스 사망 시 일괄 제거용)
    private List<GameObject> spawnedBullets;
    // 현재 작동 중인 고유 AI 패턴 코루틴을 안전하게 제어하고 중지하기 위한 참조 변수
    private Coroutine patternCoroutine;

    // --- 돌진 패턴 제어용 내부 상태 머신 플래그 ---
    private bool isDashing = false;         // 현재 초고속 돌진 물리 주입 중인지 여부
    private bool isPatternStunned = false;  // 벽에 부딪혀 기절(그로기) 상태에 빠졌는지 여부
    private Vector2 currentDashDir = Vector2.zero; // 조준 단계에서 확정된 록온 돌진 방향 벡터

    /// <summary>
    /// 외부(메인 보스 등)에서 현재 분신의 사망 여부를 확인할 수 있는 읽기 전용 프로퍼티
    /// </summary>
    public bool IsDead => isDead;

    /// <summary>
    /// 오브젝트 풀 인출 시 사도의 모든 상태 플래그를 클리어하고, 메인 보스 기반 스탯 동기화 및 AI 코루틴을 시동하는 메서드
    /// </summary>
    public void Init(ApostleType type, BossData bossData, Transform targetTransform, List<GameObject> bulletList)
    {
        // 1. 기초 데이터 및 타겟 정렬 동기화
        apostleType = type;
        data = bossData;
        target = targetTransform;
        spawnedBullets = bulletList;

        // [오브젝트 풀링 안전장치] 재사용 시 이전 상태가 남아 버그를 일으키지 않도록 모든 플래그 완전 초기화
        isDead = false;
        canMove = true;
        isDashing = false;
        isPatternStunned = false;
        currentDashDir = Vector2.zero;

        // 2. [분신 스펙 밸런싱 연산] 메인 보스의 데이터를 기반으로 분신 비율에 맞게 스케일링
        if (data != null)
        {
            maxHealth = data.maxHealth * 0.2f;      // 체력은 메인 보스의 20% 수준으로 취약하게 설정
            health = maxHealth;                      // 현재 체력 만당 충전
            attackDamage = data.attackDamage * 0.5f; // 공격력은 메인 보스의 50% 반감 적용
            moveSpeed = data.moveSpeed * 1.1f;       // 기동성은 분신의 기습 효과를 위해 10% 상향 조정
            defense = data.damageReduction;          // 방어 계수(데미지 감쇄율)는 원본 동등 적용
        }

        // 컴포넌트 런타임 유효성 검사 및 캐싱 안정화
        if (rigid == null) rigid = GetComponent<Rigidbody2D>();
        if (spriter == null) spriter = GetComponent<SpriteRenderer>();
        if (col == null) col = GetComponent<Collider2D>();

        // 비활성화되어 있던 렌더러와 충돌체를 다시 켜서 필드 상에 시각/물리적 복구
        if (spriter != null) spriter.enabled = true;
        if (col != null) col.enabled = true;

        // 연출용 가시성 오브젝트들 초기 끔 처리
        if (warningCircle != null)
            warningCircle.SetActive(false);

        // 3. [상태 머신 리셋] 기존에 작동 중이던 분신 패턴 코루틴이 있다면 안전하게 중지 및 초기화
        if (patternCoroutine != null) StopCoroutine(patternCoroutine);

        // 4. [AI 패턴 분기 기동] 할당된 타입에 맞춰 각각 독립된 고유 루틴 실행 루프 진입
        switch (apostleType)
        {
            case ApostleType.Seed:
                patternCoroutine = StartCoroutine(Pattern_SeedOfCalamity()); // 원거리 유도탄 3연사 페이즈
                break;
            case ApostleType.Wave:
                patternCoroutine = StartCoroutine(Pattern_WaveOfCalamity()); // 근접 진입 후 경고 장판 폭발 페이즈
                break;
            case ApostleType.Advent:
                patternCoroutine = StartCoroutine(Pattern_AdventOfCalamity()); // 플레이어 록온 조준선 및 3연속 벽 충돌 대시 페이즈
                break;
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 게임 전반의 프레임 업데이트 및 비동기적 시각 연출을 처리하는 라이프 사이클 메서드
    /// </summary>
    protected override void Update()
    {
        if (GameManager.instance == null || !GameManager.instance.isLive) return;
        if (isDead || target == null) return;

        // [시선 고정 처리] 돌진 중이거나 벽 충돌 기절 중이 아닐 때만 플레이어 위치를 향해 좌우 스프라이트 플립 적용
        if (!isPatternStunned && spriter != null)
        {
            spriter.flipX = target.position.x < transform.position.x;
        }

        // [벽 충돌 그로기 시각 효과] 기절 상태일 때 수학적 주기 함수를 활용해 스프라이트를 초당 15회 주기로 빠르게 점멸(깜빡임) 연출
        if (isPatternStunned && spriter != null)
        {
            // 시간을 15배속하고 내림 처리한 정수가 짝수/홀수인지 여부로 내장 하드웨어 렌더러 플래그를 스위칭
            spriter.enabled = (Mathf.FloorToInt(Time.time * 15f) % 2 == 0);
        }
    }

    /// <summary>
    /// 유니티 물리 엔진 주기에 맞춰 대시 가속 및 통상 이동 속도를 피지컬 기반으로 강제 제어하는 메서드
    /// </summary>
    protected override void FixedUpdate()
    {
        if (GameManager.instance == null || !GameManager.instance.isLive) return;
        if (isDead || target == null) return;

        // [돌진 가속 주입] 대시 상태 모드일 경우 레이캐스트 및 리지드바디 충돌 신뢰성을 위해 velocity에 강제 초고속 물리 주입
        if (isDashing)
        {
            if (rigid != null)
                rigid.linearVelocity = currentDashDir * (moveSpeed * dashSpeedMultiplier);
            return; // 대시 중일 땐 아래의 일반 추적 연산을 스킵하고 탈출
        }

        // 일반 정지 명령 수신 상태이거나 기절 그로기 상태일 때는 관성 미끄러짐을 원천 차단하기 위해 물리 속도 제로 고정 브레이크
        if (!canMove || isPatternStunned)
        {
            if (rigid != null)
                rigid.linearVelocity = Vector2.zero;
            return;
        }

        FollowTarget(); // 그 외 평시 상태일 땐 플레이어를 일반 등속도로 상시 추적 이동
    }

    /// <summary>
    /// Rigidbody2D 물리 엔진을 사용하여 플레이어의 현재 위치 방향으로 등속 이동을 수행하는 메서드
    /// </summary>
    private void FollowTarget()
    {
        if (rigid == null) return;
        Vector2 dir = ((Vector2)target.position - rigid.position).normalized;
        rigid.MovePosition(rigid.position + dir * moveSpeed * Time.fixedDeltaTime);
    }

    // ============================================================
    // 고유 AI 패턴 코루틴 상태 머신 구현부
    // ============================================================

    /// <summary>
    /// [1번 고유 패턴: 재앙의 씨앗] 일정 시간 이동 후 제자리에 멈춰 서서 플레이어를 조준해 유도탄을 3연사하는 루틴
    /// </summary>
    private IEnumerator Pattern_SeedOfCalamity()
    {
        while (!isDead && target != null)
        {
            yield return new WaitForSeconds(Random.Range(3f, 5f)); // 3~5초간 자유 무빙 텀 대기
            if (isDead || target == null) yield break;

            canMove = false; // 사격 모드를 위해 추적 중지
            if (rigid != null) rigid.linearVelocity = Vector2.zero;

            for (int i = 0; i < 3; i++)
            {
                if (target == null || isDead) break;

                Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
                FireApostleBullet(dir); // 사출

                yield return new WaitForSeconds(0.2f); // 연사 텀 연출 간격
            }

            canMove = true; // 사격 완료 후 추적 복구
        }
    }

    /// <summary>
    /// [2번 고유 패턴: 재앙의 파동] 플레이어에게 근접 사거리 내로 다가가 경고 장판을 보여준 뒤 광역 화상 폭발을 일으키는 루틴
    /// </summary>
    private IEnumerator Pattern_WaveOfCalamity()
    {
        while (!isDead && target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);

            if (distance <= waveAttackRange)
            {
                canMove = false; // 장판 시전 고정을 위해 이동 정지
                if (rigid != null) rigid.linearVelocity = Vector2.zero;

                if (warningCircle != null) warningCircle.SetActive(true); // 시각 유도용 붉은 장판 활성화

                yield return new WaitForSeconds(warningTime); // 충전 선딜레이 대기
                if (isDead) yield break;

                if (warningCircle != null) warningCircle.SetActive(false); // 폭발 직전 가이드 오프

                // 풀링 매니저에서 화상 유발 전용 광역 프리패브(VoidWaveAOE) 배치 시동
                if (PoolManager.Instance != null)
                {
                    GameObject aoeBlast = PoolManager.Instance.GetBossBullet(waveAOEIndex);
                    if (aoeBlast != null)
                    {
                        aoeBlast.transform.position = transform.position; // 사도 위치에 팝업
                        if (spawnedBullets != null) spawnedBullets.Add(aoeBlast); // 소거 리스트 트래킹 등록
                    }
                }

                yield return new WaitForSeconds(0.7f); // 후경직 딜레이
                canMove = true;

                yield return new WaitForSeconds(3.0f); // 장판 패턴 자체 내부 쿨타임
            }

            yield return new WaitForSeconds(0.2f); // 사거리 외 대기 시 CPU 과부하 방지 숨고르기
        }
    }

    /// <summary>
    /// [3번 고유 패턴: 재앙의 강림] 플레이어 위치를 저격하는 사선 조준선을 그리고 3연속 대시를 감행하며, 
    /// 플레이어는 관통 밀쳐내고 벽에 박으면 스스로 기절하여 그로기 딜타임을 주는 인공지능 루틴
    /// </summary>
    private IEnumerator Pattern_AdventOfCalamity()
    {
        while (!isDead && target != null)
        {
            yield return new WaitForSeconds(5f); // 패턴 대접 타이밍 충전 5초 대기
            if (isDead || target == null) yield break;

            // [3연속 실행 콤보 루프 코어 가동] 규칙에 의거하여 3회 연속 꺾어가며 기습 대시 실행
            for (int i = 0; i < 3; i++)
            {
                if (isDead || target == null) yield break;

                // 다음 연쇄 대시 시작 전 물리 브레이크 유동성 및 제어 상태 청소
                canMove = false;
                isDashing = false;
                isPatternStunned = false;
                if (rigid != null) rigid.linearVelocity = Vector2.zero;

                // [규칙 1: 대시 전 조준 단계] 타겟의 현재 마지막 월드 포지션을 조준해 록온 방향 벡터 정규화
                Vector2 targetLastPos = target.position;
                currentDashDir = (targetLastPos - (Vector2)transform.position).normalized;

                // 조준하는 찰나의 순간 시선 스냅샷을 1회 강제 고정하여 돌진 준비 연출 극대화
                if (spriter != null)
                    spriter.flipX = targetLastPos.x < transform.position.x;

                // 기획된 선딜레이 대기 타임 동안 자리에 미동도 없이 기를 모으며 정지 대기
                float warningTimer = 0f;
                while (warningTimer < dashWarningTime)
                {
                    if (isDead || target == null) yield break;

                    warningTimer += Time.deltaTime;
                    yield return null;
                }

                // [규칙 2: 초고속 대시 주입 활성화]
                isDashing = true;
                float dashTimer = 0f;

                // 기획된 대시 최대 한계 도달 시간에 도달하거나, 중간에 벽을 받아 물리 플래그가 꺼지기 전까지 대시 물리 유지 루프
                while (dashTimer < maxDashDuration && isDashing)
                {
                    if (isDead) yield break;
                    dashTimer += Time.deltaTime; // 프레임 시간 누적
                    yield return null; // FixedUpdate에서 물리 속도를 제어하도록 프레임 양보 대기
                }

                // [대시 페이즈 1회 종료] 시간이 다했거나 벽 충돌 검증에 의해 빠져나왔으므로 대시 모드 강제 해제 및 감속 제동
                isDashing = false;
                if (rigid != null) rigid.linearVelocity = Vector2.zero;

                // [규칙 3: 벽 충돌 그로기 기절 연출 세그먼트] 물리 충돌부에서 기절 트리거가 작동되었다면 내부 연출 루프 진입
                if (isPatternStunned)
                {
                    // 기획자가 설정한 무방비 그로기 시간(wallStunDuration)만큼 멍청히 서서 대기 (Update에서 점멸 효과 작동)
                    yield return new WaitForSeconds(wallStunDuration);

                    isPatternStunned = false; // 기절 모드 완전 해제
                    if (spriter != null) spriter.enabled = true; // 점멸 연출 루프를 멈추고 렌더러가 꺼진 채 방치되지 않게 무조건 다시 켜기 복구
                }

                // 대시 콤보와 다음 연쇄 대시 콤보 사이에 한 호흡 쉴 수 있도록 주는 짧은 경직 시간(0.4초) 적용 후 다음 루프로 순환
                yield return new WaitForSeconds(0.4f);
            }

            // 3번의 연속 조준 대시 콤보 시퀀스가 모두 안전하게 끝났으므로 플레이어 기본 AI 추적 기동 복구 허용
            canMove = true;
        }
    }

    /// <summary>
    /// 분신의 위치를 총구 원점으로 삼아 지정된 방향으로 투사체를 사출하고 오브젝트 풀 및 잔여물 리스트에 등록하는 사격 처리 메서드
    /// </summary>
    private void FireApostleBullet(Vector2 direction)
    {
        if (PoolManager.Instance == null) return;

        GameObject bulletObj = PoolManager.Instance.GetBossBullet(bulletIndex);
        if (bulletObj == null) return;

        bulletObj.transform.position = transform.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bulletObj.transform.rotation = Quaternion.Euler(0, 0, angle);

        BossBullet bullet = bulletObj.GetComponent<BossBullet>();
        if (bullet != null) bullet.Init(direction);

        if (spawnedBullets != null)
            spawnedBullets.Add(bulletObj);
    }

    /// <summary>
    /// [규칙 2, 3: 실시간 2D 물리 엔진 충돌 감지 연산 콜백 메서드]
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 이미 사망했거나 현재 대시 가속 주입 중(isDashing = true)이 아니라면 충돌 연산 전면 차단
        if (isDead || !isDashing) return;

        // [규칙 2: 돌진 도중 플레이어와 충돌 처리]
        if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>();
            if (player != null)
            {
                // 분신의 하향 조정된 전용 데미지(attackDamage)를 싱글톤 게임매니저 체력 프로퍼티에 직접 차감 반영
                if (GameManager.instance != null)
                    GameManager.instance.Health -= attackDamage;

                // [방사형 넉백 벡터 연산] 사도 중심에서 플레이어 위치 방향 벡터를 추출하고 정규화하여 설정된 파워로 튕겨냄
                Vector2 knockDir = (collision.transform.position - transform.position).normalized;
                player.ApplyKnockback(knockDir, dashKnockbackForce);
            }
            // 서리 늑대 군주 기믹 규칙에 의거: 플레이어는 치여서 날아가 버리되, 분신의 폭주 돌진은 벽에 박을 때까지 멈추지 않고 연장 유지
            return;
        }

        // [규칙 3: 돌진 중 기획된 벽 레이어 마스크 비트 연산 검증 OR 타일맵 "Wall" 태그 검증]
        if (((1 << collision.gameObject.layer) & wallLayer) != 0 || collision.gameObject.CompareTag("Wall"))
        {
            isDashing = false;         // 폭주 돌진 구동 즉시 중단 플래그 강제 다운
            isPatternStunned = true;   // AI 행동 루틴에 그로기 기절 연출을 트리거하기 위해 플래그 온
            if (rigid != null)
                rigid.linearVelocity = Vector2.zero; // 벽에 처박혔으므로 즉시 물리 관성 속도 벡터 제로 브레이크
        }
    }

    /// <summary>
    /// 단단한 2D 물리 벽 콜라이더 표면을 스치거나 미끄러지며 비벼질 때, Enter 연산이 누락되는 예외 케이스를 방어하기 위한 서브 가드 트래킹
    /// </summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        // 대시 연산이 구동 중인데 비벼지고 있다면 즉시 Enter 처리부로 강제 우회 연동 유효성 100% 확보
        if (isDashing) OnCollisionEnter2D(collision);
    }

    /// <summary>
    /// 분신의 체력이 다하거나 스테이지 클리어 시 시스템을 안전하게 차단하고 리셋 상태로 풀링 대기조에 돌려보내는 메서드
    /// </summary>
    protected override void Dead()
    {
        if (isDead) return;
        isDead = true; // 사망 플래그 lock

        // 가동 중인 AI 패턴 상태 머신 전면 강제 정지
        if (patternCoroutine != null) StopCoroutine(patternCoroutine);

        // 연출용 활성 자식 및 선 장치들 일괄 마스킹 오프
        if (warningCircle != null) warningCircle.SetActive(false);

        // 패턴 내부 플래그 정돈
        isDashing = false;
        isPatternStunned = false;

        // 물리 연산 잠금 및 완전 정지
        if (rigid != null) rigid.linearVelocity = Vector2.zero;
        canMove = false;

        // [오브젝트 풀링 가시성 버그 안전핀] 기절 점멸 도중 스위칭 타임에 spriter.enabled가 하필 false인 찰나에 풀에 들어가면,
        // 나중에 풀에서 다시 인출될 때 사도가 투명 인간인 상태로 투명하게 기어나오는 유니티 고질적 풀링 버그가 생깁니다.
        // 이를 막기 위해 오브젝트를 완전히 끄기 직전, 렌더러 가시성을 확실히 true로 원상복구시킨 뒤 비활성화합니다.
        if (spriter != null) spriter.enabled = true;
        if (spriter != null) spriter.enabled = false; // 이미지 오프 숨김
        if (col != null) col.enabled = false;         // 물리 충돌 오프

        gameObject.SetActive(false); // 풀 대기실 복귀
    }
}