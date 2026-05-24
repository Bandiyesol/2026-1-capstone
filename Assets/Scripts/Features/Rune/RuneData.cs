using UnityEngine;

[CreateAssetMenu(fileName = "Rune", menuName = "Scriptable Object/RuneData")]
public class RuneData : ScriptableObject
{
    [Header("[ 기본 정보 ]")]
    public string runeName;
    public RuneCategory category;
    public RuneType runeType;
    public Sprite runeIcon;
    [TextArea] public string runeDescription;
    [TextArea] public string runeDesc;

    [Header("[ 전투 / 균형 ]")]
    [Tooltip("룬 기능 완료 후 즉시 탄환 소멸 요청")] public bool isDestroyed;
    [Tooltip("데미지 배율 (DamageCalculator)")] public float power;
    [Tooltip("발사 간격 페널티 (초)")] public float cooldownPenalty;
    public int manaCost;

    [Header("[ 호환성 ]")]
    public RuneType[] incompatibleWith;

    [Header("[ 룬 수치 ]")]
    public float valueA = 1f;
    public float valueB = 1f;
}

public enum RuneCategory { Active, Trigger, Final, State, Logic }

public enum RuneType
{
    None,
    Orbit, Wave, Spiral, Homing,
    Split, Ricochet, Vampire, Freeze, Chain, Explode,
    Recursion,
    Gravity, Growth,
    Blink, Boing,
    Return, Delay
}
