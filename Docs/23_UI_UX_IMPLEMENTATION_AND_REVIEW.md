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
