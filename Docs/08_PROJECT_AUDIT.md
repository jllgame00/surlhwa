# SURLHWA — Project Audit

Last Updated: NOT YET AUDITED

Status:
PENDING

---

# IMPORTANT

이 문서는 실제 Unity Repository 전체를 조사한 결과를 기록한다.

파일명이나 과거 기억만으로 추측하지 않는다.

Audit 단계에서는:

- Code 수정 금지
- Asset 이동 금지
- Asset 삭제 금지
- Scene 수정 금지
- Prefab 수정 금지
- ScriptableObject 수정 금지
- `.meta` 수정 금지

분석과 문서 작성만 수행한다.

---

# 1. Executive Summary

TODO — AUDIT

현재 Project 전체 상태를 요약한다.

포함할 내용:

- 전체 구조 평가
- 가장 큰 구조 문제
- 가장 위험한 Reference 문제
- 가장 먼저 해결해야 할 항목
- Rebuild 난이도
- 현재 Working System의 상태

---

# 2. Repository / Unity Overview

TODO — AUDIT

기록:

- Unity Version
- Main Scene
- Build Scene List
- 주요 Package
- Rendering Pipeline
- Input System 여부
- 주요 Project Setting

---

# 3. Current Assets Tree

TODO — AUDIT

`Assets/` 실제 Directory Tree를 정리한다.

너무 큰 Third Party 내부 파일은 요약 가능.

예:

Assets/
├─ Code/
├─ Prefabs/
├─ ...

각 Directory의 역할도 간단히 설명한다.

4. Code Inventory

TODO — AUDIT

모든 Custom C# Script를 조사한다.

Table:

Path Class Domain Responsibility Dependencies Status

Status:

ACTIVE
LEGACY_CANDIDATE
DEAD_CANDIDATE
UNKNOWN
DO_NOT_TOUCH

파일명만 보고 판단하지 않는다.

실제 Reference와 호출 경로를 확인한다.

5. Manager Audit

TODO — AUDIT

모든 \*Manager Class를 따로 분석한다.

각 Manager:

현재 책임
너무 많은 책임을 가지고 있는가
다른 Manager와 중복되는가
Scene Dependency
Singleton 여부
Runtime 생성 여부
Refactor 필요 여부

특히 확인:

CustomerSpawner
SeatManager
MiniGameManager
BusinessDayManager
RecipeUnlockManager
기타 Manager 6. Scene Inventory

TODO — AUDIT

모든 .unity Scene을 조사한다.

Scene Path Purpose Build Used Current / Test / Backup Risk

특히 확인:

Main Scene
Layout Test Scene
Backup Scene
Prototype Scene
미사용 Scene

삭제하지 않는다.

7. Main Scene Hierarchy Audit

TODO — AUDIT

Main Scene의 주요 Hierarchy를 정리한다.

분석:

Managers
World
Bar Layout
Seats
Markers
Canvas
UI Controller
Result Panel
Codex
Mini Game 관련 Object

중복 Object가 있는지 확인한다.

8. Prefab Inventory

TODO — AUDIT

모든 Custom Prefab 조사.

Prefab Path Main Components Referenced By Status

특히:

Customer Prefab
General Customer
Little Prince
Mini Game Prefab
UI Prefab
Environment Prefab 9. ScriptableObject / Data Inventory

TODO — AUDIT

조사 대상:

CocktailData
Recipe
Order
Customer Data
Special Guest Data
기타 ScriptableObject

정리:

Asset Type Asset Path Referenced By Notes 10. Art Asset Audit

TODO — AUDIT

분류:

Character
Cocktail
Environment
UI
Prop
VFX
Placeholder
Prototype

중복 또는 위치가 불명확한 파일 기록.

실제 삭제 금지.

11. Audio Audit

TODO — AUDIT

분류:

BGM
SFX
Ambience

사용 Reference 확인.

없는 경우 없는 것으로 기록.

12. Customer Runtime Flow

TODO — AUDIT

실제 Code를 따라 Flow를 작성한다.

예:

Start
→ SpawnNextCustomer
→ ...
→ AssignSeat
→ ...
→ NotifyCustomerLeft
→ ...

과거 기억이 아니라
현재 Code 기준으로 작성한다.

13. Seat Runtime Flow

TODO — AUDIT

확인:

Seat Discovery 방식
Tag
Reserve
Release
lastAssignedSeat
sitMarker
approachMarker
exitMarker

새 Architecture에서 유지할 부분과 위험 요소를 구분한다.

14. Bartending Runtime Flow

TODO — AUDIT

Stir / Shake / Build / Float 각각:

Entry Point
Prefab
Script
Data
Input
Result
Exit

을 확인한다.

Completion Status도 평가한다.

Status 예:

WORKING
PARTIAL
PROTOTYPE
BROKEN
UNKNOWN

15. Order / Recipe / Cocktail Flow

TODO — AUDIT

실제 연결:

Customer
→ Order
→ Recipe
→ Cocktail
→ Mini Game
→ Unlock
→ Codex

를 추적한다.

어떤 Data가 Source of Truth인지 확인한다.

16. Codex Flow

TODO — AUDIT

확인:

Unlock State
Cocktail Sprite
Card 생성
Locked State
Save
UI Controller
Mask / Scroll 구조 17. Business Day Flow

TODO — AUDIT

확인:

Current Day
Customer Count
Served / Left
Day End
Result Panel
Next Day
Scene Reload 여부
Persistence 18. Tutorial Flow

TODO — AUDIT

실제 조건 확인:

tutorialCustomerCount
spawnedCount
currentDay
Customer Prefab 선택
Patience 설정
Bartending Method 고정 여부

Known Issue:

Day 2에서 Tutorial 고정 방식이 반복된 이력이 있다.

현재 Code에서 수정되었는지 확인한다.

19. Special Guest Audit

TODO — AUDIT

현재 존재하는 Special Guest 관련 항목 전부 조사.

특히:

littlePrincePrefab
littlePrinceFirstVisitCompleted
specialGuests
SpecialGuestData
ChooseCustomerPrefab()
GetCustomerPrefabForNextSpawn()
Special Guest 선택 우선순위

ChooseCustomerPrefab()가 실제 호출되는지 반드시 확인한다.

Character-specific Logic이 Spawner에 얼마나 존재하는지 기록한다.

20. Save / PlayerPrefs Audit

TODO — AUDIT

Repository 전체에서 다음을 검색한다.

PlayerPrefs.Get
PlayerPrefs.Set
PlayerPrefs.Delete
PlayerPrefs.Save

모든 Key를 Table로 만든다.

Key Type Read Location Write Location Purpose

Known Keys:

currentDay
littlePrinceFirstVisitCompleted

Known Keys도 실제 Code로 다시 검증한다.

21. UI Audit

TODO — AUDIT

주요 UI:

Interaction
Customer Wait
Bartending
Codex
Day Result

확인:

Controller Host
Canvas
Active State
Runtime Search
Serialized References
Runtime-created UI 22. Dependency Problems

TODO — AUDIT

다음 유형을 찾는다.

Circular Dependency
Manager-to-Manager Strong Coupling
FindFirstObjectByType 과다
Runtime AddComponent
Hard-coded Scene Object
Hard-coded Character Rule
Direct PlayerPrefs Access
UI / Gameplay 강결합

각 항목에 Severity:

LOW
MEDIUM
HIGH

를 부여한다.

23. Duplicate Responsibility

TODO — AUDIT

같은 역할을 여러 Class가 수행하는 부분을 찾는다.

예:

Spawn
Progress
Recipe Unlock
UI Creation
Save 24. Dead Code Candidates

TODO — AUDIT

등급:

SAFE_CANDIDATE
LIKELY_DEAD
UNKNOWN
DO_NOT_TOUCH

IMPORTANT:

실제 삭제 금지.

Reference가 불명확하면 UNKNOWN.

25. Legacy / Prototype Candidates

TODO — AUDIT

조사:

Old Scene
Backup Scene
Deprecated Prefab
Temporary Asset
Test Script
Debug Code
Old UI

삭제 여부는 사용자 Decision으로 남긴다.

26. Reference Risk

TODO — AUDIT

이동 또는 Refactor 시 특히 위험한 항목:

Scene Serialized Reference
Prefab Reference
ScriptableObject Reference
UnityEvent
Button OnClick
Tag
Layer
Resources Load
Animator
Sprite Reference
Font Asset 27. Encoding Risk

TODO — AUDIT

한글:

String
Comment
ScriptableObject Text
TMP Font

관련 문제 여부 확인.

문자 깨짐이 있으면 위치 기록.

수정하지 않는다.

28. Recommended Target Asset Structure

TODO — AUDIT

현재 Project 실제 상황을 기준으로
Target Folder Tree를 제안한다.

03_SYSTEM_ARCHITECTURE.md는 방향 참고용이며,
실제 Project에 맞게 조정 가능하다.

29. Recommended Code Structure

TODO — AUDIT

각 Script를 다음처럼 분류한다.

KEEP IN PLACE
MOVE
REFACTOR
MERGE_CANDIDATE
UNKNOWN

실제 변경은 하지 않는다.

30. Recommended Rebuild Order

TODO — AUDIT

가장 Reference Risk가 낮은 순서로
Rebuild Step을 제안한다.

각 Step:

Goal
Files
Risk
Validation
Rollback

을 포함한다.

31. Decisions Needed From User

TODO — AUDIT

Agent가 임의 결정하면 위험한 문제를 정리한다.

예:

Backup Scene 삭제 여부
Old Prefab 삭제 여부
Folder Naming
Save Migration
Story Architecture 선택 32. Suggested First Rebuild Task

TODO — AUDIT

Audit 결과 기준
가장 안전하면서 효과가 큰 첫 번째 작업을 하나 제안한다.

아직 실제 수행하지 않는다.

33. Final Classification Summary

TODO — AUDIT

Category Count
KEEP
MOVE
REFACTOR
DELETE_CANDIDATE
UNKNOWN
DO_NOT_TOUCH 34. Audit Completion Checklist
Assets Tree 확인
모든 Custom Script 확인
Scene 확인
Prefab 확인
ScriptableObject 확인
PlayerPrefs 확인
Runtime Flow 확인
Special Guest 확인
Tutorial 확인
Reference Risk 확인
Dead / Legacy 후보 분류
Target Structure 제안
Rebuild Order 제안
