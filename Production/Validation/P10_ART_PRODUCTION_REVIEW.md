# P10 art production candidate review

Recorded 2026-11-11 against `e558504` plus the preserved candidate worktree.
This records implemented bindings, source traceability and technical evidence.
It does not grant owner art approval or declare Gate 3 ready. The retained
Build03 Player evidence predates the R02 anchor correction; Full Editor04,
Build04 and corrected 720p/1080p memory captures remain pending at this record.
Dedicated performance validation is also pending.

The approved direction remains [Q1](Q1_OWNER_CONFIRMATION.md), the accepted
Prince pose at `a19a84a`, and [Gate 2](GATE2_OWNER_APPROVAL.md). The scope comes
from the [P10 production plan](P10_ART_PRODUCTION_PLAN.md), its
[source inventory](P10_ART_SOURCE_INVENTORY.json), and
[production expansion plan](PRODUCTION_EXPANSION_PLAN.md). Story First,
accumulated memories and restrained presentation remain governed by
[the project vision](../../Docs/00_PROJECT_VISION.md).

## Implemented candidate and preservation

[CampaignFinalArtBuilder](../Unity/Assets/Surlhwa/Editor/CampaignFinalArtBuilder.cs)
creates and prepares the separate
[Bar_Q1_Final scene](../Unity/Assets/Surlhwa/Scenes/Bar_Q1_Final.unity), derived
from accepted ACT7. Its optional
[FinalArt bindings](../Unity/Assets/Surlhwa/FinalArt/FinalArt.asset) extend the
existing ACT1 art set. The builder refuses dirty open scenes, verifies accepted
scene bytes after saving, and checks scene transform matrices during preparation.
The [before](Manifests/p10-candidate-before.json) and
[after](Manifests/p10-candidate-after.json) renderer inventories are retained.

A read-only serialized comparison of ACT7 and Final, repeated for this review,
found **2,013 objects in each scene, zero added or removed objects, and zero
changed Transform/RectTransform blocks**. Whitespace-normalized differences
are limited to **83 MeshRenderer blocks and one controller block**:

- 117 snow-cap material slots bind the new `FinalArt/QuietSnowCap.mat`; the
  existing ground's one material slot binds `FinalArt/QuietSnow.mat`.
- Controller presentation references change for `Art`, `Font`, `Foot` and
  `Music`, with the new `Shake` reference. Font and audio finishing belong to
  their scheduled owners; they are disclosed rather than counted as geometry.
- Camera, geometry, supports, stools, cast references, content and campaign
  history references retain their accepted serialized values.

The current accepted ACT1 through ACT7 scene files and
`Production/Content/Campaign` have no tracked diff against HEAD. This narrow
check does not reclassify inherited material/ProjectSettings worktree entries
as P10 changes. Shared runtime presentation code has intentional P09-P12 edits;
unchanged scene bytes alone are not a claim that all runtime behavior is
unchanged.

The Q1 world/focus rotations, orthographic endpoints and **1.12 character
scale** remain in [Q1ProductionView](../Unity/Assets/Surlhwa/Presentation/Q1ProductionView.cs).
The Prince's approved seated image remains byte-identical:
`Q1Polish/PrinceSideQuarter.png`, SHA-256
`8cb8383a8f28bd90f017575bb23551a2860e535c25bfaa5a84a60013bcf2e60c`.
Its stool, seated contact formula and cutout remain protected. P11 motion
composition is separately covered by
[CampaignMotionTests](../Unity/Assets/Surlhwa/Tests/EditorIntegration/CampaignMotionTests.cs);
P10 does not authorize a new pose or room direction.

| Family | Current disposition and concrete change | Remaining judgment |
|---|---|---|
| Room, warm wood, U-counter, stools, shelving, lamps, pines and exterior space | Retained. No new geometry or decorative density. The visible winter exterior and selective depth remain. | Final palette, oblique pixel stability and room coherence. |
| Snow | New source-recorded ground texture; cap materials use a restrained flat color instead of their previous fine texture. Existing surfaces and footprints remain. | Snow value, cluster scale and balance with the warm room. |
| Bartender, ordinary guests, Prince and ACT2-7 cast | Retained source images and physical contacts. P11 supplies bounded motion using the existing family. | Whole-cast consistency and animation feel; accepted Prince pose is not reopened. |
| Glass and garnish | Adds source-traced tall glass, flute, orange slice and zest to the existing presentation owner. Legacy art retains its two manual glass profiles; Final offers four. | Shell/contents readability at actual play size. |
| Shaker | New illustrated body/lid sheet with a transparent cutaway for actual contents. Source rectangles are recorded in the manifest. | Material and hand/tool coherence in motion. |
| MEM-01 | Retained small star, chair and existing variant behavior. | Overall accumulated-room reading. |
| MEM-02 through MEM-07 | Code-authored upper/side material bands within existing silhouettes; original receipts, supports, placement and activation retained. No new memory bitmap, light, collider or future completed form. R02 corrects one skipped anchor treatment. | Quiet material detail and relative visibility of all seven memories. |

The plan's optional memory surface images were resolved with small existing-mesh
edge bands in [CampaignMemoryView](../Unity/Assets/Surlhwa/Presentation/CampaignMemoryView.cs).
This supplies the bounded surface treatment without new bitmap dependencies or
an additional presentation system.

## Source trail and rights limits

All **six** final PNGs were rehashed during this review: each destination
matches its recorded source bytes and the declared SHA-256 in
[FinalProduction/assets.json](../SourceArt/FinalProduction/assets.json).

| PNG | Recorded source/method | Verified SHA-256 |
|---|---|---|
| OrangeSlice | RC1 original deterministic garnish pixels | `e4a6b89c4df00f47581287d882a6d3698e0192df5ff260b60a9fcd706aefab86` |
| OrangeZest | RC1 original deterministic garnish pixels | `62f88645c7cf4db5bc7e348ed8ba6d9d7dee868629bef8285fee52ca9ba736f7` |
| TallGlass | RC1 original deterministic glass geometry | `101d6e7013c8665ab3a82b2e297ea61a49edde14a9ab125b23c1917c482d1bc4` |
| Flute | RC1 original deterministic glass geometry | `efc48693abb2184089af1caf64769e74098ff2aa6f06f34421de5455a09ed6c7` |
| ShakerSheet | Built-in imagegen; exact cutaway output, Unity UV/import only | `cb57c2c6f691a5c73b4226b3477bd4643340a089524a53bc7627b2a629e2675b` |
| QuietSnow | Built-in imagegen; exact output, Unity UV/import only | `7177e62a07034b45b9d0056ce817050a27e27163bac89e127f4a6238897a46e4` |

The original [snow prompt](../SourceArt/FinalProduction/QuietSnow-prompt.txt)
and [source](../SourceArt/FinalProduction/QuietSnow-source.png), and
[shaker cutaway prompt](../SourceArt/FinalProduction/Shaker-cutaway-prompt.txt)
and [source](../SourceArt/FinalProduction/Shaker-cutaway-source.png), remain
available. The [earlier shaker output](../SourceArt/FinalProduction/Shaker-source.png)
and its [prompt](../SourceArt/FinalProduction/Shaker-prompt.txt) are retained as
history: its lower body hid partial liquid and it was not selected. The selected
sheet uses top-left rectangles body `(204,318,362,502)` and lid
`(885,474,362,222)`.

New texture importers use point sampling, no mipmaps, uncompressed textures,
no NPOT rescaling and alpha transparency. The read-only GUID audit found no
duplicate GUID involving FinalArt and all FinalArt asset references resolved.
New snow materials and owned final font assets avoid edits to accepted shared
material/font assets. Import settings and resolved references are technical
prerequisites, not visual approval.

Retained sources keep their existing caveats:

- [ACT1 production provenance](../SourceArt/Act1Production/assets.json) records
  built-in ImageGen outputs followed by declared crop, nearest normalization,
  palette and alpha preparation. Its candidate/human-review wording remains.
  The preparation inventory's 43 matching runtime PNGs is a historical
  preparation check, not a new rights determination.
- [ACT2](ACT2_CAST_PROVENANCE.json), [ACT3](ACT3_CAST_PROVENANCE.json),
  [ACT4](ACT4_CAST_PROVENANCE.json), [ACT5](ACT5_CAST_PROVENANCE.json),
  [ACT6](ACT6_CAST_PROVENANCE.json) and [ACT7](ACT7_CAST_PROVENANCE.json) retain
  original cast lineage and bounded owner-authorized reuse. The preparation
  inventory records 42 matching cast PNGs. Reuse authority does not imply
  independent legal clearance or final aesthetic approval.
- [The older migration ledger](../SourceArt/PROVENANCE.json) still identifies
  reference assets and pending release adoption/rights review. The original
  RC1 `D:/univ/surlhwa/Surlhwa/Docs/CREDITS.md` still states that pre-existing
  artwork origins/licenses were not comprehensively recorded and that final
  public-release attribution/permission and Prince interpretation/credits need
  owner review. Later Q1/Gate 2 acceptance does not erase those statements.
- The new shaker does not adopt the old unresolved shaker image. The four
  copied glass/garnish assets retain their original pixel-source lineage;
  their exact external source paths are in the final manifest. No external web
  artwork or commercial license is asserted by this record.
- Noto's existing [OFL notice](../Unity/Assets/Surlhwa/SliceAssets/Noto-OFL.txt)
  remains bundled. Font and audio source notices remain with their owners.

`SOURCE_TRACKED`, byte equality and bounded reuse authorization are distinct
from public-distribution clearance. This review makes no legal-clearance claim.

## Actual preparation truth and connected tests

[SliceWorkbenchView](../Unity/Assets/Surlhwa/Presentation/SliceWorkbenchView.cs)
reads actual `BrewingSession` containers or the committed service snapshot for
liquid amounts, layering, ice and garnish. The shaker lid follows actual closed
state. Transparent shell interiors expose that geometry; they are not painted
recipe results. Clearing the preparation removes its contents and garnish.

The player's existing glass command changes presentation only. It does not
alter brewing revision, recipe recognition, FIT, knowledge, supply or recipe
glassware acceptance. No requested recipe automatically chooses a vessel in
production. The proof driver's recipe-specific key presses are disclosed as
simulated test input, not player gameplay authority.

[P10WorkbenchTests](../Unity/Assets/Surlhwa/Tests/EditorIntegration/P10WorkbenchTests.cs)
covers legacy two-profile behavior, four manual profiles at 30/90/180 ml without
preparation mutation, actual shaker/lid/garnish state and clearing, and cherry
placement inside the flute. The added R02 cases exercise a real seven-memory
view with refinement both disabled and enabled.
[P11ToolMotionTests](../Unity/Assets/Surlhwa/Tests/EditorIntegration/P11ToolMotionTests.cs)
and the existing ACT presentation suites supply connected contact/pose/depth
coverage. Tests establish bounded contracts; stills and human play review must
establish how the resulting art reads.

## Retained actual Player evidence

[Build03 manifest](Evidence/final-build-03-manifest.json) records Unity
6000.3.7f1, `Succeeded`, 193,274,993 bytes, 29.4628528 seconds and zero errors or
warnings. The corresponding
[full 720p run](Evidence/final-campaign-media-720-02/player-result.json) records
**PASS, 373 captures, 2,652 checks, zero errors, 47 services, 47 departures,
nine Known recipes and seven memories**. Its
[process record](Evidence/final-campaign-media-720-02/process.json) records
natural exit 0 and no timeout.

The [launch identity](Evidence/final-campaign-media-720-02/expected-run.json)
records 1280x720, fresh `Welcome`, `Bar_Q1_Final`, the executable hash and
simulated production InputSystem. This media run used batch mode with real
D3D11 camera/UI rendering. It is neither native OS-input evidence nor an
uncaptured foreground performance benchmark.

The following files exist and were inspected with `view_image` during this
review or the retained preparation review. These are actual Player materials,
not diagnostic recolors. Before/current comparisons also include P09/P11
presentation changes; the serialized audit above isolates scene preservation.

| Review subject | Before / retained accepted presentation | Build03 candidate, before R02 |
|---|---|---|
| Seven-memory coda, room and snow | [ACT7 720p coda](Evidence/act7-player-open-720-01/045-earned-memory.png) | [Final 720p coda](Evidence/final-campaign-media-720-02/315-earned-memory.png) |
| French 75 serving profile and shaker family | [ACT4 prepared view](Evidence/act4-player-open-720-01/010-prepared-29-shake.png) | [Final prepared view](Evidence/final-campaign-media-720-02/161-prepared-29-shake.png) |
| Prince contact, actual Float layers and tool presentation | Protected pose/source and earlier accepted ACT1 evidence remain authoritative. | [Final prepared Manwol](Evidence/final-campaign-media-720-02/027-prepared-5-float.png) |

The coda comparison shows the preserved room and changed snow treatment, with
small separate memory pieces and no guest actors. The French 75 still shows
the manually selected flute containing the actual prepared serving liquid;
the shaker is empty of liquid with retained ice. The Manwol still shows actual
layers and the accepted seated Prince. These observations do not certify
motion smoothness, exact foot/audio timing, native usability or final art feel.

## R02 correction and remaining verification

The source review found one objective finishing defect:
`Contains("red")` also matched **Small weathered anchor**, so MEM07's anchor
missed its two edge bands while the incomplete hull received them. This was
not a receipt, persistence, placement or art-direction change.

The retained [combined RED XML](Evidence/final-review-regressions-red-01.xml)
and [log](Evidence/final-review-regressions-red-01.log) show **four cases: one
passed, three failed**. The disabled-refinement case passed; enabled refinement
failed on the missing anchor band. The other two failures belong to the
separate legacy-ending cleanup regression and are not attributed to R02.

After that observed RED, the guard was changed in one line to exclude the
exact `Tiny contained red point` name. Shadow, water and ripple exclusions,
geometry and all receipt logic remain unchanged. The regression requires both
nonempty rendered anchor/hull bands, keeps the exact red point/water/shadow/
ripples unrefined, and checks that disabling refinement produces no edge bands.
The source diff/whitespace check passed. No runtime GREEN is inferred from it.

Root-owned follow-up remains:

1. Record Full Editor04 and Build04 outcomes against the corrected source.
2. Inspect new 720p and 1080p seven-memory actual-material captures; retain the
   Build03 images above as pre-correction evidence.
3. Attach final candidate inventories and practical performance results under
   [the dedicated measurement method](FINAL_PERFORMANCE_METHOD.md). No
   benchmark or minimum-hardware claim follows from the media run.
4. Present the exact candidate for the bundled Gate 3 judgment. All PD01-PD11
   remain open for human sign-off in [PRESENTATION_DEBT.md](PRESENTATION_DEBT.md).

This document adds a reviewable P10 implementation/evidence record alongside
the [evolving Gate 3 draft](GATE3_RC_REVIEW_DRAFT.md). It leaves historical
packages and failed/RED evidence intact. No Unity/Player, image edit, source
change or asset import was performed to write this review.
