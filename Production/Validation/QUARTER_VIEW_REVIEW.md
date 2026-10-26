# SURLHWA / Quarter View visual direction spike

## Verdict

**QUARTER_VIEW_READY_FOR_OWNER_COMPARISON** — a separate, disposable presentation
experiment. This is neither owner approval nor replacement of C1. ACT 2 has not
begun. Open [the comparison](review-quarter-view.html) and
[the concise board](QUARTER_VIEW_COMPARISON.png).

## Quarter View Interpretation

One fixed authored view of the same small bar, with readable counter tops, returns,
stools, floor and back wall, surrounded by quiet snow. The tested direction uses
pixel textures and sprites first; geometry supplies their spatial relationships.
There is no isometric grid, free camera, overworld, new content or PBR conversion.

## Camera Candidate(s)

| View | Projection | Position XYZ | Pitch / yaw | Size / FOV | Intent |
| --- | --- | --- | --- | --- | --- |
| C1 | Orthographic | 0, 10.089, -17.209 | 28° / 0° | 3.70 | Current accepted framing with current ACT 1 art |
| Q1 | Orthographic | 11.098, 18.970, -20.223 | 38° / -28° | 4.85 | Stable sprite scale and legible U-counter |
| Q2 | Restrained perspective | 14.613, 24.819, -26.833 | 38° / -28° | 14° vertical FOV | Same authored angle, subtle convergence/depth |

The Q target is (0, .50, .65). Wide plates use size 5.75 or equivalent camera
distance; close-ups are inspection views, not new gameplay cameras. Exact values,
camera-space bounds and capture states are in `Manifests/quarter-view-package.json`.
The recorded character bounds are projected world AABBs, including transparent
canvas; they are diagnostics, not silhouette-area measurements.

For the retained 76-pixel-high seated silhouette at 70 PPU, Q1 is approximately
81 display pixels high at 720p / 121 at 1080p, versus C1's 106 / 159. Q2 varies
slightly with depth. The bar floor's projected bounding rectangle is about 66%
of frame width in Q, versus 73% in C1; this is not an exterior-pixel percentage.
Q1 is the cleaner comparison starting point. Q2 remains a deliberately restrained
projection alternative, not a mandate to add perspective.

## Environment Changes

The sibling `Bar_QuarterView.unity` reuses the actual `Bar_Act1.unity` scene and
references. Q adds shallow drift edges, a footprint approach, a distant fence and
four shelf lips. Existing counter panel art covers newly exposed return faces.
The existing floor/bar footprint, stools, shelves, lamps, bottle/glass dressing,
cloth, notebook, ceramics, empty memory niche and gameplay colliders remain.
No existing scene block was deleted or edited except the sibling's added root list.

## Exterior Snow Composition

The Q-only ground extends to 42×42 world units so restrained perspective and wide
inspection never reveal the edge seen in the first preview. Quiet five-band pixel
values replace the strong stripes revealed by rotating C1's ground. Sparse existing
pine/rock/bank art and snowfall remain. Warm ground accents and lamps stay local.
This does not enlarge the bar or add exterior gameplay. No realtime lights,
shadow casters, glossy materials or screen-wide pixel filter were added.

## Character Integration

All three accepted character bindings, feet roots, seat anchors and movement
paths are retained. Character planes receive the authored camera orientation;
seated pivots are compensated against the existing contact anchor. Furniture and
alpha-cutout sprites use real depth tests/writes, not sorting-order overrides.
The rear coat uses a documented 0.14-unit camera-axis surface offset to avoid the
stool cutting across its waist. This is local presentation thickness; actor roots,
seat ownership, progression and save state do not move.

The close-ups show the bartender's lower body behind the counter, guest in front
of the service edge, cloth/ceramics clear of faces, and the corrected coat/stool
overlap. Final seated anatomy, footrest reading and scale still require owner
judgment. No objective test can approve the feeling of belonging in the room.

## Directional Sprite Findings

The accepted mostly-front poses remain readable but face the viewer instead of
the bartender in Q. `ExistingSprites` plates preserve that alternative. Two static
back-quarter seated study poses make the facing relationship clearer. The
bartender and all standing/walking frames are retained. There is no four/eight-way
set, new character identity, new animation FSM or animation-owned progression.

The study exposes a real adoption cost: front-quarter walking switches visibly
to the back-quarter seated pose. A later approved direction may need a bounded
turn/sit/stand transition and a returning glance for reactions; those have not
been authored. Prince departure is recorded from an earned checkpoint; ordinary
arrival covers the existing entrance path. This is not a claim that every Prince
entrance or every future guest has a finished directional set.

Two new raster poses and their original atlases are **SPIKE_ONLY_UNAPPROVED**.
See [provenance and exact prompts](../SourceArt/QuarterView/PROVENANCE.md) and
`../SourceArt/QuarterView/import.json`. Accepted assets were not regenerated.

## C1 vs Quarter View

The package includes empty bar, ordinary seated, Prince seated, bartender + guest,
wide exterior, grounding, original-sprite alternatives, actual UI context, and
movement replay at 1280×720 and 1920×1080. Native PNGs remain unchanged; the board
is a labeled nearest-neighbor overview. Movement videos retain actual sampled
controller poses and their timing. Technical service commands reach the original
reaction/departure, without manufacturing narrative outcomes or replacing content.

World plates hide UI and the resident workbench equally in C1/Q to expose the
composition. `ContextUI` plates retain both. Historical C1 and ACT 1 art review
pages, originals and builds remain available. Neither the historical Quixote C1
fixture nor generated concept imagery is substituted for this ACT 1 comparison.

The Q ordinary clips include the rear-pose seating/reaction study. The Prince
clips retain the original pose as a movement control; his rear-pose comparison
is static. Prince Q `ContextUI` is a forced camera diagnostic with original
seated art, bypassing the normal focus switch to expose the existing tool-view
mismatch. It is not the normal active-bartending mode, which returns to C1.

## Pixel-Art Coherence

Actual Q frames retain point-sampled sprite/texture clusters, alpha cutouts,
restrained palette bands and existing authored prop density. The newly visible
side faces use the same counter artwork. Source artwork varies in density and
oblique textured surfaces still alias at subpixel angles; full pixel-grid
uniformity at both resolutions is not claimed. No normal 3D render is being
disguised with a low-resolution post-process. Artistic coherence remains an owner
comparison question.

## Performance Comparison

The review page contains all 12 measured rows: two actors × three views × two
resolutions. Each has 60 warmup frames then 10 seconds of uncapped Development
Player offscreen URP rendering, with UI/workbench hidden. This shortened spike
sample is intentionally below the architecture's full 60-second production
profiling recommendation. No unrelated campaign/performance suite was run.

The measured renderer/material counts are 148/35 for C1 and 208/39 for Q.
Across the final rows, frame P50 is 0.434–0.452 ms for C1 and 0.478–0.491 ms for Q;
P95 is 0.528–0.557 ms versus 0.582–0.593 ms. Draws/batches are approximately
364–366 versus 422–423. Unity allocated memory is 67.58–67.63 MiB and reserved
memory about 211 MiB. Recorded GC means are 10.6–11.9 bytes/frame including the
harness growth described below. These are local offscreen Player measurements.
All candidates load in the same Player: resident memory includes inactive Q assets
even for C1, so the memory column is not an incremental asset-memory estimate.
Reported GC includes the measurement harness's sample-list growth (80,032/160,032
byte peaks); it does not establish game-only allocation or a zero-GC claim.
Unity's process working-set counter returned zero here and is unavailable; Unity
allocated/reserved memory is retained instead. GPU frame time is not measured.
All candidates have zero realtime lights/shadow lights. No fatal cost was observed;
no optimization campaign or minimum-machine claim follows from this sample.

## Targeted Validation

- Final Unity Player build: succeeded; zero build errors/warnings.
- Targeted Unity Editor suite: 7/7 PASS (Q camera/contact/C1 isolation, existing
  input/Float boundaries, original ACT 1 character contact).
- Final standalone source compilation: see retained `quarter-dotnet-build.log`;
  existing `CampaignPlayerProof.audio` CS0108 warning remains inherited.
- Final actual ordinary and Prince runs: no runtime errors; scene/bindings load,
  camera comparison preserves the encoded checkpoint, active brewing restores C1
  projection, and technical service uses unchanged recognition/preparation.
- Each run verifies its 1,224-file source/Player inventory before/after. Both
  resolutions, complete media, scene references and hashes are checked by the
  package/serialization/preservation scripts.
- Serialized audit: 1,581 original blocks retained, 250 spike blocks added,
  only the sibling scene root list extended; 112 referenced GUIDs resolve.
- `git diff --check` reports 72 spaces after valid empty Unity YAML fields in
  the new scene/material/meta files, including inherited scene fields. Exact
  tested serialization is retained. No code/prose whitespace issue or unrelated
  scene/prefab/ProjectSettings change is included; the findings are recorded in
  `Manifests/quarter-view-diff-check.json`, not suppressed or claimed clean.

The initial UPM IPC sandbox failure and subsequent missing-license exit 198 remain
in the evidence. Owner license refresh cleared the blocker. Initial preview snow
edge/contrast and coat overlap evidence is retained separately, not relabeled final.
Two accepted seated materials briefly received incidental URP serialization during
the first Editor test; exact original bytes were restored, the test uses temporary
materials, and the final build/captures were repeated from that restored baseline.

## Preserved Gameplay / Canon

Final Canon, approved Architecture, content JSON, Domain/Application/ContentIO,
actual liquid/container state, recognition/FIT/knowledge/supply, Stir/Float,
Open/Quiet logic, save/Continue and ACT 1 text are unchanged. C1/ACT 1 scenes,
accepted art and historical evidence are byte-preserved. The owner-local
`review-slice.html` edit is excluded. No push or ACT 2 work is included.

Frozen RC1 HEAD/branch/status/index and Canon/Architecture hashes are preserved.
The initial strict RC1 filesystem snapshot is **not identical**: during the owner
license refresh, its local Editor layout changed and the unchanged KoreanFallback
asset received a new timestamp. Both snapshots are retained. No agent operation
edited/restored those files or launched RC1; attribution beyond that observation
is not claimed. The post-refresh RC1 snapshot is the stability check for the rest
of this spike.

## Known Costs

Q is provisional. Rear-pose proportions, feet/seat impression, front-to-back turns,
reaction expressiveness, oblique pixel shimmer, context-UI overlap, the hard cut
to existing focused bartending, snow detail and overall warmth require owner
review. The spike adds about 60 active renderers but no new lights or gameplay
systems. New direction art is not approved Production art. Native desktop input,
audio and 60 fps animation certification are outside this visual capture tier.

## Review Package

- [Comparison page](review-quarter-view.html), [board](QUARTER_VIEW_COMPARISON.png).
- `Evidence/quarter-final-ordinary-02/` and `Evidence/quarter-final-prince-02/`:
  native stills, frame originals, motion clips, actual state/perf records, logs and
  exact inventories. Raw media/builds are local ignored evidence, as established.
- `Manifests/quarter-view-package.json`, `quarter-view-validation.json`,
  `quarter-view-serialized.json`, `quarter-view-build.json` and preservation snapshots.
- `Builds/QuarterView/SurlhwaQuarterView.exe`; use the isolated-save launcher below.

```powershell
powershell -NoProfile -File Production/Tools/run_quarter_view.ps1 -EvidenceName quarter-owner-01 -Interactive
```

The review-only C1/Q1/Q2 controls select fixed variants. Active bartending returns
to the existing C1 view. Without an explicit isolated save argument, the Q Player
leaves the campaign controller disabled. C1 remains the accepted baseline until
the owner decides otherwise.
