# SURLHWA — POST-I16 Production Gap Audit / Completion Roadmap

Audited repository: `D:/univ/surlhwa/Surlhwa`

Audited branch: `refactor/project-rebuild`

Audited HEAD: `3fdce32736854e6793de5b142ff4c58e7e1ec650`

Report export date: 2026-09-16

## J1 Implementation Record (2026-09-17)

The audit's immediate next phase, J1 -- Coherent Demo Flow Closure, has since
been implemented as a narrow lifecycle/routing pass. This record does not
rewrite the audit's historical baseline or its content recommendations.

Implemented: a minimal New Game / Continue entry surface; delegated reset of
SURLHWA-owned progression; a configurable bounded four-night schedule; the
five-anchor Day-1 route without its unintended legacy tutorial tail; preserved
generic Recommendation request context across method selection; night-local
recipe-discovery reporting; persistent Demo Complete routing after Day 4; and
owned Guide read-state pause behavior during active bartending.

Not implemented by J1: new cocktail discovery content, additional narrative,
emotion-tag expansion, art, audio, a final authored ending, or deletion of the
active legacy `Test*` assets. The I10-I16 truth boundaries remain unchanged.
Focused J1 static validation and relevant normal-Editor Play Mode regressions
passed; I15 subjective feel approval remains a human sign-off item.

## J2 Implementation Record (2026-09-17)

J2 implements the audit's bounded cocktail/ingredient data recommendation
without revising this report's historical audit baseline. The four-night demo
production set is now explicitly Seolya, Manwol, Dry Martini, Negroni,
Manhattan, and Gin Fizz. Hanky Panky, French 75, and Last Word remain intact
as reserve recipe/catalog data. Yeomyeong and Janseol remain active
compatibility content and were not normalized or deleted.

Implemented data contract: the selected emotion matrix is Seolya
`Softness, Warmth`; Manwol `Longing, Clarity`; Dry Martini `Clarity`; Negroni
`Bitterness`; Manhattan `Warmth, Bitterness`; and Gin Fizz `Awakening,
Clarity`. Selected IBA descriptions are player-facing Korean copy, while their
existing I9B recipe quantities, preparation, garnish, glassware, and stable
identities are preserved. No new customers, discovery facts, Recommendation
requests, narrative content, or gameplay authority were created.

Ordinary staged supply is now demo-facing: the 13-item Day-1 shelf contains
the compatibility/original supply plus Gin and Dry Vermouth; Day 2 adds Sweet
Red Vermouth and Campari (15); Day 3 adds Rye Whiskey and Angostura Bitters
(17); Day 4 adds Fresh Lemon Juice, Simple Syrup, and Soda Water (20). The six
reserve-only ingredients remain at Day 5. The explicit availability fallback
is disabled, preventing omitted reserve ingredients from leaking into Days
1-4; existing request-scoped exact-order availability remains intact.

J2.1 closes the selected original-drink metadata contract using the existing
data model. Seolya now has Old-fashioned glass metadata and the authored
Mixing Glass sequence Whiskey, legacy Vermouth, Water, AddIce, Stir, and
Strain; Manwol has Old-fashioned glass metadata and ordered direct Serving
Glass pours Vodka, Coffee Liqueur, then Cream. Neither drink has a garnish.
Manwol's `Float` method and the existing `BartendingLayeredDrinkState` remain
the runtime authority for actual player-created layers; the final Cream step is
the authored upper layer. The J2 validator directly asserts these final
contracts alongside the selected-set, supply, catalog, and Prince regressions.
Selected IBA art and ingredient icons remain presentation work. The dedicated
J2 data validator passed in a normal Unity Editor run before J2.1; its J2.1
rerun was blocked before project load by the local Unity license/Software Terms
state, not a project validation failure. J3 may now consume this stable data
contract.

## J4B Implementation Record (2026-09-17)

J4B adds the bounded authored discovery-practice completion contract without
altering ordinary orders, matching, recipe knowledge ownership, or the
four-night schedule. `OrderData.requiresSuccessfulDiscovery` is false by
default and is enabled only for the existing Day-2 Little Prince first-visit
Manwol order. A flagged order completes only when its exact target recipe is
recognized and FIT-satisfied, the mini-game result succeeds, and the shared
preparation evaluation is satisfied. Recipe knowledge remains owned by
`RecipeUnlockManager`; a flagged attempt can promote only that exact target
after the complete gate passes. A recognized non-target recipe is never
incidentally promoted in that authored practice encounter.

An incomplete flagged attempt receives concise retry feedback through the
existing interaction surface, restores Brew for the same waiting customer, and
does not award service completion, trigger departure, or advance the bounded
schedule. Patience is suppressed only for a flagged discovery-practice
customer. As departure is still the existing owner of Little Prince's
first-visit completion persistence, an incomplete attempt cannot consume the
first visit; its independently persisted Partial facts remain available after
reload, while the successful served/departure path and Day-3 branch-aware
return remain unchanged.

Little Prince's existing four narrative lines and choice are preserved. Two
appended functional teaching lines now reveal Manwol identity, Float, Vodka
2, Coffee Liqueur 1, Cream 1, and its ordered direct-serving-glass preparation
as typed Partial knowledge. They do not mark it Known or expose its
Known-only glassware. `FloatMiniGame` now receives the active expected recipe
through the existing preparation-expectation receiver seam and forwards its
existing `BartendingLayeredDrinkState` trace to the shared preparation result
factory, so Vodka, Coffee Liqueur, then the final Cream layer is enforced
without a second Float simulation.

The focused J4B validator covers the flag boundary, teaching facts/Guide
projection, Float expectation and sequence, full completion gate, and
failed-attempt retry/persistence safety. Its batch-mode execution is currently
blocked by the local Unity license entitlement before project scripts can load;
the validator remains intentionally strict and must be rerun in a licensed
Editor. J3 remains blocked until J4C discovery encounters and J4D required
scheduling/recovery work are complete.

## J4C Implementation Record (2026-09-17)

J4C adds three isolated authored exact-practice assets without finalizing the
Day 2--4 schedule: Negroni (`Build`), Manhattan (`Stir`), and Gin Fizz
(`Shake`). Each order is flagged with the existing J4B
`requiresSuccessfulDiscovery` contract, points to its production cocktail and
pre-brew teaching dialogue, and supplies recipe-specific retry copy through
the existing retry surface. The dialogues grant typed Partial knowledge only:
identity, method, all ingredient amounts, preparation, and garnish. A correct
attempt is therefore still required to mark the intended recipe Known; no
recipe is default-unlocked or marked Known by dialogue.

The preparation/UI seams now read the active expected recipe without changing
`RecipeMatchService` or recipe data. Build and Stir label the garnish action
as required only where their authored target requires it (Negroni Orange Slice
and Manhattan Cherry); ordinary and optional-garnish routes retain their
previous optional presentation. The shared preparation trace already records
Manhattan's `Dash`, so a liquid-equivalent `Pour` cannot satisfy its authored
sequence. Shake now exposes its existing `PourFromContainer` action as the
primary “Pour to Glass” transfer where the expected Gin Fizz recipe requires
it, while Shake recipes that expect `Strain` remain unchanged. Gin Fizz Lemon
garnish stays optional and omitting it does not create a required garnish
step.

The existing J4 validator now covers all four discovery recipes, their typed
Partial facts and staged supply, expected preparations (including Manhattan
Dash, required/optional garnish behavior, and Gin Fizz transfer/Splash), and
the shared J4B gate/retry boundary. Editor execution remains blocked before
project load by the local Unity license entitlement; the validator is kept
strict for a licensed rerun. J4D still owns normal-play discovery scheduling
and the final end-to-end proof. J3 and J5 remain pending.

## J4D Implementation Record (2026-09-17)

J4D wires the existing J4C authored practice orders into the sole existing
`CustomerSpawner` in `BarScene`: Little Prince / Manwol remains Day 2 slot 1;
Negroni is the pending Day 2 slot 2 encounter; the existing branch-aware
Little Prince return remains Day 3 slot 1; Manhattan is the pending Day 3
slot 2 encounter; and Gin Fizz is the pending Day 4 slot 1 encounter. Only
the three new serialized order references were added to the scene. No
transforms, prefabs, customer counts, or J3/J5 slots were changed: J1's
interim `5/8/8/8` counts remain until J3D owns final `5/4/4/4` scheduling.

The selector consults the existing `RecipeUnlockManager` and schedules a
practice order only while its target is not Known. A known Negroni, Manhattan,
or Gin Fizz therefore receives the ordinary generic fallback for that position
without regressing Partial/Known knowledge or replaying a discovery unlock.
The component's spawn count is intentionally transient because normal night
continuation reloads the Bar scene. Existing persisted Little Prince first and
follow-up completion flags reconstruct the already-consumed Day 2 / Day 3
special slots after such a reload, so a still-unknown Negroni or Manhattan
remains pending without adding a J4 persistence key. An incomplete active
practice encounter still cannot advance: J4B retains the same customer, and
the existing spawner increments its counters only at spawn/departure paths.

This establishes the J3 prerequisite sequence through normal play: successful
Day 2 Manwol and Negroni practice make Manwol and Negroni Known before later
Day-2/Day-3 requests; successful Day 3 Manhattan practice makes both Warmth
and Bitterness alternatives available; successful Day 4 Gin Fizz practice
makes the Awakening route available. Existing staged supply provides each
recipe's ingredients on its scheduled day; no intended learning route relies
on temporary exact-order supply injection.

The focused J4 validator now loads the production Bar scene, verifies its
three references and unchanged interim counts, simulates fresh and reload
positions, Known skips, Prince priority, and active-customer retry blocking.
The C# project compiles. Unity's batch Editor continues to exit before project
scripts load because the local license entitlement is unavailable, so licensed
runtime/Console validation remains outstanding. J4 implementation is complete;
J3 is now unblocked for implementation on top of this uncommitted work, and
J5 remains separate.

This document preserves the findings and recommendations of the completed POST-I16 audit. The authorization that originally saved the report changed only its delivery format; the later J1 authorization is recorded above. Baseline and validation statements below describe the audited state, not newly executed Play Mode tests.

SURLHWA’s core rebuild is ready to support production. The largest remaining gap is the authored playthrough: recipes, ingredients, and reusable mechanics extend considerably beyond the encounters that teach and contextualize them.

The recommended target is **four playable nights, six production cocktails, one completed Little Prince mini-arc, and explicit New Game / Continue / demo-ending flows**. Another major framework is unnecessary.

## Baseline

| Item | Confirmed state |
|---|---|
| Repository | `D:/univ/surlhwa/Surlhwa` |
| Branch | `refactor/project-rebuild` |
| HEAD | `3fdce32736854e6793de5b142ff4c58e7e1ec650` |
| Unity | `6000.3.7f1` |
| Worktree during audit | Clean before and after inspection |
| Audit changes | None |
| Audit commits | None |

The expected I10–I16 history is present:

| Phase / work | Commit |
|---|---|
| I10 — staged ingredient availability | `d0cea8e` |
| I11 — recipe knowledge states | `7bb655e` |
| I11 — gameplay recipe discovery | `a191ddb` |
| I11 — normal-play recipe discovery loop | `e0da48a` |
| I12 — guest recommendation evaluation | `b0d876d` |
| I13 — Little Prince narrative choice persistence | `9ad2061` |
| I14 — service result feedback | `aedcb66` |
| I15 — bartending interaction polish | `d6efd2e` |
| I16 — two-night vertical slice validation | `3fdce32` |

Local-only Unity layout/settings material was explicitly excluded from production work. This includes `UserSettings/EditorUserSettings.asset`, `UserSettings/Layouts/CurrentMaximizeLayout.dwlt`, `UserSettings/Layouts/default-6000.dwlt`, and local search settings/index material. The tracked layout/settings files were not modified at the audited baseline. No hidden assume-unchanged or skip-worktree state was found masking project changes.

The audit inspected code, serialized scenes and assets, references, documentation, validation tools, and representative art. It did **not** launch Unity, rerun production Play Mode, or create a standalone build. PASS statements below refer to recorded validation evidence, with their scope stated explicitly. Static inspection is not represented as fresh subjective playtesting.

## Core Architecture Confirmation

The rebuilt runtime boundaries remain coherent and should be preserved:

| Authority | Responsibility |
|---|---|
| `BartendingContainerState` | Actual liquid contents, transfers, ice, and serving-glass state |
| `BartendingPreparationTrace` | Evidence of actions the player actually performed |
| `RecipePreparationStep[]` | Expected authored preparation sequence |
| `RecipeMatchService` | Recognition of the resulting drink from method and composition |
| `RecipeUnlockManager` / recipe knowledge | Unknown, Partial, Known, and fact-level player knowledge |
| Ingredient availability data/context | Day-based physical supply and scoped request exceptions |
| `RequestEvaluation` / Guest FIT | Exact-request satisfaction or recommendation suitability |
| Technique result | Quality of mechanical execution |
| Little Prince persistence | Stable narrative choice and visit progression |

The preparation vocabulary is supported by the current model: Measure, Pour, Dash, Splash, TopUp, AddIce, Stir, GentleStir, Shake, Float, Strain, PourFromContainer, and Garnish. These are canonical concepts, not a claim that every word must exist as one identical enum or one shared interaction. Measurement affects actual quantities; it is not dependent on the inactive legacy Jigger presentation. Float retains its own layer-oriented interaction while contributing real drink state.

| Rebuild phase | Audit confirmation |
|---|---|
| I10 | Staged physical availability is separate from recipe knowledge. |
| I11 | Fact-level learning, Partial presentation, successful discovery, and persistence exist. |
| I12 | Guest FIT is independent of mechanical technique quality. |
| I13 | Little Prince choice persistence and branch-aware return content exist. |
| I14 | Service-result feedback has an explicit hierarchy and sequencing. |
| I15 | Functional validation passed; subjective human feel acceptance remains pending. |
| I16 | The controlled Day-1 → Day-2 vertical slice has recorded validation. |

These phases should not be reopened merely because their production-content coverage is small. The architecture is ready for bounded production authoring, not another framework replacement.

Evidence includes the rebuild history and [the rebuild plan](05_REBUILD_PLAN.md), particularly its I10–I16 records.

## Production Asset Inventory

This inventory groups production-facing assets rather than listing `.meta` files or trivial support files.

| Category | Major current assets / quantity | Production interpretation |
|---|---|---|
| Build scenes | `BarScene`, `BartendingScene` | The two active game scenes |
| Other scenes | `BarScene_LayoutTest`, `_Recovery/0`, URP template scene | Layout/recovery/template material, not the production entry sequence |
| Bartending prefabs | Four method prefabs: Build, Stir, Shake, Float | Active production mechanisms with incomplete presentation |
| Active customer prefabs | `Customer_A`, `LittlePrinceCustomer` | Generic guest and authored Prince roles |
| Other customer prefab | `Customer_B` under `Assets/Assets/Prefab/Character` | Present but not assigned to the active generic pool |
| Environment prefabs | Counter, shelf, stool and related bar pieces | Functional bar construction / greybox presentation |
| UI prefabs | Seven relevant prefabs covering shelf, ingredient option, recipe button, codex card, dialogue bubble, heart popup, and related presentation | Reusable UI exists; final art and copy are incomplete |
| `CocktailData` | 11 | Four original/prototype drinks plus seven selected IBA recipes |
| `CocktailRecipeData` | 11 | Matching recipe definitions exist |
| Recipe catalog | One current catalog | Includes more data than the authored playthrough meaningfully introduces |
| `IngredientData` | 26 | Referenced ingredients exist; ingredient art is missing |
| Availability data | One staged availability asset | Day 1–5 supply progression |
| `OrderData` | 11 | Four prototype method orders, four Dry Martini discovery orders, one Recommendation order, two Prince orders |
| Dialogue | 10 `DialogueData` assets and one reaction database | Authored encounter coverage is narrow despite reusable reaction support |
| Narrative data | One Little Prince narrative asset | First visit, choice replies, and follow-up branches |
| Progression/persistence | Five relevant persistence/helper scripts, plus runtime managers | Bounded state owners rather than a generalized save framework |
| Visual assets | Four original cocktail images, locked/tool/UI art, tile/bar art, older JPG material | Mixed presentation and uncertain legacy-art role/provenance in places |
| Audio | No owned audio assets found | No production soundscape or core sound set |
| Documentation | 14 documents plus README | Extensive implementation history; some design/README passages are stale |
| Editor tooling | 11 top-level editor files and 19 rebuild/migration files | Strong validation and migration support; not player-facing content |

`ProjectSettings/EditorBuildSettings.asset` confirms the active build scene list. There is no separate title or ending scene in that list.

## Production vs Prototype Classification

The classification distinguishes implementation status from content readiness. An active production dependency can still originate in a prototype.

| Content / asset | Classification | Reason |
|---|---|---|
| Core rebuilt runtime authorities | PRODUCTION | Established responsibilities and supporting validation |
| `BarScene` / `BartendingScene` | PRODUCTION-INCOMPLETE | Functional game loop, unfinished content and presentation |
| Dry Martini discovery arc | PRODUCTION-INCOMPLETE | Reference playable learning arc; art/copy/pacing still need production acceptance |
| Little Prince first visit and follow-up | PRODUCTION-INCOMPLETE | Implemented choice and recall, not a completed narrative arc |
| Seven IBA cocktail data sets | PRODUCTION-INCOMPLETE | Recipe definitions exist; most lack actual game-content integration |
| `TestStir`, `TestShake`, `TestBuild`, `TestFloat` orders | PROTOTYPE / TEST — ACTUALLY STILL USED | Active method-selection dependencies, not safely removable residue |
| Original drinks associated with the test orders | PROTOTYPE / TEST with active dependencies | Still participate in catalog, tutorial, method, or narrative flows |
| Manwol | PRODUCTION-INCOMPLETE | Required by the Prince even though its route includes `TestFloat` ancestry |
| `Customer_B`, layout/recovery material | LEGACY-INACTIVE | Not assigned to the inspected active production flow |
| Validators and proof-only fixtures | VALIDATION-ONLY | Support evidence and regression, not authored gameplay |
| Older art with unclear current purpose/provenance | UNKNOWN / NEEDS REVIEW | Presence is not proof of production assignment or approved release usage |

The `Test*` family must not be treated as unused just because its names are internal or old. The important finding is active behavioral dependency, not the desirability of renaming assets. No assets were renamed.

## Cocktail Catalog

All 11 current cocktails have both recipe and cocktail data, and their referenced ingredient assets exist. This is a repository-content audit, not an external recertification of IBA specifications.

### Recipe and metadata coverage

| Cocktail | Recipe / CocktailData | Ingredients | Method / preparation authored | Garnish | Glassware metadata | Earliest physical supply |
|---|---|---|---|---|---|---|
| 설야 / Seolya | Yes / Yes | Complete references | Stir; liquid recipe, no authored preparation sequence | None authored | Unspecified | Day 1 |
| 여명 / Yeomyeong | Yes / Yes | Complete references | Shake; no authored preparation sequence | None authored | Unspecified | Day 1 |
| 잔설 / Janseol | Yes / Yes | Complete references | Build; no authored preparation sequence | None authored | Unspecified | Day 1 |
| 만월 / Manwol | Yes / Yes | Complete references | Float; layer-oriented recipe, no authored preparation sequence | None authored | Unspecified | Day 1 supply; Prince encounter Day 2 |
| Dry Martini | Yes / Yes | Complete references | Pour, ice, Stir, Strain | Optional lemon twist / olive | Cocktail glass | Day 1 |
| Negroni | Yes / Yes | Complete references | Pour, ice, GentleStir | Orange slice | Old-fashioned glass | Day 2 |
| Manhattan | Yes / Yes | Complete references | Pour, Dash, ice, Stir, Strain | Cherry | Cocktail glass | Day 3 |
| Hanky Panky | Yes / Yes | Complete references | Pour, ice, Stir, Strain | Orange zest | Cocktail glass | Day 3 |
| Gin Fizz | Yes / Yes | Complete references | Shake, PourFromContainer, Splash | Optional lemon slice / twist | Tall glass | Day 4 |
| French 75 | Yes / Yes | Complete references | Shake, Strain, fixed Champagne pour, GentleStir | None authored | Flute | Day 4 |
| Last Word | Yes / Yes | Complete references | Four equal measures, ice, Shake, Strain | None authored | Cocktail glass | Day 5 |

French 75 currently uses a fixed Champagne `Pour`, not a `TopUp` operation. Gin Fizz uses a 15 ml gameplay Splash. These are actual authored/runtime choices; the audit does not silently replace them with a different preparation interpretation.

### Knowledge, orders, recommendation, guide, and validation coverage

| Cocktail | Knowledge / discovery content | Emotion tags / Recommendation use | Normal-play order / access | Guide presentation | Runtime evidence / production readiness |
|---|---|---|---|---|---|
| Seolya | Known by default | Softness, Warmth; used as a negative Clarity contrast | Prototype Stir title remains, but that order’s exact target is Dry Martini | Description and art; no preparation sequence | Active compatibility content; not production-complete |
| Yeomyeong | Successful matching can reveal it; older tutorial hints exist | Awakening, Bitterness; no dedicated Recommendation encounter | `TestShake` route | Description and art; no preparation sequence | Historical/prototype route; incomplete production integration |
| Janseol | Successful matching can reveal it; older tutorial hints exist | Regret, Comfort; no dedicated Recommendation encounter | `TestBuild` route | Description and art; no preparation sequence | Historical/prototype route; incomplete production integration |
| Manwol | Ingredient-use facts and successful matching; no complete authored clue arc | Longing, Clarity; Prince uses exact requests, not an authored Recommendation validation | Prince first visit and follow-up; generic Float route | Description and art; no preparation sequence | I13/I16-related production use; necessary but incomplete |
| Dry Martini | Complete reference discovery arc | Clarity; authored positive FIT and negative contrast | Four discovery orders plus Clarity Recommendation | Full recipe/preparation presentation; no cocktail image | Strongest production integration; presentation remains incomplete |
| Negroni | Match-based discovery only; no authored clue encounter | No emotion tags; no Recommendation content | No dedicated order; generic Build experimentation is possible | Full recipe metadata; image missing | Recipe/catalog baseline exists; meaningful gameplay content missing |
| Manhattan | Match-based discovery only; no authored clue encounter | No emotion tags; no Recommendation content | No dedicated order; generic Stir experimentation is possible | Full recipe metadata; image missing | Recipe/catalog baseline exists; meaningful gameplay content missing |
| Hanky Panky | Match-based discovery only; no authored clue encounter | No emotion tags; no Recommendation content | No dedicated order | Full recipe metadata; image missing | Data/reserve candidate, not a production encounter |
| Gin Fizz | Match-based discovery only; no authored clue encounter | No emotion tags; no Recommendation content | No dedicated order | Full recipe metadata; image missing | Recipe/catalog baseline exists; meaningful gameplay content missing |
| French 75 | Match-based discovery only; no authored clue encounter | No emotion tags; no Recommendation content | No dedicated order | Full recipe metadata; image missing | Data/reserve candidate, not a production encounter |
| Last Word | Match-based discovery only; no authored clue encounter | No emotion tags; no Recommendation content | No dedicated order | Full recipe metadata; image missing | Data/reserve candidate, not a production encounter |

The I9B catalog/proof record is not equivalent to a fresh normal-play validation of every cocktail. The most explicit production-flow evidence remains around Dry Martini, the Prince/Manwol route, service feedback, and the established controlled slice.

### Recipe exists but game content does not

The six IBA cocktails other than Dry Martini exhibit the critical gap:

```text
Recipe data exists
+ required ingredients and progression exist
+ method/preparation metadata exists
- no authored guest clue encounter
- no dedicated discovery encounter
- no Recommendation use
- no story or situational context
```

The six are Negroni, Manhattan, Hanky Panky, Gin Fizz, French 75, and Last Word. They are technically experimentable where method/supply allow, but that does not make them meaningfully integrated game content. Recipe-data completeness must not be reported as game-content completeness.

## Ingredient Progression

There are 26 current `IngredientData` assets.

| Day | Newly available staged ingredients | Total available | Recipe availability enabled |
|---|---|---:|---|
| Day 1 | Gin, Dry Vermouth, in addition to 11 always-available originals | 13 | Dry Martini |
| Day 2 | Sweet Red Vermouth, Campari | 15 | Negroni |
| Day 3 | Rye Whiskey, Angostura Bitters, London Dry Gin, Fernet Branca | 19 | Manhattan / Hanky Panky |
| Day 4 | Fresh Lemon Juice, Simple Syrup, Soda Water, Champagne | 23 | Gin Fizz / French 75 |
| Day 5 | Green Chartreuse, Maraschino Liqueur, Fresh Lime Juice | 26 | Last Word |

The 11 always-available original ingredients are Whiskey, Vermouth, Water, Rum, Lime, Syrup, Tonic, Lemon, Vodka, Coffee Liqueur, and Cream.

By recipe reference usage, the inventory contains 11 ingredients used only by the original/prototype drinks, 14 used only by the IBA recipes, and one shared ingredient, Gin. No configured ingredient was completely unused, and no duplicate stable ingredient IDs were identified.

Exact identity distinctions should remain intact:

```text
Gin ≠ London Dry Gin
Dry Vermouth ≠ Sweet Red Vermouth ≠ legacy Vermouth
Whiskey ≠ Rye Whiskey
Syrup ≠ Simple Syrup
Legacy Lemon / Lime ≠ authored fresh juice identities
```

Related names are not sufficient evidence that these assets should be merged. The content problem is whether a player can understand and use them, not that exact-identity matching exists.

The staged data matches the known Day 1–5 design. Two to four newly staged ingredients per day is manageable as a data plan. Actual instructional pacing is less complete:

- Day 1 exposes 13 ingredients while its authored IBA lesson primarily needs two.
- Day 2’s Gin and Sweet Red Vermouth reuse is promising, but Negroni has no corresponding encounter.
- Day 3 introduces ingredients for two recipes without either production introduction.
- Day 4 similarly introduces both Gin Fizz and French 75 supply without authored teaching.
- Reserve-specific ingredients therefore appear without an immediate player-facing purpose.
- The availability asset’s `unlistedIngredientsAreAvailable` fallback is true. Future content additions must explicitly check whether an omitted ingredient should really be available.

The recommendation is to curate exposed ingredients after the demo cocktail/encounter dependencies are settled. This is not a recommendation to merge identities, build an inventory economy, or rebalance quantities now.

## Recipe Discovery Coverage

For the seven selected IBA cocktails:

| Coverage level | Count | Cocktails |
|---|---:|---|
| Complete authored reference production flow | 1 | Dry Martini |
| Partial dedicated discovery rollout beyond that reference | 0 | None |
| Recipe data and match-based discovery, but no authored discovery flow | 6 | Negroni, Manhattan, Hanky Panky, Gin Fizz, French 75, Last Word |

The six data-only cocktails can potentially move directly from Unknown to Known through a successful match. That is a system capability, not the authored path `Unknown → clue/fact → Partial → experimentation → Known` requested for meaningful production discovery.

Dry Martini provides the reference flow. Its clues reveal the method, Gin, and Dry Vermouth. They do not constitute a full set of exact amount facts. The experiment and later Recommendation encounter provide contextual use.

Manwol is distinct: making it can teach ingredient facts, and the Prince gives it narrative importance, but the first-visit dialogue is not a full ingredient/proportion teaching arc. The existence of an exact Manwol order does not itself explain how an unfamiliar player should make it.

Seolya starts Known. Yeomyeong and Janseol have older tutorial support. The old Day-1 Float teaching slot does not fit into the actual eight-customer sequence once the new authored anchors are inserted, leaving Day-2 Manwol onboarding particularly important.

The next discovery work is encounter/fact authoring using existing systems. It is not a missing discovery framework.

## Recommendation / Emotion Coverage

### Current EmotionTag vocabulary

| Tag | Cocktails using it |
|---|---|
| Loneliness | None |
| Comfort | Janseol |
| Awakening | Yeomyeong |
| Softness | Seolya |
| Bitterness | Yeomyeong |
| Heavy | None |
| Clarity | Dry Martini, Manwol |
| Warmth | Seolya |
| Regret | Janseol |
| Longing | Manwol |

Two tags are unused by any cocktail. Seven are represented only by the original/prototype group. Clarity is the only tag shared between a selected IBA cocktail and an original drink. Six of the seven IBA cocktails have no emotion tags at all.

The vocabulary is sufficient for a small game. Sparse, uneven authored coverage is the problem; a large new taxonomy would not solve it.

### Cocktail participation

| Cocktail / group | Emotion tags | Recommendation-compatible in data? | Existing Recommendation OrderData / dialogue | Validated positive FIT | Validated negative contrast |
|---|---|---|---|---|---|
| Dry Martini | Clarity | Yes | Yes: Clarity guest | Yes | Yes, contrasted with Seolya |
| Seolya | Softness, Warmth | Yes | No dedicated authored request | No dedicated positive encounter proof | Yes, negative Clarity contrast |
| Yeomyeong | Awakening, Bitterness | Yes | No dedicated authored request | No dedicated proof | No dedicated proof |
| Janseol | Regret, Comfort | Yes | No dedicated authored request | No dedicated proof | No dedicated proof |
| Manwol | Longing, Clarity | Yes | Prince orders are exact requests, not Recommendation encounters | No dedicated Recommendation proof | No dedicated Recommendation proof |
| Other six IBA cocktails | None | Not meaningfully, with current tags/content | None | None | None |

### Multiple-valid-answer audit

The one authored Recommendation request is Clarity. At the evaluation/tag level, both Dry Martini and Manwol can fit. In its actual encounter, however, the order routes through Stir, while Manwol is Float. The practical authored route therefore offers one reachable fitting cocktail, not two.

Exact-request encounters intentionally have one requested answer and are not defects for doing so. Generic emotion requests may suggest several plausible interpretations, but their method-order wiring does not consistently route them through the I12 FIT contract.

Expansion should create overlapping valid choices that are simultaneously reachable under the encounter’s method access, ingredient supply, and player knowledge. Merely adding the same tag to two drinks in different inaccessible methods would not demonstrate multiple-answer gameplay.

The current `RequestEvaluation` separation should be preserved. Content and narrow routing changes, not a replacement evaluator, are the appropriate response.

## Customer Content

### Generic customer loop

There is one actively used generic customer presentation. The general customer pool is empty in the inspected configuration, so spawning falls back to `Customer_A`. `Customer_B` exists as an asset but is not evidence of a varied active customer pool.

Generic request selection chooses from ten primary emotion tags, avoids the recent three primary selections, and chooses a secondary emotion. Ten fixed emotion-hint lines are combined into two-part requests. This supports combinations, but it is not the same as independently authored guest conversations.

The reaction content includes:

- 44 legacy reaction lines.
- Four FIT reaction lines.
- Five timeout lines.
- Four unknown-drink success lines.
- Four unknown-drink failure lines.
- An older dialogue asset with three enter, three success, and three failure lines that is not the normal generic route.

The visible repetition risk remains high: one generic visual, a small bank of fixed hint clauses, and shared outcome reactions. Having 48 legacy/FIT reaction lines does not mean there are 48 distinct authored customers.

The important integration issue is the method-selection route:

- Shake, Build, and Float use prototype method orders without an exact recipe constraint. Their FIT can be N/A while the older emotion/heart response still runs.
- Stir uses `TestStir`, whose displayed order title still refers to Seolya while its exact target is Dry Martini.
- The generic guest’s expressed mood is therefore not consistently preserved as a Recommendation request through method selection.

The general customer system has useful capabilities, but its active content and routing still behave like system placeholders. Fixing the narrow handoff should reuse I12 rather than create a second recommendation evaluator.

### Authored customer inventory

| Customer / encounter | Narrative role | Day / spawn rule | Dialogue and order | Bartending interaction | Persistence / follow-up | Production readiness |
|---|---|---|---|---|---|---|
| Three Dry Martini clue customers | Teach method and ingredient facts | Day 1, slots 1–3 | One fact-bearing line each | Staged learning encounters | Recipe facts persist | Functionally authored; visual/dialogue production pass remains |
| Dry Martini experiment customer | Put the clues into practice | Day 1, slot 4 | One-line exact Dry Martini request | Production Stir route | Successful recipe discovery persists | Reference experiment; amount teaching/pacing still needs acceptance |
| Clarity Recommendation guest | Demonstrate FIT independently of technique | Day 1, slot 5 | One-line request and shared FIT reactions | Authored Stir Recommendation | Recipe/progression effects; no separate guest-memory arc | Functional reference, not broad Recommendation coverage |
| Little Prince first visit | Introduce the narrative question and choice | Day 2, first eligible spawn | Four-line narrative, choice/reply, exact Manwol | Float / Manwol | Choice and first-visit completion persist | Implemented but incomplete as a whole story |
| Little Prince follow-up | Recall the player’s answer | Day 3, first eligible spawn | One branch-specific line, exact Manwol | Float / Manwol | Follow-up completion persists | Implemented recall, not an ending |
| Older/additional literary customers | Broader narrative ambitions | No active production encounter found | Documentation/plans, not shipped encounter coverage | Not implemented | Not implemented | Planned or historical, not production content |

## Day-by-Day Progression

`BusinessDayManager` currently advances the day after eight customer departures. Successful service and patience-expiry departures both contribute to that count. This is a departure-based day structure, not eight guaranteed successful drinks.

| Day | Ingredient / recipe unlock | Actual authored composition | Generic / legacy slots | Business result and persistent consequence |
|---|---|---|---|---|
| Day 1 | Dry Martini supply | Three clues, one experiment, one Clarity Recommendation | Three remaining departures route into older forced Stir, Shake, and Build behavior | Recipe facts/knowledge and next-day progression persist |
| Day 2 | Negroni supply | Little Prince first visit if incomplete | Seven generic guests | Choice and first-visit state persist; day advances; Negroni has no authored introduction |
| Day 3 | Manhattan / Hanky Panky supply | Little Prince follow-up if first visit completed and follow-up incomplete | Seven generic guests when follow-up is eligible; otherwise generic fallback | Follow-up state and recipe knowledge persist; no Manhattan/Hanky Panky introduction |
| Day 4 | Gin Fizz / French 75 supply | No authored production encounter progression found | Eight generic guests | Day/knowledge continue; this is a supply-data tier, not an authored night |
| Day 5 | Last Word supply | No authored production encounter progression found | Eight generic guests | Day/knowledge continue; no Last Word discovery context |
| Day 6+ | No further staged unlocks | No further authored progression found | Generic repetition continues | Day number continues increasing without an ending |

The Day-1 composition above is a source-level inference from `CustomerSpawner` and the method-selection/tutorial flags in `InteractionUIController`. The dedicated discovery/recommendation encounters do not consume the older forced-method flags. With `IsTutorialDay` limited to Day 1, slots 6–8 use the first three old forced routes. A ninth slot would be needed for the old forced Float lesson, but the day ends at eight.

This does not invalidate the controlled I16 proof. It distinguishes that proof from complete acceptance of the actual eight-customer tutorial pacing. In particular, the player can reach the Prince’s Day-2 Manwol request without the older Float tutorial appearing.

Authored progression effectively stops at the first eligible Day-3 Prince return. Later days have system data and generic service, not a complete authored production journey.

## Little Prince / Main Narrative

### First visit — implemented

- Scheduled on Day 2 at the first eligible spawn when the first visit is incomplete.
- Four narrative lines establish his arrival, a question about adults, numbers versus a star, and a taste he cannot forget.
- The player chooses `KeepTheLight` or `SeekTheStar`.
- Each choice has an immediate authored reply.
- The choice is persisted independently of recipe knowledge and mechanical result.
- The order is exact Manwol, using Float.
- First-visit completion is recorded on departure.

This is real narrative implementation, not merely a planned choice. It is also not yet a self-contained mini-arc: Manwol instruction, presentation, outcome semantics, and final closure remain incomplete.

### Follow-up — implemented

- Scheduled on Day 3 at the first eligible spawn after the first visit has completed, provided the follow-up is incomplete.
- `KeepTheLight` recalls protecting/keeping the light.
- `SeekTheStar` recalls the star/journey interpretation.
- The legacy/`None` branch avoids pretending a choice was made when no valid persisted choice exists.
- Each branch currently supplies one follow-up line.
- The order is again exact Manwol.
- Follow-up completion is recorded on departure.

There is no implemented story after this return: no final farewell, third visit, relationship escalation, environmental consequence, or ending. The service response uses shared reaction support; dedicated post-success/post-failure dialogue arrays do not provide a further authored story sequence.

### Actual main narrative versus design material

| Status | What exists |
|---|---|
| IMPLEMENTED | A quiet bar, emotion/request interpretation, recipe discovery, and a short Little Prince choice-and-recall sequence |
| DOCUMENTED BUT NOT IMPLEMENTED | Broader ACT structure, memory/statue ambitions, Pilot/Rose material, revisits, and additional literary customers |
| OLDER / OBSOLETE | Documentation passages describing no persistence or the old four-part Day-1 tutorial as if they were the current complete flow |
| NOT YET DESIGNED AS A CONCRETE PRODUCTION ENDPOINT | A final act/day, closing trigger, final dialogue sequence, ending presentation, and bounded demo completion |

The current main narrative, as represented in shipped project content, is a short remembered response from the Little Prince embedded in bartending service. The broader idea of memories accumulating in the bar is a documented vision, not a completed runtime story.

A possible final “설화” cocktail or ending concept is not an implemented ending. It remains unresolved design material rather than a production asset/trigger chain. The Little Prince narrative asset and the roadmap documentation should be read with that distinction intact.

## Ending Status

| Ending component | Actual status |
|---|---|
| Final day | Missing; the day counter continues |
| Ending trigger | Missing |
| Ending dialogue | Missing |
| Ending scene or final panel | Missing |
| Ending persistence | Missing |
| Title-screen ending consequence | Missing; no production title flow exists |
| Credits | Missing |

The Prince follow-up-completed flag is a visit-progress flag, not game completion. There is no explicit endpoint distinguishing “the demo is complete” from “more content should appear tomorrow.”

## Business Progression

`currentDay` advances after the eighth departure, before the player activates the next-night transition. The next-night flow reloads the Bar scene. This ordering matters for quit/reload expectations around Day Result.

Reputation is awarded through existing mechanical/request-result handling. Orders generally provide a `reputationReward` of one when the relevant conditions are met; the request reward can be withheld when the request is not satisfied. Patience expiry and service-result feedback also participate in the visible day loop.

However, reputation is not persisted across the Bar reload. It is displayed/accumulated for the current runtime session/night but is not an established long-term progression resource.

No meaningful production consumer was found that uses reputation to unlock ingredients, advance story, schedule the active authored encounters, or determine a win/failure condition. An older reputation-sensitive `ChooseCustomerPrefab` path does not supply such progression in the inspected configuration because its special-guest collection is empty.

The Day Result includes an unlock/discovery summary, but the Korean label “새로 기록된 칵테일” currently describes a list of all Known cocktails, including the default-known recipe, rather than only recipes newly discovered during the completed night. That is a concrete truthfulness/UI bug, not a request for a new progression system.

Reputation can remain modest nightly feedback for the demo. The project does not need an economy or progression tree merely to justify the current display. It does need consistent naming and an explicit decision about what, if anything, survives the night.

## Guide / Codex

The knowledge projection is functionally established:

- Unknown entries hide recipe information.
- Partial entries reveal known facts.
- Known entries expose full ingredients and authored preparation data.
- Garnish and glassware metadata can be presented.
- Grid/detail navigation, back/close, and method guidance exist.
- Discovery notifications are delayed/sequenced rather than immediately competing with every result.
- Day Result can remain present while the Codex is opened.

The production gaps are presentation and clarity:

- All seven IBA cocktail images are missing. A Known entry can fall back to the same generic/locked-looking art used for an Unknown entry.
- Validation-oriented descriptions such as “Official IBA validation recipe…” are not final game prose.
- Original cocktails can expose the absence of authored preparation with text such as “No preparation sequence recorded.”
- Oz/ml presentation is inconsistent across the game’s measurement and guide surfaces.
- Partial entries can still look very similar to Unknown entries through `???` and limited visual differentiation.
- Long detailed recipes need normal-window readability and navigation testing.
- Recipe knowledge and current ingredient availability are separate authorities, but the Guide does not yet make that distinction sufficiently legible to a player.

The formatter and knowledge system are functional. Final images, authored descriptions, unit conventions, and layout acceptance are not complete.

## UI

| Surface | Classification | Major gap |
|---|---|---|
| Main / title | MISSING | No clear New Game, Continue, save detection, or ending-aware entry flow |
| Bar HUD | FUNCTIONAL PLACEHOLDER | Incomplete production information hierarchy; some `UIManager` text references are null |
| Dialogue | FUNCTIONAL PLACEHOLDER | Final styling, speaker readability, text pacing, and reading-state behavior need acceptance |
| Choice | FUNCTIONAL PLACEHOLDER | Reuses working buttons; not a finished narrative-choice presentation |
| Bartending | FUNCTIONAL PLACEHOLDER | Working controls mixed with placeholder art and English instructions |
| Guide / Codex | FUNCTIONAL PLACEHOLDER | Missing art, unfinished prose, unit/layout consistency |
| Service-result toast | FUNCTIONAL PLACEHOLDER | Functional hierarchy, diagnostic/English terminology still visible |
| Recipe unlock popup | FUNCTIONAL PLACEHOLDER | Image coverage and localization incomplete |
| Day Result | FUNCTIONAL PLACEHOLDER | Cumulative Known list is labeled as newly recorded; final composition/presentation unfinished |
| Settings | VISUAL PLACEHOLDER | A visible button is not a functioning settings implementation |
| Tutorial / instruction surfaces | FUNCTIONAL PLACEHOLDER | Fragmented across method prompts and old tutorial routing |
| Ending / credits | MISSING | No player-facing completion surface |

### Title / main menu

There is no production Start/New Game/Continue menu, save-aware button state, title Guide access, working settings route, or ending-dependent title state. Direct scene entry is not a complete player lifecycle. No production title-screen Exit flow was found.

### Settings

No functioning production controls were found for volume, resolution, fullscreen, text speed, control remapping, language, or accessibility. Their absence should not automatically become a mandate to implement every setting.

For the intended small portfolio build, the practical minimum is volume/mute once audio exists, clear control help, and readable text/UI in the supported window/display configuration. Broader accessibility and customization options should be selected against actual demo needs rather than expanded into a separate platform project.

## Bartending Presentation

| Method | Controls / instruction discoverability | Feedback and art | Audio | Final feel status |
|---|---|---|---|---|
| Build | Space / I / D / J and Finish/Serve controls are present; a keyboard instruction block exists | Functional preparation; limited pour presentation; inactive old score visuals remain | None | I15 functional PASS; human acceptance pending |
| Stir | Preparation controls, mouse-orbit stirring, ice, and strain flow exist | Feedback is functional; final-glass presentation remains placeholder | None | I15 functional PASS; human acceptance pending |
| Shake | Left/right note interaction and judgement communicate the action | Functional timing/judgement, but silent rhythm and placeholder final-glass presentation; secondary transfers need teaching | None | I15 functional PASS; human acceptance pending |
| Float | Space / Shift / Ctrl communicate intensity/order control | Functional layer/order/boundary feedback, simple presentation; Manwol onboarding is incomplete | None | I15 functional PASS; human acceptance pending |

I15 proved functional behavior. It did not replace the owner’s subjective sign-off on pouring response, Stir feel, Shake readability/timing, or three-layer Float control. These remain human acceptance tasks.

Garnish currently communicates through keyboard/text/color-oriented proof presentation. Glassware metadata does not imply that each cocktail has a distinct production glass visual. Functional garnish and serving-glass truth should be preserved while those visuals are finished.

## Audio

| Category | Owned assets found | Actual playback coverage |
|---|---|---|
| BGM | None | None |
| Ambient | None | None |
| UI SFX | None | None |
| Bartending SFX | None | None |
| Customer / dialogue SFX | None | None |
| Environment SFX | None | None |

The project has listener infrastructure, but no owned audio clips or corresponding production `AudioSource`/clip playback coverage was found. The I15 finding that there were no owned bartending sounds to reuse remains true project-wide.

A credible minimum polished sound set is small:

- One bar/night BGM track.
- One quiet room/exterior ambience layer.
- UI confirm, back/cancel, and recipe-discovery feedback.
- Pour start/loop/stop or an equivalent controllable pour sound.
- Ice, glass placement/contact, stirring, shaking, and strain/transfer feedback.
- A restrained guest entrance/exit cue.

Several actions can share source recordings with sensible variation. Voice acting, a soundtrack per guest, and a large adaptive-music system are not required. No audio was sourced or downloaded during the audit.

## Environment / Art

### Bar environment

The active bar is a functional greybox with some existing art, not a finished production environment:

- Counter and bar layout rely heavily on colored squares/basic tiles.
- Lighting is a white Global 2D light at intensity 1 with default-like presentation rather than a finished night-lighting composition.
- The orthographic/pixel-perfect camera setup is functional.
- Three seats are active; three older seat objects remain disabled.
- The shelf and bar tools communicate system access, but ingredient/tool recognition is not yet supported by finished art.
- The nominal snow/background presentation includes tinted square/placeholder elements; no completed weather/snow/wind animation system was found.
- Three statue/memory positions remain placeholders rather than implemented narrative environmental consequences.
- Customer/tool motion exists in code, but a finished authored environmental animation pass is absent.

Interaction readability needs to be tested after the visual direction is fixed. The current ability to click an object is not equivalent to a readable finished scene.

### Character presentation

`Customer_A` and `LittlePrinceCustomer` use the same basic sprite source, with tint differentiating the Prince. Standing/sitting sprite assignments are null in the inspected setup. There are no production portraits, expression variants, or authored character animation coverage supporting the Prince’s narrative identity.

The narrative tone is therefore mostly text/system-driven. A reviewer is asked to recognize a special character from dialogue and scheduling rather than distinct visual characterization.

### Cocktail art

| Cocktail group | CocktailData art | Guide / unlock art | Result / display presentation | Backlog |
|---|---|---|---|---|
| Seolya | Assigned | Reused from cocktail art | Generic method/result presentation | Check visual consistency and production suitability |
| Yeomyeong | Assigned | Reused from cocktail art | Generic method/result presentation | Prototype/reserve decision before further art investment |
| Janseol | Assigned | Reused from cocktail art | Generic method/result presentation | Prototype/reserve decision before further art investment |
| Manwol | Assigned | Reused from cocktail art | Generic Float/result presentation | Make Prince-serving presentation coherent with the chosen style |
| Dry Martini | Missing | Missing / fallback | Generic method/result presentation | Required for proposed demo |
| Negroni | Missing | Missing / fallback | Generic method/result presentation | Required for proposed demo |
| Manhattan | Missing | Missing / fallback | Generic method/result presentation | Required for proposed demo |
| Gin Fizz | Missing | Missing / fallback | Generic method/result presentation | Required for proposed demo |
| Hanky Panky | Missing | Missing / fallback | Generic method/result presentation | Reserve; defer production art |
| French 75 | Missing | Missing / fallback | Generic method/result presentation | Reserve; defer production art |
| Last Word | Missing | Missing / fallback | Generic method/result presentation | Reserve; defer production art |

The existing cocktail illustration style, pixel-oriented tools, and very simple bar art are not yet a unified presentation. Existing art being assigned does not by itself establish final production quality or consistent provenance.

### Ingredient art

All 26 ingredient icon assignments are null. Shelf UI suppresses the empty icons and relies on text, so this is missing visual coverage rather than evidence that every ingredient has an incorrect reused bottle sprite.

Shelf recognition particularly needs to distinguish Gin from London Dry Gin, the vermouth variants, and fresh juice identities. Preserve those exact identities while improving recognition. The production backlog should initially cover the selected demo ingredients, not automatically all reserve content.

## Localization / Player-Facing Text

Production-facing issues include:

- English Guide labels such as Unknown Cocktail, Method, Ingredients, Preparation, and Glassware.
- Service-result terms such as Guest Fit, Drink, Technique, and reward withheld.
- Bartending instruction/status text such as Liquid Ready, Begin Stir, and Finish Build.
- Gin Fizz explanatory text: “This is SURLHWA gameplay tuning, not an official IBA volume.”
- IBA descriptions beginning with validation-oriented language such as “Official IBA validation recipe…”.
- Generic choice labels such as Confirm Button / Cancel Button where a narrative-specific presentation is expected.
- Inconsistent ingredient naming/localization, including Vermouth, Water, and Coffee Liqueur variants.
- Oz/ml inconsistencies between measurement and recipe presentation.
- Korean terminology that inconsistently distinguishes discovery, recording, and unlocking.

The inspected production Korean dialogue is stored/read as valid UTF-8. Some damaged text occurs in older comments/material, but the audit did not establish a broad production-dialogue encoding failure.

Serialized defaults such as `New Text` or `테스트` that are overwritten before use are not automatically player-visible leaks. The release pass should follow actual runtime presentation rather than rename every internal placeholder string.

A focused production-language pass is needed. Rewriting all narrative text or building a full localization platform is not implied.

## Legacy / Debug Residue

| Residue | Classification | Evidence / release implication |
|---|---|---|
| Legacy Jigger visuals | SAFE INACTIVE | Parent presentation is inactive; removed controller authority must not be restored |
| Build legacy score visuals | SAFE INACTIVE | Hidden/inactive, not actual preparation truth |
| Stir `GaugeRoot` | SAFE INACTIVE | Hidden/inactive legacy presentation |
| Float `TargetZone` | SAFE INACTIVE, with reused surrounding roots | Old target-zone presentation is inactive; do not delete roots blindly |
| Old `jiggerPanel` serialized reference | SHOULD CLEAN BEFORE RELEASE | Null/stale serialized residue, not the active quantity mechanism |
| `OldLayout` and disabled old seats | SAFE INACTIVE | Legacy scene layout residue |
| `TestStir` / `TestShake` / `TestBuild` / `TestFloat` | ACTUALLY STILL USED | Production method selection still depends on these orders |
| Older Day-1 forced-method flags | ACTUALLY STILL USED | Affect the remaining Day-1 customer slots |
| `debugSpawnWithKey` enabled | SHOULD CLEAN BEFORE RELEASE | Space can accelerate spawning between customers |
| Developer logs | SHOULD CLEAN BEFORE RELEASE where noisy | Not a UI label, but can affect release logging and obscure useful diagnostics |
| Recovery/layout scenes, older JPGs, `Customer_B` | LEGACY-INACTIVE / NEEDS REVIEW | Not active content; retain until dependency/provenance decisions are explicit |
| Editor migration and validation helpers | VALIDATION-ONLY | Useful tooling, not player-facing leakage |

Internal `Test*` names and recipe IDs are not by themselves production gaps if the player never sees them. The real leakage is unfinished visible prose, misleading order behavior, and enabled debug input.

`CompleteBuild` is an internal action/method name; the visible Finish Build control is part of the real preparation flow, not proof that the old score-based minigame remains authoritative.

## Persistence / Save / New Game

### Persisted state and ownership

| Key | Category / owner | Purpose |
|---|---|---|
| `currentDay` | Business progression | Current day progression |
| `SURLHWA_RECIPE_KNOWLEDGE_V1` | Recipe knowledge persistence | Versioned recipe state and learned facts |
| `SURLHWA_UNLOCKED_RECIPES` | Legacy compatibility | Older unlock data used for migration |
| `SURLHWA_FIRST_RECIPE_REVEAL_SHOWN` | Tutorial / UI state | First-reveal presentation state |
| `littlePrinceFirstVisitCompleted` | Narrative progression | First Prince visit completion |
| `SURLHWA_LITTLE_PRINCE_FOLLOWUP_VISIT_COMPLETED` | Narrative progression | Follow-up completion |
| `SURLHWA_LITTLE_PRINCE_NARRATIVE_CHOICE` | Narrative progression | Stable KeepTheLight / SeekTheStar / None choice |

Strengths include stable recipe identities, a versioned recipe-knowledge payload, migration from legacy Known/unlock data, malformed-data guards, invalid narrative-choice fallback to None, and idempotent handling. Ownership is bounded, and the inspected implementation does not save continuously every frame.

Limitations are important but do not justify a wholesale rewrite:

- There is no persisted mid-night customer index, active encounter, or dialogue position.
- Reloading can restart the night’s sequence while preserving facts and choices learned before the reload.
- The day value advances before the Next Night action, which needs an explicit Continue/resume contract around the result screen.
- There is no ending state or full player-accessible reset.
- Version/fallback handling is not a backup/recovery system for arbitrary corruption.
- Stable IDs must remain stable during later cleanup or asset renaming.
- Reputation is not part of the persisted long-term state.

### Production lifecycle support

| Player action | Current status |
|---|---|
| Start New Game | Missing in production UI |
| Reset Progress | Missing in production UI |
| Continue | Automatic entry into saved progression through Bar scene loading, not a clear production menu flow |
| Debug/editor reset | Partial preparation/reset helpers exist |

The Prepare Day 1 / Prepare Day 2 helpers do not reset all recipe knowledge, narrative choice, and follow-up state. They are not reliable substitutes for a production New Game command.

The current persistence architecture is sufficient for a bounded small game if the lifecycle/reset/resume contract is completed. A generalized save framework is not required. `RecipeProgressPersistence` and the existing state owners should be extended narrowly and regression-tested.

## Edge Cases

| Edge case | Classification | Audit finding |
|---|---|---|
| Customer patience expires | HANDLED | Penalty/feedback/departure/day counting exist |
| Unknown drink | HANDLED | Graceful result path; unknown composition is not falsely assigned a known-drink FIT |
| Failed bartending technique | HANDLED | Technique outcome remains separate from drink identity and request satisfaction |
| Request needs normally locked ingredients | HANDLED | Scoped request availability support exists |
| Generic impossible or poorly supported request | PARTIAL | Requests are not fully curated against available, reachable fitting answers |
| Empty/no usable ingredient state | PARTIAL | Guards exist; a complete player-facing recovery path is not established |
| Interrupted dialogue | PARTIAL | Runtime pause/cleanup support exists; dialogue position is not persisted |
| Day reload | PARTIAL | Knowledge/choice persist, encounter sequence does not |
| Corrupted / legacy save | PARTIAL | Migration/fallback support exists; no complete player recovery/reset UI |
| Guide opened during technique interaction | PARTIAL | No single shared modal gate guarantees technique input/timing isolation |
| Codex opened while a bar customer waits | PARTIAL | Reading does not clearly pause patience |
| Popup overlap / final-customer transition | PARTIAL | Service sequencing has validation; final-customer and broader overlap cases still need production proof |
| Prince leaves without successful service | PARTIAL | Departure completes visit state; retry/failure narrative semantics need an owner decision |
| Additive bartending scene load failure | HANDLED | Documented retry/cleanup handling exists for the failure path |

Not every partial edge case is an immediate release blocker. Reading-state safety, honest requests, a reliable reset/continue contract, and closing-night behavior directly affect a coherent demo and should be included in release acceptance.

## Technical Risks

No broad optimization or architecture rewrite is justified by this audit. The evidence supports a small set of concrete risks:

- Content-routing correctness is more urgent than low-level performance. Generic mood requests can lose their meaning when mapped to prototype method orders.
- UI/input isolation is incomplete across Guide, active technique interaction, patience timing, and transitions.
- Name-based auto-binding and intentionally null serialized references make later scene/object renames risky. A null reference must be judged against runtime binding before being called a defect.
- Candidate/card and other diagnostic logging should be reviewed for release noise.
- Transient row/text generation allocates objects, but the catalog is small. Profile a production build before optimizing it.
- Partial reset helpers and exact-day scheduling can produce confusing test/save combinations. A full lifecycle contract is more useful than adding another isolated reset button.
- Small-dose matching deserves a policy check: the inspected Manhattan case can omit a 1 ml dash and still score approximately 0.874 against a 0.85 threshold. Preparation evidence is a separate authority; decide what production acceptance should mean before changing matching rules.
- Generic orders with no `ExpectedRecipe` do not automatically enforce a cocktail’s authored preparation sequence. Do not assume a successful identity match proves all expected preparation steps were evaluated for that request.

Positive findings:

- Inspected production script references resolve to local or package scripts; no null-script production break was established.
- Shelf generation clears its existing rows; no persistent duplicate-shelf accumulation was established.
- The inspected learning/event lifecycle did not establish a repeated-subscription leak; relevant runtime objects are scoped/cleaned up.
- The bartending coordinator contains transition/duplication guards.
- No runaway per-frame PlayerPrefs save loop was established.

These are bounded release checks, not reasons to replace the working runtime truth model.

## Validation Infrastructure

The I10–I16 tooling is extensive. The relevant commands are under the `SURLHWA/Validation` menu family; the distinction between static checks, production Play Mode, and subjective acceptance must remain explicit.

| Phase / command family | Type | What it establishes |
|---|---|---|
| I10 Ingredient Availability | Edit-mode/static data and prefab validation | Staged supply and related wiring |
| I11 Recipe Knowledge | Edit-mode/static | Knowledge states, facts, data, and persistence expectations |
| I11 Recipe Knowledge Play Mode | Normal Editor Play Mode | Runtime knowledge behavior |
| I11B Production Discovery | Edit-mode/static | Production-discovery setup and references |
| I11B Production Discovery Play Mode | Normal Editor, production Stir/runtime route | Runtime discovery integration through production objects |
| I11C Dry Martini Discovery Arc | Edit-mode/static | Authored arc, scheduling, and fact content |
| I12 Guest FIT | Edit-mode/static | Exact/recommendation evaluation and independent FIT expectations |
| I13 Little Prince Narrative | Edit-mode/static | Choice data, branch predicates, and persistence wiring |
| I13 Little Prince Narrative Play Mode | Normal Editor, production integration | Choice/return integration in runtime context |
| I14 Service Feedback | Edit-mode/static | Feedback hierarchy/data expectations |
| I14 Production UI Play Mode | Normal Editor, controlled result completion against real UI | Production feedback/popup sequencing |
| I15 Bartending Feel | Edit-mode/functional validation | Functional interaction/presentation contracts, not subjective feel |
| I16 Two-Night Vertical Slice | Edit-mode/integration-oriented validation | Cross-night saved values, scheduling predicates, and slice prerequisites |

Important scope limits:

- I16 checks saved values and production predicates; it is not a full manual execution of sixteen customers using only normal player input.
- I14’s production UI proof uses controlled completion. It does not independently replay every full bartending interaction.
- Existing PASS records are historical evidence from the rebuild, not newly rerun tests in this audit.
- The recorded headless path encountered a 404/missing-entitlement environment problem. That does not prove a gameplay defect; it means the affected automated launch route is unavailable in that environment.
- Production Play Mode checks that rely on the normal Unity Editor remain distinct from batch/static checks.

Long-term retention should prioritize:

- Recipe knowledge, migration, and availability regressions.
- FIT separation and positive/negative contrast.
- Little Prince choice and return persistence.
- Feedback sequencing and production preparation-action contracts.
- A broader normal-play multi-night production slice.
- A short human checklist for I15 feel and reading/pacing.

Historical migrations should remain clearly separated from routine validation commands. Some tools overlap; do not remove them merely to reduce the menu until their unique coverage is known and retained elsewhere.

## Portfolio Readiness

A guided demonstration can currently communicate:

- The quiet-bar/customer-interpretation concept.
- Actual preparation actions and Bartending 2.0 state.
- Dry Martini clue-based discovery.
- One Recommendation FIT contrast independent of technique.
- Persistent recipe knowledge and a remembered Prince choice.
- The established two-night progression slice.

An unguided reviewer would still encounter conspicuous unfinished elements:

- No clear entry/menu/reset lifecycle.
- A single generic visual and a Prince differentiated largely by tint/text.
- Silence throughout the bar and bartending interactions.
- Missing IBA cocktail art and all ingredient icons.
- English/debug-like labels mixed with Korean narrative.
- An insufficiently taught Manwol request.
- Recipes and ingredient unlocks that have no corresponding guest encounter.
- No ending or explicit “this demo is complete” signal.

The README’s older Jigger/preview framing also needs reconciliation with the actual rebuilt project. A five-to-ten-minute guided excerpt is possible now; a self-contained, polished portfolio demo is not yet ready.

## Major Production Gaps

1. Authored progression ends at the first eligible Day-3 Prince return; later days are generic service and supply data.
2. Generic method routing does not consistently preserve the emotional Recommendation request.
3. Only one of the seven IBA cocktails has a complete authored discovery arc.
4. No authored Recommendation encounter currently demonstrates multiple practically reachable valid answers.
5. New Game, explicit Continue, complete Reset, and a demo endpoint are missing.
6. Character distinction, environment art, cocktail/ingredient imagery, final UI copy, and audio remain visibly incomplete.

These are the principal barriers between a proven rebuild and a coherent finished demo. Most are production-content and presentation work, with a small but important lifecycle/routing engineering pass.

## System Work vs Content Work

### SYSTEM WORK STILL REQUIRED

- Production New Game / Continue / Reset behavior, save detection, and explicit resume semantics.
- Bounded night/customer scheduling that preserves the authored anchors and removes the unintended legacy Day-1 tail from the proposed demo flow.
- Preserve the generic guest’s intended emotional request through method selection using the existing FIT evaluator.
- A configured final-night completion state and player-facing endpoint.
- Correct Day Result to report discoveries from the completed night rather than all Known recipes.
- Modal/read-state handling so Guide/dialogue interaction does not unintentionally compete with technique input or customer patience.
- Minimal audio playback/settings integration once owned audio content is selected.
- Broader production-playthrough and save/reset regression coverage.

### CONTENT / POLISH WORK

- Selected cocktail metadata, descriptions, emotion tags, and missing original-drink preparation/glassware authoring where needed.
- Discovery facts, guest clues, orders, and dialogue.
- Overlapping Recommendation requests with reachable valid choices and useful contrasts.
- Generic request/reaction variety.
- Little Prince closing content and owner-approved meaning.
- Cocktail, ingredient, character, UI, and environment art.
- Bartending final-glass/garnish presentation.
- Sound selection, editing, timing, and mix.
- Korean terminology, final player-facing prose, unit consistency, and readability.
- Human tuning of controls, scene pacing, and narrative pauses.

Adding a Negroni clue encounter is content work through the existing discovery machinery. It is not evidence that a generalized quest engine is missing.

## MUST / SHOULD / COULD

| Priority | Remaining work |
|---|---|
| MUST | Clear entry/reset/continue/end flow; honest generic request routing; selected cocktails learnable in normal play; a completed Prince mini-arc; reachable multiple-answer Recommendation examples; coherent core UI; credible chosen character/bar/cocktail presentation; basic audio and volume control; a packaged build with save and full-playthrough acceptance |
| SHOULD | More generic visual/request/reaction variety; one meaningful environmental memory trace; clearer Partial cards; better garnish/glass presentation; restrained release logging; additional readability and pacing refinement |
| COULD | More IBA recipes, additional literary guests/nights, richer expression/animation coverage, broader settings and language support |

Reserve cocktail art, a complex economy, and a large relationship system are not MUST items. The demo should be complete within a small scope, not expanded until every optional idea becomes a blocker.

## Delete / Defer

Do not build these before the bounded demo is complete:

- A generalized quest engine.
- A universal NPC-memory or large relationship framework.
- A replacement save architecture when bounded lifecycle fixes suffice.
- Advanced fluid, ice, garnish, or glass simulation.
- A large IBA catalog or forcing every selected reserve cocktail into the demo.
- New York Sour or other additions that reopen unresolved mechanics before current content is finished.
- A reputation economy, stock purchasing, or bar-upgrade tree.
- A multi-ACT narrative expansion, procedural narrative system, or multiplayer.

Also defer broad asset renaming/deletion until active dependencies are replaced deliberately. `Test*` naming cleanup is less urgent than correcting the behavior those orders currently carry. Do not delete useful inactive material simply to make an audit inventory look cleaner.

## Recommended Demo Scope

The recommended bounded target is four playable nights with six production cocktails, one completed Little Prince mini-arc, multiple valid Recommendation choices, a coherent bar presentation, basic audio, and an explicit beginning and endpoint.

| Content dimension | Recommended minimum | Why this is sufficient |
|---|---|---|
| Playable nights | 4 | Uses existing Day-1/Day-2 anchors, permits a return and closure, and reaches a meaningful Shake introduction without forcing Day 5 |
| Production cocktails | 6 | Seolya, Manwol, Dry Martini, Negroni, Manhattan, Gin Fizz cover contrast, narrative service, and the four method families |
| Learning paths | 5 | Dry Martini, Negroni, Manhattan, Gin Fizz, and Manwol; Seolya begins Known |
| Longer discovery arcs | 2 | Preserve Dry Martini and add Negroni; remaining learning paths can be compact rather than repeat the same multi-customer structure |
| Recommendation request types | 4 | Enough to show interpretation rather than a single Clarity password |
| Explicit multiple-answer requests | At least 2 | Demonstrates that valid alternatives are a designed gameplay property |
| Generic request phrasings | 12 | Three variants for each of four supported request types reduce obvious repetition without excessive writing |
| FIT reaction lines | 8 | Two variants for each of four FIT result categories, extending/reusing the existing base |
| Major special narrative customers | 1 | A finished Prince arc is more valuable than several introductions with no closure |
| Prince visits | 3 | First visit, remembered return, and closing visit |
| Narrative beats | 5 | Arrival/question, player response, immediate reply, remembered return, and closure |
| Total encounters | Approximately 17 | A proposed 5 + 4 + 4 + 4 structure keeps each night purposeful |

Four nights let the demo use supply progression through Day 4 while reserving Day-5 complexity. The full four-night run should not be promised as five-to-ten minutes without playtesting. A five-to-ten-minute portfolio video or guided excerpt can show representative moments from that larger bounded playthrough.

## Recommended Cocktail Set

Keep Seolya as a Known baseline/contrast and Manwol as the Prince’s narrative drink. Add production content around four of the seven IBA candidates rather than forcing all seven into the playable demo.

| IBA cocktail | Demo decision | Proposed introduction | Discovery style | Recommendation role | Content dependencies |
|---|---|---|---|---|---|
| Dry Martini | Include | Day 1 | Preserve the existing clue → experiment → Recommendation arc | Preserve Clarity reference and contrast | Final art/copy, readable amount/preparation teaching, pacing acceptance |
| Negroni | Include | Day 2 | Two-encounter arc transferring known Gin knowledge to new ingredients and GentleStir | Proposed Bitterness interpretation and later overlap | Emotion tags, clues/orders/dialogue, art, supported method/supply access |
| Manhattan | Include | Day 3 | Compact clue and experiment emphasizing the dash and familiar Stir workflow | Proposed Warmth/Bitterness; same-method overlap with Seolya is useful | Ingredient/amount clue, reaction content, tags/art, small-dose acceptance policy |
| Gin Fizz | Include | Day 4 | Compact Shake → transfer → Splash lesson and practice | Proposed Clarity/Awakening alternatives | Tags/art, clear secondary-transfer instruction, encounter method access |
| Hanky Panky | Reserve / data-only | Existing data tier is Day 3; playable introduction after the demo if desired | Later shared-vermouth/precision lesson | Potential future overlap, not required now | London Dry Gin/Fernet introduction and an extra encounter would add scope without a necessary new demo technique |
| French 75 | Reserve / data-only | Existing data tier is Day 4; optional later introduction | Later extension of Gin Fizz’s Shake/transfer knowledge | Future authored interpretation | Champagne, flute/art, and guest context add avoidable scope to the closing night |
| Last Word | Reserve / data-only | Existing data tier is Day 5; post-demo | Equal-measure discovery | Future authored interpretation | Three additional ingredients and encounter/art work do not advance the proposed four-night narrative |

Proposed emotion assignments above are recommendations requiring owner approval, not claims about current assets. Current data has only Dry Martini’s Clarity tag among these seven.

Yeomyeong and Janseol should remain available for compatibility until their active routes are replaced deliberately. After that, they can be omitted from the demo-facing catalog if desired. This audit does not authorize deleting their assets.

## Recommended Day Structure

This is proposed future content, not a description of current implementation. It retains the existing authored Day-1/Day-2 anchors.

| Day | Purpose / encounter target | Ingredients and discovery | Recommendation | Story beat |
|---|---|---|---|---|
| DAY 1 | Observation, learning, and first confident service; 5 encounters | Preserve the three Dry Martini clues and experiment; keep the existing supply lesson focused | Preserve the starter Clarity encounter | Establish the bar’s premise and the player’s interpretive role |
| DAY 2 | Broaden preparation through Build and Float; 4 encounters | Existing Negroni unlock; two Negroni encounters; explicit Manwol guidance | One supported Recommendation encounter | Preserve the Prince’s first visit and choice |
| DAY 3 | Use retained knowledge and offer meaningful alternatives; 4 encounters | Manhattan clue/practice using Rye Whiskey and Angostura Bitters | Warmth-style choice with Seolya/Manhattan reachable through Stir, subject to approved tags | Preserve the Prince’s branch-aware recall |
| FINAL DEMO NIGHT / DAY 4 | Demonstrate Shake and close the small story; 4 encounters | Gin Fizz lesson/practice using lemon juice, simple syrup, and soda | An overlapping request that does not make the newly taught drink the sole password answer | Prince closure and explicit demo-completion presentation |

The proposed removal of Day-1 slots 6–8 has a concrete reason: they are legacy tutorial tails after the five new anchors, and still do not fit the old Float lesson into the day. The proposal does not overwrite the completed discovery arc.

Reserve-specific ingredients should be removed from the demo-facing supply only after their actual dependencies are checked. The current audit changes no availability data.

## Post-I16 Phase Roadmap

The J-family shifts the project from architecture rebuilding to bounded production completion. These phases are proposed work, not implemented features.

### J1 — Coherent Demo Flow Closure

**Objective**

Provide one honest, bounded player journey through the existing production systems.

**Scope**

- New Game, Continue, full Reset, and clear save/resume behavior.
- A configured four-night/customer schedule preserving the five Day-1 authored anchors.
- Remove the unintended legacy tutorial tail from the selected demo flow.
- Correct the generic Stir hidden exact-target problem and preserve intended emotional requests through method selection.
- Minimal final-night stop/completion flow.
- Truthful completed-night discovery reporting in Day Result.

**Non-goals**

New cocktails, a replacement evaluator, final narrative prose, major art work, or a generalized quest framework.

**Acceptance**

A fresh or resumed player enters predictable content through production UI; all relevant progress resets reliably; Day 1 preserves its five authored anchors; generic requests retain their meaning; night summaries report the correct scope; the configured final night does not continue indefinitely.

**Dependencies**

Acceptance of the bounded demo target in this audit and preservation of the I10–I16 contracts.

**Likely assets/files affected**

`CustomerSpawner`, `InteractionUIController`, order routing/data, `BusinessDayManager`, existing persistence owners, Day Result, Bar entry flow, and bounded menu/completion UI.

### J2 — Selected Cocktail and Ingredient Production Data

**Objective**

Make the six selected demo drinks complete and internally coherent as production data.

**Scope**

- Final player-facing descriptions and approved emotion tags.
- Original-drink preparation/glassware data where appropriate to the demo.
- Catalog/supply curation for selected versus reserve content.
- Clear ingredient labels while preserving exact identities and stable IDs.

**Non-goals**

Additional mechanics, every IBA cocktail, or expanding all reserve recipes into encounters.

**Acceptance**

Each selected drink has the metadata required by its production route; every exposed ingredient has a planned player-facing use; identity/persistence compatibility remains intact.

**Dependencies**

J1’s selected scope and routing/lifecycle contract.

**Likely assets/files affected**

Selected `CocktailData`, `CocktailRecipeData`, recipe catalog, ingredient availability data, and ingredient data/labels.

### J3 — Customer and Recommendation Content

**Objective**

Make interpretation varied and make valid alternatives genuinely playable.

**Scope**

- Four supported Recommendation request types.
- Twelve request phrasings.
- At least two requests with multiple reachable valid cocktails.
- FIT response variants and contrasting outcomes.
- Guest scheduling and method access consistent with known recipes and available supply.

**Non-goals**

A new emotion taxonomy, procedural dialogue generator, or replacement evaluation code.

**Acceptance**

Every authored request has a reachable fitting answer and an understandable contrast. At least two requests allow two or more reachable valid answers in actual normal play, not merely in tag-level unit tests.

**Dependencies**

J1’s request preservation and J2’s selected cocktail metadata.

**Likely assets/files affected**

`OrderData`, `DialogueData`, the reaction database, and the bounded customer schedule.

### J4 — Recipe Discovery Expansion

**Objective**

Make every selected non-default recipe learnable without outside recipe knowledge.

**Scope**

- A Negroni discovery arc.
- Compact Manhattan and Gin Fizz learning encounters.
- Explicit Manwol guidance appropriate to the Prince route.
- Amount/preparation clues where the current facts do not sufficiently support experimentation.
- Persistence and wrong-experiment regression coverage.

**Non-goals**

A quest engine, recipe autofill, automatic correction of wrong preparation, or identical long arcs for every drink.

**Acceptance**

A new player can explain what each clue taught and use it to progress from Partial knowledge to a successful recipe. Failed experiments remain meaningful and do not grant false facts. Learning survives reload according to the established save contract.

**Dependencies**

J2’s selected data and J3’s encounter/request context.

**Likely assets/files affected**

Discovery orders, dialogue assets, fact-reveal data, customer scheduling, and targeted discovery validators.

### J5 — Little Prince Mini-Arc Completion

**Objective**

Complete one small story rather than introduce several unfinished stories.

**Scope**

- Preserve the implemented first visit and branch-aware recall.
- Author a closing visit/closure.
- Define appropriate branch-aware closing text.
- Decide timeout/failed-service/retry semantics.
- Connect the narrative closure to the bounded demo endpoint without turning a visit flag into an accidental game ending.

**Non-goals**

Other ACTs, universal NPC memory, or a large branching relationship structure.

**Acceptance**

Both stored choices and the legacy/None case reach authored closure. Completion, replay, and failed-service behavior are explicit and understandable. The owner approves the meaning and tone.

**Dependencies**

J1’s endpoint/lifecycle support and J4’s Manwol onboarding.

**Likely assets/files affected**

Little Prince narrative/dialogue/order assets, bounded narrative flags and scheduling, and demo-ending presentation.

### J6 — UI, Cocktail, Ingredient and Character Art Pass

**Objective**

Remove the visible proof/prototype presentation from the selected playthrough.

**Scope**

- Art coverage for all six demo cocktails.
- Readable demo ingredient visuals/labels.
- Distinct Prince and useful generic customer variants.
- Guide, dialogue, choice, result, and menu presentation.
- Final Korean player-facing terminology and consistent units.
- Serving-glass/garnish presentation appropriate to the chosen style.

**Non-goals**

Reserve-cocktail art, elaborate animation systems, or a broad UI framework replacement.

**Acceptance**

No development-oriented prose or unintended placeholder labels remain in the selected route. All selected cocktail images are present. Guide and other UI remain readable in the supported window/display configuration. Characters and ingredients can be recognized without developer explanation.

**Dependencies**

Stable selected content from J2–J5.

**Likely assets/files affected**

Art assets, UI and bartending prefabs, customer prefabs, Bar scene presentation, and formatters.

### J7 — Audio and Bar Environment Pass

**Objective**

Make the game read and sound like a quiet winter bar rather than an interaction test scene.

**Scope**

- Minimal owned BGM, ambience, UI, bartending, and entrance/exit sound set.
- Playback integration with volume/mute control.
- Bar dressing, lighting, camera composition, and interaction readability.
- Optionally one visible memory trace if it directly supports the completed Prince arc.

**Non-goals**

Weather simulation, a soundtrack per guest, or a large environment expansion.

**Acceptance**

The selected playthrough is audibly responsive, visually legible, and atmospherically consistent. The owner approves the visual/audio direction and mix. Audio can be reduced or muted through production UI.

**Dependencies**

J6’s visual direction, plus J5 if an environmental memory trace is included.

**Likely assets/files affected**

Audio assets/playback/settings integration, environment art/prefabs, lighting, and Bar scene presentation.

### J8 — Four-Night Production Slice

**Objective**

Prove the complete bounded playthrough, not only isolated system contracts.

**Scope**

- Production-UI entry and progression through four nights.
- All four preparation methods used in context.
- Selected discovery flows and multiple-answer Recommendations.
- Prince branch paths, night transitions, and the endpoint.
- Targeted fixes for defects exposed by this integrated run.

**Non-goals**

New content expansion beyond fixes necessary for the accepted slice.

**Acceptance**

A fresh player can finish through production UI without editor/debug intervention. Drink identity, preparation evidence, knowledge, availability, FIT, technique, and narrative state remain independent and consistent. The endpoint is reliable.

**Dependencies**

J1–J7.

**Likely assets/files affected**

Integration validators, narrow fixes in the affected production paths, and slice validation documentation.

### J9 — Balance, Save and Release Regression

**Objective**

Make the completed slice dependable and comfortable to play.

**Scope**

- Fresh, legacy, malformed-save, reset, and resume cases.
- Reading/Guide modal behavior, popup sequencing, and final-customer transitions.
- Small-dose recipe matching/preparation acceptance.
- Supported-window readability and focused performance/log review.
- Human I15 feel and pacing acceptance.

**Non-goals**

Premature optimization or architecture replacement.

**Acceptance**

The release matrix contains no blocking progression/save defects, lost choices/knowledge, or unsupported impossible requests. The selected build is readable and stable. Human control-feel sign-off is recorded rather than inferred from automated PASS results.

**Dependencies**

J8’s complete production slice.

**Likely assets/files affected**

Regression validators, narrow tuning/fixes, build settings, and QA documentation.

### J FINAL — Portfolio Demo Release Candidate

**Objective**

Deliver a build that a reviewer can start, understand, and finish independently.

**Scope**

- Standalone build and basic control/concept explanation.
- Credits and asset attribution.
- Accurate README and known-limitations notes.
- Representative screenshots and a short portfolio demonstration video.
- Final owner acceptance of the bounded content and presentation.

**Non-goals**

New systems, cocktails, nights, or narrative expansion.

**Acceptance**

A reviewer needs neither Unity nor debug commands to start and finish. The described features match the build. Credits/provenance are complete for shipped assets, and the owner signs off on the release candidate.

**Dependencies**

J9.

**Likely assets/files affected**

Build configuration, title/credits presentation, README, release documentation, and portfolio capture material.

### Recommended commit strategy

Keep implementation, authored content, art/audio, and validators reviewable in bounded commits. Suitable examples are:

```text
fix: preserve generic guest fit through method selection
feat: add new game and continue flow
fix: report discoveries from the completed night
content: add negroni discovery encounters
content: add day three recommendation choices
content: complete little prince demo farewell
style: finish cocktail guide presentation
audio: add core bartending feedback
test: cover four-night save and progression regression
```

Do not combine every J-phase concern into a giant mixed commit. Keep local Unity layouts/settings out of production commits. No commit is authorized by this audit/export request.

## Immediate Next Phase

```text
NEXT PHASE:
J1 — Coherent Demo Flow Closure

WHY NOW:
The project has proven mechanics but does not yet provide a controlled,
self-contained player journey. Current normal play mixes the new authored
encounters with legacy tutorial/request behavior, has no reliable player
reset/continue shell, and never ends.

DONE WHEN:
A fresh or resumed player enters predictable content through production UI;
all relevant progress can be reset; Day 1 preserves its five authored anchors;
generic requests retain their intended meaning through method selection;
night summaries are truthful; and the configured final night stops at an
explicit demo-completion flow.
```

## Human-Only / Human-Signoff Tasks

Owner participation is required for:

- Subjective I15 control feel: pouring, Stir, Shake, and three-layer Float.
- Final Korean voice, dialogue rhythm, and reading pace.
- What the Prince’s choice and closure mean, including failure/retry semantics.
- Final emotional interpretations assigned to cocktails.
- Art direction, character identity, and visual asset selection.
- Music/SFX taste, intensity, and mix.
- Overall night length, encounter pacing, and portfolio-demo framing.
- Approval of art/audio provenance and attribution for release.

Engineering/data wiring, reference checks, repeatable validation, save regression, and layout/text consistency can be automated or assisted. Those activities cannot substitute for final narrative meaning, aesthetic preference, or subjective control acceptance.

## Master Gap Table

| Area | Current State | Gap | Type | Priority | Recommended Phase |
|---|---|---|---|---|---|
| Core architecture | Established authorities | Preserve during production | Engineering constraint | MUST preserve | All |
| Player lifecycle | Direct Bar entry | New Game/Continue/reset/end | System | MUST | J1 |
| Day structure | Eight departures; legacy tail | Bounded authored schedule | System/content | MUST | J1 |
| Generic requests | Prototype method orders | Consistent emotional FIT | System/content | MUST | J1, J3 |
| Cocktail catalog | Eleven recipes; sparse integration | Six fully supported demo drinks | Content | MUST | J2 |
| Discovery | One complete IBA arc | Learnable Negroni/Manhattan/Gin Fizz/Manwol | Content | MUST | J4 |
| Multiple answers | One practical answer in authored Recommendation | Reachable overlapping choices | Content/routing | MUST | J3 |
| Prince narrative | First visit and recall | Closure and endpoint | Narrative/system | MUST | J5 |
| Day Result | Functional | Cumulative list labeled as new | System/UI | MUST | J1 |
| Guide/UI | Functioning knowledge projection | Art, language, units, layout | Content/polish | MUST | J6 |
| Characters/bar | Basic placeholders | Coherent visual presentation | Art | MUST | J6–J7 |
| Audio | Absent | Minimal soundscape and controls | Audio/system | MUST | J7 |
| Environmental memory | Placeholder slots | One meaningful visible trace | Art/content | SHOULD | J5–J7 |
| Save/edge cases | Bounded persistence | Reset/resume and full-playthrough proof | System/QA | MUST | J1, J8–J9 |
| Human feel | Functional PASS | Subjective acceptance | Human/tuning | MUST | J9 |
| Portfolio delivery | Repository/proof slice | Packaged build, credits, accurate documentation | Release | MUST | J FINAL |

## Final Verdicts

```text
CORE ARCHITECTURE:
READY
Suitable for bounded production content. No major framework replacement needed.

TWO-NIGHT VERTICAL SLICE:
READY
Within the established controlled I16 scope and recorded supporting validations.
This is not blanket acceptance of every eight-customer pacing or usability case.

CONTENT COMPLETENESS:
NOT READY
Authored progression stops early; most IBA recipes lack encounters and discovery.

PRESENTATION / POLISH:
NOT READY
Missing audio, character distinction, IBA art, ingredient visuals, and final UI copy.
I15 subjective feel sign-off remains pending.

PORTFOLIO DEMO:
NOT READY
The essential demonstration exists, but a self-contained beginning, complete
content path, polished presentation, and endpoint are still missing.
```

## Suggested Documentation Output

The original audit recommended that this report become `Docs/13_POST_I16_PRODUCTION_GAP_AUDIT.md`, stamped with the audited HEAD and clearly separating current evidence from proposed scope.

The most valuable accompanying correction would be marking obsolete README/design passages as historical so they cannot be mistaken for current runtime behavior. That remains a recommendation only; this document export does not modify those files.

Original audit-delivery status, before the subsequently authorized Markdown export:

> No documentation file was created, no project files were modified, and no commit was made.

This file is the subsequently authorized documentation output. No recommended production work is implemented by saving it, and no automatic commit is made.


## Production Autopilot Startup Audit / J3 Routing (2026-09-28)

Baseline: branch `refactor/project-rebuild`, HEAD `39a8888`. The only
pre-existing untracked paths were `AGENTS.md` and `.codex/`; neither is
part of this implementation. Historical findings above remain historical.

Current source and serialized data confirm J1 COMPLETE; J2/J2.1 and J4
IMPLEMENTED_WITH_VALIDATION_DEBT; J3 PARTIAL (metadata and generic routing
exist, authored production request coverage and final schedule do not); J5
PARTIAL (first visit and recall only). J6/J7 are PARTIAL at the functional
presentation level, with art/audio direction HUMAN_SIGNOFF_REQUIRED. J8/J9
and the final release candidate are NOT_STARTED as full four-night acceptance
phases. No checked-in J3A implementation record was found; this audit does
not infer completion from the expected starting-point prompt.

This bounded change removes the method lock from authored Recommendation
orders. Brew now uses the existing method selection; OrderManager projects
the authored request into GuestRequestContext independently of the selected
method template. Customer hint/reaction tags retain authored intent even if
the generic mood generator has run. Exact orders, discovery gating, recipe
recognition, FIT, supply and persistence authorities are unchanged. No scene,
prefab or data asset serialization changed in this routing phase.

Validation: baseline and changed Editor C# builds pass with zero errors and
one existing MSB3277 System.Threading.Tasks.Extensions conflict warning.
The J1 validator now checks authored-request selection and separation from
generated mood while retaining all exact/generic regression assertions.
Unity 6000.3.7f1 batch J4 execution exited before validators ran: missing
`com.unity.editor.headless` entitlement / access token, plus Package Manager
startup failure from a missing environment path. Classification:
VALIDATION_BLOCKED_BY_ENVIRONMENT. Log: local ignored
`Logs/production-autopilot-j4.log`. No new Editor or Play Mode PASS is claimed.

Remaining dependencies: four supported request types and twelve phrasings,
eight FIT responses, normal-play multi-answer scheduling, and final 5/4/4/4
counts. Dry Martini's current experiment teaches no amounts or preparation;
its reference design intentionally hides those facts, so production fairness
needs an explicit reconciliation rather than silent validator relaxation.
J5 closure meaning and failed-service semantics require the owner's decision
under AGENTS.md section 6 and J5 acceptance.


## J3 Production Content and Validation Recovery (2026-09-28)

J3 content and routing are IMPLEMENTED_WITH_VALIDATION_DEBT. The production
spawner now uses twelve authored OrderData/DialogueData pairs: three
situational phrasings each for Warmth, Bitterness, Clarity and Awakening.
The existing reaction database now contains eight FIT lines, two for each
FIT/execution category. These requests have no exact target, discovery grant,
ranked answer, or new UI. Their prose remains subject to human tone review.

The spawner reads the existing knowledge and ordinary supply authorities and
uses the unchanged RequestEvaluation to ensure an offered request has a
Known, supplied answer. It does not unlock recipes or inject supply. Existing
Prince and required discovery slots retain priority. Known-practice skips
now receive a reachable authored Recommendation rather than unconstrained
generic emotion combinations. No stable IDs, save keys or formats changed.

The final customer counts are explicitly serialized as 5/4/4/4; debug Space
spawning is disabled. Fresh routing is:

| Night | Slots |
|---|---|
| 1 | Existing three Dry Martini clues, experiment, Clarity request |
| 2 | Prince/Manwol, Negroni practice, Warmth, Bitterness |
| 3 | Prince recall, Manhattan practice, Warmth, Bitterness |
| 4 | Gin Fizz practice, Clarity, Awakening, reachable Recommendation pending J5 closure |

Day-3 Warmth has Seolya/Manhattan; Day-3 Bitterness has Negroni/Manhattan
across Build/Stir. Day-4 Clarity has Manwol/Gin Fizz across Float/Shake even
if the optional Day-1 Dry Martini attempt did not succeed. Learned Dry
Martini adds another Clarity interpretation. The fourth closing-night slot
is not represented as completed J5 narrative content.

### Validation findings and corrections

A licensed local execution path became available during this phase. This
supersedes the environmental restriction for the checks actually rerun below;
it does not rewrite the earlier failed-launch evidence. The full regression
run exposed a pre-existing J4 Negroni fixture error: it poured Gin, Sweet Red
Vermouth, Campari, while the authoritative recipe requires Gin, Campari,
Sweet Red Vermouth. The teaching line listed the same misleading order.
The line now teaches the canonical sequence and 30 ml per liquid. The
fixture follows that unchanged sequence; a swapped-order negative assertion
was added. The evaluator, recipe metadata and required garnish assertions
were not weakened.

The I13 Play Mode harness still advanced only the historical four narrative
lines. It now consumes exactly the six authored lines (four narrative plus
two J4 teaching), retains its choice/patience/Float assertions, and snapshots
recipe knowledge/legacy progress that the new teaching can modify.

Executed evidence:

- C# Editor build: zero errors, eight warnings after Unity project regeneration:
  one package reference conflict and seven existing J4 object-reference
  comparison warnings (CS0252). No warning is treated as runtime evidence.
- Unity 6000.3.7f1 `SURLHWAJ4DiscoveryValidation.RunProductionRegression`:
  I10, I11, I11B, I11C, I12, I13, I14, I15, I16, J1, J2/J2.1, J4 and J3 PASS
  within their static/edit-mode integration scopes. Local ignored log:
  `Logs/production-autopilot-regression-fixed.log`.
- I14 production UI Play Mode PASS: actual BarScene customer reaction, result
  toast and discovery popup sequencing, including good-fit and poor-fit cases.
  Log: `Logs/production-autopilot-i14-playmode.log`.
- I13 production UI Play Mode PASS: consumed teaching/narrative dialogue,
  patience pause, visible A/B, persisted A, Manwol Float entry and A recall.
  B/None mappings also passed the static regression; this run does not claim
  full runtime playthroughs of both branches. Log:
  `Logs/production-autopilot-i13-playmode.log`.
- Twelve new order GUIDs are unique; their scripts and dialogue references
  resolve. Unity imports the new assets and loads the production scene.
- Scene diff is limited to twelve request references, one supply reference,
  explicit counts/final night and the debug toggle. No prefab changes.

J2.1/J4 Editor-execution debt is cleared. J3's normal-input multi-answer
acceptance and the full four-night UI/save/endpoint run remain VALIDATION_DEBT;
edit-mode Known-state fixtures are not claimed as normal-play discovery.
No subjective dialogue, bartending-feel, pacing or final-readability PASS
is claimed.

### Remaining owner decisions / next dependency

Technical production closure is NOT declared. J5 still needs a chosen
closing meaning and failed-service/retry policy for A, B and legacy/None.
`04_CONTENT_ROADMAP.md` supplies a long-term departure direction but explicitly
does not define a bounded shipping endpoint. The owner was asked for the
J5 direction and failure policy under AGENTS.md section 6. Day-1 Dry Martini
also needs reconciliation between the deliberately amount-hidden I11C
reference contract and production learning without outside recipe knowledge.
No hidden amount grant or automatic Known transition was introduced.

After those decisions: complete the corresponding bounded content, run a
normal-input four-night slice including both Day-3 alternatives and Day-4
Float/Shake alternatives, and check failed/reloaded practice, New Game,
Continue and Demo Complete without Day 5. J6/J7 presentation/audio selection
and integration, J8/J9 full acceptance and the final release audit remain open.
Reserve cocktails, other literary ACTs and generalized progression systems
remain OUT_OF_SCOPE for this bounded demo.


## Dry Martini Teaching Contract — Owner Resolution (2026-09-28)

The owner approved teaching quantities and preparation before the Day-1
experiment. The first three clues retain their original fact boundaries.
The fourth encounter now teaches Gin 60 ml then Dry Vermouth 10 ml in the
Mixing Glass, ice, Stir, Strain, and optional lemon twist/olive garnish.
Typed facts are granted only as each of the two lines is consumed. The Guide
retains them as unnamed Partial knowledge; dialogue does not grant Known.
The existing ordinary failure/departure behavior remains; mandatory retry
was not part of this approval. Stable recipe/ingredient identities and
preparation truth are unchanged.

The focused I11C validator retains early-clue non-leak assertions and now
checks exact final teaching facts, unnamed Guide projection, persistence
after failure, and match-only Known transition. Historical hidden-amount
findings above remain historical.

Validation: C# Editor build passed with zero errors and the same eight
existing warnings. Unity 6000.3.7f1 production regression passed I10-I16,
J1, J2/J2.1, J4 and J3 static/edit-mode integration checks after the I16
final-attempt assertion was updated to the owner-approved teaching contract.
Early-clue non-leak and no automatic Known assertions remain intact.
Log: `Logs/production-dry-teaching-regression-fixed.log`.
Serialized diff contains only the two teaching lines and their four typed
facts; no scene or prefab changes. `git diff --check` passed.
Normal-input Day-1 teaching/readability remains VALIDATION_DEBT and
HUMAN_SIGNOFF_REQUIRED respectively; this run does not claim Play Mode PASS.


## J5 Owner Direction — Three-Visit Farewell (2026-09-28)

The owner resolved the closing meaning and failure policy:

- The third Little Prince visit is the bounded-demo final visit.
- Correct service in all three visits leads to the open farewell.
- An incorrect cocktail in any visit leads to the quiet farewell. A later
  successful service does not erase an earlier incorrect service.
- Failed service is accepted as part of the story and resolves the visit;
  the Prince must not be held in a forced-success retry loop.
- The first and second requests need not both require Manwol. Cocktail
  suitability should follow the authored dialogue at each visit.
- The open farewell should support a later Prince-related statue reward.
  The owner described that reward as future work; no statue asset or
  immediate reward presentation is selected in this record.

These are approved directions, not a claim of implemented J5 behavior.
The current first-visit mandatory Manwol discovery gate must be reconciled
with accepted narrative failure while preserving fair Manwol learning
through the existing Recipe Knowledge architecture. Follow-up requests
must retain real knowledge/supply/method reachability. Existing A/B choice
recollection and legacy save compatibility remain required.

One outcome detail was submitted for owner clarification: whether a
request-fitting cocktail counts despite poor execution, or whether all
three visits also require successful technique/preparation. FIT and
execution currently have separate authorities. This choice affects the
story outcome, so no combined success threshold has been invented.
J5 implementation remains PARTIAL pending this criterion; normal-play
validation and subjective farewell review remain outstanding.


## J5 Implementation — Strict Three-Visit Service History (2026-09-28)

The owner resolved the pending criterion: a fitting cocktail with failed
bartending does NOT count. The implementation requires recognized cocktail,
satisfied FIT, successful technique, and satisfied authored preparation in
all three visits. One failed service locks the quiet farewell, including
when the scene is reloaded before departure. The first/second/third visits
resolve after failure rather than demanding a retry.

First-visit Manwol teaching and A/B choice are preserved. The Day-3 return
asks for Warmth through branch-aware situational dialogue; the Day-4 final
slot asks for Clarity. Later visits have no hidden exact target and retain
the existing method picker. Seolya answers the return before Manhattan
practice; Manwol/Gin Fizz and learned Dry Martini answer the final visit.
Prince visits have no patience countdown. If first-visit Manwol was not
learned, Day-2 slot 3 provides separately authored Manwol recovery practice,
using the unchanged knowledge owner and required-discovery retry gate.
That learning never overwrites the failed Prince service. Counts stay
5 / 4 / 4 / 4.

Recommendation preparation is evaluated only after actual recipe recognition,
against the recognized recipe and actual trace through the existing evaluator.
No expected recipe is injected into method routing, and FIT and technique
remain independent. Generic Shake already supports container-pour on P;
its post-rhythm hint now exposes this action alongside Strain so Gin Fizz
preparation is visible without an exact request.

Outcome keys and closing-completed state are additive in
LittlePrinceVisitPersistence. Existing visit keys, choice key, IDs and
recipe relationships are unchanged. Legacy/malformed missing outcomes are
Unknown and cannot create an open farewell. New Game clears new state;
Continue preserves it. Future statue eligibility is derived from completed
open closure; no statue art, immediate grant or collection framework is added.

Open and quiet farewell assets share the existing dialogue panel and Next
button, with distinct A/B/None recall. The final guest stays until all lines
are read, after transient result/discovery feedback. Departure remains the
visit-completion authority and then reaches the independent Demo Complete
endpoint. Scene changes are exactly two order references; no prefab edits.

Validation evidence:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: zero errors,
  the same eight pre-existing warnings.
- Unity 6000.3.7f1 production static/edit-mode regression I10-I16, J1,
  J2/J2.1, J4, J3 and J5 PASS. Log:
  `Logs/production-j5-regression-final.log`.
- J5 validates all eight success/failure histories across A/B/None, strict
  FIT/technique/preparation boundaries, unknown/malformed/legacy data,
  failure retention, departure-only statue eligibility, reset, closing
  scheduling, and authored-reference coverage. J4 retains retry-safe
  discovery assertions on independent Manwol recovery and checks pending
  recovery/skip-after-Known scheduling.
- J5 Play Mode PASS through actual BarScene OrderManager completion,
  CustomerController, the existing Next/Brew controls, feedback coroutines
  and CustomerSpawner departure. Cases: all successful; technique failure
  independently in visits 1, 2 and 3; final wrong preparation; final wrong
  FIT. Manwol, Dry Martini and Gin Fizz are recognized final alternatives;
  newly learned Dry Martini/Gin Fizz popups finish before farewell reading.
  Brew/cancel cannot bypass reading. Final failed service still completes
  the demo on Day 4; New Game clears the added state. Log:
  `Logs/production-j5-playmode-final.log`. This probe injects controlled
  completed bartending results and does NOT claim normal-input discovery,
  manual bartending or full four-night acceptance.
- I14 production UI Play Mode regression PASS, preserving reaction → toast
  → one discovery popup and poor-FIT feedback. Log:
  `Logs/production-j5-i14-playmode.log`.
- I13 production UI Play Mode regression PASS: first-visit teaching, A/B
  buttons, saved A, Manwol Float entry and later recall remain intact.
  Log: `Logs/production-j5-i13-playmode.log`.
- Six new serialized assets have unique GUIDs and resolving references.
  Only two BarScene references changed; no prefab changes or stable-ID
  migration. `git diff --check` passes. Generated editor scene-history
  changes are excluded.

Status: J5 is IMPLEMENTED_WITH_VALIDATION_DEBT for normal-input playthrough.
Its focused service/reading/closure contract is objectively validated.
HUMAN_SIGNOFF_REQUIRED: authored dialogue tone, farewell pacing, emotional
meaning in play and UI readability. Future statue art/placement/presentation
is DEFERRED_PRESENTATION; eligibility alone is not a presented reward.

Remaining dependency audit: selected IBA cocktail sprites are still null,
ingredient icons are absent, the Prince still uses placeholder character
presentation, and no project-owned audio clips exist. J6/J7 remain PARTIAL
or NOT_STARTED in those respective areas; playback/volume integration is
not implemented and is not claimed complete. The owner was asked whether
to prepare cocktail-art drafts from the existing Seolya/Manwol direction
or leave art/audio sourcing to the owner. No arbitrary assets were added.
J3/J8/J9 normal-input four-night discovery/choice/save/reload and packaged
build acceptance remain VALIDATION_DEBT. Broader ACTs, reserve cocktail
production and generalized collection systems remain OUT_OF_SCOPE.
Technical production closure is not yet declared.


## J6 Cocktail Illustration Draft Integration (2026-09-28)

The owner approved generating and inserting drafts for the other cocktails using
existing Seolya/Manwol art as the style reference. This covers all seven previously
unassigned cocktails: Dry Martini, Negroni, Manhattan, Gin Fizz, Hanky Panky,
French 75 and Last Word. The latter three receive reserve presentation assets
only; selected production membership, supply, requests and discovery are unchanged.
The four existing cocktail illustrations remain intact. All eleven existing
CocktailData assets now resolve an illustration.

Seven original generated PNGs and single-sprite import metadata are added under
`Assets/_SURLHWA/Art/Cocktails/`, with exactly one `cocktailSprite` reference change
per corresponding CocktailData. CocktailData remains the art authority; no stable
identity, recipe, persistence, scene, prefab or runtime UI logic changes occur.
The Codex card/detail still hides artwork for Unknown/Partial knowledge, and the
unlock popup still reads the recognized recipe's target CocktailData. That gate
was inspected in source; no new normal-play UI observation is claimed here.

The historical J2 null-art assertion represented the previous missing-art state.
It is superseded by owner-authorized draft assignment checks: each of seven
CocktailData assets must resolve its own sprite, PNG data must decode with genuine
alpha and transparent corners, and import settings must retain single transparent
UI sprites without mipmaps. All eleven cocktail references are checked. Existing
identity, preparation, supply, reserve-exclusion and compatibility assertions remain.

Validation:

- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: PASS, zero errors,
  the same eight pre-existing warnings.
- Unity 6000.3.7f1 `SURLHWAJ2SelectedCocktailDataValidation.Run`: PASS, including
  the seven PNG alpha/import/reference checks and existing J2/J2.1 data checks.
  Local ignored log: `Logs/production-cocktail-art.log`; successful batch exit 0.
- Inspected all seven generated images for whole-glass framing, intended vessel,
  liquid and garnish, and absence of unwanted labels/background decoration.
  This is a draft asset inspection, not owner approval of final art direction.
- Serialized asset diffs are limited to seven sprite references; no scene/prefab
  changes. `git diff --check` passes. New GUIDs apply only to new sprite assets.

Exact prompts, tool provenance, asset paths and review notes are recorded in
`Docs/COCKTAIL_ART_DRAFTS.md`. Generated assets were copied without pixel editing.

Status: cocktail art integration PASS; final style consistency, small-card
readability and actual Codex/popup composition are HUMAN_SIGNOFF_REQUIRED.
J6 as a whole remains PARTIAL: ingredient and character presentation are still
open. Audio selection/integration and full normal-input four-night/build acceptance
remain outstanding as previously recorded. No final game-readiness or technical
production-closure claim is made by this art-only phase.


## J8 Integrated Four-Night Runtime Evidence (2026-09-28)

Resumed from `88ad960` after the owner's instruction to continue. J1-J4/I10-I16
baseline regression passed in licensed Unity (`Logs/production-resume-baseline.log`).
A source audit found that EndDay advanced currentDay before reading the completed
night's target, so the first-night result could show 5/4. BusinessDayManager now
captures that target before advancing; no schedule or save identity changes.

`SURLHWAFourNightPlayModeValidation` executes two fresh complete production runs:
entry New Game, automatic spawn/seat/arrival, consumed dialogue and A/B choice,
Brew/method controls, actual additive bartending scene, ingredient selection and
transfer, technique input, preparation/garnish, normal recognition/discovery,
feedback/natural departure, 5/4/4/4 results and night reloads. No MiniGameResult,
preparation trace, Known flag, grade, visit outcome or day outcome is injected.
The first three clue guests receive the already Known Seolya; the fourth teaches
and discovers Dry Martini. Early clue requests can therefore receive unsatisfied
FIT without preventing the learning arc; this is not claimed as successful
service of a recipe the player has not learned.

The second run fails the Prince's first Float technique, resolves that visit,
fails required Negroni garnish then retries the same guest, and learns Manwol
through independent recovery practice. Day-3 Warmth and Bitterness use alternative
Seolya/Manhattan and Negroni/Manhattan choices across runs. Day-4 Clarity uses
Manwol/Gin Fizz and the closing visit also exercises learned Dry Martini. All
three successful Prince visits yield open closure; first failure remains quiet
through recovery and Continue. Both runs reach Demo Complete and completed-save
Continue without Day 5. New Game between runs resets actual gameplay knowledge.
The test snapshots/restores existing player progress using SessionState.

Validation: Unity 6000.3.7f1 Play Mode PASS in `Logs/production-four-night.log`;
34 normal departures plus one same-guest practice retry. Runtime assertions verify
night result counts, persisted knowledge/choice/history, learning prerequisites,
ordinary supply on the live shelf and all four method routes. C# Editor build
passes; serialized scenes/prefabs are unchanged. UserSettings scene-history churn
is excluded. This is deterministic production-input-seam simulation: mouse angles,
pour increments, Float durations and Shake key judgements are automated through
the existing input paths, not OS-level mouse/keyboard events. Physical control
feel, raycast hit-target usability, pacing and full visual review remain
HUMAN_SIGNOFF_REQUIRED / their unobserved runtime cases VALIDATION_DEBT.

A separate source finding remains for the next bounded UI/audio phase: Shake's
unscaled rhythm clock advances while the Guide's read-state pauses input. The
pause must preserve remaining note times; this is an objective fix, not polish.
J6/J7 presentation work and standalone delivery are still open. The owner has
now authorized temporary winter-bar audio and character drafts as well as their
technical integration; final aesthetic approval remains human-owned.


## J6 Character Draft Integration (2026-09-28)

Owner-approved temporary winter-bar character direction is now connected through
three existing prefabs: Little Prince, Customer_A and Customer_B. Original PNGs
were generated with built-in imagegen using Seolya texture/light as reference;
exact prompts and provenance are in `Docs/CHARACTER_ART_DRAFTS.md`. New sprite
imports preserve the existing two-unit world canvas and feet baseline. Each
prefab's SpriteRenderer and standing/seated fields use its own draft; the Prince's
obsolete red placeholder tint is reset to white. Customer_B's existing interaction
anchor is now assigned. The scene's existing generic-variant array now references
A and B; existing anti-repeat logic is reused, with no new spawn system.

Unity `SURLHWAPresentationValidation.RunCharacters` PASS verifies three distinct
resolving sprites, prefab presentation fields, bounded world scale, interaction
anchors and PNG alpha. Log: `Logs/production-character-drafts.log`, exit 0.
C# build passes with the same eight existing warnings. Prefab diffs were inspected:
three sprite references each, one Prince tint, one B anchor; scene diff is only
the two-entry existing variant array. No transforms, IDs, recipe facts or narrative
outcomes change. `git diff --check` passes.

Final character identity, tone, placement and composition remain
HUMAN_SIGNOFF_REQUIRED. Seated poses currently reuse standing drafts; dedicated
pose/animation refinement is DEFERRED_PRESENTATION, not an animation-complete
claim. Bar dressing and ingredient icon refinement remain separately reviewable;
existing IngredientData labels continue to identify supplied ingredients.


## J7 Temporary Audio and Settings Integration (2026-09-28)

Seven owner-authorized original synthesized audio drafts are generated reproducibly
by `Tools/generate_demo_audio.mjs`. No external sound source is introduced.
`Docs/AUDIO_DRAFTS.md` records provenance, roles, defaults and review limits.
The scene-owned DemoAudioController is serialized on the existing GameManager
host, with explicit spawner and coordinator references. The coordinator passes
its loaded runner for read-only observation of accepted preparation/volume.
Music, wind, pouring, ice, glass, entrance/departure and UI sound are integrated.
No recipe, matching, FIT, score or knowledge responsibility moves.

The existing settings button, entry sound button and Esc expose master/music/
effects sliders and mute. Additive preference keys survive gameplay New Game.
Settings owns a bounded read pause, restores previous timing, and does not stack
over an open Guide. Technique input is blocked while it is open. Shake now shifts
its unscaled rhythm origin throughout Guide/settings pauses, fixing the objective
case where notes expired while the player read.

Unity `SURLHWAAudioPlayModeValidation.Run` PASS in
`Logs/production-audio-playmode.log`: seven imported clips, exactly four owned
sources, actual slider/mute controls, silence at zero/mute, preference preservation
through reset, explicit additive runner handoff, continuous accepted-pour audio
start/stop, two 1.1-second reading pauses without missed notes, resumed eight-note
Shake and clean unload. This is live source/control/timing evidence, not an auditory
mix approval. Batch ScreenCapture did not produce a review PNG; no screenshot or
visual-readability PASS is inferred. C# build passes; prior full-route evidence is
retained. Scene changes are one component, seven clips, one existing settings
button reference and two explicit same-scene audio references; no prefab edits.

J7 playback/settings technical prerequisites are implemented. Temporary sound
quality, loudness, loop feel and atmosphere remain HUMAN_SIGNOFF_REQUIRED. No
final soundtrack, bar dressing, or broad presentation approval is claimed.


## J6 Korean UI, Reading Layout and Font Closure (2026-09-28)

Scope: player-facing menu/result/preparation instructions, ml-first known ingredient quantities (legacy ounces retained as secondary display), and readable Guide/Codex detail. Recipe IDs, amounts, conversion constant, grade strings, knowledge gates and save format are unchanged. English ingredient identities and established method labels remain authored data.

Rendered evidence exposed objective defects that compilation did not: dynamically created Korean labels rendered as missing-glyph boxes; preparation hints were duplicated over each other; the Recipe button covered the shelf heading; the Guide allowed technique text to draw over it; long authored recipe detail had no scrolling. These were fixed with a project-owned dynamic fallback from the existing Noto Sans KR font, narrow four-prefab text/anchor/scaler changes, a single hint surface, foreground opaque Guide, and a Unity ScrollRect around the existing detail text. The formatter remains the sole knowledge-filtered projection. Settings now suppress garnish keyboard input as well as technique input. Font source licenses are included under Docs/Licenses.

Validation: dotnet Editor build: 0 errors, 8 pre-existing warnings. Full J1–J5 regression rerun recorded in Logs/production-final-regression.log. Focused graphics-enabled Audio PlayMode in Logs/production-ui-render.log passed clips, volume/mute/reset isolation, actual pour playback, both reading pauses, rhythm resume/unload, and the long-recipe scroll reference. The latter uses an explicitly Known Gin Fizz presentation fixture and restores prior saves; it is not discovery evidence. RenderTexture review images under Logs/Review cover title, sound settings, Shake preparation, Guide top/bottom, and garnish. Captures temporarily project real overlay canvases through a camera and restore them; they are offscreen review evidence, not OS-input or final display approval.

Logs/production-final-four-night.log separately passed two complete production runs after character/audio integration: 34 departures plus one mandatory retry, strict open/quiet Prince branches, all methods, actual liquid/preparation/technique/matching, knowledge/supply-reachable alternative answers, night-2 reload/Continue, completed Continue without Day 5, and New Game reset. No grade/result/knowledge injection was used in those runs; actual input processing seams and UI callbacks were driven. Physical mouse/keyboard, subjective control feel and final readability remain human checks.

Remaining: bar dialogue/default labels and environment presentation are the next bounded correction, followed by standalone build/delivery audit. Source-visible draft composition/animation and final Korean editorial preference remain HUMAN_SIGNOFF_REQUIRED, not automatic visual PASS.


## J6/J7 Bar Draft and Interaction Readability (2026-09-28)

Owner-authorized quiet winter-bar direction is now represented by one original generated background draft, with exact prompt/provenance in Docs/ENVIRONMENT_ART_DRAFT.md. It replaces the existing background renderer's image; the eight former interior/statue placeholder renderers are hidden. No route, seat, collider, gameplay object, persistent ID or statue reward was removed or introduced.

Bar dialogue now has an opaque bounded reading area, separate next/choice/brew controls, actual Korean button labels and a neutral guest heading. Unused default New Text strings and decorative 100×100 button children no longer appear over the selected flow. The main canvas scales from a 1280×720 reference. These are focused changes to existing serialized presentation, not a scene or dialogue architecture replacement.

A graphics review also exposed the legacy Stir spoon center at +470 canvas units and oversized cup/spoon sprites. After the supported-window scaler correction this could put the intended mouse orbit outside the screen. The anchor was brought inside the visible cup area and the existing sprites scaled to leave instructions/buttons clear. Technique thresholds and scoring were not changed. A focused runtime assertion checks a 100-pixel vertical cursor orbit stays inside the actual canvas; subjective stirring feel is still human-owned.

Validation: dotnet build has 0 errors and the 8 existing warnings. Logs/production-bar-review.log is the short production-entry review route with save snapshot/restore: real New Game, first arrival/dialogue, actual Stir entry/Guide/serve/departure, plus rendered captures. It explicitly does not report the shortened run as a full four-night PASS; the complete two-route evidence is Logs/production-final-four-night.log. Scene/prefab textual diffs were inspected: intended UI rect/text/colors, one backdrop image/scale, eight renderer enable flags, and the Stir visual/anchor corrections only. Final art, character scale/seated pose and winter-bar emotional atmosphere remain HUMAN_SIGNOFF_REQUIRED. Additional dressing, dedicated seated animation, glass/garnish illustration refinement and future Prince statue display are DEFERRED_PRESENTATION.


## J9 Audio Listener Continuity Correction (2026-09-28)

Final log/source audit identified a real transition gap: each method prefab already owns its listener, but MiniGameManager destroys that prefab immediately after serving; the coordinator formerly waited for asynchronous scene unload before restoring the Bar listener. Source.isPlaying alone had not proved a listening endpoint throughout that interval. No duplicate listener was added to BartendingScene.

The existing coordinator now disables the finishing prefab's listener and restores the captured Bar listener synchronously in its accepted-result callback. Camera restoration, result finalization, cleanup/failure handling and gameplay ownership still wait on the existing lifecycle. Audio playback stays owned by DemoAudioController.

Validation: 0 compile errors; Logs/production-audio-listener.log passes exactly one active listener before/during/after preparation and on every observed asynchronous cleanup frame, together with the previous volume/mute, reset isolation, actual pour playback, Guide/settings pause, Shake input resume and unload checks. The prior missing-listener warnings are absent. This is output-path continuity evidence, not approval of the audible mix or device-specific playback quality.


## J FINAL — Technical Closure and Windows Review Delivery (2026-09-28)

This entry supersedes earlier implementation-status summaries, not historical findings. Repository evidence was checked against the project vision, canonical gameplay/bartending contracts, current sources, authored data, scenes/prefabs and executed validators. Owner decisions remain authoritative: all three Prince services must succeed for open farewell; any wrong drink, failed technique or unsatisfied required preparation leaves quiet farewell. Later visits interpret their authored requests rather than repeating a fixed Manwol password. Statue presentation is for later; only eligibility is persisted here. Temporary quiet winter-bar character, environment and audio drafts were expressly authorized, with final aesthetic approval retained by the owner.

### Reconstructed phase state

| Phase | Current state | Evidence / remaining boundary |
|---|---|---|
| J1 coherent demo flow | COMPLETE | Real 5 / 4 / 4 / 4 flow, entry/reset/Continue, correct night totals and terminal Demo Complete. |
| J2 / J2.1 selected data | COMPLETE | Six selected cocktails, exact ingredient identities, technical preparation and staged supply; reserve compatibility preserved. |
| J3A / J3 Recommendation | COMPLETE | Authored requests/FIT responses, all-method access and learned/supplied alternatives exercised across complete runs. |
| J4 discovery | COMPLETE | Authored quantities/preparation precede required attempts; actual matching learns recipes; required failures retry safely. |
| J5 Prince | COMPLETE | All eight service histories validated; full open/quiet runtime routes, sticky failure, choice recall and future statue eligibility. |
| J6 presentation integration | IMPLEMENTED_WITH_VALIDATION_DEBT | Cocktail/character/backdrop drafts, Korean font, ml-first quantities, scrollable knowledge projection and focused scene/prefab fixes; physical display/input checks remain. Final aesthetic judgment is HUMAN_SIGNOFF_REQUIRED. |
| J7 audio/environment | HUMAN_SIGNOFF_REQUIRED | Objective playback, settings, read-pause and listener continuity pass. Owner must approve audible mix and winter-bar mood. |
| J8 integrated slice | IMPLEMENTED_WITH_VALIDATION_DEBT | Two complete production-input-seam runs; physical mouse/keyboard four-night run remains unobserved. |
| J9 release regression | IMPLEMENTED_WITH_VALIDATION_DEBT | Save/reset/legacy/invalid cases, modal timing, references, compilation and runtime regressions pass; target-device feel/performance checks remain. |
| J FINAL delivery | IMPLEMENTED_WITH_VALIDATION_DEBT | Windows build, bootstrap smoke checks, controls/limits, draft provenance and representative rendered images delivered. Public-release attribution and final owner acceptance are outstanding; portfolio video is deferred presentation. |

No remaining bounded objective implementation phase is NOT_STARTED. No known objective production blocker remains from this audit. This is technical closure with explicit validation debt, not an assertion that every roadmap acceptance criterion or subjective release approval is complete.

### Final repository-wide classification

Each item below has exactly one final classification; separate technical prerequisites and subjective judgments are separate items.

| Item | Classification | Finding |
|---|---|---|
| Four-night lifecycle, 17 encounter structure, results and endpoint | PASS | Two fresh complete 5/4/4/4 runs; completed Continue does not create Day 5. |
| Fair recipe discovery and knowledge | PASS | Actual clues, attempts and recognition; mandatory Negroni preparation failure retries the same guest; independent Manwol recovery protects later progression. No day/supply/catalog auto-Known shortcut. |
| Ingredient supply, method access and Recommendation reachability | PASS | Known and supplied alternatives on live shelves; Seolya/Manhattan, Negroni/Manhattan, Manwol/Gin Fizz choices and learned Dry Martini exercised through production routes. |
| Exact requests and FIT authority | PASS | Exact identity remains distinct from emotional suitability; route templates do not select the recognized drink or single emotional answer. |
| Prince visits and closure | PASS | Three successful services open farewell; any failed visit remains quiet through reload/recovery. Technique failure cannot be rescued by suitable FIT. Reading completion precedes departure. |
| Persistence, New Game, Continue, malformed/legacy compatibility | PASS | Existing gameplay identities/keys remain intact; additive outcomes/settings, reset isolation and night-boundary reload checks pass. |
| Bartending and selected cocktail data | PASS | Actual liquid/container/preparation/layer state feeds recognition; all four methods and six selected data contracts remain valid. |
| Architecture and serialized references | PASS | Ingredient, cocktail, recipe, catalog, knowledge, supply, recognition, FIT and runtime-state authorities remain separate. Both build scenes and dependent prefabs have no missing scripts. Intended scene/prefab diffs were reviewed in each bounded phase. |
| Korean text, recipe scroll, Guide/settings and audio integration | PASS | Fallback glyphs/rendered layouts inspected; real scroll controls, pause-safe Shake, actual playback/volume/mute and continuous single listener checked. |
| Compilation, automated regression and Windows bootstrap | PASS | Evidence below. No Unity license blockage prevented these runs. |
| Physical mouse/keyboard four-night standalone run, real-display click targets/resolutions, target-hardware performance | VALIDATION_DEBT | Input-seam automation and offscreen captures cannot certify these cases. Standalone checks cover startup only. |
| Final dialogue tone, emotional impact, control feel, pacing, art/pose/composition, readable visual hierarchy and audible mix | HUMAN_SIGNOFF_REQUIRED | Drafts are integrated and reviewable; no final subjective PASS is given. |
| Pre-existing artwork provenance/permissions and final public-release credits | HUMAN_SIGNOFF_REQUIRED | CREDITS.md states known sources and owner checks without inventing authorship or clearance. |
| Dedicated seated poses/animation, bespoke ingredient icons, refined glass/garnish art, further bar dressing/memory display, future Prince statue and portfolio video | DEFERRED_PRESENTATION | Functional labels and draft presentation exist; these are not needed to repair a known progression defect. |
| Later nights/chapters, expanded reserve production, broad quest/economy/collection systems, exact dialogue/liquid quicksave, other platforms/aspect ratios | OUT_OF_SCOPE | Bounded four-night scope and existing persistence model retained. |
| Optional Unity Pipeline runtime command/hot-reload server and existing non-blocking compiler warnings | OUT_OF_SCOPE | Build warning is from com.unity.pipeline, not URP failure. Runtime server remains disabled. Existing warning cleanup is separate from this bounded release. |

### Executed validation and artifact

- Final dotnet build Assembly-CSharp-Editor.csproj --no-restore: PASS, 0 errors, 8 existing warnings (one assembly-reference warning and seven existing J4 comparison warnings).
- Logs/production-windows-build.log: full I10-I16 and J1-J5 production regression PASS, selected cocktail/character references PASS, both enabled scenes and prefab dependency missing-script checks PASS; Unity batch exit 0.
- Logs/production-final-four-night.log: two complete production runs PASS, 34 natural departures plus one mandatory discovery retry; actual liquid, technique, preparation, recognition, discovery, reachable alternatives, choice/history, reload/Continue and no Day 5. Input seams/UI callbacks are automated; this is not OS-level input evidence.
- Logs/production-audio-listener.log: final focused playback/settings/scroll/pause/Shake/unload checks PASS, exactly one active listener throughout observed cleanup frames, no missing-listener warnings. Its Known Gin Fizz setup is a restored presentation fixture, not discovery proof.
- Logs/production-bar-review.log: current first-guest dialogue, Stir, Guide and departure review PASS, including cursor-orbit bounds. This shortened check does not replace the full-route evidence.
- Windows x64 build PASS at Builds/Windows/SURLHWA.exe, Unity 6000.3.7f1, reported player size 227590366 bytes. Runtime source corresponds to 6d86c5f; the following delivery commit adds only Editor build tooling and documentation.
- One build warning: optional Unity Pipeline RuntimePipelineConfig is absent, so its command/hot-reload server is disabled in Player. Inspected local package source confirms this is unrelated to the rendering pipeline; no runtime server was enabled to silence it.
- Logs/production-player-smoke.log and Logs/production-player-graphics-smoke.log: separate 12-second headless and Direct3D 11 standalone startup checks. Engine, scene managers and input initialize; no exception/error appears in either player log. D3D11 uses the local RTX 3070. Only each verified test process was stopped; no New Game or gameplay input was sent. This does not prove a complete standalone playthrough or graceful user-exit behavior.
- Build folder includes controls, limitations, credits/font notices and six representative offscreen review images. Exact generation prompts remain in the draft-art/audio records. Keep the entire player folder together.
- Build-generated shader caches, global runtime cache lists and local build-setting churn were inspected and restored; generated font whitespace was normalized. No unrelated ProjectSettings/UserSettings change is included. Build products, logs and local Editor files remain ignored.

### Owner review handoff

Use Docs/RELEASE_REVIEW.md for the physical playtest. Prioritize one fresh complete run, required-discovery failure/retry, alternate Recommendation drinks/methods, one failed Prince service versus all-three success, reading/settings pauses, night Continue/completed Continue/reset, then 1280x720 and 1920x1080 readability and audible mix. The existing untracked .codex/ and root AGENTS.md are preserved outside this work's commits. No push or publication is performed. Final finished-game/public-release approval remains with the owner.

## C1 — Full Campaign Infrastructure (2026-09-28)

The owner explicitly expanded scope beyond the completed four-night review demo. See Docs/16_FIRST_PASS_CAMPAIGN.md for the repository audit, bounded 18-night plan and authority decisions. Optional schedule data, departure checkpoints, post-service reading and ending-read infrastructure are implemented without activating unfinished content. Existing save IDs/formats remain intact. dotnet compile and Unity campaign persistence validation PASS; runtime full-route proof remains pending. No scene/prefab changes in C1. Historical demo closure above remains valid for its bounded scope.

## C2 — Chunhyang / Mongryong Main ACT (2026-09-28)

Five authored visits cover opposite meeting perspectives, departure, silent humiliation and shared-glass reunion. Requests remain target-free Recommendations; both service responses continue to the same authored story beat. All text is readable through the existing Next flow. dotnet compile and Unity authored-ACT asset/reference validation PASS (5 unique visits). No scene/prefab changes. Pair staging, swing memory and campaign normal-play proof depend on the planned presentation/integration phases; emotional interpretation and pacing require owner playtesting. No secret revisit implemented.

## C3 — Don Quixote Main ACT (2026-09-28)

Declaration and remembered ridicule lead to clearer conviction and a solitary departure; the ideal is not played as a drunken joke. Authored requests, pre-service dialogue, separate successful/weak-service responses and read-before-departure lines are implemented. dotnet compile and Unity authored-ACT validation PASS (7 cumulative unique visits). No scene/prefab changes or future revisits. Normal-play, memory and localized presentation proof remain dependent on campaign integration; subjective tone/pacing remain HUMAN_SIGNOFF_REQUIRED.

## C4 — Hamlet Main ACT (2026-09-28)

Audited the existing uncommitted two-visit draft against the full roadmap. Question, lifting/lowering the glass and unresolved departure preserve inaction; successful service does not cure it. Both visits request interpretation and carry a local stillness presentation flag. No TimeScale, input or global audio changes. Compile PASS (0 errors), Unity authored-ACT validation PASS (9 visits), Logs/campaign-hamlet.log exit 0. Scene/prefab changes: none. Local staging, memories and normal-play proof remain integration dependencies; emotional/pacing quality is HUMAN_SIGNOFF_REQUIRED.

## C5 — Faust Main ACT (2026-09-28)

Two visits connect wanting knowledge, recalled signing and the cost that cannot simply be reclaimed. No salvation/condemnation branch is invented. Requests allow Clarity/Awakening and Bitterness/Warmth; service changes responses, not the person's ending. Compile PASS (0 errors), Unity authored-ACT validation PASS (11 visits), Logs/campaign-faust.log exit 0. No scene/prefab changes. Cracked-glass spatial memory and full-route proof remain integration dependencies. Tone/pacing: HUMAN_SIGNOFF_REQUIRED.

## C6 — Sim Cheong Main ACT (2026-09-28)

Three visits preserve the roadmap's talkative uncertainty, self-owned resolve before the boat and changed, calm return after meeting her father. Her decision is not a bartender success reward; fear and agency remain explicit without staging the original event. Warmth/Clarity/Bitterness requests retain alternatives. Compile PASS (0 errors), Unity authored-ACT validation PASS (14 visits), Logs/campaign-simcheong.log exit 0. No scene/prefab changes. Lotus memory and normal-play remain integration dependencies; emotional/pacing quality is HUMAN_SIGNOFF_REQUIRED. Father revisit excluded.

## C7 — Odysseus Main ACT (2026-09-28)

Two visits move from silence and changed return to staying after the glass is empty. The lotus connection recalls return without inventing a new plot between guests. No later revisit or completed-ship upgrade. Compile PASS (0 errors), Unity authored-ACT validation PASS (16 main visits), Logs/campaign-odysseus.log exit 0. No scene/prefab changes. Anchor/ship memory and full-route proof remain integration dependencies. The main ACT ends in staying; the later campaign coda must preserve that meaning. Tone/pacing: HUMAN_SIGNOFF_REQUIRED.


## C8 — Ordinary Guests and Three Spaced IBA Discoveries (2026-09-28)

Four ordinary voices recur across 14 extended-night blocks, reflecting work, waiting and the changing bar. Three teach the existing Hanky Panky, French 75 and Last Word recipes before required attempts, with retry-safe OrderData and full typed Partial facts. Supply opens at nights 6/10/15 through existing IngredientAvailabilityData; no knowledge auto-grant. Official recipe/source audit is recorded in Docs/15_IBA_COCKTAIL_CATALOG.md. Stable IDs, recipe amounts and catalog membership unchanged.

Compile PASS (0 errors). Logs/campaign-content.log PASS, exit 0: 16 unique main visits, checkpoint/reset, all three discovery fact sets and strict supply/shelf tiers (13/15/17/20/20/22/23/26 across nights 1/2/3/4/5/6/10/15). I10 assertions now test the explicit campaign tiers, including exclusion before each tier. No scene/prefab changes. Normal-play recognition, retry and Recommendation alternatives remain integration validation dependencies. Dialogue/pacing: HUMAN_SIGNOFF_REQUIRED.


## C9 — Local Guest Staging and Spatial Memory Drafts (2026-09-28)

One scene-bound presentation component derives seven small bar traces from existing completion records. The Prince leaves an empty chair after either departure; the small star still requires his canonical open-farewell eligibility. Other traces use the roadmap's swing, windmill, stage/skull, circle/cracked glass, half-submerged lotus and anchor/ship fragment. No collection UI, counters, rewards or new persisted state.

The reunited pair and departure use one CustomerController with a visual companion; reunion displays one served glass during departure reading. Hamlet uses only a muted local sprite and still pool of light. Global time, audio, input, other customers and preparation are untouched. Existing character silhouettes and restrained world-space contours are first-pass placeholders.

Compile PASS (0 errors); Unity content regression PASS, exit 0 in Logs/campaign-presentation-compile.log. Inspected scene diff: one component plus its explicit day-manager reference; no transform/prefab churn. New presentation is dormant without campaign data. Rendered placement/restoration proof remains an integration dependency; final art, stillness and emotional impact require human review.

## C10 — Campaign Schedule and Authored Ending (2026-09-28)

The scene now references the 18-night schedule: the canonical first 17 encounters, followed by 16 new special visits and 14 ordinary visits. Every added night includes an ordinary guest. The ending audit in Docs/16_FIRST_PASS_CAMPAIGN.md precedes authoring: after Odysseus stays and leaves, the bartender tends the room's accumulated traces and leaves a light. Eight readable lines and a persistent final coda replace the demo endpoint in this configured campaign. The optional standalone four-night endpoint remains available when no campaign is assigned.

The first full production run, Logs/campaign-full-playmode.log, completed all 47 natural departures, live discovery/retry, Recommendation alternatives, weak-service Hamlet continuation, representative Continue points and seven memory projections. It then failed entering the ending: activating the initially inactive serialized panel ran Awake after navigation setup and closed the panel. This run is NOT a full-campaign PASS. OpenCampaignEnding now activates under the existing opening guard before binding ending navigation; its reading area is sized for authored text. Logs/campaign-ending-fixture.log subsequently passed first activation, every line's measured fit, interrupted replay and completed Continue. The fixture seeds only the final checkpoint and is not full-route evidence.

Rendered review also exposed the reunion glass and Hamlet light behind the dialogue region. Their local positions were raised without changing global time, input, audio or scene transforms. Final screenshots and a fresh full-route run are required after these corrections. Compile PASS in Logs/campaign-ending-dotnet.log; earlier integrated static regression PASS in Logs/campaign-integrated-r2.log. Final runtime/build evidence is recorded below when executed.

### Corrected full-route result

Logs/campaign-full-quiet-playmode.log: PASS, fresh New Game through all 47 natural departures and the authored ending. This run uses Prince choice B and an actual failed Float service, followed by independent Manwol recovery and the canonical quiet farewell. It performs all four methods, all three added discoveries, a garnish failure/retry for Hanky Panky, learned/supplied Recommendation alternatives, weak-service Hamlet continuation, departure checkpoints and seven restored memory projections. It invokes production UI callbacks and bartending input seams; it never injects successful service/knowledge into the full route.

Continue is exercised after the second night, inside nights 6/10/15, during Quixote's dialogue, during ending reading and after the completed ending. The final Continue returns to the coda without night 19. New Game clears campaign/knowledge/memories. A separate restored legacy fixture then verifies completed-demo handoff to night 5 without invented Prince successes or Hanky Panky knowledge. Editor saves are restored by the runner. The earlier open-Prince run reached all 47 departures before the now-fixed panel defect; it is not relabeled as a full ending PASS.

Latest rendered reunion and Hamlet captures confirm the corrected glass/light positions. Ending and coda captures show seven spatial traces, readable lines and navigation. The focused ending fixture measures all eight lines against the text area. These are objective rendering checks at 1280×720, not aesthetic approval. Final C# compile: PASS, 0 errors and one existing assembly-reference warning in Logs/campaign-final-dotnet.log.

## C11 — Full Campaign Audit and Owner Review Handoff (2026-09-28)

The following classifications apply to the current campaign and supersede the earlier demo's scope exclusions for main ACTs, later nights, memories and the three promoted recipes. Historical observations above remain unchanged.

| Item | Classification | Current evidence or remaining work |
|---|---|---|
| New Game through ending, 18 nights / 47 encounters | PASS | Corrected fresh full route and ending navigation pass in C10. No debug skip or service-result injection in that route. |
| All seven main ACTs | PASS | Canonical Prince plus 16 authored later visits execute in order. Departure reading precedes completion; poor service preserves new ACT progression. Final narrative quality is a separate review item. |
| Ordinary guests and paced discovery | PASS | One ordinary encounter on every extended night; recurring voices and discovery on nights 6/10/15 are exercised. |
| Recipe Knowledge and fair retry | PASS | Actual clues precede attempts; HP/French 75/Last Word are learned through real preparation/recognition. Required failed HP discovery keeps the guest/slot and remains Unknown until success. Failed Prince service has independent Manwol recovery. |
| Supply and Recommendation reachability | PASS | Existing ingredient IDs, recipe truth and catalog retained. Strict tier assertions and live learned/supplied alternatives accompany actual chosen drinks and all four methods. No new ACT has an exact cocktail answer key. |
| New Game / Continue / branch history | PASS | Night and departure checkpoints, dialogue restart, knowledge retention, Prince's sticky failure, interrupted ending replay and full reset pass. Additive keys preserve existing identities/formats. |
| Environmental memories | PASS | Seven visible/restored traces derive from completed ACTs; Prince's star respects his existing eligibility. Corrected glass/light and final room were inspected in rendered captures. |
| Ending and post-ending Continue | PASS | Eight authored lines resolve the bartender's closing among remembered people. Completion persists, Continue shows coda, and no night 19 spawns. |
| Legacy four-night production behavior | PASS | Original 17 encounters and both Prince outcomes observed across full runs. Logs/campaign-legacy-closure.log passes six focused scenarios including all three failure positions, preparation/FIT negatives, reading, standalone demo completion and reset. The fixture explicitly removes the optional campaign; it does not claim physical-input proof. |
| Authority, IDs and serialized scope | PASS | Existing customer/dialogue/order/knowledge/supply/recognition/FIT/runtime owners retained. Two small scene changes across C9/C10 (one presentation component and one campaign reference); no prefab edits or unrelated scene churn. |
| C# compilation | PASS | Final build reports 0 errors; existing assembly-reference warning remains. |
| Integrated regression, Windows build and executable startup | PASS | C11 build evidence below; complete standalone physical play remains separate validation debt. |
| Complete standalone physical-input playthrough, hit targets at both review resolutions, target-device performance, mid-drink interruption | VALIDATION_DEBT | Automated input seams and rendered captures do not observe these physical cases. The owner checklist requests the complete executable playthrough. |
| Dialogue interpretation/tone, emotional impact, overall pacing, bartending feel, readability hierarchy, visual composition and audible mix | HUMAN_SIGNOFF_REQUIRED | All are first-pass review material. Owner playtesting determines revisions and readiness. |
| Public-release provenance/permission and final credits | HUMAN_SIGNOFF_REQUIRED | Existing artwork ownership checks remain as recorded in CREDITS.md. No public distribution was performed. |
| Dedicated special-guest art/poses/animation, refined spatial memories/shared glass, stronger Hamlet stillness, final audio/lighting/cinematics and video | DEFERRED_PRESENTATION | Functional draft presentation is integrated; no progression repair is hidden in this classification. |
| Pilot/rose/companion/secret revisits, repeated memory-click triggers, statue upgrade trees and further IBA collection growth | FUTURE_EXPANSION | Explicitly excluded from this main campaign pass. |
| New quest/economy/scoring frameworks, destructive save migration, exact dialogue/liquid quicksave, other platforms/aspect ratios and optional Pipeline runtime server | OUT_OF_SCOPE | Existing architecture and bounded delivery target retained. |

No known objective campaign implementation defect remains after the corrected full-route run. This is first-pass technical evidence, not final artistic or finished-game approval. Build execution and standalone startup evidence follow separately.

### Executed build and final delivery

- Logs/campaign-windows-build.log: PASS, Unity batch exit 0. Campaign content/persistence/supply checks, 18-night schedule, I10–I16 and J1–J5 regressions, character references, both enabled scenes and dependent prefab missing-script checks all pass.
- Windows x64 player: Builds/Windows/SURLHWA.exe, Unity 6000.3.7f1, reported size 227646462 bytes. Runtime source/assets correspond to `1ed60d3`. The following delivery commit changes only Editor build packaging and review documentation.
- One existing build warning: optional com.unity.pipeline RuntimePipelineConfig is absent, so its command/hot-reload server is disabled. This is unrelated to URP rendering; no runtime server was enabled to hide the warning.
- Logs/campaign-player-smoke.log: D3D11/RTX 3070, assemblies, input and scene controllers initialize without errors/exceptions during the brief standalone startup check. No gameplay input was sent. Only the verified test process was stopped. This does not prove a complete standalone playthrough or graceful user exit.
- The Windows folder contains current controls, scope, owner checklist, campaign contract, audit, credits/license notices and ten rendered review images, including reunion, Hamlet, ending and coda. The six opening/presentation images remain from the demo review; four campaign images are from the corrected full route. Keep the whole player folder together.
- Build-generated shader prefilters, runtime cache lists, temporary project settings and Editor-local changes were inspected and restored. Dynamic font differences were whitespace only and restored. Temporary performance-test resources were cleaned by the build lifecycle. No generated/local assets are committed. The owner's `.codex/config.toml` change remains untouched.

Owner next step: play the complete executable using Docs/RELEASE_REVIEW.md and record a second-pass list for dialogue, pacing, interpretation, controls, memory readability, art and audio. No push or public publication was performed. Only the owner decides final finished-game readiness.
