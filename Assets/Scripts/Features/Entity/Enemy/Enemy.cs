using System.Collections;
using UnityEngine;

// IDamageable 인터페이스를 구현하여 데미지를 받을 수 있는 적 컴포넌트
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("데이터")]
    [SerializeField] EnemyData data; // 적의 기본 스탯을 담은 스크립터블 오브젝트

    [Header("기타")]
    public Rigidbody2D target; // 추적할 대상 (플레이어)
    public WaveManager waveManager; // 웨이브 관리를 위한 매니저 참조

    [Header("적 스텟(건드리지 말 것)")]
    public float health;       // 현재 체력
    public float maxHealth;    // 최대 체력
    public float attackDamage; // 공격력
    public float speed;        // 이동 속도

    // 상태 제어 변수들
    protected bool isLive;     // 현재 살아있는지 여부 (자식 클래스 접근 가능)
    public bool IsLive => isLive;  // 외부 접근용 프로퍼티
    bool isFrozen;             // 빙결(치명적 멈춤) 상태 여부
    bool hiddenInFog;          // 안개 속에 숨겨졌는지 여부
    float freezeTimer;         // 빙결 남은 시간 타이머
    bool isHitEffectRunning;   // 피격 깜빡임 코루틴 실행 중 여부

    // ── 상태이상 ──────────────────────────────────
    Coroutine burnRoutine;     // 화상 코루틴 핸들
    Coroutine poisonRoutine;   // 독 코루틴 핸들
    Coroutine bleedRoutine;    // 출혈 코루틴 핸들

    // 상태이상 색상 우선순위 (높을수록 우선)
    // 피격(임시) > 화상 > 출혈 > 독 > 빙결 > 기본
    bool isBurning;
    bool isPoisoning;
    bool isBleeding;

    // ── 중력장(블랙홀 등) ───────────────────────────
    Vector2? gravityPullCenter;
    float gravityPullForce;
    int gravityPullFrame = -1;

    // 컴포넌트 캐싱 변수들
    Rigidbody2D rigid;
    Collider2D coll;
    Animator anim;
    SpriteRenderer spriter;
    Color originColor;         // 피격 이펙트 후 되돌릴 원래 스프라이트 색상

    void Awake()
    {
        // 컴포넌트 최초 캐싱 및 기본 색상 저장
        rigid = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        spriter = GetComponent<SpriteRenderer>();
        originColor = spriter.color;
    }

    void OnEnable()
    {
        // 오브젝트 풀에서 활성화될 때 실행: 플레이어 타겟 자동 설정
        if (GameManager.instance != null && GameManager.instance.player != null)
            target = GameManager.instance.player.GetComponent<Rigidbody2D>();

        // 적 상태 및 컴포넌트 초기화(부활 세팅)
        isLive      = true;
        isFrozen    = false;

        // 상태이상 초기화
        burnRoutine   = null;
        poisonRoutine = null;
        bleedRoutine  = null;
        isBurning     = false;
        isPoisoning   = false;
        isBleeding    = false;
        coll.enabled = true;
        rigid.simulated = true;
        spriter.sortingOrder = 2; // 살아있을 때 레이어 순서 높임
        spriter.color = originColor;
        ApplyData(); // 데이터 로드 및 스텟 적용
    }

    void ApplyData()
    {
        if (data == null) return;

        // ScriptableObject(EnemyData)의 데이터를 실시간 스텟 변수에 대입
        speed = data.moveSpeed;
        maxHealth = data.maxHealth;
        health = maxHealth;
        attackDamage = data.attackDamage;
    }

    void FixedUpdate()
    {
        // 게임이 멈췄거나 적이 죽었다면 물리 연산 스킵
        if (!GameManager.instance.isLive || !isLive)
            return;

        // 빙결 상태 처리: 타이머 감소 및 물리 속도 제로화 후 리턴
        if (isFrozen)
        {
            freezeTimer -= Time.fixedDeltaTime;
            if (freezeTimer <= 0f)
            {
                isFrozen = false;
                RefreshStatusColor();
            }

            rigid.linearVelocity = Vector2.zero;
            return;
        }

        if (target == null) return;

        if (gravityPullFrame != Time.frameCount)
        {
            gravityPullCenter = null;
            gravityPullForce = 0f;
        }

        // 플레이어 방향으로 등속 이동 처리 및 관성(떨림) 방지를 위한 속도 제로화
        Vector2 dirVec = target.position - rigid.position;
        Vector2 nextVec = dirVec.normalized * speed * Time.fixedDeltaTime;

        if (gravityPullCenter.HasValue && gravityPullForce > 0f)
        {
            Vector2 pullDir = gravityPullCenter.Value - rigid.position;
            if (pullDir.sqrMagnitude > 0.0025f)
                nextVec += pullDir.normalized * gravityPullForce * Time.fixedDeltaTime;
        }

        rigid.MovePosition(rigid.position + nextVec);
        rigid.linearVelocity = Vector2.zero;
    }

    public void ApplyGravityPull(Vector2 center, float force)
    {
        gravityPullCenter = center;
        gravityPullForce = force;
        gravityPullFrame = Time.frameCount;
    }

    void LateUpdate()
    {
        // 게임 중지, 사망, 타겟 부재 시 렌더링 연산 스킵
        if (!GameManager.instance.isLive || !isLive || target == null)
            return;

        // 플레이어의 X축 위치에 따라 좌우 스프라이트 반전(Flip)
        spriter.flipX = target.position.x < rigid.position.x;

        // 안개 시스템 내부 로직: 플레이어와 멀어지면 투명하게(시야 제한) 변경
        if (hiddenInFog)
        {
            float dist = Vector2.Distance(rigid.position, target.position);
            float alpha = dist <= 2.2f ? 1f : 0.12f; // 일정 거리(2.2) 밖이면 반투명화
            Color c = spriter.color;
            c.a = alpha;
            spriter.color = c;
        }
    }

    // IDamageable 인터페이스 구현부: 외부(무기 등)에서 호출 시 데미지 적용
    public void TakeDamage(float damage)
    {
        if (!isLive || health <= 0f) return;

        health -= damage;

        // [악세사리 훅] 황금 손목 보호대·눈꽃 송이 — 적중 시 효과
        AccessoryEffect.instance?.NotifyEnemyHit(this);

        // 중복 코루틴 방지하면서 피격 빨간색 깜빡임 효과 실행
        if (!isHitEffectRunning)
            StartCoroutine(HitFlashEffect());

        if (health <= 0f)
            Die(); // 사망 처리
    }

    // 피격 시 0.1초 동안 빨갛게 변했다가 원래대로 돌아오는 코루틴
    // 상태이상 색상 우선순위에 따라 현재 색상 결정
    // 화상 > 출혈 > 독 > 빙결 > 기본
    void RefreshStatusColor()
    {
        if (isBurning)
            spriter.color = new Color(1f, 0.4f, 0f);       // 주황 (화상)
        else if (isBleeding)
            spriter.color = new Color(0.8f, 0f, 0f);        // 빨강 (출혈)
        else if (isPoisoning)
            spriter.color = new Color(0.2f, 0.8f, 0.2f);   // 초록 (독)
        else if (isFrozen)
            spriter.color = new Color(0.5f, 0.8f, 1f);     // 하늘 (빙결)
        else
            spriter.color = originColor;                    // 기본
    }

    IEnumerator HitFlashEffect()
    {
        isHitEffectRunning = true;
        spriter.color = Color.white;   // 피격 시 흰색 깜빡임
        yield return new WaitForSeconds(0.08f);
        RefreshStatusColor();          // 상태이상 색상으로 복구
        isHitEffectRunning = false;
    }

    // 외부에서 빙결 상태(디버프 등)를 부여할 때 호출하는 메서드
    /// <summary>이동속도 감소 (ratio: 0.2 = 20% 감소, duration: 지속 시간)</summary>
    public void ApplySlow(float ratio, float duration)
    {
        if (!isLive) return;
        StartCoroutine(SlowRoutine(ratio, duration));
    }

    IEnumerator SlowRoutine(float ratio, float duration)
    {
        speed *= (1f - ratio);
        yield return new WaitForSeconds(duration);
        speed /= (1f - ratio); // 원래 속도 복구
    }

    /// <summary>[악세사리] 투명 망토 — 어그로 해제</summary>
    public void ClearTarget()
    {
        target = null;
    }

    /// <summary>[악세사리] 투명 망토 — 어그로 복구</summary>
    public void RestoreTarget()
    {
        if (GameManager.instance?.player != null)
            target = GameManager.instance.player.GetComponent<Rigidbody2D>();
    }

    public void ApplyFreeze(float duration)
    {
        isFrozen = true;
        freezeTimer = Mathf.Max(freezeTimer, duration); // 더 긴 빙결 시간으로 갱신
    }

    /// <summary>
    /// 화상 적용 — 일정 시간 동안 틱 피해.
    /// 이미 화상 중이면 더 긴 시간으로 갱신.
    /// </summary>
    public void ApplyBurn(float damagePerTick, float tickInterval, float duration)
    {
        if (!isLive) return;

        // 기존 화상 코루틴 중단 후 새 파라미터로 재시작 (갱신)
        if (burnRoutine != null) StopCoroutine(burnRoutine);
        burnRoutine = StartCoroutine(BurnRoutine(damagePerTick, tickInterval, duration));
    }

    IEnumerator BurnRoutine(float damagePerTick, float tickInterval, float duration)
    {
        isBurning = true;
        RefreshStatusColor();

        float elapsed = 0f;
        while (elapsed < duration && isLive)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
            if (isLive) TakeDamage(damagePerTick);
        }

        isBurning = false;
        if (isLive) RefreshStatusColor();
        burnRoutine = null;
    }

    /// <summary>
    /// 독 적용 — 일정 시간 동안 틱 피해 (화상보다 약하지만 더 오래 지속).
    /// 스택은 없고 기존 독이 있으면 시간 갱신.
    /// </summary>
    public void ApplyPoison(float damagePerTick, float tickInterval, float duration)
    {
        if (!isLive) return;

        if (poisonRoutine != null) StopCoroutine(poisonRoutine);
        poisonRoutine = StartCoroutine(PoisonRoutine(damagePerTick, tickInterval, duration));
    }

    IEnumerator PoisonRoutine(float damagePerTick, float tickInterval, float duration)
    {
        isPoisoning = true;
        RefreshStatusColor();

        float elapsed = 0f;
        while (elapsed < duration && isLive)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
            if (isLive) TakeDamage(damagePerTick);
        }

        isPoisoning = false;
        if (isLive) RefreshStatusColor();
        poisonRoutine = null;
    }

    /// <summary>
    /// 출혈 적용 — 이동할수록 피해가 증가 (고정 피해 + 이동 보너스).
    /// </summary>
    public void ApplyBleed(float damagePerTick, float tickInterval, float duration)
    {
        if (!isLive) return;

        if (bleedRoutine != null) StopCoroutine(bleedRoutine);
        bleedRoutine = StartCoroutine(BleedRoutine(damagePerTick, tickInterval, duration));
    }

    IEnumerator BleedRoutine(float damagePerTick, float tickInterval, float duration)
    {
        isBleeding = true;
        RefreshStatusColor();

        float elapsed = 0f;
        Vector3 lastPos = transform.position;

        while (elapsed < duration && isLive)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;

            if (!isLive) break;

            // 이동 거리에 비례한 추가 피해
            float moved = Vector3.Distance(transform.position, lastPos);
            float finalDamage = damagePerTick + moved * 2f;
            lastPos = transform.position;

            TakeDamage(finalDamage);
        }

        isBleeding = false;
        if (isLive) RefreshStatusColor();
        bleedRoutine = null;
    }

    /// <summary>독 스택 전이 — 현재 독 상태인 적 주변에 독 전파 (PoisonSpread용).</summary>
    public bool IsPoisoned => isPoisoning;

    // 적 사망 처리 메서드
    protected virtual void Die()
    {
        BossBase.RecordEnemyDeath(transform.position);

        isLive = false;
        coll.enabled = false;       // 충돌체 비활성화 (시체 통과 가능)
        rigid.simulated = false;    // 물리 연산 중지
        spriter.sortingOrder = 1;   // 바닥 시체 연출을 위해 레이어 순서 낮춤

        if (GameManager.instance != null)
            GameManager.instance.Kill++; // 플레이어 총 킬 수 누적

        // 보상 드랍 시스템 연동: 코인 생성
        if (CoinDropManager.Instance != null)
            CoinDropManager.Instance.TryDropFromEnemy(transform.position);

        // 보상 드랍 시스템 연동: 상자 생성 (유니크 몬스터/보스는 더 좋은 상자)
        if (ChestDropManager.Instance != null)
        {
            if (data != null && data.isUnique)
                ChestDropManager.Instance.TryDropFromBoss(transform.position);
            else
                ChestDropManager.Instance.TryDropFromEnemy(transform.position);
        }

        // [악세사리 훅] 흡혈귀의 송곳니·미다스의 장갑 — 처치 시 효과
        AccessoryEffect.instance?.NotifyEnemyKilled();

        // [악세사리 훅] 흑마법의 인장 — 처치 시 주변 적 이동속도 감소
        AccessoryEffect.instance?.NotifyEnemyKilledWithPos(transform.position);

        // [악세사리 훅] 맹독성 확산기 — 독 상태인 적 처치 시 독 전이
        if (IsPoisoned)
            AccessoryEffect.instance?.NotifyPoisonedEnemyKilled(this);

        waveManager?.OnEnemyDead(); // 현재 웨이브 생존 적 숫자 차감 알림
        gameObject.SetActive(false); // 오브젝트 풀로 반환(비활성화)
    }

    // 즉사기 혹은 특수 기믹으로 적을 바로 처형할 때 사용
    public void KillInstantly()
    {
        if (!isLive) return;

        health = 0f;
        Die();
    }

    // 안개 트리거 진입 시 호출 (외부용)
    public void EnterFog()
    {
        hiddenInFog = true;
    }

    // 안개 영역을 벗어났을 때 호출하여 알파(투명도) 원상복구 (외부용)
    public void ExitFog()
    {
        hiddenInFog = false;
        Color c = spriter.color;
        c.a = 1f;
        spriter.color = c;
    }
}