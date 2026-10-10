# SURLHWA Production — Pass 1

Open **Production/Unity** with **Unity 6000.3.7f1**. The inherited repository-root Unity project is frozen reference history and is not the new project's build input.

This pass implements a hybrid winter-bar spatial proof for HG01 camera/depth, HG02 3D/2D coherence and HG03 scale/grounding. All three gates require owner judgment. It contains no bartending, narrative progression, save system, production UI or audio composition.

Execution and validation: `Validation/PASS1_PLAN.md`, `Validation/PASS1_PROGRESS.md`. Raw logs, renders and builds remain local under ignored `Validation/Evidence/` and `Validation/Builds/`; small result and provenance manifests are tracked.

Owner review: [HG01–HG03 notes](Validation/REVIEW_HG01_HG03.md) and [A/B comparison index](Validation/review.html). Native originals and four movement clips are linked there. Readiness for review is separate from artistic acceptance.

Start with the [six-frame comparison board](Validation/HG01_HG03_COMPARISON.png), then inspect the native images through the index. Don Quixote is **VISUAL_FIXTURE_ONLY**; his inclusion does not change Canon ACT order or identify the first canonical special guest.

To reproduce from the repository root (local Editor license required):

```powershell
python Production/Tools/prepare_reference_art.py
pwsh -File Production/Tools/run_unity.ps1 -Operation Tests -EvidenceName final-unity-tests
python Production/Tools/test_review_outcome.py
pwsh -File Production/Tools/run_unity.ps1 -Operation Build
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
