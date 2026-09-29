# SURLHWA — Game Design

Last Updated: 2026-09-28

## Current visual composition contract — owner revision 2026-09-29

BarScene is a frontal, slightly elevated 2.5D winter bar. A U/ㄷ-shaped counter
encloses the central chibi bartender; guests occupy the outside seating line.
Rear shelves, lamps and windows provide depth; snow remains outside/background
atmosphere. Ordinary guests are anonymous dark silhouettes; special guests
have distinct chibi costume/prop identities. Modular environment pieces support
character grounding, sorting and existing memory displays. Owner images in
`ArtSource/Reference/VisualDirection/` and revised Docs/18 define presentation.
Gameplay, campaign and persistence contracts below remain unchanged.

## Current full-campaign contract

The owner-authorized first pass extends the existing four-night opening to
18 nights and an authored ending. The first 17 encounters and the Prince's
three-service open/quiet farewell retain their existing contracts. Nights 5–18
add 16 special visits and 14 ordinary visits through the existing customer,
dialogue, order and bartending flow. Every added night includes an ordinary
guest. Main ACTs follow love, ideal, inaction, choice/cost, sacrifice/agency and
return; future companion/secret revisits remain outside this campaign.

New special visits use interpretation-based Recommendations with learned,
supplied alternatives. Poor service changes the immediate response while the
person's story continues. Reading the departure lines precedes completion.
Ordinary guests teach Hanky Panky, French 75 and Last Word at nights 6, 10 and
15; their required discovery attempts retain retry. Supplies expand on those
nights without granting recipe knowledge.

Completed ACTs leave small spatial traces derived from departure records.
Prince's empty chair recalls either farewell; its small star retains the
existing open-farewell condition. Memories have no progress meter or reward.
Hamlet's first-pass staging is local and does not slow input or global time.

From night 5, Continue resumes after the last completed departure. Interrupted
dialogue/preparation restarts that encounter. The ending follows Odysseus's
last visit: the bartender closes this sequence of nights among the traces and
leaves a light. Interrupted ending reading replays; completed Continue returns
to the coda without night 19. New Game clears campaign visits, memories and
ending state through the existing persistence owners. A completed legacy demo
continues at night 5 without invented service successes or knowledge.

The J/I sections below retain their historical four-night scope. Technical
evidence and human review status belong in the production audit.

## J1 Coherent Demo Flow Closure (Implemented; validated)

J1 closes the narrow lifecycle and routing gaps identified by the POST-I16
production audit without changing the I10-I16 authorities. The runtime entry
surface now offers a bounded New Game / Continue flow. New Game delegates a
full reset of SURLHWA-owned gameplay progression to its existing owners, while
preserving unrelated preferences; Continue either resumes the saved day or
opens the deterministic completion state after the demo has ended.

The playable demo has a configurable four-night boundary. Day 1 ends after its
five existing authored anchors: the three Dry Martini facts, its experiment,
and the Clarity Recommendation. This routing change removes the unintended
legacy method-tutorial tail from the selected demo flow; it does not delete or
reclassify the still-active `Test*` compatibility assets. Days 2-4 retain
authorable customer-count slots, and completing Day 4 persists Demo Complete
instead of starting an unbounded Day 5.

Generic emotional requests now keep their Recommendation context when the
player selects a bartending method. The selected method opens the appropriate
interaction, but no longer replaces the guest request with a prototype
`OrderData` or its hidden exact cocktail target. Exact authored orders,
including the Dry Martini experiment and Little Prince Manwol request, retain
their existing exact semantics. Day Result now reports the night-local delta of
Known recipes rather than all recipes that happened to be Known at day end.

The active technique Guide enters an explicitly owned read-state pause and
restores prior timing on close; its use does not introduce a generalized modal
framework. The existing Little Prince dialogue/choice pause contract is
unchanged. Automated J1 static validation and the relevant normal-Editor Play
Mode regression checks passed. I15 functional validation remains distinct from
its pending subjective human feel sign-off.

## J2 Selected Cocktail and Ingredient Production Data (Implemented; normal-Editor validation passed)

J2 establishes the bounded four-night demo's production-facing cocktail set:
Seolya, Manwol, Dry Martini, Negroni, Manhattan, and Gin Fizz. Hanky Panky,
French 75, and Last Word remain valid catalog and recipe data for later
expansion, but are reserve content rather than required demo content. The
active compatibility drinks Yeomyeong and Janseol are not reclassified or
removed.

The selected emotion matrix is deliberately overlapping: Seolya is
`Softness, Warmth`; Manwol is `Longing, Clarity`; Dry Martini is `Clarity`;
Negroni is `Bitterness`; Manhattan is `Warmth, Bitterness`; and Gin Fizz is
`Awakening, Clarity`. This is data for future multi-answer Recommendation
content, not a new evaluator or a one-cocktail-per-emotion rule.

Ordinary demo supply is explicitly curated. Day 1 provides the compatibility
supply plus Gin and Dry Vermouth; Day 2 adds Sweet Red Vermouth and Campari;
Day 3 adds Rye Whiskey and Angostura Bitters; Day 4 adds Fresh Lemon Juice,
Simple Syrup, and Soda Water. Reserve-only ingredients remain configured for
Day 5. The availability fallback is disabled, so an ingredient omitted from a
day cannot leak into ordinary supply; the existing request-scoped availability
context still permits an exact dedicated request to supply only its required
liquids. This does not change RecipeCatalogData, recipe knowledge, matching,
or the J1 demo lifecycle.

The IBA preparation, garnish, glassware, amounts, and stable IDs are preserved
from I9B. J2.1 completes the selected original-drink metadata without adding a
new runtime authority: Seolya uses an Old-fashioned glass and its authored
Mixing Glass sequence is Whiskey, legacy Vermouth, Water, ice, Stir, and
Strain; Manwol uses an Old-fashioned glass and its authored direct Serving
Glass layer order is Vodka, Coffee Liqueur, then Cream. Neither drink has a
garnish. Manwol's existing Float route and `BartendingLayeredDrinkState` remain
the authority for the player's actual ordered layers; Cream is the authored
final/upper layer. Selected IBA cocktail art and ingredient icons remain a J6
presentation backlog.

## Production Recommendation and Encounter Contract

The bounded demo uses 5 / 4 / 4 / 4 encounters. Required authored discovery
retains priority: Day 2 Prince/Manwol then Negroni; Day 3 the Prince return then
Manhattan; Day 4 Gin Fizz. If the Prince’s first service did not teach Known
Manwol, Day 2 slot 3 becomes an independent Manwol practice encounter.
Day 4 slot 4 belongs to the Prince’s closing visit. Other production slots use authored situational
requests from four supported interpretations: Warmth, Bitterness, Clarity,
and Awakening, with three phrasings each. A request is offered only when an
existing Known recipe can answer it using that night's ordinary supply.
Selection never grants knowledge or injects ingredients.

Recommendation Brew presents four selectable tool setups on the bar counter:
mixing glass/spoon (Stir), Shaker (Shake), serving glass/bottle (Build), and
glass/horizontal spoon (Float). Choosing a setup and confirming uses the
existing four-method selection. The selected
method determines preparation controls; the authored guest request retains
its own meaning, title, and reward. Recipe recognition and FIT stay separate.
Exact discovery and the Prince’s first Manwol request keep their required methods.
The Prince’s return and closing requests use the same free method selection as
other Recommendations.

After Day-3 Manhattan practice, Warmth accepts both Seolya and Manhattan;
Bitterness accepts both Negroni and Manhattan. Day-4 Clarity accepts Manwol
and Gin Fizz, as well as Dry Martini when learned. These alternatives are
unranked. The Guide remains the player's recipe memory; no answer list or
best-drink marker is added. Eight FIT responses distinguish interpretation
from execution through the existing four response categories.

## I10 Ingredient Availability (Implemented)

I10 makes physical ingredient availability a day-derived bar-supply rule. It
does not make a recipe known or reveal authored details in the Guide. `currentDay`
plus authored `IngredientAvailabilityData` determines normal supply; no
per-ingredient save key or inventory quantity exists.

For the bounded J2 demo, normal supply expands over Days 1-4 as Gin/Dry
Vermouth; Sweet Red Vermouth/Campari; Rye/Angostura; then Fresh Lemon
Juice/Simple Syrup/Soda Water. Day 5 retains London Dry Gin, Fernet, Champagne,
Green Chartreuse, Maraschino Luxardo, and Fresh Lime Juice for reserve data
outside the four-night demo. Legacy tutorial ingredients remain available for
compatibility. Garnish proof controls are not IngredientData supply and remain
ungated in I10.

For a dedicated request, its required liquid ingredients may be added only to
that Bartending session's temporary availability context. This does not change
normal bar supply or persistence.

## I11A Recipe Knowledge Foundation (Implemented)

Recipe knowledge is now a separate player-progress state with three bounded
states: `Unknown`, `Partial`, and `Known`. Availability of an ingredient does
not reveal a recipe, and knowing a recipe does not add any ingredient to the
shelf.

`Partial` is fact-based rather than percentage-based. The initial fact set is
cocktail identity, method, exact `IngredientData` identity, that ingredient's
amount, garnish expectation, and preparation sequence. `Known` is explicitly
granted by a successful recipe discovery/match, at which point the Guide may
show the complete currently supported recipe presentation.

The full authored recipe remains in `CocktailRecipeData`; player progress saves
only stable recipe/ingredient IDs and fact booleans. Guest-driven clue sources
and the normal gameplay discovery loop remain future I11 work.

## I11B Production Discovery Triggers (Implemented)

I11B connects three bounded gameplay sources to the I11A knowledge authority.
An authored request hint may reveal explicit typed facts only when its line is
shown to the player; the initial Stir tutorial hint teaches only the method for
its explicitly targeted Dry Martini request. A player-used liquid is observed
after the shelf accepts it, and can reveal that ingredient only when the
request already carries that recipe as its authoritative target. Incorrect
liquids remain player action/preparation evidence and never become recipe
facts or amount knowledge.

Successful `RecipeMatchService` discovery continues to grant explicit `Known`
through `OrderManager`. Neither hint facts nor player ingredient use affect
ingredient availability, technique quality, preparation evaluation, or guest
satisfaction.

## I11C Normal-Play Discovery Arc (Implemented; validated)

Day 1 now schedules one deterministic, authored four-customer Dry Martini arc
through the existing generic customer and dedicated-request flow. Its three
pre-brew dialogue lines teach, in order, `Method: Stir`, `Gin`, and `Dry
Vermouth`; a fourth unnamed Stir request is the discovery opportunity. The
request retains its explicit internal recipe target for matching and safe
ingredient learning, while the player-visible order names and dialogue never
name Dry Martini before it becomes `Known`.

Partial facts persist between customers, day/session boundaries, and failed
attempts. The Guide is the retained player memory: it shows Stir and the
learned ingredients while the first three clues withhold identity and amounts.
Before the fourth encounter's attempt, two consumed lines teach Gin 60 ml,
Dry Vermouth 10 ml, the Mixing Glass / ice / Stir / Strain sequence, and
optional garnish. These facts remain Partial and persist in the Guide;
identity stays hidden until the established successful-match `Known` transition.
The teaching does not add a mandatory-success retry gate.
Correct repeated Gin use is idempotent; incorrect ingredients remain only
preparation evidence. A technically recognized but policy-rejected attempt
also keeps an Unknown recipe unnamed in the result UI. This is content pacing
over I11A/I11B ownership, not a quest, dialogue-parser, or guest-memory system.

## I12 Guest Recommendation Fit (Implemented; validated)

Guest response now separates four result axes:

```text
WHAT    = recognized CocktailData / recipe composition
HOW     = recipe-owned preparation evaluation
QUALITY = MiniGameResult mechanical execution
FIT     = whether a recognized cocktail suits the authored guest request
```

`OrderData` has two bounded request semantics. `ExactCocktail` preserves the
existing requested-cocktail constraint. `Recommendation` has no hidden target:
it evaluates one or more authored desired `EmotionTag` values against the
recognized cocktail's authored tags. The pure `RequestEvaluation` service owns
FIT; it does not read preparation, technique, knowledge, availability, or
dialogue prose.

The first normal-play request follows the Day-1 Dry Martini discovery arc and
asks for a quiet, clarifying Stir drink. Dry Martini is minimally authored as
`Clarity`; the existing tagged TestStir cocktail is a constructible recognized
incompatible contrast. A fitting drink with weak execution and an unfitting
drink with good execution now produce distinct bounded guest response tiers.
`repDelta` remains mechanical; the existing `reputationReward` is withheld
only for `Unsatisfied` FIT. Broader cocktail mood coverage remains future work.

## I13 Little Prince Remembered Choice (Implemented)

Little Prince now proves one bounded persistent narrative consequence without
introducing a general customer-memory or dialogue-graph system. During the
existing Day-2 first-visit dialogue, the player explicitly chooses either
`KeepTheLight` or `SeekTheStar`. The stable enum value is immediately saved in
`SURLHWA_LITTLE_PRINCE_NARRATIVE_CHOICE`; fresh and pre-I13 saves read as
`None`.

The existing `littlePrinceFirstVisitCompleted` value remains the sole answer
to whether the first visit has resolved. Choice commitment does not complete
the visit: the existing departure path still does that after the Manwol Float
flow. On Day 3, one deterministic Little Prince follow-up is scheduled only
after first-visit completion and consumes the saved choice through an authored
dialogue mapping. A legacy completed visit with `None` uses a neutral follow-up
instead of inventing a remembered statement.

The dedicated reading pause remains active through the four dialogue lines,
the two visible responses, and the immediate reply; it releases at the existing
Brew transition. The first encounter retains its exact Manwol Float request;
the return now asks for Warmth through Recommendation. Narrative choice
controls remembered dialogue, independently of cocktail suitability,
preparation, technique and visit outcomes.

## J5 Little Prince Three-Visit Farewell

The Prince visits on Day 2, Day 3, and the final encounter of Day 4. The first
visit preserves the question, player choice, and Manwol teaching. The return
recalls that choice and asks for a drink that lets him rest beside familiar
light (Warmth). Before leaving for his star, the third visit asks for a clear
view of the road home (Clarity). Seolya answers the return with normal Known
knowledge and supply; learned Manhattan is also valid. Manwol and Gin Fizz
answer the final request, with learned Dry Martini providing another option.
No Recommendation has a hidden recipe target or preferred best answer.

An open farewell requires successful service in all three visits: a recognized
cocktail that satisfies FIT, successful technique, and satisfied recipe-owned
preparation. A fitting drink with failed execution is not accepted. A single
failed visit produces the quiet farewell. Each failed service still resolves
the visit; the Prince never demands a forced-success retry. A reload cannot
erase an already served failure. Existing A/B and legacy/None choices receive
distinct recalled lines without changing this success requirement. Old saves
without service evidence never invent successful past drinks.

Prince visits are unhurried and have no patience countdown. If the first
service fails, separate Day-2 Manwol practice preserves the fair learning
path and existing retry-safe discovery policy without changing that narrative
failure. Visiting, answering the first choice, serving a drink, and completing
a visit on departure remain distinct events.

The final farewell opens after service feedback and uses the existing
Next button. Departure and the final night wait until every line is read.
The existing four-night lifecycle then marks Demo Complete; narrative flags
do not substitute for the demo endpoint. Only an open farewell followed by
completed departure makes the future Prince statue eligible. No immediate
statue, reward popup or new collection framework is introduced. New Game
clears all visit outcomes and eligibility; Continue retains them.

## I14 Service Result Feedback (Implemented; automated production-UI Play Mode sign-off passed)

I14 presents existing resolved service truths in one bounded hierarchy without
adding an overall score or another evaluator:

```text
Guest FIT       primary guest interpretation
Matched drink   recognized identity when it is safe to reveal
HOW / QUALITY   preparation issue only when relevant, then technique grade
KNOWLEDGE       one delayed recipe-discovery popup when a recipe becomes Known
REWARD          applied reputation delta and request-reward outcome
```

`GuestReactionResolver` remains the primary spoken response owner. After its
normal reaction sequence, `ResultToastController` consumes the resolved
`GuestServiceResult` as a concise diagnostic summary; it does not recompute
matching, FIT, preparation, technique, discovery, or reward. The existing
recipe popup is the only normal service discovery surface and is queued after
the summary, so a newly discovered but poor-fitting drink still reads as a
poor service choice and a separate knowledge event. Feedback is transient:
I14 introduces no persistence, score, reward-economy, or recipe-truth change.

The automated BarScene proof drives the real `OrderManager` completion seam
through `CustomerController` / `GuestReactionResolver`, `ResultToastController`,
and `RecipeUnlockPopupController`. It verifies reaction-to-toast-to-popup order,
one discovery presentation for newly Known Dry Martini, and no discovery popup
or generic-success contradiction for a recognized but poor-fitting cocktail.

## I15 Bartending Feel / Audiovisual Polish (Implementation complete; final human feel sign-off pending)

I15 keeps the established preparation, technique, matching, FIT, knowledge, and
reward authorities intact while making their existing interactions more
readable. Stir and GentleStir use the same real wrapped-angle input tracker as
before, with a 36 px center dead zone, a 110 degree anti-teleport guard, and a
0.08 s smoothed **visual-only** spoon orbit. Smoothing never contributes
rotation or quality. Pour remains immediate 30 ml/s container transfer and
stops without a delayed amount. Shake retains its 0.09 s Perfect and 0.20 s
Good windows while showing the next direction/key and an immediate
Perfect/Good/Miss label. Float retains its 15/30/60 ml/s Gentle/Normal/Aggressive
flow and Clean/Partial/Mixed thresholds while showing the active mode and its
authored spoon visual.

Ice, Strain, PourFromContainer, and garnish continue to use their existing
actual container/trace actions with concise confirmation. No project-owned
audio clips or audio playback system currently exist, so I15 adds no arbitrary
external audio or fake audio authority. The visual language remains restrained:
accepted input gets a short visible response, completed actions get a clear
confirmation, and weak technique remains readable without arcade-style effects.

## I16 Two-Night Vertical Slice (Validated)

The first bounded two-night production slice is validated from a controlled
progress state in the shipped `BarScene`. Day 1 keeps Dry Martini physically
available but Guide-unknown, consumes its authored Stir, Gin, and Dry Vermouth
clues into retained Partial knowledge, preserves that state through a failed
attempt, and promotes it to Known only through the existing successful match.
The following Clarity Recommendation remains unnamed before evaluation and
accepts Dry Martini as FIT-satisfied without producing a duplicate discovery.

The normal day persistence boundary retains Day 2 and Known Dry Martini. Day 2
uses the existing Little Prince first-visit eligibility, separate persisted
choice, patience-safe narrative-to-Brew handoff, exact Manwol Float context,
departure-based completion, and Day-3 follow-up eligibility. Recipe knowledge,
availability, preparation, FIT, reward, and narrative-choice authorities remain
separate throughout this proof.

Normal Unity 6000.3.7f1 Editor validation passed I10 through I16. Existing
production Play Mode evidence remains the interaction-level proof for I11B
ingredient/discovery, I13 Little Prince choice/patience/Manwol, and I14's
BarScene reaction-to-toast-to-popup order; those routes were unchanged by I16.
The local Unity license lacks `com.unity.editor.headless`, so headless runs are
an environment limitation rather than a project failure. I15 functional
validation passed; its optional subjective human feel sign-off remains pending.

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
