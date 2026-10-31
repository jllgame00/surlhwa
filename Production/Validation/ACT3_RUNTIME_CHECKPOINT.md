# ACT 3 runtime checkpoint

Date: 2026-10-31. Baseline: ACT 2 commit `184e1af`. Scope: P07c,
`through_act_03`, `Bar_Q1_Act3`.

This is a bounded implementation/evidence record, not a new owner gate or a
declaration that all Production work is complete. Build 02's logical PASS routes
exposed the remembered-chair/star interaction defect documented below. The
correction is implemented in Build 03. All five final Player runs completed with
verified PASS and exit 0: both paired-to-ACT-3 routes, the actual mismatch
mid-farewell reload, and both completed Continue checks. The bounded ACT 3
technical checkpoint is stable enough to continue to ACT 4. This does not grant
subjective presentation approval or certify later content. Native-input debt
and the post-build source-byte qualification below remain explicit.

## Build 03 final runtime evidence

The [Build 03 manifest](Manifests/act3-production-build.json) records success,
zero errors/warnings, 176,005,620 bytes and 10.7020045 seconds. The
[full Editor suite](Evidence/act3-full-editor-01.xml) passed 338 / 338 before the
last dialogue-border correction. The final
[UI/chair suite](Evidence/act3-final-ui-01.xml) then passed 30 / 30 after that
correction. These are separate ordered checks, not a claim that the full suite
was rerun after the last change.

| Actual Player run | Resume boundary and observed behavior | Final result |
| --- | --- | --- |
| [paired-open-720-03](Evidence/act3-player-paired-open-720-03/player-result.json) | Actual ACT 2 Farewell 24, line 0; retains the Open star; serves slots 25–28. | PASS, exit 0; 29 services, 29 departures, seven Known, three memories; 46 captures. |
| [paired-quiet-mismatch-1080-03](Evidence/act3-player-paired-quiet-mismatch-1080-03/player-result.json) | Actual ACT 2 Farewell 24, line 0; retains the Quiet star and earlier failed Hanky attempt; actual Dry Martini FIT=false at slot 27, then natural departure. | PASS, exit 0; 30 services, 29 departures, seven Known, three memories; 46 captures; new mismatch observed. |
| [mismatch-farewell-reload-720-03](Evidence/act3-player-mismatch-farewell-reload-720-03/player-result.json) | Actual slot-27 Farewell line 1 from the preceding 1080p run; resumes remaining lines and serves only slot 28. | PASS, exit 0; 30 services, 29 departures, seven Known, three memories; 21 captures; entire inherited Quixote service and its FIT=false preserved. |
| [complete-720-03](Evidence/act3-player-complete-720-03/player-result.json) | Completed Open route; no additional service or visited slot. | PASS, exit 0; 29 services / 29 departures, seven Known, three memories; 12 captures; both save generations byte-identical. |
| [complete-1080-03](Evidence/act3-player-complete-1080-03/player-result.json) | Completed Quiet/mismatch route; no additional service or visited slot. | PASS, exit 0; 30 services / 29 departures, seven Known, three memories; 12 captures; both save generations byte-identical. |

The [final runtime summary](Evidence/act3-final-runtime-summary.json) binds all
five reports to the same unchanged 1,425-file source/Player inventory during
their runs. SHA-256 of each original `inventory.json` is
`dc249b628d05bd8c99edec817c5a2e1f4fe67867c8a124d7d9e08cbd56982d37`.
Both main routes begin before the lovers' departure, covering corrected paired
seating and the following ACT 3 solo guests without re-serving slot 24. They
are four-service ACT 3 continuations from earned histories, not fresh
uninterrupted 29-service playthroughs. All five reports have empty error lists.

The mid-farewell reload records `visitedSlots=[28]`,
`inheritedQuixoteServicePreserved=true`,
`inheritedQuixoteMismatchPreserved=true` and
`newQuixoteMismatchObserved=false`. Both completed Continue runs also preserve
the entire inherited Quixote service. Their inherited mismatch flag is false
for Open and true for Quiet; neither performs a new mismatch.

Normal-material inspection by the coordinating agent of the
[720p pair](Evidence/act3-player-paired-open-720-03/004-act3-farewell-24-0.png),
[1080p pair](Evidence/act3-player-paired-quiet-mismatch-1080-03/004-act3-farewell-24-0.png)
and [720p postman](Evidence/act3-player-paired-open-720-03/008-act3-intro-25-0.png)
shows the remembered chair/star clear, the one actual serving cup correctly
placed, and the paired feet separated from the dialogue border. The
[720p Quixote introduction](Evidence/act3-player-paired-open-720-03/014-act3-intro-26-0.png)
and [1080p introduction](Evidence/act3-player-paired-quiet-mismatch-1080-03/014-act3-intro-26-0.png)
show the solo guest clear of the reserved chair/star. The
[720p immediate MEM-03](Evidence/act3-player-paired-open-720-03/031-act3-earned-memory-after-27.png)
and [1080p immediate MEM-03](Evidence/act3-player-paired-quiet-mismatch-1080-03/031-act3-earned-memory-after-27.png)
show the earned fragment before the final ordinary service, supported by the
after-departure-27 saved receipt. These are bounded observations of the corrected
interaction, not human aesthetic approval.

After all five Players closed, trailing whitespace was removed from 38 new
ACT 3 serialized files only. The
[post-build whitespace record](Evidence/act3-postbuild-whitespace.json) retains
every before/after hash and records unchanged non-whitespace tokens. Historical
Player inventories were not rewritten: their PASS certifies the pre-cleanup
bytes. A subsequent read-only audit found exactly those 38 recorded source
changes, with no other inventory differences. Re-running the strict verifier
against the cleaned working tree correctly reports `Source/Player inventory
changed` for each historical run; every other evidence check passes. No new
Player run or runtime PASS for the changed source bytes is claimed. The built
Player, implementation code and accepted ACT 1/2 asset bytes are unchanged.

The full regression invoked historical experimental builders and generated
unrelated `Bar_C`/`Bar_C1` serialization churn. The
[scene restoration record](Evidence/act3-fulltests-scenes-restoration.json) and
[retained scene diff](Evidence/act3-fulltests-scenes-churn.diff) document rejection
of those two rewrites. A further
[66-file importer/settings restoration record](Evidence/act3-build03-editor-churn-restoration.json)
retains the corresponding
[full diff](Evidence/act3-build03-editor-generated-churn.diff). The restored files'
hashes were checked against those records. Accepted `Bar_Q1.unity` and
`Bar_Q1_Act2.unity` remain byte-identical to the baseline; the owner's unrelated
edit is not part of this correction.

## Historical Build 02 results and discovered interaction defect

| Actual Player run | Actual inherited history and route | Recorded result |
| --- | --- | --- |
| [prior-open-720-02](Evidence/act3-player-prior-open-720-02/player-result.json) | Continues the earned Open ACT 2 completion. Prepares all four ACT 3 services through production controls. | PASS, exit 0; 29 services, 29 departures, seven Known, three memories; 42 captures. |
| [prior-quiet-mismatch-1080-02](Evidence/act3-player-prior-quiet-mismatch-1080-02/player-result.json) | Continues the earned Quiet ACT 2 completion, retaining its earlier failed Hanky attempt. Dry Martini at Quixote-alone produces actual FIT=false and still reaches the natural memory departure. | PASS, exit 0; 30 services, 29 departures, seven Known, three memories; `quixoteMismatchObserved=true`; 42 captures. |

Both full evidence verifications passed against the same unchanged 1,423-file
Build 02 source/Player inventory, SHA-256 of `inventory.json`:
`18f1d1030afa89cf5d62ba4f3083e9ba708b528875161f4500ed1062700556e4`.
Both preserve their original source generations. These are actual four-service
ACT 3 continuations from earned ACT 2 histories, not fresh uninterrupted
29-service runs.

The immediate [720p MEM-03 capture](Evidence/act3-player-prior-open-720-02/027-act3-earned-memory-after-27.png)
and [1080p capture](Evidence/act3-player-prior-quiet-mismatch-1080-02/027-act3-earned-memory-after-27.png)
show the earned fragment during slot 28 arrival, with 28 completed departures
and three memories before the final ordinary service. Normal-material render
inspection confirms MEM-03 is visible; this does not resolve the separate
remembered-chair defect.

The defect is visible in the [postman introduction](Evidence/act3-player-prior-open-720-02/004-act3-intro-25-0.png)
and [Quixote introduction](Evidence/act3-player-prior-open-720-02/010-act3-intro-26-0.png).
The retained star is drawn across the new seated guest, while that guest occupies
the place intended to remain empty after the Prince. This is an objective
memory/cast interaction defect, not optional presentation polish. The reserved-chair
source correction is in Build 03, with the paired/solo render and save-reload
evidence above. These original Build 02 findings and logical PASS records remain
intact; they do not retrospectively certify the correction.

The correction's meaningful [chair RED 04](Evidence/act3-chair-red-04.xml)
executed all five tests and failed for the intended reasons: later solo seating,
the served cup's position, the lovers' two adjacent unreserved stools, and
paired-dialogue clearance at 720p/1080p. Earlier launch attempts are classified
separately: [RED 02 log](Evidence/act3-chair-red-02.log) could not connect to the
Package Manager IPC process (exit 1), and [RED 03 log](Evidence/act3-chair-red-03.log)
lacked the required Editor entitlement (exit 198). Those two are
`VALIDATION_BLOCKED_BY_ENVIRONMENT`, not executed failing tests or implementation
regression results. Their original files are retained.

## Implemented contract and preserved history

The cumulative candidate contains 29 slots. ACT 3 adds four encounters at slots
25–28: postman, Quixote's declaration, Quixote alone, and the snow worker. There
is no new cocktail, ingredient, supply grant or recipe-knowledge seed. Seven
Known recipes remain the expected completed collection.

`mem_03 / windmill_fragment` belongs to slot 27's natural departure, after all
of that encounter's farewell reading and regardless of FIT. The final ordinary
guest at slot 28 is not the grant boundary. The windmill has one tilted but
attached wing; earlier memories remain. The view reads the durable receipt and
cannot award it. Source/domain details remain in
[ACT3_CONTENT_PROVENANCE.md](ACT3_CONTENT_PROVENANCE.md).

Accepted ACT 1 and ACT 2 JSON bytes and definitions are unchanged:

- `act1.json`: `0d347e6976825ba61ab6a9fec39c73decc8f13ac10c5f0bf4d80558b1f33e3d4`.
- `act2.json`: `e38fe3dce7cdcb004dd0731df730925c2531562379fda7fde95d922c0d620fa1`.
- New `act3.json`: `9db4998cd4f3dd8fc1c0371e8396504c07c0cce253e504caf39f48ec370555f0`.

Shared runtime code extends current ACT display, cast selection and continuation;
this is not a claim that every shared source file is byte-identical. Prior scenes,
content identities, recipe authorities and earned service/knowledge/memory records
remain authoritative. The existing schema-1 save format is retained. Conversion
creates a separate destination and preserves both original source generations.
The latest existing accepted history takes priority even when corrupt or
incompatible; older progress cannot silently replace it.

## Compilation and automated evidence

- [Standalone contract suite](Evidence/act3-continuation-green.log): 233 PASS /
  0 FAIL, including ACT 1/2/3 continuation, source preservation, publication
  interruption and progress validation. This is .NET domain/codec evidence,
  not Unity or physical input.
- [Normal Editor integration suite](Evidence/act3-integration-01.xml): 91 / 91
  PASS, zero skipped/inconclusive. Covers content, preserved source prefixes,
  history selection, Quixote cast, memory structure and existing presentation
  regressions. The later actual Player inspection exposed the remembered-chair
  interaction that these tests did not establish.
- Python Tools suite: 76 / 76 PASS with normal temporary-directory access.
  Nine new campaign-proof tests use rejection cases for incomplete ACT 3,
  missing/late MEM-03 evidence, incorrect Quixote FIT, wrong memory owner,
  incomplete departure archives, changed inherited Quixote service records and
  non-isolated history roots. Existing
  ACT 2 proof tests remain 11 / 11 PASS.
- [Retained Build 02 manifest](Evidence/act3-build-02-manifest.json): Unity 6000.3.7f1,
  `Bar_Q1_Act3`, success with zero errors/warnings, 175,971,972 bytes and
  4.2473593 seconds. Its actual Player-run inventory is recorded above; the
  corrected candidate has its separate inventory above. An executable hash alone
  does not identify all Player data.

The initial [history RED run](Evidence/act3-history-red-02.xml) records five
expected failures before implementation. The earlier
[interrupted Editor attempt](Evidence/act3-history-red-interrupted.json) stopped
before tests because presentation declarations were still being authored; it is
neither RED nor PASS evidence. Build 01 succeeded with one retained
[font-object naming warning](Evidence/act3-build-01-messages.json): the main
object `ACT 3 Korean` differed from filename `Act3Korean`. The root corrected
that object name; Build 02 has no warnings. These earlier records remain intact.

## Isolated actual-history Player protocol

`run_campaign_proof.ps1 -Act 3` uses a fresh evidence/save directory. Every start
passes `-campaignHistoryRoot <run>/history` and an isolated legacy root, preventing
test runs from discovering the owner's ordinary save directories.

| Start | Source handling |
| --- | --- |
| New | Empty run-local ACT 1/2 history namespaces and an empty destination. |
| Prior | Copies actual ACT 2 `current.json` / `previous.json` into `origin/slot1` and `history/campaign-through-act-02/slot1`; Continue imports into the separate destination. |
| Legacy | Copies actual ACT 1 generations into `origin/slot1` and `history/campaign-act1/slot1`; Continue follows validated cumulative conversion. |
| Checkpoint | Copies actual ACT 3 generations into the new destination; both historical namespaces remain empty. |

Every copied source is hashed before launch and checked unchanged afterward.
No save is constructed to seed knowledge, service outcomes or memories. The
historical Build 02 Prior sources, relative to `Production/Validation/Evidence`,
are:

- Open: `act2-player-final-farewell-720-04/checkpoints/act2-complete` — 25 actual
  services, 25 departures, seven Known recipes, glowing star and swing memories.
- Quiet: `act2-player-final-farewell-1080-04/checkpoints/act2-complete` — 26 actual
  services including the earlier required Hanky failure, 25 departures, seven
  Known recipes, unlit star and swing memories.

The final Build 03 runs instead use these exact actual checkpoint roots,
relative to the same Evidence directory:

| Final run | Copied source checkpoint |
| --- | --- |
| paired-open-720-03 | `act2-player-resume-720-02/checkpoints/act2-farewell-24-0` |
| paired-quiet-mismatch-1080-03 | `act2-player-legacy-retry-1080-02/checkpoints/act2-farewell-24-0` |
| mismatch-farewell-reload-720-03 | `act3-player-paired-quiet-mismatch-1080-03/checkpoints/act3-farewell-27-1` |
| complete-720-03 | `act3-player-paired-open-720-03/checkpoints/act3-complete` |
| complete-1080-03 | `act3-player-paired-quiet-mismatch-1080-03/checkpoints/act3-complete` |

The first two sources contain 24 completed departures and only MEM-01; the
inherited slot-24 service is already committed. Continue reads its remaining
farewell and earns MEM-02 at natural departure before starting ACT 3. Source and
copied current/previous generations remain byte-identical to their recorded
origin hashes throughout all five runs.

The completed 720p route prepared Manhattan, Gin Fizz, Hanky Panky and Negroni at slots
25–28. The 1080p `-QuixoteMismatch` route prepares Dry Martini at slot 27 and
requires actual `Fits=false`, good preparation, one service, no Retry and the
same natural departure/memory grant. An inherited failed ACT 2 service remains
in history: expected totals are 29 services for the Open source and 30 for the
Quiet source, with 29 departures, seven Known recipes and three memories in both.

The proof records `after-departure-27` and `act3-earned-memory-after-27` while
slot 28 is arriving and before its dialogue/service. Both must show 28 completed
occurrences and MEM-03; the published save must contain no slot-28 service yet.
This observes the existing session transition without freezing or injecting
game state. Mid-farewell Continue must resume the consumed-line cursor and award
the memory once. Completed Continue must preserve both published generations
byte-for-byte and perform no additional service.

A mismatch mid-farewell restart uses plain `-Start Checkpoint`, without
`-QuixoteMismatch`: the drink has already been served and must not be served
again. The verifier reads the actual copied origin and requires its entire
slot-27 service record, including snapshot and FIT outcome, to remain unchanged
at completion. It reports `inheritedQuixoteMismatchPreserved` separately from
`newQuixoteMismatchObserved`; the latter correctly remains false when the reload
only reads the remaining farewell and serves slot 28. The added rejection test
first caught a changed inherited FIT being accepted, then passed after the
bounded Python-only validator correction. No runtime/save mutation is involved.

## Evidence limits and remaining work

The recorded runs send simulated Keyboard/Mouse events through production
InputSystem/UI controls. Actual Player images, liquid composition, service
records and saves are evidence of that tier. They are **not OS-native input**.
Source/Player inventory must remain frozen throughout each run; no direct service
callback, fabricated save or transformed old report is accepted as runtime proof.

The retained [ACT 2 native attempt](Evidence/act2-native-paired-01/native-status.json)
is carried `VALIDATION_DEBT`: Computer Use approval timed out before any native
input action. It is not a new ACT 3 native attempt or PASS. Final dialogue,
pacing, bartending feel, art, animation, audio and emotional approval remain
human-owned. Capture-loop frame timings, if later reported, are diagnostic and
include capture/save/automation overhead; they do not establish dedicated final
performance approval.

The bounded ACT 3 runtime matrix is complete: paired-to-solo continuation at
720p/1080p, natural MEM-03 departure with fitting and non-fitting drinks, the
actual mismatch farewell reload, and completed Continue preserving both
generations. This supports proceeding to the next package with the recorded
source-byte qualification, native debt and human sign-off requirements intact.
`CampaignPhase.Complete` means the end of this loaded ACT 3 candidate. ACT 4
transition belongs to the next package; later ACTs, the main ending and overall
Production completion are not certified here.
