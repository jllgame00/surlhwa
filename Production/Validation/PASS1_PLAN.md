# Foundation and Hybrid Spatial Proof Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task by task. Steps use checkbox syntax for tracking.

**Goal:** Deliver one real rendered winter bar in which the owner can judge camera/depth, 2D/3D coherence, and character grounding before HG04 work.

**Architecture:** New Unity project in Production/Unity. Immutable typed JSON definitions flow through ContentIO into pure Domain values and a small Application review selection; Unity adapters own geometry and visual bindings. One shared room and fixed authored sprite planes support two camera projections, without gameplay or progression state.

**Tech Stack:** Installed Unity 6000.3.7f1; minimal compatible URP Universal Renderer Forward, official Json.NET, Input System, Unity Test Framework. Python/Pillow only for evidence inspection/boards; selected existing PNGs remain unchanged.

**Spec:** User's IMPLEMENTATION PASS 1 attachment (2026-10-11), approved SURLHWA_ARCHITECTURE/01,03,10,11,21,30,31,42,50,51 and Final Canon/11,12,70,71,73. Authoritative absolute roots are in preservation-baseline.json. The user's explicit implementation sequence authorizes inline execution and limits this pass to HG01–HG03.

## Global Constraints

- Worktree D:\univ\surlhwa\SURLHWA_PRODUCTION, branch production-rebuild, baseline 93939bb6179e51504a1bf3b036abcbe2cbb895b3. No branch/worktree creation.
- All new files live under Production/. Frozen RC1 and external specifications stay read-only; inherited legacy project is REFERENCE_ONLY.
- No Stir, tools, full UI, audio system, narrative/progression, campaign, save system, or full Vertical Slice.
- Unity root Production/Unity. Bootstrap + Bar. No copying old scenes, prefabs, controllers, caches, or settings.
- Same scene, art, light, staging and target framing for orthographic A / restrained-perspective B. No artistic winner selected.
- World MeshRenderer planes, opaque alpha clipping, real depth write/test, fixed orientation set once, explicit feet/seat anchors, no free-camera controller.
- Native 1280×720 and 1920×1080 captures for both variants, original files retained. Evidence never implies human approval.
- Bounded commits only after relevant validation and diff inspection; no push.

## Review Focus

1. Duplicate JSON keys, unknown fields/version, invalid ASCII IDs and missing visual bindings must fail explicitly.
2. Korean Unicode and immutable loaded definitions must survive round trip without normalization or mutation leaks.
3. Transparent sprite margins must not occlude the room; actual counter geometry must occlude opaque body pixels.
4. Capture switching must not move actor roots/feet or silently modify material/geometry between A/B.
5. Editor-generated cache, build output, hidden Unity errors and external-file changes must not enter commits or masquerade as PASS.

## Task 1: New Unity foundation and preservation baseline

**Files:** Production/.gitignore, README.md, Unity/Packages/{manifest,packages-lock}.json, Unity/ProjectSettings/, Validation/Manifests/preservation-baseline.json, Tools/verify_preservation.py.
**Interfaces:** Produces the independent Unity project root and exact read-only source manifest.

- [x] Record production Git identity, external corpus hashes, frozen RC1 status/index/65 dirty-file hashes before modification.
- [x] Create new project using installed 6000.3.7f1. Resolve only packages needed for this proof; document each direct dependency.
- [x] Verify project import/compile and that caches are ignored. Verify preservation baseline without modifying sources.
- [x] Inspect intentional changes and commit the foundation when validated.

## Task 2: Minimal immutable content/domain/application boundary

**Files:** Unity/Assets/Surlhwa/{Domain,Application,ContentIO,Tests/EditMode}/, Content/spatial-proof.json, Content/spatial-proof.schema.json.
**Interfaces:** StableId value types; immutable GuestDefinition and SpatialProofContent; SpatialProofLoader.Load(string); ReviewSession selects a fixture/camera without creating campaign facts.

- [x] Write failing behavior tests: typed Korean load/round trip; invalid version/ID; duplicate JSON key/ID; missing reference; unknown field; immutable collection isolation.
- [x] Run the tests against unimplemented loader contract and record RED evidence.
- [x] Implement only the loader, immutable values, and review selection needed by this pass. Generate runtime content from the one authored source.
- [x] Run the EditMode suite, read results, and commit the content foundation.

## Task 3: Shared hybrid room and character bindings

**Files:** Unity/Assets/Surlhwa/{Editor,Presentation,Art,Scenes}/, SourceArt/PROVENANCE.json.
**Interfaces:** Deterministic SpatialProofBuilder creates Bootstrap/Bar and explicit actor/floor/seat/hand/dialogue/path/workbench/memory anchors. ReviewStage references the room and authored camera presets.

- [x] Define integration assertions for required room forms, three distinct actors, references, feet/seat contact, no missing scripts, fixed cameras and actual depth occlusion before implementing the builder.
- [x] Build clean low-detail floor/wall/U-counter/stools/shelves/window/door/lamps with restrained warm interior and cool exterior. Reserve an empty memory anchor and future resident workbench.
- [x] Selectively copy unchanged bartender, ordinary charcoal and Don Quixote PNGs with source/hash/status ledger; add fixed cutout planes, standing/seated pivots and restrained contact patches.
- [x] Verify anchor/reference contracts and real rendered occlusion. Inspect renders; correct objective contact defects before expanding anything.
- [x] Commit intentional blockout and character presentation after compile/integration validation.

## Task 4: Camera A/B, rendered validation and owner package

**Files:** Unity/Assets/Surlhwa/{Presentation,Editor,Tests}/, Tools/{inspect_captures,build_review}.py, Validation/{Manifests,REVIEW_HG01_HG03.md,review.html}.
**Interfaces:** Capture matrix uses the same stage: Empty, OrdinarySeated, QuixoteSeated, Composition, GroundingInspection plus standing/path checks. Native-resolution capture manifest records camera/render/asset identity and evidence tier. Player measurement records frame/memory/count/GC/batches where supported.

- [ ] Test stage switching/root stability, camera matching/fixed-view and missing-reference failure; verify tests fail before new behavior.
- [ ] Capture A/B at 720p/1080p with one shared scene; include original full-resolution grounding inspection frames and bounded path motion evidence.
- [ ] Build and run a Windows Player; collect honest idle baseline and objective shader/occlusion/reference/resolution results. Separate Editor evidence from Player evidence.
- [ ] Inspect original captures for magenta, clipping, counter penetration and feet contact. Produce review index with HG01/HG02/HG03 OWNER_REVIEW_PENDING, exact provisional camera/material/anchor values, facts, limits and tradeoffs.
- [ ] Run final suite, preservation and diff checks; request one fresh whole-branch reviewer under executing-plans, address important findings, commit bounded result, stop before HG04.

## Plan self-review

The four tasks cover the user's sections 0–25. Shared interfaces are Task 1 project → Task 2 assemblies → Task 3 builder/stage → Task 4 captures/Player review. The pass intentionally narrows Architecture/51's full Foundation by deferring save/input gameplay and broader domain implementation, as explicitly directed by the user. No new product approval or architecture decision is required; provisional spatial values are recorded for owner review.
