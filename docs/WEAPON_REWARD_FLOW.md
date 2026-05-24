# 무기·보상 흐름 (기획 ↔ 코드)

## 무기 수치는 언제 고정되나

- **선택창에 카드가 뜰 때** `WeaponInstance`를 한 번 생성 → damage, reach, cooltime 등 **롤링 완료**
- 인벤토리에 넣은 뒤에는 그 인스턴스 수치 유지 (공격마다 재롤링 없음)
- 선택하지 않은 후보 인스턴스는 **Destroy / 버림**

```
RewardUI.ShowWeaponChoices()
  → 후보 3개 각각 new WeaponInstance(info, balance)  // 미리보기 수치 확정
  → 플레이어 1택 → Inventory.Add(picked)
  → 나머지 2개 폐기
```

## JSON 역할

| 파일 | 역할 |
|------|------|
| `WeaponInfo.json` | 이름, type, grade, balanceKey, (선택) weaponCategory, legendaryPassiveId |
| `WeaponBalance.json` | 등급별 **랜덤 범위** (damageRange, cooltimeRange …) |

`legendaryPassiveId` 예시 (전설만):

```json
{
  "id": "SWORD_LEGEND_001",
  "name": "오션엠페러 나이트",
  "type": "Sword",
  "grade": "Legendary",
  "balanceKey": "Sword_Legendary",
  "weaponCategory": "Melee",
  "legendaryPassiveId": "OCEAN_EMPEROR"
}
```

일반 무기는 `legendaryPassiveId`를 비우거나 필드를 생략.

## 전설 무기 vs 룬

- **플레이어 룬 3슬롯**: 모든 무기 공격(Motion)에 공통 적용
- **legendaryPassiveId**: 전설 무기만 추가되는 고유 패시브 (별도 스크립트/매니저)
- 전설도 룬 조합은 가능 (아쉬움 해소). 패시브는 “무기 고유 레이어”로만 얹음.

## 투사체 개수

- `WeaponBalance`에 countRange **불필요** (기본 1발)
- `PlayerStats.ProjectileCount` (기본 1, 악세로 증가)
- `Attack()` / `MotionBow` 등에서 `PlayerStats.Instance.ProjectileCount`만큼 발사

## 인벤토리

- `PlayerWeaponInventory` — 무기 인스턴스 리스트, `maxWeapons` 상한
- `WeaponController` — 인벤토리만 `Tick` (시작 시 3무기 강제 장착 제거)
- **같은 type 중복 허용**, 상한만으로 밸런스 조절

## 게임 시작 UI 연결 (Unity)

1. Player에 `PlayerWeaponInventory` 추가
2. `WeaponSelectUI` 패널 생성 — 버튼 3개 + TMP 라벨 3개
3. 버튼 OnClick → `WeaponSelectUI.OnPickWeapon(0/1/2)`
4. `GameManager.uiWeaponSelect`에 연결
5. `WeaponSelectUI.inventory` → Player 인벤토리

흐름: `GameStart` → 무기 3택1 → `RuneSelectUI` → `Resume`
