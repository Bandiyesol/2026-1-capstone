using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 공격 패턴: 재앙의 파동 범위 공격 (VoidWaveAOE)
/// 부모 클래스(BossBullet)를 상속받으며, 다른 탄막처럼 날아가지 않고 지정 위치에 고정되어
/// 플레이어에게 광역 피해(AOE), 넉백, 그리고 지속적인 화상(Burn) 상태이상을 부여하는 장판형 오브젝트입니다.
/// </summary>
public class VoidWaveAOE : BossBullet
{
    [Header("화상 상태이상 설정")]
    [Tooltip("플레이어가 장판에 피격되었을 때 적용될 화상 디버프의 총 유지 시간")]
    public float burnDuration = 5f;
    [Tooltip("화상 디버프의 매 주기(Tick)마다 플레이어가 입게 될 도트 데미지")]
    public float burnTickDamage = 4f;
    [Tooltip("화상 데미지가 들어가는 시간 주기 (예: 0.5초마다 틱 데미지 발생)")]
    public float burnTickInterval = 0.5f;
    [Tooltip("화상 상태일 때 플레이어 캐릭터 스프라이트가 빨갛게 깜빡이는 점멸 속도")]
    public float burnBlinkSpeed = 10f;

    [Header("넉백")]
    [Tooltip("피격 시 플레이어를 장판 바깥 방향으로 밀어내는 물리적인 힘의 크기")]
    public float knockbackForce = 12f;

    [Header("애니메이션 설정")]
    [Tooltip("재앙의 파동 이펙트 애니메이션 상태(State) 이름")]
    public string animationName = "VoidApostle_VoidWave";

    // --- 내부 컴포넌트 캐싱 및 상태 플래그 ---
    SpriteRenderer spriter; // 장판 이펙트의 가시성을 제어하기 위한 스프라이트 렌더러 참조
    Animator anim;          // 이펙트 애니메이션 재생 및 프레임 제어를 위한 애니메이터 참조
    bool canDamage;         // 중복 타격을 방지하고 실질적인 공격 판정 유효 여부를 판별하는 플래그

    void Awake()
    {
        // [컴포넌트 캐싱] 부모(BossBullet)에 선언된 물리/충돌 컴포넌트를 하위 클래스 초기화 시 캐싱
        col = GetComponent<Collider2D>();
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// 오브젝트 풀링 시스템에 의해 이 오브젝트가 필드에 활성화(인출)될 때마다 매번 호출되는 초기화 메서드
    /// </summary>
    protected override void OnEnable()
    {
        // 부모 클래스의 기본적인 타이머 및 파라미터 리셋 로직 수행
        base.OnEnable();

        // 직전에 실행 중이던 모든 잔여 코루틴을 완전히 청소하여 꼬임 방지
        StopAllCoroutines();
        canDamage = true; // 공격 가능 상태로 판정 활성화

        // 스프라이트 렌더러가 꺼져있다면 시각적 표시를 위해 다시 켜기
        if (spriter != null)
            spriter.enabled = true;

        // [물리 상태 완전 초기화] 투사체처럼 사방으로 날아가지 않고 생성된 제자리에 고정되도록 강제 정지 연산
        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero; // 선형 속도 제로화
            rigid.angularVelocity = 0f;          // 회전 속도 제로화
            rigid.Sleep();                       // 리지드바디를 잠시 휴면 상태로 전환하여 물리 연산 일시 중지
            rigid.WakeUp();                      // 다시 깨워 고정된 상태로 물리 엔진에 안착
        }

        // 생성 직후 1프레임 동안 의도치 않은 비정상적 충돌이 발생하는 것을 막기 위해 초기 충돌체 비활성화
        if (col != null)
            col.enabled = false;

        // [애니메이션 시스템 리셋] 오브젝트 풀 재사용 시 애니메이션이 멈추거나 꼬이는 유니티 고질적 버그 차단
        if (anim != null)
        {
            anim.Rebind();                        // 애니메이터의 내부 본딩 상태 및 파라미터를 완전 초기화
            anim.Update(0f);                      // 초기화된 스냅샷 상태를 내부 엔진에 즉시 강제 반영
            anim.Play(animationName, 0, 0f);      // 첫 번째 레이어(0)의 지정된 상태 이름을 0프레임(시작점)부터 강제 재생
        }

        // 안정적인 동기화를 위해 한 프레임 뒤에 충돌체를 가동하는 루틴 실행
        StartCoroutine(ActivateRoutine());

        // 애니메이션 이벤트 누락 등으로 장판이 영구히 필드에 남는 예외를 막기 위한 안전 수명 코루틴 실행
        StartCoroutine(LifeRoutine());
    }

    /// <summary>
    /// 부모(BossBullet)의 Update 내 이동 로직을 오버라이드하여 차단합니다.
    /// base.Update()를 호출하지 않으므로, 장판이 다른 탄막처럼 조준 방향으로 날아가지 않고 제자리에 고정됩니다.
    /// </summary>
    protected override void Update()
    {
        // 매 프레임 시간의 누적량을 타이머에 더함
        timer += Time.deltaTime;

        // 누적 타이머가 지정된 최대 수명(lifeTime)을 넘어서면 오브젝트 풀로 자가 반환(비활성화)
        if (timer >= lifeTime)
            gameObject.SetActive(false);
    }

    /// <summary>
    /// 오브젝트가 풀에 다시 반환(비활성화)될 때 안전하게 상태를 클리어하는 소멸자 성격의 메서드
    /// </summary>
    void OnDisable()
    {
        // 구동 중인 소멸 및 활성화 대기 코루틴 전면 중지
        StopAllCoroutines();

        canDamage = false; // 타격 판정 잠금

        // 충돌체 비활성화
        if (col != null)
            col.enabled = false;

        // 리지드바디 속도 리셋 및 안전 휴면 처리
        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
            rigid.angularVelocity = 0f;
            rigid.Sleep();
        }
    }

    /// <summary>
    /// [애니메이션 이벤트 콜백] 이펙트 애니메이션 클립의 가장 마지막 프레임에 심어놓은 이벤트를 통해 호출되며,
    /// 연출이 끝나는 타이밍에 맞춰 정밀하게 오브젝트를 풀에 반환합니다.
    /// </summary>
    public void OnAnimationEnd()
    {
        Debug.Log("AOE End Called");
        gameObject.SetActive(false); // 오브젝트 비활성화
    }

    /// <summary>
    /// 장판 생성 직후, 아주 미세한 물리 타이밍 차이로 충돌 판정이 씹히거나 왜곡되는 현상을 방지하는 동기화 코루틴
    /// </summary>
    IEnumerator ActivateRoutine()
    {
        // 유니티 물리 연산 주기(FixedUpdate)가 1회 완료될 때까지 안전하게 대기
        yield return new WaitForFixedUpdate();

        // 물리 프레임이 안착된 직후 실질적인 충돌 트리거/콜라이더 가동 시작
        if (col != null)
            col.enabled = true;
    }

    /// <summary>
    /// 애니메이션 타임라인 버그나 프레임 드랍 등으로 인해 OnAnimationEnd 이벤트가 누락되더라도
    /// 장판이 맵에 영구히 무한 상주하는 치명적인 버그를 원천 차단하는 백업용 수명 타이머 코루틴
    /// </summary>
    IEnumerator LifeRoutine()
    {
        // 설정된 최대 수명 시간만큼 타임 아웃 대기
        yield return new WaitForSeconds(lifeTime);

        // 시간이 지났음에도 애니메이션 이벤트에 의해 꺼지지 않고 활성화 상태라면 강제 풀 반환
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    /// <summary>
    /// 이 장판 패턴은 다단히트가 아닌 '단발성 타격'이므로, 플레이어와 한 번 접촉했다면 즉시 충돌 판정을 영구 해제하는 메서드
    /// </summary>
    public void DisablePlayerCollision()
    {
        canDamage = false; // 추가 타격 차단

        if (col != null)
            col.enabled = false; // 콜라이더를 꺼서 플레이어가 범위 안에 서있어도 연쇄 판정이 나지 않게 차단
    }

    /// <summary>
    /// 2D 물리 엔진에 의해 충돌체가 처음 접촉했을 때 실질적인 데미지 및 상태이상을 연산하는 콜백 메서드
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 이미 유저를 타격했거나 타격 불가 상태라면 연산 즉시 스킵
        if (!canDamage) return;

        // 충돌한 대상의 태그가 "Player"가 아니라면 예외 없이 리턴 차단
        if (!collision.gameObject.CompareTag("Player")) return;

        // 대상 오브젝트로부터 Player 메인 컨트롤러 컴포넌트 인출
        Player player = collision.gameObject.GetComponent<Player>();

        if (player != null)
        {
            // [넉백 방향 벡터 연산] 장판 중심 좌표(transform.position)에서 플레이어의 현재 위치를 빼서 
            // 장판 중심부로부터 바깥쪽으로 밀려나는 방사형 척력 방향을 구하고 정규화(.normalized)
            Vector2 dir =
                (collision.transform.position - transform.position).normalized;

            // 1. 플레이어에게 밀려나는 물리 효과(Knockback) 및 지속 도트 피해인 화상(Burn) 디버프 적용
            player.ApplyKnockback(dir, knockbackForce);
            player.ApplyBurn(burnDuration, burnTickDamage, burnTickInterval, burnBlinkSpeed);

            // 2. 글로벌 싱글톤 GameManager의 체력 프로퍼티를 직접 깎아 실질적인 데미지 반영
            if (GameManager.instance != null)
                GameManager.instance.Health -= damage;

            // 3. 단발성 타격 원칙에 따라, 접촉 프레임 직후 즉시 본체의 판정 기능을 잠금 처리하여 버그 방지
            DisablePlayerCollision();
        }
    }

    /// <summary>
    /// 플레이어가 장판 내부를 돌아다니다가 뒤늦게 판정에 걸리거나, 생성 시점에 이미 겹쳐있어 
    /// OnCollisionEnter2D가 누락되는 예외 케이스를 상시 구제하기 위한 물리 유지 보정 메서드
    /// </summary>
    void OnCollisionStay2D(Collision2D collision)
    {
        // 플레이어 태그 검증 통과 시 Enter 로직으로 우회 진입시켜 판정 신뢰성을 100% 확보
        if (collision.gameObject.CompareTag("Player"))
            OnCollisionEnter2D(collision);
    }
}