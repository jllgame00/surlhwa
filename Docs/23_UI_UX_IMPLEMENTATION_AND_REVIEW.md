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

The following records describe each bounded implementation and its evidence at
that point. The final source/handoff record below supersedes earlier pending
validation status without rewriting the historical findings.

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

### M9–M10: integrated presentation and review tooling

Service technique labels now translate existing grades without changing grade
values, recipe identity, preparation compliance or FIT. Guide's close action
was widened after a native 1080p capture exposed an unwanted line wrap. No
scene, prefab, art importer, font or authored content change was needed.

The complete Phase M probe passed after these runtime changes. It tests four
method choices/cancel/reopen, actual top raycasts and pointer events, an immediate
second reading click being rejected, three repeated Settings cycles, shielded
underlying Guide controls, pointer-operated volume persistence, restoration of
an original 0.5 time scale, and final absence of modal/pause locks. It restores
and verifies the owner's progress/audio snapshot. This is PROGRAMMATIC INPUT,
not native input-module or human mouse evidence.

Audio/reading Play Mode passed: real imported sources, persisted sliders/mute,
reset isolation, Guide and Settings preserving the unscaled Shake note timeline,
all eight notes hit after resume, service/unload and one listener throughout.
The existing audio harness now restores on interrupted Play Mode exit and only
exits the Editor in batch mode. Static service feedback and Phase K/campaign
integrated contracts pass; C# compile has 0 errors / 8 existing warnings.

The established Windows build pipeline now offers `RunPhaseM()`: an explicitly
versioned output directory, exact Git revision, path-only dirty inventory, target,
Unity version/time/result/warnings, current review images and these documents.
It refuses dirty implementation/scenes, preserves the Editor scene setup, and
isolates legacy static validators. Build completion and final full campaign
evidence are recorded below after execution, not inferred from this tooling.

## Final coverage boundaries

Candidate `c26a9dd` passed compile, the complete UI probe, audio pause and
physical regression. Its first full campaign run was deliberately interrupted
after the visual corrections described below were identified. It is not final
campaign PASS evidence. Final source/build identity is recorded below; subsequent
documentation-only commits do not change executable behavior.

### Render-driven corrections before final campaign

Native Build/Float captures exposed an old 36-unit request title still visible
under the new request band. The redundant label is now hidden in presentation;
the public request is shown once in a 64-unit area. Technique result labels have
a separate 20-unit row below measurements, and reuse the service formatter's
Korean grade labels. Shake's momentary beat words are also Korean; judgments,
timing and scores are unchanged.

The first real discovery capture exposed the serialized yellow `UnlockPanel`
below the animated root: styling the root alone had not changed the background.
The actual background now uses the opaque shared surface; the popup sits below
the corner settings icon. Queue/acknowledgment/knowledge timing is unchanged.
The interrupted campaign restored the exact progress/audio/knowledge hash.
The complete UI probe passed the Build/Float correction. The physical fixture
now applies the same presentation entry point and retains additional, explicitly
labelled `phase-m-physical-*-finished` captures at both resolutions. Final
campaign and physical regression are rerun after the corrections.

| Required scenario | Evidence and boundary |
|---|---|
| Fresh entry → New Game → first guest | Phase M pointer/raycast path; absent-save Continue is disabled; full campaign uses callbacks |
| Early and extended Continue | UI fixture nights 2/5/18; campaign exercises persisted departure checkpoints and interrupted encounter restart |
| Dialogue → choice → request → method | actual authored production controls; pointer reading boundary; current request routing also covered by integrated request validators |
| Four method selections, back/reopen/confirm | center raycasts + down/up/click; explicit check mark; each reaches exactly one active method workspace |
| Guide/read/resume | all four actual route surfaces; nested Settings retains Guide pause; audio regression proves unscaled Shake notes resume without misses |
| Settings/resume | three pointer-operated cycles, actual volume persistence, underlying Guide button shielded, prior 0.5 time scale restored; native held-input edge cases remain debt |
| Complete Build/Stir/Shake/Float | Phase K uses actual physical input seams and containers; campaign uses reachable authored recipes; Float is the existing controlled physical fixture |
| Partial, retry, successful learning | real filtered Partial/Known views; existing static knowledge contracts; normal campaign discovery/retry regression |
| Reaction → feedback → discovery → departure | existing queue retained; campaign observer captures actual first service/discovery and rejects overlap; 47-departure run checks completion continuity |
| Summary → Codex → close | production panel fixture retains same summary; full campaign checks nightly lifecycle; the fixture is not a claim that a summary normally overlaps dialogue |
| Ending interruption/coda/entry/reset | existing ending regression plus programmatic pointer navigation; cancel preserves progress, confirm resets, entry has no hidden ending panel |
| Repeated surfaces/scene returns | repeated settings, four additive route returns and multiple title reloads; no leftover modal/pause lock or runtime errors in complete UI probe |

### Evidence levels

- **A — compile/static:** C# and unchanged-authority regression validators.
- **B — callbacks/input seams:** full campaign, physical mechanics, audio pause,
  Codex detail selection and some fixture setup. A callback is not a hit-area test.
- **C — pointer/raycast:** Phase M constructs PointerEventData, checks the actual
  EventSystem top raycast, then dispatches pointer down/up/click to the reached
  control. Includes the guest prompt, dialogue/choice, four method controls,
  Guide close/open, Settings close/volume and ending/reset navigation. This
  exercises uGUI routing. The additional input-module probe below exercises
  the existing module with queued virtual mouse states, still not OS input.
- **D — render:** production scene/prefab/controller output at both required
  resolutions, using the existing offscreen Canvas projection helper. Review
  fixtures are explicitly seeded; normal-service captures come from the campaign.
- **E — standalone:** see build/smoke record below. Editor tests do not substitute
  for a complete standalone navigation or campaign run.
- **F — human:** not performed by the agent. Physical mouse/keyboard feel,
  held-input opening/releasing/closing overlays and full human playthrough remain
  VALIDATION_DEBT, with visual/pacing preference HUMAN_UI_TUNING_LATER.

No native mouse test, accessibility certification, controller support or untested
aspect ratio is claimed. Masked/offscreen cards and wheel scrolling are covered
by the input-module probe; native pointer dragging across panel boundaries still
requires physical-input review. Current desktop project defaults are 1920×1080, 16:9;
1280×720 is a reviewed equivalent-ratio presentation, not a new display framework.

### Fonts, assets and review files

TMP's existing LiberationSans SDF uses `KoreanFallback.asset` (dynamic Noto Sans
KR) from the existing project. No new font download or dependency. Native captures
show Korean dialogue, punctuation, choices, ingredient labels, ml/oz, recipe
instructions and ending without observed missing glyphs. This is sampled rendered
coverage, not a claim that every possible Unicode character is supported.

Retained art: book/settings icons, all cocktail/glass illustrations and colors,
Shaker, 31 physical pixel assets, modular bar, chibi special cast and charcoal
ordinary guests. No new UI raster artwork was needed; panels/edges are deterministic
uGUI graphics with live text. Owner reference inputs were unchanged.

Individual captures: `Logs/Review/phase-m-review-*-1280x720.png` and
`*-1920x1080.png` (entry, idle, dialogue, choice, request, selected methods,
four workspaces/Guides, Partial/Known Codex, summary/return, settings, ending,
coda and reset confirmation). Matching `.txt` files contain bounds/raycast/text
observations. `phase-m-normal-service-*` and `phase-m-normal-discovery-*` are
automated normal-campaign captures. Baseline and earlier navigation captures
retain their distinct prefixes. Native images were inspected separately from
the contact sheet; thumbnails alone were not used to approve text readability.

### Existing input-module verification

Current production UI uses `InputSystemUIInputModule`, while physical technique
scripts poll legacy `Input`. The legacy InputManager's duplicate Space Submit
entry is not the active UI Submit binding. The active `*/{Submit}` resolves to
`/Keyboard/enter`; Space has no Submit usage. No input settings or stack changed.
A second campaign attempt was interrupted for this read-only check, not because
of an observed campaign failure; progress/knowledge/audio hashes restored exactly.

`SURLHWAPhaseMUIValidation.RunInputModule()` adds a temporary virtual Mouse,
queues position/button states and lets the actual module drive the UI. With
the existing Game view focused, it passed entry Settings/close, New Game,
guest prompt, authored dialogue, bartending entry, Guide pause/resume and local
Settings/close. No callback or ExecuteEvents call drives this probe. The virtual
device is removed on exit; progress and sound preferences are restored.
This is stronger programmatic C evidence, not a human mouse test or legacy held-key
test. The first unfocused attempt failed to open Settings under the project's
`PointersAndKeyboardsRespectGameViewFocus` policy; no production setting was
changed to force a pass. Keep the Editor/Game view focused when rerunning.

### Notification scaling correction

The 1080p normal capture showed NotificationCanvas still using ConstantPixelSize
at its legacy 800×600 reference, while the bar used 1280×720 screen scaling.
This made notification text smaller and reduced its separation from the corner
icon at 1080p. Both notification controllers now request the shared reading
Canvas scale (1280×720, match 0.5). This is a runtime presentation change only;
no serialized Canvas, world scale, queue or timing changed. A third in-progress
campaign attempt was interrupted to apply this correction, with snapshot restore.

The new `RunNotificationLayout()` fixture passed and produced separate
`phase-m-notification-layout-*-fixture` images at both sizes. It invokes visual
presentation with explicit fixture content, without granting knowledge or
manufacturing a gameplay result. Native 1080p discovery/service images were
inspected for text contrast, bounds and icon separation. Compile and the full
static integration passed after this correction. Earlier interrupted campaigns
are not counted as complete runs; final normal-play evidence follows below.

### Final campaign and focus regression

At `4dac780f45f7337f56139c1f9e0516a7f687b104`, the full 18-night/47-encounter
campaign passed on 2026-09-29 at 08:43:12 UTC. All 47 natural departures,
discovery failure/retry, alternatives, weak-service continuation, checkpoint and
ending resume, seven memories, terminal Continue and reset were exercised.
The owner's progress/knowledge/audio hash exactly matched the pre-test hash.
Normal-service and discovery captures were refreshed from this complete run.

The subsequent expanded virtual input-module probe exposed a real focus defect:
after opening a scrolled Codex card and pressing Back, EventSystem retained the
now-hidden Back button. The same probe first verified that the final card was
masked, then used a real module wheel event to expose and open it. The modal
utility now repairs null, inactive or non-interactable focus and refuses to
restore unusable parent focus. This is transient UI ownership only. The failing
assertion is retained; final regression results below include this correction.

The corrected module probe passed, including an attempted click on disabled
fresh-entry Continue (no session start), masked-card rejection, wheel scrolling,
detail/Back focus recovery and the full entry-to-workspace overlay path.
Compilation: zero errors, eight existing warnings (MSB3277 and seven CS0252).

## Final source and handoff record

Final runtime source is `ba4346f14a1aa9dfd73f5083399ca1230f0bd1fe` on
`refactor/project-rebuild`. Later documentation-only work does not change the
executable source. Focused commits since the verified starting HEAD:

| Commit | Scope |
|---|---|
| `6a731b4` | current-screen audit and design contracts |
| `47d7c0c` | shared theme, modal/input boundaries, guest prompt and method selector |
| `5140a3c` | dialogue and knowledge reading surfaces |
| `64bbd95` | actual bartending state presentation |
| `bd0c0dd` | entry/reset, settings, results and ending navigation |
| `c26a9dd` | integrated readability and review harness |
| `1b7ed5c` | rendered workspace overlap and discovery contrast corrections |
| `1182d37` | existing input-module virtual pointer verification |
| `4dac780` | notification Canvas scaling |
| `ba4346f` | usable focus restoration and masked/scroll/disabled module regression |

The complete UI probe passed again on `ba4346f` at 08:53:42 UTC, refreshing
individual 720p/1080p production UI captures. The review board is
`Logs/Review/phase-m-review-board.png`; F means checkpoint/setup fixture and N
means the automated normal campaign. The board is a deterministic Pillow
composition outside runtime assets, not new game artwork or image-generation output.

Final-source Phase K Play Mode passed all four routes, including finite Jigger
measurement/overflow, source reduction/target growth, actual tool transforms,
technique results, Float layers and prepared-glass visuals. Updated controlled
physical-fixture captures use `phase-m-physical-*-finished-*`. These fixtures
exercise direct transfers and are not normal-play UI sequence evidence.
The audio/reading-pause Play Mode regression also passed: one active listener,
volume/mute persistence isolation, pour audio, Guide/Settings preserving all eight
Shake notes, resume and unload. Progress/knowledge/audio hashes restored exactly.

### Final validation matrix

The final 18-night/47-encounter rerun on `ba4346f` completed **PASS at
2026-09-29 09:19:38 UTC**. It included all 47 natural departures, 16 extended
special visits, actual discovery/retry/alternatives, weak-service continuation,
checkpoint/mid-dialogue/ending Continue, seven memories, terminal coda, reset and
legacy handoff. No runtime errors were logged during this run. Owner progress,
both knowledge payloads and audio preferences matched their pre-run hash exactly.
Evidence: ignored `Temp/PhaseM/campaign-ba4346f-evidence.json`; the previous complete
`4dac780` run is retained separately. Only the normal Prince route was rerun as
the full campaign in Phase M; this is not a claim of two full branch playthroughs.

| Level | Final coverage and result |
|---|---|
| A — compile/static | 0 errors, 8 existing warnings; physical static regression including campaign/knowledge/request/preparation/FIT integration and service-feedback assertions PASS |
| B — callbacks/input seams | final-source complete 18-night/47-encounter campaign, all four Phase K physical routes and audio/reading-pause regression PASS; not pointer or human proof |
| C — pointer/raycast/module | complete UI review checks actual top raycasts plus pointer dispatch; virtual Mouse through existing InputSystemUIInputModule checks disabled Continue, Settings shielding, masked Codex card, wheel, detail/Back focus, dialogue, Guide and workspace Settings; PROGRAMMATIC INPUT |
| D — rendered inspection | individual production UI captures at 1280×720 and 1920×1080 inspected; seeded fixtures identified; actual normal service/discovery captures from the final campaign; no missing glyphs or clipping observed in reviewed samples |
| E — standalone | Windows build and graphics-enabled 15-second startup smoke PASS; standalone UI navigation/full playthrough not exercised |
| F — human | not performed; native held-input/drag-release paths, physical feel and full human campaign remain open |

The input-module probe passed once more on the committed final source at
**09:20:33 UTC**, after the build and full campaign. Its save/knowledge/audio
snapshot restored exactly. No further runtime-affecting edits followed.

### Windows review package

- Source: `ba4346f14a1aa9dfd73f5083399ca1230f0bd1fe`; no dirty implementation
  files. Only baseline excluded paths and in-progress handoff documentation were
  dirty; their path-only inventory is in `BUILD_INFO.txt`.
- Existing `SURLHWAWindowsBuild.RunPhaseM()` pipeline, Unity **6000.3.7f1**,
  **StandaloneWindows64**, normal non-development build, two production scenes.
- **Succeeded**, 2026-09-29 **08:57:17 UTC** (17:57:17 KST), **37.678 seconds**,
  report size **210,556,945 bytes**. Output:
  `Builds/Windows-PhaseM-ba4346f/SURLHWA.exe`. Keep the entire folder together.
- One build warning: no RuntimePipelineConfig asset, so the Unity tooling
  pipeline is disabled in Player builds. This is the existing configuration;
  no runtime remote-control dependency was enabled for validation.
- Executable SHA256:
  `4BF762016829B0C975960CF300AAC5A5BB1F84D25745F2AA8F4A508F9AF5243F`.
  Game assembly SHA256:
  `D703707B7119DEAA9DDCC53BF7014260F9E28082BDE8A22C1E34961B4957DE43`.
- Startup smoke at **08:57:51 UTC**: new executable, hidden batch mode with
  graphics enabled at 1280×720; Direct3D11/RTX3070 initialized, game assembly
  loaded, BarScene SeatManager/OrderManager initialized, process alive after
  15 seconds, no logged exception. The owned process was then stopped.
- Standalone `HKCU/Software/DefaultCompany/surlhwa` values were snapshotted and
  exactly restored, including original value types. No other running game was
  touched. Evidence: ignored `Temp/PhaseM/standalone-smoke.json` and
  `standalone-player.log`; the private preference snapshot is not distributed.
- **E coverage is startup only.** No supported player UI automation connection
  was available with the existing build configuration. Standalone navigation,
  native input and a complete standalone playthrough remain VALIDATION_DEBT.
  Editor virtual input-module evidence is listed separately above.
- Package `Review/` retains individual Phase M images and the board; final
  documentation is refreshed after the handoff commit without rebuilding or
  changing the executable source identity. Generated builds/logs are untracked.

### Worktree preservation and build-generated exclusions

All 59 pre-existing dirty paths retain their original byte hashes, including the
protected config and both UserSettings files. They were never staged. No accepted
reference/world sprite, scene, prefab, recipe, identity or narrative data was
committed by Phase M.

The Windows build additionally serialized four previously clean tracked files:

| Excluded path | Inspected change |
|---|---|
| `Assets/Settings/UniversalRP.asset` | shader prefilter flags derived during build |
| `Assets/UniversalRenderPipelineGlobalSettings.asset` | generated runtime settings list |
| `Assets/_SURLHWA/Art/Fonts/KoreanFallback.asset` | additional dynamic 1024×1024 atlas texture plus whitespace; source-font GUID/population contract unchanged |
| `ProjectSettings/ProjectSettings.asset` | existing InputSystem_Actions asset added to preloadedAssets; Standalone batching defaults serialized |

These are **non-whitespace build-generated changes**, not user baseline changes
or authored Phase M implementation. They remain visible and excluded from commits;
no blanket restore/cleanup was run. Their hashes are in ignored
`Temp/PhaseM/build-generated-files.json`. The post-build full-campaign rerun uses
the committed game code with these generated Editor files present. The build
started with no dirty implementation/settings files; its own serialization is
reported here separately from the pre-build inventory in BUILD_INFO.txt.
Local screenshots, manifests, test snapshots/logs and versioned build output
also remain ignored. Private save snapshots are never copied into the review package.

### Remaining classifications

| Classification | Scope |
|---|---|
| PASS | implemented Phase M screen contracts; compile/static checks; programmatic UI routing and sampled rendered readability, subject to the exact execution records |
| VALIDATION_DEBT | OS/physical held-input boundaries, native drag/release paths, full standalone UI navigation and human campaign playthrough; Editor virtual pointer events do not settle these |
| HUMAN_UI_TUNING_LATER | final font/spacing preference, visual balance, narrative reading cadence and overall feel |
| CHARACTER_ANIMATION_DEFERRED | walking, turning, seated poses and full-body transitions: real unimplemented work, explicitly excluded from Phase M |
| DEFERRED_AUDIO | final sound assets/mix and subjective auditory review |
| FUTURE_EXPANSION | bounded future guest/cocktail/revisit content, not triggered by this UI pass |
| OUT_OF_SCOPE | mobile/ultrawide/controller expansion, new UI/input/render frameworks, game-rule or save-format redesign |

No known required Phase M screen implementation is left as aesthetic tuning.
The owner retains final human approval; this record does not certify accessibility
or claim a complete human playthrough.

## Phase M patch — countertop method selection (2026-09-29)

This section supersedes the large method menu as the current presentation
contract. Earlier Phase M screenshots and PASS records remain historical.

### Baseline and bounded implementation

- Start: `aa4fc33b87791757f4642f4665c04b17314a9227`, branch
  `refactor/project-rebuild`; no reset or switch.
- Actual baseline: 63 dirty paths: protected config, 56 whitespace-only sprite
  metadata files, two UserSettings files and the four build-generated files
  already described above. Ignored `Temp/PhaseMDiegetic/baseline.json` records
  their byte hashes. Config contents are never read into reports or staged.
- Previous UX: Recommendation Brew opened an 800×224 charcoal menu with four
  176×70 technique tiles, explanation, confirmation and Back.
- New UX: the same controller/buttons present small counter-anchored tool
  setups. The legacy menu surface/title/tiles are hidden at runtime. A lower
  request note retains the guest, request type, already-visible request text,
  contextual help and small Start/Back controls. Selection is explicit; no
  route starts from hover or selection alone.
- `SurlhwaMethodSetupView` owns presentation only. `InteractionUIController`
  still owns the selected method and the sole existing confirmation callback.
  `OnClickConfirm`, authored Recommendation routing and Exact direct entry are
  unchanged. No authored dialogue, OrderData, recipe, save or bartending
  container/trace/timing/matching/evaluation/knowledge code changes.
- Back returns to the current request dialogue. Reopening clears local
  selection; a retry-safe route failure preserves the controller's selection.
  Codex/Settings keep existing modal priority; Guide remains local to the
  entered workspace. No new permanent corner controls or shortcuts.

### Visuals, mapping and input contract

| Counter setup | Existing route |
|---|---|
| Mixing glass and upright bar spoon | Stir / 젓기 |
| Existing illustrated Shaker | Shake / 흔들기 |
| Empty serving glass and small bottle | Build / 잔에 만들기 |
| Empty serving glass and horizontal spoon | Float / 층 쌓기 |

Float help explicitly describes pouring slowly along the spoon to form layers.
These are tool choices, not previews of an intended or already prepared drink.
The tools are uGUI images projected from four fixed world countertop points;
they do not add colliders or another world-input system. Camera, furniture,
characters, bar layout and reference inputs stay intact.

Each target is 128×132 reference units, with an 18-unit Korean label. Hover and
focus outline the tool; selection also has a check mark and short underline.
The transparent modal shield blocks underlying controls without dimming the
counter. All tool/shadow/label/marker graphics are non-raycasting. The request
note backs live text; its controls use the existing theme. See Docs/22 for
precise tuning locations and navigation rules.

Reused assets: `Assets/_SURLHWA/Art/Bartending/Props/shaker.png`, plus
`MixingGlass`, `RocksGlass`, `Spoon`, `SpoonHorizontal`, and `BottleSmall` from
the existing `PixelBartendingArt` catalog. BarScene gains one serialized Shaker
sprite reference; no world transforms or prefab changes. Shadows, label plates,
outline and underline are deterministic uGUI graphics. No new bitmap art,
external font or AI-generated image is introduced.

### Patch validation record

- A: `dotnet build Assembly-CSharp-Editor.csproj --no-restore` passes with zero
  errors and the eight existing warnings (MSB3277 and seven CS0252). Patch-only
  `git diff --check` passes. Repository-wide whitespace checking still reports
  the preserved pre-existing metadata/font whitespace; it is not normalized.
- Tested implementation: **`72ff492700b6ec74ff70ba5ff655710e68b6f4f5`**.
  Validation used the identical working sources before committing. All five
  changed source/scene files were SHA256-compared with the executed copy;
  ignored `Temp/PhaseMDiegetic/runtime-manifest.json` records those hashes.
  The following documentation commit has no runtime changes.
- The original Editor's Pipeline main-thread commands timed out (5/10/60 s).
  Automatic approval review rejected forced termination because unsaved Editor
  state could not be established. No forced termination occurred. The safe
  alternative was a source-only copy under
  `Temp/PhaseMDiegetic/ValidationProject`, opened with the same Unity 6000.3.7f1.
  No config, Library, UserSettings or save snapshot was copied into it. Its
  fresh import and temporary orchestration script are ignored test artifacts.
  Original Editor restart permission is no longer needed for this patch.
- Final executions on **2026-09-29 UTC**:
  `RunDiegetic` at **11:01:02** (1280×720) and **11:02:03** (1920×1080);
  physical/campaign integrated Editor regression at **11:02:04**;
  four-method physical Play Mode regression followed by `RunInputModule`,
  completed **11:02:57**. Each UI run restored and verified progress and sound
  preferences. The physical regression uses independent prefab fixtures.

| Evidence | Executed coverage and limits |
|---|---|
| A compile/static | zero compile errors; physical measurement/transfer/Stir/prefab checks; I10–I16, J1–J5 and campaign integrated regressions. Covers request identity, knowledge/supply, discovery retry, 18-night/47-encounter configuration and ending/persistence contracts. This is not a new full-campaign Play Mode run. |
| B direct invocation | nested Codex/Settings opening while method selection is visible is deliberately invoked as an overlay fixture. It checks restoration and blocking, not normal-play availability of those open buttons. |
| C raycast and input module | at both real Game View sizes, all four targets receive queued virtual mouse hover/down/up through the existing InputSystemUIInputModule. Center plus four padded hit-area samples; disabled confirmation; transparent background shield; selected marks; change/Back/reopen; correct route and unchanged Recommendation type/cocktail/tags; Guide open/close. Child overlays block targets and close through module clicks. |
| C existing input regression | fresh entry, disabled Continue, New Game, Codex masked cards/wheel/detail/back focus, guest prompt/dialogue, first Exact Stir route, Guide and Settings return. Exact/Recommendation semantics also pass the independent request regressions. |
| D rendered inspection | 26 retained production-hierarchy PNGs at 720p and 1080p, including all four selected setups and entered workspaces. Inspected native-size selected Stir, Shake, Build and Float examples across both resolutions: readable Korean/check marks, tool/label alignment, no missing glyphs or clipped request/help in the sampled authored request. Float moved right to clear the guest's head. |
| Physical regression | all four actual production prefabs: bottle pose/stream, finite Jigger measurement/overflow, transfer conservation, Stir motion, Shake presentation and Float layering paths. Programmatic mechanic seams in separate fixtures; not four human-made complete cocktails after selection. |
| E standalone | no new patch build or standalone navigation test. |
| F human | no human-input or complete human-playthrough claim. |

`RunDiegetic` seeds Night 5, then uses the normal Chunhyang guest/request flow
before opening the selector. Captures are **checkpoint fixtures**, not evidence
of playing nights 1–4 first. Existing bar corner buttons remain gated by the
method modal; Back returns to the request before opening Codex. Guide is
available inside the chosen workspace. The fixture-opened overlays do not add
another player-facing access route.

Earlier attempts are retained: the first 720p run timed out after capture
temporarily changed Canvas dimensions; the harness now yields a frame before
computing the next pointer position. A later run missed entry input during external
window activity, consistent with Game View focus interference. The final uninterrupted pair passed every assertion without
changing input policy or weakening checks. No game-rule change was made to
obtain a pass. Logs retain the early failures separately from final results.

Evidence paths (ignored, retained locally):

- `Temp/PhaseMDiegetic/isolated-editor.log`, `validation-results.txt`,
  `compile-final.log`, `runtime-manifest.json` and `baseline.json`.
- `Logs/Review/phase-m-diegetic-selected-{Stir,Shake,Build,Float}-{1280x720,1920x1080}.png`
  and corresponding `workspace-*`, `all-setups-float-selected-*`, dialogue PNGs
  and text/raycast reports. Individual images are retained; no thumbnail board
  substitutes for them.
- Examples: [Float 720p](../Logs/Review/phase-m-diegetic-selected-Float-1280x720.png),
  [Build 1080p](../Logs/Review/phase-m-diegetic-selected-Build-1920x1080.png).

All 63 baseline paths retained their exact byte hashes after validation and
implementation commit. The config remained untouched and unstaged. No additional
tracked importer/settings changes were caused in the original project. Generated
state in the isolated copy is excluded along with logs and captures. The only
committed scene delta is the Shaker sprite reference; the new script metadata
introduces one new GUID and changes no existing GUID.

The existing Windows package remains source `ba4346f`, predating this patch.
No patch standalone build or standalone navigation result is claimed.

PASS applies to this bounded implementation and the executed A–D/physical
contracts above. VALIDATION_DEBT remains for native physical input/held-button
release, a patch standalone package/navigation, and full human playthrough.
HUMAN_UI_TUNING_LATER covers final preference for scale, labels and feel.
CHARACTER_ANIMATION_DEFERRED, DEFERRED_AUDIO, FUTURE_EXPANSION and OUT_OF_SCOPE
remain as above. No required method-selection implementation is left as tuning.

## Method-selection polish — countertop staging and light (2026-09-29)

### Scope and current presentation

Start: `ca16d956c2589fdef6aa007a501db9547b7a2906` on
`refactor/project-rebuild`. The previous patch was functionally intact. Its
thin flat contact strokes, separate upright props, 120×28 opaque label bars
and check/underline emphasis still made the setups resemble a row of buttons.
This pass changes only their presentation and focused capture validation.

- Stir: larger mixing glass, with the spoon overlapping inside it.
- Shake: existing Shaker remains dominant; a small adjacent Jigger completes
  the arrangement.
- Build: serving glass dominates, with the small bottle behind and beside it.
- Float: horizontal spoon rests at the glass rim, with a small Jigger alongside.
  The existing explicit layering help remains in the request note.
- Every arrangement has a soft oval contact shadow and individual prop
  footprints. Minor art offsets vary depth without moving any hit area.
- Hover gently brightens the props and adds warm rim/countertop light. Selected
  light persists after pointer exit. The check is a secondary non-color cue.
- Labels shrink from 18 to 16 reference units; opaque bars and underlines are
  removed. A small soft text backing and one-unit shadow preserve legibility.
- Existing 128×132 targets, anchors, request note, Start/Back, controller
  callbacks, modal shield, focus/input policy and method mapping are unchanged.
  No order, technique, liquid, preparation, knowledge, progression or save
  authority changes. Exact direct entry and Recommendation choice remain intact.

All tools reuse the existing art catalog and Shaker reference. `Jigger` is the
only additional reused catalog sprite. The helper `SurlhwaSetupSoftOval` makes
contact/light/text-backdrop meshes with four alpha bands and 32 segments. This
is deterministic code geometry, not generated bitmap or AI artwork. No new
asset, font, scene, prefab, world transform, importer or GUID change is needed.
Docs/22 lists sizes, alpha values and tuning locations.

### Source and validation identity

The final runtime source is the implementation included with this section,
based on the starting revision above. Executed copy and original sources were
SHA256-compared after the last runtime change:

| Source | SHA256 |
|---|---|
| `Assets/Code/UI/SurlhwaMethodSetupView.cs` | `533452C273B2A11A23D4CA4CB33448E262E8E94EB7E1AEBED5971FC45E7E2046` |
| `Assets/Code/Editor/SURLHWAPhaseMUIValidation.cs` | `0BAC14AB6BC927D3F78A0A64ACB3B8F16C7C5533B8E5DA3356F2F24233D17428` |

The original Editor still timed out on a five-second Pipeline probe. Validation
used the existing source-only copy in `Temp/PhaseMDiegetic/ValidationProject`
with Unity 6000.3.7f1. Only the two changed source files were updated there.
The original Editor was not terminated. Owned validation Editors closed
normally after tests. Each UI run snapshot/restored and verified progress and
sound preferences; no owner New Game reset was left behind.

| Evidence level | Result and boundary |
|---|---|
| A compile/static | `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: zero errors, eight existing warnings (MSB3277 and seven CS0252). Scoped diff whitespace check passes; baseline metadata/font whitespace is preserved. |
| B callback fixtures | Existing diegetic regression directly opens nested Codex/Settings as fixtures; this does not imply their corner buttons are available through the method modal. |
| C pointer/raycast | `RunStagingAfter` passes all four hover and selected states at both resolutions, including selection after pointer exit, actual nonempty shadow CanvasRenderer meshes and non-raycasting decorations. `RunDiegetic` passes all four route starts, center/four padded hit samples, disabled confirm, shield, selection changes, Back/reopen, unchanged Recommendation context, Guide return and nested modal blocking/close. These use queued virtual mouse states through InputSystemUIInputModule. |
| C shared input regression | Fresh-session `RunInputModule` passes entry/settings/New Game, Codex masked cards/wheel/detail/back focus, guest/dialogue, Exact Stir entry, Guide/settings and no remaining modal/time lock. |
| D rendered inspection | Before/after hover and selected PNGs for every method at actual 1280×720 and 1920×1080 Game Views. Inspected representative full-resolution outputs across both sizes: lighter labels, tool grouping, readable request/help/Start/Back, contact shadows and restrained selected light. Contact shadow also darkens sampled countertop pixels; it is rendered, not just a populated component. |
| Physical/campaign regression | `SURLHWAPhysicalBartendingValidation.RunRegression` passes four production prefab hierarchies, measurement/mixed transfer, spoon orbit, bottle pose/stream/state isolation, and campaign integrated static/edit-mode checks including request/knowledge/discovery and 18 nights/47 encounters. Four route entry checks are Play Mode; this polish pass does not claim four complete physical-input cocktails or a new full-campaign Play Mode run. |
| E standalone | No new package or standalone navigation run for this polish. Existing package predates both selector patches. |
| F human | No human mouse/keyboard playtest or aesthetic approval claimed. |

Final executions, **2026-09-29 UTC**, after the final runtime change:

- Staging captures/checks: **12:09:16** at 720p, **12:09:34** at 1080p.
- Four-method diegetic regression: **12:10:27** at 720p, **12:11:22** at 1080p.
- Physical/campaign integrated edit-mode regression followed the 1080p run.
- Fresh-session common input-module regression: **12:15:47**.

Earlier attempts remain recorded. An early TMP measurement while the panel was
inactive threw before font initialization; fixed label widths remove that call.
The first custom shadow lacked a CanvasRenderer, found during rendered review;
the helper now requires it and the capture validator checks real mesh output.
A test API compile error was corrected before final compilation. A temporary
orchestration flag was lost on Editor restart and mislabeled one later capture
run as before; those copy-local images are excluded. The original baseline PNGs
had already been preserved separately. The explicit final after-run above is
the source of all after evidence.

The common input test appended to the final multi-run sequence reported a
remaining modal lock at 12:11:38. Its cause was not conclusively isolated. The
same unchanged assertions passed in a fresh Editor session at 12:15:47, while
both dedicated diegetic modal runs passed. Both results remain in the logs;
fresh-session success does not certify arbitrary long-session or native-input
behavior. No assertion or input policy was weakened to pass.

### Retained review evidence

These are production hierarchies reached from a deterministic **Night 5
checkpoint fixture**, then the normal Chunhyang dialogue/request flow. They
are not evidence of playing the preceding nights or human mouse input.
Baseline runtime is `ca16d95`; before captures completed at 11:51:08/11:51:26 UTC.

- [Before/after review index](../Logs/Review/phase-m-staging-review.html)
- `Logs/Review/phase-m-staging-{before,after}-{Stir,Shake,Build,Float}-{hover,selected}-{1280x720,1920x1080}.png`
- 32 method-state PNGs retained, plus four dialogue context captures and
  corresponding text/raycast reports. Individual full-size PNGs remain
  available from the review index.
- [Float before 720p](../Logs/Review/phase-m-staging-before-Float-selected-1280x720.png)
  / [Float after 720p](../Logs/Review/phase-m-staging-after-Float-selected-1280x720.png).
- `Temp/PhaseMStaging/compile-polish.log`, `editor-final-after.log`,
  `input-final.log`, `validation-results-final-sequence.txt`,
  `input-final-results.txt`, `runtime-manifest.json` and `baseline.json`.

Captures, review HTML, logs and validation copy are ignored local artifacts;
private progress snapshots are not copied into review material. All 63 baseline
dirty paths match their starting byte hashes, including protected config,
56 metadata files, two UserSettings and four prior build-generated files. No
additional tracked asset/settings churn was caused by this task.

PASS: bounded presentation implementation and the checks above.
VALIDATION_DEBT: native/held-input checks, full long-session playtesting and a
current standalone package. HUMAN_UI_TUNING_LATER: owner approval of grounding,
light strength, subordinate labels and physical feel. CHARACTER_ANIMATION_DEFERRED
remains real unfinished animation work; DEFERRED_AUDIO, FUTURE_EXPANSION and
OUT_OF_SCOPE remain unchanged. This patch does not reopen broad UI or bar design.
