# 보스 알리미 UI — Unity에서 처음부터 만들기 (상세 스텝)

이 문서는 **코드는 이미 있다고 가정**하고, 에디터에서 **클릭 순서대로** UI를 만드는 방법만 설명합니다.

**목표 흐름**  
게임 시작 → 메인 스토리 → **보스 알리미(전체 화면)** → 무기 선택 → 룬 선택 → 플레이  
플레이 중에는 **체력바 오른쪽 `!` 또는 아이콘**에 마우스를 올리면 **같은 설명**이 툴팁으로 뜹니다.

**준비물**

- 씬: 보통 `ProtoType_LTG` (또는 실제 게임 씬)
- Hierarchy에 **Canvas**가 있고, 그 안에 **HUD**, **GameManager** 오브젝트가 있는 구조를 권장합니다.
- 씬에 **EventSystem**이 있어야 마우스 호버가 됩니다. 없으면: **Hierarchy 우클릭 → UI → Event System** 생성.

---

## 0단계 — 씬 열고 구조 확인

1. **File → Open Scene** → `Assets/Scenes/ProtoType_LTG.unity` (본인 프로젝트 씬명에 맞게).
2. **Hierarchy**에서 **Canvas**를 찾습니다.  
   - 없으면: **Hierarchy 우클릭 → UI → Canvas** 로 만듭니다.
3. Canvas 선택 → **Inspector**에서 **Render Mode**가 `Screen Space - Overlay` 인지 확인 (대부분 이 설정).
4. **GameManager** 오브젝트를 Hierarchy에서 찾습니다.  
   - 없으면 빈 오브젝트 만들고 `GameManager` 스크립트를 붙입니다 (다른 문서대로 이미 있다면 생략).

이제 **두 덩어이** UI를 만듭니다.

- **A. 보스 알리미** (전면 패널) — Canvas 아래 새 트리  
- **B. HUD 보스 툴팁** — 기존 `HUD` 아래에 트리 추가  

---

## A부 — 보스 알리미 (전면) 만들기

### A-1. 루트 오브젝트 만들기 (스크립트는 여기 붙임)

1. **Hierarchy**에서 **Canvas**를 선택합니다.
2. **Canvas 우클릭 → Create Empty**  
3. 이름을 **`BossAlarmRoot`** 로 바꿉니다. (이름은 바꿔도 되지만, 문서와 통일)
4. **BossAlarmRoot** 선택 상태에서 **Inspector → Add Component** → 검색창에 **`BossAlarm`** 입력 → **`BossAlarmUI`** 선택.

> `BossAlarmUI`는 **항상 켜져 있는 오브젝트**에 붙이세요.  
> 패널(`BossAlarmPanel`)은 **처음에 꺼 둡니다** (나중 단계).

### A-2. 전체 화면 패널 만들기

1. **BossAlarmRoot** 우클릭 → **UI → Image**  
2. 이름을 **`BossAlarmPanel`** 로 변경합니다.
3. **BossAlarmPanel** 선택 → **Rect Transform**:
   - 왼쪽 위 **Anchor Presets** 박스를 **Alt(또는 Option) 누른 채** 우하단 **Stretch-Stretch** 클릭 (전체 화면).
   - **Left, Right, Top, Bottom** 을 모두 **0** 으로 맞춥니다.
4. 같은 오브젝트의 **Image** 컴포넌트:
   - **Color** 를 검정에 투명도 약 **200~220** 정도 (반투명 딤).
5. **Inspector 맨 위 체크박스(활성)** 를 **끕니다** → `BossAlarmPanel` 이 **비활성** 상태로 시작합니다.  
   (게임 시작 전에는 안 보이다가, 스크립트가 켤 때만 보이게 하기 위함입니다.)

### A-3. 제목 텍스트 (TMP) — 스크롤 **밖**

1. **BossAlarmPanel** 우클릭 → **UI → Text - TextMeshPro**  
2. 이름 **`BossAlarmTitle`**
3. **Rect Transform**: Anchor **Top-Stretch**, Height **72**, Pos Y **-36** (상단 고정).
4. Font Size 32~40, Alignment 가운데.

### A-4. 스크롤 영역 (MainStoryPanel의 StoryScrollView와 동일)

1. **BossAlarmPanel** 우클릭 → **UI → Scroll View**  
2. 이름 **`BossAlarmScrollView`**
3. **Rect Transform** (제목·버튼·초상 여백만 남기고 가운데 크게):
   - Anchor **Stretch-Stretch**
   - **Left 40, Right 40, Top 90, Bottom 100** (값은 레이아웃에 맞게 조정)
4. **Scroll Rect** 컴포넌트:
   - **Horizontal** ✗ (가로로 움직이면 안 됨 — 코드도 Play 시 자동으로 끔)  
   - **Vertical** ✓  
   - **Scrollbar Horizontal** 자식은 **비활성**하거나 삭제해도 됨  
   - **Movement Type** `Elastic` 또는 `Clamped`  
   - **Inertia** ✓  
   - **Content** → 아래 `Content` 오브젝트 연결 확인
5. **Viewport** (Scroll View 자식):
   - **Mask** 컴포넌트가 있는지 확인 (없으면 **Add Component → Mask**)
6. **Content** (Viewport 자식) — **가장 중요**:
   - **Anchor Min** `(0, 1)` **Max** `(1, 1)` → **위쪽 가로로만 늘어남**
   - **Pivot** `(0.5, 1)` → **위에서 아래로** 커짐
   - **Pos X, Y** = `0, 0`
   - **Add Component → Content Size Fitter**  
     - Horizontal: **Unconstrained**  
     - Vertical: **Preferred Size**
   - (코드가 **Vertical Layout Group**도 붙여 특징/패턴을 세로로 쌓습니다)
7. **Content** 우클릭 → **UI → Text - TextMeshPro** → **`BossAlarmTraits`**
   - Word Wrapping ✓, Alignment Top-Left, Font Size 22~28  
   - **Height는 고정하지 말 것** (아래 A-4-2 참고)
8. **Content** 우클릭 → 다시 **Text - TMP** → **`BossAlarmPatterns`**
9. Unity가 만든 **Scrollbar**는 있어도 되고, 없어도 됩니다 (마우스 휠로 스크롤).

#### A-4-2. StoryText / BossAlarmTraits 가 잘리거나 스크롤이 되돌아갈 때

각 **TMP**(스토리 본문, 특징, 패턴)에 대해:

| 항목 | 설정 |
|------|------|
| Rect Transform Anchor | **Top-Stretch** (위 가로 전체) |
| Pivot | **(0.5, 1)** |
| Height | **작은 고정값(예: 80) 쓰지 말 것** — 글이 잘림 |
| Content Size Fitter | **Vertical = Preferred Size** (코드가 Play 시 자동 추가 가능) |
| TMP Overflow | **Overflow** (말줄임 Truncate 아님) |
| Extra Settings | **Margins** Left/Right 10~20 |

**Content**에 **Content Size Fitter (Vertical Preferred)** 가 없으면 스크롤 높이가 0이라 **스크롤이 바로 되돌아옵니다.**

Play 후에도 문제면: **BossAlarmRoot → BossAlarmUI → Scroll Rect** 필드에 `BossAlarmScrollView`를 드래그해 연결.

### A-5. (선택) 보스 초상 이미지

1. **BossAlarmPanel** 우클릭 → **UI → Image**  
2. 이름 **`BossAlarmPortrait`**  
3. **Rect Transform**으로 적당한 크기(예: 200×200) 오른쪽 또는 위쪽에 배치.  
4. **Image → Source Image** 에 스프라이트를 넣을 수 있습니다. 없으면 이 오브젝트를 **삭제**해도 됩니다 (코드가 비어 있으면 숨김).

### A-6. 「다음」 버튼

1. **BossAlarmPanel** 우클릭 → **UI → Button - TextMeshPro**  
2. 이름 **`BossAlarmContinueButton`**
3. **Rect Transform**: 화면 **오른쪽 아래** 앵커 (예: Anchor **Bottom-Right**, Pos X/Y로 여백).
4. 자식 **Text (TMP)** 에 글자 **`다음`** 또는 **`확인`** 입력.

### A-7. BossAlarmUI 필드 연결

**BossAlarmRoot** 선택 → **BossAlarmUI**:

| 필드 | 드래그할 오브젝트 |
|------|------------------|
| Panel | `BossAlarmPanel` |
| **Scroll Rect** | `BossAlarmScrollView` |
| Title Text | `BossAlarmTitle` |
| Traits Text | `Content` 안의 `BossAlarmTraits` |
| Patterns Text | `Content` 안의 `BossAlarmPatterns` |
| Portrait Image | (선택) |
| Continue Button | `BossAlarmContinueButton` |
| Korean Font | (선택) |

> 이름을 위와 같이 맞추면 **Inspector를 비워도** 스크립트가 자식에서 찾습니다.  
> 연결을 직접 해두면 **더 확실**합니다.

### A-8. 다른 UI보다 앞에 그리기

**BossAlarmPanel** 이 메뉴·스토리보다 **위에** 나와야 합니다.

- **Hierarchy**에서 `BossAlarmPanel` 을 **Canvas 자식 목록 맨 아래**로 드래그 (아래일수록 앞에 그려지는 경우가 많음).  
- 또는 **BossAlarmRoot** 전체를 **MainStoryPanel** 아래로 옮겨 순서 조정.

---

## A′ — MainStoryPanel의 StoryScrollView 고치기 (스크롤 되돌아감 / 위 잘림)

메인 스토리와 보스 알리미는 **같은 ScrollRect 규칙**입니다. `MainStoryPanel`만 수정할 때:

### 1) Hierarchy 확인

```
MainStoryPanel
├── StoryTitle          ← 스크롤 밖 (상단 고정)
├── StoryScrollView     ← 여기만 스크롤
│   ├── Viewport        (Mask ✓)
│   │   └── Content     (Anchor 위, Pivot Y=1, CSF Vertical Preferred)
│   │       └── StoryText
└── SkipButton          ← 스크롤 밖
```

`StoryText`가 **Content 밖**(MainStoryPanel 직접 자식)이면 스크롤이 안 됩니다 → **Content 안으로** 옮기세요.

### 2) Content 설정

- Anchor: **Min (0,1), Max (1,1)**  
- Pivot: **(0.5, 1)**  
- **Content Size Fitter** → Vertical **Preferred Size**

### 3) StoryText 설정

- Anchor **Top-Stretch**, Pivot **(0.5, 1)**  
- **Height 80 같은 고정값 제거** → **Content Size Fitter** Vertical Preferred  
- TMP **Overflow = Overflow**, **Word Wrapping** ✓  
- Left/Right 여백 15~30 (Rect 또는 TMP Margin)

### 4) StoryScrollView Rect

- **Top**을 `StoryTitle` 아래까지 (예: Top **-100**)  
- **Bottom**을 `SkipButton` 위까지 (예: Bottom **80**)

### 5) Play

코드가 열릴 때 본문 높이를 다시 계산합니다. 그래도 위가 잘리면 **Viewport**의 Top offset이 너무 크지 않은지 확인하세요.

> 스크린샷처럼 **메인 스토리와 보스 알리미가 동시에** 보이면, 테스트 중 **MainStoryPanel**을 끄거나 스토리 단계만 Play 하세요. 정상 플로우에서는 **한 번에 하나만** 켜집니다.

---

## B부 — HUD에서 호버 툴팁 만들기

### B-1. 체력바 위치 찾기

1. **Hierarchy**에서 **`HUD`** 를 펼칩니다.
2. **Slider** 또는 체력을 표시하는 UI 오브젝트를 찾습니다. (프로젝트마다 이름이 `HealthBar` 등 다를 수 있음)
3. 그 **Slider의 부모**를 기준으로, **오른쪽에 빈 공간**이 있는지 확인합니다.

### B-2. 앵커(묶음) 오브젝트 — 선택

1. **체력 Slider** 의 **부모**를 선택하거나, Slider와 **형제**가 되도록 할 위치를 정합니다.  
   - 가장 단순한 방법: **Slider와 같은 부모** 아래에 새 UI를 만듭니다.

### B-3. 트리거 (호버 영역)

1. 체력바와 **같은 부모**에서: **우클릭 → UI → Image**  
2. 이름 **`BossBriefTrigger`**
3. **Rect Transform**: 체력 Slider **오른쪽**, **40 × 40** 정도.
4. **Image**: `!` 등 **에디터에서 원하는 스프라이트** 직접 지정 (코드가 바꾸지 않음). **Raycast Target** ✓.

### B-4. 툴팁 패널 (트리거 자식, 호버 시 **트리거 오른쪽 아래**에 표시)

1. **BossBriefTrigger** 우클릭 → **UI → Image** → **`BossBriefTooltip`**
2. **Image** 반투명 배경, **처음 비활성**
3. **Rect Transform**은 에디터에서 **Top-Stretch 로 두지 마세요** (그러면 화면 맨 위에 붙음).  
   Play 시 코드가 **중앙 앵커 + 인벤토리처럼 월드 좌표**로 트리거 오른쪽 아래에 붙입니다.

### B-5. 툴팁 안 TMP (자동 세로 정렬)

**BossBriefTooltip** 자식으로 **순서대로**만 만들면 됩니다 (위치·Height 수동 조정 불필요):

1. **`TooltipTitle`**
2. **`TooltipTraits`**
3. **`TooltipPatterns`**

> HUD 툴팁에는 초상 없음 (`TooltipPortrait` 사용 안 함). 보스 그림은 **전면 보스 알리미** `BossAlarmPortrait` 만.

각 TMP: Word Wrap ✓, Font Size는 코드가 Title **22** / 본문 **17** 로 맞춥니다 (Inspector 7처럼 너무 작게 두지 마세요).

코드가 **Vertical Layout Group + Content Size Fitter** 를 붙여 세로로 쌓습니다.

### B-6. BossBriefingHudTip

1. **BossBriefTrigger**에 **`BossBriefingHudTip`** 추가
2. **Tooltip Root** = `BossBriefTooltip`
3. **Tooltip Parent** 비우면 **Canvas** (인벤토리 툴팁과 동일)
4. **Offset From Trigger** 예: `(12, -8)` — 트리거 오른쪽·아래 간격

> 전면 **보스 알리미**는 긴 문구, HUD **호버 툴팁**은 **짧은 문구** (`BossBriefingDefaults` 의 `traitsHud` / `patternsHud`).

### B-8. HUD가 꺼져 있을 때

게임 시작 전·메뉴에서는 **HUD**가 꺼져 있을 수 있습니다.  
**룬 선택 후 플레이가 시작되면** `GameManager`가 HUD를 켜고, 그때 **BossBriefingHudTip** 이 `OnEnable`에서 표시 여부를 갱신합니다.  
별도 작업은 없습니다.

---

## C부 — GameManager에 연결

1. **Hierarchy**에서 **GameManager** 오브젝트 선택.
2. **Inspector → Game Manager (스크립트)** 맨 아래 **Boss briefing** 섹션:
   - **Boss Brief Database** → 비우면 **기본 3보스 문구**(천공의 눈 / 지하 굴착사 / 폭풍의 해룡) 사용.
   - **Boss Alarm UI** → **BossAlarmRoot** (또는 `BossAlarmUI`가 붙은 오브젝트)를 드래그.

3. **Ctrl+S** 로 씬 저장.

---

## D부 — 플레이로 확인하는 순서

1. **Play** 누름.
2. **게임 시작** → 메인 스토리 → **스킵**(또는 끝까지).
3. **보스 알리미** 전체 화면이 뜨는지 확인.
4. **다음** 클릭 → **무기 선택** → **룬 선택** → 스테이지 시작.
5. **HUD**가 보이면 **체력바 옆 `!`** 에 마우스를 올려 **툴팁**이 뜨는지 확인.
6. 마우스를 빼면 툴팁이 **꺼지는지** 확인.

---

## E부 — (선택) ScriptableObject로 문구만 바꾸기

기본 문구 말고 **직접 쓴 설명**을 쓰려면:

1. **Project** 창 빈 곳 **우클릭 → Create → Boss → Boss Brief Profile**  
   - 예: `Brief_HeavenEye`, `Brief_Driller`, `Brief_StormDragon`  
   - 각 에셋에 **Display Name**, **Traits Summary**, **Patterns Hint**, (선택) **Portrait** 입력.

2. **Create → Boss → Stage Boss Brief Database**  
   - 이름 예: `StageBossBriefDatabase`  
   - **Bosses Per Stage** 배열 **Size** 를 스테이지 수만큼 (예: 3).  
   - **Element 0, 1, 2** 에 위에서 만든 프로필을 드래그.

3. **GameManager → Boss Brief Database** 에 이 에셋을 연결.

배열 인덱스는 **`StageManager.stageIndex`** 와 같습니다. (0 = 첫 스테이지)

---

## 자주 생기는 문제

| 현상 | 점검 |
|------|------|
| 보스 알리미가 안 뜸 | `BossAlarmUI`가 붙은 오브젝트가 **비활성이면 안 됨** (루트는 활성). `BossAlarmPanel`만 비활성. `GameManager`에 **Boss Alarm UI** 연결 여부. |
| 무기 선택으로 바로 감 | `BossBriefingRuntime.HasBrief` 가 false — 스테이지 인덱스가 **3 이상**이면 기본 문구가 없어 생략됨. DB 에셋으로 채우기. |
| 호버해도 반응 없음 | **EventSystem** 존재 여부. 트리거 **Image → Raycast Target** ✓. **Canvas**에 **Graphic Raycaster**. |
| 툴팁이 안 보임 | `BossBriefTooltip` 이 **트리거의 직계 자식**인지, 이름이 정확한지. **Tooltip Root** 필드에 수동 연결. |
| 글자가 □ | TMP 폰트에 글리프 없음 — `neodgm` 등으로 바꾸거나 Tools 메뉴의 한글 글리프 추가. |
| 알리미가 다른 UI 뒤에 가림 | Hierarchy에서 **BossAlarmPanel** 순서를 **맨 아래**로. |

---

## 한 줄 체크리스트

- [ ] Canvas 아래 **BossAlarmRoot** + **BossAlarmUI** + **BossAlarmPanel**(비활성) + TMP 3 + 버튼  
- [ ] HUD 아래 **BossBriefTrigger**(Raycast ✓) + 자식 **BossBriefTooltip**(비활성) + TMP + **BossBriefingHudTip**  
- [ ] 씬에 **EventSystem**  
- [ ] **GameManager**에 **Boss Alarm UI** 연결 및 **씬 저장**

이후 문구·레이아웃은 팀 디자인에 맞게만 조정하면 됩니다.
