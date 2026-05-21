using UnityEngine;


[CreateAssetMenu(fileName = "SplitRune", menuName = "RuneData/Trigger/Split")]
public class SplitRuneData : RuneData 
{
    public int splitCount;         // 분열 개수 (n개)
    // 데미지 감쇄율은 부모가 가진 'power' 변수를 그대로 사용합니다. (예: 0.5f 입력)
}

[CreateAssetMenu(fileName = "RicochetRune", menuName = "RuneData/Trigger/Ricochet")]
public class RicochetRuneData : RuneData 
{
    public int bounceCount;        // 튕기는 횟수 (n번)
    public float interval;
}

[CreateAssetMenu(fileName = "FreezeRune", menuName = "RuneData/Trigger/Freeze")]
public class FreezeRuneData : RuneData 
{
    public float freezeRadius;     // 빙결 범위 (n범위)
    public float freezeDuration;   // 멈추는 시간 (m초)
    public float interval;
}

[CreateAssetMenu(fileName = "ExplodeRune", menuName = "RuneData/Trigger/Explode")]
public class ExplodeRuneData : RuneData 
{
    public float explodeRadius;    // 폭발 반지름 (n인 원 크기)
    // 폭발 데미지 배율은 부모의 'power'를 사용합니다. (예: 2.5f 입력)
    public float interval;
}