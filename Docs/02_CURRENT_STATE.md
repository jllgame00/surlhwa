# SURLHWA — Current Development State

Last Updated: 2026-08-11

Status:
ACTIVE DEVELOPMENT / PRE-REBUILD

이 문서는 현재 실제 구현 상태를 기록한다.

Audit 이후 실제 Repository와 충돌하는 내용이 발견되면
추측하지 말고 실제 구현을 기준으로 갱신한다.

---

## 1. Current Milestone

현재 단계:

Core Gameplay Prototype
→ Main Bar Layout Revision
→ Special Guest Prototype
→ Project Rebuild 준비

특별 손님 콘텐츠를 대량으로 추가하기 전에
프로젝트 구조와 데이터 구조를 정리하려는 단계다.

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

최근 Main Scene에서 전체 흐름이 정상 동작하는 것을 직접 테스트했다.

---

## 3. Current Scene

현재 Main Scene:

`Assets/Scenes/SampleScene.unity`

Front-facing Bar Layout이 적용되어 있다.

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

### Needs Audit Verification

Shake / Build / Float:

- 관련 구현 및 Data가 존재한 개발 이력이 있음
- 현재 Repository 기준 정확한 완성도와 사용 파일은 Audit에서 다시 확인한다

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

## 15. Known Tutorial / Day Problem

최근 발견된 문제:

Day 2가 되었음에도
Tutorial 기반 고정 조주 흐름이 다시 적용되는 현상이 있었다.

예상 원인:

`spawnedCount`가 Scene Reload 후 0으로 초기화되면서

`spawnedCount < tutorialCustomerCount`

조건이 다음 Day에서도 다시 참이 되는 구조.

목표 조건:

`currentDay == 1 && spawnedCount < tutorialCustomerCount`

현재 이 수정이 Repository에 최종 반영되었는지,
그리고 실제 Play Test를 통과했는지는 Audit / Regression Test에서 확인한다.

Status:

NEEDS VERIFICATION

---

## 16. Save / Progress

현재 저장은 여러 System에서 PlayerPrefs를 직접 사용할 가능성이 있다.

확인된 Key:

- currentDay
- littlePrinceFirstVisitCompleted

Recipe Unlock 관련 저장 위치 및 Key는 Audit에서 확인한다.

Target:

SaveManager 또는 이에 준하는 Persistence Layer를 만들어
직접 PlayerPrefs 접근을 중앙화한다.

---

## 17. Current Folder / Asset State

현재 프로젝트가 여러 Prototype과 기능 추가를 거치면서
다음 문제가 있다고 판단하고 있다.

- 파일 위치가 일관되지 않음
- Prototype / Production Asset 혼재 가능성
- Test Scene 존재
- Manager 폴더에 다른 Domain 코드 혼재 가능성
- Legacy 파일 존재 가능성
- Data / Prefab / Art 위치 규칙 불명확
- 사용 여부를 즉시 판단하기 어려운 파일 존재

정확한 내용은 `08_PROJECT_AUDIT.md`에서 확인한다.

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

## 19. Needs Audit Verification

- Shake 현재 완성도
- Build 현재 완성도
- Float 현재 완성도
- 정확한 RecipeData 구조
- 정확한 OrderData 구조
- 모든 ScriptableObject 종류
- 모든 PlayerPrefs Key
- 사용하지 않는 Legacy Script
- Test / Backup Scene
- Duplicate Prefab
- Dead Code
- SpecialGuestData 기존 구조
- `ChooseCustomerPrefab()` 사용 여부
- BusinessDayManager의 Runtime Component Fallback 여부
- 실제 Scene Serialized Reference 상태

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

**Project Rebuild**

기존 정상 기능을 보존하면서

- Project Structure
- Data Structure
- Code Responsibility
- Save / Progress
- Special Guest Architecture

를 정리한다.

Rebuild 완료 후 Little Prince Story 개발을 재개한다.
