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
- [x] B — Environment: new wood surfaces, shelves/bottles, lamps, stools, counter life and empty memory niche; quieter snow bands, banks, distant silhouettes and warm spill. Applied only to Bar_Act1 through CampaignProductionArtBuilder; inspected at the same camera.
- [x] C — Characters: bartender/Prince/two ordinary variants at existing actor/contact anchors, restrained idle and existing travel phases. No story or pacing state change.
- [x] D — Tools/UI/audio candidate: authored shells, state-driven Float spoon/strainer and restrained core panels/focus; foot cue integrated and recorded with no clipping. Final timbre, cadence and mix audition remains owner sign-off.
- [x] E — Verification/package: affected Unity suite, actual Player 720p/1080p and motion/tool evidence, preserved Continue saves, provenance/debt inventory, fresh source review with scoped executor follow-up, intentional serialization inspection and bundled Gate 2 art package. Final owner judgment and native-input debt remain open.

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

Historical source-only deliverable at dcc0297: `review-act1-art-source.html` and source provenance, **SOURCE_READY / UNITY_INTEGRATION_PENDING**. Tasks B–E were open until licensed integration; the record below supersedes that delivery status without rewriting the source checkpoint.

## Licensed integration resume — 2026-10-25

Resumed from dcc0297 on the owner's license-activated instruction. The source
checkpoint's 43 prepared images and four atlases are retained without regeneration.
The prior source-only status above is historical, not the current integration state.

Ruling: retain pre-existing RC1 resume drift rather than overwrite owner state or
silently replace the frozen baseline — the original layout file's bytes and the
unchanged font file's mtime already differ before integration; branch, HEAD, index,
status and all 74 Canon/Architecture hashes still match — cost: original strict
preservation remains a disclosed historical mismatch; the separate resume snapshot
must prove this task introduced no further changes.

Unity import succeeded. Before applying art, the four new tests produced 1 PASS /
3 expected missing-art failures; unsaved-scene protection already passed.
`art-scene-upgrade-01` applied the bounded existing-scene upgrader. Both affected
integration runs (`art-integration-green-01` and `-02`) pass 29/29, including
character contacts, actual fills/layers, four-method input, disclosure/readability,
focus and retained Slice behavior. No broad unrelated QA was rerun. The prior
149/149 standalone result remains baseline evidence, not a new execution claim.

The scene contains 82 added serialization blocks, no removed original block, and
no modified original Transform, Camera or BoxCollider. Existing changed blocks
are presentation renderers/meshes/controller references plus scene-root wiring.
65 serialized scene/art files resolve 153 referenced GUIDs, including actual URP
package assets; local scene references resolve. See art-integration-checks.json.
Both initial Player builds succeed with zero build errors/warnings.

First actual 720p recording `gate2-player-art-open-720-01` is retained FAIL, exit 2.
Service 0 was recognized with Good preparation but weak Stir quality (stable
fraction 0.7109201). Capture stalls expose the QA coroutine's previous-frame
pointer interval and .04-second cap, independently of unchanged production Stir
rules. The process had already exited when an executor stop was attempted; no
stop signal was delivered. Source/Player inventory 1193 remained unchanged.

Ruling: synchronize the explicitly enabled proof fixture's pointer with the current
dynamic InputSystem update — actual capture stalls must not fabricate erratic
simulated hand speed — cost: automated-input evidence remains a distinct tier and
cannot certify physical-device feel. No gameplay adapter, technique threshold,
composition, quantity or save authority changed. The same real Player assertion
is the failing regression; current rerun first two services pass with stable
fraction 1.0. Full-run evidence remains pending at this ledger entry.

Ruling: hide legacy square snow/cloth/tray proxy renderers and narrow the generated
ground palette into directional snow bands — actual menu/workbench captures show
blockout duplicates and dark enclosed pools — cost: these within-C1 material
refinements still require owner taste judgment. Meshes/support bounds are retained;
none of the completed ImageGen/source assets is regenerated. Second actual menu
capture confirms the duplicate blocks/pads and dark pools are removed.

Second recorded preflight `gate2-player-art-open-720-02` completed nine services
with correct recognition/preparation/quality, including Manwol, before an explicit
executor stop for visual corrections (process exit -1). It remains incomplete.
The first failure and this stopped run are never merged into final-run evidence.

Ruling: shift only the opaque ACT 1 selected-bottle display left of the bartender's
face and give the Float spoon a one-pixel metal handle support — actual 720p
dialogue/Float frames show mouth occlusion and subpixel disappearance — cost:
these minor tool-display placements still need owner visual judgment. The source
PNGs, camera, tool input coordinates, actual flow and HG04 contracts are unchanged.
The final affected suite is 29/29 PASS (`art-integration-final-green`), and the
final Player build reports zero errors/warnings (`art-player-final-build`). Actual
final 720p reaction and active Float frames confirm the face/handle are visible.

Final 720p recorded Player (`gate2-player-art-open-720-final`): PASS, 167 checks,
54 captures, 17 services/departures, six learned recipes and earned glowing memory;
exit 0, 1193 source/Player files unchanged. Runtime images confirm readable face,
Float handle/layers, requests, reactions and retained chair/star. Media contains
3050 actual frames over 636.3106 seconds and 636.3307 seconds of stereo 48 kHz Unity
mix, peak PCM16 2716, zero clipped samples. The 0.0201-second duration difference
does not establish sample-synchronized foot contact; owner listening remains open.

Serialized diff inspection: 232 existing renderer blocks, three character mesh
filters, four presentation/controller components and the root list changed; 82
new dressing blocks. No original object deletion or spatial block change. All
component changes are art/pose/footstep references. No prefab or setting changed.
`git diff --check` is clean for code, scene and metadata. Unity's final build
rewrote three new standing-character materials with six spaces after empty YAML
fields (`m_Name` / `m_LockedProperties`); the full check reports those six lines.
Ruling: retain that valid Unity-generated material serialization byte-for-byte —
keeps the actual final build/evidence inventory exact without a formatting-only
Player rerun — cost: the six non-code whitespace findings remain disclosed; no
validator is disabled and no gameplay/art defect is hidden by this disposition.

Final 1080p Player (`gate2-player-art-open-1080-final`): PASS, 167 checks,
54 captures, 17 services/departures, six learned recipes and earned memory;
exit 0, 1193 source/Player files unchanged. Actual Prince introduction, Float,
Shake/ordinary guest, farewell/memory and large settings images inspected.
Both resolutions preserve the same C1 spatial blocks and visible exterior.

Completed Continue: `gate2-player-art-open-continue-final-02` and
`gate2-player-art-quiet-continue-final`, each PASS, five checks/ten captures,
exit 0, exact current inventory. Both current/previous generations of both earned
historical saves remain byte-identical: 17 services, 17 departures, one correct
glowing/unlit memory. No full quiet campaign replay was needed for this art pass.
The first sandboxed Continue (`gate2-player-art-open-continue-final`) failed before
checks with File.Replace access denial in the QA observation writer. All actual
save hashes were unchanged; the same Player passed using the desktop execution
context of the full runs. Its environmental failure is retained separately.

The generated `review-act1-art.html` includes same-C1 before/after, actual images
at both sizes, active Float frame, full video and six focused clips, raw evidence,
asset provenance and the Player link. `art-gate2-review.json` reports
READY_FOR_OWNER_ART_REVIEW_WITH_NATIVE_INPUT_DEBT; every package requirement is
satisfied. This completes the bounded integration/review-preparation work, not
owner aesthetic acceptance or final native technical closure. PD01–PD11 stay OPEN.
Stop here for the requested bundled owner review; ACTs 2–7 are not started.
