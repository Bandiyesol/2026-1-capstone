using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class Player : MonoBehaviour
{
    [Header("이동 관련")]
    public Vector2 inputVec; // 키보드 및 패드 이동 입력 벡터
    public Vector2 inputModifier = Vector2.one; // 특정 축 입력 감도 보정용 가중치
    public float inputJitter; // 혼란 상태 이상 등을 표현하기 위한 입력 노이즈 값

    [HideInInspector] public float speed; // 외부 스탯 스크립트와의 실시간 동기화용 변수

    [Header("속도 배율")]
    public float moveSpeedMultiplier = 1f; // 슬로우 상태 이상 등에 사용하는 이동속도 배율

    [HideInInspector] public Vector2 externalVelocity; // 넉백이나 인력 등 외부 환경에 의해 받는 물리 힘

    [Header("탐색/환경")]
    public Scaner scaner; // 주변 적들을 탐색하는 스캐너 컴포넌트 참조
    public LayerMask groundMask; // 바닥 타일 기믹 감지용 레이어마스크

    [Header("스프라이트 틴트")]
    public Color defaultTint = Color.white; // 플레이어의 원래 기본 스프라이트 색상

    Color currentTint; // 현재 상태 이상에 의해 변경된 스프라이트 색상

    Rigidbody2D rigid; // 물리 연산을 처리하는 리지드바디 컴포넌트
    public SpriteRenderer spriter; // 이미지 출력을 담당하는 스프라이트 렌더러
    Animator anim; // 애니메이션 제어를 위한 애니메이터 컴포넌트

    bool isStunned; // 현재 기절(CC기) 상태인지 판별하는 플래그
    bool isDead; // 현재 사망 상태인지 판별하는 플래그

    Coroutine iceSlowRoutine; // 빙결 슬로우 상태 이상을 제어하는 코루틴 핸들

    public Vector2 lastTravelDirection = Vector2.right; // 움직임이 멈췄을 때 사용할 직전 이동 방향 기억용 변수

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>(); // 리지드바디 컴포넌트 자동 캐싱
        spriter = GetComponent<SpriteRenderer>(); // 스프라이트 렌더러 컴포넌트 자동 캐싱
        anim = GetComponent<Animator>(); // 애니메이터 컴포넌트 자동 캐싱
        scaner = GetComponent<Scaner>(); // 적 탐색 스캐너 컴포넌트 자동 캐싱

        defaultTint = spriter.color; // 상태 이상 해제 시 복구할 원본 색상 저장
        currentTint = defaultTint; // 현재 색상을 기본 색상으로 초기화
    }

    void Update()
    {
        if (PlayerStats.Instance != null && PlayerStats.Instance.CurrentHP <= 0f)
            PlayerDead(); // 독립 스탯 시스템 기준으로 체력이 다 달면 사망 처리
        else if (GameManager.instance != null && GameManager.instance.Health <= 0f)
            PlayerDead(); // 통합 게임 매니저 기준으로 체력이 다 달면 사망 처리
    }

    void FixedUpdate()
    {
        if (isStunned)
        {
            rigid.linearVelocity = Vector2.zero; // 기절 상태일 때는 조작 및 물리 이동을 즉시 완전 차단
            return;
        }

        if (GameManager.instance == null || !GameManager.instance.isLive)
            return; // 게임이 정지 상태이거나 라이브 상태가 아니면 이동 물리 연산 중단

        float finalSpeed = PlayerStats.Instance.MovementSpeed * moveSpeedMultiplier; // 기본 이속에 디버프 배율을 곱해 최종 속도 계산

        Vector2 inputVelocity = inputVec * finalSpeed; // 계산된 최종 속도를 조작 입력 방향 벡터에 대입

        if (inputVelocity.sqrMagnitude > 1e-10f)
            lastTravelDirection = inputVelocity.normalized; // 미세한 떨림이 아닌 유효한 이동 시 최신 방향 갱신

        rigid.linearVelocity = inputVelocity + externalVelocity; // 조작 속도와 외부 환경 힘(넉백 등)을 최종 합성하여 속도 적용

        externalVelocity = Vector2.zero; // 단발성 외력 충격량을 연산 직후 매 프레임 초기화
    }

    void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>(); // 인풋 시스템으로부터 실시간 좌우/상하 입력값 추출

        input = Vector2.Scale(input, inputModifier); // 설정된 보정치를 곱해 특정 축의 감도 조절

        if (inputJitter > 0f)
            input += Random.insideUnitCircle * inputJitter; // 노이즈 값이 활성화되어 있으면 무작위 입력 방해 추가

        inputVec = Vector2.ClampMagnitude(input, 1f); // 대각선 이동 시 이동속도가 빨라지는 현상을 막기 위해 크기 제한
    }

    void LateUpdate()
    {
        if (GameManager.instance == null || !GameManager.instance.isLive)
            return; // 게임 정지 상태면 연출 및 애니메이션 갱신 중단

        anim.SetFloat("Speed", inputVec.magnitude); // 입력 강도 크기를 전달하여 정지 및 달리기 애니메이션 블렌딩

        if (inputVec.x != 0f)
            spriter.flipX = inputVec.x < 0; // 왼쪽 입력 시 스프라이트 이미지를 좌우 반전
    }

    public Vector2 GetWorldPosition() => rigid.position; // 물리 엔진 기준 플레이어의 정확한 월드 위치 반환

    public Vector2 GetFacingDirection()
    {
        const float velEps = 0.06f; // 움직임 여부를 판별하기 위한 임계값 오차 범위

        if (rigid.linearVelocity.sqrMagnitude > velEps * velEps)
            return rigid.linearVelocity.normalized; // 1순위: 현재 물리 속도가 존재하면 이동 중인 물리 방향 반환

        if (inputVec.sqrMagnitude > 0.01f)
            return inputVec.normalized; // 2순위: 속도가 없어도 키보드를 누르고 있다면 입력 방향 반환

        if (lastTravelDirection.sqrMagnitude > 1e-6f)
            return lastTravelDirection; // 3순위: 완전히 멈췄다면 직전까지 이동하던 최신 유효 방향 반환

        return spriter.flipX ? Vector2.left : Vector2.right; // 예외: 데이터가 없으면 이미지의 반전 상태를 기준으로 방향 반환
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (GameManager.instance == null || !GameManager.instance.isLive)
            return; // 게임 정지 중에는 접촉 데미지 연산 패스

        float damage = 0f; // 이번 프레임에 입힐 데미지 초기화

        Enemy enemy = collision.collider.GetComponent<Enemy>(); // 부딪힌 물체에서 일반 몬스터 컴포넌트 추출
        if (enemy != null)
            damage = enemy.attackDamage; // 일반 몬스터의 접촉 피해량 할당

        BossBase boss = collision.collider.GetComponent<BossBase>(); // 부딪힌 물체에서 보스 몬스터 컴포넌트 추출
        if (boss != null)
            damage = boss.AttackDamage; // 보스 몬스터의 접촉 피해량 할당

        if (damage <= 0f)
            return; // 피해량이 없으면 연산 종료

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.TakeDamage(damage * Time.deltaTime); // 프레임당 지속 피해를 방어/회피 스탯 공식으로 전달
        else
            GameManager.instance.Health -= damage * Time.deltaTime; // 스탯 시스템이 없으면 게임매니저 체력을 직접 삭감
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("BossBullet"))
            return; // 일반 보스 투사체(BossBullet 태그)가 아니면 연산 건너뜀

        BossBullet bullet = collision.gameObject.GetComponent<BossBullet>(); // 부딪힌 투사체 스크립트 컴포넌트 획득
        if (bullet != null)
        {
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.TakeDamage(bullet.damage); // 단발성 투사체 데미지를 스탯 시스템 공식으로 연동
            else
                GameManager.instance.Health -= bullet.damage; // 스탯 시스템이 없으면 다이렉트로 체력 차감
        }

        collision.gameObject.SetActive(false); // 부딪힌 투사체 오브젝트를 풀로 반환 및 비활성화
    }

    public void PlayerDead()
    {
        if (isDead)
            return; // 중복 사망 방지용 예외 처리

        isDead = true; // 사망 플래그 ON

        for (int index = 2; index < transform.childCount; index++)
            transform.GetChild(index).gameObject.SetActive(false); // 사망 시 하위 장착 무기 및 이펙트 자식들을 일괄 비활성화

        anim.SetTrigger("Dead"); // 사망 애니메이션 작동 트리거 발동
        GameManager.instance.GameOver(); // 게임매니저에 게임오버 UI 호출 전달
    }

    public void Stun(float time)
    {
        StopAllCoroutines(); // 새로운 상태 이상 갱신을 위해 기존 연출 코루틴 전체 강제 종료
        StartCoroutine(StunRoutine(time)); // 지정된 시간만큼 기절 코루틴 작동
    }

    IEnumerator StunRoutine(float time)
    {
        isStunned = true; // FixedUpdate 물리 이동 차단 락 작동
        yield return new WaitForSeconds(time); // 기절 시간만큼 대기
        isStunned = false; // 물리 조작 권한 원복
    }

    public void SetStatusTint(Color tint)
    {
        currentTint = tint; // 현재 변조될 컬러 저장
        spriter.color = currentTint; // 스프라이트 렌더러에 해당 컬러 즉시 적용 (예: 독, 빙결 색상)
    }

    public void ResetStatusTint()
    {
        currentTint = defaultTint; // 상태 변조 컬러를 원본 색상으로 복구
        spriter.color = defaultTint; // 원래 스프라이트 렌더러 색상으로 환원
    }

    public bool IsOnLava()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(rigid.position, 0.15f, groundMask); // 발밑 일정 반경 안의 기믹 바닥 확인

        if (hits == null)
            return false; // 충돌체가 전혀 없으면 거짓 반환

        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.CompareTag("Lava"))
                return true; // 충돌체 중 하나라도 용암(Lava 태그) 타일이면 참 반환
        }

        return false; // 안전지대 바닥이면 거짓 반환
    }

    public void ApplyIceSlow(float slowMultiplier, float duration)
    {
        if (iceSlowRoutine != null)
            StopCoroutine(iceSlowRoutine); // 이미 슬로우 코루틴이 도는 중이면 중복 방지를 위해 강제 종료

        iceSlowRoutine = StartCoroutine(IceSlowRoutine(slowMultiplier, duration)); // 새로운 빙결 감속 코루틴 시작
    }

    IEnumerator IceSlowRoutine(float slowMultiplier, float duration)
    {
        moveSpeedMultiplier = slowMultiplier; // 이동속도 배율을 디버프 값으로 감속
        yield return new WaitForSeconds(duration); // 디버프 지속 시간만큼 대기
        moveSpeedMultiplier = 1f; // 원래 정상 이동속도 배율로 복구
        iceSlowRoutine = null; // 코루틴 참조 변수 초기화
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        Rigidbody2D rigid = GetComponent<Rigidbody2D>(); // 리지드바디 컴포넌트 실시간 재확인 및 캐싱

        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero; // 넉백 순간 정확한 거리를 밀기 위해 기존 플레이어 조작 관성 제거
            rigid.AddForce(direction * force, ForceMode2D.Impulse); // 즉시 적용되는 임펄스(Impulse) 모드로 타격 반대 방향 넉백 적용
        }
    }
}