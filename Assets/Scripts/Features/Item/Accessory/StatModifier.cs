using System;

/// <summary>
/// 악세사리가 PlayerStats에 가하는 수정값 한 단위.
/// isMulti = false → AddFlat (고정 수치)
/// isMulti = true  → AddMulti (비율, 0.1f = +10%)
/// </summary>
[Serializable]
public struct StatModifier
{
    public StatType statType;
    public float    value;
    public bool     isMulti;

    public StatModifier(StatType statType, float value, bool isMulti)
    {
        this.statType = statType;
        this.value    = value;
        this.isMulti  = isMulti;
    }
}
