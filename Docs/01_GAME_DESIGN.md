# SURLHWA — Game Design

Last Updated: 2026-09-08

## I10 Ingredient Availability (Implemented)

I10 makes physical ingredient availability a day-derived bar-supply rule. It
does not make a recipe known, unlocked, or visible in the Guide. `currentDay`
plus authored `IngredientAvailabilityData` determines normal supply; no
per-ingredient save key or inventory quantity exists.

The production vocabulary expands over Days 1-5 as Gin/Dry Vermouth; Sweet Red
Vermouth/Campari; Rye/Angostura/London Dry Gin/Fernet; Fresh Lemon Juice/Simple
Syrup/Soda Water/Champagne; then Green Chartreuse/Maraschino Luxardo/Fresh Lime
Juice. Legacy tutorial ingredients remain available for compatibility. Garnish
proof controls are not IngredientData supply and remain ungated in I10.

For a dedicated request, its required liquid ingredients may be added only to
that Bartending session's temporary availability context. This does not change
normal bar supply or persistence.

---

## 1. 장르

Story-driven Bartending Game

주요 구성:

- Narrative
- Bartending Mini Games
- Character Interaction
- Collection
- Light Management
- Persistent Environment

---

## 2. 기본 플레이 루프

기본적인 한 명의 손님 처리 흐름은 다음과 같다.

Customer Spawn
↓
Seat Assignment
↓
Customer Arrival
↓
Interaction
↓
Dialogue / Order
↓
Cocktail Interpretation
↓
Cocktail Choice
↓
Recipe Knowledge / Discovery
↓
Actual Preparation
↓
Result
↓
Character Response
↓
Recipe / Memory Update
↓
Customer Leave
↓
Seat Release

하루 단위 전체 흐름:

Business Day Start
↓
Customer Loop
↓
Daily Customer Limit
↓
Business Day End
↓
Night Result
↓
Codex / Records
↓
Next Day

---

## 3. 일반 손님

일반 손님은 특별 스토리를 진행하지 않는 기본적인 바 손님이다.

주요 목적:

- 기본 조주 플레이 제공
- 칵테일 레시피 경험
- 평판 변화
- 하루 플레이 리듬 형성
- 특별 손님 사이의 완충 역할

일반 손님은 가능한 한 반복적인 패턴을 줄인다.

현재 구조에서 고려하는 요소:

- Random Prefab
- Random Seat
- Random Arrival Delay
- 동일 Prefab 연속 등장 방지
- 동일 Seat 연속 배정 방지
- Patience
- Timeout
- Disappointed Response
- Reputation Change

---

## 4. 좌석 시스템

손님은 현재 사용 가능한 SeatPoint를 할당받는다.

SeatPoint는 시각적인 의자가 아니라 Gameplay Logic용 객체다.

기본 데이터:

- IsOccupied
- Occupant
- sitMarker
- approachMarker
- exitMarker

기본 동작:

SeatManager
→ Free Seat 검색
→ Reserve
→ Customer 이동
→ Customer 퇴장
→ Release

Visual Chair와 Seat Logic은 분리한다.

---

## 5. 첫 번째 영업일

첫 번째 영업일은 4가지 조주 방식을 소개하는 Tutorial 역할을 한다.

초기 네 손님:

1. 설야 — Stir
2. 여명 — Shake
3. 잔설 — Build
4. 만월 — Float

목적은 텍스트 Tutorial을 길게 보여주는 것이 아니라,
실제 손님 주문을 처리하면서 자연스럽게 네 방식을 경험하게 만드는 것이다.

Tutorial 고정 진행은 1일차에만 적용한다.

2일차 이후에는 일반 손님 / 특별 손님 구조로 전환한다.

---

## 6. Bartending System

설화의 기본 조주 방식은 네 종류다.

### Bartending 2.0 Direction

현재 prototype은 재료와 수량을 UI에서 먼저 정한 뒤 별도 technique minigame을
수행하는 흐름을 사용한다. 이 흐름은 Phase I Bartending 2.0에서 즉시 삭제되는
current-runtime 사실이 아니라, 교체를 검증해야 하는 legacy/prototype baseline이다.

승인된 target은 다음과 같다.

```text
Understand the guest
→ Choose the drink
→ Know / discover the recipe
→ Actually prepare it
→ Guest responds
→ Encounter may be remembered
```

핵심 원칙은 다음과 같다.

- preparation 자체가 gameplay다.
- 플레이어가 실제로 옮긴 liquid amount와 container state가 실제 cocktail
  composition이 된다.
- narrative choice는 **무엇을 만들지**를 정한다.
- recipe knowledge는 **무엇이 들어가는지**를 정한다.
- bartending skill은 **어떻게 잘 만들었는지**를 정한다.
- technique 뒤에 실제 drink state와 분리된 redundant scoring minigame을 두지 않는다.
- authentic-feeling bartending을 지향하되 real-world fluid simulation을 목표로 하지
  않는다.

상세 preparation vocabulary, container, unit, implementation phase, non-goal은
`Docs/11_BARTENDING_2_0_DESIGN.md`를 따른다.

### 6.1 Stir

재료와 ice를 Mixing Glass에 넣은 뒤 Bar Spoon으로 실제 젓는 방향이다.

핵심 감각:

- player-driven spoon motion
- 회전 consistency
- speed / duration의 game-scale control
- dilution / chilling을 대신하는 simplified technique state

입력이 visible spoon motion과 preparation state에 연결되어야 한다. 실제 bartending
시간을 그대로 재현하지 않고 satisfying한 game-scale duration으로 압축한다.

---

### 6.2 Shake

재료와 ice를 Shaker에 넣은 뒤 short rhythm interaction으로 흔드는 방향이다.

핵심 감각:

- left / right timing
- rhythm
- consistency
- chilling / aeration을 대신하는 simplified technique state

Shaker는 player input에 맞춰 visibly move해야 한다. recipe별 bespoke music-game
beatmap을 기본 requirement로 두지 않고, reusable rhythm pattern template을 우선한다.

---

### 6.3 Build

Serving Glass에 재료를 직접 pour하여 완성한다.

핵심 감각:

- measurement
- sequence
- correct destination
- optional top-up
- recipe understanding

Build의 challenge는 actual pour-based preparation에 있다. drink가 이미 구성된 뒤
별도의 Build timing minigame을 다시 수행하지 않는다. Build는 의도적으로 가장
쉬운 technique로 남을 수 있다.

---

### 6.4 Float

common Pour foundation 위에서 spoon / surface를 이용해 layer를 만드는 방향이다.

핵심 감각:

- low-force pouring
- flow control
- layer separation
- visible CleanLayer / PartialMix / Mixed-style result

Float는 Build의 hold/release test를 재사용하지 않는다. gentle pour와 aggressive
pour가 drink의 visible layer state를 다르게 만들어야 한다.

---

## 7. MiniGameManager

각 조주 미니게임을 직접 Customer가 관리하지 않는다.

Order / Recipe가 필요한 조주 방식을 정의하고,
MiniGameManager가 해당 Mini Game을 호출하는 구조를 지향한다.

Conceptual Flow:

OrderData
↓
Required Method
↓
MiniGameManager
↓
IMiniGame
↓
Stir / Shake / Build / Float
↓
Result

각 Mini Game은 가능한 한 공통 Result 형식을 반환한다.

---

## 8. Cocktail / Recipe System

칵테일은 데이터 기반으로 관리한다.

대표 데이터:

### CocktailData

- ID
- Display Name
- Cocktail Sprite
- Description
- Mood / Notes
- 관련 Recipe

### RecipeData

- Recipe ID
- Required Ingredients
- Bartending Method
- 기타 조주 규칙

향후 real/classic content에서는 ingredient identity와 technique뿐 아니라 actual
preparation sequence가 필요해진다. 이는 현재 `CocktailRecipeData` schema가 이미
교체되었다는 뜻이 아니다. Phase I representative validation이 current codebase에
필요한 narrow data extension을 먼저 증명해야 한다.

### OrderData

- Customer Request
- Target Cocktail 또는 조건
- Dialogue와 연결되는 주문 정보

코드에 개별 Cocktail 조건을 Hard Coding하는 방식은 지양한다.

최종 카탈로그는 특정 prototype 칵테일 이름이나 한 조주 방식당 하나의 고정 Order에
의존하지 않는다. 실제/established classic cocktails를 primary direction으로 삼고,
shared ingredient를 통해 knowledge가 여러 recipe로 이어지게 한다. 실제 recipe는
Stir, Shake, Build, Float의 target gameplay를 의미 있게 cover하도록 curated하되,
technique 수를 맞추기 위해 recipe를 왜곡하지 않는다.

최종 catalog production 전에 약 6–8개의 representative real/classic cocktail로
measurement, container, Dash / TopUp / Strain / Float requirement, shared ingredient
learning, 그리고 interaction fun을 검증한다. 이 검증 전에는 30개 이상의 catalog를
추가하지 않는다. 상세 catalog direction은 `04_CONTENT_ROADMAP.md`, Bartending 2.0
design은 `Docs/11_BARTENDING_2_0_DESIGN.md`를 따른다.

---

## 9. 초기 칵테일

현재 초기 플레이에 사용되는 대표 칵테일:

- 설야
- 여명
- 잔설
- 만월

각 칵테일은 1일차 조주 Tutorial과 연결된다.

이 네 asset은 현재 prototype/tutorial baseline이다. 설야, 여명, 잔설, 만월은
삭제하거나 rename하지 않지만, 최종 catalog에 반드시 남는다는 design requirement로
취급하지 않는다. 이 네 prototype은 Bartending 2.0 target interaction 또는 final
catalog structure의 증거도 아니다. 최종 카탈로그 방향은 `04_CONTENT_ROADMAP.md`와
ADR-028, ADR-035를 따른다.

### Ingredient Progression

재료는 시작부터 전체 catalog를 모두 노출하지 않는다. Business Day, story progress,
authored unlock event 등 의도적으로 정의된 progression point를 통해 availability를
넓힌다.

목적은 arbitrary gate가 아니라 recipe space의 확장과 transferable learning이다.

```text
Learn ingredient
→ recognize where it can be used
→ multiple recipes become possible
→ knowledge carries into later encounters
```

새 ingredient는 가능한 한 여러 recipe와 연결된다. 한 recipe마다 완전히 고립된
ingredient set을 추가하는 prototype pattern은 final content model로 삼지 않는다.
구체적인 unlock data, persistence, UI는 Phase I validation 이후 별도로 결정한다.

---

## 10. Cocktail Codex

플레이어가 경험하거나 해금한 칵테일은 도감에 기록된다.

목표:

- Recipe 확인
- Cocktail Sprite 확인
- Unlock 상태 확인
- 이전 경험 기록
- Collection Motivation 제공

Codex는 단순한 데이터베이스 UI보다
플레이어가 지나온 밤을 기록하는 느낌을 지향한다.

---

## 11. Reputation

평판은 존재하지만 게임의 최종 목표는 아니다.

사용 가능 목적:

- 일반 손님 결과 반영
- 특정 콘텐츠 해금 조건
- 손님의 반응 변화
- 플레이 진행의 부드러운 Feedback

평판이 부족하면 즉시 Game Over 되는 형태는 지양한다.

---

## 12. Business Day

BusinessDayManager는 하루 단위 게임 흐름을 관리한다.

관리 대상:

- Current Day
- Daily Customer Limit
- Served Customers
- Left Customers
- Newly Unlocked Recipes
- Day End Condition

하루 종료 후 Night Result를 표시한다.

---

## 13. Night Result

하루 종료 화면은 KPI Report가 아니라
하루를 정리하는 기록 화면에 가깝게 구성한다.

표현 예:

- 밤의 마감
- 오늘의 평판
- 마주한 손님
- 새로 기록된 칵테일

행동:

- 도감 펼치기
- 다음 밤으로

Codex를 열었다 닫아도 Day Result는 유지한다.

플레이어가 직접 다음 밤을 선택해야 다음 날로 진행한다.

---

## 14. Special Guest

특별 손님은 게임의 Main Narrative를 담당한다.

일반 손님과 동일한 기본 Customer Flow를 최대한 재사용한다.

차이점:

- 등장 조건
- 전용 Dialogue
- 전용 Order
- Cocktail Response
- Story Progress
- 공간에 남는 흔적
- Revisit 가능성

CustomerSpawner가 캐릭터별 Story Logic을 직접 보유하지 않도록 한다.

Target Flow:

CustomerSpawner
↓
SpecialGuestResolver
↓
Eligible Special Guest?
├─ YES → Special Guest
└─ NO → General Customer

상세 Story는 `04_CONTENT_ROADMAP.md` 참고.

---

## 15. Persistent Bar

특별 손님의 주요 Story가 완료되면
해당 이야기를 상징하는 흔적이 바에 남는다.

이 시스템의 목적은 Collection 숫자를 늘리는 것이 아니다.

플레이어가 실제 바 화면만 보고도
자신의 Story Progress를 느끼게 만드는 것이다.

예:

- Statue
- Prop
- Lighting Change
- Small Environmental Effect

구체적인 조건은 Content Roadmap에서 관리한다.

---

## 16. Revisit

일부 특별 손님은 Main ACT 완료 이후 다시 등장할 수 있다.

Revisit는 단순 반복 대화가 아니다.

목적:

- Main Story 이후 인물의 변화 보여주기
- 기존 Statue / Environment 변화
- 플레이어의 기억 재호출
- 작은 후일담 제공

---

## 17. Save / Progress

장기적으로 저장해야 하는 정보:

- Current Day
- Recipe Unlock
- Special Guest Progress
- Visit Completion
- Statue State
- Revisit State
- Persistent Environment State
- 기타 Progress Flags

현재 PlayerPrefs 기반 Prototype이 존재하지만,
리빌딩 과정에서 Persistence 접근을 중앙화하는 것을 목표로 한다.

---

## 18. Game Over에 대한 방향

설화는 강한 Failure State 중심 게임이 아니다.

잘못된 Cocktail이나 늦은 응대가 있을 수 있지만,
가능하면 게임 진행 자체를 차단하기보다
작은 결과와 Narrative Response로 반영한다.

---

## 19. Non-Goals

현재 설화가 핵심적으로 만들지 않는 것:

- Hardcore Economy Simulation
- 복잡한 Stock Management
- 경쟁형 Leaderboard
- Combat
- Skill Tree 중심 성장
- 반복 Grinding
- 과도한 업그레이드 경제 시스템

이 요소들이 필요한 경우에도
Project Vision을 훼손하지 않는 범위에서만 검토한다.
