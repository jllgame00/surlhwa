# RC automated gameplay QA

Baseline: `92ff5d4`, `refactor/project-rebuild`, Unity `6000.3.7f1`.
Completed 2026-10-04: **RC_AUTOMATED_QA_PASS_WITH_NATIVE_DEBT**.
No known P0/P1 remains after the executed audit. This is bounded automated
evidence, not a guarantee of no undiscovered defects or human approval.
This record supplements the historical production audits;
it does not reinterpret old fixture passes as keyboard delivery or human approval.

## Architecture and reproduction

`SurlhwaGameplayInput` forwards raw keys and cursor position to legacy Unity
Input by default. Its exclusive injectable source exists only in the Editor or
an explicit `SURLHWA_RC_QA` build. Modal permission, preparation, liquid state,
recognition, FIT, knowledge, supply and campaign ownership remain unchanged.

`SurlhwaRCGameplayQA` runs enabled production method components and their
Update/input gates. It injects key edges, held keys and cursor trajectories,
then observes physical coroutines, actual containers, preparation traces,
recognition, completion and unloading. Buttons receive EventSystem Submit
handlers. These are **SIMULATED_PRODUCTION_INPUT** and **PRODUCTION_HANDLER**,
respectively; neither is native device delivery. Recipe fixtures explicitly
open production routes and provide guide knowledge. The separate campaign
suite establishes earned learning and scheduled encounter reachability.

The existing campaign, J5, physical, UI, motion and audio validators remain
authoritative for their assertions. Their direct technique/service fixtures
are **LOGIC_ONLY_PASS**, not evidence that legacy polling received a key.
Existing UI virtual-device tests retain their validated point/raycast/press/
release/callback architecture. They are simulated Input System pointer delivery.

Run in a disposable current-source project at
`Logs/Review/RCAutomatedQA/ValidationProject`, excluding Recovery. Set only the
copy's company/product to `SURLHWA_RC_AUTOMATED_QA` / `Isolated_20261004`, for
separate preferences. Do not launch these destructive save fixtures in the
owner's Editor. `Tools/Invoke-RcUnity.ps1` checks the local Editor descriptor's
project identity and never prints its authentication token.

```powershell
Tools/Invoke-RcUnity.ps1 -Action Run -Suite static
Tools/Invoke-RcUnity.ps1 -Action Run -Suite focused
Tools/Invoke-RcUnity.ps1 -Action Run -Suite stress
Tools/Invoke-RcUnity.ps1 -Action Run -Suite campaign
# Also: physical, audio, motion, motion-safety, ui, ui-review, modal, navigation, j5
Tools/Invoke-RcUnity.ps1 -Action Status
# Sequential non-pointer batch; stops on the first failed suite:
Tools/Invoke-RcRegression.ps1
```

Wait for each result before starting the next suite. Recompile only while idle.
The dispatcher refuses stale assemblies after a compile failure, requires each
suite's terminal PASS marker, and records all errors/warnings. A diagnostic
watchdog reports failure; it never advances gameplay. Existing delayed Editor
runner attachment was changed to the EnteredPlayMode boundary because an
unrelated relocated Search index exception can discard delayCall callbacks.

## Executable inventory

| Suite | Scope | Current evidence |
| --- | --- | --- |
| focused | 4 M/transfer/next-action regressions; 2 modal regressions; 9 exact recipes; 12 seeded method cases | 27/27 PASS, 664 discrete input cases, 109 Submit callbacks |
| stress | Each method: two reloads during pending/active measurement, then exact completion | 4/4 PASS; 110 inputs, 32 Submits; 8 interrupted-route reloads and 4 successful completions |
| static | Integrated campaign/J4/physical/motion references; recipe IDs, ingredients, steps, scenes, prefab scripts, font | PASS including ordinary emotion corpus |
| ordinary corpus | Seeds 92, 49524, 20261004; 128 requests each; valid distinct tags and no repeated previous primary | PASS: 384 requests, all 10 primary emotions and 30 pairs |
| campaign | 18 nights, 47 natural departures, all ACTs, discovery retry, memories, ending, Continue | PASS; 47 natural departures; zero gameplay errors/warnings |
| physical / audio / motion / motion-safety | Existing production integration, disabled-owner, fallback and cleanup assertions | PASS |
| UI / ui-review | All requested screen groups at 720p/1080p; method Back/re-entry; summary/Codex return; ending/reset navigation | PASS; layout/raycast/callback fixtures, not native delivery |
| modal | Four consecutive services, 12 combined overlay/Back cycles, live module raycast/press/release | PASS; 204 virtual pointer cycles, every callback exactly once |
| J5 | Six service/quality/FIT cases, all Prince failure positions, popup/farewell, low cadence/long audio | PASS; natural departure and exactly-once feedback |
| Windows focused / stress | Same enabled production-input suites in compiled Player | 27/27 + 4/4 PASS; 774 key pulses and 141 Submits per environment; stress process exit 0 |
| Windows native / mixed UI | Real keyboard and Stir cursor; simulated UI pointer; New Game, service, Settings, Codex, Continue | PASS for the stated boundaries; OS pointer delivery remains environment debt |

Never add counts from historical runs to current-source totals. Reproduction
runs and harness failures remain in evidence, separate from final green runs.
The 31 distinct new scenario definitions ran in both Editor and Windows Player;
774 is the number of discrete key pulses, not 774 distinct named states.
The focused/stress pair opens 39 routes, including eight interrupted measurement
reloads and four subsequent completed re-entries. Existing suite counts above
are separate and are not added to manufacture one inflated scenario total.

## State and input matrix

| State group | Driven input / invariant | Executable owner |
| --- | --- | --- |
| Bar: no guest, entering, seated, interaction available | Natural spawn/seat/interaction; no encounter starvation | Campaign; motion |
| Dialogue, request, method selection | Dialogue completion; premature/duplicate/reopen selection; Back twice; available methods | Campaign; Phase M modal |
| Settings, Codex, guide | Open/close/reopen; top raycast; underlying key blocked; knowledge unchanged; focus restored | Focused; Phase M |
| Discovery, result, farewell | Feedback ordering; nonblocking decoration; one completion; read before departure; slow-frame/long-clip cases | J5; campaign |
| Departure, day result, ending | Natural seat release; next guest/day; final coda; no night 19 | Campaign |
| Route loading, preview, ingredient selection | One enabled method/EventSystem/listener/soundscape; supplied ingredient button; reachable primary control | Focused; physical |
| Jigger inactive/measuring/filled/transfer | M on/off; empty T; partial/full/overflow; conserved T; repeated T; M stays on; next I/D/J/technique | Focused |
| Pending measure / active pour on reload | Incompatible I/D/J/M rejected; Settings blocks Space; unload and reopen twice; no completion from abort | Stress |
| AddIce | Fresh, held, repeated; exactly one trace/cue; wrong technique phase rejected | Focused; audio |
| Stir | Premature finish rejected; real cursor orbit; invalid preparation keys leave liquid unchanged | Focused |
| Shake | Timed alternating arrow input; end, strain or unstrained transfer | Focused |
| Build / Float active and inactive pour | Space edges/hold; Float modifier; actual ratios/layers; gentle stir | Focused; physical |
| Strain, Dash, Splash, PourFromContainer, garnish | Authored keys/Submit; tool completion; actual preparation evaluation | All nine recipe cases |
| Result, unload, re-entry | Repeated method keys cannot re-complete; route audio stops; fresh route has no old liquid/tools | Focused; stress; audio |
| Focus loss / OS hardware | Actual OS input and visible standalone frame, if available | Native smoke; independently labelled |

The matrix uses equivalence classes, not an exhaustive Cartesian product.
An action differing from the expected recipe is not automatically illegal:
production permits player interpretation and records actual preparation for
mismatch/retry. Tests reject physically/state-invalid actions without turning
recipe metadata into a mandatory answer machine.

There is no production Cancel binding for an active drink. Supported method
selection Back is tested through real UI. Active-route interruption is tested
through production scene restart and explicitly called reload, not keyboard
Cancel. No new cancellation design or persistence format was introduced.

Fuzz seeds are **92 / 49524 / 20261004**, each run on Stir, Shake, Build and
Float. Diagnostic frame deltas are 30 / 144 / 60 Hz respectively. Each case
uses 28 reproducible invalid-key choices, two guide/settings cycles, finite
measurement and held I. Existing J5 additionally stresses low-cadence popup
completion. This is bounded coverage, not arbitrary-duration fuzz certification.

## Recipe and progression reachability

The active campaign has nine recipes: Seolya (`recipe_test_stir`), Manwol,
Dry Martini, Manhattan, Negroni, Gin Fizz, Hanky Panky, French 75 and Last Word.
Every one has completed through enabled production input handlers with actual
composition recognition and satisfied preparation. The campaign separately
checks their learning/supply/scheduling context and recommendation alternatives.

`recipe_janseol` and `recipe_test_shake` remain catalog reserve members with
missing authored preparation arrays. They are explicitly inventoried as
`OUT_OF_SCOPE_RESERVE; INCOMPLETE_PREPARATION_METADATA`, not counted as reachable
campaign recipes and not deleted or silently repaired. Their future promotion
needs bounded authored preparation, supply/knowledge and normal-play proof.

## Reproduced defects

| ID | Severity | Reproduction / cause | Fix and permanent regression |
| --- | --- | --- | --- |
| RC001 | P1 | M, measure, T, D on Manhattan or J on Negroni: idle measurement still consumed method input after the earlier I-only repair | Idle measurement releases unowned input; existing pending-measure/tool guards remain. Four `measurement-shortcuts` cases, including I and next Stir/Shake. |
| RC002 | P2 | Open Settings, disable its owning object: invisible global settings owner still gated input | OnDisable closes/relinquishes Settings. `disabled-settings-owner` verifies cleanup and reopen. |
| RC003 | P1 | Hold Space, open Guide, release inside Guide, close and press fresh Space on first available update: stale held-key set swallowed the new press | Modal boundary prunes released keys. `held-key-modal-reentry` intentionally has no idle polling frame before the fresh press. |

Production fixes: `4e2ddff`. Original failing logs/snapshots are retained under
`reproduction-RC001`, `reproduction-RC002-and-first-sweep`, `reproduction-RC003`.
Fixture corrections (reserve classification, physical center lookup, float
tolerance and screenshot directory) are retained separately; they are not
reported as production bugs or retried without diagnosis.
Standalone capture/focus helpers and the comprehensive UI dispatcher's terminal
marker were also corrected as QA issues. The latter now recognizes the existing
fixture's `COMPLETE` marker, emitted only after its runtime-error check; no
gameplay assertion was weakened. Original wrapper failure logs are retained.

## Evidence and limitations

Local artifacts live under `Logs/Review/RCAutomatedQA/`; suite artifacts and
screens live under the isolated project's corresponding folder. JSON reports
give per-scenario status/input/Submit counts. Traces include accepted input
observations, state transitions, completion counts and invariant checks.
Soft-lock snapshots include scene, route, customer/order/recipe, modal/focus,
raycast, seats, enabled method, containers/Jigger, pending completion delegate,
last input, last transition and trace tail. Diagnostic waits are attached to
explicit expected transitions; authored reading waits are driven separately.

Protected owner state: 67 unique files in 71 baseline entries (four Recovery
paths were recorded using both slash styles), including configuration, settings,
layout, art metadata, font and all discovered Recovery artifacts. No such file
is staged. Original byte backups and hashes are local evidence only.

The Korean audit found **570 distinct Hangul characters**, all available from
the actual KoreanFallback/source font chain in both Editor and the packaged
Player. Visible-window Player framebuffer captures at 1280x720 and 1920x1080
show populated Korean text and a rendered scene. No missing-glyph warning was
observed. The NativeFormatImporter cache inconsistency still appears during
build; packaged glyph/render proof establishes its current lack of player impact.
The protected owner font was not rewritten. Typography taste is human-owned.

`Missing RuntimePipelineConfig` comes from the optional `com.unity.pipeline`
player command/hot-reload server build processor. Missing configuration disables
that server in Player builds; it is unrelated to Unity rendering. Do not enable
a runtime remote-command server merely to remove its tooling warning.

The relocated Editor SearchDatabase startup exception is logged as
`ENVIRONMENT_ONLY`, with the exact original stack retained. It does not excuse
any gameplay exception or missing suite terminal evidence. In the isolated
process only, later suites detach that exact pending SearchInit startup-index
callback before Play Mode. Editor preferences and gameplay callbacks are not
changed. This prevents an Editor tooling failure from contaminating legacy
validators' runtime exception observers.

## Windows package and input proof

Final QA package: `Builds/Windows-RC-5bdc0b1/SURLHWA.exe`, source
`5bdc0b10034fc44b53b502799cb6d5f58ea5dda0`, Unity 6000.3.7f1, Windows x64,
2026-10-04 12:09:12 UTC. Build succeeded in 10.183 seconds with two explained
warnings (font importer and optional Pipeline server). It is a Development
QA build with opt-in instrumentation, separate preferences, and no remote server.
Later changes only expose the existing comprehensive Editor UI fixture, guard
against stale runner PASS, and update this handoff; gameplay/Player code matches.

The package's `source-inputs.json` records 1,504 input hashes: committed code,
preserved owner art/font overlay, isolated company/product, and Unity-generated
URP prefilter/runtime resource tables in the copy. Those two generated assets
were inspected separately and were not applied to the original repository.
This is a known code revision with explicit local asset provenance, not a claim
that the owner's intentionally dirty asset/settings worktree was pristine.

| Boundary | Actual evidence |
| --- | --- |
| OS keyboard | `KEYBOARD_DELIVERY_PASS`: focused Player, Windows SendInput, legacy and Input System key-down observations; native Return/New Game, Escape Settings, M/held Space/T/I/I |
| Native gameplay cursor | OS cursor orbit reached legacy `Input.mousePosition` and the enabled Stir Update; final 1080p run recorded 1297.75 degrees |
| OS pointer buttons | `VALIDATION_DEBT`: another process, PickerHost PID 44688, owned the target hit region despite foreground recovery and moving only the QA window; guard prevented input being sent to that process |
| Simulated UI device | `POINTER_DELIVERY_PASS (SIMULATED_DEVICE)`: real InputSystemUIInputModule, live pointer/raycast/down/up/click/callback; explicit `--surlhwa-rc-ui-device` only |
| Simulated gameplay input | 31 cases in Editor and compiled Player through enabled production Update/input gates; this is not a native-keyboard claim for every recipe |
| Logic fixtures | Campaign, audio, motion, J5 and physical technique seams retain `LOGIC_ONLY_PASS` where commands are called directly |

Native service was completed at 720p on `9186ad3`, then repeated after Continue
at 1080p on final `5bdc0b1`. Both used an intentionally incomplete Whiskey drink:
unknown/mismatch response, natural departure, seat reuse and next interaction
all worked. Both retained M after transfer, accepted I once, and rejected repeat
I without another action. Nine exact recipes completed separately in the final
compiled input suite. The full 47-encounter campaign was Editor automation;
there is no claim of a full native standalone campaign.

Reproduce compiled suites from the repository working directory:

```powershell
Builds/Windows-RC-5bdc0b1/SURLHWA.exe --surlhwa-rc-focused
Builds/Windows-RC-5bdc0b1/SURLHWA.exe --surlhwa-rc-stress
Builds/Windows-RC-5bdc0b1/SURLHWA.exe --surlhwa-rc-native --surlhwa-rc-ui-device
```

Run one Player at a time. The last command observes real legacy keys and adds
an explicitly simulated UI Mouse. `Tools/Invoke-RcNativeInput.ps1` validates the
target executable/foreground; `Tools/Send-RcUiDevice.ps1` sends the existing
released-move/press/release sequence. Neither drives gameplay commands.
Native observer snapshots follow the method's authoritative shelf reference
even when technique presentation hides its GameObject.

## Saves, cleanup and final dispositions

The campaign exercised eight Continue entry points: night 3 start; nights
6/10/15 after slot 1; interrupted night 8 slot 2; interrupted and completed
night 18 ending; legacy four-night completion into night 5. It also tested
New Game/reset, all 47 departures and applicable visit flags, earned knowledge,
seven memories, and no night 19. First four nights intentionally resume at the
night boundary; later nights preserve departure slots. Mid-drink/animation
restoration is not a current persistence contract. Standalone day-1 Continue
and a subsequent complete service also passed.

Audio fixtures verified one listener, bounded ambience/tool sources, locomotion
footsteps with none while seated, settings/mute persistence, reading pause,
route unload/cancellation and disabled-owner silence. Motion fixtures covered
10 identities (two ordinary and all eight special guests), lovers at all three
seats, missing frames/presenter, service reaction and natural exit cleanup.
No audio/animation event became progression authority.

Warning harvest distinguishes expected negative-service warnings (missing
ingredient recognition/unknown service), deliberately exercised guard warnings,
the relocated Search exception, and the two build warnings. Final compiled
focused/stress runs have no observed exception/error/missing-glyph warning.
Compilation has zero errors and eight pre-existing warnings. No unexplained
progression stall or known objective P0/P1/P2 defect remains from this audit.

| Disposition | Remaining scope |
| --- | --- |
| PASS | Automated gameplay/discovery/recommendation, nine recipes/four methods, lifecycle, supported Back/re-entry, saves/ending, objective UI, audio/motion cleanup, stated standalone boundaries |
| VALIDATION_DEBT | OS pointer delivery under the observed external PickerHost interception; full physical-device/platform certification is not inferred from simulated delivery |
| HUMAN_SIGNOFF_REQUIRED | HUMAN_VISUAL: art/animation/layout taste; HUMAN_AUDIO: mix and sound satisfaction; HUMAN_WRITING: tone/emotional pacing; HUMAN_PLAY_FEEL: satisfaction/fatigue; NATIVE_HARDWARE_ONLY: unavailable real mouse/output-device checks |
| DEFERRED_PRESENTATION | Existing final memory/glass/garnish art, animation tuning and music composition backlog |
| OUT_OF_SCOPE | Two reserve recipes with incomplete preparation, future revisits/catalog expansion, other platforms/aspect ratios, public distribution approvals |

Routine mechanics and the 18-night bug hunt are no longer assigned to the
owner. The older IceInput owner-rerun request is superseded by current native
M/Space/T/I proof. Historical records remain historical. Final human approval
of SURLHWA as a finished game remains with the owner.
