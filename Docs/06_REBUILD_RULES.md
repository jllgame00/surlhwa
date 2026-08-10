# SURLHWA — Rebuild Rules

Last Updated: 2026-08-11

이 문서는 Project Rebuild 중 반드시 지켜야 하는 규칙이다.

Codex 또는 다른 자동화 Agent가 작업할 때도 동일하게 적용한다.

---

# 1. ABSOLUTE RULES

- 기존 정상 동작 기능을 임의로 삭제하지 않는다.
- 분석 없이 파일을 삭제하지 않는다.
- 분석 없이 Scene을 다시 만들지 않는다.
- 분석 없이 Prefab을 다시 만들지 않는다.
- ScriptableObject를 임의로 재생성하지 않는다.
- `.meta` 파일을 임의로 삭제하지 않는다.
- GUID를 불필요하게 변경하지 않는다.
- UserSettings를 작업 대상으로 삼지 않는다.
- Third Party Asset을 이유 없이 수정하지 않는다.
- Packages 관련 파일을 이유 없이 수정하지 않는다.
- Project 전체를 Rewrite하지 않는다.

---

# 2. DOCUMENT FIRST

구조 변경 전에 관련 Docs를 읽는다.

우선순위:

1. 00_PROJECT_VISION.md
2. 01_GAME_DESIGN.md
3. 02_CURRENT_STATE.md
4. 03_SYSTEM_ARCHITECTURE.md
5. 04_CONTENT_ROADMAP.md
6. 05_REBUILD_PLAN.md
7. 06_REBUILD_RULES.md
8. 07_DECISIONS.md
9. 08_PROJECT_AUDIT.md

문서와 코드가 충돌하면:

- Vision / Design 방향은 임의 변경 금지
- Current State는 실제 Code 기준 갱신
- Architecture 변경은 Decision 기록
- 알 수 없는 것은 추측하지 않는다

---

# 3. AUDIT FIRST

기본 작업 순서:

Audit
↓
Plan
↓
Small Change
↓
Compile
↓
Unity Validation
↓
Git Diff
↓
Commit

분석과 수정 작업을 한 번에 섞지 않는다.

---

# 4. DELETION RULE

파일을 삭제하기 전에 반드시:

1. 사용 위치 검색
2. Scene Reference 확인
3. Prefab Reference 확인
4. ScriptableObject Reference 확인
5. Resources / Addressables 등 Runtime Load 확인
6. Audit 문서 기록
7. 사용자 승인

을 거친다.

확실하지 않으면:

UNKNOWN

으로 둔다.

---

# 5. MOVE RULE

Asset 이동은 작은 단위로 한다.

가능하면 Unity Editor 내부에서 이동한다.

File System을 직접 사용하는 경우:

Asset

- .meta

를 함께 이동한다.

Move 후:

- Compile
- Console
- Missing Script
- Missing Reference
- Scene
- Prefab

을 확인한다.

---

# 6. CODE PATCH RULE

중요:

- UTF-8 유지
- 한글 문자열 / 주석 깨짐 금지
- 필요한 부분만 최소 Patch
- 이유 없는 파일 전체 Rewrite 금지
- `�`, `?�` 같은 깨진 문자가 생기면 작업 중단
- 기존 기능을 깨뜨리지 않는다

---

# 7. RESPONSIBILITY RULE

Refactor 시 단순히 코드를 다른 파일로 옮기는 것보다
책임을 명확히 하는 것을 우선한다.

한 Class에 Domain이 다른 책임이 여러 개 있으면 기록한다.

하지만 한 번에 모든 책임을 분리하지 않는다.

작은 단위로 이관한다.

---

# 8. WORKING GAME RULE

Rebuild 중에도 가능한 한
Main Gameplay Loop가 항상 실행 가능한 상태를 유지한다.

Large Bang Rewrite 금지.

Old System 삭제 전에
New System 정상 동작을 먼저 확인한다.

---

# 9. SAVE RULE

기존 Save Key를 무단으로 변경하지 않는다.

현재 확인 Key:

- currentDay
- littlePrinceFirstVisitCompleted

다른 Key는 Audit에서 확인한다.

Save 구조 변경 시 Migration 필요 여부를 검토한다.

---

# 10. STORY RULE

Project Architecture Refactor를 이유로
Story Design을 임의 변경하지 않는다.

다음은 Source of Truth:

`04_CONTENT_ROADMAP.md`

Story 상 충돌을 발견하면 해결하지 말고
OPEN DECISION으로 보고한다.

---

# 11. SCENE RULE

Scene을 통째로 Rewrite하지 않는다.

Scene 수정 시:

- 변경 목적
- 변경 GameObject
- Serialized Reference
- Before / After

를 확인한다.

특히 Main Scene은 작은 단위로 수정한다.

---

# 12. PREFAB RULE

기존 Prefab이 정상 동작한다면
"더 깔끔해 보인다"는 이유만으로 새 Prefab으로 교체하지 않는다.

Prefab Variant / Data 분리가 필요한 경우 먼저 분석한다.

---

# 13. SCRIPTABLEOBJECT RULE

기존 Asset GUID를 가능한 한 유지한다.

ScriptableObject Type 변경 시
기존 Serialized Data 손실 위험을 먼저 확인한다.

---

# 14. UI RULE

Controller Host를 표시 제어 목적으로 비활성화하지 않는다.

Visible Child Panel을 Toggle한다.

Scene UI를 Runtime Code가 무조건 재생성하도록 만들지 않는다.

---

# 15. TEST RULE

각 Phase 완료 후 최소 확인:

- Compile Error 0
- Missing Script 없음
- Broken Reference 없음
- Main Scene Open
- Play 시작 가능

Gameplay 변경 Phase라면 추가:

- Spawn
- Seat
- Interaction
- Bartending
- Leave
- Next Customer
- Day Result
- Next Day

---

# 16. GIT RULE

작업 시작:

`git status`

작업 완료:

`git status`
`git diff`

예상하지 않은 파일이 변경되면
Commit 전에 원인을 확인한다.

`UserSettings/EditorUserSettings.asset` 같은 Local Setting은
의도하지 않았다면 Commit하지 않는다.

---

# 17. ONE PURPOSE PER COMMIT

한 Commit은 하나의 목적을 가진다.

좋은 예:

`refactor: extract special guest resolution`

나쁜 예:

`refactor project and fix ui and add story and delete old stuff`

---

# 18. AUTOMATION AGENT RULE

Codex가 판단할 수 없는 항목은
사용자에게 Decision Needed로 보고한다.

자동으로 결정하면 안 되는 대표 항목:

- 실제로 사용 중인지 불명확한 Asset 삭제
- Story 순서 변경
- Main Scene 교체
- Save Key 변경
- Public API 변경
- Data Migration
- Final Folder Naming의 큰 변경

---

# 19. STOP CONDITIONS

다음 상황에서는 작업을 중단한다.

- 대규모 Missing Reference 발생
- Scene Serialization 손상
- GUID 대량 변경
- 한글 Encoding 손상
- 예상하지 않은 Asset 삭제
- Main Scene Load 실패
- Compile Error 원인이 불분명
- 기존 Gameplay Loop가 갑자기 작동하지 않음

원인을 분석한 뒤 다음 작업을 결정한다.

---

# 20. ARCHITECTURE BOUNDARY RULES

- MiniGame마다 Scene을 만들지 않는다. 하나의 BartendingScene과 method-specific
  Prefab을 사용한다.
- ACT마다 Scene을 만들지 않는다.
- Special Guest마다 Scene을 만들지 않는다. Guest content는 canonical BarScene에서
  진행한다.
- Gameplay logic과 Presentation 책임을 분리한다.
- Scene 연결을 쉽게 만들기 위해 scene-local gameplay system을 global singleton으로
  승격하지 않는다.
- Bar ↔ Bartending, Story ↔ Presentation, Persistence ↔ Gameplay 경계는 hidden
  cross-scene Find가 아니라 최소한의 명시적 contract를 우선한다.
- 책임 refactor 전에 현재 runtime baseline과 serialized ownership을 보존한다.
- Asset Move, Namespace Change, Logic Refactor, Persistence Migration을 하나의
  Commit에 섞지 않는다.
- Code file move를 먼저 Compile / Commit하고 Namespace 변경은 다음 Commit에서
  수행한다.
- 하나의 Commit은 하나의 structural purpose만 가진다.
- 현재 규모에서는 asmdef 세분화를 기본 해결책으로 도입하지 않는다.
- 빈 Bootstrap, Ending, Guest, ACT Scene을 target tree를 맞추기 위해 미리 만들지
  않는다.
