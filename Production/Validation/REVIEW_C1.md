# C1 — bounded Production polish

HG01 — APPROVED (C / Pixel Cutaway Hybrid)<br>
HG02 — APPROVED (pixel-art first, selective 2.5D second)<br>
HG03 — READY_FOR_OWNER_REVIEW

Owner ruling: 2026-10-12. A/B are REFERENCE_ONLY. C1 retains the accepted C architecture; HG04 is on hold.

[C versus C1 overview](C_C1_COMPARISON.png) · [All six native fixtures and clips](review-c1.html) · [Original C evidence](review-c.html) · [Historical A/B/C overview](ABC_COMPARISON.png)

## Focused changes

- U counter: top width 8.0m, returns 3.7m long / 1.1m wide, top thickness 0.13m. Bartender stands within the working area; guests sit outside the front seating edge. Rear wall remains a 0.06m plane and the bar stays open.
- Snow: removed C's nested blue bands. Broken neutral snow banks and cold shadows now cross dark winter ground; lighter accumulation stays on the structure. Quiet static snowfall marks remain. No production weather simulation.
- Scale: bartender 52 PPU (C: 56); both guest families including walking/seated poses 70 PPU (C: 64), all root scales 1. Seat height/support was recomputed for the new source density; opaque hip, knee, floor contact and real counter occlusion checks remain active.
- Density: normal orthographic half-height 3.7m (C: 4.1m); wide 4.55m (C: 4.9m); inspection remains 1.65m. Playable floor spans approximately 73% of normal frame width with exterior margins. Fixed pitch 28°, yaw/roll 0°, 16:9, clip 0.1/60m; no camera tracking, perspective, HDR, MSAA or post effects.

Native captures are 1280×720 and 1920×1080 for Empty, OrdinarySeated, SpecialSeated, Composition, GroundingInspection and Wide. Don Quixote is VISUAL_FIXTURE_ONLY and establishes no ACT-order claim. Existing source art remains provenance-pending; no new campaign/memory content is adopted.

## Evidence and limits

Full native suite 63/63; Python evidence contracts 22/22. The C1 Player build succeeds with zero errors/warnings. Run `718ced1bd6544789b628deffbc69ad00` exits 0, errors `[]`; capture validator PASS. Actual counter removal reveals 4499 opaque lower-body pixels. Two native clips each contain 121 frames at 12fps; replay demonstrates pose/contact behavior, not final navigation or animation acceptance.

On 12th Gen Intel(R) Core(TM) i7-12700KF / NVIDIA GeForce RTX 3070, Unity 6000.3.7f1: 60.013s explicit offscreen 1080p sample, 87,645 requests; mean 0.68ms / P95 0.77ms, 1615 draws/batches, zero idle GC. This is a Development/offscreen baseline; GPU time/display latency/minimum hardware/cross-platform quality are unmeasured. No optimization claim.

Separate C1 scene/assets/build/captures/manifests preserve A/B/C review evidence. `c_history.py`, `c1_history.py` and `verify_preservation.py` guard original files and frozen owner/specification state. The prior C source identity remains a historical record of its implementation commit; current shared source changes belong to C1's new identity inventory.

The fresh read-only technical review of implementation `5b80032` and this package found no Critical or Important issues. It independently verified all native images/motion samples, source/Player fingerprints, capture identities, clips, board links and preservation. One Minor is documented/deferred: 393 trailing-space findings in Unity-generated scene/mesh YAML have no runtime effect; handwritten C#/Python/PowerShell checks pass. Generated serialization is retained rather than normalized. Automatic freshness validation checks the executable only; the complete inventory was verified separately by executor and reviewer. [Review result and scope rulings](C1_PROGRESS.md).

## Reproduce using fresh evidence names

```powershell
./Production/Tools/run_unity.ps1 -Operation Tests -EvidenceName c1-tests-new
./Production/Tools/run_unity.ps1 -Operation Build -EvidenceName c1-build-new -BuildMethod Surlhwa.Editor.PixelCutawayBuilder.BuildC1Player
python Production/Tools/record_build_identity.py --variant-c1
./Production/Tools/run_player_review.ps1 -EvidenceName c1-review-new -Executable Validation/Builds/VariantC1/SurlhwaPixelCutaway.exe
./Production/Tools/encode_motion.ps1 -EvidenceName c1-review-new -CameraVariants C1
python Production/Tools/inspect_c1_review.py Production/Validation/Evidence/c1-review-new
python Production/Tools/build_c1_review.py
python Production/Tools/c_history.py
python Production/Tools/c1_history.py
python Production/Tools/verify_preservation.py
```

`-CPreview` is never accepted as final capture evidence. Historical C native tests regenerate its scene and Unity may upgrade old material serialization; exact recorded historical bytes must be reinstated before the final guards pass. The C1 package builder reads the completed `c1-native-final.xml` evidence.
