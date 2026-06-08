using System.Collections; // 코루틴 기능 사용을 위한 네임스페이스
using UnityEngine; // Unity 엔진 기본 API
using UnityEngine.InputSystem; // New Input System 기반 입력 처리

// 물리 및 입력 처리가 꼬이지 않도록 타 스크립트보다 무조건 먼저 실행되도록 설정
[DefaultExecutionOrder(-100)]
public class Player : MonoBehaviour
{
    [Header("이동")]
    public Vector2 inputVec;                                    // 현재 키보드/패드로 입력된 방향 벡터
    public Vector2 inputModifier = Vector2.one;                 // 특정 컨텐츠용 입력 보정치 (예: 디버프 시 입력 축소 등)
    public float inputJitter;                                   // 혼란 상태이상 시 입력 방향을 흔드는 무작위 강도

    [HideInInspector]
    public float speed;                                         // 타 스크립트 참조용 현재 속도 변수
    public float baseSpeed = 5f;                                // 캐릭터의 순수 기본 이동속도

    [Header("이속 배율")]
    public float moveSpeedMultiplier = 1f;                      // 버프/디버프 등으로 인한 이동속도 총 배율
    [HideInInspector]
    public float iceSlowMultiplier = 1f;                        // 빙결 상태이상 전용 감속 배율 (기본 1.0)
    [HideInInspector]
    public Vector2 externalVelocity;                            // 넉백, 외부 밀어내기 등 물리적으로 가해지는 강제 외력

    [Header("탐색/환경")]
    public Scaner scaner;                                       // 주변 적을 자동으로 탐색하는 센서 컴포넌트
    public LayerMask groundMask;                                // 특수 타일(용암 등)을 판정하기 위한 지면 레이어 마스크

    [Header("스프라이트")]
    public SpriteRenderer spriter;                              // 캐릭터 이미지를 그리는 렌더러 컴포넌트
    public Color defaultTint = Color.white;                     // 정상 상태일 때의 플레이어 기본 색상
    public Color freezeTint = new Color(0.72f, 0.9f, 1f, 1f);   // 빙결 상태일 때 변할 푸른색 틴트
    public Color burnTint = new Color(1f, 0.45f, 0.45f, 1f);    // 화상 상태일 때 변할 붉은색 틴트

    Color currentTint;                                          // 현재 스프라이트에 적용 중인 중간 색상 상태값
    Rigidbody2D rigid;                                          // 2D 물리 연산 및 이동을 담당하는 컴포넌트
    CapsuleCollider2D bodyCollider;                             // 충돌체 (적 접촉 피해용, Trigger 아님) — main 브랜치
    Animator anim;                                              // 애니메이션 상태 제어기 컴포넌트

    public bool isStunned;                                      // 기절 상태 플래그 (이동 및 행동 불가)
    bool isDead;                                                // 사망 완료 플래그 (중복 사망 처리 방지)
    bool isBurning;                                             // 현재 화상 디버프가 걸려있는지 여부
    bool isFrozen;                                              // 현재 빙결 디버프가 걸려있는지 여부

    Coroutine iceSlowRoutine;                                   // 실행 중인 빙결 코루틴 핸들 (중복 실행 제어용)
    Coroutine burnRoutine;                                      // 실행 중인 화상 코루틴 핸들 (중복 실행 제어용)

    public Vector2 lastTravelDirection = Vector2.right;         // 입력이 멈췄을 때 바라볼 마지막 이동 방향 기록 (기본값 우측)

    void Awake()
    {
        // 최적화를 위해 런타임 시작 시 주요 컴포넌트들을 미리 캐싱
        rigid = GetComponent<Rigidbody2D>();

        // main 브랜치: 적 접촉 피해(OnCollisionStay2D)가 동작하려면 Trigger가 꺼져 있어야 함
        bodyCollider = GetComponent<CapsuleCollider2D>();
        if (bodyCollider != null)
            bodyCollider.isTrigger = false;

        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        scaner = GetComponent<Scaner>();

        // 인스펙터 혹은 초기 설정된 스프라이트의 순수 기본 색상 보관
        defaultTint = spriter.color;
        currentTint = defaultTint;
    }

    void Start()
    {
        // 글로벌 스탯 관리 매니저가 존재한다면 스탯 변경 델리게이트 이벤트에 함수 바인딩
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += SyncBaseSpeed;
            SyncBaseSpeed(); // 첫 진입 시 초기 스탯 기반 속도 동기화
        }
    }

    void OnDestroy()
    {
        // 메모리 누수 방지를 위해 오브젝트 파괴 시 이벤트 구독 해제
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= SyncBaseSpeed;
    }

    /// <summary>
    /// 플레이어 스탯이 변경되었을 때 기본 이동 속도를 동기화해주는 함수
    /// </summary>
    void SyncBaseSpeed()
    {
        baseSpeed = 5f; // 필요 시 PlayerStats에서 가변적인 이속 값을 가져오도록 확장 가능
    }

    void Update()
    {
        // [매 프레임 체력 검사] 실시간 최신 스탯 시스템 HP 체크 -> 0 이하 시 사망 처리
        if (PlayerStats.Instance != null && PlayerStats.Instance.CurrentHP <= 0f)
            PlayerDead();

        // [구 버전 매니저 호환성 검사] 구형 게임 매니저 체력 체크 -> 0 이하 시 사망 처리
        else if (GameManager.instance != null && GameManager.instance.Health <= 0f)
            PlayerDead();
    }

    void FixedUpdate()
    {
        // 1. 기절 상태인 경우 이동 속도를 즉시 제로로 고정하고 물리 연산 스킵
        if (isStunned)
        {
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        // 2. 게임 세션이 멈춰있거나 라이브 상태가 아니라면 속도를 0으로 고정하고 동작 차단
        if (GameManager.instance == null || !GameManager.instance.isLive)
        {
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        // 3. 최신 인스턴스 스탯 속도에 각종 상태이상 배율(일반 배율 * 빙결 감속)을 누적 곱 연산
        float finalSpeed = PlayerStats.Instance.MovementSpeed * moveSpeedMultiplier * iceSlowMultiplier;

        // 4. 입력 벡터에 최종 계산된 이속을 곱해 순수 조작 속도(Velocity) 산출
        Vector2 inputVelocity = inputVec * finalSpeed;

        // 5. 아주 미세하게라도 움직이고 있다면 방향을 정규화(Normalized)하여 최신 이동 방향으로 저장
        if (inputVelocity.sqrMagnitude > 1e-10f)
            lastTravelDirection = inputVelocity.normalized;

        // 6. 리지드바디에 [조작 속도 + 외부에서 밀려난 넉백 힘]을 합산해 물리적 실제 이동 적용
        rigid.linearVelocity = inputVelocity + externalVelocity;

        // 7. 프레임 간 누적 현상을 방지하기 위해 단발성 외력(externalVelocity)은 적용 후 즉시 제로 초기화
        externalVelocity = Vector2.zero;
    }

    /// <summary>
    /// New Input System 메시지 브로드캐스팅 수신 (이동 키 입력 발생 시 자동 호출)
    /// </summary>
    void OnMove(InputValue value)
    {
        // main 브랜치: 게임이 멈춰있을 때 입력 자체를 차단
        if (GameManager.instance != null && !GameManager.instance.isLive)
        {
            inputVec = Vector2.zero;
            return;
        }

        // 입력 장치로부터 현재 2차원 입력 방향 패킷 획득
        Vector2 input = value.Get<Vector2>();

        // 축별 가중치 보정 (필요에 따라 x, y축 조작감 튜닝용)
        input = Vector2.Scale(input, inputModifier);

        // 혼란(Jitter) 디버프 수치가 존재할 경우 입력값에 무작위 구체 좌표계 오차를 더해 조작 방해
        if (inputJitter > 0f)
            input += Random.insideUnitCircle * inputJitter;

        // 대각선 이동 시 속도가 1.414배 빨라지는 것을 막기 위해 벡터 크기를 최대 1.0으로 제한(Clamp)
        inputVec = Vector2.ClampMagnitude(input, 1f);
    }

    void LateUpdate()
    {
        // 게임 진행 중이 아니라면 애니메이션 및 그래픽 업데이트 차단
        if (GameManager.instance == null || !GameManager.instance.isLive)
        {
            // main 브랜치: 정지 시 Speed 파라미터도 명시적으로 0으로 리셋
            if (anim != null)
                anim.SetFloat("Speed", 0f);
            return;
        }

        // 현재 조작 중인 입력 벡터의 순수 크기(0~1)를 애니메이터에 전달해 런/아이들 애니 분기
        anim.SetFloat("Speed", inputVec.magnitude);

        // 좌우 입력(x값)이 발생한 경우에만 렌더러 flipX 플래그를 조절해 이미지 좌우 반전 처리
        if (inputVec.x != 0f)
            spriter.flipX = inputVec.x < 0;
    }

    /// <summary>
    /// 외부 참조용: 현재 플레이어 리지드바디의 월드 좌표 반환
    /// </summary>
    public Vector2 GetWorldPosition()
        => rigid.position;

    /// <summary>
    /// 발사체 생성 및 무기 각도 계산용: 플레이어가 현재 현실적으로 향하고 있는 정밀 방향 벡터 산출
    /// </summary>
    public Vector2 GetFacingDirection()
    {
        const float velEps = 0.06f; // 움직임으로 인정할 물리적 최소 속도 한계치

        // 우선순위 1: 플레이어가 실제로 넉백이나 물리 힘에 의해 밀려나고 있다면 그 물리 방향 우선 반환
        if (rigid.linearVelocity.sqrMagnitude > velEps * velEps)
            return rigid.linearVelocity.normalized;

        // 우선순위 2: 멈춰있지만 조작 패드를 누르고 있다면 입력 패드 방향 반환
        if (inputVec.sqrMagnitude > 0.01f)
            return inputVec.normalized;

        // 우선순위 3: 조작도 물리 힘도 없다면 바로 직전에 완전 마지막으로 이동했던 방향 반환
        if (lastTravelDirection.sqrMagnitude > 1e-6f)
            return lastTravelDirection;

        // 우선순위 4: 모든 데이터가 유실된 극단적 상황 시 스프라이트가 뒤집힌 상태(좌/우)를 보고 정면 판정
        return spriter.flipX ? Vector2.left : Vector2.right;
    }

    /// <summary>
    /// 몬스터 몸체 콜라이더와 지속적으로 부딪히고 있을 때 (초당 도트 데미지 처리)
    /// </summary>
    void OnCollisionStay2D(Collision2D collision)
    {
        if (GameManager.instance == null || !GameManager.instance.isLive)
            return;

        float damage = 0f; // 부딪힌 대상으로부터 추출할 기초 공격력

        // 대상 유형 1: 일반 몬스터 컴포넌트 검사 후 공격력 추출
        Enemy enemy = collision.collider.GetComponent<Enemy>();
        if (enemy != null)
            damage = enemy.attackDamage;

        // 대상 유형 2: 보스형 몬스터 컴포넌트 검사 후 공격력 추출
        BossBase boss = collision.collider.GetComponent<BossBase>();
        if (boss != null)
            damage = boss.AttackDamage;

        // 공격력이 없거나 유효 타격이 아니면 탈출
        if (damage <= 0f)
            return;

        // 프레임에 독립적인 도트 데미지 연산을 위해 Time.deltaTime을 곱해 체력 감산
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.TakeDamage(damage * Time.deltaTime);
        else
            GameManager.instance.Health -= damage * Time.deltaTime;
    }

    /// <summary>
    /// 보스 발사체 등 트리거/충돌체 내부 진입 순간 처리 (단발성 즉시 타격 피해)
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 보스 탄막 태그를 가진 투사체와 충돌했는지 검사
        if (!collision.gameObject.CompareTag("BossBullet"))
            return;

        BossBullet bullet = collision.gameObject.GetComponent<BossBullet>();
        if (bullet != null)
        {
            // 연사/단발 타격이므로 델타타임 없이 투사체 고유 데미지 통째로 피해 적용
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.TakeDamage(bullet.damage);
            else
                GameManager.instance.Health -= bullet.damage;
        }

        // 충돌한 보스 탄막은 제 역할을 다했으므로 오브젝트 풀 반환(비활성화)
        collision.gameObject.SetActive(false);
    }

    /// <summary>
    /// 플레이어 사망 로직 파이프라인 개시 함수
    /// </summary>
    public void PlayerDead()
    {
        if (isDead)
            return;

        isDead = true; // 중복 호출 플래그 락킹

        // 인벤토리, 무기, 장착형 이펙트 오브젝트가 위치한 하위 자식(Child) 오브젝트들을 일괄 비활성화
        for (int i = 2; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);

        // 애니메이터에 Dead 트리거를 파이어하여 사망 연출 시퀀스 발동
        anim.SetTrigger("Dead");

        // 게임오버 UI 팝업 및 상태 동결을 위해 매니저에 연동 보고
        GameManager.instance.GameOver();
    }

    /// <summary>
    /// 외부 호출용 기절 상태이상 유발 시스템 함수
    /// </summary>
    public void Stun(float time)
    {
        // 기절 시 기존에 돌고 있던 상태이상 코루틴(화상, 빙결 등) 리스트를 초기화하여 교통정리
        StopAllCoroutines();

        // 입력받은 시간만큼 캐릭터를 얼려버릴 기절 코루틴 독점 구동
        StartCoroutine(StunRoutine(time));
    }

    /// <summary>
    /// 기절 타이머 및 상태 복구를 관리하는 코루틴
    /// </summary>
    IEnumerator StunRoutine(float time)
    {
        isStunned = true;
        yield return new WaitForSeconds(time); // 지정된 초만큼 물리 연산부 블로킹 대기
        isStunned = false;
    }

    /// <summary>
    /// 상태이상에 따른 플레이어 스프라이트 컬러 색상 변조 주입 함수
    /// </summary>
    public void SetStatusTint(Color tint)
    {
        currentTint = tint;
        spriter.color = tint;
    }

    /// <summary>
    /// 모든 상태이상이 해제되었을 때 순수 기본 색상으로 되돌리는 그래픽 복구 함수
    /// </summary>
    public void ResetStatusTint()
    {
        currentTint = defaultTint;
        spriter.color = defaultTint;
    }

    /// <summary>
    /// 레이캐스트 대신 OverlapCircle 구조를 사용하여 현재 딛고 있는 바닥 타일이 용암(Lava)인지 감지하는 함수
    /// </summary>
    public bool IsOnLava()
    {
        // 플레이어 중심부에 아주 작은 가상의 원(반지름 0.15)을 그려 groundMask 레이어에 속한 콜라이더들 서치
        Collider2D[] hits = Physics2D.OverlapCircleAll(rigid.position, 0.15f, groundMask);

        if (hits == null)
            return false;

        // 수집된 지면 오브젝트 중 "Lava" 태그를 가진 맵 타일이 존재하는지 루프 순회 검사
        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.CompareTag("Lava"))
                return true; // 용암 위에 서있음이 확인됨
        }

        return false;
    }

    /// <summary>
    /// 외부 유발용: 빙결 감속 디버프 적용 함수 (역상성인 화상 상태일 시 서로 상쇄됨)
    /// </summary>
    public void ApplyIceSlow(float slowMultiplier, float duration)
    {
        // 불타는 중(화상)에 얼음 공격을 받으면 감속이 걸리지 않고 서로 비겨서 화상만 치유됨
        if (isBurning)
        {
            ClearBurn();
            return;
        }

        // 이미 기존에 실행 중이던 빙결 감속 타이머가 있다면 중첩 방지를 위해 코루틴 인터셉트 종료
        if (iceSlowRoutine != null)
            StopCoroutine(iceSlowRoutine);

        // 신규 빙결 코루틴 등록 및 구동
        iceSlowRoutine = StartCoroutine(IceSlowRoutine(slowMultiplier, duration));
    }

    /// <summary>
    /// 빙결 틴트 적용 및 감속 배율 타이머 관리 코루틴
    /// </summary>
    IEnumerator IceSlowRoutine(float slowMultiplier, float duration)
    {
        isFrozen = true;
        SetStatusTint(freezeTint); // 외형을 차가운 푸른 빛으로 변경
        iceSlowMultiplier = slowMultiplier; // 디버프 속도 배율 주입 (예: 0.5f 시 이속 반토막)

        yield return new WaitForSeconds(duration); // 지속 시간 동안 프레임 홀딩

        // 디버프 종료에 따른 능력치 및 외형 원상복구 프로세스
        iceSlowMultiplier = 1f;
        isFrozen = false;
        ResetStatusTint();
        iceSlowRoutine = null;
    }

    /// <summary>
    /// 외부 유발용: 화상 도트 데미지 디버프 적용 함수 (역상성인 빙결 상태일 시 서로 상쇄됨)
    /// </summary>
    public void ApplyBurn(float duration, float tickDamage, float tickInterval, float blinkSpeed)
    {
        // 얼어있는 중(빙결)에 불 공격을 받으면 대미지 사이클 없이 서로 비겨서 빙결만 해제됨
        if (isFrozen)
        {
            ClearIceSlow();
            return;
        }

        // 이미 기존에 불타고 있던 화상 타이머 코루틴이 진행 중이라면 인터셉트 중단 처리
        if (burnRoutine != null)
            StopCoroutine(burnRoutine);

        // 신규 화상 지속 피해 코루틴 등록 및 구동
        burnRoutine = StartCoroutine(BurnRoutine(duration, tickDamage, tickInterval, blinkSpeed));
    }

    /// <summary>
    /// 화상 상태에서 주기적인 틱 데미지 가산 및 불꽃처럼 깜빡이는 연출을 처리하는 실시간 루틴
    /// </summary>
    IEnumerator BurnRoutine(float duration, float tickDamage, float tickInterval, float blinkSpeed)
    {
        isBurning = true;

        float burnTimer = duration;       // 전체 화상 유지 타이머
        float tickTimer = tickInterval;   // 도트 데미지가 들어갈 주기 타이머 (예: 0.5초마다)

        while (burnTimer > 0f)
        {
            burnTimer -= Time.deltaTime;
            tickTimer -= Time.deltaTime;

            // 틱 타이머가 만료되는 순간마다 플레이어의 실시간 헬스 수치를 직접 차감 및 타이머 복구
            if (tickTimer <= 0f)
            {
                GameManager.instance.Health -= tickDamage;
                tickTimer = tickInterval;
            }

            // [깜빡임 그래픽 연출] PingPong 함수를 활용해 시간을 기준으로 0.0 ~ 1.0 사이를 무한 왕복하는 알파 진폭값 계산
            float blink = Mathf.PingPong(Time.time * blinkSpeed, 1f);

            // 기본 틴트와 화상 붉은 틴트 사이를 진폭값(blink)에 따라 부드럽게 그라데이션 보간(Lerp)
            Color color = Color.Lerp(defaultTint, burnTint, blink);
            SetStatusTint(color);

            yield return null; // 1프레임 양보 후 다음 루프 재개
        }

        // 화상 시간이 완전 만료된 후 청소 및 상태 복구
        ResetStatusTint();
        isBurning = false;
        burnRoutine = null;
    }

    /// <summary>
    /// 상성 상쇄 및 특수 정화용: 가동 중인 화상 상태를 즉시 강제 종료시키는 완치 함수
    /// </summary>
    public void ClearBurn()
    {
        if (burnRoutine != null)
        {
            StopCoroutine(burnRoutine);
            burnRoutine = null;
        }
        isBurning = false;
        ResetStatusTint();
    }

    /// <summary>
    /// 상성 상쇄 및 특수 정화용: 가동 중인 빙결 감속 상태를 즉시 강제 종료시키는 완치 함수
    /// </summary>
    public void ClearIceSlow()
    {
        if (iceSlowRoutine != null)
        {
            StopCoroutine(iceSlowRoutine);
            iceSlowRoutine = null;
        }
        iceSlowMultiplier = 1f;
        isFrozen = false;
        ResetStatusTint();
    }

    /// <summary>
    /// 몬스터 피격, 트랩 조우 시 물리적으로 짧고 강하게 플레이어를 밀어내는 임펄스 넉백 함수
    /// </summary>
    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (rigid != null)
        {
            // 넉백 방향 왜곡을 막기 위해 현재 관성으로 흐르던 기존 속도를 즉각 순간 소거
            rigid.linearVelocity = Vector2.zero;

            // 특정 방향 및 힘의 세기대로 리지드바디에 순간 충격량(Impulse) 모드로 물리 힘 전달
            rigid.AddForce(direction * force, ForceMode2D.Impulse);
        }
    }

    /// <summary>
    /// 스테이지 클리어/사망 후 메인 타이틀로 복귀하거나 게임 세션을 완전히 언로드할 때 모든 원격 데이터를 공장 초기화하는 함수
    /// </summary>
    public void ResetForMainMenu()
    {
        // 1. 모든 상태이상 논리 플래그 리셋
        isDead = false;
        isStunned = false;
        isBurning = false;
        isFrozen = false;

        // 2. 조작 및 물리 관성 데이터 완전 정지
        inputVec = Vector2.zero;
        externalVelocity = Vector2.zero;

        // 3. 백그라운드에서 생존해 돌아가던 화상/빙결/기절 등 모든 타이머 코루틴 일제 강제 청소
        StopAllCoroutines();
        iceSlowRoutine = null;
        burnRoutine = null;

        // 4. 리지드바디 물리 속도를 영(Zero)으로 돌리고, 캐릭터 위치를 월드 중심점(0, 0, 0)으로 텔레포트
        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            transform.position = Vector3.zero;
        }

        // 5. 그래픽 렌더 색상 정화
        ResetStatusTint();
        speed = baseSpeed;

        // 6. 사망 시 숨겨놓았던 자식 오브젝트(무기, 장비 파츠 등)들을 루프 순회하여 일괄 재활성화(True)
        for (int i = 2; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(true);

        // 7. 사망 애니메이션 상태 플래그 및 파라미터 값 초기화 (main 브랜치: Rebind + Update로 즉시 상태 강제 리셋)
        if (anim != null)
        {
            anim.ResetTrigger("Dead");
            anim.Rebind();
            anim.Update(0f);
            anim.SetFloat("Speed", 0f);
        }
    }
}