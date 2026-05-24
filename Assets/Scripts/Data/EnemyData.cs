using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyData",
    menuName = "Scriptable/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("애니메이터 타입")]
    public int spriteType;

    [Header("최대 체력")]
    public float maxHealth = 10f;

    [Header("이동 속도")]
    public float moveSpeed = 2f;

    [Header("공격력")]
    public float attackDamage = 5f;

    [Header("방어력")]
    [Range(0f, 1f)]
    public float defense = 0f;
}