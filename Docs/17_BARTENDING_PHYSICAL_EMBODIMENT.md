# Phase K — Bartending physical embodiment

Baseline: `4cec4fb`, branch `refactor/project-rebuild`, 2026-09-29.
The only starting modification is the owner's `.codex/config.toml`; excluded
from all edits and staging. Campaign content and persistence are frozen.

## K1 audit

Read the repository contract, vision, current gameplay/architecture, rebuild
rules, I-series bartending design and current production/campaign records.
Inspected the four production method scripts, shelf, container/preparation
state, action trace, preparation evaluator, matcher, garnish/GentleStir views,
BartendingScene and the referenced four prefabs. The scene instantiates the
method prefabs; their UI RectTransforms use very different legacy scales.

| Action | Baseline classification | Input → visible action → state → evaluation |
|---|---|---|
| Bottle pour | STATE_ONLY | Space → text → AddLiquid ml → coalesced Pour → composition matcher |
| Jigger/Measure | NOT_APPLICABLE | I7.5 removed fixed-increment authority; inactive prefab residue only |
| Overflow | NOT_APPLICABLE | Capacity clamping exists in container; production targets have no capacity |
| Container transfer | STATE_ONLY | Button → text → immediate full liquid move → Strain/PourFromContainer trace |
| Stir | INCORRECT_PRESENTATION | Mouse angle → orbit plus full self rotation → existing angular tracker → Stir result |
| GentleStir | INCORRECT_PRESENTATION | Same self-rotation problem in shared serving-glass spoon |
| Shake | PARTIALLY_REPRESENTED | L/R → horizontal translation → unchanged contents/rhythm judgment → Shake trace |
| Build | STATE_ONLY | Space/D/I/J → quantities/technique → serving state → preparation and matcher |
| Float | PARTIALLY_REPRESENTED | Space/Shift/Ctrl → proportional full-height bands → actual ordered additions/boundaries → Float result |
| Ice | STATE_ONLY | I → text → container-local boolean → AddIce trace; source retains ice on transfer |
| Garnish | PARTIALLY_REPRESENTED | Selection/G → immediate marker → serving garnish → final garnish trace |

Confirmed Stir windmill cause: `spoonBaseAngleDeg + angleDeg`; GentleStir
similarly adds the complete input angle to shaft rotation. Float bands divide
by current total volume, so even a tiny first pour appears completely full.
All transfer/strain paths currently move liquid before any tool movement.

## Authority contract

Container/preparation state owns every ml and ice/garnish identity. The shelf
orchestrates accepted player actions and trace evidence. Presentation follows
that state and never builds a composition or evaluates a recipe. Existing
layer, technique, matcher, FIT, knowledge and supply authorities remain.
Bottle supply has availability but no finite stock quantity; do not invent an
inventory. Temporary visuals and motion defaults are HUMAN_TUNING_LATER.

Implementation and executed validation records will follow by milestone.

## K2 — Bottle foundation

Added a replaceable `BartendingPhysicalPresentation` on the method root. Live
Space input waits for grip movement, neck alignment and tilt before invoking
the existing ml API. Release/source/target changes stop the stream immediately
and return the bottle upright/home. Fill observes container state. All math
converts vessel/neck world points to the UI's center-anchor space, including
the existing non-centered UI pivots and scaled vessel transforms.

Validation: Unity import/compile and `SURLHWACampaignValidation.RunIntegrated`
PASS (`Logs/phase-k2-editor.log`, exit 0). Focused Editor test instantiates all
four real prefabs, exercises pickup/tilt/return, checks neck alignment and
absence of presentation-created liquid, and checks accepted-transfer stream
gating: PASS (`Logs/phase-k2-physical.log`, exit 0). Project synchronization
then `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: PASS, 0 errors,
8 existing warnings. The first dotnet attempt preceded project regeneration
and failed to find the new file; it is superseded by the regenerated build.
No scene/prefab changes. Generated font whitespace is excluded/restored.
Play Mode and rendered readability remain pending K11; this is no human feel
claim. Dash/Splash and container motion are subsequent milestones.

## K3 — Finite measurement

The former Jigger was inactive legacy residue. The new optional M measurement
mode uses a session-local Jigger in `BartendingPreparationState`, implemented
by the same `BartendingContainerState` as every other vessel. Appended runtime
enum member preserves existing numeric identities; no saved format changes.
60 ml is the first-pass capacity (Dry Martini's largest individual pour is
60 ml; larger amounts use repeated measurements). Capacity is serialized on
the shelf. Space fills; release returns the bottle; T moves/tilts/empties the
Jigger into the current working vessel, then returns it. Source choice is
locked while a measured portion remains. A nonempty Jigger must be transferred
before technique/serve progression. Build still permits direct Pour/Dash.

Overflow fate: incoming minus accepted ml is spilled/lost, not waste currency,
not recipe composition and not preparation evidence. The Jigger remains
capped; a simple short spill stream displays excess. Supply has no finite
bottle-stock model. Partial transfer is proportional and lives entirely in
the existing container class. Measurement records destination Pour evidence;
adjacent repeated measures coalesce so existing authored recipes need no new
Measure step or changed semantics. Float measurement integration follows K8.

Validation: focused Editor capacities 30/60/75 ml, overflow, partial emptying,
repeat and trace PASS (`Logs/phase-k3-physical.log`). Actual Play Mode production
Build/Stir/Shake prefabs PASS for pickup-before-liquid, release, 72 ml incoming
→ 60 ml retained, progressive source reduction, conservation each frame, empty
Jigger and tools returning (`Logs/phase-k3-playmode.log`). Programmatic inputs;
not human gameplay. Integrated existing campaign/bartending regressions PASS
(`Logs/phase-k3-regression.log`, exit 0). Dotnet PASS, 0 errors, 8 existing
warnings (`Logs/phase-k3-dotnet.log`). No prefab/scene/content/save edits.

## K4 — Working-container transfer

Stir/Shake production transfer buttons now orchestrate movement and tilt,
then progressive authoritative liquid transfer, stream, upright return and
only then the garnish/serve stage. Strain and PourFromContainer retain their
distinct trace actions and existing liquid-only semantics. The same pose path
serves Jigger transfers. Motion runs on the method root while the shelf panel
is hidden. Mixing Glass front/back now move together. Garnish binds the actual
destination visual instead of creating a second placeholder glass.

Dotnet PASS (0 errors, existing warnings). Focused Play Mode PASS
(`Logs/phase-k4-playmode.log`): source/destination conservation each frame,
retained source ice/no destination duplication, duplicate-click rejection,
correct final target and action trace for Strain and PourFromContainer. The
shared campaign test driver now waits for these real asynchronous transfers;
its assertions are unchanged. Full campaign replay follows K11. No serialized
scene/prefab changes. Tool timing/art remain HUMAN_TUNING_LATER/DEFERRED_ART.

## K5 — Stir tip orbit

The existing spoon now pivots at its bowl, travels around the actual Mixing
Glass interior in a common parent coordinate space and keeps an almost upright
shaft (8-degree first-pass lean). Orbit radius is clamped to vessel width;
the production prefab changes only radius 16 → 60 UI units. No rotation is
added to gameplay by visual smoothing. Rejected cursor jumps no longer move
the visual target. GentleStir receives the same shaft fix and Shake's gentle
stir now uses its actual serving vessel rather than a duplicate fallback.

Dotnet PASS, 0 errors. Focused real-prefab Editor tests verify a closed orbit,
tip inside the scaled vessel and a bounded shaft at every 30 degrees; GentleStir
still accumulates two input revolutions. Existing I15 and integrated campaign
regressions PASS (`Logs/phase-k5-regression.log`, exit 0). Visual feel is not
approved; runtime integrated technique validation follows K11.

## K6 — Held Shake motion

The entire existing Shaker transform (including attached liquid) follows L/R
translation, 24-unit vertical travel and a limited 12-degree rotation, with a
serialized movement curve and return duration. Motion pauses with reading/
settings. BPM, eight-note sequence, timing windows, misses and completion
remain unchanged. A transfer clicked during the final shake waits for its
return, preventing competing transform writers. Existing regression drivers
wait for that pending transfer.

Dotnet/Unity compile PASS, 0 errors. `Logs/phase-k6-playmode-r3.log` PASS for
all eight actual-time L/R inputs, directional translation, limited rotation,
exactly unchanged pre/post-shake contents and subsequent transfer. The first
run was interrupted by an agent edit causing domain reload; r2 exposed a
test's exact comparison to a decimal literal after float accumulation. R3
compares the actual pre-shake value to the actual post-shake value without
tolerance and passes. Neither earlier run is claimed as PASS. No rhythm
assertion was weakened. Ice visuals follow K10; no human feel claim.

## K7 — Direct Build and discrete pours

Build retains direct serving-glass preparation. D performs a bottle Dash and
S a bottle Splash; Stir/Shake Dash and post-Shake Splash share that physical
path. Each accepted discrete action invokes its existing state/trace API once
at the pour pose, holds a short readable stream, then returns the bottle.
It never sends a Dash/Splash through a Jigger. Configured TopUp guidance uses
the same hold-to-pour motion and records TopUp rather than changing it into
Pour. No current production recipe authors a TopUp step; K11 covers that
existing vocabulary with a focused fixture. Removed only Build's active
legacy liquid-area presentation at runtime so state-driven fill is visible.

Compile PASS, 0 errors. `Logs/phase-k7-playmode.log` PASS: no Dash before tool
positioning, exact existing Dash/Splash quantities, TopUp trace, and previous
measurement/transfer/Shake checks. All inputs are programmatic. Final visual
composition and broad recipe/campaign replay remain K11 work.

## K8 — Surface-following Float

Float liquid bands grow against a fixed 240 ml visual reference, rather than
normalizing every tiny pour to a full glass. The existing bands and boundary
judgments remain authoritative ordered-layer presentation. Their surface
anchor drives the spoon and low pour point; the generic mixed-liquid fill is
disabled only for that layered serving vessel. Direct bottle and optional
Jigger pours share that surface. Measured transfers report only accepted ml
to `BartendingLayeredDrinkState`; Shift/normal/Ctrl use the existing 15/30/60
ml/s later-layer rules. The source Jigger actually empties. Bottle availability
still has no modeled stock quantity. No density/fluid model was added.
Changing a bottle while holding Space ends the old layer before a new source
can contribute, preserving visible ingredient ownership and the real trace.

Compile PASS. Four-method Play Mode PASS (`Logs/phase-k8-playmode.log`), including
measured Float allocation matching serving volume and a Clean boundary from
actual gentle transfer. Earlier measurement, overflow, Dash/Splash/TopUp,
container transfer and Shake checks also pass. No serialized asset changes.

## K9/K10 — Strainer, ice and garnish placement

Strain now attaches a placeholder strainer to the source rim during the existing
progressive liquid-only transfer. PourFromContainer uses the same transfer
without a strainer. Both retain source ice under the existing preparation
contract; neither duplicates ice or changes recipe semantics.

Ice travels from the tray position into the selected vessel before authoritative
HasIce and AddIce evidence are applied. State-driven cubes remain children of
that vessel through Stir, Shake and transfer. Garnish travels from a pickup
offset before applying the existing state and trace. Rim, InGlass and Surface
placements use the actual serving vessel; Surface follows actual liquid height.
Serve and other physical actions wait for placement. Reading/settings suspend
placement and transfer motion. Measurement updates the existing method summary.

Graphical Play Mode exposed opaque liquid baked into the legacy vessel art and
an overlapping Jigger position. Runtime open vessel outlines now reveal actual
fill; original image assets are unchanged. Jigger and its label were moved clear
of the ingredient shelf. These are replaceable straight-walled placeholders,
not approved final glassware. No scene or prefab serialization was required.

Compile PASS (0 errors, 8 existing warnings). Graphical four-method checks PASS
in `Logs/phase-k10-rendered-r2.log`; captured pour, measurement and final garnish
images were inspected. They cover delayed ice/garnish state, retained attached
ice, actual surface placement, conservation and the earlier technique checks.
Final action-lock checks are recorded with K11 integrated validation below.

## K11 — Complete physical recipe validation

PASS: `Logs/phase-k11-campaign-playmode.log` runs a fresh New Game through
all 18 nights and 47 natural departures, then the ending. The shared driver
replaces only method keyboard polling with programmatic input. It waits for
real bottle positioning, pours over multiple frames, uses finite repeated
Jigger measurement for Stir recipes, performs the techniques, waits for actual
container/ice/garnish movement, and serves through the production result path.
It does not inject successful recipes, preparation, FIT, knowledge or campaign
outcomes. Stir inputs use the existing angular seam and visual follower;
Shake uses real-time L/R note windows. This is not human gameplay evidence.

| Completed recipe | Physical sequence exercised |
|---|---|
| Seolya | Bottle → Jigger → Mixing Glass, ice, Stir, Strain, serve/match/FIT |
| Dry Martini | Measurement, ice, Stir, Strain, required garnish and recognition |
| Manhattan | Measurement plus discrete Dash, ice, Stir, Strain, garnish |
| Negroni | Direct Build pours, serving ice, GentleStir, garnish |
| Manwol | Bottle at liquid surface, slow/aggressive Float, ordered growing layers, serve |
| Gin Fizz | Direct Shaker pours, ice, eight-note Shake, PourFromContainer, Splash |
| Hanky Panky | Measured Stir/Strain, required garnish failure and real retry/discovery |
| French 75 | Shake/Strain, subsequent serving-glass Pour, GentleStir and discovery |
| Last Word | Four actual ingredient pours, ice, Shake, Strain and discovery |

The same route verifies learned/supplied Recommendation alternatives, quiet
Prince failure and later independent recovery, weak-service ACT continuation,
Continue after night 2 and inside nights 6/10/15, interrupted dialogue/ending,
completed-ending Continue, seven memories, New Game reset and legacy handoff.
The runner restores the prior Editor save after completion. Campaign code and
content were not changed to satisfy this regression.

Additional executed evidence:

- `Logs/phase-k10-closure-playmode.log`: four actual method prefabs PASS, including
  action-lock rejection while garnish is held. Its earlier graphical r2 run
  supplies inspected 1280x720 views. Runtime transforms and per-frame transfer
  conservation are asserted, not merely serialized field values.
- `Logs/phase-k11-closure-regression.log`: final focused Editor checks plus the
  complete existing I10–I16/J1–J5 and campaign static set PASS, Unity exit 0.
  Varied Jigger capacities, mixed per-ingredient transfer into a bounded target,
  single final Dash evidence, retained ice, source exhaustion, spoon reversal,
  rejected cursor jumps and interruption of bottle pickup all pass.
- Final code review found a potential target snap in the first-pass
  return path. An interrupted upright approach now returns from its current
  position. This correction was present in the full campaign run above.
- `Logs/phase-k11-closure-dotnet.log`: C# compile PASS, 0 errors, 8 existing
  warnings (assembly-reference conflict and J4 reference-comparison warnings).
- Focused physical fixtures cover measured Float, all three garnish placements,
  overflow, direct Dash/Splash and TopUp. TopUp has no authored production recipe;
  this is fixture coverage, not a normal-play TopUp claim.
- `Logs/phase-k11-audio-playmode.log`: existing audio/reading Play Mode PASS.
  One scene listener, accepted pour start/stop, volume/mute, preference isolation,
  additive handoff/unload and Guide/settings pause preserving Shake timing all
  pass. This does not approve the audible mix or add pickup/spill sounds.

No scene/recipe/campaign/persistence serialization changed. The single Stir
prefab value was inspected; generated font whitespace is excluded. No human
feel, aesthetic or final game-readiness approval is claimed.

## K12 — Owner tuning handoff

### Components and authority

| Component | Responsibility |
|---|---|
| `BartendingContainerState` / `BartendingPreparationState` | Sole runtime ml, ingredient, ice and garnish state; finite Jigger uses the same class. Progressive transfer lives here. |
| `IngredientShelfController` | Select source/target, admit actions when tools are ready, orchestrate coroutines, record actual accepted destination ml in the existing trace. |
| `BartendingPhysicalPresentation` (new) | Replaceable bottle grip/neck, vessel transforms, stream, spill, fill, ice and strainer views. Reads state; owns no composition, supply or score. |
| Existing four method controllers | Their existing technique/stage rules, now with physical input readiness and asynchronous completion. |
| `ServingGlassGentleStirInteraction` | Existing angular tracker with an orbiting tip and limited shaft lean. |
| `ServingGlassGarnishController` | Existing garnish choices and final trace, now applied after visible pickup/placement. |
| `BartendingLayeredDrinkState` | Existing ordered Float layers and boundary judgment; receives only actual accepted transfer. |
| `RecipeMatchService`, preparation evaluator and RequestEvaluation | Unchanged recognition, preparation and guest FIT responsibilities. |
| Existing data/knowledge/availability | Unchanged recipe truth, stable IDs, catalog membership, learned facts and physical supply availability. |

No second liquid authority, recipe authority, inventory economy or save format
was introduced. Runtime `Jigger` was appended after existing container enum
members. Campaign scheduling, ACT dialogue, ending, memories and persistence
were not edited. The only production prefab change is Stir orbit radius.

### Controls and physical sequence

- Select an available ingredient; hold Space. Pickup/alignment/tilt precede
  accepted liquid. Release stops flow, returns upright, then releases the tool.
- M toggles optional measurement while the Jigger is empty. Space fills it;
  release the bottle, then T transfers the measured contents. Repeat for larger
  amounts. Source/target and technique completion are locked while it is full
  or in transfer. Capacity is 60 ml; excess is visibly spilled and lost.
- Direct Build remains available. D is Dash, S is Splash. Existing configured
  TopUp uses its own preparation action. Neither Dash nor Splash becomes Measure.
- I places ice. Existing Stir mouse input, Shake L/R rhythm and Float Shift/
  normal/Ctrl flow rules remain. Existing buttons perform transfer and serve.
- Garnish selection retains existing controls. The garnish must arrive before
  serving; Rim/InGlass/Surface refer to the actual serving vessel and liquid.

### First-pass tuning values — HUMAN_TUNING_LATER

Distances are local UI units; inspect at the intended canvas scale. Serialized
fields can be adjusted on the active component during Play Mode. Presentation
and garnish components are currently added at runtime: to persist Inspector
overrides, add that same component to the method prefab root before play; the
initializer reuses it. No separate configuration framework is necessary.

| Owner / fields | First-pass default | Tune for |
|---|---|---|
| Physical: `approachSeconds`, `tiltSeconds`, `returnSeconds` | .25 / .18 / .25 s | Bottle speed, pickup and release readability |
| Physical: `bottleTiltDegrees`, `bottleHome` | 115 degrees; (-250, 140) | Held bottle pose and shelf return |
| Physical: `pourHeight`, `streamWidth` | 32 / 4 | Neck clearance and readable liquid stream |
| Physical: `containerMoveSeconds`, `containerTiltDegrees` | .3 s / 105 degrees | Jigger/working-container transfer timing |
| Shelf: `jiggerCapacityMl` | 60 ml | Measurement readability; retain finite capacity and repeated measures |
| Shelf: `jiggerTransferMlPerSecond`, `containerTransferMlPerSecond` | 90 / 120 ml/s | Source reduction and destination growth readability |
| Physical: `spillSeconds`; shelf: `doseStreamSeconds` | .3 / .12 s | Overflow and discrete pour visibility |
| Physical: `displayVolumeMl` | 240 ml | Display reference for unbounded working/serving vessels; not gameplay capacity |
| Stir: `spoonOrbitRadius`, `spoonOrbitYScale` | Prefab 60; .65 | Tip orbit shape; radius is clamped inside vessel width |
| Stir: `spoonBaseAngleDeg`, `spoonShaftTiltDegrees`, `spoonVisualSmoothingSeconds` | 90 / 8 degrees; .08 s | Shaft naturalness and visual following; never add gameplay angle |
| GentleStir constructor | Radius 18, lean 8 degrees, smoothing .08 s | Small serving-glass orbit |
| Shake: `shakeMoveX`, `shakeMoveY`, `shakeRotationDegrees` | 80 / 24 / 12 degrees | Amplitude, direction and impact |
| Shake: `shakeDuration`, `returnDuration`, `shakeMovementCurve` | .08 / .08 s; EaseInOut | Held movement/return; preserve rhythm timing windows |
| Float: `visualVolumeMl`, `spoonOffsetY`; physical: `surfacePourHeight` | 240 ml / 6 / 12 | Growing layers, surface approach and spoon clearance |
| Physical: `icePlacementSeconds` | .35 s | Ice placement timing |
| Garnish: `placementSeconds`, `pickupOffset` | .35 s; (-180, 110) | Pickup/placement timing and path |

Existing continuous pour rate is 30 ml/s. Float retains first-layer 30 and
later-layer 15/30/60 ml/s. These values participate in existing gameplay; visual
tuning must not silently change recipe amounts, technique judgment or timing
windows. Spoon speed follows actual angular input, with visual smoothing only.
Garnish shape/size/colors and relative placement offsets are small replaceable
view helpers; tool sprite dimensions/neck offsets are in the presentation class.

### Known first-pass limitations

- Open UI rectangles, a bottle silhouette, straight streams, three ice cubes,
  garnish markers and a strainer strip are placeholders. Final pixel assets,
  hand poses, material/color differentiation and layout composition remain art work.
- Liquid fill remains attached to the vessel during tilt. It is a state-driven
  stylized indicator, with no gravity surface, splash physics, dilution or melting.
- Bottle supply is availability only. There is no finite stock to decrement and
  no waste counter. Overflow never reaches the recipe or adds a progression cost.
- Working/serving containers retain existing unbounded gameplay capacity; their
  visual fill saturates at the display reference. Only Jigger capacity is new.
- Strain and PourFromContainer both retain source ice under current recipe
  semantics. PourFromContainer does not introduce an unauthored dirty-dump rule.
- Garnish replacement preserves the existing single-garnish correction rule.
  Individual cube counts and multiple decorations are not modeled.
- No production recipe currently authors TopUp. Its existing action vocabulary
  is covered by a focused physical fixture, not a newly invented campaign drink.
- No Phase K player executable has been packaged. The existing Windows build
  predates Phase K. Editor automation is not a physical-keyboard playthrough.

### Human tuning checklist

- [ ] Bottle speed, grip/neck relationship, tilt and return feel.
- [ ] Pour stream readability and bottle/target clearance.
- [ ] Jigger fill and numeric reading, repeated measurement controls.
- [ ] Overflow visibility and clear distinction from delivered liquid.
- [ ] Stir orbit naturalness, direction reversal, speed and smoothing.
- [ ] Shake amplitude, visual impact and attached contents.
- [ ] Float surface approach, layer growth and slow-pour readability.
- [ ] Transfer movement, stream and source/destination timing.
- [ ] Ice readability and garnish positions for all three placements.
- [ ] Replace runtime placeholders with final pixel assets; review at target resolutions.
- [ ] Listen to the existing mix; decide whether pickup/spill/placement sounds are needed.
- [ ] Play complete recipes and the campaign with physical controls; judge feel,
  readability, pacing and emotional experience. Automated results do not approve these.

## Final classification

| Classification | Scope |
|---|---|
| PASS | K2–K10 bounded physical interactions; state/trace ownership, finite measurement/overflow, source/destination conservation, Stir reversal, Shake rhythm, layered Float, ice/garnish, recipe recognition and FIT. Compile, focused Editor/graphical Play Mode, audio pause and full 18-night/47-encounter campaign regressions pass. |
| VALIDATION_DEBT | Human physical mouse/keyboard play, complete standalone Phase K playthrough, target-device performance and alternate-resolution/hit-target review. Existing Windows executable predates Phase K. No Unity licensing blocker remains. |
| HUMAN_TUNING_LATER | All motion/spacing/readability values in the checklist; bartending feel and campaign pacing with the new interaction durations. |
| DEFERRED_ART | Final pixel bottle/Jigger/vessel/spoon/ice/garnish/strainer sprites, hands, liquid appearance and final composition. Current views are simple replaceable placeholders. |
| DEFERRED_AUDIO | Owner mix review and any new pickup, spill, transfer or placement accents. Existing audio behavior remains functional. |
| FUTURE_EXPANSION | Finite bottle inventory, additional glass/container capacities, more authored cocktail content and additional tool vocabulary only if separately scoped. |
| OUT_OF_SCOPE | Campaign/narrative rewrites, replacement recipe/knowledge/scoring authorities, destructive save migration, CFD, dynamic rigid-body fluid/breakage simulation. |

No known objective defect remains in the implemented bounded Phase K contracts
after the executed checks. This is a first-pass technical handoff. The owner
retains final approval of art, audio, readability, physical feel and finished-game
readiness. Logs/captures are local ignored validation artifacts, not committed
production assets. No push or public publication was performed.
