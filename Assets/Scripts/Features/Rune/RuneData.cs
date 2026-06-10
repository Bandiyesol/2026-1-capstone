using UnityEngine;

// 인스펙터에서 관리할 수 있는 형태의 룬 데이터 베이스
[CreateAssetMenu(fileName = "Rune", menuName = "Scriptable Object/RuneData")]
public class RuneData : ScriptableObject
{
    [Header("[ 기본 정보 ]")]
    public string runeName;         // 룬 이름
    public RuneCategory category;   // 발동 시점 (Active, Trigger 등)
    public RuneType runeType;       // 룬 종류 (Homing, Split 등)
    public Sprite runeIcon;         // UI용 아이콘
    [TextArea] public string runeDescription; // 룬 설명 (간략)
    [TextArea] public string runeDesc;        // 룬 상세 설명

    [Header("[ 전투 / 균형 ]")]
    public bool isDestroyed;        // 효과(로직)가 끝난 후 총알/투사체를 즉시 파괴할지 여부
    public float power;             // 데미지 배율 (기본 데미지에 곱해지는 수치)

    [Header("[ 룬 수치 ]")]
    public float valueA = 1f;       // 범용 수치 변수 A (룬마다 다르게 해석됨)
    public float valueB = 1f;       // 범용 수치 변수 B (룬마다 다르게 해석됨)
}

// 룬이 언제, 혹은 어떤 방식으로 실행될 것인가를 결정하는 카테고리
public enum RuneCategory
{
    Active,  // 실시간으로 궤적이나 움직임을 조작할 때 (Update 주기)
    Trigger, // 충돌, 도탄 등 특정 조건(이벤트)이 달성되었을 때 발동
    Final,   // 수명이 다하거나 소멸하기 직전 마지막으로 발동
    State,   // 무기의 상태(크기, 데미지 등)를 지속적으로 변경할 때
    Logic    // 백그라운드에서 주기적으로 발동하는 특수 로직
}

// 룬이 구체적으로 어떤 기능을 하는가 (누락 없이 전체 복구)
public enum RuneType
{
    None,       // 없음 (기본 상태)

    // [ 이동 궤적 관련 (주로 Active) ]
    Orbit,      // 공전: 무기가 특정 지점이나 플레이어 주위를 빙글빙글 돎
    Wave,       // 파동: 무기가 물결 치듯(사인/코사인파) 상하좌우로 흔들리며 날아감
    Spiral,     // 나선: 무기가 나선형 궤적을 그리며 뻗어나감
    Homing,     // 유도: 적을 감지하고 방향을 틀어 스스로 쫓아감

    // [ 충돌 및 타격 관련 (주로 Trigger) ]
    Split,      // 분열: 적이나 벽에 부딪히면 여러 갈래의 자식 무기로 쪼개짐
    Ricochet,   // 도탄: 벽이나 적에게 부딪히면 튕겨져 나와 다른 각도로 날아감
    Vampire,    // 흡혈: 적에게 피해를 입히면 플레이어의 체력 등을 회복함
    Freeze,     // 빙결: 적중한 적의 움직임을 얼리거나 둔화시킴
    Chain,      // 연쇄: 적에게 명중 시 근처의 다른 적에게 번개처럼 옮겨 붙음
    Explode,    // 폭발: 충돌 시 일정 범위 내의 적들에게 광역 데미지를 입힘

    // [ 특수 및 소멸 관련 (주로 Final) ]
    Recursion,  // 재귀: 무기의 수명이 다하면 자신을 복제하여 다시 발사함

    // [ 상태 및 물리 변화 관련 (State, Logic 등) ]
    Gravity,    // 중력: 주변의 적을 끌어당기거나 투사체가 중력의 영향을 받아 떨어짐
    Growth,     // 성장: 날아가는 시간이나 거리에 비례하여 크기와 데미지가 점점 커짐

    // [ 특수 기동 관련 ]
    Blink,      // 점멸: 특정 조건에서 투사체가 순식간에 다른 위치로 텔레포트함
    Boing,      // 통통 튐: 바닥이나 벽에 닿았을 때 고무공처럼 탄성 있게 튕김
    Return,     // 귀환: 일정 거리를 날아가거나 시간이 지나면 부메랑처럼 주인을 향해 되돌아옴
    Delay       // 지연: 발사된 직후 바로 날아가지 않고 잠시 대기했다가 날아감
}