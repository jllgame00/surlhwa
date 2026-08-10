# SURLHWA — Current Development State

Last Updated: 2026-08-11

Status:
ACTIVE DEVELOPMENT / ARCHITECTURE REBUILD

이 문서는 현재 실제 구현 상태를 기록한다.

Audit 이후 실제 Repository와 충돌하는 내용이 발견되면
추측하지 말고 실제 구현을 기준으로 갱신한다.

---

## 1. Current Milestone

현재 단계:

```text
Core Gameplay Prototype
→ Full Project Audit
→ Runtime Baseline Stabilization
→ Production Asset Migration B2.1~B2.4
→ Architecture Documentation Checkpoint
→ Phase C0 — Full Architecture Boundary Audit
→ Phase C1 — Canonical BarScene Name and Path
```

현재 Branch는 `refactor/project-rebuild`다.

Runtime Baseline Stabilization은 완료됐다. canonical `CustomerSpawner`와
`BusinessDayManager`를 각각 하나로 확정했고, automatic customer loop와 Day 1
Tutorial, Day 2 Tutorial repetition 차단, Day 2 Little Prince first spawn을 Manual
Regression으로 확인했다.

### Architecture Boundary and Canonical Scene

| Phase | Scope | Status |
| --- | --- | --- |
| C0 | Full Architecture Boundary Audit | COMPLETE |
| C1 | Canonical BarScene Name and Path | COMPLETE |

C1은 current production Scene의 identity와 path만 바꿨다. Scene GUID와 YAML
content, hierarchy, serialized reference, runtime C# 및 playable behavior는 보존됐다.
Manual smoke regression도 PASS했다.
현재 canonical production Scene은
`Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity`이며 Build Settings index 0의 유일한
enabled production Scene이다.

### Production Asset Migration

| Batch | Scope | Status |
| --- | --- | --- |
| B2.1 | Production Art | COMPLETE |
| B2.2 | Environment Tiles | COMPLETE |
| B2.3 | Gameplay ScriptableObject Data | COMPLETE |
| B2.4 | Production Prefabs | COMPLETE |

현재 canonical production asset root는 다음과 같다.

```text
Assets/_SURLHWA/Art
Assets/_SURLHWA/Data
Assets/_SURLHWA/Prefabs
```

다음 단계는 `C2 — Bartending Boundary Preparation in Current Single Scene`이다.
현재 BarScene 하나 안에서 Bar와 Bartending 사이의 최소 session/result contract를
먼저 준비한다. BartendingScene 생성, additive loading, MiniGame Scene extraction은
아직 시작하지 않았다.

---

## 2. Main Gameplay Loop

현재 기본적인 Customer / Business Day Loop는
Playable 상태로 확인되어 있다.

현재 확인된 흐름:

Customer Spawn
↓
Seat Assignment
↓
Customer Sit
↓
Wait Indicator
↓
Interaction
↓
Bartending
↓
Result
↓
Customer Leave
↓
Seat Release
↓
Next Customer
↓
Business Day End
↓
Day Result
↓
Codex
↓
Next Day

Phase A Manual Regression에서 automatic spawn, seat, interaction, customer leave,
next spawn, Day 1의 Stir → Shake → Build → Float, Day 2 Little Prince first
spawn과 tutorial repetition 차단을 확인했다.

---

## 3. Current Scene

현재 canonical production Scene:

`Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity`

Build Settings index 0의 enabled production Scene은 BarScene 하나다. Front-facing Bar
Layout이 적용되어 있으며, current runtime architecture는 여전히 Bar와 Bartending
execution이 함께 있는 single-Scene 구조다.

기존 `Assets/Scenes/SampleScene.unity`는 C1에서 canonical BarScene path로 이동됐으며
더 이상 current production Scene이 아니다.

별도 Layout Test Scene이 존재한다.

확인된 과거 경로:

`Assets/BarScene_LayoutTest.unity`

Audit에서 Scene 전체 목록과 실제 사용 여부를 다시 확인한다.

---

## 4. Current Bar Layout

현재 방향:

- Front-facing rectangular room
- ㄷ자 / U-shaped Bar Counter
- Larger interior space
- Reduced snow background dominance
- Customer Seat 영역 확보
- Bartending UI 영역 확보
- Future Statue 영역 확보

Seat / Marker / UI가 새 Layout에 맞게 한 차례 재연결되어 있다.

---

## 5. Customer System

확인된 주요 구성:

- CustomerSpawner
- CustomerController
- SeatManager
- SeatPoint
- CustomerWaitIndicator
- InteractionUIController

CustomerSpawner가 다음을 수행하는 구조가 존재한다.

- Customer Spawn
- Tutorial Customer 처리
- General Customer Prefab 선택
- 동일 Prefab 연속 등장 방지
- Spawn Delay
- Business Day Limit 연동
- Active Customer 관리
- Customer Leave 이후 Next Spawn

---

## 6. Seat System

SeatManager는 `SeatPoint` Tag 기반으로 좌석을 탐색한다.

과거 새 Layout 적용 과정에서

`[SeatManager] Found seats: 0`

문제가 발생했으며,
각 SeatPoint GameObject의 Tag를 수정해 해결했다.

중요:

SeatPoint Tag는 다음 객체 자체에 존재해야 한다.

- SeatPoint_Left
- SeatPoint_Center
- SeatPoint_Right

Parent 또는 Child Marker에 Tag를 적용해서는 안 된다.

SeatPoint는 logical object이며 Renderer가 필수는 아니다.

---

## 7. Interaction UI

InteractionUIController가 있는 Host Canvas / GameObject는
Active 상태를 유지해야 한다.

과거 부모 Canvas를 비활성화하면서
런타임 탐색이 실패해 UI가 표시되지 않는 문제가 있었다.

현재 원칙:

Controller Host = Active ON

Visible Child Panel = 필요할 때 ON/OFF

---

## 8. Customer Patience

일반 손님의 Patience 시스템이 존재한다.

CustomerWaitIndicator는 Customer를 따라가며
화면에 대기 상태를 표시한다.

Serialized Screen Offset은 Inspector 값이 우선하므로
코드 Default 변경만으로 기존 Scene 값이 바뀌지 않을 수 있다.

---

## 9. Bartending

현재 프로젝트에는 다음 조주 시스템의 구현 또는 Prototype이 존재한다.

- Stir
- Shake
- Build
- Float

### Confirmed

Stir:

- 실제 Play Loop에서 사용
- Perfect / Good / Fail 평가 구조가 존재하는 것으로 확인

### Confirmed Runtime Baseline

Stir / Shake / Build / Float:

- 네 조주법 모두 Production Prefab과 runtime 진입 경로가 존재한다.
- Day 1 Manual Regression에서 Stir → Shake → Build → Float 순서로 진입을
  확인했다.
- 향후 BartendingScene 분리는 동작 변경이 아니라 Scene boundary 재구축으로
  진행한다.

중요:

포트폴리오용으로 생성한 완성형 UI Mockup 이미지는
실제 구현 상태를 의미하지 않는다.

---

## 10. Recipe / Cocktail / Codex

현재 다음 구조가 존재한다.

- CocktailData
- Recipe / Order 관련 Data
- Recipe Unlock
- Cocktail Codex
- Cocktail Sprite 연결

초기 Cocktail:

- 설야
- 여명
- 잔설
- 만월

과거 Codex에서 Mask 때문에 Cocktail Image가 보이지 않는 문제가 있었고
UI Mask 구조 수정으로 해결했다.

---

## 11. Business Day

BusinessDayManager가 존재한다.

현재 알려진 역할:

- 하루 손님 수 관리
- Day End
- Result Panel
- Unlock 정보
- Current Day

최근 Current Day 저장 기능이 추가되었다.

현재 PlayerPrefs Key:

`currentDay`

---

## 12. Day Result

Scene-authored DayResultPanel을 우선 사용한다.

현재 우선순위:

1. Inspector에 연결된 Result Panel
2. Scene에 존재하는 DayResultPanelController
3. Runtime Fallback

Result 화면에서 Codex를 열었다가 닫아도
Result 화면이 유지되도록 개선되어 있다.

---

## 13. Special Guest Prototype

현재 첫 번째 Special Guest Prototype으로
어린왕자가 연결되고 있다.

최근 구현:

- CustomerSpawner에 `littlePrincePrefab`
- Day 2 First Spawn 조건
- 방문 완료 Flag
- Prefab 미연결 시 기존 Customer Loop Fallback

현재 PlayerPrefs Key:

`littlePrinceFirstVisitCompleted`

실제 테스트 결과:

Day 2 첫 손님으로 Little Prince Prefab이 등장하는 것을 확인함.

Little Prince 전용 DialogueData / OrderData asset과 Prefab reference는 존재하지만,
현재 interaction runtime에서 전용 Data를 소비하지 않는 Known Issue는 남아 있다.

---

## 14. Known Special Guest Architecture Problem

현재 Little Prince 조건이 CustomerSpawner 내부에 직접 존재한다.

캐릭터가 7명까지 확장되면
CustomerSpawner에 Story Condition이 누적될 위험이 있다.

리빌딩 목표:

CustomerSpawner
→ SpecialGuestResolver
→ Story / Progress

로 책임을 분리한다.

---

## 15. Tutorial / Day Stabilization

Phase A2에서 Tutorial eligibility를 Business Day 기준과 연결했다.

```text
Day 1 AND tutorial progression not finished
→ forced Stir / Shake / Build / Float route 허용

Day 2+
→ forced Tutorial route 차단
```

Status:

`COMPLETE / MANUAL REGRESSION PASS`

---

## 16. Save / Progress

현재 저장은 여러 System에서 PlayerPrefs를 직접 사용한다.

확인된 Key:

- currentDay
- littlePrinceFirstVisitCompleted
- SURLHWA_UNLOCKED_RECIPES
- SURLHWA_FIRST_RECIPE_REVEAL_SHOWN

Target:

SaveManager 또는 이에 준하는 Persistence Layer를 만들어
직접 PlayerPrefs 접근을 중앙화한다.

---

## 17. Current Folder / Asset State

Production Art, Environment Tiles, Gameplay Data, Production Prefab은
`Assets/_SURLHWA/` 아래 canonical domain path로 이동됐다. 이동된 asset의 GUID와
serialized reference는 각 B2 batch에서 보존 검증했다.

다음 deferred / legacy 항목은 아직 기존 위치에 남아 있다.

- `Assets/Assets/Prefab/Character/Customer_B.prefab`
- `Assets/texture/Tiles/Table.prefab`, `wall.prefab`, `Floor.asset`
- legacy minigame JPG assets
- Fonts / TextMesh Pro assets
- `Assets/Code/` 전체
- legacy source folder `Assets/Scenes/` 및 `Assets/BarScene_LayoutTest.unity`
- 비어 있거나 legacy asset만 남은 기존 source folder

이 항목은 사용 관계와 architecture boundary를 재검증하기 전 이동하거나 삭제하지
않는다.

---

## 18. Implemented / Confirmed

현재까지 직접 확인된 핵심:

- Customer Spawn
- Seat Assignment
- Random Seat
- Seat Release
- General Customer Loop
- Customer Patience
- Wait Indicator
- Interaction UI
- Basic Bartending Loop
- Recipe / Cocktail Data 기반
- Recipe Unlock
- Cocktail Codex
- Business Day
- Day Result
- Next Day
- Current Day Persistence Prototype
- Front-facing Main Bar Layout
- Little Prince Day 2 Spawn Prototype

---

## 19. Audit Resolution

위 초기 질문은 Full Project Audit에서 정적 증거 기준으로 조사됐다. 역사적 기준점과
세부 결과는 `Docs/08_PROJECT_AUDIT.md`에 보존한다. 이후 Phase A와 B에서 변경된
현재 상태는 이 문서와 현재 Repository를 우선한다.

아직 후속 Architecture Phase에서 다룰 Known Issues:

- Little Prince Dialogue / Order runtime 미사용
- general customer pool 비어 있음과 `Customer_B` 미사용
- Cocktail / Recipe / Order 책임 중복과 TestShake mismatch
- PlayerPrefs 접근 분산
- Managers 중심 Code 분류와 scene lookup dependency
- Encoding / TMP / Korean glyph coverage
- Scene hierarchy와 Scene boundary 미분리

---

## 20. Not Yet Fully Implemented

현재 Roadmap 기준 아직 남은 주요 작업:

- 확장 가능한 Special Guest Architecture
- Little Prince Full ACT
- 캐릭터별 Cocktail Response
- Statue System
- Persistent Bar Change
- Revisit ACT
- 중앙화된 Save System
- Special Guest Progress Data Model
- Final Character Art
- Final Environment Art
- Final UI Polish
- Sound / Ambience Polish
- Ending Integration

---

## 21. Immediate Next Goal

현재 최우선 목표:

**C2 — Bartending Boundary Preparation in Current Single Scene**

현재 BarScene을 유지한 채 다음 최소 boundary를 먼저 준비한다.

- Bar → Bartending start session
- Bartending → Bar result return
- current Customer와 Business Day state의 BarScene ownership
- Bartending active-state, camera, input transition boundary
- existing direct MiniGameManager dependency를 contract 뒤로 옮길 준비

BartendingScene 생성, additive loading, MiniGame execution Scene extraction은 C2의
범위가 아니다. Bootstrap은 `CREATE_LATER`, MainMenu는 `CREATE_LATER`, EndingScene은
`DEFER`, Special Guest Architecture는 Phase G, Persistence centralization은 Phase H,
AudioManager는 실제 global requirement가 확인될 때까지 deferred 상태를 유지한다.

`BarScene_LayoutTest.unity`는 여전히 `DEVELOPMENT / UPDATE_AFTER_BAR_REBUILD`다.
C1에서 canonical BarScene으로 대체되거나 동기화되지 않았다.
