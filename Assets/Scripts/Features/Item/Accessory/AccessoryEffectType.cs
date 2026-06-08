/// <summary>
/// 악세사리 특수 효과 종류.
/// None = 스탯 수정만 있는 단순 악세사리.
/// </summary>
public enum AccessoryEffectType
{
    None,               // 특수 효과 없음 (스탯만)

    // ── 일반 ───────────────────────────────────────
    DamageReflect,      // 바늘 뭉치    : 받은 피해 일부 반사
    SpeedOnHit,         // 빨간 리본    : 피격 시 이동속도 일시 증가
    SlowOnHit,          // 얼음 조각    : 피격 시 적 이동속도 감소
    BurnOnAttack,       // 부싯돌       : 공격 시 화상 고정 피해

    // ── 희귀 ───────────────────────────────────────
    LightningStrike,    // 번개 맞은 나뭇가지 : 확률 낙뢰
    PoisonOnAttack,     // 메두사의 이빨      : 공격마다 독 부여
    ChainLightning,     // 에키드나 목걸이    : 확률 연쇄 번개
    FreezeChance,       // 눈꽃 송이          : 타격 시 동결 확률
    BleedOnAttack,      // 사막의 전갈 꼬리   : 확률 출혈
    DuplicateBullet,    // 쌍둥이 보석        : 복제 탄환 확률
    ShieldOnLowHP,      // 강철의 심장        : 체력 낮을 때 방어 증가
    LifeStealOnKill,    // 흡혈귀의 송곳니    : 처치 시 체력 회복
    InvincibleOnPotion, // 신비한 약병        : 포션 사용 시 무적
    BlockHeavyDamage,   // 단단한 껍질        : 큰 피해 무효화 확률
    Revive,             // 부활의 씨앗        : 사망 시 부활 1회
    RevengeArrow,       // 가시 목걸이        : 피격 시 화살 발사
    AutoHeal,           // 약초 꾸러미        : 주기적 자동 회복
    ShadowClone,        // 그림자 가면        : 분신 소환
    GoldOnHit,          // 황금 손목 보호대   : 적중 시 골드 드랍
    Ricochet,           // 고무 화살촉        : 투사체 벽 튕기기
    SkeletonOnKill,     // 흑마법의 인장      : 처치 시 해골 소환
    PoisonSpread,       // 맹독성 확산기      : 독 스택 전이
    RandomElement,      // 불투명한 프리즘    : 무작위 속성 부여
    ElementStack,       // 원소의 조율자      : 속성 중첩 버프

    // ── 유니크 ─────────────────────────────────────
    RuneMaxEffect,      // 대마법사의 부서진 지팡이
    BloodContract,      // 피의 계약서
    DimensionCompass,   // 차원의 나침반
    ElectricOnHit,      // 번개 깃든 암령
    AlchemistBag,       // 연금술사의 가방
    ShadowTracker,      // 그림자 추적자의 망토
    ExecutionEye,       // 집행자의 눈가리개
    BlackHolePull,      // 검은 구멍
    SoulBullet,         // 영혼의 등불
    MovingDamage,       // 대지의 신발
    RuneEcho,           // 메아리의 소라
    BurningAura,        // 화염의 외투
    GoldenFinger,       // 황금 손가락
    ExtraRuneSlot,      // 고대 문양 : 룬 슬롯 +1

    // ── 전설 ───────────────────────────────────────
    ZeusJudgment,       // 제우스의 심판
    AbyssLord,          // 심연의 군주
    PhoenixFeather,     // 불사조의 깃털
    MinervaWisdom,      // 미네르바의 지혜
    MidasGlove,         // 미다스의 장갑
    TimeStop,           // 시간술사의 모래시계
    GodShield,          // 신의 방패
    InfiniteMana,       // 무한의 마력
    CalamitySeed,       // 재앙의 씨앗
    DragonHeart,        // 용의 심장
    DimensionBoots,     // 차원 여행자의 장화
    TheLastRune,        // The Last Rune
}
