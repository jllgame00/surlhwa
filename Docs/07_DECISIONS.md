# SURLHWA — Architecture & Design Decisions

Last Updated: 2026-09-10

## ADR-040 — Recipe Knowledge Is Fact-Based Player Progress

**Status:** ACCEPTED / I11A IMPLEMENTATION

### Context

The old `RecipeUnlockManager` treated every catalog recipe as binary locked or
unlocked and persisted a pipe-delimited list in `SURLHWA_UNLOCKED_RECIPES`.
That could not express actionable partial learning and caused the Guide to
either hide a recipe entirely or expose its complete authored details.

### Decision

The existing serialized `RecipeUnlockManager` component remains the single
runtime authority for player recipe knowledge so existing scene references stay
valid. Its active API is `GetKnowledgeState`, `IsKnown`, `GetKnownFacts`, typed
`Learn…` operations, and `MarkKnown`. The state derives monotonically from
facts: no facts is `Unknown`; one or more facts is `Partial`; the explicit
`known` marker is `Known`. `Known` is granted by the existing successful
recipe-match result path rather than inferred from partial facts.

The bounded facts are identity, method, actual `IngredientData` identity,
actual ingredient amount, garnish expectation, and preparation sequence. An
amount fact automatically records its corresponding ingredient fact. Player
progress is persisted as versioned JSON in `SURLHWA_RECIPE_KNOWLEDGE_V1`:
`version` plus per-recipe stable `recipeId`, booleans, and stable ingredient-ID
lists. It stores no display strings or full authored recipe duplication.

`SURLHWA_UNLOCKED_RECIPES` remains a read-only legacy migration input: each
pipe-delimited legacy ID is derived as `Known` during load. New writes target
only the versioned knowledge key. `SURLHWA_FIRST_RECIPE_REVEAL_SHOWN` remains a
separate one-time feedback/UI state. A deliberate debug progress clear removes
both recipe-knowledge and legacy-unlock keys.

### Consequences

`CocktailRecipeData` remains the complete authored technical truth;
`RecipeCatalogData` remains catalog membership/default-known configuration;
`RecipeMatchService`, `RecipePreparationStep`, and runtime preparation state
remain independent gameplay truths. Guide/Codex consumers project only learned
facts. `IngredientAvailabilityData` and its day/request context neither read
nor alter recipe knowledge, preserving both available-but-unknown and
known-but-currently-unavailable states.

## ADR-039 — Ingredient Availability Is Day-Derived and Separate from Recipe Knowledge

**Status:** ACCEPTED / I10 IMPLEMENTATION

`IngredientData` remains stable ingredient identity, display, category, and
sorting data. `IngredientAvailabilityData` owns static normal availability
policy for every shared-shelf ingredient: either a minimum business day or
legacy/test always available. `IngredientAvailabilityContext` supplies the
current business day and an optional dedicated-request temporary override.
`IngredientShelfController` only consumes that evaluation.

Normal availability derives from the existing persisted `currentDay` and
static policy data. No ingredient PlayerPrefs key, stock quantity, shop, or
explicit normal-unlock state is added. Existing `IngredientData.ingredientId`
is already stable for matching; availability evaluates authored asset references
and therefore adds no second identity scheme.

`RecipeConstructibilityService` tests catalog liquid requirements against a
given available IngredientData set without consulting unlock state or mutating
anything. Guide/recipe unlock knowledge remains separate. Garnish remains the
I8 runtime garnish state and is not supply-gated in I10. Dedicated requests can
temporarily provide their required liquid ingredients for that session only.
Manwol is additionally safe because its three existing liquids are legacy
always available. Save/reload reconstructs normal availability from `currentDay`.

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
ACCEPTED / IMPLEMENTED (H5)

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

## Implemented boundary (H5)

`SurlhwaPlayerPrefsStore` is the sole raw `UnityEngine.PlayerPrefs` boundary.
The implemented domain storage owners are:

- `BusinessDayPersistence` — `currentDay`;
- `RecipeProgressPersistence` — `SURLHWA_UNLOCKED_RECIPES` and
  `SURLHWA_FIRST_RECIPE_REVEAL_SHOWN`; and
- `LittlePrinceVisitPersistence` — `littlePrinceFirstVisitCompleted`.

`BusinessDayManager`, `RecipeUnlockManager`, and `CustomerSpawner` retain
their gameplay rules. `SURLHWARegressionTools` uses the same domain APIs.
All legacy PlayerPrefs key strings, primitive types, serialized formats,
missing-key defaults, and immediate-save behavior remain compatible; H does
not add a save-format migration.

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

# ADR-028 — Real-Cocktail-First Catalog

Status:
ACCEPTED / FUTURE DATA ARCHITECTURE

## Context

현재 repository의 설야, 여명, 잔설, 만월과 `testStirOrder` /
`testShakeOrder` / `testBuildOrder` / `testFloatOrder`는 prototype catalog coupling이다.
F1 bartending compatibility audit은 `OrderData.defaultBrewSession`이 legacy transport
path를 통과하지만 normal shelf-backed gameplay의 recipe authority가 아님을 확인했다.
실제 player composition은 IngredientShelf가 만들고, recipe matching은
`CocktailRecipeData` candidate와 비교하며, served cocktail은 matched
`CocktailRecipeData.targetCocktail`에서 나온다.

따라서 current implementation을 final catalog design으로 고정하지 않고, content
replacement가 gameplay code의 cocktail-name-specific branching을 만들지 않도록 해야
한다.

## Decision

다음 방향은 locked다.

- Established real-world cocktails가 future catalog의 primary direction이다.
- Fictional cocktail은 catalog를 지배하지 않으며, SURLHWA Original은 narrative하게
  의미 있는 드문 set으로 남긴다.
- Korean/local recipe는 작은 curated category로 포함한다.
- 현재 prototype drinks는 temporary development content이며 final inclusion이 보장되지
  않는다.
- Future recipe/catalog data에는 하나의 명확한 canonical source of truth가 있어야
  한다.

F1의 current evidence는 `CocktailRecipeData` / recipe catalog layer를 stronger
candidate로 가리키지만, final source-of-truth type, ScriptableObject ownership, metadata
schema는 Data Architecture phase가 명시적으로 결정하고 기록한다.

## Consequences

Future Data Architecture는 다음을 피한다.

- cocktail-name-specific runtime logic
- preparation technique마다 하나의 hardcoded `OrderData` slot
- `OrderData`, `defaultBrewSession`, `CocktailRecipeData`, UI-specific data 사이의
  duplicate recipe definition
- preparation technique가 cocktail identity를 결정한다는 가정
- IBA membership이 recipe matching behavior를 바꾼다는 가정

Future metadata may include display/localized Korean name, origin/category, preparation
technique, ingredients/amounts, garnish, glassware, description, recipe-standard/source,
and catalog classification. 이는 field나 enum을 지금 추가하라는 결정이 아니다.

Stir, Shake, Build, Float는 protected gameplay system으로 유지한다. Future real content는
네 technique를 의미 있게 cover할 수 있도록 curated하되, 균등한 수를 맞추기 위해
real recipe를 왜곡하지 않는다.

## Planning Targets Not Locked

- total cocktail count
- exact IBA/classic list
- exact Korean-local list 및 도화/동해의 selected standard
- exact SURLHWA-original list
- Korean/local recipe의 final source text
- "설화" cocktail의 late-game 또는 ending 역할
- final metadata schema 및 ScriptableObject ownership

## Phase F Boundary

Phase F compatibility cleanup은 proven-safe forwarding/transport surface를 정리할 수
있지만 catalog migration은 하지 않는다. 특히 `defaultBrewSession`은 별도의 approved
serialized migration 전까지 제거하지 않으며, prototype content replacement와
compatibility cleanup을 같은 change에 섞지 않는다.

---

# ADR-029 — Canonical Cocktail, Recipe, Catalog, and Progression Ownership

Status:
ACCEPTED / G2 ARCHITECTURE DECISION

## Context

G1 confirmed that the current runtime is already separated more clearly than
the serialized data model suggests:

- `IngredientShelf` creates the player-authored `BrewSessionData`, which is
  converted to `BrewComposition` for matching.
- `RecipeMatchService` receives a brew plus candidate `CocktailRecipeData`
  values; it does not read `OrderData` or progression state.
- The served cocktail is `matchedRecipe.targetCocktail`.
- `OrderData.targetCocktail`, `CocktailData.miniGameType`, and
  `CocktailData.steps` are not active runtime authorities.
- `RecipeUnlockManager` currently combines recipe catalog membership, unlock
  persistence, candidate supply, guide queries, and first-reveal state.
- `CocktailCodexPanelController` serializes a second recipe-membership list.

The existing prototype catalog must become replaceable with real, Korean-local,
and rare SURLHWA-original content without cocktail-name-specific gameplay code
or duplicate recipe definitions.

## Decision

### Ownership

| Owner | Canonical responsibility | Explicit non-responsibility |
| --- | --- | --- |
| `IngredientData` | Stable ingredient identity, display/localized identity, category, icon, shelf sorting/presentation. | Recipe ratios and cocktail-specific behavior. |
| `CocktailData` | Stable cocktail identity, public display name/description, art, reaction metadata, future cocktail-level presentation metadata. | Recipe method and recipe steps. |
| `CocktailRecipeData` | One technical recipe standard: `recipeId`, target cocktail reference, method, ingredient specification, amounts/ratios, threshold, and discoverability. | Duplicated public cocktail name/description. |
| `OrderData` | Customer/request presentation, reward, and temporary bartending routing. | Cocktail identity, recipe specification, and recipe-match authority. |
| Recipe progression service | Unlock state and persistence. | Independent catalog membership. |

`CocktailData.miniGameType` and `CocktailData.steps` are
`REMOVE_AFTER_MIGRATION`. `CocktailRecipeData.displayName` and `description`
are `MIGRATE_OR_REMOVE_AFTER_UI_MIGRATION`. `OrderData.targetCocktail` and
`steps` are `REMOVE_AFTER_REQUEST_MODEL_MIGRATION`.

### Catalog and progression

The future single recipe-membership source is:

```text
RecipeCatalogData
  entries[]

RecipeCatalogEntry
  recipe : CocktailRecipeData
  unlockedByDefault : bool
```

`RecipeCatalogData` answers which recipes exist. `RecipeCatalogEntry` owns the
game-progression default-unlock configuration. The progression service consumes
that catalog and owns only current unlocked-state persistence. Guide, Codex,
and match candidate queries must derive from the same catalog source.

`CocktailRecipeData.discoverable` remains technical matching/discovery
eligibility. It is distinct from whether a recipe is unlocked.

### Ingredient authoring and runtime matching boundary

Future recipe authoring uses an `IngredientData` reference plus `ratio`.
`RecipeIngredientEntry.category` is `REMOVE_AFTER_MIGRATION` because it has no
active reader. Runtime `BrewComposition` may continue to carry stable
`ingredientId` and amount, so RecipeMatchService does not require a premature
DTO rewrite.

### Method and display policy

`CocktailRecipeData.method` is the canonical authored method of a technical
recipe. `OrderData.miniGameType` remains temporary request/routing data;
`BrewSessionData.method`, `BrewComposition.method`, and the active mini-game
method are runtime-derived/execution contexts. They are intentionally not one
universal field. `CocktailData.miniGameType` is legacy duplicate data.

`CocktailData` owns public cocktail name, description, and art. Recipe Guide
and Codex must later migrate from recipe display fallbacks to this owner. A
future recipe variant/standard label, if needed, is separate from copying the
cocktail display name.

### Identifier policy

- `IngredientData.ingredientId` is a persistence/data-reference-safe ID.
- `CocktailRecipeData.recipeId` remains the PlayerPrefs unlock key.
- A future `CocktailData.cocktailId` is the stable cocktail-identity key.
- IDs use lowercase ASCII `snake_case` (`[a-z0-9]+(_[a-z0-9]+)*`) and are
  unique within their identifier domain.
- Shipped IDs are never casually renamed. Any rename requires an explicit
  old-to-new migration, including PlayerPrefs migration for `recipeId`.

### Request and story boundary

Special/story requests may later reference `CocktailData` or
`CocktailRecipeData` when their semantics require a target, but must not copy
recipe ingredients or ratios. `LittlePrinceFirstVisitOrder` remains untouched;
its current dormant serialization does not define the final request model.

## Compatibility Requirements

Later migrations must preserve:

- Stir, Shake, Build, and Float production play;
- matching of all four prototype recipes during transition;
- existing `recipeId` unlock PlayerPrefs, default Stir unlock, and
  first-recipe-reveal PlayerPrefs until explicitly migrated;
- unlocked-guide filtering by technique;
- locked-but-discoverable recipe matching and unlock popup behavior;
- Codex lock presentation;
- customer reactions from matched `CocktailData.emotionTags`; and
- existing ingredient-ID meaning without silent reinterpretation.

## Deferred Decisions

- Final recipe-standard/source, garnish, glassware, localization, and catalog
  metadata schema;
- whether one cocktail identity may expose multiple recipe standards;
- exact special-customer request model; and
- final cleanup timing for obsolete Order/Cocktail fields and prototype slots.

## G3 Boundary

G3 begins catalog/progression separation only: introduce the catalog asset and
entry type, make progression consume it, and remove Codex's independent
membership list while preserving current IDs and behavior. It does not migrate
recipe ingredient authoring, `OrderData`, or `CocktailData` schema.

---

# ADR-030 — Customer Request and Bartending Routing Ownership

Status:
ACCEPTED / G5A ARCHITECTURE DECISION

## Context

The G5A audit confirms that the current `OrderData` is the Bar-side request
asset, but its prototype fields have different meanings:

- `orderName` is request/minigame presentation;
- `reputationReward` is awarded only after a successful mini-game result and a
  successful recipe match;
- `miniGameType` actively supplies the request route, Recipe Guide context,
  and `MiniGameManager` prefab selection;
- `steps` is not recipe authoring. All five current assets contain one empty,
  default `StirGauge` step, and only `BuildMiniGame` reads its length to append
  `"(1 step)"` to a display title;
- The legacy `targetCocktail` field had no active C# reader. In G5F it is
  normalized as `requestedCocktail`: it is null on the four technique-test
  orders and retains the authored cocktail constraint on the dormant Little
  Prince order.

The current Day 1 flow is intentionally prototype/tutorial coupling:
`InteractionUIController` selects one of four serialized technique slots and
then calls `OrderManager.PlaySpecificOrder`. It is not the final normal or
story-request selection model. `LittlePrinceFirstVisitOrder` is serialized on
the Little Prince prefab. G5G consumes that dedicated request at the
customer-known Brew interaction seam, before generic technique selection is
shown.

## Decision

`OrderData` remains the request-definition type. A rename or split to a new
`CustomerRequestData` type is not justified before a live consumer needs a
semantic that `OrderData` cannot express.

An order/request owns request presentation, completion reward, and an explicit
bartending route when the request is technique-directed. It does not own
cocktail identity, recipe ingredients or ratios, recipe matching thresholds,
recipe unlock state, catalog membership, served-cocktail identity, or customer
reaction tags.

`RecipeMatchService` remains strictly:

```text
BrewComposition + candidate CocktailRecipeData -> RecipeMatchResult
```

It does not receive `OrderData`. The served cocktail remains
`matchedRecipe.targetCocktail`.

### Request semantic categories

| Category | Current status | Required request data | Constraint |
| --- | --- | --- | --- |
| Technique-directed | REQUIRED_NOW for the Day 1 prototype/tutorial | explicit `MiniGameType` route | Does not identify a cocktail or a recipe. |
| Specific cocktail | REQUIRED_LATER when a guest explicitly asks for a cocktail identity | optional `CocktailData` reference | Must not derive a recipe or route through a cocktail-to-recipe reverse lookup. |
| Specific recipe standard | REQUIRED_LATER only when a request explicitly requires one authored standard | optional `CocktailRecipeData` reference | Direct reference is permitted; ingredients, ratios, and matching rules are still read from the recipe owner. |
| Open/emotional/story | REQUIRED_LATER; current customer emotion responses are only a partial prototype | request/story presentation and later evaluation metadata | May accept any fitting served cocktail; it does not duplicate a technical recipe. |
| Tutorial/prototype route | REQUIRED_NOW, PROTOTYPE_ONLY | the four technique test slots and forced route state | Must not become the production order-selection architecture. |

An optional `OrderData.requestedCocktail` is a request constraint, not cocktail
identity authority and not the identity of the cocktail actually served. A null
value means the request has no explicit cocktail constraint. It must not select
a route or recipe, participate in technical matching, or be obtained through a
cocktail-to-recipe reverse lookup. If a future request needs a particular
technical standard, it must carry that direct recipe-standard reference.

### Dedicated request precedence

At the Bar-side Brew interaction seam, a non-forced customer with an authored
dedicated `OrderData` issues that exact request through
`OrderManager.PlaySpecificOrder`. A customer without one continues to the
existing generic prototype technique-selection flow. Day 1 forced tutorial
routes retain precedence and continue to resolve their generic prototype
requests. This selection rule does not read or enforce `requestedCocktail`.

### Specific-cocktail post-match evaluation

`requestedCocktail` is evaluated only after the technical result is known.
The boundary is deliberately:

```text
BrewComposition + candidate CocktailRecipeData -> RecipeMatchResult
current OrderData + RecipeMatchResult            -> RequestEvaluationResult
```

`RecipeMatchService` remains the first, technical operation and does not
receive an order, customer, or request constraint. The Bar-side `OrderManager`
owns invocation because it owns the active request and processes the returned
result. A small pure helper/value type may own the comparison itself; it is not
a global request service or a Bartending dependency.

The first evaluation result is deliberately a constraint result, not a new
overall-order-success flag:

```text
RequestEvaluationStatus
  NotApplicable  // requestedCocktail is null
  Satisfied      // a successful technical match serves the requested cocktail
  Unsatisfied    // no successful technical match, or a different valid cocktail
  InvalidData    // missing active request or required cocktail identity/link
```

`CocktailData.cocktailId`, after non-empty identity validation, is the
comparison key. Direct asset references remain the authored links, but object
reference equality is not the runtime identity contract. A failed
`RecipeMatchResult` is a normal `Unsatisfied` outcome for a specific cocktail
constraint; a successful match with a missing target cocktail or a missing
identity is `InvalidData`, not an ordinary request failure.

The result is transient and must not alter the existing technical consequences:

- recipe unlock/discovery remains based on the technically matched recipe and
  the existing mini-game-success gate, even when a specific request is
  unsatisfied;
- the actual served cocktail remains `matchedRecipe.targetCocktail`, and
  customer emotion reaction continues to use that cocktail's `emotionTags`;
- `MiniGameResult.success` remains mechanical performance, separate from this
  cocktail-identity constraint result; and
- `requestedCocktail` does not cross into Bartending or influence route,
  candidate selection, threshold, score, or recipe lookup.

### G5J specific-cocktail outcome policy

`OrderData.reputationReward` is a **request-completion reward**, not a
recipe-discovery reward or a mechanical-performance score. Its existing
eligibility gates remain the mechanical and technical ones:

```text
MiniGameResult.success
&& RecipeMatchResult.success
&& matched recipe exists
&& active OrderData exists
```

For a request with a completed evaluation, the reward policy is:

| `RequestEvaluationStatus` | `reputationReward` policy |
| --- | --- |
| `NotApplicable` | Preserve the legacy eligibility gate. Generic technique requests must not regress. |
| `Satisfied` | Preserve the legacy eligibility gate. A specific request needs mechanical success, a technical match, and `Satisfied`. |
| `Unsatisfied` | Do not grant the request-completion reward. This adds no numeric penalty. A technical no-match already fails the existing technical gate; the changed case is a mechanically successful, technically valid, wrong cocktail. |
| `InvalidData` | Log diagnostics and preserve the player-safe legacy eligibility gate. Authored/runtime data failure is never player failure. |

`MiniGameResult.repDelta` remains exclusively mechanical and is never changed
by request status. Recipe unlock/discovery remains attached to the actual
technically matched recipe and the existing mini-game-success gate; an
unsatisfied request can still unlock the cocktail actually served. Customer
reaction remains based on that actual cocktail's `emotionTags` and the
mini-game result.

When an `Unsatisfied` specific request changes the reward outcome, it needs
explicit Bar-side player feedback. The existing `ResultToastController` is the
first implementation seam: it can report request mismatch while the existing
unlock popup continues to report an actual recipe discovery. This feedback is
separate from character emotion reaction; it does not invent or replace special
guest dialogue. `InvalidData` remains developer-facing diagnostics plus the
legacy player-safe result presentation.

Little Prince first-visit completion remains the current departure-based
prototype/encounter state for now. It is not coupled to the transient
`RequestEvaluationResult`. Any later story branch must define and persist a
meaningful derived story outcome rather than persist the raw generic evaluation
result. Authored correct/wrong Little Prince response content and final
story-completion semantics remain deferred to the narrative vertical slice.

### G5L first-visit dialogue sequencing

The G5L audit confirms that `LittlePrinceFirstVisitDialogue` contains only
`onEnterLines`: an introduction/request presentation. Its `onSuccessLines` and
`onFailLines` are empty. The next Little Prince slice may therefore activate
only an optional, linear **pre-request** dialogue sequence:

```text
spawn -> seat -> normal interaction prompt -> dedicated pre-request dialogue
-> Brew -> dedicated request -> Bartending -> existing result/reaction -> leave
```

`CustomerController` remains the owner of its optional serialized
`DialogueData` reference. `InteractionUIController` is the smallest owner of
the interaction-panel playback and Brew gate, because it already owns the
active customer, the dialogue panel, and dedicated-request start. Neither
`CustomerSpawner` nor `OrderManager` should acquire dialogue sequencing for
this slice. A dedicated dialogue and a dedicated request remain independently
optional.

There is no authored or runtime-ready first-visit outcome dialogue. The
existing result toast communicates request correctness, and customer reaction
continues to communicate the actual served cocktail/quality. A future story
branch must derive a meaningful serving outcome from the independent mechanical,
technical, and request-evaluation axes when content requires it; it must not
persist `RequestEvaluationResult` itself.

`littlePrinceFirstVisitCompleted` remains a departure flag for the prototype
encounter, not request success or narrative completion. Retry and final story
completion semantics remain deferred.

### Method and runtime boundary

`OrderData.miniGameType` is the authored request-route requirement. It is
distinct from:

- `CocktailRecipeData.method`, the authored method of a technical recipe
  standard;
- `BrewSessionData.method` and `BrewComposition.method`, which are runtime
  execution values produced by the active mini-game; and
- the active mini-game prefab, which is selected from the route (or can be
  overridden by an editor debug mode).

The Bar owns the active customer and active request. Bartending needs only a
narrow execution context: route, request display title, the existing
`RecipeGuideContext`, and callbacks needed for the current flow. Reputation,
story state, and the full request asset remain Bar-side. The existing
`BartendingSession` currently transports the full `OrderData`; a small
`BartendingRequestContext` may replace that transport in a later dedicated
slice, but is not introduced by G5B merely to remove legacy data.

### Current field disposition

| Field | Disposition | Reason |
| --- | --- | --- |
| `orderName` | KEEP | Current request/minigame presentation. It is not cocktail identity. |
| `reputationReward` | KEEP | Request-completion reward, currently gated by mini-game success and recipe match. |
| `miniGameType` | KEEP | Active request route for guide context and prefab selection. |
| `steps` | REMOVE_AFTER_G5B | Empty prototype residue; only its length affects a Build display suffix. |
| `requestedCocktail` | KEEP (OPTIONAL CONSTRAINT) | Optional explicitly requested `CocktailData` identity. G5F normalizes generic technique requests to null and preserves the Little Prince `cocktail_manwol` constraint. It is currently unread; the next post-match slice may use it only as the Bar-side input to `RequestEvaluationResult`, never as a route, recipe, technical matching, or served-cocktail authority. |

`OrderManager.orderPool` has no C# reader. It is a dead scene-local duplicate
of the four test assets and must be removed in a later small scene-authoring
slice; it is not catalog membership.

### Special guest boundary

Little Prince spawn eligibility and the
`littlePrinceFirstVisitCompleted` persistence key remain outside this decision.
G5G activates the existing Little Prince order through the ordinary Bar-side
request seam only. Dialogue sequencing, story evaluation, completion semantics,
and generalized special-guest progress remain separate work.

## Compatibility requirements

Later G5 work must preserve:

- the four current technique routes and their method-prefab mapping;
- `RecipeMatchService` candidate, threshold, and score behavior;
- `recipeId` unlock persistence and first-recipe-reveal persistence;
- the matched recipe to `CocktailData.emotionTags` reaction path;
- the Day 1 forced route sequence; and
- `currentDay` and Little Prince first-visit persistence semantics.

## G5B boundary

G5B is limited to removing `OrderData.steps` and the dead `RecipeStep`
representation after changing `BuildMiniGame` to present the request title
without a fabricated step-count suffix. It does not change request selection,
`miniGameType`, `targetCocktail`, `orderPool`, Little Prince activation,
matching, persistence, Scenes, or Prefabs.

---

# ADR-031 — Bartending Preparation Actions Are Authoritative

**Status:** ACCEPTED — Phase I future direction

## Context

The playable prototype establishes ingredient quantities through UI selection,
then grades a separate technique mini-game. The visible preparation action and
the state used for recipe recognition are therefore not yet one authoritative
player action. The red-team review identified this as a core player-facing
risk, despite the completed data, request, and persistence rebuilds.

## Decision

Phase I adopts this target flow:

```text
Select ingredient / bottle
→ prepare into a container
→ actual action updates container state
→ actual container state is the drink composition
→ perform only the recipe-required technique
→ serve what was made
```

The governing rule is: **what the player pours is what the player made.**
Preparation is gameplay. A technique cannot be followed by an unrelated score
mini-game whose result is disconnected from the drink state.

The target is authentic-feeling, game-scale bartending, not literal fluid,
thermal, or density simulation. Existing prototype Stir, Shake, Build, and
Float implementations remain temporary regression/reference fallbacks until
their replacements pass reachability and runtime validation.

## Consequences

- Future preparation must make transferred quantities and target-container
  state authoritative inputs to recognition and serving.
- Narrative choice answers **what** drink to make; recipe knowledge answers
  **what goes into it**; bartending skill answers **how well it is made**.
- This does not reopen ADR-029 data ownership, ADR-030 Little Prince dialogue
  sequencing, or Phase H persistence contracts.
- It is a future Phase I direction, not a claim that the current runtime has
  already replaced the prototype flow.

---

# ADR-032 — Milliliters Are the Canonical Future Liquid Quantity Unit

**Status:** ACCEPTED — Phase I future direction

## Context

The prototype quantity UI is constrained by `0.5 oz` and `1.0 oz` choices.
Representative classic recipes require arbitrary values such as
quarter-ounce-equivalents, three-quarter-ounce-equivalents, and one-and-a-half
ounces. A future preparation model needs a single precise internal unit while
remaining readable to players familiar with ounce recipes.

## Decision

Future runtime and recipe liquid quantities use milliliters as their canonical
data unit. Ounces remain an allowed presentation and reference unit through UI
conversion. The future model must represent amounts equivalent to `0.25 oz`,
`0.75 oz`, `1.5 oz`, and other appropriate values; the old fixed increment UI
does not constrain Phase I content.

This decision does not set player measurement tolerances, rewrite existing
prototype recipe assets, or authorize a save migration. Matching tolerance and
presentation tuning remain future gameplay decisions.

## Consequences

- Recipe data and actual transfer state can share one unambiguous quantity
  language when Phase I implementation begins.
- UI may present a localized or ounce-oriented value without changing the
  canonical internal value.
- Existing IDs, persistence keys, and current recipe matching contracts remain
  intact until a separately scoped implementation migration is approved.

---

# ADR-033 — Initial Bartending Preparation Vocabulary Is Bounded

**Status:** ACCEPTED — Phase I future direction

## Context

Bartending 2.0 requires enough vocabulary to express a representative classic
cocktail set without recreating a broad real-world bartending simulator.
Feature completeness is not a valid reason to build unused mechanics.

## Decision

The initial intended vocabulary is:

```text
Measure
Pour
Dash
TopUp
AddIce
Stir
Shake
Float
Strain
Garnish
```

`Measure` / `Pour` transfer an actual liquid amount. `Dash` is a discrete
small dispensing action, conceptually one input per dash. `TopUp` uses the
same transfer foundation but targets an appropriate glass-fill condition.
`AddIce` targets a container. `Stir`, `Shake`, and `Float` have the distinct
interactions defined by Bartending 2.0. `Strain` transfers prepared liquid to
the serving glass while retaining ice where applicable. `Garnish` begins as a
simple preparation action, not a precision mini-game.

Build is direct construction in the serving glass and must not retain a
redundant standalone timing test after the drink has already been made.

## Consequences

- This vocabulary is a content-driven target, not a final generalized recipe
  scripting schema.
- New operations are added only when adopted recipes require them.
- Details such as exact TopUp data representation and technique tolerances are
  deliberately deferred to narrow implementation/design slices.

---

# ADR-034 — Ingredient Availability Expands the Shared Recipe Space

**Status:** ACCEPTED — Phase I future direction

## Context

Exposing every ingredient at the start makes the intended learning curve and
catalog expansion weak. Conversely, a prototype pattern where every recipe
uses isolated ingredients creates gates without transferable player knowledge.

## Decision

Future production ingredient availability expands through authored business
days, story progression, unlock events, or other explicitly defined progression
points. The purpose is to expand possible recipe space, not to impose arbitrary
gating. Ingredient and catalog curation should prefer shared ingredients so a
newly introduced ingredient can make multiple understandable recipes possible.

```text
Learn ingredient
→ recognize where it can be used
→ multiple recipes become possible
→ knowledge carries into later encounters
```

## Consequences

- Phase I must validate the learning effect with a representative catalog before
  content scale-up.
- This decision does not introduce ingredient-unlock persistence, new PlayerPrefs
  keys, or a required long-term progression format.
- Existing recipe/catalog/progression ownership remains valid; future content
  work must preserve its boundaries.

---

# ADR-035 — Representative Real/Classic Cocktail Validation Precedes Catalog Scale-Up

**Status:** ACCEPTED — Phase I future direction

## Context

The four current prototype recipes prove architecture and routes, not a final
catalog structure or a compelling preparation loop. Bartending 2.0 must be
tested against recognizable recipes with shared ingredient knowledge before a
large content burden is created.

## Decision

Before production expands to a large catalog, Phase I validates approximately
six to eight curated real/classic cocktails. IBA-recognized recipes are a major
reference direction, not an automatic claim that every future recipe must be
an IBA entry. The set should deliberately exercise shared ingredients,
measurement, containers, Dash, TopUp, Strain, and Float where adopted content
requires them.

The validation answers whether the milliliter model, transfer interactions,
container operations, recipe discovery, and player learning are understandable
and enjoyable. It does not select the final recipe list in advance or authorize
30-plus cocktails before that proof.

## Consequences

- Prototype-only disconnected recipes are not evidence of final catalog scope.
- Recipe reuse is desirable because it creates transferable player knowledge.
- ADR-028 catalog ownership remains authoritative; this decision changes future
  content direction, not its canonical data owner.

---

# ADR-036 — Bartending 2.0 Must Not Generalize Ahead of Adopted Content

**Status:** ACCEPTED — Phase I future direction

## Context

The project needs a physically legible preparation loop, but it is a
student/indie production. Systems built for hypothetical cocktails can delay
the proof that the core loop is fun.

## Decision

Phase I must not preemptively implement:

- Muddle, Blend, Flame, Dry Shake, Reverse Dry Shake, Double Strain, or
  salt/sugar-rim mini-games;
- advanced density, fluid, or thermal simulation;
- a generalized bartending scripting language;
- bespoke Shake rhythm charts for every cocktail; or
- a 30-plus cocktail catalog before representative validation.

Likewise, Phase I does not justify a giant rhythm-game framework, a new
story/ACT framework, or a broad architectural rewrite. A narrow mechanic is
added only when adopted representative content proves it is required.

## Consequences

- Reusable Shake pattern templates are preferred to per-cocktail beatmap
  production unless later content demonstrably needs more.
- The detailed scope and non-goals live in
  `Docs/11_BARTENDING_2_0_DESIGN.md`.
- The next work is a player-facing Phase I proof, not another structural
  rebuild.

---

# ADR-037 — Preparation Expectations Evaluate Evidence, Not Cocktail Contents

**Status:** ACCEPTED — I9B

## Context

Final liquid composition alone cannot distinguish a correct French 75 from the
same liquids prepared in the wrong order. Conversely, duplicating the matcher
inside a sequence evaluator would create competing quantity authorities.

## Decision

`CocktailRecipeData` owns a typed, bounded ordered expected-preparation list.
The active preparation records actual production actions as evidence. Runtime
container contents remain canonical liquid truth, and `RecipeMatchService`
remains the existing liquid-recognition authority. Preparation evaluation checks
action/order/container/semantic actions and garnish expectations only; it never
inserts, repairs, or re-measures cocktail contents.

## Consequences

- Composition recognition, preparation correctness, and technique quality stay
  distinct result axes.
- The schema is deliberately limited to adopted I9B actions, not a generic
  cocktail scripting language.
- Future content adds new action types only when adopted requirements justify
  them.

---

# ADR-038 — Splash and Container-Pour Semantics Remain Content-Bounded

**Status:** ACCEPTED — I9B

## Context

Gin Fizz officially specifies a qualitative `Splash of Soda Water` and a
post-shake `Pour` into its serving glass. Neither fact authorizes inventing an
official ml value or duplicating container-transfer math.

## Decision

`Splash` is a player-triggered, configurable SURLHWA gameplay action with an
actual 10–20 ml liquid transfer (15 ml default); it is not a Dash or an IBA
volume conversion. `PourFromContainer` records the authored pour meaning while
delegating liquid movement to the existing transfer primitive. `GentleStir` is
a short Serving-Glass reuse of the direct spoon-input foundation.

## Consequences

- The actual amount in the Serving Glass remains authoritative.
- Gin Fizz can preserve the official qualitative/pour semantics without a new
  fluid engine or duplicate transfer implementation.
- New York Sour remains deferred until `Few Drops` has an explicitly approved
  amount semantic.

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
