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

## Chained modal-lock follow-up (2026-09-29)

**Original issue: UNRESOLVED / VALIDATION_DEBT.** A fresh-session pass did not
resolve the chained failure. The investigation below preserves that failure;
successful repetition is evidence of the observed runs, not a root-cause fix.

### Baseline and original evidence

Start: `a027d6c6536c96dedded6050e83846217e1dc9e5`, branch
`refactor/project-rebuild`. Staging history: diegetic implementation `72ff492`,
handoff `ca16d95`, countertop polish `a027d6c`. Actual baseline was inventoried
again: 63 dirty files, recorded by path and byte hash in
`Temp/PhaseMModalFollowup/baseline.json`. Protected config is excluded from
edits/staging; only its hash is used. No owner Editor is force-closed.

Original evidence retained:

- Failed: `Temp/PhaseMStaging/editor-final-after.log`, assertion at line 17929,
  result **2026-09-29 12:11:38.7304364 UTC**.
- Successful fresh Editor: `Temp/PhaseMStaging/input-final.log`, result
  **12:15:47.5410781 UTC**. It used the unchanged test, not a corrected test.
- Both executed the runtime/test sources subsequently committed in `a027d6c`.
  Their two SHA256 values are recorded in the preceding section and
  `Temp/PhaseMStaging/runtime-manifest.json`. There was no intervening production
  input fix. At execution time these were working sources based on `ca16d95`.

The failed chain used one Editor process, with normal Play Mode exit/entry
between probes: staging-after 720p, staging-after 1080p, diegetic 720p,
diegetic 1080p, physical/campaign edit-mode regression, then `RunInputModule`.
Each UI probe used its existing save snapshot/restore fixture. The final test
performed disabled Continue, entry Settings open/close, New Game, Codex scroll,
detail/Back/close, guest/dialogue, first Exact Stir entry, Guide open/close,
bartending Settings open/close.

Expected final state: time scale exactly 1 and `SurlhwaModalSurface.Top == null`.
Actual original evidence: `InputModuleFlow` threw
`InvalidOperationException: Module flow left a modal lock`, caught by the
test runner's `Guarded` coroutine. This is an assertion exception, not a timeout
or an identified production lock owner. The preceding Guide-close assertion
passed. A queued click on `CloseSoundSettings` was logged, but that log alone
does not prove the callback executed. The old assertion joined two conditions
and recorded neither their values nor the top modal name. It cannot distinguish
a missed close, pause restoration problem, unexpected surface or another cause.

`SurlhwaModalSurface` owns the observed modal list, gates and shared pause count;
`DemoAudioController` owns Settings visibility and its close callback;
`RecipeGuidePanelController` owns the technique read flag. Only the modal owner
sets/restores time scale in production. No evidence identifies which of these,
if any, was responsible in the original run. Environment/focus interference,
fixture teardown and production cleanup remain hypotheses, not diagnoses.

### Investigation changes and production policy

Only `Assets/Code/Editor/SURLHWAPhaseMUIValidation.cs` changes executable code.
Production modal/input, tools, hit areas, callbacks, requests, knowledge,
preparation, progression and persistence are unchanged.

Read-only traces now record before/after module clicks: frame, time scale,
pause-owner count and saved prior scale, top/active surfaces and instance IDs,
Settings/Guide flags, selected control, top center raycast, active bartending
route, button runtime-listener count, liquid observer count and Editor/app focus.
The original input-module click timing and final assertion are retained.
After a later unapplied Brew action, diagnostics also record callback delivery
and up to 16 processed mouse events per click (device, frame, position, button).
The temporary observer is removed after the click; the input-event subscription
is removed with the runner. Original click timing, input devices/bindings and
production callbacks are retained; missed clicks are not retried.

`RunModalSequence` uses one initial Night 5 checkpoint per resolution. Thereafter
it makes no checkpoint reset, forced scene return, lock clear, time-scale write
or Play Mode restart between customers. Each of four consecutive services:

1. Repeats legal idle-bar Codex/Settings open/close three times.
2. Reads the normal request, repeats all four setup selections and Back three
   times, verifies corner controls stay blocked, then reopens and confirms the
   chosen route. Request type, requested cocktail and emotion tags are compared.
3. Repeats Guide/Settings open/close three times in the workspace, checking one
   surface/shield while open and none when closed, pause state and focus.
4. Makes a supplied, deliberately imperfect single-ingredient preparation
   through public physical action seams; uses pointer clicks for technique/serve
   controls, then waits for actual result, additive unload, authored farewell
   and natural departure. The ordinary Next Night control continues the sequence.

This is legal experimentation, not a recipe-success test. Preparation uses
programmatic action seams, including finite Jigger measurement for Stir; no
result, knowledge, guest completion or narrative outcome is injected. During
the preparation seam only, legacy keyboard polling is suppressed as in the
existing campaign driver, then restored before technique/UI checks.

The new fixture's development failures are separate from the original issue:
it first attempted Codex after Back, but request dialogue's `OverlayBlocker`
correctly blocks corner controls. It now tests them at the idle bar. A direct
bottle seam initially conflicted with legacy released-Space polling and was
interrupted normally; save restoration ran. Its new logger also needed Unity
destroyed-object handling for focus after unload. Finally it needed a rendered
frame after newly revealed Shake and night-summary controls before raycasting.
Those fixture changes do not weaken the old post-Settings assertion or alter
production input policy. Failed attempt logs remain retained.

### Execution environment and visual scope

The same owned validation Editor process uses
`Temp/PhaseMDiegetic/ValidationProject`, Unity 6000.3.7f1. A byte-hash comparison
of all **1,251 original files under Assets, Packages and ProjectSettings** matched
the executed copy, including runtime assets, metadata, package lock and the
existing dirty project settings. The copy's extra temporary launcher and its
metadata are test orchestration only. Library/import caches and UserSettings are
local to the copy. No config or private save snapshot is copied into review
artifacts. The existing snapshot/restore harness protects shared PlayerPrefs.

The old launcher's delayed Editor callbacks did not advance in this run. Host
dispatch therefore invoked the same probes through the copy's Unity Pipeline
connection, retaining the Editor process and original probe order/Play Mode
boundaries. This scheduling difference is a reproduction limit. Full comparison
and dispatch records are retained under `Temp/PhaseMModalFollowup/`.

Rendered inspection used existing staging captures. The Stir hover image shows
brightened tool edges; the Shake selected image shows brighter object surfaces
and rim light. Other, unhovered setups in those frames show the idle treatment.
There is no separate all-idle capture claimed. Countertop light/contact shadows
support the tool emphasis. No aesthetic edit was made.

- [Stir hover / other idle tools, 720p](../Logs/Review/phase-m-staging-after-Stir-hover-1280x720.png)
- [Shake selected, 1080p](../Logs/Review/phase-m-staging-after-Shake-selected-1920x1080.png)
- [Existing complete before/after index](../Logs/Review/phase-m-staging-review.html)

No new standalone build or standalone navigation is claimed. Human physical
input and aesthetic approval are not claimed. A successful instrumented run
must not be used to close the original issue without identifying its cause.

### Follow-up results

Test implementation commit: **`424b65bdb3979ae6a8016e295b420112c375ec99`**.
The following handoff correction is documentation-only; no further executable
change was made after the recorded final checks.

Executed production C# revision: **`a027d6c`**, unchanged, with the preserved
asset/settings baseline recorded in the manifest. The final follow-up test source
has SHA256 **`5F12E74A17639013FBE90E8FC104D6974615C53BC61A4B06B8D8CEDED1D75B08`**;
it matches the executed copy. The earlier state-trace/repeated-navigation version
was **`94BD5F658AE25439F2535C8B3587F770465C691BD1B06E484915F999E3BB1A23`**.
The only subsequent change adds callback/device-event observations. Test-only
changes and this record are included in the follow-up commit. The full comparison is
`Temp/PhaseMModalFollowup/copy-manifest.json`.

Confirmed **2026-09-29 UTC**:

- Initial instrumented original-order replay: staging 720p **12:45:45**,
  staging 1080p **12:48:05**, diegetic 720p **12:49:35**, diegetic 1080p
  **12:51:16**, physical/campaign regression then input-module **12:52:05**.
  All passed without restarting the Editor. An additional input-module run
  passed at **12:53:18**. This first replay used the initial trace-only test
  revision, before the repeated-navigation fixture was added.
- Final same-Play-Mode legal navigation: **13:13:09** at 1280×720 and
  **13:15:42** at 1920×1080. Each run completed four consecutive services
  (Stir, Shake, Build, Float), 12 idle Codex/Settings cycles, 12 method
  selection/Back cycles and 12 workspace Guide/Settings cycles. Only the
  initial checkpoint was seeded. Both crossed the ordinary Night 5→6 boundary.
- Both legal runs kept the original request semantics, released modal shields
  and pause ownership on ordinary close/unload, and restored usable focus on
  overlay close. Settings/Guide runtime button listeners remained one; the
  visible workspace shelf had one liquid observer. No duplicate active modal
  or shield was accepted by the assertions.

Three further original-order chains used the `94BD...` test revision in the same
Editor process. The first two completed every probe; the third stopped at a
different assertion, which is **not counted as a passed chain**:

| Chain | Staging 720p / 1080p | Diegetic 720p / 1080p | Physical/campaign → input-module |
|---|---|---|---|
| 1 | 13:16:41 / 13:17:04 PASS | 13:18:01 / 13:19:02 PASS | 13:19:23 PASS |
| 2 | 13:19:45 / 13:20:08 PASS | 13:21:06 / 13:22:07 PASS | 13:22:28 PASS |
| 3 | 13:22:50 / 13:23:14 PASS | 13:23:29 FAIL at Brew entry / not reached | not reached |

At the third chain's Brew failure, both before/after traces showed time scale 1,
zero pause owners, no top/active surface, an inactive bartending route, one Brew
listener, a BrewButton center raycast and active Game View/app focus. Selection
did not open and focus remained on NextButton. Thus this observation is **not a
retained modal/pause lock**. At that revision the trace did not count delivered
callbacks, so it cannot distinguish input delivery from a guarded callback.
Nor does focus sampled before/after settle transient focus changes between them.
Its cause and relationship to the original post-Settings failure remain unknown.
No production or input-isolation workaround was added.

After adding the callback/device observations (`5F12...`), diegetic 720p passed
at **13:28:59**. Six further `RunInputModule` probes alternated 720p/1080p in the
same Editor and passed at **13:30:11, 13:30:32, 13:30:53, 13:31:14, 13:31:35,
13:31:56**. Every sampled Settings close delivered one callback; its queued
virtual events arrived on separate frames, and no external mouse event was
observed in those sampled click windows. This does not establish that the old
failures were caused by external input, timing, fixture teardown or production.

Final `5F12...` validation then passed diegetic 1080p at **13:34:32**, and reran
the complete legal same-Play-Mode sequence at **13:37:08** (720p) and **13:39:42**
(1080p). These final two runs again completed eight total services and 24 cycles
each of idle overlays, selection/Back and workspace overlays. No production
change occurred between any of the replays.

After those final UI runs, physical/campaign edit-mode regression passed,
including measurement, mixed transfer, Stir motion, four prefab hierarchies,
request/knowledge/discovery and 18-night/47-encounter configuration. The existing
Phase K Play Mode regression also passed Build, Stir, Shake and Float physical
tool/measurement/overflow/transfer fixtures. This is not a new full-campaign
Play Mode or human-input run. Compilation passed with zero errors and eight
pre-existing warnings; scoped `git diff --check` passed.

An independent read-back at **13:41:06 UTC** verified all snapshotted progress,
knowledge and sound preferences after the final physical tests. The private
snapshot's hash also matched the earlier follow-up snapshot. No save contents
are in review artifacts. All 63 original dirty files retain their baseline byte
hashes; config remains untouched and unstaged. No task-induced tracked importer,
scene, prefab or settings changes remain. The owned validation Editor closed
normally; the owner's original Editor was not closed.

**Disposition:** retain the original post-Settings assertion as open
UNRESOLVED / VALIDATION_DEBT, along with the later unapplied Brew action. Three
complete original-order replays passed (initial plus two final-order chains);
a fourth stopped at Brew. Successful focused and legal-navigation repetitions
do not identify either original cause. There is insufficient evidence for a
production cleanup fix or for declaring an environmental/test-harness root
cause. The added traces provide the missing state and delivery evidence for
the next recurrence. No blanket lock clear, time reset, automatic retry or
production input change was used to make a result pass.

Evidence levels remain separate: the repeated navigation uses actual EventSystem
raycasts and queued virtual mouse input through InputSystemUIInputModule.
Dialogue setup also uses the existing programmatic pointer helper. Physical
preparation uses action seams, while technique/serve/navigation buttons use
module clicks. The original diegetic fixture's directly opened nested overlays
remain callback-only setup evidence; they are not confused with the legal
pointer-only overlay access exercised by `RunModalSequence`.

Original logs remain under `Temp/PhaseMStaging/`. Follow-up logs and reproducible
host dispatch scripts are under `Temp/PhaseMModalFollowup/`, including
`reproduce-chain.log`, `latest-results.txt`, `dispatch.log`, `compile.log`,
`baseline.json`, `copy-manifest.json` and `run.py`. These are ignored local
artifacts. The actual tests and this investigation record are tracked.

A [sanitized local evidence index](../Logs/Review/phase-m-modal-followup-evidence.txt)
collects probe results and exceptions without raw Editor startup output. An early
process inspection unintentionally printed an Editor launch token to tool output;
subsequent process inspection was restricted. No token or config content is
included in this record or commits.


## Targeted event trace follow-up — 2026-09-29

### Separate issues and safety baseline

Start HEAD `cef41122eae3a4c451702a17656dab8ecd4fc9e2`, branch
`refactor/project-rebuild`; previous diagnostic revision `424b65b`, staging
revision `a027d6c`. The actual dirty inventory again contains 63 tracked files,
recorded by path and SHA256 in `Temp/PhaseMEventTrace/baseline.json`. No protected
config contents are read or printed. Reference art, staged tools, hit areas,
request semantics, gameplay and persistence are outside this change.

**A — Settings-close state mismatch remains a separate historical issue.**
The original exception at `Temp/PhaseMStaging/editor-final-after.log:17929`
combined time scale and top-modal tests without recording either value.
`input-final.log:2185` records the unchanged successful run. The earlier
investigation's sequence, revision hashes and failure evidence above remain
valid historical records. Neither a callback nor actual close completion was
established in the original failure.

**B — Brew activation failure is not an established modal-lock defect.**
`Temp/PhaseMModalFollowup/reproduce-chain.log:145236` records inactive selection
following a queued Brew attempt. Its adjacent state snapshots show a valid
manual raycast, no modal/pause lock, and active Game View focus. They do not
establish delivered Down/Up/Click, production callback entry, guard rejection,
or transient open/close. A relationship between A and B is unproven.

Original logs were re-inspected using allowlisted diagnostic prefixes only;
raw Editor startup output and process arguments were not printed. The extracted
metadata is `Temp/PhaseMEventTrace/original-observations.json`.

### Small observational extension of the existing runner

Installed and locked packages: Input System **1.18.0**, uGUI **2.0.0**. Runtime
traces confirm `InputSystemUIInputModule`, with enabled Point and Click actions.
The installed module dispatches Down through `ExecuteHierarchy`, then stores
pointerPress/pointerClick. On release it compares the current click handler and
eligibility before dispatching Up and Click. This distinction matters: an
independent RaycastAll does not prove that dispatch occurred.

Opt in with Editor SessionState `SURLHWA_UI_EVENT_TRACE=true`, then invoke the
existing Phase M probes. Ordinary use leaves the sink unbound. The existing
runner owns an **8,192-event ring buffer**, flushed on success or first failure
before ordinary fixture teardown. Headers report dropped old entries. Files
are local under the validation copy's `Temp/PhaseM/EventTraces/`, archived for
this investigation at `Logs/Review/phase-m-event-trace/traces/` (with a working
copy under `Temp/PhaseMEventTrace/traces/`). Nothing is streamed from
process arguments, environment, authentication files, dialogue or save values.

Every module click has a run/attempt/control correlation ID. Boundaries record
frame, monotonic realtime and unscaled game time:

- queued virtual mouse state;
- input event received before application, post-update device state, and the
  actual UI action's Performed/Canceled notification;
- actual module raycast after processing;
- delivered PointerDown, PointerUp and PointerClick with pointer identity,
  position, current/press raycasts, press/click targets and eligibility;
- production Brew callback entry and each existing early-return/route branch;
- selection open request/completion, cancel, controller disable and panel close;
- existing modal acquisition/release, including pause count and restored scale;
- Settings callback entry and completed visibility change.

State includes enabled UI actions, target/component instance identities,
active/interactable/allowed state, focus, app/Game View focus, current modal,
shared pause state, route-busy state and only the relevant guest/reading guards.
Object and component instance IDs differ normally; both retain their type context.
The selection's active-in-hierarchy state is recorded alongside its own state.

The Editor-only pointer observer is added to the **same existing Button object**
which already implements Down/Up/Click. The fixture asserts handler resolution
is unchanged before/after attachment. It adds no graphic or raycast target,
never consumes/forwards events, changes focus, retries a click or invokes a
fallback callback. Observer Click runs after Button's original handler in that
same dispatch, so the production callback can appear earlier in the trace.
Input action subscriptions and the diagnostic sink are removed at teardown.
The final trace records post-input updates only after a received event and
module state only when its target/device state changes; it does not flood the
buffer with unchanged frames. All instrumentation has some execution overhead;
a non-recurrence under tracing cannot establish timing independence.
No package source is patched. Production-source hooks are conditional on
`UNITY_EDITOR`; standalone calls and their argument evaluation are omitted.

Settings checks now capture the legitimate state **before opening**: time scale,
top modal, shared pause, opener permission and interactability. Closing reports
separate expected/actual values and whether both Settings visibility and its
active modal have gone away. A one-second realtime deadline observes close
completion only if necessary; it can expire while paused. It supplies no input
and does not turn a mismatch into a reset. Existing click timing is unchanged.
The owner currently closes synchronously. Entry Settings legitimately restores
the entry modal; workspace Settings normally restores no modal. An Exact Brew
may directly start its authored route, so selection-open is not applicable to
that route and is not treated as a missing boundary.

### Declared reproduction budget and execution policy

Before execution: **one original-order chain**, then **one legal same-Play-Mode
navigation probe at each resolution**, with a stop at the first failure. At most
one comparison run was reserved after preserving a failure. No repeated full
campaign playthrough was budgeted. The chain retains the recorded order and
Play Mode boundaries: staging 720p/1080p, diegetic 720p/1080p, existing physical
and campaign static regressions, then InputModuleFlow at 1080p.

The legal navigation probe provides focused Brew repetition and focused Settings
cycles within one session: four successive services, all four targets, three
selection/change/Back cycles per service, idle Codex/Settings and workspace
Guide/Settings cycles, actual supported service/departure and Next Night return.
It seeds only the initial checkpoint. It does not clear locks, reset global state
or restart between steps. The older diegetic probe's directly opened nested
overlays remain explicitly fixture coverage, not legal pointer reachability.

Execution uses the existing validation copy at
`Temp/PhaseMDiegetic/ValidationProject`, Unity 6000.3.7f1. All **1,251 original
files** under Assets, Packages and ProjectSettings matched by byte hash, including
metadata and dirty settings. The only copy-only source is the existing isolated
Editor launcher and its metadata; caches/UserSettings are local. Host dispatch
uses the copy's existing Unity Pipeline connection and disables only that old
launcher's automatic scheduling, as documented previously. The source manifest,
finite host dispatch and compile result are in `Temp/PhaseMEventTrace/`.

### Sensitive-output containment and owner action

Exposure metadata: on 2026-09-29, a prior process-command-line inspection placed
an **Editor launch access credential** in tool output. Its exact scope, validity
and revocation state have not been established. It is not reproduced, used,
parsed for claims of expiry, or included in this record. Not committing it does
not contain the earlier tool-output exposure.

This follow-up allowlists process ID/name/start time only and extracts only
known diagnostic messages from raw logs. No environment/authentication dump,
account action, session revocation, log purge or history rewrite was performed.
The owner's running Editor is preserved.

Supported owner actions are available under Unity Dashboard → Account → Security:
review Sessions and Security history, end an unrecognized session, and reset the
password. Unity states a password change signs devices out. These are owner
actions; this investigation does not claim they invalidate this particular
launch credential. Consult Unity Support for token-specific containment if
needed. Source: [Unity account security documentation](https://docs.unity.com/en-us/cloud/accounts/manage-account-security),
checked 2026-09-29. Authentication SDK player-token instructions were not applied
to an Editor/Hub credential.


### Bounded replay results and last confirmed boundaries

The declared reproduction budget completed without a new A or B failure:

| UTC completion, 2026-09-29 | Probe | Result |
|---|---|---|
| 14:08:21 | Staging 1280×720 | PASS |
| 14:08:45 | Staging 1920×1080 | PASS |
| 14:09:42 | Diegetic 1280×720 | PASS |
| 14:10:43 | Diegetic 1920×1080 | PASS |
| 14:11:06 | Original InputModuleFlow, 1920×1080 | PASS |
| 14:13:51 | Same-session navigation, 1280×720 | PASS |
| 14:16:25 | Same-session navigation, 1920×1080 | PASS |

Each same-session run completed Stir, Shake, Build and Float through natural
service/departure and the night boundary. Together: eight services, 24 idle
overlay cycles, 24 selection/Back cycles and 24 workspace overlay cycles. The
original chain includes the existing physical and campaign static regression.
The reserved comparison was not needed during those seven probes; it was later
used after the final verification failed, as recorded below.

**A: UNRESOLVED / VALIDATION_DEBT.** Last historically confirmed successful state:
Guide closed and time scale was 1. The Settings close was queued. First missing
boundary: actual delivery of that close and entry to its callback. First known
incorrect observation: the combined final state assertion, without individual
values. The new successful trace establishes the close callback, owner release,
visibility completion and separate expected/actual values only for this run.

**B: UNRESOLVED / VALIDATION_DEBT.** Last historically confirmed boundary:
preflight/manual raycast and queued virtual activation, with no lock in before/
after snapshots. First missing boundary: actual input-module press delivery to
Brew; Up/Click/callback/guards were also unobserved. First known incorrect state:
selection remained inactive at the old post-attempt check. There is no captured
new failure timeline that narrows these historical boundaries further.

**Relationship: not demonstrated.** Neither production cleanup, invalid test
sequencing, timing, nor environment has been established as either root cause.
No speculative gameplay/input fix is justified. The remaining evidence needed
is an actual failed correlated attempt showing where delivery or restoration
diverges, compared with a successful attempt at that same request/parent state.

A complete successful Brew reference is retained in
`traces/20260929T140808211-staging-after-pass.txt`, attempt `/1/BrewButton`:

| Frame | Monotonic seconds | Observed boundary |
|---|---|---|
| 2454 | 9.854926 | Queue position/released state |
| 2455 | 9.859223 | Input received; Point performed at 9.859728 |
| 2455 | 9.860490 | Queue pressed state |
| 2456 | 9.862791 | Receive press; Click action performed at 9.863982 |
| 2456 | 9.865226 | PointerDown, current/press ray = BrewButton |
| 2456 | 9.865695 | Queue released state |
| 2457 | 9.867987 | Receive release; Click action performed at 9.868207 |
| 2457 | 9.868635 | PointerUp, eligible, same press/current target |
| 2457 | 9.868907 | Production Brew callback entry; guards permit selection |
| 2457 | 9.869212 | Selection open requested |
| 2457 | 9.870648 | Selection active in hierarchy; method modal acquired |
| 2457 | 9.871289 | PointerClick observer in the original dispatch |
| 2564 | 10.167034 | One callback; selection still visible |

Pointer device 14, position (892,56). This is a Night 5 Recommendation fixture,
matching the historical failed probe's setup, not a captured failure. The new
InputModuleFlow Exact request correctly uses its existing dedicated route;
selection opening there is not applicable.

Successful Settings reference:
`traces/20260929T141055872-input-module-pass.txt`, attempt `/15/CloseSoundSettings`.
At frame 4003, monotonic 14.387526: close callback entered while paused. At
14.388453 the modal owner released to zero owners, scale 1, no top modal. At
14.388784 visibility close completed. Frame 4105 / 14.685512 recorded:
closeComplete=true; scale expected/actual 1/1; top NONE/NONE; paused false/false;
opener allowed true/true and interactable true/true. Earlier entry Settings in
that trace restored DemoEntryPanel, demonstrating why an empty-stack assumption
cannot be applied to every close.

### Final diagnostic review and test-copy interruption

Diff review retained the previous open-state pause, shield-count and focus
assertions inside the new Settings helper, now relative to its legitimate
parent. It also reduced repeated trace samples to changed module state and
post-update samples following actual received events. The original budget's
longer traces had dropped old entries from the ring; this is recorded in their
headers, not hidden. The complete first Brew reference above has no drops.
The test refinement does not change a production guard or click timing.

A finite final verification was declared: one 720p same-session probe, one 1080p
InputModuleFlow, and the existing Phase K Play Mode regression. Its first launch
could not start because Assets and some settings had disappeared from the copy:
CLI reported `Not a Unity project`, with 28/1,251 files still matching in the
failed copy manifest. No test result was manufactured from that invocation.
The original owner Editor PID was no longer present; no agent close/kill command
was issued. The mechanism causing the filesystem change was not established.
This later interruption is not evidence about A or B.

All seven completed buffers and the private progress snapshot survived. They
were archived before recovery; independent read-back matched saved progress,
knowledge and all audio preferences. Source/assets/settings were recopied
additively from the current original, matching 1,251/1,251 files again. The same
owned validation Editor process (PID 6612) was retained. The old copy-only
launcher had disappeared, so final host dispatch omitted that launcher's
unsubscription preamble and invoked the existing validators directly. No
production modal, EventSystem or save reset was used as a recovery workaround.

Two unrelated original paths changed bytes during this interval:
`ProjectSettings/ProjectSettings.asset` and `UserSettings/Layouts/default-6000.dwlt`.
They were not edited, staged, restored or committed by this task. Their new bytes
are preserved. The other 61 baseline paths, including protected config, retained
the recorded hashes at recovery. This is not reported as 63 byte-identical files.
`budget-source-manifest.json` records the completed reproduction source state;
`final-source-manifest.json` records the recovered copy and final diagnostic
source state. The failed intermediate `copy-manifest.json` remains retained.


### New failed verification: delivery observations, not a retrospective diagnosis

The final verification did **not** pass. In the 720p run starting 14:20:20,
ordinary Brew and the stricter Settings cycles worked before service/farewell.
A later Next activation lost delivery. The inherited farewell loop did not
assert the callback count and kept attempting Next. Thus its intended
stop-on-first-failure policy was not met: the first retained zero-delivery action
was attempt 51, and the loop reached attempt 826 before the investigation stopped
it through the runner's normal failure/ExitPlayMode path at **14:24:58**.
No gameplay locks were cleared. Save restoration then verified successfully.

The pre-stop buffer retains the first missed attempt and its preceding success:
[full live trace](../Logs/Review/phase-m-event-trace/traces/live-before-stop.txt).
The later automatic flush has dropped older events, as its header records:
[failed final verification](../Logs/Review/phase-m-event-trace/traces/20260929T142020944-modal-sequence-FAIL.txt).

Relevant sequence (monotonic seconds):

- Attempt 50 / Next: Down, Up and Click delivered, one callback.
- Frame 15400 / 43.341284: Point canceled from probe device 21;
  appFocus=false. Click canceled at 43.341787.
- Frame 15401 / 43.348434: Point performed from **non-probe device 2**;
  appFocus=true. This is device evidence, not a claim that a human moved a mouse.
- Attempt 51: desired point (388,56), valid preflight. Probe device 21 received
  press/release and generated Click action notifications on frames 15403/15404.
  There was no new Point action from device 21; actual module raycast was NONE.
  No Down, Up, Click or button callback reached Next.

**Last confirmed boundary:** the module's Click action notification, with actual
raycast NONE. **First missing boundary:** PointerDown delivery to Next. A shared
mouse pointer handoff is a supported explanation from the installed module:
`SingleMouseOrPenButMultiTouchAndTrack` shares mouse position, and
`OnPointCallback` updates that position. The trace does not contain the module's
internal position for this earlier revision, so this explanation remains an
inference. Focus had returned by attempt 51; simply blaming its failure on an
unfocused app would be inaccurate. This is a new observation, not a demonstrated
cause of historical A or B.

**Demonstrated harness defect and correction:** `ModuleClick` logged zero
callbacks but allowed its caller to continue. It now requires one callback for
an enabled action and zero only for an explicitly disabled-control probe. A
mismatch throws immediately after the existing observation window; the runner
flushes and exits. It never supplies a second click. Existing successful buffers
contain zero callbacks only for their explicit disabled Continue/Confirm probes.
No production handler or input policy was changed to compensate.

### Reserved comparison: Brew failed before the negative fixture

The one reserved comparison ran at 720p. `RunPointerHandoffDiagnostic` uses the
existing snapshot, guest/request setup and ModuleClick runner. It is designed to
compare a normal Brew with a deliberately competing diagnostic mouse on the same
request, clearly labeled as a negative fixture. **The negative fixture was never
reached.** Its first ordinary Brew failed and the corrected harness stopped on
attempt 1 at **14:29:46**, proving immediate failure retention instead of the
previous farewell retry loop.

[Complete failed Brew trace](../Logs/Review/phase-m-event-trace/traces/20260929T142937378-pointer-handoff-FAIL.txt),
correlation `20260929T142937378-pointer-handoff/1/BrewButton`:

| Frame | Monotonic seconds | Boundary / actual state |
|---|---|---|
| 3234 | 13.340846 | Active/interactable Brew; no modal/pause lock; appFocus=false, no focused Editor window |
| 3234 | 13.344641 | Queue position (892,56), released, virtual device 22 |
| 3234 | 13.351835 | Event received before application |
| 3235 | 13.352637 | Dynamic post-update position remains (0,0), left=0 |
| 3235 | 13.353140 | Queue press; event received at 13.355273 |
| 3236 | 13.355911 | Dynamic state still (0,0), left=0 |
| 3236 | 13.356484 | Queue release; received at 13.358235 |
| 3237 | 13.358727 | Dynamic state still unchanged; module pointer records empty |
| 3372 | 13.657807 | Zero callbacks; selection inactive |
| 3372 | 13.659230 | Immediate delivery-mismatch exception, then failure flush |

**Last confirmed boundary:** raw virtual event receipt before application.
**First incorrect/missing boundary:** the queued position/press never appeared
in the player's Dynamic state; no Point/Click action, Down/Up/Click dispatch,
Brew callback or selection-open request followed. A callback guard did not reject
this attempt: the callback was never entered. No panel opened then closed.

The installed policy was read without changing it:
`ResetAndDisableNonBackgroundDevices`, `PointersAndKeyboardsRespectGameViewFocus`,
`ProcessEventsInDynamicUpdate`. Input System 1.18.0's `InputManager.cs` around
line 3323 routes pointer/keyboard events to Editor updates when the game lacks
focus; `InputSystemUIInputModule` then has no player pointer to dispatch here.
This establishes an upstream input-routing limitation for **this new unfocused
Brew attempt**, consistent with the recorded unchanged player state. The exact
reason the Editor lost focus was not established. No focus, action binding,
background policy or device was changed to make the attempt succeed.

**Historical B remains UNRESOLVED:** its snapshots had app/Game View focus and
lack the per-boundary evidence above. A transient loss or device handoff could
only be a hypothesis for that older run. **Historical A also remains UNRESOLVED.**
No relationship between A and either new failure was demonstrated. The budget
ended here; the planned final 1080p InputModuleFlow was **not run**. The controlled
second-mouse comparison remains **VALIDATION_DEBT**, not a passed regression.

### Tested source identity and final coverage

Executable diagnostic commit: **`bf89c6dbea1a93191c0b4a0d6742ebc0ad887c2f`**.
The later handoff commit changes this document only. No executable edit follows
the final compilation, failed Brew trace and physical regression.

The exact Editor test source bytes differ across the bounded investigation:

| Test source SHA256 | Evidence |
|---|---|
| `44feab960afbec0fa7a02221063c17ca3cc19105adf72f0c318af672ab648793` | Seven initial budget probes passed; unchanged-frame sampling still present |
| `3b2064ae86266e3f166f6193a1107f6f88f7352b9f52e229f3a6383294badc23` | Restored strict Settings checks and reduced sampling; final navigation failed at Next |
| `35b76f25201c31495496af670385eb7d14d4176de28e5ff4f29d4e20fd9ad862` | Final commit above: stop on missing callback, module instance/pointer records, bounded comparison entry; first Brew failed and stopped |

The three production files contain only conditional Editor observations; their
runtime guards, selection logic, modal ownership and Settings behavior are
unchanged throughout these runs. The source/settings manifests distinguish the
original and recovered copies. A final comparison still matches 1,251/1,251 files.

- **Compile/static:** final C# compile PASS, zero errors/eight existing warnings;
  scoped and staged diff checks PASS. No scene/prefab, art or package edits.
- **Callback-only:** existing guest/dialogue/checkpoint setup and older direct
  nested-overlay fixtures remain fixture evidence. They do not establish input
  delivery. No fallback callback was used after a module click failed.
- **Programmatic pointer/input module:** initial seven-probe budget passed both
  resolutions and all targets. Final navigation and Brew comparison failed as
  detailed above; no complete final-revision UI pass is claimed. The immediate
  failure guard is demonstrated by the final one-attempt Brew failure.
- **Physical:** final existing Phase K Play Mode regression passed Build, Stir,
  Shake and Float measurement/overflow/source-transfer/tool checks. Input is
  programmatic through physical seams, not human mouse input. The original-order
  chain also ran physical and campaign static regressions; the full campaign was
  not repeatedly played.
- **Rendered inspection:** no new aesthetic review or approval. Existing
  [staging review](../Logs/Review/phase-m-staging-review.html), hover and selected
  captures remain unchanged. Staging probes rendered the current production UI;
  their success is not substituted for visual inspection.
- **Standalone:** no new build or standalone run. Conditional diagnostic calls
  are excluded from player compilation. The old executable is not new evidence.
- **Human physical input:** not tested or claimed.

Final independent snapshot read-back at **14:34:40 UTC** verified progress,
knowledge and sound preferences after all tests. The private snapshot hash
matched the one retained before copy recovery. This verifies the fixture's
snapshot restoration, not unobserved concurrent edits by another Editor.
After normal test exit: no pending fixture, diagnostic sink, pause owner,
active modal or Settings; time scale 1. No manual state clear produced that state.
61/63 baseline files retain their initial byte hashes; the two unrelated changes
listed above remain excluded. Protected config is unchanged and unstaged.
No task-generated importer/scene/settings changes are committed. Local test-copy,
trace, snapshot and build/cache files are excluded. The owned validation Editor
closed normally after verification; no agent action closed the owner Editor.

Safe artifacts: `Logs/Review/phase-m-event-trace/` contains trace buffers,
source manifests, original-evidence hashes and the result index. The private
snapshot is separate and is not linked as a public review artifact. Original A/B
logs still exist with recorded hashes; they were not purged or rewritten.
**Disposition:** diagnostic coverage and the harness stopping correction are
implemented; A/B causality and final complete UI regression remain open validation
debt. No new gameplay or presentation fix is claimed.

## Focused input preflight — 2026-09-29

**Disposition: BLOCKED_TEST_ENVIRONMENT / VALIDATION_DEBT.** The 720p preflight
passed and 26 subsequent clicks reached their callbacks. Unexpected application
focus loss stopped the first navigation sequence. No retry or 1080p run followed.
Historical **A (Settings-close mismatch) and B (Brew activation failure) remain
UNRESOLVED**, independently. This run establishes no causal relationship between
them and does not retroactively explain either failure.

### Reconciled source and settings

- Start: `5a748765168887eb226986bf6af04dc291ffaaee`, branch
  `refactor/project-rebuild`; diagnostic predecessor `bf89c6d`.
- Actual baseline: 63 dirty paths. All 63 retain their starting SHA256 after this
  task, including protected `.codex/config.toml`. No baseline path was staged.
- The earlier changed path `ProjectSettings/ProjectSettings.asset` lost the
  `preloadedAssets` reference `{fileID: -944628639613478452,
  guid: 2bcd2660ca9b64942af0de543d8d7100, type: 3}`. Re-inserting that one entry
  into the current bytes with LF reproduces the previous SHA256 exactly:
  `3c96e7c360c7b2eb8f7b0874d2b56bb4acf177acf648dedbe70aba65cf952d61`.
  Current original/copy SHA256 is
  `7164a495ab0f50825079f51dec8bdc2e70b531a0341121d868cda53434cda827`.
  This is a functional preload-list difference, not whitespace. Authorship is
  unknown; no restoration or normalization was performed. The remaining Git
  difference is Standalone batching (static 1, dynamic 0).
- Current BarScene's UI module explicitly references the separate action asset
  `ca9f5fa95ffab41fb9a615ab714db018`. No project code consumer of
  `InputSystem.actions` was found. The missing preload is therefore not a missing
  reference for this UI module; its actions were enabled and delivered input in
  this run. This does not establish equivalence with the older preload behavior.
- The other earlier changed path is `UserSettings/Layouts/default-6000.dwlt`.
  The retained `Logs/PhaseLRestartBaseline/` copy matches the previous hash
  `83e0f8340d11261bec227c870506d408f5f511be9cb9d1a930ad58e00dc1e1f6`.
  Changed serialized keys are `x`, `y`, `controlID`, `m_Title`, `m_Data` (Editor
  geometry/control identities/stored view data). Embedded data was not printed.
  Current hash is `86bd9574a92eaaf1503bb377e6dce00858b920bcd6970a39258ba691ea75fddd`.
  The test copy has its own layout; focus was observed live rather than inferred
  from either layout. Both original files remain excluded and unmodified here.
- Tested project: `Temp/PhaseMDiegetic/ValidationProject`. All **1,251/1,251**
  files under Assets, Packages and ProjectSettings matched the original at run
  start, including dirty generated assets and metadata. Separate Library,
  UserSettings and fixed-resolution Game View are testing-only differences.
  PlayerSettings: input handler Both, runInBackground false, 1920×1080 default,
  fullscreen mode 1. No original setting was changed to accommodate testing.

### Preflight and finite budget

`RunForegroundNavigation()` reuses `ModalSequence`, its event ring buffer,
fail-fast callback check, Settings assertions and save snapshot/restore. Budget:
one 720p and one 1080p run, four consecutive routes per run, one overlay/Back
cycle per route. Stop the entire budget at its first block/failure. No campaign
replay or deliberate focus-loss scenario was included.

One native foreground request targeted only the newly launched validation Editor
(PID 41308); the observed foreground PID matched. Game View focus was requested
before Play Mode, never by a Brew click. Runtime readiness requires application
and Game View focus, an unpaused Editor, enabled UI actions, advancing Dynamic
updates, virtual mouse state, Point action delivery, and the input module's actual
pointer-enter/hover records on the harmless Codex target. Deadlines use unscaled
time (10 seconds), without forced updates or retry clicks. This mode retains
`Application.runInBackground=false`; older harness modes' override is not used.
Focus is monitored through application-focus callbacks, input updates and frames.

Actual installed/runtime configuration: Input System **1.18.0**, uGUI **2.0.0**,
`InputSystemUIInputModule`, `ProcessEventsInDynamicUpdate`,
`PointersAndKeyboardsRespectGameViewFocus`,
`ResetAndDisableNonBackgroundDevices`. No input/background policy was modified.

### Observed sequence and first incorrect boundary

Trace: `Logs/Review/phase-m-preflight/20260929T145613251-foreground-navigation-FAIL.txt`
(1280×720, no dropped buffered events). `timeline.txt` is its compact index.

| Boundary | Evidence |
|---|---|
| Preflight, frame 3114 / monotonic 11.861394 | Ready: two Dynamic updates, Point action, device 14 and UI pointer at (56,668), Codex hover; focus true |
| Attempts 1–9 | Idle Codex and Settings open/close, guest/dialogue input delivered |
| Attempts 10–18 | Brew, all four setup selections/hover checks, Back, reopen, Stir confirmation; request comparison passed |
| Attempts 19–22 | Guide and bartending Settings open/close delivered |
| Settings close, frame 6016 / 19.232745 | Close complete; expected/actual scale 1/1, top NONE/NONE, paused false/false, input allowed true/true, interactable true/true |
| Attempt 26, frame 9234 / 26.435774 | Final Stir service-button attempt completed with exactly one callback; route had returned inactive |
| Frame 9965 / 27.963988 | First incorrect precondition: application focus false while Editor focused-window type still GameView; immediate stop, no further click |

The focus-changing actor is **unproven**. No external interference or
application-induced cause is assigned. Departure completion and the next guest
were not reached. Shake/Build/Float **selection targets** were exercised, but
their route entries were not; 1080p and repeated four-route completion remain
blocked. Both current Settings-close checks passed separately; this does not
resolve historical A. Both Brew attempts delivered; this does not resolve B.

### Revision, preservation and coverage limits

Runtime-tested tree: starting HEAD plus the Editor validator whose SHA256 is
`309979e9db84e3ead09e3843a486e80dd02888eac824b6b29448617d35636444`.
The retained `tested-ui-validation.cs` and `source-manifest.json` identify those
exact bytes. Final validator SHA256 is
`9a27d69363fe5d885f7858c7c27991f3a06cff3058c8de405a8c69a9e00f702e`:
after the stop, a small cleanup reused the existing Point observer (avoiding a
local subscription surviving interruption) and classified absent preflight
module/actions/target as an environment block. Those final changes are
compile-reviewed only; no final-source runtime replay is claimed.

- **Compile/static:** final C# build, zero errors/eight existing warnings;
  intentional two-file diff, no production/scene/asset/settings changes.
  Scoped/staged diff checks pass. The whole-worktree check still reports the
  preserved baseline sprite/font trailing whitespace; it was not normalized.
- **Callback setup:** existing initial Night 5 checkpoint and Continue fixture;
  not normal-play reachability evidence. Preparation uses existing physical
  action seams. No callback fallback followed a missing input event.
- **Programmatic input module:** partial 720p coverage above; all 26 attempts
  delivered once. No complete navigation PASS. The previous missed-callback
  fail-fast correction is retained; unrelated Phase K/static results are not
  invalidated. No additional physical regression or full campaign run here.
- **Rendered / standalone / human:** no new visual review, build, standalone run
  or human physical-input test. Countertop staging and production logic unchanged.
- Independent snapshot read-back verified progress, knowledge and sound values
  after exit. Pending fixture, diagnostic sink, modal, Settings and pause all
  cleared through ordinary teardown; no manual lock clearing. Save content stays
  private. `save-preservation-final.txt` and `preservation.json` record checks.
- Only allowlisted process metadata was observed. No process arguments, token,
  environment or authentication content was printed. Prior exposure evidence
  remains unchanged; no claim of credential expiry/safety or revocation is made.

Remaining execution needs an uninterrupted foreground **Game tab in the
ValidationProject Editor**, without switching windows during the bounded run.
No automatic refocus/retry will conceal interruption. Full traces/manifests are
under ignored `Logs/Review/phase-m-preflight/`; private save snapshots are separate.

## UI visual finish — object surfaces and utility controls (2026-10-01)

Starting HEAD `eb8e198`, branch `refactor/project-rebuild`; 63 pre-existing dirty
files inventoried and hash-protected, including `.codex/config.toml`. No owner
Editor was closed or modified. No connected Editor was found at task start;
one hidden batch Editor used the existing isolated ValidationProject. The
completed A/B/C harness investigation was not repeated. Docs/24's final harness
validation remains authoritative; historical Settings/Brew incidents remain
unresolved and are not attributed to or resolved by this visual work.

### Visible scope

- Retained all tool groupings, counter anchors, footprints/contact shadows,
  compact labels, request information and small Start/Back controls. Removed
  the counter-light oval and broad tool halo. A local uGUI shader follows the
  sprites' material alpha edges and existing bright planes. Idle uses original
  material colors; hover/focus .55 and stable selection 1 use the existing fade.
  Glass interiors keep their alpha. Blocking/disabling clears transient visual
  focus/hover without changing selected method or calling a route callback.
- Replaced the **open-button** book/settings illustrations with original ivory
  book/gear geometry, 48-unit charcoal backing and 28-unit glyphs inside the
  unchanged 64-unit hit targets. Hover, press, focus, open and disabled states
  use the existing palette; open state reads the real Settings/Codex owners.
  Existing simple close/back/workspace controls already share that palette and
  were retained. No additional listeners or modal state were introduced.
- The owner's latest permission explicitly supersedes older utility-icon
  preservation language. Shaker pixels/concept, Codex content art/glasses and
  liquid colors remain unchanged, as do environment, characters, mechanics,
  requests, knowledge and saves. Provenance: deterministic uGUI mesh and shader,
  not imagegen. See `ArtSource/Pixel/UIVisualFinish/README.md`.

### Evidence and rendered review

- [Before/after native-crop board](../Logs/Review/UIVisualFinish/before-after-board.png).
- [Full-resolution comparison and native crops](../Logs/Review/UIVisualFinish/before-after.html).
- Individual files: `Logs/Review/UIVisualFinish/phase-m-visual-{before,after}-`
  `{counter-idle,Stir-hover,Shake-hover,Float-selected,Float-selected-Stir-hover,`
  `idle-bar,settings-open,codex-open,codex-content}-{1280x720,1920x1080}.png`.
  Utility hover/pressed/focus/disabled captures are retained separately too.
  `native-crops/` contains unscaled object/button crops at both resolutions;
  `capture-sha256.json` and `validated-source-sha256.json` identify exact evidence.
- These are actual Unity production-scene renders with seeded Night-5 setup
  and direct UI events/callbacks. They are not human play or pointer-delivery
  evidence. Guest walking position can differ in utility screens; counter,
  camera and control framing match. No new recording infrastructure was added.
- Initial batch captures exposed a **capture-only** mismatch: projected tools
  retained 640×480 Game View coordinates during 16:9 offscreen rendering. The
  existing capture helper now refreshes counter projection after changing and
  restoring camera/Canvas dimensions. Initial files remain in `initial-projection/`.
  Final BEFORE/AFTER both use the same corrected helper; the four original
  visual source files were rendered only in the isolated copy, then final
  source restored. Main repository HEAD/source was never rolled back.

Rendered inspection: **the objects receive the emphasis**, following glass rims,
bases, spoon/Jigger edges and Shaker material planes. Transparent interiors stay
readable; no texture-bound rectangle or light disc carries selection. Existing
footprints keep the tools on the counter. Labels remain secondary. Native-size
book and gear silhouettes are recognizable and share padding/palette. The
Shaker remains recognizable; the sampled Codex illustration region is exactly
pixel-identical before/after. These observations do not constitute owner approval.

### Executed validation and limits

| Check | Result / boundary |
|---|---|
| Final compile | `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: 0 errors, 8 existing warnings. `Temp/UIVisualFinish/compile-final.log`. |
| Shader/reference | Resource resolves, supported shader, zero ShaderUtil messages; eight tested source/asset files match main and isolated copy byte-for-byte. No scene/prefab/import changes. |
| Final AFTER fixture | PASS at 11:11:45 UTC: four selection-to-confirmed-route mappings, Back/re-entry, Settings/Codex open/close/detail/back, modal raycast blocking, nested pause restoration, stale hover clearing, non-raycasting decoration, 64×64 utility targets and rendered utility states. Direct UI events/callbacks only. |
| Matched BEFORE fixture | PASS at 11:14:05 UTC with starting visual source and the same capture helper. |
| Retained development result | First BEFORE run at 11:02:26 captured all requested views, then failed when its newly written callback fixture queried Brew in the same frame as Back. Added one explicit .26 s layout/reading-boundary yield before the first query. No failed click was silently retried and no pointer harness/input policy changed. Result retained in `before-fixture-result.txt`. |
| Save/audio and cleanup | Every exit used existing snapshot restoration; logs confirm progress/sound restored and verified. Final query: not playing/compiling, no pending fixture, observers or dirty scene. |
| Protected baseline | All 63 starting dirty-file hashes match. Historical archives and Docs/24 unchanged; no config contents exposed. |
| Diff | Task-owned diff/serialized changes reviewed; task `git diff --check` clean. Full non-config check reports existing whitespace in 57 protected baseline files only; those files were preserved. No scene or prefab was edited. |
| Pointer debt | Corrected queued-mouse regression **not run**: only a hidden batch Editor was available, without uninterrupted foreground Game View. Existing relevant entries are `RunStagingAfter`, `RunDiegetic`, and `RunInputModule` in `SURLHWAPhaseMUIValidation`; A/B/C need no rerun. Render dimensions do not establish input coverage at 720p/1080p. |

Status: **IMPLEMENTED_WITH_VALIDATION_DEBT**. HUMAN_VISUAL_APPROVAL_PENDING.
CHARACTER_ANIMATION_DEFERRED. DEFERRED_AUDIO. No full campaign, standalone build,
historical-incident resolution, human aesthetic approval or final game closure
is claimed.


## J5 feedback/farewell sequencing follow-up (2026-10-01)

Starting source `97b71a1`. The retained Phase N J5 failure is reproducible with
unchanged production code: the focused fixture changes cadence from 30 to 10
FPS while discovery is visible. At t=16.358 discovery opens; at t=19.525 the
farewell is entered while discovery is still active. Uncapped and fixed-30-FPS
baseline runs passed, confirming that this is timing-sensitive. The captured
popup had reached its offscreen position but remained active until the final
coroutine completion; the image alone does not prove visible panel obstruction
or a modal lock. No Phase N causation is established. Original and new
baseline logs remain archived.

`OrderManager` previously scheduled both feedback coroutines independently and
returned their summed nominal durations. `CustomerController` waited for the
remaining estimated time before entering farewell. Nested coroutine yields,
slide/fade frames and timed holds do not complete at that exact sum. Neither
presenter signaled actual completion; this was an estimate race, not a missed
popup acknowledgment, duplicate unlock or modal-lock failure.

The current UI contract remains a nonblocking, automatically dismissed toast
and discovery notification (no acknowledge button or modal shield). The order
is service resolution/knowledge persistence -> diagnostic toast completion ?
discovery notification completion when newly Known -> authored farewell ?
player reads every line -> existing stand/walk/departure and visit completion.
The established initial reaction delay and small inter-feedback gap remain.

Both existing presenters now signal completion after hiding. Their callbacks
are cleared before invocation, and replacement/disable/missing presentation
releases the pending request once as cancellation. A canceled toast does not
open a late discovery notification. `OrderManager.PresentServiceFeedback`
chains these callbacks and returns a request-local completion predicate.
The customer awaits that signal at the existing farewell seam. There is no
GameObject visibility polling, new modal coordinator or progression authority.
The old immediate popup fallback without a toast was removed because the same
service also reached the normal discovery scheduling path; that path now owns
presentation once even when the toast is missing.

The existing J5 fixture now records actual completion order and counts,
checks Known persistence remains unchanged throughout presentation, exercises
known/no-popup services and cancellation, and uses real closing departure and
seat release. The first two visits retain the older direct-departure fixture;
closing guests start already seated as an explicit setup shortcut. Farewell
Next/Brew/Back checks use callbacks, not OS pointer delivery. Evidence and final
executed results are recorded in Docs/25 and `Logs/Review/J5FarewellDiscovery/`.
