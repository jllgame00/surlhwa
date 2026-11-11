# ACT 7 and primary ending Production checkpoint

Baseline `ac3aaf3`, following completed ACT6 `3c25ba0`. Interrupted work was
reconstructed before changes; [resume audit](RESUME_2026_11_11.md) records the
actual commits and retained evidence. ACT7 and the common ending are implemented
and verified at this bounded checkpoint. The full
Production request and Gate3 package are not complete. Nothing has been pushed.

## Implemented scope

- Four authored encounters append slots43–46 on nights19–20. The station worker,
  Odysseus return, postman and Odysseus stay retain their exact sourced fields.
  The accepted43-slot prefix, nine recipes,22 ingredients and learning/supply
  timing are unchanged. [Content provenance](ACT7_CONTENT_PROVENANCE.md).
- MEM07 `anchor_ship_fragment` is granted once at natural departure46 regardless
  of FIT. A small supported anchor and incomplete hull remain in the room.
  There is no completed-ship revisit, trophy requirement or additional trigger.
- Departure46 publishes47 completed visits, MEM07 and `EndingReady` together.
  Explicit reading proceeds through `EndingEntered`, `EndingReading`, then
  `Complete`. The eight common source lines remain in order; one remembered
  Prince quotation after common0002 reads saved keep/seek/absent choice.
  This callback placement is explicitly recorded as new placement of existing
  source text. The Prince does not return as an actor. Open/Quiet history remains
  unchanged; no absent success or glowing star is synthesized.
- Existing phase/cursor/consumed receipts persist the nine-line reading. There
  is no payload/schema change, encounter48 or generated night21. Completed
  Continue shows the common coda and stays in the bar without another write.
- ACT7 uses a separate destination namespace and accepts pinned ACT1–6 sources.
  Historical service outcomes, source generations and destination precedence
  remain intact. Main completion does not require every recipe Known or every
  service successful.
- Separate `Bar_Q1_Act7`, owned font and history6..2+legacy1 extend Q1 with the
  independently sourced Odysseus standing/walk/seated contacts. His two visits
  use the existing unreserved stool; the Prince's remembered chair stays empty.
  Ending UI hides the departed guest/request and clears the visible served
  liquid while preserving the historical service snapshot.

## Verification

- Full standalone regression: **576/576 PASS**, exit0,
  `Evidence/act7-foundation-full-resume-01.log`.
- Full tooling regression: **140/140 PASS**, exit0,
  `Evidence/act7-tools-full-resume-01.log`.
- Focused content:68 PASS, including40 ACT7 and28 retained ACT6 cases; source
  ownership/tamper, unchanged prefix, exact ending and domain service routes.
  These domain routes are not simulated-input Player evidence.
- Focused ending/continuation:72 PASS under normal local filesystem permissions.
  Every unread cursor, all recall branches, Open/Quiet, no all-known gate,
  malformed receipts, write failures/retry and six File.Replace fault positions
  are covered. The absent-choice history is an explicit compatibility fixture.
  Sandbox-denied File.Replace failures are retained separately.
- Focused ACT2–7 proof-tool regression:70 PASS. Verifiers inspect each remaining
  ending line, exact displayed text/role, actor absence, earned memory, actual
  archived cursor/receipts and preserved source histories.
- The initial inherited presentation RED was0/6; its routing fixture lacked
  content and was corrected. The first resumed Editor attempt stopped at a
  test-only internal-API access compile error. After that fixture correction,
  the meaningful RED ran11 tests:4 PASS,7 expected missing routing/scene/ending
  UI failures. All attempts remain retained with their original classifications.
- Separate scene preparation succeeded with natural exit0. It adds15 objects,
  removes0 and changes only two existing object blocks semantically: parent child
  membership and controller content/font/history/Odysseus binding. Other textual
  differences were Unity trailing whitespace, removed from new generated files
  without changing their non-whitespace tokens; original bytes remain archived.
  Accepted ACT1–6 content/scene bytes remain unchanged.
- Independent read-only runtime/save and presentation/content reviews found no
  blocking defect. Those reviews did not launch tests or grant visual approval.

Full licensed Editor regression: **726/726 PASS**, natural exit0,
`Evidence/act7-editor-full-resume-01.xml/log`. This includes the earlier ACTs,
all new ending UI cases, Odysseus routing/contact/history, and actual GPU depth
checks for all seven memories. Anchor/fragment changed-pixel counts are87/93 at
1280×720 and187/210 at1920×1080; all14 earlier memory-part counts remain unchanged.
These diagnostic renders establish technical visibility, not final art approval.

After that run, the read-only native evidence probe gained `visibleServingMl`
alongside its unchanged historical `serving` value. This makes the emptied ending
vessel distinguishable from the retained final-service snapshot. It changes no
gameplay state; the following Player build compiles that observation addition.

Built Player: **Succeeded**, zero errors/warnings, 177984691 bytes,27.2553202s,
natural exit0; `Manifests/act7-production-build.json` and
`Evidence/act7-build-resume-01.log`.

Nine actual Player runs pass the complete proof verifier and unchanged-inventory
check, with natural exit0. Each run uses simulated production InputSystem input
and an isolated copy of a previously earned save, never a fabricated checkpoint.

| Evidence directory under `Evidence/` | Source / coverage |
| --- | --- |
| `act7-player-open-720-01` | Actual ACT6 Open completion; all four encounters, nine ending lines and coda. |
| `act7-player-quiet-mismatch-1080-01` | Actual ACT6 Quiet completion; mismatching final French75 retains WHAT/HOW/QUALITY and FIT=false, then natural MEM07/common ending. |
| `act7-player-ready-reload-720-01` | First run's earned EndingReady. |
| `act7-player-entered-reload-1080-01` | First run's first unread ending line. |
| `act7-player-recall-reload-720-01` | Quiet run's first unread remembered Prince quotation. |
| `act7-player-last-line-reload-1080-01` | Quiet run's last unread common line. |
| `act7-player-complete-720-01` | Open completed Continue; both save generations remain byte-identical. |
| `act7-player-complete-1080-01` | Quiet completed Continue; both save generations remain byte-identical. |
| `act7-player-farewell-reload-1080-01` | Quiet final farewell before departure/MEM07; resumes remaining reading exactly once. |

All finish with47 completed encounters and seven earned memories. The full Open
run retains47 services; the Quiet run retains its earlier retries and mismatches.
The nine Known recipes are inherited earned history, not a completion condition.
Exact run identities, counters, source histories, inventories and process telemetry
are recorded in `Evidence/act7-final-runtime-summary.json`.

Normal-material images inspected at720p and1080p include Odysseus seated at the
unreserved stool, the remembered Prince quotation, and the empty bar's final
line. All seven memories remain visible; the Quiet star stays unlit. Ending
actors/request/visible serving liquid are absent, while the historical service
snapshot is retained. The renderer and process checks do not constitute a
dedicated performance benchmark or final artistic approval.

Unity generated72 unrelated serialization changes during verification. Only
those newly generated changes were reversed after matching every preimage to
the resumed-state hashes; raw generated bytes/diffs are retained in
`Evidence/act7-build-unrelated-serialization-01.*`. Inherited owner HTML,
Graphics/Quality settings, material stat changes and PackageManager settings
remain untouched and excluded.31 new generated files received whitespace-only
hygiene, with originals and token-equivalence checks retained in
`Evidence/act7-generated-whitespace-*`. Accepted ACT1-6 content/scene bytes and
the frozen RC1/Canon/Architecture authorities remain preserved.

## Remaining scheduled work

Commit the internally complete bounded work, then continue P09–P12 UI,
environment/cast/tool, animation/tactile and audio/music work, followed by P13
fresh full-campaign regression, practical native input and named-device performance.
All PD01–PD11 remain open for the named Gate3 ruling. Automated visual checks and
simulated production InputSystem use do not confer native certification or final
owner artistic, pacing, listening or feel approval.
