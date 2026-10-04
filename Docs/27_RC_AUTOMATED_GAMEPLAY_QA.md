# RC automated gameplay QA

Baseline: `92ff5d4`, `refactor/project-rebuild`, Unity `6000.3.7f1`.
Started 2026-10-04. This record supplements the historical production audits;
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
# Also: physical, audio, motion, motion-safety, ui, modal, navigation, j5
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
| UI / modal / J5 | Existing production integration and delivery assertions | Current-source broad run pending |
| Windows RC QA | Known committed code, explicit input/asset provenance, separate saves | Build and native smoke pending |

Never add counts from historical runs to current-source totals. Reproduction
runs and harness failures remain in evidence, separate from final green runs.

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

## Evidence and limitations

Local artifacts live under `Logs/Review/RCAutomatedQA/`; suite artifacts and
screens live under the isolated project's corresponding folder. JSON reports
give per-scenario status/input/Submit counts. Traces include accepted input
observations, state transitions, completion counts and invariant checks.
Soft-lock snapshots include scene, route, customer/order/recipe, modal/focus,
raycast, seats, enabled method, containers/Jigger, pending completion delegate,
last input, last transition and trace tail. Diagnostic waits are attached to
explicit expected transitions; authored reading waits are driven separately.

Protected owner state: 71 hashed files, including configuration, settings,
layout, art metadata, font and all discovered Recovery artifacts. No such file
is staged. Original byte backups and hashes are local evidence only.

The Korean audit found **570 distinct Hangul characters**, all available from
the actual KoreanFallback/source font chain. This establishes glyph availability,
not a claim of beautiful typography or visible standalone rendering.

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

Final classification and human-only handoff will be updated after broad runtime
regression and fresh standalone verification; no RC PASS is asserted yet.
