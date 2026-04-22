using UnityEngine;
 
// 룬 카테고리 분류 (궤적 / 로직 / 속성)
public enum RuneCategory
{
    Trajectory, // 궤적: 탄환의 움직임 결정 (Wave, Orbit, Ricochet, Blink, Return)
    Logic,      // 로직: 실행 방식 결정 (Homing, Split, Chain, Recursion, Delay)
    Effect      // 속성: 최종 효과 결정 (Explode, Gravity, Freeze, Growth, Vampire)
}
 
// 룬 종류 열거형 (없음 포함)
public enum RuneType
{
    None,
    // 궤적
    Orbit, Ricochet, Blink, Wave, Return,
    // 로직
    Homing, Split, Chain, Recursion, Delay,
    // 속성
    Explode, Gravity, Freeze, Growth, Vampire
}
 
[CreateAssetMenu(fileName = "Rune", menuName = "Scriptable Object/RuneData")]
public class RuneData : ScriptableObject
{
    [Header("# 기본 정보")]
    public RuneType runeType;
    public RuneCategory category;
    public string runeName;
    [TextArea] public string runeDesc;
    public Sprite runeIcon;
 
    [Header("# 비용 / 균형")]
    // 이 룬을 장착했을 때 발사 간격에 추가되는 페널티 (초)
    public float cooldownPenalty = 0f;
    // 이 룬을 장착했을 때 발사당 소모되는 마나 (추후 마나 시스템 연동)
    public int manaCost = 0;
 
    [Header("# 호환 불가 룬 목록")]
    // 이 룬과 함께 쓰면 '런타임 에러' 판정나는 룬 조합
    public RuneType[] incompatibleWith;
 
    [Header("# 룬 수치")]
    // 각 룬에서 쓰는 범용 수치 파라미터 (의미는 룬마다 다름)
    // 예) Wave: 진폭 / Ricochet: 첫 도탄 데미지 배율 / Delay: 폭발 배율 등
    public float valueA = 1f;
    public float valueB = 1f;
}