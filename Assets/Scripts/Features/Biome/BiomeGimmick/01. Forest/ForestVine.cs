using UnityEngine;

// 플레이어를 잠시 속박하는 덩굴
public class ForestVine : BiomeGimmick
{
    [Header("속박 시간")]
    [SerializeField] float stunTime = 1f;

    // 애니메이터 캐싱
    Animator anim;

    protected override void Awake()
    {
        base.Awake();

        // Animator 가져오기
        anim = GetComponent<Animator>();
    }

    protected override void Update()
    {
        // 부모 Update 실행
        base.Update();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // 자라는 동안 충돌 비활성
        DisableCollider();

        // 애니메이션 재시작
        if (anim != null)
        {
            anim.Rebind();                        // 상태 완전 초기화
            anim.Update(0f);                      // 즉시 반영
            anim.Play("SpikeVine", 0, 0f);      // 첫 프레임부터 재생
        }
    }

    protected override void OnPlayerTrigger(Player player)
    {
        // 잠시 이동 불가
        player.Stun(stunTime);

        gameObject.SetActive(false);
    }
}