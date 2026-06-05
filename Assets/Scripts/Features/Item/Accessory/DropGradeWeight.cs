using System;

/// <summary>
/// 악세사리 등급이 뽑힐 가중치.
/// 인스펙터에서 일반 몬스터용 / 유니크 몬스터용 각각 설정 가능.
/// </summary>
[Serializable]
public struct DropGradeWeight
{
    public float common;
    public float rare;
    public float unique;
    public float legendary;

    /// <summary>일반 몬스터용 기본값</summary>
    public static DropGradeWeight Normal => new DropGradeWeight
    {
        common    = 60f,
        rare      = 30f,
        unique    = 8f,
        legendary = 2f
    };

    /// <summary>유니크 몬스터용 — 높은 등급 확률 증가</summary>
    public static DropGradeWeight Unique => new DropGradeWeight
    {
        common    = 10f,
        rare      = 45f,
        unique    = 35f,
        legendary = 10f
    };
}
