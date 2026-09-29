# Phase M — implementation and review

## Baseline and preservation

Actual branch `refactor/project-rebuild`; starting HEAD
`e0e67ed68df60b683462477fdcce87167ed35a6e`. Initial worktree has 59 modified
paths: config, 56 sprite metas and two UserSettings files. The 56 meta diffs
are empty with `--ignore-space-at-eol`; this is current evidence, not an
assumption from Phase L. Per-file SHA256 baseline is in ignored
`Temp/PhaseM/baseline.json`. Protected config hash:
`ADC64A6DF66005DBC3F384F7889BB15596B28CD0B10DFF287C11F4AB01E55C03`.
No config contents were read or printed. No branch switch/reset/cleanup.

Root AGENTS.md is the only discovered repository instruction file. All five
owner reference images were inspected and retained. Unity 6000.3.7f1 was
connected with clean BarScene open in Edit Mode. The sandbox process launcher
initially failed with `helper_unknown_error: setup refresh had errors`;
approved local execution restored read access. This is not a Unity license error.

## M0 screen map — current implementation before edits

All UI is uGUI/TMP. BarScene Canvas is 1280×720, match 0.5. Runtime entry and
Settings use 1920×1080; notification Canvas retains legacy 800×600 settings.
BartendingScene contains the scene-scoped runner and instantiates one of four
production method prefabs. It has no independent EventSystem. Bar owns input routing.

| Screen / trigger | Owner and creation | Information / actions / exit | Timing and baseline finding |
|---|---|---|---|
| Entry / scene without selected run | GameManager, runtime Canvas | progress helpers; New Game, Continue, sound settings; Bar reload | FUNCTIONAL_DEFECT: meaningful progress reset has no confirmation. READABILITY_DEFECT: small 1920-based labels at 720; near-black world |
| Idle / guest seated | CustomerController, InteractionUIController, serialized prompt | seat/guest anchor; click to dialogue | VISUAL_INCONSISTENCY: oversized corner icons; preserve accepted room |
| Dialogue / prompt | InteractionUIController, serialized lower panel | DialogueData and consumed fact events; Next or Brew | customer patience pause; no typewriter. READABILITY_DEFECT: broad button and unstructured band; speaker is generic |
| Choice / Prince gate | same controller and two existing buttons | authored choice labels; persisted choice reply | preserve meaning/order. Repeated activation can cross reading beats |
| Request / reading complete | same panel, active customer | existing hint; Brew | Exact routes directly; Recommendations show method selector |
| Method selection / Recommendation Brew | same controller, serialized BrewPanel | local route selection; four buttons, confirm/back | VISUAL_INCONSISTENCY: English-only techniques and color-only state; no concise explanation |
| Build/Stir/Shake/Float | method prefab, method script + IngredientShelfController | actual containers, action trace, timing; shelf/actions/Guide/serve | READABILITY_DEFECT: white prototype buttons, raw container labels and dense English help. Preserve physical space/quantities |
| Recipe Guide / local button | RecipeGuidePanelController, serialized per-method UI | RecipeGuideContext/knowledge formatter; method-local list, details, close | local timeScale pause. NAVIGATION_CONFUSION: Escape cannot close Guide; separate pause restoration |
| Codex / corner or result button | CocktailCodexPanelController, serialized panel/cards | catalog membership + knowledge formatter; grid/detail/back/close | unpaused. Partial filtering is already present. READABILITY_DEFECT: translucent competing text; white buttons |
| Service feedback / resolved service | ResultToastController, serialized canvas | GuestServiceResult formatter; actual served drawing; timed exit | reaction then toast then delayed unlock. Preserve semantic axes and sequence |
| Discovery / existing notification seam | RecipeUnlockPopupController | already-authorized discovery; timed dismissal | no new knowledge authority; inspect overlap during final service regression |
| Night summary / day end | BusinessDayManager → serialized DayResultPanelController, runtime fallback | night-local recipe delta, next night, Codex | result remains open beneath Codex already; preserve this behavior |
| Settings / icon or Escape | DemoAudioController, runtime Canvas | current persisted master/music/effects/mute; close | timeScale pause, refuses Guide nesting; values lack numeric readout |
| Ending / night 18 departure or Continue | same DayResultPanelController | authored ending lines, coda, entry/Codex | existing reached/read persistence and no night 19; prevent double-reading activation |

## Baseline evidence

`SURLHWAPhaseMUIValidation.RunBaseline()` uses the actual production scene,
prefabs and controllers. It seeds later-night/ending checkpoints solely to open
screens. These are **fixtures**, not proof of normal-play reachability. It backs
up all existing game progress and audio keys plus both recipe payloads and
restores/verifies them on every Play Mode exit, including interruption.

Native captures: `Logs/Review/phase-m-baseline-*-1280x720.png` and
`*-1920x1080.png`. Matching `.txt` files retain live labels/control bounds and
topmost center raycast observations. Callback invocation is level B; raycast
observations alone do not prove a physical mouse click. Offscreen capture
projects/restores existing canvases through the existing screenshot helper.

Baseline fixture covers entry, Settings, idle, dialogue, request, Partial Codex,
all four routes/Guides/method selections, Prince choice, summary→Codex→summary,
ending and coda. Known Codex and resolved service/discovery need final focused
coverage; no fabricated baseline evidence is claimed for them.

## Implementation and final evidence

Milestone records will be appended after each bounded implementation and validation.
No final Phase M PASS is implied by the audit or design contract.

### M2–M3: input foundations and representative method selector

Added `SurlhwaUITheme` and a bounded `SurlhwaModalSurface`: visible shields,
topmost Escape, focus restoration, underlying Selectable gates, shared nested
reading pause, and release-before-next-action keyboard boundaries. Guide and
Settings retain technique read semantics, including the unscaled Shake clock.
Stir/GentleStir rebase their angle after reading; continuous pours stop at an
overlay boundary without replacing container or preparation truth.

Pointer audit found a real baseline defect: only the guest prompt's visual
child followed the guest; its invisible parent button stayed at screen center.
The visible prompt and its hit rectangle now follow together. Method selection
uses a compact Korean-first panel, explicit selected check, short technique
explanation and separate confirm/back actions. Request routing is unchanged.

Review iterations caught inactive nested Canvas initialization resetting its
sort override, and a layout inherited from the old method button parent.
Overrides are applied on activation and method controls have one panel parent.
These were corrected before committing. The test waits one rendered frame
after a newly opened surface, as real successive pointer input does.

Evidence: `SURLHWAPhaseMUIValidation.Run()` exercises top raycast plus
programmatic pointer down/up/click through all four method choices, cancel /
reopen, Guides, and nested Guide → Settings → Guide pause restoration. Later
night/summary/ending screens remain seeded fixtures. Both-resolution captures
are `Logs/Review/phase-m-review-*`. The method screen at 1280×720 was inspected
at native size. Compile: 0 errors, 8 existing warnings. Full campaign, physical
held-key/mouse, final restyled screens and standalone coverage remain pending.

### M4 / M6: reading band and knowledge surfaces

The lower dialogue band uses the authored speaker identity, 22-unit dialogue,
wrapped 54-unit action rows, and a shared 0.24-second pointer boundary between
reading beats. It does not change narrative text, reveal events or choice data.
The existing book/settings artwork uses aligned 64-unit icons and hit areas.

Codex now has an opaque reading surface, one wired close action for both pages,
five-column scrolling cards, explicit Unknown/Partial/Known presentation, and
separate artwork / description / method / scrollable preparation areas. The
old detail-page close button had no listener and is retired. Card silhouettes
and all cocktail artwork remain the existing assets. Guide retains its own
method-local list, filtered detail and reading pause; its selection has a check
mark and its close action supports Escape. No knowledge or supply data changed.

`Run()` completed without runtime errors after this implementation, including
Unknown/Partial/Known captures and all four route/Guide pointer paths. Native
720p dialogue, card grid, Known detail, and method renders were inspected.
The harness now fails on any runtime error, including errors outside its own
coroutine. Earlier development iterations with layout and CanvasGroup errors
are not PASS evidence. Compile remains 0 errors / 8 existing warnings; integrated
campaign static contracts passed. Full normal-play regression remains M10 work.

### M5: work surface and contextual instructions

`SurlhwaBartendingPresentation` arranges existing hint/progress controls above
the tools and displays the request's existing public title. The coordinator's
existing audio observer supplies a local Settings button; it has no new settings
or gameplay authority. Shelf rows preserve actual ml and oz values on separate
lines, increase text/row height, and mark the selected ingredient. Container,
measurement, rhythm, flow and garnish labels are Korean-first. Guide remains
the only recipe facts source in the workspace. No tool transforms, liquid
colors, timings, matcher, FIT or preparation rules changed.

Phase K Play Mode passed all four routes: real tool transforms, measured finite
capacity / overflow, partial transfer, ice, spoon motion, Shake motion, garnish
placement and actual served appearance. These are programmatic input seams,
not human mouse/keyboard tests. A rendering iteration caught truncated ingredient
names caused by the old row's 30-unit LayoutElement; its height is now 50 within
a 56-unit row. The corrected names and ml/oz rows were inspected in the physical
fixture capture. Production workspace captures use the Phase M probe.

Static Phase K + campaign/knowledge/request/preparation regressions pass when
started from an empty clean Editor scene. An earlier repeated invocation reused
a scene modified in memory by a legacy I16 validator and failed its five-guest
fixture assertion. Running the unchanged validator with isolated scene setup
passed; no authored schedule or assertion was changed. Compile: 0 errors,
8 existing warnings. Held native input remains physical-input validation debt.

### M7–M8: entry, settings, result and ending navigation

Entry keeps the accepted bar visible behind its menu. Continue names the saved
night or completed coda; an absent save is visibly disabled. New Game asks once
before erasing the run observed when entry opened. Fresh initialization of the
default recipe does not turn the first New Game click into a reset confirmation.
Cancellation preserves the run. Settings shares 1280×720 scaling, numeric
volume values and correctly bounded slider handles. Values/persistence remain
with DemoAudioController.

Night summary and ending share a readable panel and primary navigation. The
result remains underneath Codex. Reading controls reject rapid pointer repeats;
scene-return actions have a local duplicate-transition guard. BusinessDayManager
now waits for GameManager's accepted gameplay session before opening a terminal
night/ending: the previous Start path opened a coda underneath the entry menu.
The day/ending owner, save keys, authored ending and no-night-19 rules are intact.
Service toast and discovery popup use the shared colors, larger reading areas
and nonblocking decoration; their existing timing/queue is unchanged.

Compile: 0 errors / 8 existing warnings. Full Phase M pointer fixtures passed
before the terminal-entry correction; the affected `RunNavigation()` then passed
ending → coda → entry → cancel reset → confirm reset, with no runtime errors.
The existing Campaign Ending fixture also passed interruption/replay, every
line fitting, completed Continue and no extra night. Native 720p entry/settings/
summary/confirmation and 1080p ending captures were inspected. Updated navigation
captures are `phase-m-navigation-*`; final M10 supersedes these with the complete
review set. Actual service/discovery sequencing and normal-play captures remain
part of the full campaign run, not inferred from layout changes.
