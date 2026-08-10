# SURLHWA — Architecture & Design Decisions

Last Updated: 2026-08-11

이 문서는 프로젝트에서 중요한 결정을
"무엇을 선택했는가"뿐만 아니라
"왜 그렇게 선택했는가"까지 기록한다.

Format:

ADR-XXX
Status
Context
Decision
Reason
Consequences

---

# ADR-001 — Story First

Status:
ACCEPTED

## Context

설화는 경영 시스템의 형태를 가지고 있지만
프로젝트의 핵심 목적은 Narrative와 Emotion 전달이다.

## Decision

Story / Emotion을 Management보다 높은 우선순위로 둔다.

## Reason

게임의 차별점은 경제 Simulation이 아니라
문학적 Character와 Cocktail을 통한 감정적 Interaction에 있다.

## Consequences

- 경제 압박 최소화
- UI에서 KPI 강조 최소화
- Narrative 콘텐츠 우선 개발

---

# ADR-002 — Front-facing Bar Layout

Status:
ACCEPTED

## Context

기존 Bar Layout은 Snow Background 비중이 높고
실제 Gameplay 공간이 작았다.

향후 Statue / Persistent Object를 배치하기 어려웠다.

## Decision

정면형 사각 공간과
ㄷ자 / U-shaped Bar Counter를 사용한다.

## Reason

- Character 가독성
- UI 배치
- Seat 관리
- Statue 공간
- Production 난이도
- 확장성

## Consequences

Snow는 Main Gameplay Area가 아니라
분위기 Frame 역할을 한다.

---

# ADR-003 — SeatPoint Is Logic, Not Visual

Status:
ACCEPTED

## Context

SeatPoint GameObject에 Renderer가 없어
Visual Chair가 필요한지 혼동이 있었다.

## Decision

SeatPoint는 Gameplay Logic Object로 유지한다.

Chair Visual은 별도 Object로 관리할 수 있다.

## Reason

Gameplay 좌석 위치와 Visual을 분리하면
Art 교체 및 Layout 변경이 쉽다.

---

# ADR-004 — Seat Discovery Uses Explicit SeatPoint Identity

Status:
CURRENT IMPLEMENTATION / REVIEW DURING REBUILD

## Context

현재 SeatManager는 SeatPoint Tag 기반으로
활성 좌석을 탐색한다.

Tag 누락으로 Found seats: 0 문제가 발생한 적이 있다.

## Decision

현재 Rebuild 전까지 Tag 기반 구조를 유지한다.

Audit에서 더 안전한 Reference 방식이 필요한지 검토한다.

## Reason

현재 정상 동작 중인 구조를 Rebuild 전에 불필요하게 변경하지 않는다.

---

# ADR-005 — UI Controller Host Stays Active

Status:
ACCEPTED

## Context

InteractionUIController Host 자체를 비활성화하면
Runtime Search에서 Controller를 찾지 못했다.

## Decision

Controller Host는 항상 Active 상태를 유지한다.

표시 여부는 Child Panel에서 제어한다.

## Consequences

UI Hierarchy를 만들 때
Controller와 Visual Toggle 대상을 구분해야 한다.

---

# ADR-006 — Scene-authored Day Result First

Status:
ACCEPTED

## Context

기존에는 DayResultPanel이 없을 경우
Runtime UI를 생성하는 Fallback이 존재했다.

Runtime UI는 Editor에서 디자인하기 어렵다.

## Decision

Priority:

1. Explicit Inspector Result Panel
2. Scene DayResultPanelController
3. Runtime Fallback

## Reason

Editor 기반 UI 제작 편의성과
Runtime 안전성을 동시에 유지한다.

---

# ADR-007 — Tutorial Is Day 1 Only

Status:
ACCEPTED / IMPLEMENTED / MANUAL REGRESSION PASS

## Context

spawnedCount만 사용하면 Scene Reload 후
다음 Day에서도 Tutorial이 반복될 수 있다.

## Decision

Tutorial 여부는 Day와 Customer Count를 함께 고려한다.

Target:

currentDay == 1
AND
spawnedCount < tutorialCustomerCount

CustomerSpawner는 Day와 spawn progression으로 Tutorial Customer eligibility를
판정하고, InteractionUI의 forced route도 같은 `CurrentDay == 1` eligibility와
각 route의 consumed state를 함께 사용한다.

## Consequences

Day 2부터 General / Special Guest Flow로 전환한다.

---

# ADR-008 — Special Guest Logic Must Leave CustomerSpawner

Status:
TARGET

## Context

현재 Little Prince 조건이 CustomerSpawner에 추가되기 시작했다.

7명의 Special Guest와 Revisit까지 추가하면
Spawner가 Story Logic을 과도하게 보유하게 된다.

## Decision

Special Guest Eligibility 판단을
별도의 Story / Resolver 책임으로 분리한다.

Target:

CustomerSpawner
→ SpecialGuestResolver
→ Story Progress

## Reason

새 Character를 추가할 때
CustomerSpawner를 수정하지 않는 구조를 지향한다.

---

# ADR-009 — Persistence Must Be Centralized

Status:
TARGET

## Context

현재 PlayerPrefs 접근이
BusinessDayManager / CustomerSpawner 등에 분산되기 시작했다.

## Decision

Progress 접근 API와 Save 책임을 중앙화한다.

Target:

```text
Gameplay
→ SaveManager
→ GameProgress
→ Storage
```

Persistent state에는 CurrentDay, unlock, Codex, Special Guest progress, Story /
Ending flag를 둔다. current seated customer, seat occupancy, running mini game 같은
Scene runtime state는 기본적으로 SaveData에 넣지 않는다.

## Reason

ACT / Revisit / Statue가 추가될수록
Key가 기하급수적으로 늘어날 수 있다.

## Alternatives Rejected

- 각 gameplay class가 PlayerPrefs key와 serialization format을 계속 직접 소유
- Scene runtime state를 편의상 모두 persistent model에 포함

초기 storage backend로 PlayerPrefs를 유지하는 것은 허용한다. 이 Decision은 backend
교체가 아니라 ownership boundary에 관한 것이다.

---

# ADR-010 — Data-driven Content

Status:
ACCEPTED / EXPANDING

## Context

Cocktail / Recipe 등 콘텐츠가 계속 늘어날 예정이다.

## Decision

새 콘텐츠는 가능한 한
Code Hard Coding보다 ScriptableObject / Data Asset으로 정의한다.

적용 대상:

- Cocktail
- Recipe
- Order
- Customer
- Special Guest
- Visit

## Consequences

Runtime State와 Static Content Data를 구분해야 한다.

---

# ADR-011 — Rebuild Is Incremental

Status:
ACCEPTED

## Context

현재 프로젝트는 이미 Playable Loop를 가지고 있다.

새 프로젝트로 다시 시작하면
정상 기능을 재구현해야 하고 Regression 위험이 높다.

## Decision

현재 Repository를 유지하면서
단계적으로 구조를 개선한다.

## Reason

Working Software를 보존한다.

---

# ADR-012 — Documentation Lives Outside Assets

Status:
ACCEPTED

## Context

개발 문서를 Assets 내부에 넣으면
Unity Asset Database가 불필요하게 Import한다.

## Decision

Project Root의 `Docs/`를 사용한다.

구조:

SURLHWA/
├─ Assets/
├─ Docs/
├─ Packages/
└─ ProjectSettings/

## Reason

Unity Runtime Asset과
Human Documentation을 분리한다.

---

# ADR-013 — Private Repository

Status:
ACCEPTED

## Context

설화는 현재 개발 중인 개인 프로젝트이며
Story / Source 전체를 공개할 필요가 없다.

## Decision

GitHub Repository는 Private 상태를 유지한다.

외부 Portfolio에서는
기술 구조와 결과물만 선택적으로 공개한다.

---

# ADR-014 — Internal Story Roadmap Can Be Detailed

Status:
ACCEPTED

## Context

외부 Portfolio에서는 Story Secret을 최소 공개하지만
내부 개발에는 상세 Story 정보가 필요하다.

## Decision

`04_CONTENT_ROADMAP.md`에는
ACT / Revisit / Trigger 방향을 상세 기록한다.

외부 자료는 별도로 작성한다.

---

# ADR-015 — Prototype UI Mockups Are References, Not Implementation Proof

Status:
ACCEPTED

## Context

향후 완성형 Stir / Shake / Build / Float,
Cocktail Codex 등의 Visual Mockup이 제작되었다.

## Decision

Mockup 이미지는 Visual Direction Reference로 사용한다.

Current Implementation State 판단에는 사용하지 않는다.

## Reason

Concept Art와 Runtime Implementation을 혼동하지 않기 위해서다.

---

# ADR-016 — Target Scene Architecture

Status:
ACCEPTED / TARGET IMPLEMENTATION

## Decision

Target Scene은 Bootstrap, MainMenu, BarScene, BartendingScene과 필요 시 하나의
EndingScene으로 구성한다. BarScene_LayoutTest는 Development로 분류한다.

Bootstrap에는 Save, Scene Flow, Audio, Transition처럼 실제 global lifetime이
필요한 최소 system만 둔다. EndingScene은 필요해질 때 생성한다.

## Reason

application lifetime, menu, persistent Bar runtime, isolated Bartending gameplay의
수명과 책임을 구분하기 위해서다.

## Alternatives Rejected

- 모든 system과 UI를 하나의 Main Scene에 계속 누적
- target tree를 맞추기 위한 빈 Scene 선행 생성
- CustomerSpawner나 BusinessDayManager 같은 scene-local system의 Bootstrap 이동

---

# ADR-017 — One BartendingScene with Four MiniGame Prefabs

Status:
ACCEPTED / TARGET IMPLEMENTATION

## Decision

Stir, Shake, Build, Float는 하나의 BartendingScene에서 기존 네 Production Prefab을
선택해 실행한다.

## Reason

조주법은 공통 session, UI, result contract를 공유하며 Scene lifetime을 따로 가질
이유가 없다.

## Alternatives Rejected

- StirScene, ShakeScene, BuildScene, FloatScene 네 개 운영
- 네 MiniGame을 BarScene hierarchy에 영구 배치

---

# ADR-018 — Additive Bartending Flow

Status:
ACCEPTED / TARGET IMPLEMENTATION

## Decision

BarScene을 유지한 채 BartendingScene을 additive load한다. 최소
`BartendingSession`을 전달하고 `MiniGameResult`를 반환한 뒤 BartendingScene을
unload한다. 구체 API는 C0 Audit 이후 확정한다.

## Reason

current customer, seat occupancy, Business Day count, Special Guest state와 Bar
runtime state를 unload 없이 보존하기 위해서다.

## Alternatives Rejected

- 조주 시작마다 BarScene unload / reload
- BartendingScene이 BarScene의 manager와 hierarchy를 직접 탐색하고 조작

---

# ADR-019 — No Scene per Special Guest

Status:
ACCEPTED

## Decision

모든 Special Guest는 canonical BarScene에 등장한다.

## Reason

설화의 핵심 경험은 같은 설원의 바에 서로 다른 이야기가 찾아오고 그 흔적이 한
공간에 누적되는 것이다.

## Alternatives Rejected

- LittlePrinceScene, ChunhyangScene, HamletScene 등 Guest별 Scene
- Guest 연출마다 Bar의 복제 Scene 생성

---

# ADR-020 — No Scene per ACT

Status:
ACCEPTED

## Decision

ACT progression은 Data, Progression, Story State가 결정하며 ACT별 Scene을 만들지
않는다.

## Reason

ACT는 공간 ownership이 아니라 narrative progress의 단위다.

## Alternatives Rejected

- Act1_LittlePrince.unity, Act2_Chunhyang.unity 같은 ACT별 Scene
- Scene 이름이나 Scene 존재 여부로 Story progress 판정

---

# ADR-021 — Separate Special Guest Logic and Presentation

Status:
ACCEPTED / TARGET IMPLEMENTATION

## Decision

SpecialGuestController 계열은 등장 인물, visit, condition, progress를 담당하고,
SpecialGuestPresentationController 계열은 entrance, exit, VFX, SFX, camera,
lighting, screen effect를 담당한다.

## Reason

Story rule이 Scene-specific visual 구현에 결합되면 Character 추가와 연출 변경이
서로를 깨뜨리기 때문이다.

## Alternatives Rejected

- Character gameplay controller가 global camera와 lighting을 직접 변경
- 처음부터 범용 cinematic/event framework 구축

---

# ADR-022 — Guest Environment Uses Bar Override

Status:
ACCEPTED / TARGET IMPLEMENTATION

## Decision

Hamlet, Faust 등의 공간 연출은 `BarEnvironmentController`가 normal profile에
temporary guest override를 적용하고 sequence 뒤 복원하는 방식으로 구현한다.

## Reason

BarScene을 canonical story space로 유지하면서 Guest별 saturation, ambient,
lighting, audio distortion, floor VFX를 독립적으로 제어하기 위해서다.

## Alternatives Rejected

- Guest마다 alternate Bar Scene 생성
- 복원 계약 없이 각 Character script가 environment object를 직접 변경

---

# ADR-023 — Domain-based Code Organization

Status:
ACCEPTED / TARGET IMPLEMENTATION

## Decision

Code는 Managers 중심이 아니라 Core, SceneFlow, Progression, Customers, Story,
Bartending, Data, UI, Persistence, Presentation, Utilities, Editor domain으로
분류한다.

## Reason

Manager suffix나 단일 Managers folder는 실제 책임과 dependency 방향을 드러내지
못한다.

## Alternatives Rejected

- 모든 orchestration class를 Managers folder에 계속 누적
- 파일 이동과 logic responsibility refactor를 한 Commit에서 수행

---

# ADR-024 — Singleton Only for True Global Lifetime

Status:
ACCEPTED

## Decision

Save, Scene Flow, Audio처럼 실제 application lifetime이 필요한 system만
singleton/global 후보로 둔다. CustomerSpawner, SeatManager, MiniGameManager,
InteractionUIController는 기본적으로 scene-local이다.

## Reason

Scene boundary 연결을 위해 singleton을 사용하면 ownership과 lifetime 오류가
숨겨지기 때문이다.

## Alternatives Rejected

- 모든 Manager를 singleton으로 변환
- `FindFirstObjectByType`가 하나를 골라주는 것을 canonical ownership으로 사용

---

# ADR-025 — asmdef Segmentation Deferred

Status:
DEFERRED

## Decision

현재 Rebuild에서는 Assembly Definition 세분화를 도입하지 않는다. Code
architecture가 안정되고 규모가 커지면 재검토한다.

## Reason

현재 규모에서는 dependency maintenance, compile boundary, cross-domain setup
비용이 개발 편의보다 클 가능성이 높다.

## Alternatives Rejected

- domain folder를 옮기는 즉시 domain별 asmdef 추가

---

# ADR-026 — Namespace Cleanup Follows Code Path Migration

Status:
ACCEPTED / PLANNED

## Decision

`Surlhwa.Core`, `Surlhwa.Customers`, `Surlhwa.Bartending`, `Surlhwa.Story`,
`Surlhwa.UI`, `Surlhwa.Progression`, `Surlhwa.Persistence` 방향으로 namespace를
정리하되 Code file move 다음의 별도 Commit에서 수행한다.

## Reason

path migration과 symbol/reference rewrite를 분리해야 compile regression의 원인을
작게 추적할 수 있다.

## Alternatives Rejected

- 49개 file move와 namespace 변경을 한 Commit에 수행

---

# ADR-027 — Architecture Phase Supersedes Direct Scene Moves

Status:
ACCEPTED

## Decision

기존 B2.5 Development Scene 단순 이동과 B2.6 Main Scene 단순 이동 계획을
`SUPERSEDED`로 처리한다. 다음 작업은 C0 Full Architecture Boundary Audit이다.

## Reason

Scene path를 먼저 바꾸면 current single-scene runtime ownership을 target
Bootstrap / Bar / Bartending boundary로 잘못 고정할 수 있다.

## Alternatives Rejected

- 기존 manifest 순서대로 Scene을 먼저 이동한 뒤 architecture를 재설계

---

# OPEN DECISIONS

## OD-001 — Save Format

후보:

- PlayerPrefs Wrapper
- JSON Save
- Other Local Serialization

Rebuild에서 실제 Progress 규모를 확인한 후 결정.

---

## OD-002 — Hamlet / Faust Story Order

Hamlet이 Faust Statue를 보는 연출과
현재 ACT 번호에 충돌 가능성이 있다.

`04_CONTENT_ROADMAP.md` 참고.

---

## OD-003 — Final Special Guest Condition Architecture

Condition을:

- Interface
- ScriptableObject
- Enum / Data
- Resolver Code

중 어떤 수준으로 추상화할지
Little Prince Vertical Slice를 기준으로 결정한다.

과설계를 피한다.
