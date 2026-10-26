# SURLHWA — Q1 ACT 1 / final Gate 2 review

Status: **GATE2_Q1_ACT1_READY_FOR_OWNER_REVIEW**.
Objective integration and the consolidated evidence package are complete.
Final subjective Gate 2 acceptance remains with the owner; ACT 2 has not begun.

Q1 and its bounded polish are **owner approved**. Q2 is REFERENCE_ONLY; C1 is
historical accepted evidence. This review concerns the coherent ACT 1 experience
under Q1. It does not reopen camera direction or begin ACT 2.

## Integration

Normal Production builds now compile the accepted `Bar_Q1` directly, without
reconstructing it from the historical spike. Active build:
`Builds/Q1Production/SurlhwaQ1.exe`. The approved polish Player is preserved in
`Builds/Q1ApprovedPolish`; original Q1/C1 reports and native captures are unchanged.

The focused bartending projection switches immediately, preserving the existing
tool/input mapping. A 220 ms warm-charcoal dim/reveal makes entry and return
intentional. It sits below reading UI, has no raycaster, never gates input or
progression, and reuses one canvas. There is no added sound cue or gameplay delay.
The earned star faces the Q1 camera; receipt, chair offset, visibility and
glowing/quiet outcome remain authoritative and unchanged.

The fixed 38° pitch / −28° yaw / 4.85 orthographic composition, +12% normal-view
character presence, Prince side-quarter seat, U-counter and snowy negative space
are unchanged. No accepted art is regenerated. No scene/prefab or project-setting
serialization is needed for this integration.

## One owner gate

Use [the consolidated review page](review-gate2-q1.html) for all twelve areas:
quarter view/identity; snow and warm bar; character scale/direction/grounding;
ACT 1 pacing; Prince portrayal; all four methods; Manwol/Float; Open/Quiet and
memory; UI/Codex; animation; audio/music/silence; overall SURLHWA identity.

For interactive review from the repository root:

```powershell
powershell -File Production/Tools/run_gate2_q1.ps1 -Start New -Height 1080
```

Other `-Start` options: `First`, `Warmth`, `Clarity`, `QuietClarity`, `OpenMemory`, `QuietMemory`.
`Clarity` carries understood Warmth history; `QuietClarity` carries the actual
missed-Warmth history, so both farewell routes can be reviewed interactively.
Each launch creates an isolated save. Choose Continue for checkpoint starts.
Those checkpoints are copies of actual earned publications, not manufactured
state. The existing production save namespace and format remain unchanged.

## Evidence boundaries

- Targeted Unity tests cover Q1/input/art, ACT 1 flow, recognition/FIT/knowledge,
  save publication/Continue, Float and Return-Visit Understanding.
- Player evidence uses **simulated production InputSystem devices**, actual
  native-resolution camera/UI renders, and actual Unity audio. It is not
  OS-native input proof. Historical native-input environment debt remains separate;
  no new native-input attempt is claimed.
- Video samples about 5 Hz with timestamps. Automated reading waits are 1.5 seconds.
  These recordings expose content and transitions; interactive play remains
  necessary for final pacing, animation, input feel and audio judgment. Sampling
  can miss the short 220 ms reveal; assess that treatment in the interactive Player.
- Bounded performance uses the previous Q1 pose-held/offscreen harness. Its idle
  GC and frame timings are not a claim about normal-play allocation or minimum PC.

## Validation ledger

Initial targeted run: 48/49 passed. The single failure was the new EditMode test
using `SendMessage` for `OnDisable`, rejected by Unity's lifecycle guard. The test
now uses the existing reflection pattern. Focused rerun: 6/6 passed. A subsequent
capture-harness regression adds the frame-increment stop boundary; final Q1 rerun
passes 7/7, resolving all 50 unique targeted checks. Original results are retained.

The first full media attempt stopped at slot 13's fine lemon-juice measurement,
after 13 actual services. A 333 ms capture stall exposed the fixture's reactive
key release. The harness now predicts the next adapter step and releases simulated
keys before overshoot, retaining all production rates/timing. Review identified
and corrected a 50 ms frame-increment deadlock;
the exact boundary is covered by regression. A focused actual-save replay passed
43 checks, both remaining Shake services, Clarity, Open and earned memory. This
diagnostic replay is separate from the final complete ACT 1 evidence.

The next complete recordings passed the original gameplay/UI assertions, but a
closure audit found six added Q1 postconditions had been skipped because the
observer looked on the controller rather than the separate presentation root.
That lookup and the reveal-child checks are corrected. Final package validation
requires all six named postconditions in every final Player report; superseded
recordings remain retained separately.

The 1080p Quiet media run then exposed a stricter existing preparation boundary:
14.727195 ml of late soda was recognized but correctly failed the 14.75 ml minimum.
The capture fixture now aims within 0.2 ml and tightens its assertion from 0.5 to
0.25 ml. The captured boundary and exact 0.25 ml edge are regression-covered;
final Q1 tests pass 7/7. Production tolerances and actual liquid authority are
unchanged. The affected Quiet recording is rerun; the valid full Open recording
is retained with its exact `Builds/Gate2RecordedPlayer` archive. Inventory comparison
permits only the recorder and its test to differ from the final Player; every
other gameplay/presentation source, scene, asset, package and content file is
identical. This distinction is explicit in the validation manifest.

Production build: successful, zero build errors and warnings. Read-only review
found no important defects in transition/input safety, memory authority, capture
ordering, the accepted-scene build path or isolated launcher.

| Final evidence | Checks | Named native stills | Result |
| --- | ---: | ---: | --- |
| Full Open ACT 1 / 720p | 173 | 54 | PASS |
| Warmth return through Quiet / 1080p | 76 | 29 | PASS |
| Open completed-save Continue / 1080p | 11 | 10 | PASS |
| Quiet completed-save Continue / 720p | 11 | 10 | PASS |

Each report includes all six required Q1 postconditions: presentation root,
return projection, memory facing, exact reveal-canvas count, reveal cleanup,
and active material/shader references. Full Open has all 17 actual services,
17 natural departures and six earned recipe receipts. Quiet begins from its
byte-identical earned Warmth checkpoint, then plays through the remaining seven
services. Good recognized but mismatched Warmth plus understood Clarity produces
Quiet; understood Warmth and Clarity produce Open. Both keep one correct memory.
Completed Continue preserves both published save generations byte-for-byte.

The package contains 103 named 720p/1080p stills, 4,406 raw video frames, two
playthroughs and nine focused clips. Both original mixes are stereo 48 kHz,
with nonzero audio and zero clipped samples. Their recorded audio durations are
3.845 s (Open) and 1.985 s (Quiet) shorter than the last wall-clock frame timestamps.
Capture stalls therefore limit audiovisual synchronization; the exports use the
shorter stream. These are mood/content references, not timing certification.
Use the live isolated Player for footstep sync, transition feel and final listening.

## Bounded performance comparison

Same i7-12700KF / RTX 3070, held-pose offscreen harness, measured separately from
recording and encoding. Both new samples are retained; no optimization was made.

| Resolution | Approved Q1 p50 / p95 ms | Integration sample 1 | Integration repeat |
| --- | --- | --- | --- |
| 1280×720 | 0.490 / 0.571 | 0.532 / 0.702 | 0.527 / 0.682 |
| 1920×1080 | 0.497 / 0.592 | 0.541 / 0.765 | 0.523 / 0.676 |

All remain below 1 ms in this limited sample. The tail is modestly higher than
the prior measurement; exact timing equality is not claimed. Structural costs
are unchanged: 225 renderers, 46 materials, about 429–431 draws/batches, no realtime
lights or shadow lights. Allocated memory is 67.69–67.70 MiB, reserved 210.98 MiB,
and preallocated held-pose idle GC is zero. Normal-play GC, GPU timing and minimum
hardware performance are not certified by this harness.

Details: [validation manifest](Manifests/gate2-q1-validation.json),
[outcomes](Manifests/gate2-q1-outcomes.json),
[performance samples](Manifests/gate2-q1-performance.json).

## Preserved authority

Domain/application rules, actual liquid/container state, Stir/Float semantics,
recognition, FIT, knowledge/discovery, authored ACT 1 content, Prince choice,
Return-Visit Understanding, Open/Quiet, memory grants and save format are unchanged.
Current UI, character animation and audio/music policy are retained. Canon,
approved Architecture, frozen RC1 and the user's local `review-slice.html` remain
protected. Historical findings and evidence are not rewritten.

Final preservation check: all 74 specification files and the frozen RC1 snapshot
match; 918 protected native evidence files remain unchanged. No scene/prefab or
project-setting diffs. The accepted polish Player archive matches its original
inventory, and both media/current Players match their own exact inventories.

## Remaining debt

**HUMAN_SIGNOFF_REQUIRED:** the single final Gate 2 subjective review above.

**VALIDATION_DEBT:** OS-native four-method completion remains environment-blocked
historical evidence; simulation is not relabeled as native proof. Recorded media
does not establish sample-accurate audio/contact sync, 60 fps animation quality,
input latency or minimum-machine performance.

**DEFERRED_PRESENTATION:** directional walk/turn/sit transitions; oblique surface
pixel shimmer; deeper focused-view spatial continuity beyond this small reveal;
final environment art; final character/environment consistency; UI/Codex polish;
final audio/music polish. These do not reopen approved Q1.

**OUT_OF_SCOPE:** ACT 2–7, new gameplay, narrative expansion, camera exploration,
asset migration, bartending architecture replacement and full campaign QA.
