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

- Documentation foundation (`Docs/00~03`, `06~08`)
- Full Project Audit (`Docs/08_PROJECT_AUDIT.md`)
- Phase A1 — Runtime Ownership / Automatic Customer Start

### Phase A1 Result

다음은 Manual Smoke Test PASS로 확인됐다.

- canonical CustomerSpawner 1개
- canonical BusinessDayManager 1개
- legacy CustomerSpawner component 제거
- standalone duplicate BusinessDayManager 제거
- canonical CustomerSpawner `autoStart` 복구
- Space 입력 없이 첫 Customer 자동 Spawn
- Seat assignment, Interaction, Customer leave 후 다음 Spawn 정상
- Console critical error 없음

### Documentation Recovery State

`Docs/04_CONTENT_ROADMAP.md`와 `Docs/05_REBUILD_PLAN.md`는 Audit 당시 0 byte였다.
이번 복구 변경이 review 및 commit되면 Documentation Phase는 COMPLETE가 된다.

### Known Unresolved Work

- Day 2 Tutorial repetition / forced interaction route ownership
- Little Prince Dialogue / Order asset의 runtime 미사용
- general customer pool 비어 있음
- PlayerPrefs responsibility 분산
- Encoding / TMP coverage 문제
- Project-owned Asset organization
- Main Scene hierarchy organization
- Code domain restructuring
- Special Guest Story condition 분리

## 4. Phase 0 — Baseline

### Goal

현재 Working Prototype과 Git 상태를 확인하고, 변경 전 기준점을 만든다.

### Status

`COMPLETE`

### Validation

- Git baseline 확인
- Main Scene / Build Settings 확인
- 최소 gameplay loop 확인

## 5. Phase 1 — Documentation

### Goal

Vision, Design, Current State, Architecture, Story Roadmap, Rebuild Plan,
Rules, Decisions, Audit을 Source of Truth 체계로 유지한다.

### Scope

```text
Docs/00~08
```

### Status

`COMPLETE CANDIDATE` — 04 / 05 복구 변경의 review 및 commit 대기.

### Validation

- 문서가 실제 Repository와 충돌하지 않는지 Audit 기준으로 확인
- Story Design과 현재 runtime 구현 상태를 분리
- 문서 변경 외 파일이 없는지 Git diff 확인

## 6. Phase 2 — Full Project Audit

### Goal

Repository의 실제 구조, Script, Scene, Prefab, Data, dependency, persistence,
legacy risk를 증명한다.

### Status

`COMPLETE`

### Audit

```text
Docs/08_PROJECT_AUDIT.md
```

## 7. Phase 3 — Runtime Baseline Stabilization

### Goal

리빌딩 전에 현재 Runtime 흐름에서 명백히 깨진 ownership 및 startup 구성을 먼저
안정화한다.

### A1 — Runtime Ownership

#### Scope

- duplicate CustomerSpawner
- duplicate BusinessDayManager
- automatic initial customer start

#### Status

`COMPLETE / MANUAL SMOKE TEST PASS`

#### Result

Main Scene의 canonical CustomerSpawner와 BusinessDayManager를 각각 하나로
확정했고, Space 입력 없이 자동 첫 Spawn이 시작되는 것을 확인했다.

### A2 — Tutorial State Stabilization

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

### A3 — Baseline Regression

#### Goal

Day 1부터 Day 2까지의 core flow가 A1/A2 변경 뒤에도 유지되는지 검증한다.

#### Validation

- Application start
- Customer spawn / seat / interaction / leave / next spawn
- Day end / result / codex / next day
- PlayerPrefs compatibility

## 8. Phase 4 — Asset Organization

### Goal

Project-owned Asset을 일관된 root와 작은 batch로 정리한다.

### Target Concept

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

### Guardrails

- 작은 batch만 이동한다.
- Unity Editor에서 `.meta`와 GUID를 함께 보존한다.
- 이동 뒤 compile, Scene, Prefab, serialized reference를 검증한다.
- Backup 폴더를 새로 만들기보다 Git history를 우선 사용한다.

## 9. Phase 5 — Scene Hierarchy Organization

### Goal

Main Scene Hierarchy를 runtime 책임과 world / UI 책임이 보이는 구조로 정리한다.

### Concept Candidate

```text
_SYSTEMS
_WORLD
_RUNTIME
_UI
_CAMERA
```

### Guardrails

- Hierarchy 정리만 수행한다.
- Visual position, rotation, scale을 변경하지 않는다.
- world transform과 serialized reference를 보존한다.
- Prefab override를 최소화한다.
- Unity Editor API 기반의 안전한 이동을 우선 검토한다.

## 10. Phase 6 — Code Domain Organization

### Goal

Managers 중심 구조를 기능 Domain 기준으로 읽기 쉬운 구조로 정리한다.

### Target Concept

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

### Guardrails

- 먼저 파일 위치와 responsibility를 명확히 한다.
- 대규모 Architecture rewrite와 동시에 하지 않는다.
- namespace 추가, folder move, behavior refactor를 한 Commit에 섞지 않는다.

## 11. Phase 7 — Persistence Centralization

### Goal

분산된 PlayerPrefs responsibility를 작은 Progress API로 중앙화한다.

### Target Direction

```text
Gameplay
→ SaveManager / Progress API
→ Storage
```

### Long-term Data Candidates

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

### Decisions Required

PlayerPrefs wrapper와 JSON save 중 프로젝트 규모에 맞는 형식을 선택한다. 기존 key
호환과 migration을 먼저 검증하며, 과도한 Save Framework는 만들지 않는다.

## 12. Phase 8 — Special Guest Architecture

### Goal

CustomerSpawner에서 Story condition을 분리한다.

### Target Responsibility

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

## 13. Phase 9 — Data Responsibility Cleanup

### Goal

Cocktail, Recipe, Order가 같은 정보를 중복해서 소유하는 문제를 정리한다.

### Scope

- CocktailData
- CocktailRecipeData / Recipe Data
- OrderData
- RecipeStep
- MiniGame method resolution
- TestShake inconsistency

### Guardrail

Data type 변경과 모든 asset migration을 한 번에 하지 않는다. Recipe / method별로
작게 검증하고 Codex, unlock, Bartending 결과를 확인한다.

## 14. Phase 10 — Encoding / TMP Cleanup

### Goal

Audit에서 확인된 encoding 및 한글 font coverage 위험을 gameplay 변경과 분리해
안전하게 정리한다.

### Scope

- CP949 source files의 UTF-8 전환
- OrderManager replacement character 점검
- Static Korean TMP asset
- fallback 부재
- LiberationSans 혼용

### Guardrail

Encoding 수정과 gameplay refactor를 같은 Commit에 섞지 않는다. byte validation과
Korean UI glyph sweep을 별도로 수행한다.

## 15. Phase 11 — Legacy Cleanup

### Goal

Audit의 `DELETE_CANDIDATE`, `MERGE`, `UNKNOWN` 항목을 다시 검증하고, 승인된
대상만 정리한다.

### Required Before Deletion

- repository-wide reference search
- Scene reference 확인
- Prefab reference 확인
- ScriptableObject reference 확인
- Resources / runtime load 확인
- Git history 및 user review

`DELETE_CANDIDATE`는 삭제 허가가 아니다.

## 16. Phase 12 — Full Regression

### Goal

Rebuild 작업 전반이 기존 gameplay loop를 유지하는지 end-to-end로 확인한다.

### Minimum Checklist

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

## 17. Phase 13 — Narrative Vertical Slice

### Goal

Rebuild 안정화 후 Little Prince ACT 1을 완결된 최초 Narrative Vertical Slice로
구현한다.

### Target Flow

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

## 18. Recommended Execution Order

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

이 순서는 Scope와 dependency가 바뀌면 Audit 근거와 함께 갱신할 수 있다. 단,
현재 다음 실제 작업은 `Phase A2 — Tutorial State Stabilization`이다.

## 19. Phase Completion Rule

각 Phase는 완료 전에 다음을 충족해야 한다.

- 목표 범위 밖 변경이 없음
- Static validation 통과
- 필요한 Unity manual validation 통과
- Git diff가 의도한 파일과 serialized 변경만 포함
- Regression checklist의 해당 경로 통과
- rollback 가능한 하나의 목적 commit으로 기록

한 Phase가 끝나기 전 다음 Phase의 구조 변경을 섞지 않는다.

## 20. Git Rule

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
