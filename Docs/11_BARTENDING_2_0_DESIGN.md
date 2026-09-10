# SURLHWA — Bartending 2.0 Design Specification

Last Updated: 2026-09-10

## I10 Ingredient Availability (Implemented Boundary)

I10 implements one bounded day-derived availability layer, not an inventory
system. `IngredientAvailabilityData` owns minimum-day production policy and
legacy/test always-available policy. `IngredientAvailabilityContext` contains a
business day plus optional request-local liquid ingredients. The normal shelf
is filtered from this evaluation; it is never filtered to only a chosen recipe.

Physical construction is a separate pure query over `RecipeCatalogData` liquid
requirements and available IngredientData references. It ignores whether a
recipe is unlocked, known, or visible in the Guide. Recipe garnish expectations
remain outside liquid requirements, and I8 proof garnish state remains available
rather than starting a second garnish-supply system.

Earliest physical availability: Dry Martini, Day 1; Negroni, Day 2; Manhattan
and Hanky Panky, Day 3; Gin Fizz and French 75, Day 4; Last Word, Day 5. Exact
Gin and London Dry Gin remain distinct identities. Normal availability is
reconstructed from `currentDay`; no ingredient save key exists. A special
request may get a temporary local supply override, while Manwol remains
protected by legacy availability.

Status: APPROVED GAMEPLAY DIRECTION / PHASE I IMPLEMENTATION REFERENCE

---

## 1. Purpose and Document Boundary

This document records the implementation-facing design direction for
Bartending 2.0. It follows the completed structural rebuild through Phase H and
the independent red-team review in Docs/10_SURLHWA_FULL_PROJECT_RED_TEAM_REVIEW.md.

It is intentionally more detailed than 01_GAME_DESIGN.md.

- 01_GAME_DESIGN.md owns player-facing principles and the high-level game loop.
- This document owns Bartending 2.0 interaction vocabulary, preparation
  concepts, container behavior, data-direction requirements, phase boundaries,
  and non-goals.
- 04_CONTENT_ROADMAP.md owns catalog and content-direction planning.
- 05_REBUILD_PLAN.md owns the canonical Phase I execution sequence.
- 07_DECISIONS.md records accepted decisions.
- 08_PROJECT_AUDIT.md remains a historical/static audit with concise
  current-state notes; it is not this design specification.

This document does not claim that the current runtime has already been broadly
replaced. I1 validates the first Pour Core proof: Build preparation can
hold-to-pour into a runtime Serving Glass whose actual liquid amounts are held
in milliliters and bridged to the existing ounce-authored matcher. I2 extends
that same state with arbitrary fixed amounts, a discrete Dash compatibility
amount/count, and manual TopUp target guidance; recipe data and the matcher
remain unchanged. I3 now creates separate runtime Serving Glass, Mixing Glass,
and Shaker state for an active preparation session; bounded ice belongs to one
container, full liquid transfer moves actual ingredient amounts and Dash counts,
and Strain retains source ice. I4 makes Build the first fully migrated route:
the player pours directly into a fresh Serving Glass and manually confirms its
actual contents for the existing matching/result boundary. Build's detached
target-zone/timing score is no longer active. I5 makes Stir the second migrated
route: actual liquid is prepared in the Mixing Glass, explicit ice is required,
mouse-angle input visibly controls the spoon, and an explicit Strain moves the
actual liquid to the Serving Glass. I6 makes Shake the third migrated route:
actual liquid is prepared in the Shaker, explicit ice is required, a bounded
Left/Right rhythm visibly moves the Shaker, and an explicit Strain moves actual
liquid to the Serving Glass. I7 makes Float the fourth migrated route: ordered
actual additions enter the Serving Glass, each later spoon-assisted layer records
its own adjacent-boundary quality, and manual completion submits only that actual
Serving Glass composition. The legacy Float target gauge and Jigger path are no
longer active. I7.5 removes their proven-dead fixed-increment runtime
authority: `JiggerPanelController`, `BrewSessionData`,
`BrewCompositionFactory`, and their shelf/mini-game adapters no longer exist.
The four Jigger prefab components are removed and their visual roots are
inactive; `BrewComposition` remains the active compatibility snapshot for the
unchanged matcher.

---

## 2. Core Principle

The old prototype flow is:

~~~text
Select ingredient
→ choose quantity through UI
→ pour
→ composition is effectively established
→ enter a separate technique minigame
→ receive technique score
~~~

The approved target flow is:

~~~text
Select bottle / ingredient
→ physically perform the preparation
→ the player's actual action changes the container state
→ the actual container state becomes the cocktail composition
→ perform only the technique required by the recipe
→ serve what was actually made
~~~

> **What you pour is what you made.**

Cocktail preparation itself is the gameplay. A preparation technique must not
be followed by an unrelated scoring minigame whose result is disconnected from
the actual drink state.

This target means authentic-feeling bartending, not literal real-world fluid
simulation. Inputs should be readable, responsive, and satisfying at game
scale. The project does not need to reproduce real preparation duration,
temperature curves, liquid dynamics, or professional physical precision.

---

## 3. Design Goals

Bartending 2.0 must satisfy all of the following:

1. **Preparation itself is gameplay.** The player does not merely configure a
   recipe in UI and then complete an unrelated timing test.
2. **Actual input corresponds to actual drink state.** A transferred amount,
   selected target, ice action, or layer-building action updates the
   authoritative preparation state.
3. **Techniques feel physically distinct.** Build, Stir, Shake, and Float
   communicate different gestures and preparation meanings.
4. **Authentic-feeling over literal simulation.** Simplified states may stand
   in for dilution, chilling, aeration, density, and other physical properties.
5. **No duplicated grading layers.** Do not construct a drink, then grade a
   detached minigame for the same technique.
6. **Recipe complexity comes from meaningful preparation.** Ingredients,
   order, source/target container, amount, and required technique should make
   recipes interesting.
7. **New mechanics follow adopted content.** Add a mechanic only after a
   representative cocktail/content requirement demonstrates why it is needed.
8. **Narrative and preparation remain connected.** The guest request informs
   what the player chooses to make; preparation determines what was actually
   served.
9. **The first implementation remains narrow.** A representative subset must
   prove the interaction/data model before catalog-scale production begins.

---

## 4. Current Prototype Versus Target

| Concern | Current prototype baseline | Bartending 2.0 target |
| --- | --- | --- |
| Amount authority | UI increment selection establishes shelf/session composition before technique play | Amount transferred by player action updates the authoritative target-container state |
| Build | Separate hold/release score after composition is prepared | Direct construction in the serving glass through actual pours |
| Stir | Automated gauge and one timing input | Player-driven bar-spoon interaction in a Mixing Glass |
| Shake | Short sequential input test | Short left/right rhythm interaction that visibly moves the Shaker |
| Float | Build-like hold/release test | Ordered layer construction: layered shots and final floats share actual pours, visible layer order, and per-boundary quality |
| Recipe matching input | Prototype BrewComposition from prepared session | Composition derived from the final/served preparation state |
| Unit limitations | UI exposes fixed 0.5 oz / 1.0 oz increments | Canonical quantity supports arbitrary data amounts; UI presentation is separate |

The target does not invalidate Phase G/H ownership decisions. In particular:

- cocktail identity remains separate from technical recipe identity;
- RecipeCatalogData remains catalog membership/default-unlock authority;
- actual served cocktail remains distinct from a request constraint;
- technical matching remains independent from customer/story request evaluation;
- existing persistence keys and formats are not changed merely by adopting this
  direction.

---

## 5. Preparation Model

### 5.1 Authoritative state flow

~~~text
Source ingredient or source container
        ↓
Player preparation action
        ↓
Target container state changes
        ↓
Further technique/transfer actions change that state
        ↓
Final served container state
        ↓
Runtime composition for technical recipe recognition
~~~

The source of truth is the resulting container/preparation state, not a
parallel UI-selected recipe amount. Visual action and recorded gameplay state
must agree.

### 5.2 Container state: required conceptual minimum

The initial model must support these conceptual fields when relevant:

- liquid ingredients and their transferred quantities;
- current total liquid volume;
- ice presence/amount;
- layer state for drinks that require it;
- serving/intermediate-container identity;
- simplified technique outcomes when a recipe requires them.

The exact C# type, field layout, serialization boundary, and runtime ownership
are deliberately not fixed by this document. Phase I should first identify the
narrowest extension that fits the current recipe/matching architecture.

### 5.3 Deliberate simplifications

The initial model does not require:

- continuous fluid meshes;
- per-particle liquid physics;
- real density calculations;
- real thermal simulation;
- a complete dilution chemistry model;
- full professional glassware constraints;
- a physically correct ice-melt model.

Where feedback matters, use visible/readable simplified states rather than
claiming a scientific simulation.

---

## 6. Measurement and Units

### 6.1 Canonical runtime/data direction

Internal liquid quantity uses **milliliters (ml)** as the canonical future
runtime/data unit.

Oz remains allowed as a presentation, learning, or reference unit. UI can
display converted values when that is clearer for a recipe or player audience.

The old 0.5 oz / 1.0 oz button limitation must not constrain future recipe
data. Arbitrary authored quantities such as 0.25 oz, 0.75 oz, 1.5 oz, and
their ml equivalents must be representable.

### 6.2 Player precision

Exact real-world professional precision is not required from the player.

Recipe matching, acceptance ranges, near-miss feedback, and the relationship
between technique quality and drink quality remain tuning/design questions.
They are not finalized by this specification.

The important contract is:

~~~text
player transfers an amount
→ that amount is represented in the preparation state
→ matching/evaluation reads that state
~~~

### 6.3 Presentation conversion

If an interface presents ounces:

- conversion is presentation-only;
- internal authored/runtime values remain ml;
- converted display must not silently round away an amount that the recipe
  model needs to distinguish;
- an implementation may choose a readable display policy after representative
  recipe validation.

Do not introduce a general conversion service, data migration, or final
tolerance table before a real implementation phase proves a need.

---

## 7. Containers

The first target supports at least the following containers.

| Container | Primary role | Relevant initial state |
| --- | --- | --- |
| Serving Glass | Final presentation and direct Build target | liquids, total volume, ice, layer state, garnish state when needed |
| Mixing Glass | Intermediate container for stirred drinks | liquids, total volume, ice, simplified Stir outcome |
| Shaker | Intermediate container for shaken drinks | liquids, total volume, ice, simplified Shake outcome |

### 7.1 Serving Glass

Build primarily happens directly in the Serving Glass. It may also receive a
strained drink, a float layer, ice, a top-up, or a garnish.

### 7.2 Mixing Glass

Stirred ingredients and required ice are prepared here before a Strain action
transfers liquid to the Serving Glass.

### 7.3 Shaker

Shaken ingredients and required ice are prepared here before a Shake action
and later Strain transfer.

### 7.4 Container boundary

Containers are a gameplay/preparation concept. This document does not require
a physical GameObject for every conceptual container, a new Scene, a global
singleton, or a container component hierarchy before a narrow implementation
requires one.

### 7.5 I3 current implementation boundary

I3 implements one runtime-only preparation state per active shelf with exactly
one Serving Glass, Mixing Glass, and Shaker. Its active target receives actual
Pour Core liquid. Ice is bounded present/absent state on its target container;
it is not a global recipe flag. Full transfer moves the actual liquid entries
and retained Dash counts while leaving source ice in place. Non-scored Strain
accepts only Mixing Glass or Shaker as a source and moves liquid to the Serving
Glass while retaining source ice.

I4 removes those temporary target/ice/transfer/Strain controls from the active
Build route: Build now owns only direct Serving Glass preparation. I5 then
adopts the Mixing Glass, explicit ice, and Strain operations for Stir; I6
adopts the Shaker, explicit ice, and Strain operations for Shake. I7 reuses
the Serving Glass and shared Pour Core for ordered Float additions. It adds a
runtime-only layered-drink companion and prototype stacked visuals, not fluid
physics, density simulation, capacity/overflow rules, or authored recipe
preparation-sequence data.

---

## 8. Liquid Transfer: Common Pouring Foundation

The shared foundation is:

~~~text
Source ingredient/container
→ target container
→ amount transferred
→ target state updated
~~~

### 8.1 Pour requirements

A Pour interaction should support:

- selecting a source bottle/ingredient or an eligible source container;
- selecting/using a valid target container;
- continuous or sufficiently granular player-controlled transfer;
- a visible pour state proportional to the interaction;
- authoritative update of transferred amount;
- clear cancellation/stop behavior;
- feedback when a target/action is invalid.

The initial interaction need not mimic every physical bottle gesture. It must
make the player feel that they are controlling a transfer, and the recorded
quantity must match that visible action.

### 8.2 Source-to-target transfer

The common transfer rule must also support later container operations:

~~~text
Bottle → Serving Glass
Bottle → Mixing Glass
Bottle → Shaker
Mixing Glass → Serving Glass through Strain
Shaker → Serving Glass through Strain
~~~

This is a conceptual transfer vocabulary, not authorization to implement every
route before the representative recipe set requires it.

### 8.3 No parallel authority

Avoid a design where a UI field says one amount was selected while the visible
pouring action has no authoritative gameplay effect.

Avoid preserving a compatibility DTO as a second long-lived composition source
if the new container state is authoritative. Any transition bridge must be
explicit, minimal, and temporary.

---

## 9. Preparation Vocabulary

The first intended supported vocabulary is:

~~~text
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
~~~

This is an initial vocabulary target, not a mandate to implement all actions
before representative content needs them.

### 9.1 Measure / Pour

Measure prepares an intended liquid amount for transfer. It may be represented
through a jigger, a pour stop, a fill indicator, or another readable
game-scale interaction.

Pour transfers a liquid from a bottle/source to a target container. The
transferred amount becomes actual target state.

Measure must not reintroduce the old fixed 0.5 oz / 1.0 oz data constraint.

### 9.2 Dash

Dash is for ingredients such as bitters.

- It is a discrete small-dispensing action.
- A simple initial model may use 1 input = 1 dash.
- It should not force the player to manually pour microscopic quantities.

I2 currently uses one project gameplay compatibility amount of 1 ml per dash
at the container-to-legacy bridge. It is not a universal real-world claim, and
the serialized recipe representation remains deferred to representative
validation. Dash is therefore expressible without pretending it is a normal
long pour.

### 9.3 TopUp

TopUp uses the common Pour foundation, but its expected completion condition
is filling the drink/glass appropriately rather than hitting only a
conventional fixed measured amount.

I2 proves the first target-final-volume form: a runtime guidance instruction
reports current liquid volume against a target, while the player still transfers
the liquid through Pour Core. It neither auto-fills the target nor introduces a
glass-capacity rule. Possible later representations include:

- target fill amount;
- target fill ratio;
- another equivalent simplified policy.

The exact data model is intentionally not locked before adopted recipes prove
which policy is sufficient.

### 9.4 AddIce

Ice is a preparation action with an explicit target container.

Examples:

- ice into Shaker;
- ice into Mixing Glass;
- ice into Serving Glass.

The initial model can represent ice as a discrete presence/count/amount state.
It does not require melting or thermal simulation.

### 9.5 Stir

Stir is an actual spoon-driven interaction:

1. Ingredients are in a Mixing Glass.
2. Required ice is added.
3. Player movement/rotation drives visible bar-spoon motion.
4. Simplified evaluation may consider rotation consistency, speed, duration,
   or equivalent readable variables.
5. Output may represent simplified dilution/chilling/consistency quality.

The interaction is compressed to satisfying game-scale duration. It must not
recreate a detached automatic gauge after the player’s composition is already
final.

### 9.6 Shake

Shake is a short rhythm-driven Shaker interaction:

1. Ingredients and ice are placed into the Shaker.
2. A short left/right rhythm pattern is presented.
3. Player uses keyboard/controller input such as A / D or Left / Right.
4. The Shaker visibly responds to each input.
5. Timing may report Perfect / Good / Miss.
6. Performance maps to simplified consistency, chilling, or aeration quality.

Recipes may eventually select reusable rhythm-pattern templates. Do not build
a bespoke beatmap/music-game system for every cocktail unless adopted content
later justifies it.

### 9.7 Float

Float is **ordered layer construction**, not a special final-topping action.
One bounded model supports both a layered shot beginning from an empty Serving
Glass and a final float over a prepared base:

1. The first actual pour creates Layer 0. It establishes the bottom of the
   stack and has no boundary to judge.
2. Every later actual addition is a spoon-assisted Float pour into the same
   Serving Glass. It becomes the next ordered upper layer.
3. Each later addition records exactly one adjacent lower-to-upper boundary as
   `Clean`, `Partial`, or `Mixed` from aggregate controlled-pour behavior.
4. The visible stack reflects the same ordered layers and boundary states.

I7's bounded controls are intentionally gameplay-scale rather than fluid
simulation. Normal Pour remains unchanged for other methods; Layer 0 uses the
ordinary 30 ml/s proof rate. For later layers, Shift/`Gentle` uses 15 ml/s,
normal uses 30 ml/s, and Ctrl/`Aggressive` uses 60 ml/s. For later layers, an average rate at or below
22 ml/s with no more than 10% aggressive duration is `Clean`; a rate at or
below 42 ml/s with no more than 50% aggressive duration is `Partial`; the
remaining result is `Mixed`. These are initial tuning values. This aggregate
per-addition policy means a short late aggressive burst cannot be judged solely
from the release frame.

Layer state is preparation/technique information, not duplicate cocktail
composition authority. The Serving Glass still owns the actual ingredient ml
amounts supplied to the legacy BrewComposition bridge. A bad boundary never
deletes ingredient identity or quantity, a later clean boundary never repairs an
earlier mixed boundary, and technical recipe matching remains independent of
Float quality. Current recipe assets do not author expected layer sequences:
I7 preserves actual layer order but deliberately defers order scoring to I9
preparation-step authoring.

The visual drink must reflect the result. I7 uses stacked runtime regions and
distinct thin/medium/thick boundary bands for `Clean`/`Partial`/`Mixed`; final
art, shaders, and real liquid behavior remain out of scope. Float therefore
differs from Build through physical control and visible separation, not a reused
timing gauge.

### 9.8 Strain

Strain transfers liquid from a Shaker or Mixing Glass into a final Serving
Glass while retaining ice where appropriate.

The initial interaction may be simple, but it must make the transfer and
source/target distinction legible. Do not require a complex strainer
simulation before it creates a player-facing benefit.

### 9.9 Garnish

Garnish is a separate preparation action. Initial implementation may remain
simple—such as click or drag/drop—when a representative recipe needs it.

Do not create an unnecessary garnish precision minigame.

#### I8 current implementation boundary

I8 implements the minimum discrete final-preparation proof. Once actual liquid
has reached the Serving Glass, the player may choose one garnish identity,
choose `Rim`, `InGlass`, or `Surface`, apply or replace that choice, clear it,
or leave no garnish before manual serve.

```text
Serving Glass with actual liquid
→ choose garnish identity
→ choose Rim / InGlass / Surface
→ apply, replace, clear, or leave absent
→ manually serve
```

`BartendingContainerState` owns one optional, runtime-only
`BartendingGarnishState` only for a Serving Glass. It is deliberately
non-liquid: it does not enter `BrewComposition`, change canonical milliliter
contents, alter technique quality, or affect the unchanged `RecipeMatchService`.
`MiniGameResult` carries a result-bound snapshot for later I9 preparation
evaluation without reconstructing the player's choice.

The current proof controls expose Lemon Twist, Cherry, and Olive plus the three
placements. They are not a final garnish catalog, inventory, unlock model, or
recipe requirement. The temporary UI renders from the same runtime state: it
uses an existing glass visual when present and a labelled runtime Serving Glass
placeholder where a Stir or Shake prefab has only a source-container visual.

Expected garnish validation belongs to I9 preparation-step authoring. Salt or
sugar rims are not garnish placement, and peel expression, multi-object
garnishes, flaming, cutting, and other presentation mechanics remain deferred
until adopted content proves a need.

---

## 10. Method Identities

| Method | Player fantasy | Primary challenge | Not the target |
| --- | --- | --- | --- |
| Build | Construct the drink directly in its final glass | amount, order, destination, top-up, recipe knowledge | a second timing score after the drink is built |
| Stir | Calm, controlled integration in a Mixing Glass | spoon-driven consistency and game-scale technique quality | an automatic moving gauge unrelated to spoon motion |
| Shake | Deliberate energetic mixing | short left/right rhythm and readable response | a large standalone music game |
| Float | Construct an ordered visible layer stack | controlled upper-layer flow, boundary preservation, and order awareness | a final-topping-only rule or Build's hold/release test with different art |

The methods may share the Pour foundation. They should not share an
indistinguishable player decision merely because they all involve a container.

### 10.1 I4 current implementation boundary

Build now begins with the Serving Glass as its only active target. The player
selects ingredients, pours actual milliliter amounts through Pour Core, and
chooses when to press **Complete Build**. The final Serving Glass is converted
only at the established legacy BrewComposition boundary; expected recipe data is
not used to construct or repair the submitted drink. Because the current shared
MiniGameResult contract has no separate preparation-complete value, Build emits
`success = true`, `repDelta = 0`, and `grade = "Complete"` to mean that direct
preparation ended, not that a detached timing challenge was passed. Recipe and
request correctness remain downstream.

### 10.2 I5 current implementation boundary

Stir begins with the Mixing Glass as its only direct preparation target. The
player pours actual milliliter contents, explicitly adds ice, and then moves the
mouse around the mixing-glass center. Wrapped angular deltas accumulate valid
rotation; implausibly large per-sample jumps are rejected; continuity and
direction stability form a bounded consistency value. This produces
`Good`, `Acceptable`, or `Poor` technique quality after a short minimum
interaction, without inspecting expected recipe data. The player then explicitly
Strains through the existing I3 operation. The Serving Glass snapshot is the
only submitted composition. `Good` maps to the existing compatible `success =
true`, `repDelta = 1`; `Acceptable` to `success = true`, `repDelta = 0`; and
`Poor` to `success = false`, `repDelta = 0`. Recipe matching remains a separate
downstream judgement.

### 10.3 I6 current implementation boundary

Shake begins with the Shaker as its only direct preparation target. The player
pours actual milliliter contents, explicitly adds ice, and then follows one
bounded alternating Left/Right eight-note rhythm. A/Left Arrow and D/Right
Arrow use time-based Perfect (`0.09s`) and Good (`0.20s`) windows against a
fixed `0.5s` interval after a `0.6s` lead-in. Each input drives the Shaker in
its selected direction. Every event is consumed once: a wrong direction is a
Miss, expired events become one Miss, and off-beat/extra inputs earn no hit and
count only as bounded input errors. The resulting correct-hit accuracy and
input-error count produce `Good`, `Acceptable`, or `Poor` without inspecting
expected recipe data. The player then explicitly Strains through the existing
I3 operation. The Serving Glass snapshot is the only submitted composition.
As in I5, `Good` maps to `success = true`, `repDelta = 1`; `Acceptable` to
`success = true`, `repDelta = 0`; and `Poor` to `success = false`, `repDelta =
0`. Recipe matching remains a separate downstream judgement.

### 10.4 I7 current implementation boundary

Float begins with a fresh Serving Glass and its actual ml contents remain the
only submitted cocktail composition. The first successful Pour Core addition is
Layer 0; each later addition is an explicit Float-mode, spoon-assisted layer.
`BartendingLayeredDrinkState` records individual ordered additions only after
the same liquid has been accepted by the Serving Glass, plus one immutable
`Clean`, `Partial`, or `Mixed` boundary for every adjacent pair. It is therefore
a bounded technique/visual companion rather than a second composition store.

The player chooses when to complete Float; expected recipe quantity, count, and
order never auto-complete or repair the drink. All clean boundaries produce
`Good` (`success = true`, `repDelta = 1`); one or more `Partial` boundaries and
no `Mixed` boundary produce `Acceptable` (`success = true`, `repDelta = 0`);
any `Mixed` boundary produces `Poor` (`success = false`, `repDelta = 0`).
Recipe recognition consumes the unchanged Serving Glass compatibility snapshot.
Current recipe assets have no authoritative expected layer sequence, so actual
order is preserved but sequence scoring is deferred to I9.

---

## 11. Recipe and Preparation Data Direction

Future recipe data must eventually represent more than ingredient identity and
one technique label. It must be able to express the actual preparation
sequence required by adopted content.

Conceptual example only:

~~~text
Daiquiri

1. Rum 60 ml → Shaker
2. Lime Juice 25 ml → Shaker
3. Simple Syrup 15 ml → Shaker
4. Add Ice → Shaker
5. Shake
6. Strain → Coupe
~~~

Another conceptual example:

~~~text
Stirred Cocktail

1. Base Spirit → Mixing Glass
2. Modifier → Mixing Glass
3. Add Ice
4. Stir
5. Strain → Serving Glass
6. Garnish
~~~

### 11.1 Direction, not final schema

This specification does not define:

- a final generalized recipe-step schema;
- a generic preparation scripting language;
- final ScriptableObject field names;
- final enum values;
- final serialization migration;
- final tolerance values;
- a new global execution manager.

Phase I must first inspect the current recipe/matching boundary and choose the
narrowest extension that can express the representative recipe set. A
generalized framework is justified only when validated adopted content proves
that repeated structure is genuinely shared.

### 11.2 Existing ownership remains valid

The following Phase G/H contracts remain valid during this direction change:

| Concern | Existing owner/boundary to preserve |
| --- | --- |
| Ingredient identity/display | IngredientData |
| Cocktail identity/presentation/reaction metadata | CocktailData |
| Technical recipe standard | CocktailRecipeData |
| Catalog membership/default configuration | RecipeCatalogData / RecipeCatalogEntry |
| Recipe knowledge state | RecipeUnlockManager / recipe progression persistence |
| Technical recognition | RecipeMatchService |
| Customer request | OrderData |
| Actual served cocktail | matched recipe target cocktail |

Bartending 2.0 changes how the runtime composition is produced. It does not
license canonical ownership duplication or a reverse CocktailData-to-recipe
route.

---

## 12. Real/Classic Cocktail Validation Direction

Prototype-only disconnected recipes are not the final catalog model.

Future production uses a curated set of real/classic cocktails, with
IBA-recognized recipes as a major reference direction where applicable.
Established non-IBA classics may also be included according to the catalog
policy in 04_CONTENT_ROADMAP.md.

### 12.1 Representative validation set

Before large catalog production, validate approximately **6–8 representative
real/classic cocktails**.

This set exists to answer:

- Does the measurement model support real recipes?
- Do shared ingredients create understandable learning?
- Do containers and preparation steps support the recipes?
- Are Dash, TopUp, Strain, and Float needs represented correctly?
- Does the recipe interaction remain fun?

I9A selected the source-audited candidates in
`Docs/12_IBA_REPRESENTATIVE_CATALOG_AUDIT.md`. I9B now validates the adopted
seven: Dry Martini, Manhattan, Negroni, Last Word, Gin Fizz, French 75, and
Hanky Panky. New York Sour is deferred pending an approved semantic for the
official `Few Drops of Egg White`; I9B does not implement `Drop`.

#### I9B implemented preparation boundary

`CocktailRecipeData` owns a bounded ordered `RecipePreparationStep[]` using
only the adopted action vocabulary: `Pour`, `Dash`, `Splash`, `AddIce`, `Stir`,
`GentleStir`, `Shake`, `Strain`, `PourFromContainer`, `TopUp`, and `Garnish`.
This is typed recipe data, not a generalized scripting language.

The same production action that changes a container records an
`ActualPreparationAction` in the current preparation trace. Container contents
remain canonical liquid truth; the trace is action/order evidence only.
`RecipePreparationEvaluator` compares that trace and final garnish state with
the recipe expectation, while the unchanged `RecipeMatchService` remains the
sole normal liquid-volume recognition boundary. The result keeps composition,
preparation, and technique quality separate.

I9B adopts a 10–20 ml SURLHWA gameplay range for `Splash` (15 ml default).
It is a qualitative player-triggered Soda Water addition whose actual ml enters
the Serving Glass; it is neither a Dash nor an IBA volume conversion. A shared
short direct-input `GentleStir` operates on the Serving Glass for Negroni and
French 75. `PourFromContainer` records Gin Fizz's official post-shake pour
semantic while reusing the existing transfer primitive and retaining source
ice. Garnish remains one active runtime garnish; expected data can accept an
alternative or mark it optional. Glassware is recipe metadata only.

### 12.2 Shared ingredient principle

Prefer shared ingredients. One newly introduced ingredient should be capable
of making several future cocktails possible when content supports that result.

~~~text
Learn ingredient
→ recognize where it can be used
→ multiple recipes become possible
→ knowledge carries into later encounters
~~~

Avoid the prototype pattern in which every recipe introduces an entirely
isolated ingredient set.

### 12.3 Ingredient availability

Ingredients should not all be available from the start. Availability may expand
through business days, story progression, authored unlock events, or another
intentionally defined progression point.

This is not arbitrary gating. The purpose is to expand the player’s possible
recipe space and reinforce transferable knowledge.

This document does not authorize ingredient-unlock persistence, key changes,
or a full progression data model.

---

## 13. Guide and Discovery Direction

The fresh-save learning problem identified by the red-team review must be
solved before public content scale-up.

The player must not be required to know a locked recipe in order to unlock it.
Known recipes must remain easy to consult. The Guide must not become a forced
memorization challenge.

| Knowledge state | Intended information |
| --- | --- |
| Unknown | A reason to care and a possible learning path |
| Partially Known | Actionable clues: ingredient family, preparation method, sensory character, or guest observation |
| Known | Full learned recipe |

The exact UI, unlock trigger, and clue data are deferred. The requirement is a
learnable path from curiosity to informed attempt.

### I11A implemented foundation

I11A now supplies the bounded persistence and presentation foundation without
authoring a guest discovery loop. Recipe knowledge is owned by the existing
serialized `RecipeUnlockManager` component, whose active semantics are
`Unknown` → `Partial` → `Known`. `Partial` stores typed fact categories:
identity, method, exact ingredient identity, that ingredient's amount, garnish,
and preparation sequence. Amount facts always carry their corresponding
ingredient fact. `Known` remains an explicit successful-discovery grant; partial
facts do not infer completeness.

The Guide and Codex project only those facts. `CocktailRecipeData` remains full
authored recipe truth, `RecipeMatchService` remains liquid-composition truth,
and actual preparation/container state remains gameplay truth. The I10 shelf
continues to ignore recipe knowledge, so available ingredients and unknown
recipes remain valid together. Legacy binary unlock entries are derived as
`Known` when loading old saves; the first-recipe-reveal UI flag remains a
separate persistence concern.

Discovery is not invalidated by showing a learned full recipe. Discovery can
include learning why a cocktail matters, when it fits a guest, how its
preparation feels, and what later uses become possible.

### I11B production trigger implementation

I11B begins normal-play learning with three explicit sources only. A consumed
authored request or dialogue line can dispatch one or more typed facts. The
initial Stir tutorial request carries a method-only Dry Martini hint. The
actual shelf may reveal a liquid ingredient the player used, but only when the
request supplied an explicit target recipe and that authored recipe contains
the same `IngredientData`. A wrong liquid remains action-trace evidence, not a
player-knowledge guess; no amount is inferred from a pour.

The existing successful composition match remains the only I11B source of full
`Known` state. These triggers do not change container truth, preparation
evaluation, technique quality, guest satisfaction, or I10 availability. I11C+
will add richer authored clues and multi-encounter pacing rather than a text
parser or generalized inference system.

---

## 14. Guest and Cocktail Response Direction

Using real/classic cocktails does not automatically solve emotional matching.

The future response model should be able to distinguish:

| Category | Examples |
| --- | --- |
| Physical/sensory cocktail characteristics | Bitter, Sweet, Sour, Dry, Strong, Light, Rich, Refreshing, Herbal |
| Narrative/emotional interpretation | Comfort, Clarity, Awakening, Longing, and other authored concepts |

The exact taxonomy is not finalized by this document. Existing EmotionTag code
and serialized data are not redesigned by this decision.

Important guests must not receive arbitrary hidden emotional targets that
contradict their authored request/dialogue. A future special-guest encounter
should distinguish a deliberate player interpretation from a generic
randomized mood score.

The intended relationship is:

~~~text
Narrative request helps determine what to make
Recipe knowledge helps determine how to make it
Actual preparation determines what was served
Guest response acknowledges the actual offering and the encounter
~~~

This does not require every guest to have a giant branching outcome tree.

---

## 15. Explicit Non-Goals

The following must not be implemented preemptively:

- Muddle;
- Blend;
- Flame;
- Dry Shake;
- Reverse Dry Shake;
- Double Strain;
- Salt/Sugar Rim minigames;
- advanced density/fluid simulation;
- realistic thermal simulation;
- generalized bartending scripting language;
- bespoke Shake rhythm charts for every recipe;
- a large 30+ cocktail catalog before representative validation.

These may be reconsidered only when adopted content demonstrates a
player-facing requirement. A technique that is not represented in the
representative content set is not a reason to build its framework now.

---

## 16. Phase I Delivery Sequence

The canonical detailed work order is in 05_REBUILD_PLAN.md. Bartending 2.0
uses the following implementation progression:

| Phase | Bartending 2.0 responsibility | Required proof |
| --- | --- | --- |
| I0 | Narrative reading safety; preserve legacy minigames as temporary fallback/reference | Story reading does not accidentally consume a customer encounter; old implementations remain available until replacements are proven |
| I1 | Pour Core | Actual player transfer updates authoritative target-container quantity |
| I2 | Amount Variants | Arbitrary small amounts, Dash, and TopUp are expressible without fixed increment UI |
| I3 | Container Operations | Serving Glass, Mixing Glass, Shaker, AddIce, liquid transfer, and Strain can express intermediate preparation |
| I4 | Build — validated | Direct Serving Glass preparation is manually completed and its actual contents enter the existing matcher/result flow without a detached Build score |
| I5 | Stir — validated | Mixing Glass direct preparation, explicit ice, mouse-driven spoon motion, and explicit Strain produce a technique result distinct from recipe matching |
| I6 | Shake — validated | Shaker direct preparation, explicit ice, an eight-note alternating rhythm, visible input-driven motion, and explicit Strain produce a technique result distinct from recipe matching |
| I7 | Float / Layering — validated | Ordered actual Serving Glass additions support layered shots and final floats; each later layer has visible Clean/Partial/Mixed boundary quality while composition remains separate |
| I7.5 | Legacy Bartending Cleanup — validated | Removes the unreachable fixed-increment Jigger/session/factory runtime path; inactive legacy gauge and target-zone prefab visuals remain deferred asset cleanup |
| I8 | Garnish — validated | One optional actual garnish identity and bounded Serving Glass placement is recorded separately from liquid composition and technique quality before manual serve |
| I9 | Representative Real Cocktail Catalog | 6–8 recipes validate the interaction/data model |
| I10 | Ingredient Progression — implemented | Day-derived shelf availability expands recipe space; licensed-Editor runtime validation remains pending |
| I11 | Guide / Discovery | Unknown, partially-known, and known learning path works from fresh progress |
| I12 | Guest Response | Cocktail characteristics and authored important-guest preferences are revisited |
| I13 | Little Prince Interpretive Choice / Memory | One choice → response → later recall → environmental trace proof exists |
| I14 | Feedback Hierarchy | Physical result, guest response, quiet record, then night result are legible |
| I15 | Representative Audio / Visual Pass | One complete encounter proves the atmosphere/presentation target |
| I16 | Two-Night Vertical Slice | A 25–35 minute, playtested proof combines the adopted direction |

No later phase starts merely because an earlier one has code. Each exit criterion
requires focused validation and an intended diff review.

---

## 17. Implementation Guardrails

Phase I must preserve the following constraints:

- Do not reopen completed Phase G/H ownership or persistence decisions without
  a proven conflict.
- Do not change existing PlayerPrefs keys/formats merely to support Bartending
  2.0.
- Do not delete legacy minigames until replacements are validated and all
  runtime/serialized references have been audited.
- Do not create Scenes, Prefabs, ScriptableObjects, or packages merely to make
  a target architecture appear complete.
- Do not add techniques, content, or data fields without an adopted
  representative requirement.
- Do not treat this document as authority to select a final cocktail list,
  write new story content, or define final guest outcomes.
- Keep one-purpose implementation phases and commits.

The desired outcome is a small playable proof, not an abstract universal
bartending platform.

---

## 18. Phase I Completion Target

Phase I is successful when the project can demonstrate, through a focused
two-night vertical slice:

~~~text
learnable recipe
→ actual player preparation
→ authoritative drink/container state
→ meaningful technique only where required
→ understandable guest response
→ one remembered choice
→ one environmental trace
~~~

The slice should be approximately 25–35 minutes, subject to actual playtesting.

It is not sufficient for Phase I to produce a new container model, more
minigames, or a larger catalog in isolation. The player-facing proof is the
completion condition.
