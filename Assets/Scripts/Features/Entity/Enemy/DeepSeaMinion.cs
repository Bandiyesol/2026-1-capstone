using UnityEngine;

/// <summary>
/// 심해 괴수인 부하 몬스터
/// 사망 시 근처 DeepSeaMutant에게 OnSummonDead() 신호를 전달한다
/// </summary>
public class DeepSeaMinion : Enemy
{
    [Header("심해 괴수인 연동")]
    [SerializeField] float searchRadius = 50f; // 보스 탐색 반경 (넉넉하게 설정)

    protected override void Die()
    {
        // 기존 Enemy 사망 처리 그대로 실행
        base.Die();

        // 근처 DeepSeaMutant 찾아서 OnSummonDead() 호출
        DeepSeaMutant boss = FindBoss();
        if (boss != null)
            boss.OnSummonDead();
    }

    DeepSeaMutant FindBoss()
    {
        // 반경 내 DeepSeaMutant 탐색
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, searchRadius);
        foreach (var hit in hits)
        {
            DeepSeaMutant boss = hit.GetComponent<DeepSeaMutant>();
            if (boss != null)
                return boss;
        }
        return null;
    }
}
