using UnityEngine;

// 일정 시간 동안 독 데미지
public class PoisonField : BiomeGimmick
{
    [Header("초당 데미지")]
    [SerializeField] float damagePerSecond = 5f;

    protected override void Update()
    {
        // 부모 Update 실행
        base.Update();
    }

    // 생성 시 별도 동작 없음
    protected override void OnSpawn() { }

    // 접촉 즉시 효과 없음
    protected override void OnPlayerTrigger(Player player) { }

    void OnTriggerStay2D(Collider2D collision)
    {
        // 플레이어가 아니면 무시
        if (!collision.CompareTag("Player"))
            return;

        // 지속 피해
        GameManager.instance.Health -= damagePerSecond * Time.deltaTime;
    }
}
