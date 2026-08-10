# SURLHWA REBUILD PLAN

Last Updated: 2026-08-11

Status: ACTIVE EXECUTION PLAN

---

## 1. Purpose

SURLHWA Rebuild의 목표는 프로젝트를 처음부터 다시 만드는 것이 아니다.

```text
Working Prototype
→ Maintainable Game Project
```

현재 playable loop를 보존하면서, Audit으로 증명된 위험을 작은 검증 단위로
해결한다. 이 문서는 작업 순서와 각 Phase의 경계를 기록하는 execution plan이다.

Repository의 실제 상태가 이 문서나 다른 기획 문서와 충돌하면
`Docs/08_PROJECT_AUDIT.md`와 현재 Repository를 우선한다.

## 2. Rebuild Principles

- 현재 정상 기능을 보존한다.
- Big Bang Rewrite를 하지 않는다.
- Audit 기반으로만 구조 변경을 시작한다.
- 하나의 Phase는 하나의 명확한 목적만 가진다.
- Asset, Scene, Code, Data를 동시에 대규모로 변경하지 않는다.
- 모든 Phase 뒤에는 그 Phase에 맞는 Regression을 수행한다.
- 한 Commit은 한 목적만 가진다.
- Delete는 Audit, reference search, review 이후에만 수행한다.
- `.meta`와 GUID를 보존한다.
- Working Game을 항상 유지한다.
- 범용 Framework, Event System, Quest System을 필요 전에 만들지 않는다.

각 Phase의 기본 절차는 다음과 같다.

```text
Analyze
→ Plan
→ Small Change
→ Static Validation
→ Unity Validation
→ Git Diff
→ Commit
```

## 3. Current Rebuild Status

### Completed

- Documentation foundation and recovery (`Docs/00~08`)
- Full Project Audit (`Docs/08_PROJECT_AUDIT.md`)
- Phase A — Runtime Baseline Stabilization
- B1 — Asset Audit / Exact Move Manifest
- B2.1 — Production Art Migration
- B2.2 — Environment Tile Migration
- B2.3 — Gameplay ScriptableObject Data Migration
- B2.4 — Production Prefab Migration

### Runtime Baseline Result

다음은 Manual Regression PASS로 확인됐다.

- canonical CustomerSpawner와 BusinessDayManager 각각 1개
- automatic Customer spawn / seat / interaction / leave / next spawn
- Day 1 Tutorial: Stir → Shake → Build → Float
- Day 2 Tutorial repetition 차단
- Day 2 first spawn: Little Prince
- Editor-only regression state utility

### Asset Migration Result

Production Art, Environment Tiles, Gameplay Data, Production Prefab은 GUID와
serialized reference를 보존하며 다음 root로 이동됐다.

```text
Assets/_SURLHWA/Art
Assets/_SURLHWA/Data
Assets/_SURLHWA/Prefabs
```

Legacy, unknown, Font/TMP, Code, Scene은 아직 후속 Phase 대상이다.

### Known Unresolved Work

- Little Prince Dialogue / Order asset의 runtime 미사용
- general customer pool 비어 있음
- PlayerPrefs responsibility 분산
- Encoding / TMP coverage 문제
- deferred / legacy Asset organization
- Main Scene hierarchy organization
- Code domain restructuring
- Special Guest Story condition 분리
- Cocktail / Recipe / Order responsibility overlap와 TestShake mismatch

### Immediate Next Step

`C0 — Full Architecture Boundary Audit`

기존 B2.5 Development Scene 단순 이동과 B2.6 Main Scene 단순 이동 계획은
`SUPERSEDED`다. Scene을 이동하기 전에 global/scene-local lifetime, Bar ↔
Bartending contract, presentation, persistence boundary를 먼저 확정한다.

## 4. Canonical Rebuild Roadmap

### PHASE A — Runtime Baseline

Status: `COMPLETE`

- A1 Runtime ownership and automatic customer start
- A2 Tutorial state stabilization
- A3 Day 1 / Day 2 baseline regression

### PHASE B — Production Asset Migration

Status:

| Batch | Scope | Status |
| --- | --- | --- |
| B1 | Asset Audit / Exact Move Manifest | COMPLETE |
| B2.1 | Production Art | COMPLETE |
| B2.2 | Environment Tiles | COMPLETE |
| B2.3 | Gameplay Data | COMPLETE |
| B2.4 | Production Prefabs | COMPLETE |
| B2.5 | Direct Development Scene Move | SUPERSEDED |
| B2.6 | Direct Main Scene Move | SUPERSEDED |

B2.5/B2.6은 asset 이동 실패가 아니라 architecture decision 변경으로 대체됐다.
Scene path와 naming은 Phase C에서 boundary를 확정한 뒤 Phase D에서 처리한다.

### PHASE C — Architecture Rebuild

현재 진행할 다음 Phase다.

| Step | Goal |
| --- | --- |
| C0 | Full Architecture Boundary Audit |
| C1 | Final Scene Architecture 확정 |
| C2 | Bootstrap / Scene Flow 최소 구현 |
| C3 | BarScene canonicalization |
| C4 | BartendingScene extraction |
| C5 | Bar ↔ Bartending additive flow와 최소 contract |
| C6 | Scene boundary regression |

C0에서는 current Scene ownership, runtime lifetime, lookup, serialized reference,
persistent/runtime state를 먼저 증명한다. 구현 전 `BartendingSession`과
`MiniGameResult` contract의 최소 범위를 결정한다.

### PHASE D — Scene Structure

| Step | Goal |
| --- | --- |
| D1 | BarScene hierarchy |
| D2 | BartendingScene hierarchy |
| D3 | MainMenu structure |
| D4 | Development Scene 분류와 유지 정책 |
| D5 | Scene naming / path cleanup |

Scene 이동과 hierarchy 변경은 한 Commit에 섞지 않는다. world transform,
PrefabInstance link, serialized reference를 각 step마다 검증한다.

### PHASE E — Code Structure

| Step | Goal |
| --- | --- |
| E1 | Domain classification |
| E2 | Code path migration |
| E3 | Compile regression |
| E4 | Namespace cleanup |
| E5 | Manager responsibility cleanup |
| E6 | Scene lookup dependency cleanup |

Code path move와 Namespace 변경은 별도 Commit이다. asmdef 세분화는 현재 범위에서
도입하지 않는다.

### PHASE F — Data Architecture

| Step | Goal |
| --- | --- |
| F1 | Cocktail / Recipe / Order responsibility 확정 |
| F2 | TestShake mismatch 해결 |
| F3 | Production `Test*` naming cleanup |
| F4 | SpecialGuestData structure |
| F5 | Data validation tooling |

Data responsibility 변경, asset rename, serialized migration을 한 번에 수행하지 않는다.

### PHASE G — Special Guest Architecture

| Step | Goal |
| --- | --- |
| G1 | Special Guest flow / eligibility / progress |
| G2 | Entrance / Exit Presentation |
| G3 | Bar Environment Override |
| G4 | Little Prince vertical implementation |
| G5 | Second guest extensibility validation |

Little Prince 하나를 위해 generic Quest 또는 cinematic framework를 만들지 않는다.
두 번째 Guest 구현에서 실제 공통점을 확인한 뒤 abstraction한다.

### PHASE H — Persistence

| Step | Goal |
| --- | --- |
| H1 | Persistence ownership audit |
| H2 | GameProgress model |
| H3 | SaveManager / storage boundary |
| H4 | direct PlayerPrefs caller migration |
| H5 | persistence regression |

기존 PlayerPrefs key compatibility를 보존한다. persistent state와 scene runtime
state를 구분한다.

### PHASE I — UI Structure

- UI domain cleanup
- Overlay
- Dialogue
- Codex
- Progression UI
- Feedback

Bar-specific UI, Bartending UI, MainMenu UI의 Scene ownership을 분리하고 UI Host
Rule을 유지한다.

### PHASE J — Tech / Legacy Cleanup

| Step | Goal |
| --- | --- |
| J1 | TMP / Korean Fonts |
| J2 | Encoding / CP949 |
| J3 | Legacy assets |
| J4 | Dead code |
| J5 | Empty folder cleanup |
| J6 | Migration utility cleanup |

삭제 후보는 다시 reference search와 user review를 거친다. Encoding migration과
gameplay refactor는 같은 Commit에 넣지 않는다.

### PHASE K — Full Regression

최소 검증 범위:

```text
Day 1
→ Stir / Shake / Build / Float
→ Day Result → Codex → Result 유지 → Next Night

Day 2
→ Little Prince first
→ no tutorial repeat

Bartending additive flow
Scene transitions
Customer / Seat runtime state
Recipe / Codex
Persistence
Special Guest flow and presentation
```

---

## 5. Historical Pre-Checkpoint Roadmap

Status: `SUPERSEDED`

아래 Phase 0~13 계획은 Documentation Recovery 시점의 초기 execution history다.
원칙과 검증 항목은 계속 유효하지만 실행 순서는 위 Canonical Roadmap으로
대체됐다.

### Phase 0 — Baseline

#### Goal

현재 Working Prototype과 Git 상태를 확인하고, 변경 전 기준점을 만든다.

#### Status

`COMPLETE`

#### Validation

- Git baseline 확인
- Main Scene / Build Settings 확인
- 최소 gameplay loop 확인

### Phase 1 — Documentation

#### Goal

Vision, Design, Current State, Architecture, Story Roadmap, Rebuild Plan,
Rules, Decisions, Audit을 Source of Truth 체계로 유지한다.

#### Scope

```text
Docs/00~08
```

#### Status

`COMPLETE CANDIDATE` — 04 / 05 복구 변경의 review 및 commit 대기.

#### Validation

- 문서가 실제 Repository와 충돌하지 않는지 Audit 기준으로 확인
- Story Design과 현재 runtime 구현 상태를 분리
- 문서 변경 외 파일이 없는지 Git diff 확인

### Phase 2 — Full Project Audit

#### Goal

Repository의 실제 구조, Script, Scene, Prefab, Data, dependency, persistence,
legacy risk를 증명한다.

#### Status

`COMPLETE`

#### Audit

```text
Docs/08_PROJECT_AUDIT.md
```

### Phase 3 — Runtime Baseline Stabilization

#### Goal

리빌딩 전에 현재 Runtime 흐름에서 명백히 깨진 ownership 및 startup 구성을 먼저
안정화한다.

#### A1 — Runtime Ownership

#### Scope

- duplicate CustomerSpawner
- duplicate BusinessDayManager
- automatic initial customer start

#### Status

`COMPLETE / MANUAL SMOKE TEST PASS`

#### Result

Main Scene의 canonical CustomerSpawner와 BusinessDayManager를 각각 하나로
확정했고, Space 입력 없이 자동 첫 Spawn이 시작되는 것을 확인했다.

#### A2 — Tutorial State Stabilization

#### Goal

```text
Day 1 Tutorial only
Day 2+ no forced tutorial repetition
```

#### Scope

CustomerSpawner, InteractionUIController, Scene reload state 사이의 Tutorial
ownership을 다시 추적하고, Single Source of Truth 방향으로 최소 수정한다.

#### Guardrails

- Little Prince spawn priority를 바꾸지 않는다.
- Dialogue / Order Vertical Slice를 함께 구현하지 않는다.
- general customer pool randomization을 함께 수정하지 않는다.
- persistence facade를 먼저 도입하지 않는다.

#### Validation

- Day 1의 Stir / Shake / Build / Float 순서
- Day 2 첫 Little Prince spawn
- Day 2에서 Day 1 forced interaction route가 반복되지 않음
- Customer leave, seat release, 다음 spawn

#### A3 — Baseline Regression

#### Goal

Day 1부터 Day 2까지의 core flow가 A1/A2 변경 뒤에도 유지되는지 검증한다.

#### Validation

- Application start
- Customer spawn / seat / interaction / leave / next spawn
- Day end / result / codex / next day
- PlayerPrefs compatibility

### Phase 4 — Asset Organization

#### Goal

Project-owned Asset을 일관된 root와 작은 batch로 정리한다.

#### Target Concept

```text
Assets/_SURLHWA/
├─ Art/
├─ Audio/
├─ Code/
├─ Data/
├─ Prefabs/
├─ Scenes/
└─ Settings/
```

Audit 결과에 따라 Development / Legacy / Fonts 구분을 추가할 수 있다. 이 구조는
현재 파일 위치의 사실이 아니라, GUID-safe 이동을 위한 목표안이다.

#### Guardrails

- 작은 batch만 이동한다.
- Unity Editor에서 `.meta`와 GUID를 함께 보존한다.
- 이동 뒤 compile, Scene, Prefab, serialized reference를 검증한다.
- Backup 폴더를 새로 만들기보다 Git history를 우선 사용한다.

### Phase 5 — Scene Hierarchy Organization

#### Goal

Main Scene Hierarchy를 runtime 책임과 world / UI 책임이 보이는 구조로 정리한다.

#### Concept Candidate

```text
_SYSTEMS
_WORLD
_RUNTIME
_UI
_CAMERA
```

#### Guardrails

- Hierarchy 정리만 수행한다.
- Visual position, rotation, scale을 변경하지 않는다.
- world transform과 serialized reference를 보존한다.
- Prefab override를 최소화한다.
- Unity Editor API 기반의 안전한 이동을 우선 검토한다.

### Phase 6 — Code Domain Organization

#### Goal

Managers 중심 구조를 기능 Domain 기준으로 읽기 쉬운 구조로 정리한다.

#### Target Concept

```text
Core/
Customers/
Bartending/
Story/
Progression/
Data/
UI/
Utilities/
```

#### Guardrails

- 먼저 파일 위치와 responsibility를 명확히 한다.
- 대규모 Architecture rewrite와 동시에 하지 않는다.
- namespace 추가, folder move, behavior refactor를 한 Commit에 섞지 않는다.

### Phase 7 — Persistence Centralization

#### Goal

분산된 PlayerPrefs responsibility를 작은 Progress API로 중앙화한다.

#### Target Direction

```text
Gameplay
→ SaveManager / Progress API
→ Storage
```

#### Long-term Data Candidates

```text
GameProgressData
- currentDay
- reputation
- unlockedRecipes
- specialGuestProgress
- statueProgress
- revisitProgress
- environmentFlags
```

#### Decisions Required

PlayerPrefs wrapper와 JSON save 중 프로젝트 규모에 맞는 형식을 선택한다. 기존 key
호환과 migration을 먼저 검증하며, 과도한 Save Framework는 만들지 않는다.

### Phase 8 — Special Guest Architecture

#### Goal

CustomerSpawner에서 Story condition을 분리한다.

#### Target Responsibility

```text
CustomerSpawner
→ asks for next special guest

SpecialGuestResolver
→ decides whether a special guest should appear

Story / Progress
→ owns story state
```

Little Prince 하나로 최소 architecture를 검증한다. 범용 Quest Engine이나 대규모
Event System은 이 Phase의 목표가 아니다.

### Phase 9 — Data Responsibility Cleanup

#### Goal

Cocktail, Recipe, Order가 같은 정보를 중복해서 소유하는 문제를 정리한다.

#### Scope

- CocktailData
- CocktailRecipeData / Recipe Data
- OrderData
- RecipeStep
- MiniGame method resolution
- TestShake inconsistency

#### Guardrail

Data type 변경과 모든 asset migration을 한 번에 하지 않는다. Recipe / method별로
작게 검증하고 Codex, unlock, Bartending 결과를 확인한다.

### Phase 10 — Encoding / TMP Cleanup

#### Goal

Audit에서 확인된 encoding 및 한글 font coverage 위험을 gameplay 변경과 분리해
안전하게 정리한다.

#### Scope

- CP949 source files의 UTF-8 전환
- OrderManager replacement character 점검
- Static Korean TMP asset
- fallback 부재
- LiberationSans 혼용

#### Guardrail

Encoding 수정과 gameplay refactor를 같은 Commit에 섞지 않는다. byte validation과
Korean UI glyph sweep을 별도로 수행한다.

### Phase 11 — Legacy Cleanup

#### Goal

Audit의 `DELETE_CANDIDATE`, `MERGE`, `UNKNOWN` 항목을 다시 검증하고, 승인된
대상만 정리한다.

#### Required Before Deletion

- repository-wide reference search
- Scene reference 확인
- Prefab reference 확인
- ScriptableObject reference 확인
- Resources / runtime load 확인
- Git history 및 user review

`DELETE_CANDIDATE`는 삭제 허가가 아니다.

### Phase 12 — Full Regression

#### Goal

Rebuild 작업 전반이 기존 gameplay loop를 유지하는지 end-to-end로 확인한다.

#### Minimum Checklist

```text
Application Start

Day 1
→ 설야 / Stir
→ 여명 / Shake
→ 잔설 / Build
→ 만월 / Float
→ Day End

→ Result
→ Codex
→ Close Codex
→ Result 유지
→ Next Night

Day 2
→ Little Prince first
→ Tutorial repetition 없음
→ General Customer

Customer leave
Seat release
Next spawn
Recipe unlock
Persistence
```

각 mini game의 complete / fail path, no-seat retry, result panel의 Codex close
동작, 기존 PlayerPrefs key 호환도 포함한다.

### Phase 13 — Narrative Vertical Slice

#### Goal

Rebuild 안정화 후 Little Prince ACT 1을 완결된 최초 Narrative Vertical Slice로
구현한다.

#### Target Flow

```text
Day 2
→ Little Prince Spawn
→ Dedicated Dialogue
→ Dedicated Order
→ Cocktail Choice
→ Bartending Result
→ Character Response
→ Progress Save
→ Leave
→ Later Revisit / Statue / Persistent Bar Change
```

이 Phase부터 Content development를 재개한다. 한 Character로 architecture를
검증한 뒤 다른 ACT로 확장한다.

### Previous Recommended Execution Order — SUPERSEDED

```text
Phase A2 — Tutorial State Stabilization
→ Phase A3 — Baseline Regression
→ Phase 4 — Asset Organization
→ Phase 5 — Scene Hierarchy Organization
→ Phase 6 — Code Domain Organization
→ Phase 7 — Persistence Centralization
→ Phase 8 — Special Guest Architecture
→ Phase 9 — Data Responsibility Cleanup
→ Phase 10 — Encoding / TMP Cleanup
→ Phase 11 — Legacy Cleanup
→ Phase 12 — Full Regression
→ Phase 13 — Little Prince Narrative Vertical Slice
```

이 순서는 Runtime Stabilization 이전의 계획으로 보존한다. 현재 실행 순서와 다음
작업은 Section 4의 Canonical Rebuild Roadmap을 따른다.

## 6. Phase Completion Rule

각 Phase는 완료 전에 다음을 충족해야 한다.

- 목표 범위 밖 변경이 없음
- Static validation 통과
- 필요한 Unity manual validation 통과
- Git diff가 의도한 파일과 serialized 변경만 포함
- Regression checklist의 해당 경로 통과
- rollback 가능한 하나의 목적 commit으로 기록

한 Phase가 끝나기 전 다음 Phase의 구조 변경을 섞지 않는다.

## 7. Git Rule

한 Commit은 한 목적만 가진다.

예시:

```text
docs: restore content and rebuild roadmaps
fix: stabilize tutorial state across days
chore: reorganize project assets
chore: organize main scene hierarchy
refactor: organize customer domain
refactor: centralize progression persistence
refactor: extract special guest resolution
chore: remove confirmed legacy assets
test: verify rebuilt gameplay loop
```

Commit 전에는 `git status`, `git diff --check`, focused diff를 검토한다.
