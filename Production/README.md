# SURLHWA Production — Pass 1

Open **Production/Unity** with **Unity 6000.3.7f1**. The inherited repository-root Unity project is frozen reference history and is not the new project's build input.

This pass implements a hybrid winter-bar spatial proof for HG01 camera/depth, HG02 3D/2D coherence and HG03 scale/grounding. All three gates require owner judgment. It contains no bartending, narrative progression, save system, production UI or audio composition.

Execution and validation: `Validation/PASS1_PLAN.md`, `Validation/PASS1_PROGRESS.md`. Raw logs, renders and builds remain local under ignored `Validation/Evidence/` and `Validation/Builds/`; small result and provenance manifests are tracked.

Owner review: [HG01–HG03 notes](Validation/REVIEW_HG01_HG03.md) and [A/B comparison index](Validation/review.html). Native originals and four movement clips are linked there. Readiness for review is separate from artistic acceptance.

To reproduce from the repository root (local Editor license required):

```powershell
python Production/Tools/prepare_reference_art.py
pwsh -File Production/Tools/run_unity.ps1 -Operation Tests
pwsh -File Production/Tools/run_unity.ps1 -Operation Build
python Production/Tools/record_build_identity.py
pwsh -File Production/Tools/run_player_review.ps1 -EvidenceName captures-final
pwsh -File Production/Tools/encode_motion.ps1 -EvidenceName captures-final
python Production/Tools/inspect_captures.py Production/Validation/Evidence/captures-final
python Production/Tools/build_review.py
```

The motion encoder uses locally installed FFmpeg at 12fps and verifies native stream dimensions/frame counts. The current local review already includes these clips. `Surlhwa/Rebuild spatial proof` in the new Editor regenerates the scene and imported assets; it does not read old Unity scenes or copy settings. Editor startup and builds validate/regenerate the ignored runtime JSON while preserving its tracked GUID metadata. For interactive review, launch `Validation/Builds/Windows/SurlhwaSpatialProof.exe` without capture arguments; the review-only controls select fixed camera/fixture presets. No player preferences or save namespace is read or written.

Frozen inputs can be checked without writing to them:

```powershell
python Production/Tools/verify_preservation.py
```
