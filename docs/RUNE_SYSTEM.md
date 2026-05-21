# 룬 시스템 정리 (코드 기준)

> 기획 문서의 「발사 / 이동 / 충돌」3단계는 구현 시 **5카테고리**로 운영합니다. Wiki/README 용어 갱신 시 아래 표를 기준으로 맞추세요.

## 카테고리 (RuneCategory)

| 코드 | 역할 | 기획 대응 (권장 문구) |
|------|------|----------------------|
| Active | 탄환 이동·궤도 제어 (`IActiveDriver`) | 이동 |
| Trigger | 충돌 시 발동 (`ITriggerEffect`) | 충돌 |
| State | 지속 상태 (`IStateEffect`) | 지속·특수 |
| Logic | 주기·좌표 변화 (`ILogicEffect`) | 지속·특수 |
| Final | 소멸 직전 1회 (`IFinalEffect`) | 마무리 |

## RuneType ↔ 데이터 클래스 ↔ Effect

| RuneType | SO 클래스 | Effect | 상태 |
|----------|-----------|--------|------|
| Homing | `ActiveRuneData` | `EffectHoming` | 구현 |
| Orbit | `ActiveRuneData` | `EffectOrbit` | 구현 |
| Split | `SplitRuneData` | `EffectSplit` | 구현 |
| Ricochet | `RicochetRuneData` | `EffectRicochet` | 구현 |
| Recursion | `FinalRuneData` | `EffectRecursion` | 구현 |
| Wave | (미정) | — | 미구현 |
| Spiral | (미정) | — | 미구현 |
| Vampire | (미정) | — | 미구현 |
| Freeze | `FreezeRuneData` | — | 데이터만 |
| Chain | (미정) | — | 미구현 |
| Explode | `ExplodeRuneData` | — | 데이터만 |
| Gravity | `GravityRuneData` | — | 미구현 |
| Growth | `GrowthRuneData` | — | 미구현 |
| Blink | `LogicRuneData` | — | 미구현 |
| Boing | `LogicRuneData` | — | 미구현 |

**제거 예정(잔해):** Return, Delay — 에셋·Validator 규칙 삭제됨.

## 수치 필드 (서브클래스)

- **ActiveRuneData:** `duration`, `speedMultiplier`, `affectedRange`
- **SplitRuneData:** `splitCount`, `power`(데미지 배율)
- **RicochetRuneData:** `bounceCount`, `interval`
- **Freeze / Explode:** 각 전용 필드 + `interval`

Effect에서는 `RuneDataAccess` 정적 메서드로 읽습니다.

## 조합 검증 (RuneValidator)

- 중복 `RuneType` 금지
- 비호환: Orbit+Homing, Gravity+Wave
- 순서: Explode→Homing, Freeze→Chain

## 신규 룬 추가 체크리스트

1. `RuneType` enum 추가
2. `Scripts/Data/Rune/` 에 SO 클래스 추가
3. `Scripts/Features/Rune/Effect*.cs` 구현
4. `RuneEffectRegistry.EffectTypes`에 매핑 등록
5. `Assets/Arts/Data/` 에셋 생성 (올바른 SO 스크립트 연결)
6. `RuneValidator` 규칙 필요 시 추가
