# Variant C implementation plan

Authority: owner revision attachment `64cfb8e7-f3dc-41ba-91ea-a933d2aa343e`, the read-only Final Canon / Production Architecture, and the actual layout anchor in `ArtSource/Reference/VisualDirection`. Start: `187dbc81c7a287fde4a7276b15d78634e08de96f`.

Design: a fixed orthographic, mildly elevated cutaway, with a shallow wood interior embedded in a navy snow field. Broad surfaces are flat, palette-limited pixel textures. Counter, stools, shelves and selected props retain physical depth. No screen pixelation, enclosing side walls, large perspective floor, gameplay, campaign facts, or production UI. Character families get authored C meshes and contact anchors; A/B assets remain historical.

## Task 1 — historical guard and native scene contracts

Files: `Tools/c_history.py`, `Validation/Manifests/c-history.json`, `Unity/Assets/Surlhwa/Tests/EditorIntegration/PixelCutawayTests.cs`.
Record hashes of A/B captures, validation, Player and scene assets before changes. Write native tests for exterior visibility, fixed camera, pixel surface import/materials, scale/contact, physical occlusion and initialization failure. Run `Tools/run_unity.ps1 -Operation Tests -EvidenceName c-red -TestFilter PixelCutawayTests`. Expected: missing C builder assertion, not compilation failure.

## Task 2 — C scene and separate Player

Files: new `PixelCutawayBuilder.cs`, `PixelCutawayStage.cs`, `PixelCutawayRun.cs`, C scene/assets; additive script parameters for C execute method / executable. Do not regenerate A/B scene assets. C fixtures are Empty, OrdinarySeated, SpecialSeated, Composition, GroundingInspection, Wide. C camera and capture interface use these integer fixture indices. Capture run requires initialized binding validation and run identity; failure exits nonzero. Build C into `Validation/Builds/VariantC`.
Run native contracts, then full native suite. Expected: all pass, A/B history and frozen RC1/spec preservation pass. Build and run C; inspect actual native renders and correct objective visual/contact defects.

## Task 3 — review evidence and owner handoff

Files: new C inspection/package tool, `Validation/REVIEW_C.md`, `Validation/review-c.html`, `Validation/ABC_COMPARISON.png`, C manifests and progress. Native 720p/1080p six fixtures (12 images), counter diagnostic, short deterministic movement proof, 60s offscreen 1080p performance sample. Preserve A/B originals. Review source hashes, run identity, process outcome, completeness, image sizes, startup/errors, native tests and untouched histories.
Run one fresh whole-change review using requesting-code-review; re-grade findings, one tested Critical/Important fix pass, defer minors. Commit bounded changes locally, no push or integration. Expected final state: HG01/HG02/HG03 READY_FOR_OWNER_REVIEW; final art/camera judgment pending; HG04 untouched.

## Review focus

1. Exterior snow really occupies world space outside the floor, including normal framing; no window-only workaround.
2. Pixel material/form language and selected physical depth; no fullscreen low-resolution filter or global PBR shading.
3. Guest/bartender scale, foot/seat/counter relationships, torso occlusion, alpha depth and actor stability across fixtures.
4. Evidence freshness, native resolutions, failure gating, original A/B preservation and honest performance limits.
5. Frozen RC1/specifications, scope boundaries, no automatic C approval, no HG04 implementation.
