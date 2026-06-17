using System.Collections;
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
        DisableCollider();
        StartCoroutine(PlayAnimNextFrame());
    }

    IEnumerator PlayAnimNextFrame()
    {
        yield return null;

        if (anim != null)
        {
            anim.enabled = false;        // Animator 완전히 끄기
            yield return null;           // 한 프레임 대기
            anim.enabled = true;         // 다시 켜면 Entry부터 재시작
        }
    }

    protected override void OnPlayerTrigger(Player player)
    {
        // 잠시 이동 불가
        player.Stun(stunTime);

        gameObject.SetActive(false);
    }
}