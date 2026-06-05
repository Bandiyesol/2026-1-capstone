using System.Collections; // 코루틴 사용
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 입력 시스템 사용

// 다른 스크립트보다 먼저 실행
[DefaultExecutionOrder(-100)]
public class Player : MonoBehaviour
{
    [Header("이동")]
    public Vector2 inputVec; // 현재 입력 방향
    public Vector2 inputModifier = Vector2.one; // 입력 보정치
    public float inputJitter; // 혼란 시 입력 흔들림

    [HideInInspector]
    public float speed; // 외부 참조용 속도

    public float baseSpeed = 5f; // 기본 이동속도

    [Header("이속 배율")]
    public float moveSpeedMultiplier = 1f; // 상태이상 이속 배율

    [HideInInspector]
    public Vector2 externalVelocity; // 넉백 같은 외부 힘

    [Header("탐색/환경")]
    public Scaner scaner; // 적 탐색 컴포넌트
    public LayerMask groundMask; // 지면 감지용 레이어

    [Header("스프라이트")]
    public Color defaultTint = Color.white; // 기본 색상

    Color currentTint; // 현재 적용 색상

    Rigidbody2D rigid; // 물리 이동 담당
    public SpriteRenderer spriter; // 스프라이트 렌더러
    Animator anim; // 애니메이션 제어기

    public bool isStunned; // 기절 여부
    bool isDead; // 사망 여부

    Coroutine iceSlowRoutine; // 빙결 코루틴
    Coroutine burnRoutine; // 화상 코루틴

    public Vector2 lastTravelDirection = Vector2.right; // 마지막 이동 방향

    void Awake()
    {
        // 필수 컴포넌트 캐싱
        rigid = GetComponent<Rigidbody2D>();

        // 렌더러 캐싱
        spriter = GetComponent<SpriteRenderer>();

        // 애니메이터 캐싱
        anim = GetComponent<Animator>();

        // 탐색기 캐싱
        scaner = GetComponent<Scaner>();

        // 시작 색상 저장
        defaultTint = spriter.color;

        // 현재 색 초기화
        currentTint = defaultTint;
    }

    void Start()
    {
        // 스탯 시스템이 있으면
        if (PlayerStats.Instance != null)
        {
            // 스탯 변경 시 속도 동기화
            PlayerStats.Instance.OnStatsChanged += SyncBaseSpeed;

            // 최초 동기화
            SyncBaseSpeed();
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= SyncBaseSpeed;
    }

    void SyncBaseSpeed()
    {
        // 스탯 이동속도 반영
        baseSpeed = 5f;
    }

    void Update()
    {
        // 스탯 기반 체력 체크
        if (PlayerStats.Instance != null &&
            PlayerStats.Instance.CurrentHP <= 0f)
            PlayerDead();

        // 구 시스템 체력 체크
        else if (GameManager.instance != null &&
                 GameManager.instance.Health <= 0f)
            PlayerDead();
    }

    void FixedUpdate()
    {
        // 기절 시 이동 차단
        if (isStunned)
        {
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        // 게임 멈춤 상태면 종료
        if (GameManager.instance == null ||
            !GameManager.instance.isLive)
            return;

        // 최종 이동속도 계산
        float finalSpeed =
            PlayerStats.Instance.MovementSpeed *
            moveSpeedMultiplier;

        // 입력 기반 속도 생성
        Vector2 inputVelocity =
            inputVec * finalSpeed;

        // 이동 중이면 방향 저장
        if (inputVelocity.sqrMagnitude > 1e-10f)
            lastTravelDirection =
                inputVelocity.normalized;

        // 실제 이동 적용
        rigid.linearVelocity =
            inputVelocity + externalVelocity;

        // 외부 힘은 1프레임만 적용
        externalVelocity = Vector2.zero;
    }

    void OnMove(InputValue value)
    {
        // 입력 읽기
        Vector2 input = value.Get<Vector2>();

        // 방향별 보정
        input = Vector2.Scale(input, inputModifier);

        // 혼란 상태 랜덤 흔들림
        if (inputJitter > 0f)
            input += Random.insideUnitCircle * inputJitter;

        // 최대 크기 제한
        inputVec = Vector2.ClampMagnitude(input, 1f);
    }

    void LateUpdate()
    {
        // 게임 중 아닐 때 종료
        if (GameManager.instance == null ||
            !GameManager.instance.isLive)
            return;

        // 이동값을 애니메이션에 전달
        anim.SetFloat("Speed", inputVec.magnitude);

        // 좌우 반전 처리
        if (inputVec.x != 0f)
            spriter.flipX = inputVec.x < 0;
    }

    public Vector2 GetWorldPosition()
        => rigid.position; // 현재 월드 위치 반환

    public Vector2 GetFacingDirection()
    {
        const float velEps = 0.06f; // 최소 속도 기준

        // 실제 속도 우선
        if (rigid.linearVelocity.sqrMagnitude > velEps * velEps)
            return rigid.linearVelocity.normalized;

        // 입력 방향 차선
        if (inputVec.sqrMagnitude > 0.01f)
            return inputVec.normalized;

        // 마지막 이동 방향
        if (lastTravelDirection.sqrMagnitude > 1e-6f)
            return lastTravelDirection;

        // 기본 방향
        return spriter.flipX ? Vector2.left : Vector2.right;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // 게임 중 아닐 때 무시
        if (GameManager.instance == null ||
            !GameManager.instance.isLive)
            return;

        float damage = 0f; // 누적 피해

        // 일반 적 체크
        Enemy enemy =
            collision.collider.GetComponent<Enemy>();

        if (enemy != null)
            damage = enemy.attackDamage;

        // 보스 체크
        BossBase boss =
            collision.collider.GetComponent<BossBase>();

        if (boss != null)
            damage = boss.AttackDamage;

        // 피해 없으면 종료
        if (damage <= 0f)
            return;

        // 초당 피해 적용
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.TakeDamage(
                damage * Time.deltaTime
            );
        else
            GameManager.instance.Health -=
                damage * Time.deltaTime;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 보스 탄막만 처리
        if (!collision.gameObject.CompareTag("BossBullet"))
            return;

        BossBullet bullet =
            collision.gameObject.GetComponent<BossBullet>();

        if (bullet != null)
        {
            // 즉시 피해 적용
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.TakeDamage(bullet.damage);
            else
                GameManager.instance.Health -= bullet.damage;
        }

        // 탄 비활성화
        collision.gameObject.SetActive(false);
    }

    public void PlayerDead()
    {
        // 중복 실행 방지
        if (isDead)
            return;

        // 사망 상태 진입
        isDead = true;

        // 장착/이펙트 비활성화
        for (int i = 2; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

        // 사망 애니메이션
        anim.SetTrigger("Dead");

        // 게임오버 처리
        GameManager.instance.GameOver();
    }

    public void Stun(float time)
    {
        // 기존 상태이상 제거
        StopAllCoroutines();

        // 새 기절 시작
        StartCoroutine(StunRoutine(time));
    }

    IEnumerator StunRoutine(float time)
    {
        // 기절 시작
        isStunned = true;

        // 지정 시간 대기
        yield return new WaitForSeconds(time);

        // 기절 해제
        isStunned = false;
    }

    public void SetStatusTint(Color tint)
    {
        // 현재 색 저장
        currentTint = tint;

        // 렌더 적용
        spriter.color = tint;
    }

    public void ResetStatusTint()
    {
        // 기본 색 복구
        currentTint = defaultTint;

        // 렌더 복구
        spriter.color = defaultTint;
    }

    public bool IsOnLava()
    {
        // 주변 바닥 검사
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                rigid.position,
                0.15f,
                groundMask
            );

        // 검사 실패
        if (hits == null)
            return false;

        // 용암 탐색
        foreach (Collider2D hit in hits)
        {
            if (hit != null &&
                hit.CompareTag("Lava"))
                return true;
        }

        return false;
    }

    public void ApplyIceSlow(float slowMultiplier, float duration)
    {
        // 기존 빙결 갱신
        if (iceSlowRoutine != null)
            StopCoroutine(iceSlowRoutine);

        // 새 빙결 시작
        iceSlowRoutine =
            StartCoroutine(
                IceSlowRoutine(
                    slowMultiplier,
                    duration
                )
            );
    }

    IEnumerator IceSlowRoutine(float slowMultiplier, float duration)
    {
        // 감속 적용
        moveSpeedMultiplier = slowMultiplier;

        // 유지
        yield return new WaitForSeconds(duration);

        // 복구
        moveSpeedMultiplier = 1f;

        // 참조 해제
        iceSlowRoutine = null;
    }

    public void ApplyBurn(
        float duration,
        float tickDamage,
        float tickInterval,
        Color burnTint,
        float blinkSpeed)
    {
        // 기존 화상 제거
        if (burnRoutine != null)
            StopCoroutine(burnRoutine);

        // 새 화상 시작
        burnRoutine =
            StartCoroutine(
                BurnRoutine(
                    duration,
                    tickDamage,
                    tickInterval,
                    burnTint,
                    blinkSpeed
                )
            );
    }

    IEnumerator BurnRoutine(
        float duration,
        float tickDamage,
        float tickInterval,
        Color burnTint,
        float blinkSpeed)
    {
        float burnTimer = duration; // 남은 시간
        float tickTimer = tickInterval; // 틱 타이머

        while (burnTimer > 0f)
        {
            // 시간 감소
            burnTimer -= Time.deltaTime;

            // 틱 감소
            tickTimer -= Time.deltaTime;

            if (tickTimer <= 0f)
            {
                // 지속 피해
                GameManager.instance.Health -= tickDamage;

                // 틱 초기화
                tickTimer = tickInterval;
            }

            // 깜빡임 계산
            float blink =
                Mathf.PingPong(
                    Time.time * blinkSpeed,
                    1f
                );

            // 색상 보간
            Color color =
                Color.Lerp(
                    defaultTint,
                    burnTint,
                    blink
                );

            // 적용
            SetStatusTint(color);

            yield return null;
        }

        // 상태 복구
        ResetStatusTint();

        // 참조 해제
        burnRoutine = null;
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (rigid != null)
        {
            // 기존 이동 제거
            rigid.linearVelocity = Vector2.zero;

            // 즉시 밀쳐냄
            rigid.AddForce(
                direction * force,
                ForceMode2D.Impulse
            );
        }
    }

    public void ResetForMainMenu()
    {
        // 상태 초기화
        isDead = false;
        isStunned = false;

        // 입력 초기화
        inputVec = Vector2.zero;

        // 외력 제거
        externalVelocity = Vector2.zero;

        // 모든 코루틴 종료
        StopAllCoroutines();

        // 상태이상 참조 제거
        iceSlowRoutine = null;
        burnRoutine = null;

        if (rigid != null)
        {
            // 정지
            rigid.linearVelocity = Vector2.zero;

            // 원점 복귀
            transform.position = Vector3.zero;
        }

        // 색 복구
        ResetStatusTint();

        // 속도 초기화
        speed = baseSpeed;

        // 자식 다시 활성화
        for (int i = 2; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(true);

        if (anim != null)
        {
            // 애니 상태 초기화
            anim.ResetTrigger("Dead");

            // 정지 상태
            anim.SetFloat("Speed", 0f);
        }
    }
}