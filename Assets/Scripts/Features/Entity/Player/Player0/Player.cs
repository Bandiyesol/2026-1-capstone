using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class Player : MonoBehaviour
{
    // 입력 시스템에서 받은 이동 입력값
    public Vector2 inputVec;
    // 이동 속도
    public float speed;
    // 근처 적 탐색 컴포넌트
    public Scaner scaner;
    // 물리 이동 처리용 리지드바디
    Rigidbody2D rigid;
    // 좌우 반전 제어용 스프라이트 렌더러
    public SpriteRenderer spriter;
    // 이동/사망 애니메이션 제어
    Animator anim;

    // 입력이 멈춰도 바라보는 방향을 유지하기 위한 마지막 이동 방향
    Vector2 lastTravelDirection = Vector2.right;

    void Awake()
    {
        // 자주 사용하는 컴포넌트 캐싱
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        scaner = GetComponent<Scaner>();
    }

    void FixedUpdate()
    {
        // 게임 정지 시 이동 입력 처리 중단
        if (!GameManager.instance.isLive)
            return;

        // 입력값 기반으로 물리 프레임 이동량 계산
        Vector2 nextVec = inputVec * speed * Time.fixedDeltaTime;
        // 이동 중일 때만 마지막 진행 방향 갱신
        if (nextVec.sqrMagnitude > 1e-10f)
            lastTravelDirection = nextVec.normalized;
        rigid.MovePosition(rigid.position + nextVec);
    }

    void OnMove(InputValue value)
    {
        // Input System Action에서 이동값 수신
        inputVec = value.Get<Vector2>();
    }

    void LateUpdate()
    {
        // 게임 중이 아닐 때는 애니메이션/방향 갱신 중단
        if (!GameManager.instance.isLive)
                    return;

        // 이동량을 애니메이션 파라미터로 전달
        anim.SetFloat("Speed", inputVec.magnitude);

        // 좌/우 입력이 있을 때만 캐릭터 좌우 반전
        if (inputVec.x != 0) {
            spriter.flipX =  inputVec.x < 0;
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
        // 게임 정지 상태에서는 접촉 데미지 처리 중단
        if (!GameManager.instance.isLive)
            return;

        // 적과 닿아있는 동안 지속 피해 적용
        GameManager.instance.Health -= Time.deltaTime * 10;

        // 체력이 0 이하이면 사망 처리
        if (GameManager.instance.Health < 0)
        {
            // 무기/보조 오브젝트 비활성화
            for (int index=2; index < transform.childCount; index++)
            {
                transform.GetChild(index).gameObject.SetActive(false);
            }

            // 사망 애니메이션 재생 후 게임 오버
            anim.SetTrigger("Dead");
            GameManager.instance.GameOver();
        }
    }
}
