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

실제 Dependency는 Project Audit에서 다시 확인한다.

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

목표 예시:

Assets/\_SURLHWA/Code/

Core/
GameFlow/
Common/

Customers/
CustomerSpawner.cs
CustomerController.cs
SeatManager.cs
SeatPoint.cs
CustomerWaitIndicator.cs

Bartending/
Common/
IMiniGame.cs
MiniGameManager.cs
BartendingResult.cs
Stir/
Shake/
Build/
Float/

Story/
SpecialGuestResolver.cs
StoryProgressService.cs
SpecialGuestCondition.cs

Progression/
BusinessDayManager.cs
RecipeUnlockManager.cs
SaveManager.cs
GameProgressData.cs

Data/
CocktailData.cs
RecipeData.cs
OrderData.cs
CustomerData.cs
SpecialGuestData.cs
SpecialGuestVisitData.cs

UI/
Interaction/
Bartending/
Codex/
DayResult/

Utilities/

실제 Folder명은 Audit 이후 확정한다.

8. Target Asset Structure

목표 예시:

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
│ ├─ Main/
│ ├─ Development/
│ └─ Test/
│
└─ Settings/

ThirdParty/
TextMesh Pro/

---

# PART C — RESPONSIBILITY RULES

## 9. CustomerSpawner

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

## 10. SpecialGuestResolver

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

## 11. SaveManager

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

## 12. GameProgressData

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

## 13. SpecialGuestData

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

## 14. SpecialGuestVisitData

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

## 15. Dependency Direction

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

## 16. Cross-Domain Rule

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

## 17. ScriptableObject Rule

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

## 18. Scene Rule

Scene은 가능한 한 다음 역할만 가진다.

- GameObject Composition
- Serialized Reference Wiring
- Visual Placement
- Scene-specific Configuration

게임의 중요한 Story Rule을
Scene Hierarchy 상태만으로 표현하지 않는다.

---

## 19. Prefab Rule

Prefab은 재사용 가능한 Game Object 구조를 정의한다.

캐릭터별 Gameplay 차이는 가능한 한

Prefab Duplicate + Code Duplicate

보다

Shared Component + Data

로 처리한다.

---

## 20. UI Host Rule

Controller / Tracking Component가 있는 Host는
단순 표시 제어 목적으로 비활성화하지 않는다.

필요한 Child Visual만 Toggle한다.

---

# PART E — REBUILD PRINCIPLE

## 21. Architecture Goal

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

## 22. Overengineering Warning

리빌딩 과정에서 다음을 피한다.

- 필요 없는 Interface 대량 생성
- 실제 사용하지 않는 Generic Framework
- 한 Character를 위해 거대한 Event System 구축
- 모든 Manager를 무조건 Singleton화
- Dependency Injection Framework 도입 자체가 목적이 되는 구조
- 현재 규모에 필요하지 않은 추상화

Little Prince Vertical Slice를
깨끗하게 구현할 수 있는 최소 구조를 우선한다.
