using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Boss Data")]
public class BossData : ScriptableObject
{
    [Header("기본")]
    // 최대 체력
    public float maxHealth = 100;
    // 이동 속도
    public float moveSpeed = 3f;
    // 공격 데미지
    public float attackDamage = 10f;
    // 피해 감소율
    [Range(0f, 1f)] public float damageReduction = 0.3f;

    [Header("패턴")]
    // 패턴 쿨타임
    public float patternCooldown = 5f;
}
