# Cursor 모드 & 모델 선택 가이드

> Cursor IDE의 Agent / Plan / Debug / Multitask / Ask 모드와 모델 선택 방법 정리  
> 작성일: 2026-05-29

---

## 한눈에 보기: 모드 선택 가이드

| 모드 | 역할 | 파일 수정 | 언제 쓰나 |
|------|------|-----------|-----------|
| **Ask** | 코드베이스 탐색·설명 | ❌ 읽기 전용 | 구조 이해, 설계 논의 |
| **Plan** | 구현 계획 수립 | ❌ (계획만) | 큰 작업 전 설계 |
| **Agent** | 직접 구현·수정 | ✅ | 기능 추가, 리팩터, 버그 수정 |
| **Debug** | 런타임 버그 추적 | ✅ (로그·수정) | 재현 어려운 버그 |
| **Multitask** | 병렬 서브에이전트 | ✅ | 독립 작업 여러 개 동시 |

**추천 워크플로우:** `Ask → Plan → Agent → Debug`

---

## 1. Ask (질문 모드)

### 특징

- 코드베이스를 **읽기만** 함 (파일 수정 없음)
- "이 코드가 뭐 하는지", "어디에 있는지" 탐색에 적합
- Agent보다 안전하고, Plan보다 가볍게 쓸 수 있음

### 효과적인 사용

- 구현 전에 구조를 먼저 파악할 때
- 여러 접근법의 장단점을 비교할 때
- "이 함수 어디서 호출돼?" 같은 탐색

### 프롬프트 예시

```
@RuneEffectRegistry.cs 이 파일의 역할과
EffectVampire가 등록되는 흐름을 설명해줘.
수정은 하지 말고, 관련 파일 목록만 알려줘.
```

```
WaveManager와 BiomeGimmickSpawner 사이에
데이터가 어떻게 전달되는지 다이어그램으로 설명해줘.
```

---

## 2. Plan (계획 모드)

### 특징

- 구현 **전**에 단계별 계획을 세움
- 큰 작업을 작은 단위로 나누고, 모호한 부분을 질문함
- 계획을 검토·수정한 뒤 Agent로 넘기는 패턴이 효과적

### 효과적인 사용

- 3개 이상 파일을 건드리는 작업
- 아키텍처 결정이 필요한 기능
- "어디서부터 손대야 할지 모르겠을 때"

### 프롬프트 예시

```
룬 효과 5개(Chain, Explode, Freeze, Spiral, Wave)를
RuneEffectRegistry에 등록하는 작업 계획을 세워줘.

요구사항:
- 기존 EffectVampire 패턴을 따를 것
- RuneDataAccess 수정 범위 포함
- 테스트 방법도 단계에 포함

코드는 아직 수정하지 말고 계획만.
```

```
WaveManager 리팩터링 계획:
- 목표: 스폰 로직과 웨이브 설정 분리
- 제약: public API는 변경하지 않음
- 영향받는 파일 목록과 순서를 정리해줘.
```

---

## 3. Agent (에이전트 모드) — `Ctrl+I`

### 특징

- 파일 수정, 터미널 실행, 테스트까지 **자율 실행**
- 멀티파일 편집, 리팩터, 기능 구현에 적합
- 범위를 넓게 주면 예상 밖으로 갈 수 있음

### 효과적인 사용

- Plan에서 정리한 작업을 실제로 구현할 때
- 범위가 명확한 단일 작업 (1~3개 파일)
- "이렇게 고쳐줘"처럼 구체적인 지시

### 프롬프트 예시 (좋은 형태)

```
EffectChain.cs를 EffectVampire.cs와 같은 패턴으로 구현해줘.

제약:
- RuneEffectRegistry.cs만 등록 추가
- public API 변경 금지
- Assets/Scripts/Features/Rune/ 폴더만 수정

완료 후 컴파일 에러 없는지 확인해줘.
```

```
BiomeGimmickSpawner.cs 45번째 줄 근처 null reference 수정.
다른 파일은 건드리지 말 것.
```

### 피해야 할 프롬프트

```
❌ "룬 시스템 전체 개선해줘"  → 범위가 너무 넓음
❌ "버그 고쳐줘"              → 재현 조건·파일 없음
```

---

## 4. Debug (디버그 모드)

### 특징

- Ask와 달리 **런타임 증거**를 활용 (로그, 스택 트레이스, 가설 검증)
- 가설 → 로그 추가 → 재현 → 수정 흐름
- 재현이 어렵거나 원인이 불분명한 버그에 적합

### 효과적인 사용

- Agent로 고쳤는데 여전히 안 될 때
- 간헐적 크래시, 스레드/상태 관련 버그
- Unity 콘솔 에러, 스택 트레이스가 있을 때

### 프롬프트 예시

```
Unity 콘솔 에러:
NullReferenceException at BiomeGimmickSpawner.Spawn() line 78

재현: 웨이브 3 시작 시 50% 확률로 발생
관련 파일: @BiomeGimmickSpawner.cs @WaveManager.cs

가설을 세우고, 필요하면 Debug.Log 추가해서
원인 찾고 최소 수정으로 고쳐줘.
```

```
EffectFreeze 적용 후 적이 움직이지 않는데
Freeze 해제가 안 되는 것 같아.
EffectFreeze.cs와 적 AI 코드를 추적해서
상태 해제 로직 버그를 찾아줘.
```

---

## 5. Multitask (멀티태스크)

### 특징

- 하나의 요청을 **여러 서브에이전트**로 나눠 **병렬 실행**
- 서로 의존 없는 작업을 동시에 처리
- `/multitask`로 시작하거나 Multitask 모드 선택

### 효과적인 사용

- 서로 독립적인 작업 여러 개
- 대규모 리팩터를 모듈별로 나눠 처리
- 테스트 작성 + 문서 업데이트 + 코드 수정을 동시에

### 프롬프트 예시

```
/multitask 다음 작업을 병렬로 진행해줘:

1. EffectChain, EffectExplode를 RuneEffectRegistry에 등록
2. EffectFreeze, EffectSpiral 구현 및 등록
3. EffectWave 구현 및 등록

각각 EffectVampire 패턴 따르고, 서로 파일 충돌 없게.
```

```
/multitask
- auth 모듈 리팩터
- auth 단위 테스트 추가
- README 업데이트
(세 작업은 서로 독립적)
```

### 비추천

- A 파일 수정 → B 파일이 A에 의존하는 **순차 작업**은 Multitask보다 Agent 한 번이 나음

---

## 모델 선택

| 모델 | 특성 | 추천 용도 |
|------|------|-----------|
| **Composer 2.5 Fast** | 빠름, Cursor IDE 최적화 | 일상 코딩, Agent, 멀티파일 편집 (기본값) |
| **GPT-5.5 Medium** | 터미널·스크립트 강함 | CI, 셸 자동화, 복잡한 명령 체인 |
| **Codex 5.3 Medium** | 코드 생성 특화 | API/보일러플레이트, 반복 패턴 |
| **Sonnet 4.6 Medium** | 균형 | 일반 코딩, 중간 난이도 |
| **Opus 4.8 High** | 추론·아키텍처 최강 | 대규모 설계, 어려운 버그, 긴 컨텍스트 |
| **Auto Efficiency** | 비용·속도 자동 조절 | 대부분 작업 |
| **Premium Intelligence** | 품질 우선 | 중요한 설계·리뷰 |
| **MAX Mode** | 최고 성능 (비용↑) | 한 번에 맞춰야 하는 어려운 작업 |

### 실무 패턴

- **90%:** Composer 2.5 Fast (기본값)
- **어려운 설계/Plan:** Opus 4.8 High
- **터미널·빌드 자동화:** GPT-5.5 Medium
- **백그라운드·긴 작업:** Composer Standard (설정에서)

---

## Unity / 게임 프로젝트 워크플로우 예시

룬 시스템 작업 기준:

```
1. Ask   → "RuneEffectRegistry 등록 패턴과 EffectVampire 구조 설명해줘"
2. Plan  → "Chain, Explode, Freeze, Spiral, Wave 5개 등록 계획 세워줘"
3. Agent → "Plan 1단계대로 EffectChain만 구현하고 Registry에 등록해줘"
4. Debug → "EffectFreeze 적용 시 NullRef, 스택트레이스 첨부..."
5. Multitask → "/multitask Effect 5개 병렬 구현..."
```

---

## 효과적인 프롬프트 공통 구조

모든 모드에서 아래 템플릿을 쓰면 결과가 안정적이다.

```
[목표] 한 문장으로 무엇을 할지

[컨텍스트] @파일명 또는 관련 코드/에러

[제약]
- 수정할 폴더/파일
- 변경하면 안 되는 API
- 따라야 할 패턴 (예: EffectVampire)

[완료 기준]
- 컴파일 통과
- 특정 동작 확인 방법
```

### Agent 모드 예시

```
EffectChain.cs를 EffectVampire와 동일 패턴으로 구현하고
RuneEffectRegistry에 등록해줘.

@EffectVampire.cs @RuneEffectRegistry.cs 참고.
Assets/Scripts/Features/Rune/ 만 수정.
public API 변경 금지.
```

---

## 빠른 참조: 상황별 모드 선택

| 상황 | 추천 모드 |
|------|-----------|
| 코드 구조가 궁금할 때 | Ask |
| 큰 기능 추가 전 | Plan |
| 파일 수정·구현 | Agent |
| Agent로 고쳤는데 여전히 버그 | Debug |
| 독립 작업 3개 이상 동시에 | Multitask |
| 아키텍처·설계 논의 | Ask + Opus |
| 일상 코딩 | Agent + Composer 2.5 Fast |
