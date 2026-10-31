# SURLHWA Production — active Q1 spatial direction

Owner ruling: **Q1 — Soft Orthographic Quarter View is the active Production
spatial baseline.** Q2 is REFERENCE_ONLY; C1 remains historical accepted evidence.
Do not revert to C1 or explore Q2 without explicit owner direction. ACT 2 has not
begun. Gameplay, Canon and architecture authorities remain unchanged.

Open `Unity/Assets/Surlhwa/Scenes/Bar_Q1.unity`. Normal Unity Build/Build And Run and
`CampaignBuilder.BuildPlayer` / `BuildExistingPlayer` and `run_unity.ps1 -Operation Build`
select Q1. Existing ACT 1 launchers target that Player; simulated production input
and OS-native evidence remain separately classified. The build lives at
`Validation/Builds/Q1Production/SurlhwaQ1.exe`; historical C1/ACT 1/QuarterView
builds and review evidence are retained separately. Q1 has no player camera
selector. Existing focused bartending is retained, returning to Q1 afterward.

Q1 and the bounded polish are now **owner approved**:
[recorded ruling](Validation/Q1_OWNER_CONFIRMATION.md). Normal builds compile the
accepted scene directly; historical spike reconstruction is an explicit utility.
The ACT 1 integration adds a short nonblocking focus reveal and Q1 memory facing.
Final Gate 2 package: [current review page](Validation/review-gate2-current.html),
[technical report and evidence limits](Validation/GATE2_ACCEPTED_BASELINE.md).
Use `Tools/run_gate2_q1.ps1 -Start New -Height 1080` for isolated interactive review;
`First`, `Warmth`, `Clarity`, `QuietClarity`, `OpenMemory` and `QuietMemory` select earned checkpoints.
ACT 2 remains stopped pending the one final Gate 2 owner approval.

The Prince seated pose at `a19a84a` is owner-accepted (2026-10-31).
Preserve it and the locked Q1 staging; further pose work requires a clear problem
found in a later full-scene review. Final cast-wide art/animation polish remains
eligible. [Acceptance and scope](Validation/Q1_OWNER_CONFIRMATION.md).

Preserved bounded-polish review: [comparison page](Validation/review-q1-polish.html),
[report](Validation/Q1_POLISH_REVIEW.md), [plan](Validation/Q1_POLISH_PLAN.md).
Use `Tools/run_q1_polish.ps1 -EvidenceName q1-owner-01 -Interactive` for a fresh
isolated review save. Running the Production Player directly uses the existing
ACT 1 save namespace/format; the polish introduces no migration.

## Historical Pass 1 record

Open **Production/Unity** with **Unity 6000.3.7f1**. The inherited repository-root Unity project is frozen reference history and is not the new project's build input.

This pass implements a hybrid winter-bar spatial proof for HG01 camera/depth, HG02 medium coherence and HG03 scale/grounding. The owner approved C for HG01/HG02 on 2026-10-12 and requested bounded C1 polish for HG03. It contains no bartending, narrative progression, save system, production UI or audio composition.

Execution and validation: `Validation/PASS1_PLAN.md`, `Validation/PASS1_PROGRESS.md`. Raw logs, renders and builds remain local under ignored `Validation/Evidence/` and `Validation/Builds/`; small result and provenance manifests are tracked.

Approved spatial baseline: [C1 bounded polish](Validation/REVIEW_C1.md), [native C1 index](Validation/review-c1.html) and [C versus C1 overview](Validation/C_C1_COMPARISON.png). HG01/HG02/HG03 are APPROVED: C1 / Pixel Cutaway Hybrid, pixel-art first and selective 2.5D second. HG04 Core Domain/Stir and the bounded Slice are in implementation; [owner baseline and ACT 1 excerpt scope](Validation/HG04_OWNER_BASELINE.md), [plan](Validation/HG04_IMPLEMENTATION_PLAN.md), [progress](Validation/HG04_PROGRESS.md). [Original C notes](Validation/REVIEW_C.md), [native C index](Validation/review-c.html) and [A/B/C overview](Validation/ABC_COMPARISON.png) remain historical evidence. A/B/C are REFERENCE_ONLY.

Historical review: [A/B HG01–HG03 notes](Validation/REVIEW_HG01_HG03.md) and [A/B comparison index](Validation/review.html). Native originals and four movement clips remain linked there. Readiness for review is separate from artistic acceptance.

Start with the [C versus C1 comparison board](Validation/C_C1_COMPARISON.png), then inspect the native C1 images through the index. Don Quixote is **VISUAL_FIXTURE_ONLY**; his inclusion does not change Canon ACT order or identify the first canonical special guest.

Historical A/B reproduction from the repository root (REFERENCE_ONLY; local Editor license required). Current C1 commands are in `Validation/REVIEW_C1.md`:

```powershell
python Production/Tools/prepare_reference_art.py
pwsh -File Production/Tools/run_unity.ps1 -Operation Tests -EvidenceName final-unity-tests
python Production/Tools/test_review_outcome.py
pwsh -File Production/Tools/run_unity.ps1 -Operation Build -BuildMethod Surlhwa.Editor.SpatialProofAutomation.BuildPlayer
python Production/Tools/record_build_identity.py
$reviewName='captures-owner-001' # Use a fresh name for each run; existing evidence is preserved.
pwsh -File Production/Tools/run_player_review.ps1 -EvidenceName $reviewName
pwsh -File Production/Tools/encode_motion.ps1 -EvidenceName $reviewName
python Production/Tools/inspect_captures.py "Production/Validation/Evidence/$reviewName"
python Production/Tools/write_validation_summary.py
python Production/Tools/build_review.py
```

The motion encoder uses locally installed FFmpeg at 12fps and verifies native stream dimensions/frame counts. The current local review already includes these clips. `Surlhwa/Rebuild spatial proof` in the new Editor regenerates the scene and imported assets; it does not read old Unity scenes or copy settings. Editor startup and native build preprocessing (including ordinary Build / Build And Run) validate/regenerate the ignored runtime JSON while preserving its tracked GUID metadata. Capture validation requires successful initialization, a matching run identity, zero process exit and the complete Player log. For interactive review, launch `Validation/Builds/Windows/SurlhwaSpatialProof.exe` without capture arguments; the review-only controls select fixed camera/fixture presets. No player preferences or save namespace is read or written.

`Tools/test_player_initialization.ps1` repeats the negative built-Player regression using a temporary change to this Production project's authored JSON, restores its exact bytes in `finally`, and records the expected failure. It replaces the local Player with that negative probe; always rebuild the valid Player afterward before positive captures.

Frozen inputs can be checked without writing to them:

```powershell
python Production/Tools/verify_preservation.py
```
