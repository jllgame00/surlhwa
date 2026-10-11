# C — Pixel Cutaway Hybrid / HG01–HG03

HG01 — READY_FOR_OWNER_REVIEW  
HG02 — READY_FOR_OWNER_REVIEW  
HG03 — READY_FOR_OWNER_REVIEW

Owner judgment remains pending. C is not selected as final. HG04 / Stir / bartending A/B / narrative production UI / audio / campaign / full Slice remain unimplemented.

Start with [A/B/C overview](ABC_COMPARISON.png), then [native C review index](review-c.html). [Historical A/B review](review.html) and all native originals remain separate and byte-identical to the historical hash inventory.

C follows the actual owner anchor: exposed snowy world ground around an open U-counter bar, a low rear plane and no enclosing side walls. Large surfaces use palette-limited point-sampled source textures; counter, stools, selected shelves, bottles and lamps retain depth. No screen filter, realistic PBR or smooth lighting pass. The warm wall light patches are quantized source-texture regions, not runtime light bloom. Snowfall marks are static authored world quads; no production weather system is claimed.

Camera: fixed orthographic, pitch 28°, yaw/roll 0°. Half-height 4.1 m normally, 1.65 m inspection, 4.9 m wide; aspect 16:9, near/far 0.1/60 m. No post effects, HDR, MSAA or camera-facing sprite scripts. Native output is 1280×720 and 1920×1080.

Character scale was rebuilt: bartender 56 PPU, ordinary/special 64 PPU, shared within each family including poses; root scale 1. Seat anchors use the rotated source hip, feet meet the real floor, the seat lip clears the visible knee, the real counter blocks the bartender's lower body. Don Quixote remains **VISUAL_FIXTURE_ONLY** and does not establish the first canonical special guest or alter ACT order. All supplied sprites remain provenance-pending reference assets.

Native suite 44/44; Python evidence contracts 14/14; standalone .NET foundation 25/25. C build has zero errors/warnings. Native stills 12, plus counter removal diagnostic and two 121-frame / 12fps motion clips. Source/Player/capture/run hashes are recorded in C-specific manifests. Frozen RC1 dirty state and all 74 specification hashes are unchanged; A/B preservation is checked against 892 original files.

Measured on 12th Gen Intel(R) Core(TM) i7-12700KF / NVIDIA GeForce RTX 3070, Unity 6000.3.7f1: 60s native offscreen 1080p sample, mean 0.72 ms / P95 0.86 ms; draws 1624; idle GC mean 0 bytes/frame. GPU time, display latency, VRAM pressure and minimum-spec support were not measured. Pose replay is deterministic fixture evidence, not final animation or gameplay navigation.

## Reproduce C without replacing A/B evidence

```powershell
./Production/Tools/run_unity.ps1 -Operation Tests -EvidenceName c-tests-new
./Production/Tools/run_unity.ps1 -Operation Build -EvidenceName c-build-new -BuildMethod Surlhwa.Editor.PixelCutawayBuilder.BuildPlayer
python Production/Tools/record_build_identity.py --variant-c
./Production/Tools/run_player_review.ps1 -EvidenceName c-review-new -Executable Validation/Builds/VariantC/SurlhwaPixelCutaway.exe
./Production/Tools/encode_motion.ps1 -EvidenceName c-review-new -CameraVariants C
python Production/Tools/inspect_c_review.py Production/Validation/Evidence/c-review-new
python Production/Tools/build_c_review.py
python Production/Tools/c_history.py
python Production/Tools/verify_preservation.py
```

Use fresh evidence names. `-CPreview` is deliberately rejected by the final validator. Review-only controls appear when launching the C Player interactively; automatic captures contain no review UI. The historical materials can be auto-upgraded by Unity while opening A/B test fixtures; the completed C revision reinstates their original recorded bytes before final historical verification.
