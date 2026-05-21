using UnityEngine;


[CreateAssetMenu(fileName = "GravityRune", menuName = "RuneData/State/Gravity")]
public class GravityRuneData : RuneData 
{
    public float duration;         // 지속 시간 (n초)
    public float pullForce;        // 끌어당기는 힘 (m의 힘)
}

[CreateAssetMenu(fileName = "GrowthRune", menuName = "RuneData/State/Growth")]
public class GrowthRuneData : RuneData 
{
    public float maxGrowthTime;    // 최대 증폭 완료 시간
    public float maxScaleRatio;    // 최대 크기 배율 (n배)
    // 최대 데미지 증폭 배율은 부모의 'power'를 사용합니다. (예: 1.5f 입력)
}