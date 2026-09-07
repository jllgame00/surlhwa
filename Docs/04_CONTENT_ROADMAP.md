# SURLHWA CONTENT ROADMAP

Last Updated: 2026-09-07

Status: INTERNAL STORY BIBLE / CONTENT DIRECTION

---

## 1. Purpose

이 문서는 설화의 특별 손님, ACT, 재방문, 그리고 바에 남는 기억의 방향을
기록하는 내부 콘텐츠 로드맵이다.

이 문서는 구현 명세나 현재 Runtime 동작의 증거가 아니다. 실제 구현 여부는
`Docs/08_PROJECT_AUDIT.md`를 우선하며, Story Design과 Prototype 구현 상태는
항상 구분해서 기록한다.

프로젝트의 목표는 원작 사건을 모두 재현하는 것이 아니라, 각 인물이 대표하는
인간적인 질문을 바라는 경계 공간 안에서 다시 만나게 하는 것이다.

## 2. Special Guest Design Principles

- 특별 손님은 단순 Cameo가 아니라 하나의 인간적 주제를 대표한다.
- 칵테일은 정답 시험이 아니라 플레이어가 손님의 감정에 응답하는 언어다.
- 각 손님은 질문, 선택, 침묵, 떠남 중 하나 이상의 장면을 남겨야 한다.
- Main ACT가 끝난 뒤에도 Statue, Prop, Lighting, Ambient 변화가 바에 남는다.
- Revisit는 반복 대화가 아니라 Main ACT 이후의 변화와 기억을 보여준다.
- 캐릭터별 Story 조건은 장기적으로 CustomerSpawner가 아닌 Story / Progress
  책임에서 판단한다.
- 특별 손님 콘텐츠는 Data 확장 가능성을 고려하되, 범용 Quest Framework를
  먼저 만들지 않는다.

## 3. Theme Map

| Special Guest | Theme | 핵심 질문 |
| --- | --- | --- |
| Little Prince | 낭만 | 중요한 것을 보지 못하는 어른은 무엇을 놓치는가? |
| Chunhyang | 사랑 | 같은 순간을 서로 다른 사람이 어떻게 기억하는가? |
| Mongryong | 사랑 | 사랑을 말과 행동으로 어떻게 증명하는가? |
| Don Quixote | 이상 | 현실이 부정해도 믿는 세계를 지킬 수 있는가? |
| Hamlet | 고뇌 | 생각과 망설임도 선택이 될 수 있는가? |
| Faust | 선택 | 더 얻기 위해 무엇을 내어줄 것인가? |
| Sim Cheong | 희생 | 스스로 내린 희생의 선택은 누구의 것인가? |
| Odysseus | 귀환 | 돌아온 뒤에도 같은 사람이 될 수 있는가? |

춘향과 몽룡은 하나의 ACT에서 서로 다른 시점을 이루는 한 쌍의 손님이다.

## 4. ACT 1 — Little Prince

### Theme

낭만 / 질문 / 중요한 것을 보지 못하는 어른

Little Prince는 플레이어에게 정답을 알려주는 인물이 아니다. 그는 같은 질문을
다시 던지고, 플레이어가 즉시 이해하지 못하는 것을 허용한다.

### Narrative Direction

#### 질문

- “어른들은 왜 중요한 걸 몰라요?”
- 별 하나
- 모래바람

#### 이해하지 못함

- 같은 질문이 다른 상황에서 반복된다.
- 플레이어에게 하나의 정답이나 교훈을 강요하지 않는다.

#### 떠남

방향:

> “이제 다른 별로 가야 해요.”

### Statue / Persistent Memory

- 빈 의자
- 작은 별

처음에는 비어 있는 자리와 작은 별만 남는다. 이후 Revisit와 함께 바의 기억이
조금씩 구체화된다.

### Current Prototype Note — Audit Source of Truth

다음은 Story Design이 아니라 current repository에서 확인된 구현 상태다.

- `CONFIRMED_IMPLEMENTED`: Day 2 첫 Spawn에서 Little Prince prefab을 우선
  선택하는 code/scene 연결이 존재한다.
- `CONFIRMED_IMPLEMENTED`: Little Prince prefab의 optional DialogueData와
  dedicated OrderData는 active interaction flow에서 소비된다. 네 개의
  pre-request line은 NextButton으로 진행되고, 마지막 line 뒤 Brew가 열린다.
- `CONFIRMED_IMPLEMENTED`: dedicated first-visit request는 Float / 만월
  route를 사용한다. first-visit completion flag는 Customer leave 시 저장된다.
- `DEFERRED_CONTENT`: post-result Little Prince dialogue, interpretive
  drink choice, revisit, statue/environment trace, and final story completion
  semantics are not implemented by this current prototype.
- `NEEDS_VERIFICATION`: Bartending 2.0 이후 Day 2 전체 방문, actual
  preparation, response, memory, and revisit path는 별도 Vertical Slice
  검증이 필요하다.

## 5. ACT 2 — Chunhyang / Mongryong

### Theme

사랑의 온도

두 인물은 같은 순간을 서로 다른 높이와 감정에서 기억한다. 이 ACT의 핵심은
누가 더 옳은가가 아니라, 같은 사랑이 서로에게 어떻게 다르게 남는가다.

### 발견 — Chunhyang

위에서 아래를 바라보는 시점.

> “그네를 타고 있었죠.”

### 발견 — Mongryong

아래에서 위를 바라보는 시점.

> “첫눈에 반한 사람은 처음이었어요.”

### 떠남

- 몽룡은 많은 설명을 남긴다.
- 춘향은 침묵으로 남는다.

### 수모

춘향의 Cutscene은 말보다 장면과 침묵을 우선한다.

### 재회

두 사람이 동시에 등장해 하나의 잔을 공유한다. 재회는 보상 화면이 아니라,
서로 다른 기억이 같은 현재에 닿는 순간으로 표현한다.

### Statue

- 그네
- 빈 자리

## 6. ACT 3 — Don Quixote

### Theme

모험이라는 병 / 이상

### 선언

> “저건 거인이었습니다.”

### 조롱

그가 풍차 이야기를 하는 동안 다른 손님들의 웃음이 들린다. 이 장면의 목적은
그를 단순히 우스운 사람으로 만들지 않는 것이다.

### 확신

술을 마실수록 자신의 세계에 대한 확신은 강해진다.

### 혼자 남음

> “그래도 저는 봤습니다.”

현실이 부정해도 자신이 본 세계를 끝까지 믿는 사람이 이 ACT의 중심이다.

### Statue

풍차를 연상시키는 작은 조각과, Revisit에서 더해지는 깃발을 남긴다.

## 7. ACT 4 — Hamlet

### Theme

생각이 사람을 죽일 때 / 고뇌

### Arrival Presentation Concept

- 환경음이 줄어든다.
- Bartender animation과 다른 손님 animation이 잠시 멈춘다.
- 전체 채도가 낮아지고 Hamlet만 spotlight를 받는다.
- 색이 빠진 세계처럼 보이는 정지의 순간을 만든다.

### Dialogue Direction

> “시간이 멈춘 것 같군요.”

> “아무것도 하지 않는 것도… 선택입니다.”

### Gameplay Concept

- 칵테일 제작은 가능하다.
- 제작 속도가 느리게 느껴지는 연출을 사용한다.
- Menu sound는 거의 제거한다.

### Questions and Stillness

> “해야 할까요?”

컵을 들었다가 내려놓고, 아무 일도 일어나지 않는다.

> “결국 아무것도 하지 않았습니다.”

### Statue / Persistent Change

- 무대 조각
- 해골
- Spotlight
- 그림자

ACT 완료 뒤 Bar lighting은 아주 미세하게 영구적으로 어두워진다.

## 8. ACT 5 — Faust

### Theme

거래의 대가 / 선택

### 욕망

> “더 알고 싶었습니다. 모든 것을.”

### 계약

Cutscene에서 서명하는 순간을 보여준다. 이 장면은 초자연적 spectacle보다
돌이킬 수 없는 선택의 조용함을 우선한다.

### 이미 늦음

> “대가가 있었죠.”

> “잠시라도… 멈추고 싶었습니다.”

### Statue / Persistent Change

- 바닥의 희미한 원
- 깨진 잔
- 잔 내부의 매우 작은 붉은 빛

주변 환경은 약하게 왜곡되고, 아주 희미한 유리 깨지는 소리와 조명 흔들림이
오래 머무를 때만 드러난다.

## 9. ACT 6 — Sim Cheong

### Theme

희생 / 돌아오지 않을 각오

Sim Cheong은 피해자로만 묘사하지 않는다. 그는 자신의 선택을 내리는 주체로
변화한다.

### Visit 1

말이 많고 감정이 많다. 설명을 이어가지만 아직 결정을 내리지 못했다.

### Visit 2

배를 타기 전의 방문이다. 말은 짧아지고 질문은 사라진다.

> “나 이거 할 거예요.”

### Visit 3

귀환 후 아버지를 만난 뒤의 방문이다. 차분하게 “괜찮다”는 표현을 하지만,
돌아오기 전과 같은 사람은 아니다.

### Statue

- 반쯤 잠긴 연꽃
- 매우 천천히 움직이는 물결

## 10. ACT 7 — Odysseus

### Theme

귀환 / 그래도 돌아가는 인간

### 방랑

긴 침묵이 먼저 온다. 여행의 사건을 모두 설명하기보다, 돌아갈 곳을 생각하는
사람의 시간을 보여준다.

### 귀환

> “돌아오긴 했습니다.”

### 머무름

처음으로 Bar에서 오랫동안 머문다.

### Statue

- 닻
- 배 조각

## 11. Revisit System

Revisit는 Main ACT 완료 이후에만 열리는 후일담이다. 조건의 최종 수치와 저장
형식은 Story Architecture 및 Persistence 단계에서 Data로 확정한다.

### Little Prince Revisit 1 — Pilot

Little Prince와 Pilot이 함께 방문한다. Pilot은 거의 말하지 않는다.

> “이분이요. 저에게 알려줬어요.”

Condition concept는 Little Prince Statue에 대한 반복적인 관심 / Interaction이다.
빈 의자 옆에는 작은 종이비행기가 추가된다.

### Little Prince Revisit 2 — Rose

Little Prince가 장미를 들고 혼자 방문한다.

> “제가 지켜야 하는 건… 이것 하나면 충분해요.”

꽃 또는 허브 계열 Recipe가 condition concept이며, 빈 의자 위 장미와 별빛 색
변화가 남는다.

### Chunhyang / Mongryong Revisit

재회 뒤 시간이 흐른 후, 그네 Statue와의 반복 Interaction을 계기로 다시
방문한다. 그네 움직임, 작은 말, 마패, 어사화가 기억을 확장한다.

과거의 5회 클릭 같은 구체값은 확정하지 않는다. 최종 threshold는 Data와
Balance 단계에서 결정한다.

### Don Quixote Revisit

Sancho와 함께 방문한다. Sancho는 현실적이고, Don Quixote는 이상과 허상에
대해 끝까지 진지하다. 풍차 조각에는 작은 깃발이 추가된다.

### Sim Cheong Revisit

아버지와 함께 방문한다. 아버지는 거의 말하지 않는다. 반쯤 잠긴 연꽃과의
Interaction 후 연꽃이 완전히 물에 잠기고 Sim Cheong이 등장한다.

이후 연꽃은 완전히 개화하고 물결은 더 선명해진다.

### Odysseus Revisit

Odysseus가 혼자 다시 방문하지만 이번에는 오래 머물지 않는다.

> “이제는 떠나도 괜찮을 것 같군요.”

배는 완성되고 닻은 고정된다.

### Sim Cheong / Odysseus Narrative Echo

두 인물은 다음 구조를 공유한다.

```text
떠남
→ 돌아옴
→ 돌아온 뒤 이전과 같지 않음
```

Sim Cheong은 결과를 알고 바다로 들어가는 선택을 하고, Odysseus는 예상하지
못한 결과를 항해하며 겪는다. 둘은 같은 사건의 반복이 아니라 서로 다른 방식의
귀환을 비추는 Narrative Echo다.

## 12. Statue / Persistent Bar Changes

Statue와 Persistent Change는 수집 숫자가 아니라, 플레이어가 누구를 만났는지
공간만 보고도 기억하게 하는 장치다.

| ACT | 기본 흔적 | Revisit / 환경 변화 |
| --- | --- | --- |
| Little Prince | 빈 의자, 작은 별 | 종이비행기, 장미, 별빛 색 변화 |
| Chunhyang / Mongryong | 그네, 빈 자리 | 그네 움직임, 마패, 어사화 |
| Don Quixote | 풍차 조각 | 작은 깃발 |
| Hamlet | 무대 조각, 해골, Spotlight | 미세하게 어두워진 Bar lighting |
| Faust | 희미한 원, 깨진 잔 | 붉은 빛, 약한 sound / light distortion |
| Sim Cheong | 반쯤 잠긴 연꽃 | 개화, 선명한 물결 |
| Odysseus | 닻, 배 조각 | 완성된 배, 고정된 닻 |

구현 시 Visual hierarchy 정리와 Story Progress 저장을 한 Phase에 대규모로
섞지 않는다. 먼저 하나의 Character Vertical Slice에서 상태 전이와 복원 방식을
검증한다.

## 13. Unlock Direction

현재 Design Direction은 다음과 같다.

```text
Little Prince = 초기 Story / 자동
Chunhyang = 자동
Don Quixote = 자동
Faust = 제작 조건 기반
Hamlet = 플레이어의 기다림 / 아무 행동을 하지 않는 상황과 연결
Sim Cheong = 일정 Story Progress
Odysseus = Sim Cheong 이후
```

이는 최종 Lock된 조건이 아니다. 등장 eligibility, Recipe 조건, Statue
Interaction 횟수, Revisit threshold는 Special Guest Architecture와 Persistence
형태가 확정된 뒤 Data로 결정한다.

## 14. Story Open Decisions

### OD-001 — Hamlet / Faust Order

현재 ACT 번호는 다음과 같다.

```text
ACT 4 Hamlet
ACT 5 Faust
```

하지만 Hamlet이 Faust Statue를 잠깐 바라본다는 연출 Concept이 존재한다.
Faust가 아직 등장하지 않았다면 Narrative order와 충돌한다.

후보:

```text
A. Faust를 Hamlet보다 먼저 진행한다.
B. ACT 번호와 실제 unlock order를 분리한다.
C. Hamlet의 Faust Statue 연출을 수정한다.
```

현재는 확정하지 않는다.

### OD-002 — Progress and Revisit Conditions

Revisit 조건을 클릭 횟수, Recipe 종류, 날짜, 대화 선택, 또는 이들의 조합 중
어떤 Data로 표현할지 결정해야 한다. 구현 전에는 고정 숫자를 코드에 넣지 않는다.

### OD-003 — Cocktail Response Authority

전용 Dialogue, 전용 Order, Cocktail Choice, 결과 반응의 책임 경계를 Little
Prince Vertical Slice에서 먼저 확정한다. 현재 Prototype asset의 존재만으로
runtime 연결을 가정하지 않는다.

## 15. Content Architecture Note

모든 Special Guest ACT와 Revisit는 canonical BarScene에서 진행한다. Little Prince,
Chunhyang, Don Quixote 등 Guest별 Scene이나 ACT별 Scene을 만들지 않는다.

Guest-specific entrance, exit, VFX, SFX, camera, lighting, environment effect는 Story
progression 자체가 아니라 Presentation 책임이다. Hamlet과 Faust의 공간 연출도
alternate Scene이 아닌 BarScene environment override로 구현하는 방향을 따른다.

이 경계는 Narrative 내용을 축소하려는 것이 아니라, 같은 설원의 바가 모든 이야기와
기억을 축적하는 canonical story space로 남도록 하기 위한 것이다.

## 16. Content Implementation Order

전체 ACT를 동시에 구현하지 않는다. Rebuild 완료 뒤 첫 Narrative Vertical Slice는
Little Prince ACT 1이다.

```text
Day 2
→ Little Prince Spawn
→ Dedicated Dialogue
→ Dedicated Order
→ Cocktail Choice
→ Response
→ Progress Save
→ Leave
```

그 다음 단계는 다음과 같다.

```text
Next Visit
→ Statue
→ Persistent Bar Change
```

한 Character에서 spawn, dialogue, order, response, save, revisit, persistent
environment의 최소 연결을 검증한 뒤 다른 Character로 확장한다.

---

## 17. Cocktail Catalog Direction

Status: LOCKED DIRECTION / FUTURE CONTENT ARCHITECTURE

설화의 fiction은 손님, dialogue, story, atmosphere, progression, 그리고 bar 자체가
주로 담당한다. 칵테일 catalog는 그 fiction을 무한히 늘리는 기본 재료가 아니라,
현실의 칵테일 문화에 닿는 grounded한 play space가 되어야 한다.

플레이어가 설화를 하면서 실제 칵테일 이름, 재료, technique, 기본 문화를 더
친숙하게 느끼는 것이 product goal이다. 이는 교육용 simulation 수준의 정확성을
요구하지 않으며, gameplay readability와 설화의 tone을 유지한다.

### 17.1 Locked Catalog Direction

```text
Cocktail Catalog
├─ Established / IBA
├─ Established Classic
├─ Korean Local
└─ SURLHWA Original
```

이 분류는 현재의 conceptual content map이며 code enum이나 serialized schema가 아니다.
정확한 taxonomy는 Data Architecture에서 변경될 수 있다.

- Established real-world cocktails가 future catalog의 majority다. 현재 IBA-recognized
  cocktails는 적절한 경우 중요한 reference/base catalog가 되며, IBA list 밖의
  established classic도 포함할 수 있다.
- Korean Local은 작은 curated category다. 도화, 동해 등은 검토 대상이며, 목표는
  대규모 parallel catalog가 아닌 약 4–5개의 recipe다.
- SURLHWA Original은 narrative하게 의미 있는 드문 예외다. 목표는 약 2–5개이며,
  기본 catalog를 대체하지 않는다.
- 설야, 여명 및 현재 prototype drinks는 development content로 유지한다. 이들은
  삭제/rename 대상이 아니지만 final catalog가 보장된 content도 아니다.

### 17.2 Recipe Standard / Source Policy

Real cocktail은 이후 각 recipe가 의도적으로 선택된 documented standard/source를
가져야 한다. 현재 IBA specification은 해당되는 경우 사용할 수 있고, established
non-IBA classic은 다른 documented standard를 선택할 수 있다.

Korean-local cocktail은 단 하나의 universally authoritative specification이 없을 수
있다. 이런 경우 설화는 하나의 documented in-game **SURLHWA standard**를 선택할 수
있으며, 그것을 보편적으로 유일한 official recipe인 것처럼 주장하지 않는다.

이 문서는 실제 recipe를 조사·수록하거나 외부 recipe text를 복사하지 않는다.

### 17.3 Future Data Requirements

Future Data Architecture는 prototype content를 real cocktail content로 교체해도
cocktail별 runtime code 변경이 필요하지 않도록 해야 한다. 향후 data는 최소한 다음
관심사를 구분할 수 있어야 한다.

- cocktail identity
- preparation method
- ingredient specification
- garnish, glassware, presentation metadata
- source / recipe-standard metadata
- catalog classification

이는 미래의 data requirement이며, 현재 schema, enum, ScriptableObject를 변경하는
지시는 아니다. IBA status도 eventual metadata/reference information이어야 하며,
gameplay behavior를 바꾸는 hardcoded rule이 되어서는 안 된다.

### 17.4 Staged Prototype-to-Final Transition

```text
A. Keep prototype content while architecture is rebuilt.
B. Establish canonical recipe/catalog data architecture.
C. Add a small verified real-cocktail starter catalog.
D. Replace prototype progression/order dependencies.
E. Add Korean-local recipes.
F. Add the small SURLHWA-original set.
G. Remove prototype assets only after no runtime or serialized reference remains.
```

순서와 실제 catalog는 planning target이며 launch requirement나 final recipe list가
아니다. 총 칵테일 수, exact IBA/classic list, exact Korean-local/original list, 그리고
설화라는 이름의 cocktail이 late-game/ending 역할을 가질지는 아직 확정하지 않는다.

---

## 18. Bartending 2.0 and Representative Catalog Validation

Status: APPROVED DIRECTION / PHASE I FUTURE WORK

Red-team review 이후 다음 production rule이 승인되었다.

현재 설야, 여명, 잔설, 만월의 disconnected prototype recipe는 final catalog
structure나 final bartending interaction의 증거가 아니다. 먼저 Bartending 2.0의
actual preparation model을 representative real/classic cocktail set으로 검증한
뒤에만 large catalog production을 시작한다.

상세 preparation design은 `Docs/11_BARTENDING_2_0_DESIGN.md`, implementation
sequence는 `05_REBUILD_PLAN.md` Phase I을 따른다.

### 18.1 Representative validation gate

초기 검증 target은 약 **6–8개의 representative real/classic cocktail**이다.

이 set은 final cocktail list가 아니다. 다음 질문에 답하기 위한 gameplay/data
validation set이다.

- ml-based amount model이 selected real recipe를 표현할 수 있는가?
- shared ingredient가 recipe 간의 transferable learning을 만드는가?
- Serving Glass, Mixing Glass, Shaker와 preparation sequence가 필요한 recipe를
  표현할 수 있는가?
- Dash, TopUp, AddIce, Strain, Float requirement가 actual interaction으로
  표현되는가?
- preparation 자체가 재미있고 readable한가?

검증 전에는 30개 이상의 cocktail catalog를 추가하지 않는다. actual recipe
selection/source text는 이 documentation update에서 확정하지 않는다.

### 18.2 Shared ingredient and staged availability

Ingredient는 시작부터 전체 catalog를 모두 노출하지 않는다. Business Day, story
progression, authored unlock event 등 intentionally defined progression point를 통해
availability를 넓힌다.

목적은 arbitrary gate가 아니라 player가 recipe space를 단계적으로 이해하게 하는
것이다.

```text
Learn ingredient
→ recognize where it can be used
→ multiple recipes become possible
→ knowledge carries into later encounters
```

새 ingredient는 가능한 한 여러 recipe와 연결된다. 각 recipe가 완전히 isolated
ingredient set을 요구하는 prototype pattern은 final content direction이 아니다.

Ingredient unlock persistence, final unlock conditions, and final UI are
separate future work. 이 section은 현재 save/runtime behavior를 변경하지 않는다.

### 18.3 Content scale-up boundary

Section 4–11의 seven-ACT / revisit material은 long-term story direction으로
보존한다. 이는 Bartending 2.0 proof 이전의 immediate shipping commitment가 아니다.

Phase I은 먼저 다음을 입증해야 한다.

```text
learnable recipe
→ actual preparation
→ understandable guest response
→ one remembered choice
→ one environmental trace
```

이 proof가 완료되기 전에는 full ACT production, large catalog, or broad special
guest framework를 scale-up하지 않는다.
