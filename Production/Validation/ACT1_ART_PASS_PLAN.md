# ACT 1 Production Art Pass implementation plan

> Execution: superpowers:executing-plans, inline implementation, one fresh review at completion. Owner explicitly requests autonomous work and bundled Gate 2 review; no routine design approval gate.

**Goal:** Make the existing ACT 1 Player reviewable as a quiet, lived-in SURLHWA bar, using the provisionally accepted C1 composition.

**Architecture:** Extend the existing ACT 1 presentation builders and read-only presenters. Keep C1 camera, floor/counter dimensions, movement/contact anchors and all gameplay/content/save authorities. New art lives in ACT 1 assets; historical C1/Slice art stays unchanged.

**Stack:** Unity 6000.3.7f1 / URP unlit pixel planes, existing selective 3D geometry, ImageGen source PNGs, deterministic source extraction/import, existing TMP/InputSystem and proof harness.

**Spec:** Owner `ACT1_PRODUCTION_ART_PASS_START`, Docs/00, Docs/01, Docs/11, current Production source and Gate 1 decisions. The latest owner instruction supersedes older closed-room presentation wording in Docs/18/21. Source Canon/Architecture remain read-only.

## Constraints and audit

- Baseline f6d83d8, existing isolated `production-rebuild` worktree. Owner-local `Production/Validation/review-slice.html` is excluded.
- Keep camera orthographic size 3.7 and 28-degree elevation; preserve exposed snow and open sides. No camera A/B, new cocktails, story edits or ACTs 2–7.
- Actual 720p captures show primitive bottles/tools, repetitive wood, coarse patch snow and reference character poses. Existing request/focus/knowledge fixes must remain.
- Use new source artwork as modular surfaces/silhouettes, never a flattened scene card. Retain actual volume/layer/ice/garnish meshes behind transparent tool shells.
- Ordinary guests remain anonymous charcoal coats. Prince keeps blond/teal/gold/cream identity; bartender keeps brown hair, white shirt/dark waistcoat. One bounded family; no full cast production.
- Codex structure/knowledge projection remains. Core reading/workbench chrome may be refined without obscuring controls or introducing scoring feedback.
- Native input remains environment-blocked under prior owner instruction. Offscreen real Player rendering/input simulation is available and must be labelled accurately.
- Art/motion/audio feel are owner judgments, not automated PASS. Keep presentation debt explicit.

## Review focus

1. New shells cannot obscure actual fill/Float boundaries or manufacture contents; verify all four methods and empty/partial/served states.
2. Seated hips/feet and walk frames must stay supported; verify silhouette baseline and C1 contacts.
3. Exterior dressing must retain negative space and avoid covering the U arms, character corridor or memory.
4. Korean text, focus, modal and click targets remain legible at 720p/1080p; retain existing layout regressions.
5. Rebuilding must not rewrite historical scenes/assets or source content/save policy; inspect serialized diffs and preservation hashes.

## Tasks

- [x] A — Source foundation: preserve input hashes, generate a compact modular environment atlas and coherent ACT 1 character source set; extract Point/uncompressed PNGs with original prompts, bounds and hashes. Reuse existing pixel tool source where appropriate.
- [ ] B — Environment: apply new wood surfaces, shelves/bottles, lamps, stools, small counter life and empty memory-ready furniture; replace patchy snow with quieter shaped banks, distant silhouettes and warm spill. Apply only to Bar_Act1 through CampaignArtBuilder; render and inspect same camera.
- [ ] C — Characters: bind new bartender/Prince/two ordinary variants at existing actor scale/contact anchors, restrained listening/idle and existing travel phases. No story or pacing state change.
- [ ] D — Tools/UI/audio: bind authored pixel shells to existing transforms, add state-driven Float spoon/strainer where missing; refine core panel edges/materials/focus without restructuring Codex. Audition and tune grounded foot cue if supported by evidence.
- [ ] E — Verification/package: compile and affected Unity suites; new art/authority regressions where meaningful; actual Player 720p/1080p captures and movement/tool evidence; preserved Continue saves; source/asset provenance and debt inventory; fresh review, fixes, intentional diff/serialization inspection, bounded commits and Gate 2 art review package.

## Ledger

2026-10-25: audited current repository, source presentation, existing Player captures and C1 anchor. Native input is not needed to author assets or run offscreen evidence; no native retry planned.
Ruling: use the established tracked Production/Validation plan/ledger convention instead of a disposable skill workspace — preserves project handoff history; cost: one additional review document.
Ruling: use ImageGen for authored modular raster art and existing code-native geometry for supports/material placement — keeps C1 spatial approval while replacing provisional pixels; cost: generated source still needs production extraction and owner art review.

2026-10-25 source foundation: three original built-in ImageGen atlases, exact prompts and source hashes retained. Prepared 33 new environment/material/character PNGs plus 10 unchanged existing pixel tool assets. Prepared-assets.png inspected; corrected source crop boundaries that included adjacent-row pixels, and seated canvas baseline to preserve ground contact. New import metadata uses Point/uncompressed/no mipmaps/no NPOT resizing. Generated source art is a milestone candidate, not owner approval.

Baseline evidence: sandbox standalone run 132/149 (17 atomic save publication Access denied failures); identical local unsandboxed run 149/149 PASS. No gameplay fix was made. Preservation: 74 source specification hashes, RC1 HEAD/branch/index/status and 65 dirty paths unchanged; owner-local review-slice.html SHA256 B18D7CA3459788E6232B3F4156990A50D45B4F0AAA495544F929AB6C3557EECF unchanged.

Unity validation: sandbox startup failed before load; local Editor attempt exited 198 with `No valid Unity Editor license found`, no entitlement for com.unity.editor.headless. VALIDATION_BLOCKED_BY_ENVIRONMENT. Owner will notify after activating the license. No Editor/runtime PASS claimed. No scene was regenerated while this blockage remains.

Source integration prepared, not yet applied: the bounded existing-scene upgrader retains C1 transforms/camera/contact anchors, replaces visual surfaces and adds modular dressing. ACT 1 art set supplies pose materials and optional tool shells; actual liquid geometry remains in the existing read-only workbench presenter. Core UI has restrained material/edge treatment and an active-method marker. Original felt/wood footstep candidate preserves the old clip, event cadence and mix policy.

Compile: complete production domain/application/presentation/Editor/test source compiled against installed Unity assemblies using PresentationCompile.csproj: 0 errors, one inherited CS0108 warning on CampaignPlayerProof.audio. This is a static C# compile, not Unity import, Shader/asset validation or Player execution. New Unity art/contact tests remain NOT_RUN until licensing is restored.

2026-10-25 source checkpoint: added a fourth ImageGen atlas for wide shelves, preserving bottle proportions in the existing shallow shelf space. Total remains 43 prepared PNGs (33 new, 10 unchanged tool sources), plus one separately synthesized footstep. The contact sheet includes every asset. All four original source hashes and declared extraction bounds are retained. Re-extraction leaves all 88 current runtime-source PNG/WAV/meta files byte-identical. Source verification passes 48 checks, including unique GUIDs throughout Production assets. Preservation checks pass; no scene, prefab, content, save schema or project setting was changed.

The one fresh review found five Important source issues (cocktail fill bounds, opaque jigger fill, Float source/spoon continuity, cocktail ice bounds, unsaved-scene protection) and one Minor preview clipping issue. All are corrected at source; see ACT1_ART_SOURCE_REVIEW.md. Three new CampaignArtTests and one CampaignInputTests regression compile but have not executed. The reviewer did not certify runtime or aesthetic acceptance. The subsequent shelf atlas refinement is executor-inspected source art, not a second independent review.

Ruling: retain an internally complete source checkpoint while the licensed Editor is unavailable — preserves bounded assets/code/provenance without fabricating scene or Player evidence; cost: this commit is not an integrated ACT 1 milestone and must not be presented as Gate 2 completion.

## Resume boundary after license activation

1. Import with Unity 6000.3.7f1; run the new art tests against the unchanged scene to retain the missing-art integration failure, distinguishing any other genuine failure.
2. Run `Production/Tools/run_unity.ps1 -Operation Build -BuildMethod Surlhwa.Editor.CampaignProductionArtBuilder.UpgradeScene -EvidenceName art-scene-upgrade`. Inspect the Bar_Act1 diff and generated material/mesh/ArtSet references. Reject unrelated scene, prefab, font, project-setting or historical C1 churn. Repeat application must preserve asset GUIDs and the accepted spatial anchors; new dressing object IDs may be regenerated.
3. Run the affected domain/application/Campaign presentation, input, art, review and Slice/focus regressions. Do not invoke historical scene-regeneration fixtures. Confirm all four methods, active-stream cancellation, actual liquid/ice/layers and existing knowledge filtering.
4. Build with CampaignBuilder.BuildExistingPlayer. Capture real 720p/1080p stills and motion through normal ACT 1 progression; inspect C1 framing, snow reading, prop/seat contact, silhouettes, Korean UI, Stir and Manwol/Float. Refine art within C1 as required by actual renders.
5. Verify Continue from preserved earned checkpoints, source/Player identity, audio contact and no clipping; keep native input separately blocked until the owner reauthorizes desktop availability. Final listening and aesthetic judgment remain bundled owner work.
6. Update asset adoption inventory and the actual Gate 2 package with versioned Player evidence, remaining presentation debt and intentional serialized commits. The source gallery is supplementary, never a substitute for gameplay captures.

Current deliverable: `review-act1-art-source.html` and source provenance, **SOURCE_READY / UNITY_INTEGRATION_PENDING**. Tasks B–E remain open until actual integration, inspection and evidence are completed. The next required action depends on licensed Unity execution; no further safe source-only work can establish the requested in-game art quality.
