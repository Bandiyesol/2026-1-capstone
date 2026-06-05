using UnityEngine;

// 안개 은신 지대
public class VoidFog : BiomeGimmick
{
    protected override void Update()
    {
        // 부모 Update 실행
        base.Update();
    }

    // 적 진입
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);

        if (!collision.CompareTag("Enemy"))
            return;

        Enemy enemy = collision.GetComponent<Enemy>();

        if (enemy == null)
            return;

        enemy.EnterFog();
    }

    // 적 이탈
    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy"))
            return;

        Enemy enemy =
            collision.GetComponent<Enemy>();

        if (enemy == null)
            return;

        enemy.ExitFog();
    }

    protected override void OnSpawn()
    {

    }

    protected override void OnPlayerTrigger(Player player)
    {

    }
}