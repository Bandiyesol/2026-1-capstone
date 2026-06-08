using UnityEngine;
using System.Collections;

// 모래바람 기믹 스크립트
public class SandstormGimmick : BiomeGimmick
{
    [Header("이동 속도")]
    [SerializeField] float moveSpeed = 0.7f;

    [Header("가리개 유지 시간")]
    [SerializeField] float overlayDuration = 2f; // 화면이 가려질 시간

    Vector2 moveDir; // 이동 방향
    SpriteRenderer sr; // 스프라이트 렌더러
    SandOverlayController sandOverlay; // 전역 가리개 컨트롤러

    protected override void Awake()
    {
        base.Awake();
        sr = GetComponent<SpriteRenderer>();
        sandOverlay = SandOverlayController.Instance;
    }

    protected override void OnSpawn()
    {
        StopAllCoroutines();

        if (sandOverlay == null)
            sandOverlay = SandOverlayController.Instance;

        // 💡 [복구됨] 일반 바이옴에서 스폰될 때를 위한 기본 좌/우 랜덤 방향 설정
        moveDir = Random.value < 0.5f ? Vector2.left : Vector2.right;

        if (sr != null)
        {
            sr.flipX = moveDir.x > 0f;
            sr.flipY = false;
        }
    }

    // ========================================================
    // 보스가 방향을 정해줄 때 호출할 초기화 함수
    // ========================================================
    public void Init(Vector2 dir)
    {
        // 💡 보스가 소환했을 때는 OnSpawn에서 정해진 랜덤 방향을 무시하고 덮어씌웁니다.
        moveDir = dir.normalized;

        if (sr != null)
        {
            sr.flipX = moveDir.x > 0f;
            sr.flipY = false;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (GameManager.instance == null || !GameManager.instance.isLive)
            return;

        // Init에서 받은 8방향(moveDir)으로 이동
        transform.position += (Vector3)(moveDir * moveSpeed * Time.deltaTime);
    }

    protected override void OnPlayerTrigger(Player player)
    {
        // 플레이어 피격 시 화면 가리기 명령
        if (sandOverlay != null)
            sandOverlay.TriggerOverlay(overlayDuration);
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }
}