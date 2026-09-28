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
