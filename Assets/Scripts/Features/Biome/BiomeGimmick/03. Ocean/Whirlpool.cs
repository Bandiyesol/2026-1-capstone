using UnityEngine;

// 플레이어를 끌어당기는 소용돌이
public class Whirlpool : BiomeGimmick
{
    [Header("흡입력")]
    [SerializeField] float pullForce = 2.2f;

    protected override void Update()
    {
        // 부모 Update 실행
        base.Update();
    }

    protected override void OnSpawn()
    {

    }

    protected override void OnPlayerTrigger(Player player)
    {

    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // 플레이어만 처리
        if (!collision.CompareTag("Player"))
            return;

        Player player =
            collision.GetComponent<Player>();

        if (player == null)
            return;

        // 중심 방향
        Vector3 dir =
            transform.position -
            player.transform.position;

        // 외부 힘 추가
        player.externalVelocity +=
            (Vector2)dir.normalized * pullForce;
    }
}