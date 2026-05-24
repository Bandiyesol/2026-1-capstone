# 룬 에셋 점검 결과 (자동 분석)

## 원인

`RuneData` 구조를 **서브클래스**(ActiveRuneData, SplitRuneData …)로 바꾼 뒤,
`Assets/Arts/Data/Rune_*.asset` 은 **전부 예전 Script: RuneData** 에 묶여 있었습니다.

→ 인스펙터에 `duration` / `spawnsPerTrigger` 필드가 없고, Effect가 읽을 값이 0에 가깝게 동작.

**Growth / Ricochet / Freeze / Explode “스크립트 로드 불가”:** 한 `.cs` 파일에 SO 클래스가 여러 개면 Unity는 **첫 클래스만** 스크립트로 인식합니다. `GrowthRuneData` 등은 **파일 분리** 후 **Tools → Rune → Fix Missing Script References** 실행.

## 폴더 역할 (정상)

| 경로 | 역할 |
|------|------|
| `Scripts/Features/Rune/RuneData.cs` | 공통 부모 + enum |
| `Scripts/Data/Rune/*.cs` | SO **타입 정의** (CreateAssetMenu) |
| `Scripts/Features/Rune/Effect*.cs` | 런타임 **동작** |
| `Arts/Data/Rune_*.asset` | 실제 **데이터** |

`Data` vs `Features` 분리는 **이상하지 않음**. 문제는 에셋이 구 스크립트에 남아 있던 것.

## 수정 방법 (Unity)

메뉴: **Tools → Rune → Repair All Rune Assets**

실행 후 `Rune_Homing` Inspector **Script**가 `ActiveRuneData` 등으로 바뀌었는지 확인.

## 카테고리별 올바른 Script

| 카테고리 | Script |
|----------|--------|
| Active (Orbit, Wave, Spiral, Homing) | ActiveRuneData |
| Trigger Split | SplitRuneData |
| Trigger Ricochet | RicochetRuneData |
| Trigger Freeze | FreezeRuneData |
| Trigger Explode | ExplodeRuneData |
| State Gravity | GravityRuneData |
| State Growth | GrowthRuneData |
| Logic Blink, Boing | LogicRuneData |
| Final Recursion | FinalRuneData |

## power 규칙

- **데미지 배율만** (`DamageCalculator`, Split 자식 데미지 등)
- Active 이동/회전: `speedMultiplier`, `affectedRange` (power 사용 안 함)
