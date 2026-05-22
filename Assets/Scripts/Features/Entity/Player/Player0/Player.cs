using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class Player : MonoBehaviour
{
    [Header("이동 관련")]
    // 입력 시스템에서 받은 이동 입력값
    public Vector2 inputVec;
    // 실제 입력 보정용
    public Vector2 inputModifier = Vector2.one;
    public float inputJitter;

    [Header("물리작용 관련")]
    // 이동 속도
    public float speed = 5f;
    // 추가
    public float baseSpeed = 5f;
    // 외부 힘(블랙홀 등)
    [HideInInspector]
    public Vector2 externalVelocity;

    [Header("탐색 관련")]
    // 근처 적 탐색 컴포넌트
    public Scaner scaner;
    // 환경 체크
    public LayerMask groundMask;

    [Header("스프라이트 색 관련")]
    // 상태 시각 효과
    public Color defaultTint = Color.white;
    // 현재 적용 중인 색
    Color currentTint;

    [Header("스프라이트 관련")]
    // 물리 이동 처리용 리지드바디
    Rigidbody2D rigid;
    // 좌우 반전 제어용 스프라이트 렌더러
    public SpriteRenderer spriter;

    // 이동/사망 애니메이션 제어
    Animator anim;
    // 플레이어 기절
    bool isStunned;
    // 플레이어 사망
    bool isDead;
    // 플레이어 타일 태그 검사 함수
    bool lavaTintApplied;

    // 입력이 멈춰도 바라보는 방향을 유지하기 위한 마지막 이동 방향
    Vector2 lastTravelDirection = Vector2.right;

    void Awake()
    {
        // 자주 사용하는 컴포넌트 캐싱
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        scaner = GetComponent<Scaner>();

        // 기본 색 저장
        defaultTint = spriter.color;
        currentTint = defaultTint;

        // 처음 이동속도 저장
        speed = baseSpeed;
    }

    void Update()
    {
        // 사망
        if (GameManager.instance.Health <= 0)
        {
            PlayerDead();
        }
    }

    void FixedUpdate()
    {
        if (isStunned)
        {
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        // 게임 정지 시 중단
        if (!GameManager.instance.isLive)
            return;

        // 플레이어 입력 이동
        Vector2 inputVelocity = inputVec * speed;

        // 마지막 방향 저장
        if (inputVelocity.sqrMagnitude > 1e-10f)
            lastTravelDirection = inputVelocity.normalized;

        // 플레이어 이동 + 외부 힘 합산
        rigid.linearVelocity = inputVelocity + externalVelocity;

        // 외부 힘 초기화
        externalVelocity = Vector2.zero;
    }

    void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();

        // 방향 반전
        input = Vector2.Scale(input, inputModifier);

        // 입력 흔들림
        if (inputJitter > 0f)
            input += Random.insideUnitCircle * inputJitter;

        inputVec = Vector2.ClampMagnitude(input, 1f);

        Debug.Log(speed);
    }

    void LateUpdate()
    {
        // 게임 중이 아닐 때는 애니메이션/방향 갱신 중단
        if (!GameManager.instance.isLive)
            return;

        // 이동량을 애니메이션 파라미터로 전달
        anim.SetFloat("Speed", inputVec.magnitude);

        // 좌/우 입력이 있을 때만 캐릭터 좌우 반전
        if (inputVec.x != 0)
        {
            spriter.flipX = inputVec.x < 0;
        }
    }

    // 현재 플레이어의 월드 좌표 반환
    public Vector2 GetWorldPosition() => rigid.position;

    /// <summary>재배치 등: 실제 이동 속도 → 입력 → 마지막 이동 방향 → 스프라이트 좌우.</summary>
    public Vector2 GetFacingDirection()
    {
        // 가장 신뢰도 높은 정보부터 순서대로 바라보는 방향 결정
        const float velEps = 0.06f;
        if (rigid.linearVelocity.sqrMagnitude > velEps * velEps)
            return rigid.linearVelocity.normalized;

        if (inputVec.sqrMagnitude > 0.01f)
            return inputVec.normalized;

        if (lastTravelDirection.sqrMagnitude > 1e-6f)
            return lastTravelDirection;

        return spriter.flipX ? Vector2.left : Vector2.right;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // 게임 중 아닐 때
        if (!GameManager.instance.isLive)
            return;

        float damage = 0f;

        // 일반 적 체크
        Enemy enemy =
            collision.collider.GetComponent<Enemy>();

        if (enemy != null)
        {
            damage = enemy.attackDamage;
        }

        // 보스 체크
        BossBase boss =
            collision.collider.GetComponent<BossBase>();

        if (boss != null)
        {
            damage = boss.AttackDamage;
        }

        // 둘 다 아니면 무시
        if (damage <= 0f)
            return;

        // 체력 감소
        GameManager.instance.Health -=
            damage * Time.deltaTime;
    }
    // 보스 탄막 피격 처리
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 보스 탄막 아니면 무시
        if (!collision.gameObject.CompareTag("BossBullet"))
            return;

        // 탄막 스크립트 가져오기
        BossBullet bullet =
            collision.gameObject.GetComponent<BossBullet>();

        // 탄막 데미지 적용
        if (bullet != null)
        {
            GameManager.instance.Health -=
                bullet.damage;
        }

        // 탄막 제거
        collision.gameObject.SetActive(false);
    }

    public void PlayerDead()
    {
        // 이미 죽었으면 중단
        if (isDead)
            return;

        isDead = true;

        // 무기/보조 오브젝트 비활성화
        for (int index = 2; index < transform.childCount; index++)
        {
            transform.GetChild(index).gameObject.SetActive(false);
        }

        // 사망 애니메이션 재생 후 게임 오버
        anim.SetTrigger("Dead");
        GameManager.instance.GameOver();
    }

    // 플레이어 기절 구현
    public void Stun(float time)
    {
        StopAllCoroutines();
        StartCoroutine(StunRoutine(time));
    }

    System.Collections.IEnumerator StunRoutine(float time)
    {
        isStunned = true;
        yield return new WaitForSeconds(time);
        isStunned = false;
    }

    // 상태 색 적용
    public void SetStatusTint(Color tint)
    {
        currentTint = tint;
        spriter.color = currentTint;
    }
    // 기본 색 복구
    public void ResetStatusTint()
    {
        currentTint = defaultTint;
        spriter.color = defaultTint;
    }

    // 용암 체크용
    public bool IsOnLava()
    {
        Vector2 pos = rigid.position;

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                pos,
                0.15f,
                groundMask
            );

        if (hits == null)
            return false;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag("Lava"))
                return true;
        }

        return false;
    }
}