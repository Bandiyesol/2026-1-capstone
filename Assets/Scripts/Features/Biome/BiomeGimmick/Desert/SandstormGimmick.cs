using UnityEngine;

// 모래바람 기믹
public class SandstormGimmick : BiomeGimmick
{
    [Header("이동 속도")]
    [SerializeField] float moveSpeed = 0.7f;

    // 이동 방향
    Vector2 moveDir;

    // 스프라이트
    SpriteRenderer sr;

    protected override void Awake()
    {
        // 부모 초기화
        base.Awake();

        // 렌더러 캐싱
        sr = GetComponent<SpriteRenderer>();
    }

    protected override void OnSpawn()
    {
        // 이전 상태 정리
        StopAllCoroutines();

        // 좌/우 랜덤 방향
        moveDir = Random.value < 0.5f
            ? Vector2.left
            : Vector2.right;

        // 오른쪽이면 좌우 반전
        if (sr != null)
            sr.flipX = moveDir.x > 0f;

        // --- 투사체 속도 감소 ---
        // 나중에 여기 연결
    }

    protected override void Update()
    {
        // 부모 Update 실행
        base.Update();

        // 게임 정지면 중단
        if (GameManager.instance == null)
            return;

        if (!GameManager.instance.isLive)
            return;

        // 천천히 이동
        transform.position +=
            (Vector3)(moveDir * moveSpeed * Time.deltaTime);
    }

    protected override void OnPlayerTrigger(Player player)
    {
        // 접촉형 아님
    }

    void OnDisable()
    {
        // 풀 재사용 대비 정리
        StopAllCoroutines();

        // --- 투사체 속도 복구 ---
        // 나중에 여기 연결
    }
}
