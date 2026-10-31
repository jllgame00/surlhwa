# ACT 1 Gate 2 — accepted-baseline follow-through, 2026-10-31

The owner accepts the Little Prince seated pose at **a19a84a** as the current
Production baseline. Q1, character scale, stool position, grounding, counter
occlusion, bartender-facing orientation and ACT 1 logic remain protected.
No further pose polish is in this pass. Final cast-wide art/animation polish
remains eligible. [Recorded ruling](Q1_OWNER_CONFIRMATION.md).

This pass continues the completed integration's verification and packaging work;
it does not manufacture a new gameplay phase. The single current review entry is
[ACT 1 / Gate 2](review-gate2-current.html). Gate 2 is **HUMAN_SIGNOFF_REQUIRED**;
ACT 2 remains **OUT_OF_SCOPE** until formal closure.

## Scope and evidence

No runtime source, content, asset, scene, prefab, package or ProjectSettings changes.
All 1,271 source/Player files recorded during the accepted pose correction still
match. Current run inventories additionally cover the retained historical ACT 1
Player: 1,555 files. The accepted Player is reused; no rebuild or new Unity Editor
test execution is claimed. The existing successful build and 7/7 Q1 Editor tests
remain the compile/Editor evidence for these unchanged inputs.

Fresh standalone .NET compilation and contract suite: **149 passed, 0 failed**.
This covers actual domain/application contracts, including knowledge, recognition,
FIT, save publication, recovery and progression. It is not a Unity runtime claim.

The current Player evidence and both completed-save Continue checks are recorded
in [the package manifest](Manifests/gate2-accepted-baseline.json). All runs use
simulated production InputSystem devices and actual native-resolution rendering.
They do not establish OS-native input or subjective feel. The package validator
requires all six Q1 postconditions, complete actual services/departures/knowledge,
correct farewell/memory outcomes, the original Quiet service prefix, exact
source/Player identity and byte-identical completed-save generations.

| Current Player run | Checks passed | Native stills |
| --- | ---: | ---: |
| Complete Open / 720p | 173 | 54 |
| Earned Warmth through Quiet / 1080p | 76 | 29 |
| Completed Open Continue / 1080p | 11 | 10 |
| Completed Quiet Continue / 720p | 11 | 10 |

All four processes exit 0 with no reported errors; their 1,555-file inventories
match. Both outcomes finish 17 services and departures, six learned recipes and
one correct memory. Completed Continue leaves both save generations byte-identical.
Current first-visit and Quiet memory frames were visually inspected for basic
scene/UI integrity; this does not replace the bundled subjective review.

Read-only code review identified two package-verifier gaps: launch arguments were
not bound to the claimed save copy, and added files could escape inventory checks.
Both are corrected. Five focused regressions pass after failing with the missing
checks; actual package validation also passes. No Critical findings or deferred
Minor findings remain from that review. Runtime behavior was not part of the
reviewer's new-code scope and is covered by the executed Player/contract checks.

Initial sandbox attempts are retained: the Player stopped before gameplay when
atomic replacement of `observation.json` was denied; the standalone suite reported
132 passes / 17 save-related failures with access-denied evidence. The unchanged
commands were rerun outside the sandbox. No runtime tolerances, assertions or save
logic were changed to obtain a pass. See `Evidence/gate2-accepted-open-20261031/`
and `Evidence/gate2-accepted-foundation-20261031.log` for the initial attempts.

The 74 Canon/Architecture files and the frozen RC1 snapshot still match the
prior integration baseline. The pre-existing `review-slice.html` edit is preserved
and excluded. Earlier review pages, recordings and manifests remain untouched.

## Review use and remaining limits

The current package brings together all twelve areas from the approved Gate 2
plan, the accepted-pose scene image, fresh current-Player stills and the existing
isolated launcher. Launch from the repository root:

```powershell
powershell -File Production/Tools/run_gate2_q1.ps1 -Start New -Height 1080
```

Other starts: `First`, `Warmth`, `Clarity`, `QuietClarity`, `OpenMemory`,
`QuietMemory`. Checkpoint starts use copies of earned saves; choose Continue.

Ruling: preserve the earlier complete audiovisual recordings and explicitly label
their previous seated pose. Current stills/live play show accepted art. Re-recording
the unchanged story/audio is not needed to establish pose acceptance, and the old
media remains useful with its limits disclosed. Cost: reviewers must use current
stills/live play for the accepted pose, not infer current art from the older video.

| Area | Classification | Boundary |
| --- | --- | --- |
| Accepted Prince pose | PASS | Owner accepted this Production pose, not final cast-wide art/animation. |
| ACT 1 objective behavior | PASS | Current Player/contract evidence in the manifest; simulated-input tier. |
| OS-native four-method completion | VALIDATION_DEBT | Historical environment interruption; no new desktop-input attempt. |
| Capture sync / minimum hardware | VALIDATION_DEBT | Existing 5 Hz recordings/audio-duration differences and held-pose performance limits remain. |
| Final cast motion, UI/Codex, environment, tools, audio/music | DEFERRED_PRESENTATION | Existing debt remains; no speculative polish added. |
| Overall ACT 1 tone, pacing, feel and bundled presentation | HUMAN_SIGNOFF_REQUIRED | One formal Gate 2 decision; this pose ruling alone does not close it. |
| ACT 2–7 | OUT_OF_SCOPE | No implementation until Gate 2 is formally closed. |

Reproduce package verification after generating the named evidence runs:
`python Production/Tools/package_gate2_accepted.py`.
This writes only the additive current page and its manifest. Prior Gate 2
validation scripts/manifests remain snapshots of their original build/evidence;
they are not relabeled as current accepted-pose validation.
