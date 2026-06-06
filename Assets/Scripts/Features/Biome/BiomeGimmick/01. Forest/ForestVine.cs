using UnityEngine;

// 플레이어를 잠시 속박하는 덩굴
public class ForestVine : BiomeGimmick
{
    [Header("속박 시간")]
    [SerializeField] float stunTime = 1f;

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
    }

    protected override void OnPlayerTrigger(Player player)
    {
        // 잠시 이동 불가
        player.Stun(stunTime);

        gameObject.SetActive(false);
    }
}