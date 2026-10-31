# ACT 2 runtime checkpoint

Date: 2026-10-31. Scope: P07b, `through_act_02`, `Bar_Q1_Act2`.

This is an implementation/evidence checkpoint, not final production closure or
a new owner approval gate. Build 04, the focused correction tests, both final
farewell-to-memory runs, both completed-Continue runs and the clean retry reload
pass. The earlier Build 03
runs establish their recorded full-route results; the Build 04 runs below cover
the corrected final-departure, memory-rendering and completed-Continue paths.

## Build 04 correction evidence

The [build manifest](Manifests/act2-production-build.json) records Unity
6000.3.7f1, successful `Bar_Q1_Act2` output, zero errors/warnings, 175,568,344
bytes and 35.5044937 seconds. The focused
[normal Editor suite](Evidence/act2-runtime-correction-tests.xml) passes 23 / 23,
including the destroyed-serving-child teardown regression and actual Q1 depth
render checks for both memory parts. Diagnostic changed pixels are swing 804 /
adjacent place 96 at 1280×720, and 1,835 / 245 at 1920×1080.

| Build 04 actual Player run | Verified scope | Result |
| --- | --- | --- |
| [final-farewell-720-04](Evidence/act2-player-final-farewell-720-04/player-result.json) | Open history, slot 24 Farewell through natural departure and MEM-02. | PASS, exit 0, 16 captures; 25 services / 25 departures / seven Known / two memories. |
| [final-farewell-1080-04](Evidence/act2-player-final-farewell-1080-04/player-result.json) | Quiet history, slot 24 Farewell through natural departure and MEM-02. | PASS, exit 0, 16 captures; 26 services / 25 departures / seven Known / two memories. |
| [complete-720-04](Evidence/act2-player-complete-720-04/player-result.json) | Continues the preceding Build 04 Open completed checkpoint. | PASS, exit 0, 12 captures; no service replay; current/previous save generations byte-identical to the earned source. |
| [complete-1080-04](Evidence/act2-player-complete-1080-04/player-result.json) | Continues the preceding Build 04 Quiet completed checkpoint. | PASS, exit 0, 12 captures; no service replay; current/previous save generations byte-identical to the earned source. |
| [retry-reload-720-04](Evidence/act2-player-retry-reload-720-04/player-result.json) | Continues the actual `legacy-retry-1080-02/checkpoints/hanky-retry` save, prepares Hanky successfully and completes slots 21–24. | PASS, exit 0, 43 captures, no logged errors; 26 services / 25 departures / seven Known / two memories; 137.5755159 s. |

All five pass the full evidence verifier against one unchanged 1,368-file
source/Player inventory. The identical `inventory.json` SHA-256 is
`7b354bf8ad01d2b6f987f9e8aef45091af37a0cc36c7f3b020107facd88876d9`.
The complete inventory, rather than the Unity executable alone, identifies the
candidate. Normal-material Player render inspection confirms the swing and
adjacent place are visible and supported beside the window in
[720p](Evidence/act2-player-final-farewell-720-04/008-earned-memory.png) and
[1080p](Evidence/act2-player-final-farewell-1080-04/008-earned-memory.png).
The [720p paired farewell](Evidence/act2-player-final-farewell-720-04/005-act2-farewell-24-1.png)
was also inspected for distinct actors and the single shared cup. These are
technical visibility observations, not human visual or emotional approval.

## Evidence retained

Build 03 executable SHA-256:
`87f775393ac5ef273e9e6d20c8b768f5d4c41b2fbf10d536b32dbfe961cac490`.

| Actual Player run | Start and outcome | Recorded result |
| --- | --- | --- |
| [resume-720-02](Evidence/act2-player-resume-720-02/player-result.json) | Continues the actual interrupted fresh run at slot 7 / Arriving. Open ACT 1 memory retained. | PASS, exit 0; 25 services, 25 departures, seven Known recipes, two memories; 101 captures. |
| [legacy-retry-1080-02](Evidence/act2-player-legacy-retry-1080-02/player-result.json) | Imports the actual completed Quiet ACT 1 save. Omits required Hanky Panky orange zest, reaches Retry, then succeeds using the production controls. | PASS, exit 0; 26 services including the failed attempt, 25 departures, seven Known recipes, two memories; 78 captures. |
| [retry-reload-720-02](Evidence/act2-player-retry-reload-720-02/player-result.json) | Continues an actually earned Hanky Retry checkpoint and reaches the completed campaign. | FAIL retained. Completion counts are 26 / 25 / seven / two, but shutdown logs a real `NullReferenceException`. Process exit 0 does not override this failure. |

The Open history is a fresh-start plus actual restart chain. It is not described
as one uninterrupted 25-service run. The Quiet import preserves its original
`star_unlit` memory; the Open history retains `star_glowing`. Both earn
`mem_02 / swing_empty_place` only after the final departure.

All gameplay actions in these runs use simulated Keyboard/Mouse events through
the production InputSystem and UI. Drinks are poured, stirred, strained and
garnished through those controls. No service callback or constructed save is a
substitute for that input. The screenshots are actual Player camera/UI renders.
This evidence is **not OS-native input**, subjective pacing approval, or final
visual/audio/game-feel acceptance.

## Discovered defects and retained prior attempts

- The [retry-reload error log](Evidence/act2-player-retry-reload-720-02/errors.log)
  records `CampaignCastView.OnDisable → Hide →
  SliceWorkbenchView.SetServingPresentationPosition` after the serving transform
  has been destroyed. Build 04's focused teardown regression and five targeted
  Player exits, including the matching retry reload, now pass. The earlier
  run remains FAIL. This was an implementation defect, not presentation debt.
- Render inspection found the earned memory hidden in the scene despite its
  active presentation object. The Build 04 placement correction passes actual
  720p/1080p depth regressions and normal-material Player render inspection, as
  recorded above. `LoversVisible` alone was not accepted as proof that a player
  can see the object; the earlier logical-memory results are not retroactively
  described as visibility proof.
- [fresh-720-01](Evidence/act2-player-fresh-720-01/interruption.json) and
  [legacy-retry-1080-01](Evidence/act2-player-legacy-retry-1080-01/interruption.json)
  were deliberately interrupted after the seated-pivot/stool-occlusion defect
  was found. Their process exits are -1 and they have no final PASS report.
  Their actual published generations remain valid provenance for later restart
  tests; the interrupted attempts remain incomplete evidence.
- [native-paired-01](Evidence/act2-native-paired-01/native-status.json) is
  `VALIDATION_DEBT`: Computer Use app approval timed out before any native input
  action was sent. The isolated Player was closed. No native-input PASS or native
  save modification is claimed.

No earlier FAIL or interrupted attempt is rewritten by a later successful run.

## Exact earned roots for final targeted validation

These paths are relative to `Production/Validation/Evidence`. Each contains
`slot1/current.json` and `slot1/previous.json`; both hashes were checked against
the source run's `checkpoints.jsonl` during this audit.

| Purpose | Actual checkpoint root | Starting durable state |
| --- | --- | --- |
| 720p Open, final farewell through departure and memory | `act2-player-resume-720-02/checkpoints/act2-farewell-24-0` | Slot 24, Farewell line 0; 25 services, 24 departures, seven Known, one glowing-star memory. |
| 1080p Quiet, final farewell through departure and memory | `act2-player-legacy-retry-1080-02/checkpoints/act2-farewell-24-0` | Slot 24, Farewell line 0; 26 services, 24 departures, seven Known, one unlit-star memory. |
| Verified Build 04 Open complete source | `act2-player-final-farewell-720-04/checkpoints/act2-complete` | Complete; 25 services, 25 departures, seven Known, two memories. |
| Verified Build 04 Quiet complete source | `act2-player-final-farewell-1080-04/checkpoints/act2-complete` | Complete; 26 services, 25 departures, seven Known, two memories. |

Use `run_act2_proof.ps1 -Start Checkpoint -CheckpointRoot <actual root>` with a
new evidence name. The launcher makes byte-identical copies into a new isolated
save root and points legacy lookup at a separate empty directory. A final
completed-Continue run should preferably use the new candidate's freshly earned
`checkpoints/act2-complete`, preserving that candidate's complete provenance.

The helper already supports Farewell and Complete starts: a pending accepted
service is not counted twice; already-consumed introductory lines are not
replayed; no new service is needed at Complete. The bounded verifier additions
require every remaining natural-departure archive even when no brewing occurs,
and require completed Continue to leave both published generations byte-identical
to the copied source. Their focused tests failed before the changes and then all
11 ACT 2 proof tests passed. The full Tools Python suite then passed 64 / 64 with
normal temporary-directory access. No Unity source was changed for these verifier
fixes.

Each new run must retain its source/Player inventory unchanged until verification
ends. Earlier inventories are historical records, not evidence for a new binary.

## Recorded performance observations

| Build 03 run | Recorded frames | Median / p95 / p99 frame ms | Maximum frame ms | Peak process working set | Wall time |
| --- | ---: | --- | ---: | ---: | ---: |
| resume-720-02 | 30,510 | 16.667 / 16.862 / 18.228 | 750.650 | 591,183,872 bytes | 518.773 s |
| legacy-retry-1080-02 | 16,629 | 16.667 / 16.915 / 18.810 | 766.366 | 607,391,744 bytes | 288.643 s |

These values come from `player-result.json` frame samples and `process.json`.
Percentiles use the sorted sample at `floor((count - 1) × percentile)`, with the
ordinary sample median. Synchronous QA capture, save and automation overhead, and
concurrent Player runs are included.
The long maximum frames are retained; this is not a clean performance benchmark,
latency certification, or final performance approval.

## Remaining evidence

The bounded Build 04 correction/restart matrix is complete. Build 04 is not
described as a fresh repeat of the entire 25-service route: earlier actual
route histories and these bounded correction reruns remain distinct evidence.
Native-input validation remains debt. Human judgments of dialogue, pacing,
bartending feel, visual composition and emotional impact remain human-owned.
`CampaignPhase.Complete` here means the end of the loaded ACT 2 content. The
transition into ACT 3 belongs to the next package; ACT 3 and later content are
not certified by this checkpoint. This record does not declare all Production
work complete.

Commit hygiene: after Player validation, trailing Unity serialization whitespace
was removed from 66 newly added asset/meta/scene files. Every non-whitespace
token is identical to the validated files; the before/after hashes are retained
in `Evidence/act2-postbuild-whitespace.json`. No gameplay, reference, geometry,
material value or authored text changed. Historical run inventories retain the
exact files used for those runs.
