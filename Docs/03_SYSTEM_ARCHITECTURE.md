# SURLHWA — System Architecture

Last Updated: 2026-08-11

---

## 1. 목적

이 문서는 설화의 시스템 책임과 Dependency 방향을 정의한다.

현재 구현 구조와 Target Architecture를 구분해서 기록한다.

리빌딩 과정에서 단순히 Folder만 정리하는 것이 아니라
각 System의 책임을 명확히 나누는 것이 목표다.

---

# PART A — CURRENT ARCHITECTURE

### Architecture Checkpoint

`Docs/08_PROJECT_AUDIT.md`는 Rebuild 시작 시점의 historical snapshot이다. Audit
이후 canonical CustomerSpawner / BusinessDayManager 확정, automatic spawn 복구,
Day 1-only Tutorial gate, Production Asset Migration B2.1~B2.4가 완료됐다.

현재 runtime은 아직 `SampleScene.unity` 한 Scene에 Bar, Bartending, UI,
Progression 책임이 함께 존재한다. 아래 Target Scene Architecture는 구현 완료 상태가
아니며 C0 Boundary Audit 이후 단계적으로 적용한다.

## 2. Current Runtime Flow

현재 알려진 전체 Flow:

CustomerSpawner
↓
SeatManager
↓
CustomerController
↓
Interaction
↓
Order
↓
MiniGameManager
↓
Bartending Mini Game
↓
Result
↓
Recipe / Unlock
↓
BusinessDayManager
↓
DayResultPanel

Rebuild 시작 시점의 상세 Dependency는 Project Audit에 보존되어 있으며, 이후
변경 사항은 현재 Repository와 이 문서의 Architecture Checkpoint를 우선한다.

---

## 3. Customer Domain

현재 주요 Script:

- CustomerSpawner
- CustomerController
- SeatManager
- SeatPoint
- CustomerWaitIndicator

### CustomerSpawner Responsibility

현재 여러 역할을 가지고 있을 가능성이 있다.

- Customer Spawn
- Spawn Timing
- Tutorial Customer
- General Customer
- Special Guest
- Business Day 연결
- Customer Leave
- Next Spawn

문제:

Story Logic까지 누적되면 책임이 지나치게 커질 수 있다.

---

## 4. Bartending Domain

Target Concept:

MiniGameManager
↓
IMiniGame
├─ Stir
├─ Shake
├─ Build
└─ Float

MiniGameManager는 Mini Game 선택과 Lifetime을 담당하고,
각 Mini Game의 세부 Gameplay는 개별 구현체가 담당한다.

---

## 5. Progression

현재 주요 System:

- BusinessDayManager
- RecipeUnlockManager
- PlayerPrefs 기반 Progress

문제:

Persistence 접근 위치가 분산될 가능성이 있다.

---

## 6. UI

현재 알려진 주요 UI:

- InteractionUIController
- CustomerWaitIndicator
- Cocktail Codex
- DayResultPanelController
- Bartending UI

Controller Host와 Visible Panel을 분리한다.

Scene-authored UI를 기본으로 사용하고
필요한 경우에만 Runtime Fallback을 허용한다.

---

# PART B — TARGET ARCHITECTURE

## 7. Target Code Structure

목표 개념 구조:

```text
Assets/_SURLHWA/Code/
├─ Core/
├─ SceneFlow/
├─ Progression/
├─ Customers/
├─ Story/
│  ├─ Dialogue/
│  └─ SpecialGuests/
├─ Bartending/
│  ├─ Common/
│  ├─ MiniGames/
│  ├─ Recipes/
│  └─ Orchestration/
├─ Data/
├─ UI/
├─ Persistence/
├─ Presentation/
├─ Utilities/
└─ Editor/
```

현재 `Assets/Code/Managers/`는 Customer, Bartending, Progression, Core 책임이
혼재한다. Target은 Manager suffix 제거가 아니라 domain ownership을 경로와
dependency로 드러내는 것이다.

예상 분류 방향:

- `BusinessDayManager` → Progression
- `CustomerSpawner` → Customers
- `MiniGameManager`, `OrderManager` → Bartending / Orchestration
- `GameManager` → 실제 책임 Audit 후 Core 또는 removal candidate

Code path 이동과 logic refactor를 같은 작업으로 처리하지 않는다.

## 8. Target Scene Architecture

현재 Production Scene은 `Assets/Scenes/SampleScene.unity` 하나에 Bar gameplay,
Bartending, UI, Progression이 함께 있다. 다음 구조는 현재 구현 상태가 아니라 C0
Audit 이후 단계적으로 도달할 Target이다.

```text
Scenes/
├─ System/
│  └─ Bootstrap.unity
├─ UI/
│  └─ MainMenu.unity
├─ Gameplay/
│  ├─ BarScene.unity
│  └─ BartendingScene.unity
├─ Narrative/
│  └─ EndingScene.unity
└─ Development/
   └─ BarScene_LayoutTest.unity
```

`EndingScene.unity`는 실제 Ending 구현에 공통 Scene이 필요할 때 추가할 Target이다.
지금 빈 Scene을 미리 생성한다는 의미가 아니다.

### Bootstrap

Bootstrap에는 application lifetime이 실제로 필요한 system만 둔다.

- GameBootstrap
- SceneFlowController
- SaveManager
- AudioManager
- TransitionController

구체 component 수와 lifetime은 C0 Audit에서 최소 범위로 확정한다.
`CustomerSpawner`, `BusinessDayManager`, `SeatManager`, `MiniGameManager`,
`InteractionUIController` 같은 scene-local gameplay system은 Bootstrap에 두거나
global singleton으로 승격하지 않는다.

### BarScene

BarScene은 다음을 소유하는 canonical story space다.

- Business Day, Customers, Seats
- Special Guests, Dialogue, Story
- Codex, Day Result
- Bar World와 Bar-specific UI
- Special Guest presentation

Target hierarchy concept는 `_SYSTEMS`, `_WORLD`, `_RUNTIME`, `_UI`, `_CAMERA`다.
세부 hierarchy는 C0와 Scene Structure Phase에서 current serialized dependency를
분석한 뒤 결정한다.

### BartendingScene

BartendingScene은 조주 gameplay 공간을 담당한다. Target hierarchy concept는
`_SYSTEMS`, `_GAMEPLAY`, `_UI`, `_CAMERA`다.

Stir, Shake, Build, Float마다 Scene을 만들지 않는다. 하나의 BartendingScene에서
기존 네 Production MiniGame Prefab을 선택해 instantiate 또는 activate한다.

### Bar ↔ Bartending Flow

```text
BarScene
→ Begin Bartending
→ BartendingSession 전달
→ BartendingScene Additive Load
→ MiniGame 실행
→ MiniGameResult 반환
→ BartendingScene Unload
→ BarScene flow 재개
```

BarScene을 unload하지 않아 current customer, seat occupancy, Business Day state,
served count, Special Guest state와 current Bar runtime state를 유지한다.
BartendingScene은 BarScene 내부 구현을 직접 알지 않고 최소 contract만 사용한다.
`BartendingSession`과 `MiniGameResult`는 conceptual model이며 구체 API는 C0 Audit
이후 결정한다.

### MainMenu, Ending, ACT and Guest Scenes

MainMenu는 BarScene에서 분리하며 New Game, Continue, Settings, Quit, Menu
Presentation을 소유한다.

기본 Ending target은 variation Data를 소비하는 하나의 `EndingScene.unity`다.
엔딩별로 완전히 다른 gameplay environment가 필요한 경우에만 복수 Scene을
재검토한다.

ACT별 Scene 또는 Special Guest별 Scene은 만들지 않는다. ACT progression은 Data,
Progression, Story State가 결정하고 모든 Special Guest는 canonical BarScene에
등장한다.

## 9. Target Asset Structure

목표 예시:

현재 `Art`, `Data`, `Prefabs` production asset은 `Assets/_SURLHWA/` 아래로 이동
완료됐다. `Code`와 `Scenes`는 아직 기존 위치에 있으며 C/E Phase 전에는 target
path를 구현 완료 상태로 해석하지 않는다.

Assets/
└─ \_SURLHWA/
├─ Art/
│ ├─ Characters/
│ │ ├─ General/
│ │ └─ SpecialGuests/
│ ├─ Cocktails/
│ ├─ Environment/
│ ├─ Props/
│ ├─ UI/
│ └─ VFX/
│
├─ Audio/
│ ├─ BGM/
│ ├─ SFX/
│ └─ Ambience/
│
├─ Code/
├─ Data/
│ ├─ Cocktails/
│ ├─ Recipes/
│ ├─ Orders/
│ ├─ Customers/
│ ├─ SpecialGuests/
│ └─ Dialogue/
│
├─ Prefabs/
│ ├─ Customers/
│ ├─ Bartending/
│ ├─ Environment/
│ ├─ UI/
│ └─ VFX/
│
├─ Scenes/
│ ├─ System/
│ ├─ UI/
│ ├─ Gameplay/
│ ├─ Narrative/
│ └─ Development/
│
└─ Settings/

ThirdParty/
TextMesh Pro/

---

# PART C — RESPONSIBILITY RULES

## 10. CustomerSpawner

CustomerSpawner가 해야 하는 것:

- 어떤 Prefab을 Spawn할지 요청
- Spawn
- Seat 요청
- Active Customer 관리
- 다음 Spawn Scheduling

CustomerSpawner가 하지 말아야 하는 것:

- 어린왕자 Story Flag 직접 해석
- 햄릿 등장 조건 직접 해석
- 캐릭터별 ACT 분기
- Save Key 직접 관리
- Cocktail Narrative 판정

---

## 11. SpecialGuestResolver

목표 책임:

현재 Day / Story Progress 등을 기반으로

"지금 우선 등장할 Special Guest가 있는가?"

만 판단한다.

예:

CustomerSpawner
↓
SpecialGuestResolver.TryResolve(...)
↓
Special Guest Prefab or Data
↓
없으면 General Customer

캐릭터별 조건을 Spawner에 Hard Coding하지 않는다.

---

## 12. SaveManager

목표:

PlayerPrefs / File Save 등 실제 저장 방식을
Gameplay System이 직접 알지 않도록 한다.

예:

잘못된 방향:

CustomerSpawner
→ PlayerPrefs.GetInt(...)

BusinessDayManager
→ PlayerPrefs.SetInt(...)

Story
→ PlayerPrefs...

목표:

Gameplay
↓
SaveManager / Progress Service
↓
Storage

---

## 13. GameProgressData

장기적으로 저장할 수 있는 구조 예:

GameProgressData

- currentDay
- reputation
- unlockedRecipes
- specialGuestProgress
- statueProgress
- revisitProgress
- environmentFlags

실제 Serialization 방식은 Rebuild 단계에서 결정한다.

---

## 14. SpecialGuestData

목표:

캐릭터 추가가 Code 추가보다 Data 추가에 가깝게 만든다.

예상 구조:

SpecialGuestData

- id
- displayName
- prefab
- portrait
- theme
- visits

---

## 15. SpecialGuestVisitData

예상 구조:

SpecialGuestVisitData

- visitId
- dialogue
- order
- unlockCondition
- completionCondition
- responseSet
- nextVisit

실제 구현 전 과설계를 피하고
Little Prince Vertical Slice 기준으로 최소 구조부터 만든다.

---

# PART D — DEPENDENCY RULES

## 16. Dependency Direction

권장 방향:

Data
↑
Gameplay Domain
↑
Orchestration
↑
UI

UI가 Gameplay를 호출할 수는 있지만,
Core Gameplay가 특정 UI Hierarchy를 직접 전제로 하는 구조는 최소화한다.

---

## 17. Cross-Domain Rule

가능한 한:

Customers
→ Story의 구체 구현을 모름

Bartending
→ 특정 Character를 모름

Story
→ Mini Game 내부 구현을 모름

UI
→ Save Storage 방식을 모름

하도록 한다.

---

## 18. ScriptableObject Rule

ScriptableObject는 Content Configuration에 사용한다.

적합:

- Cocktail
- Recipe
- Order
- Customer Definition
- Special Guest Definition

주의:

Runtime Mutable State를 ScriptableObject Asset 자체에 저장하지 않는다.

Play 종료 후에도 남아야 하는 Runtime Progress는 Save Data에서 관리한다.

---

## 19. Scene Rule

Scene은 가능한 한 다음 역할만 가진다.

- GameObject Composition
- Serialized Reference Wiring
- Visual Placement
- Scene-specific Configuration

게임의 중요한 Story Rule을
Scene Hierarchy 상태만으로 표현하지 않는다.

---

## 20. Prefab Rule

Prefab은 재사용 가능한 Game Object 구조를 정의한다.

캐릭터별 Gameplay 차이는 가능한 한

Prefab Duplicate + Code Duplicate

보다

Shared Component + Data

로 처리한다.

---

## 21. UI Host Rule

Controller / Tracking Component가 있는 Host는
단순 표시 제어 목적으로 비활성화하지 않는다.

필요한 Child Visual만 Toggle한다.

---

# PART E — ARCHITECTURE BOUNDARIES

## 22. Special Guest Logic and Presentation

Special Guest마다 별도 Scene을 만들지 않는다. 같은 설원의 바에 서로 다른 이야기가
찾아온다는 핵심 경험을 유지하기 위해 모든 Guest flow는 BarScene 안에서 진행한다.

Target responsibility:

```text
SpecialGuestController
→ Who appears
→ Visit number
→ Story conditions
→ Progress state

SpecialGuestPresentationController
→ Entrance / Exit presentation
→ VFX / SFX
→ Camera / Lighting / Screen effects
```

Character gameplay logic가 Scene lighting, camera 또는 global effect를 직접 수정하지
않는다.

Guest-specific 공간 연출은 `BarEnvironmentController`의 temporary override로
처리하는 방향을 사용한다.

```text
Normal Bar
→ Apply Guest Environment Override
→ Guest Sequence
→ Restore Normal Environment
```

Hamlet의 saturation / ambient / pause-like presentation과 Faust의 red lighting /
audio distortion / floor VFX는 별도 Scene이 아니라 이 override의 content다.

Presentation definition 후보는 `EntranceEffect`, `ExitEffect`,
`EnvironmentProfile`, `EntranceSFX`, `Timing`, `CameraEffect`다. 지금 generic
cinematic framework를 만들지 않는다. Little Prince를 먼저 구현하고 두 번째
Special Guest에서 확인된 공통점만 추상화한다.

## 23. Data Responsibility

Target responsibility는 다음과 같다.

| Data | Target authority |
| --- | --- |
| IngredientData | 재료 identity와 정의 |
| CocktailData | 완성 칵테일 identity, display, Codex-facing data |
| CocktailRecipeData | 제작 method, ingredients, ratios, steps |
| OrderData | 손님의 요청, target cocktail, request-specific conditions |

현재 Cocktail / Recipe / Order는 일부 field를 중복 소유한다. 특히
`Recipe_TestShake.method = 3`, `TestShake.method = 0` 불일치는 Known Issue다.
F Phase 전에는 runtime data를 수정하지 않는다.

## 24. Persistence and Runtime State

Target boundary:

```text
Gameplay
→ SaveManager
→ GameProgress
→ Storage
```

초기 backend는 PlayerPrefs여도 된다. 장기적으로 Gameplay code가 storage key와
format을 직접 알지 않게 한다.

Persistent state 후보:

- CurrentDay
- Recipe Unlocks와 Codex Progress
- Special Guest Progress
- Story / Ending flags

Runtime state:

- Current seated customer
- Current seat occupancy
- Currently running mini game
- Temporary interaction state

Runtime state를 편의상 모두 SaveData에 넣지 않는다.

## 25. Lookup and Singleton Boundaries

C0/E Phase에서 `FindObjectOfType`, `FindFirstObjectByType`, `GameObject.Find`,
`Transform.Find` 사용을 조사한다. 모든 Find 사용을 제거하는 것이 목표는 아니지만,
Bar ↔ Bartending, Story ↔ Presentation, Persistence ↔ Gameplay 경계를 숨기기
위한 lookup은 명시적 contract로 교체한다.

global lifetime이 실제 필요한 Save, Scene Flow, Audio만 singleton/global 후보다.
CustomerSpawner, SeatManager, MiniGameManager, InteractionUIController는 기본적으로
scene-local이다.

## 26. Assembly and Namespace

현재 Rebuild에서는 asmdef 세분화를 도입하지 않는다. 현 규모에서는 compile
boundary 관리와 cross-domain setup cost가 얻는 이점보다 클 가능성이 있다. Code
architecture가 안정되고 규모가 커질 때 재검토한다.

Namespace cleanup은 계획되어 있다.

```text
Surlhwa.Core
Surlhwa.Customers
Surlhwa.Bartending
Surlhwa.Story
Surlhwa.UI
Surlhwa.Progression
Surlhwa.Persistence
```

Code file move와 Namespace 변경은 같은 Commit에 넣지 않는다.

```text
Move → Compile → Commit
→ Namespace → Compile → Commit
```

---

# PART F — REBUILD PRINCIPLE

## 27. Architecture Goal

Rebuild의 목적은

"예쁜 Folder Tree"

를 만드는 것이 아니다.

목표는:

- 책임을 찾기 쉽다
- 수정 위치가 명확하다
- 새로운 Character를 쉽게 추가할 수 있다
- 새로운 Cocktail을 Data로 추가할 수 있다
- Save Key가 여기저기 퍼지지 않는다
- Scene Reference가 예측 가능하다
- Test / Prototype / Production 파일이 구분된다

이다.

---

## 28. Overengineering Warning

리빌딩 과정에서 다음을 피한다.

- 필요 없는 Interface 대량 생성
- 실제 사용하지 않는 Generic Framework
- 한 Character를 위해 거대한 Event System 구축
- 모든 Manager를 무조건 Singleton화
- Dependency Injection Framework 도입 자체가 목적이 되는 구조
- 현재 규모에 필요하지 않은 추상화

Little Prince Vertical Slice를
깨끗하게 구현할 수 있는 최소 구조를 우선한다.
