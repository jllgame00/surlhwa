# P10 art production: bounded final candidate

Prepared 2026-11-11 against `ac3aaf3` and the current preserved worktree. This is
a read-only preparation record. No Unity/Player was launched and no asset,
scene, C#, content, bitmap or existing inventory was changed for this plan.
The new [source inventory](P10_ART_SOURCE_INVENTORY.json) records the files
actually inspected and their hashes. P10 implementation and final art approval
are **not** claimed.

## Decision and authority

Produce a separate final candidate with three visible improvements: quieter,
more coherent snow surfaces; illustrated shaker/garnish presentation and missing
glass profiles; and restrained authored surfaces for the six later memories.
Retain the accepted room, warm wood, cast identities and contacts, and MEM-01.
Review every selected family in that candidate, including retained assets.
This is a finishing pass with new reviewable work, not a change of labels from
reference to final.

Authority is `Docs/00_PROJECT_VISION.md`, `Docs/01_GAME_DESIGN.md`,
`Docs/06_REBUILD_RULES.md`, [P10/P11 expansion scope](PRODUCTION_EXPANSION_PLAN.md),
[presentation debt](PRESENTATION_DEBT.md), [Q1 owner ruling](Q1_OWNER_CONFIRMATION.md),
[Gate 2 approval](GATE2_OWNER_APPROVAL.md), and each ACT's cast/content provenance.
Final Canon memory definitions remain the content authority. Earlier C1/spike
and pending-review wording is historical where later Q1/Gate 2 rulings supersede
it; those rulings do not make every source asset release-approved.

Keep fixed soft orthographic Q1 framing, visible winter exterior and negative
space, warm U-counter, bartender inside/guests outside, selective depth, point
sampling and 1.12 character scale. Preserve the owner-accepted `a19a84a` Prince
seated pose, its stool, footprint and occlusion exactly. No room fork, PBR,
strong perspective, new decorative density, future memory upgrades, new light
or interaction on memories, recipe authority, or save format change.

## What the actual evidence establishes

The nine normal-material Player images listed with hashes in the
inventory were opened with `view_image`: ACT2 paired farewell; ACT4 prepared
French 75; ACT5 Faust introduction; ACT6 Sim Cheong introduction, prepared Last
Word, and complete-room memory views at 1280x720 and 1920x1080; and the newly
available root-run ACT7 720p final farewell and earned-memory coda. The ACT1 prepared
asset board, current seated PNGs for all seven ACT2-7 characters, current Q1
Prince texture, and source flute/tall glass/orange/zest were also inspected.
The board is source inspection, not a rendered-game substitute.

| Observation | Classification | Consequence |
|---|---|---|
| Warm textured wood, lamps, shelves and sparse winter silhouettes already read together in the actual room. | Retain; minor finishing review | Do not regenerate the interior or add more props. Compare the snow sample beside these retained anchors. |
| Snow ground has broad low-detail bands; the wall caps have much finer bright texture than adjacent snow. | Visible finishing opportunity, not a measured rendering fault | Refine the snow cluster scale and contrast within current surfaces. No increased geography or perimeter clutter. |
| MEM-02/03/04/05/06 read as flat rectilinear shapes against the richer room. | Visible material-family gap | Author compact edge/material detail within existing silhouettes and supports. Preserve quiet scale and negative space. |
| Extended shaker is a line outline with a flat lid; orange/zest use flat quads. | Confirmed presentation coverage gap | Add source-recorded shells/garnish images, keeping actual contents visible. |
| `SliceWorkbenchView` exposes only rocks/cocktail glass through `CocktailGlass`; current recipe presentation includes tumbler and flute. | Confirmed glass representation gap | Add existing source-traced glass profiles to the current manual glass presentation. Do not auto-select the hidden requested recipe. |
| Prepared French 75/Last Word images show the current regular glass. | Evidence of the selected visual profile only | This does not demonstrate an incorrect liquid result or recipe-matching failure. Glass presentation and recipe recognition stay separate. |
| ACT6 earned lotus is visible in normal 720p/1080p captures. | Historical occlusion defect corrected | Preserve the corrected counter placement. Do not describe the earlier behind-glass failure as current. |
| Reviewed paired/Faust/Sim Cheong stills do not establish a new foot or seat penetration. | No new objective contact defect established | Keep current contacts; verify transitions in P11 rather than inventing a new pose problem. |
| ACT7 720p Odysseus farewell has a coherent seated silhouette; final coda shows the small separate anchor and incomplete hull with guests absent. | Current normal-material still evidence | Keep their physical anchors. This is not motion, 1080p or final art certification. |

Pixel shimmer, hand contact over motion, timing and tactile response cannot be
certified from these stills. They require P11 actual Player clips. ACT7 1080p,
focus transitions and its complete runtime proof remain root-owned validation;
this plan's two 720p stills do not replace those results. A source PNG and a GPU
diagnostic do not substitute for actual Player views. No current objective contact/visibility defect is being hidden
as minor polish here.

## Source and disposition by family

`SOURCE_TRACKED` below means the recorded creation/import lineage is available.
`OWNER_AUTHORIZED_REUSE` is the existing bounded source reuse authority, not an
independent assertion of public-distribution rights. `RETAIN` is a production
decision; it is not a statement that subjective final art approval has occurred.

| Family / current owner | Disposition | Source status and bounded action |
|---|---|---|
| Q1 geometry, counter, stools, shelf supports, camera | RETAIN EXACT | Project-authored builders/scenes. Freeze transforms, contacts and camera. Do not rerun historical builders over accepted scenes. |
| Warm wood, shelves, lamps, cloth, ceramics, notebook, niche, pines/rocks/banks | RETAIN, inspect together with new snow | `SourceArt/Act1Production/{Environment,Materials,Shelves-generated}.png`, exact prompts and `assets.json`; original built-in ImageGen lineage. Current 43 runtime PNG hashes match the declared manifest. No blanket replacement or extra clutter. |
| Q1 ground and snow cap/ridge surfaces | REWORK in new candidate bindings | Ground is actually `QuarterView/QuietSnow.png` from `QuarterViewBuilder.Snow`, not merely old `ProductionArt/SnowGround.png`. Caps descend from `PixelCutawayBuilder`; trace each active renderer before rebinding. New source-recorded low-contrast cluster art on the existing surfaces; no changes to footprint meshes or pines. |
| Bartender and ordinary guests | RETAIN; full family contact review | ACT1 Characters atlas/prompt plus adopted ordinary back-quarter lineage in `SourceArt/QuarterView/`. Preserve anonymous ordinary silhouette and current idle references. Directional/transition frames belong to P11; do not replace the character family to obtain a new screenshot. |
| Prince | RETAIN EXACT | Current `Q1Polish/PrinceSideQuarter.png` hash `8cb8383a8f28bd90f017575bb23551a2860e535c25bfaa5a84a60013bcf2e60c`; `SourceArt/PrinceSeatedCorrection/import.json` and later owner ruling. Preserve standing/walk lineage and exact accepted seated texture, geometry and stool. |
| Chunhyang, Mongryong, Quixote, Hamlet, Faust, Sim Cheong, Odysseus | RETAIN source PNGs, pivots, scale and contacts | `ACT2_CAST_PROVENANCE.json` through `ACT7_CAST_PROVENANCE.json`: 42/42 destination and original-source hashes match. OWNER_AUTHORIZED_REUSE for the existing bounded cast. Pair remains two independent actors/one shared service. All selected actors still receive final-room normal/focus/transition checks. No hypothetical all-cast repaint. |
| Existing bottle, jigger, spoons, strainer, ice, grip | RETAIN and inspect in all method states | Ten byte-preserved Phase L pixel tools recorded in ACT1 `assets.json`; RC1 `Docs/19_PIXEL_BARTENDING_ASSET_MANIFEST.md` and `ArtSource/Pixel/StyleReference/` record generated versus original raster lineage. Preserve tiny spoon readability and actual hand/tool alignment. |
| Rocks/cocktail/mixing glass | RETAIN shell family, bounded contour finishing only if actual sample needs it | Original authored glass pixels, distinct from generated metal tools. Preserve clear interior and actual fill geometry. Do not make a static illustrated drink replace a container. |
| Tall glass, flute, orange slice and zest | ADOPT inspected source candidates into new assets | Existing RC1 `Glassware/{TallGlass,Flute}.png`, masks and `Garnishes/{OrangeSlice,OrangeZest}.png` are documented original authored pixels. Four images were inspected; hashes/methods are in the new inventory. Their translucent interior is not liquid: flute/tall center alpha is 18/255, with separate masks. Import/cutoff and actual fill clipping require a Player sample. No need to generate new designs. |
| Shaker body and lid | REAUTHOR small candidate shell pair | Current Production has no authored shaker binding. Old RC1 shaker is a preserved anchor with broader legacy-art release-source caveats; do not silently promote it. Create original restrained metal shell/lid matching the existing tool family, retain exact prompt/output and shape bounds. No mechanism or grip-location redesign. |
| MEM-01 star and empty chair | RETAIN EXACT | Current project-authored small star and approved reserved chair. Keep open/quiet variant and existing glow behavior; no taller statue or additional light. |
| MEM-02 through MEM-07 | REWORK surfaces only, preserve definitions/supports | Current shapes are project-authored code in `CampaignMemoryView`. New source-recorded images may add small material/edge detail at existing parts; receipt IDs/variants, scale, world planes, support bounds and activation remain unchanged. No imported RC1 memory system or future completed forms. |
| Fonts, UI and audio | RETAIN source notices; other scheduled owners | Noto OFL stays bundled. P09 owns UI/Codex; P11 motion; P12 audio. This art pass does not invent their completion or duplicate their implementation. |

Source gaps are specific: older `SourceArt/PROVENANCE.json` and the original
project `Docs/CREDITS.md` retain broader pre-existing artwork/public-release
rights caveats. Later cast reuse authorization resolves the bounded reuse
decision, not every old attribution question. Carry those original statements
into the release ledger; map each retained output to its actual source and
authorization. Preserve generated source outputs and exact prompts. Do not
claim external commercial assets are licensed or substitute untraceable web
art. New shaker and memory candidates can be created without importing the old
unresolved shaker image. No new art source is needed for the documented glass
or garnish candidates. This task makes no legal clearance determination.

## Small implementation sequence and concrete files

All paths in this section are proposed future work, not files created by this
preparation. Start only after the root releases the current Unity/Player freeze.

1. **Freeze the active dependency list.** Keep this inventory as the preparation
   snapshot. Create `Validation/Manifests/p10-candidate-before.json` from the
   selected ACT7 scene's active renderers and dependencies, not all dormant
   objects in older 87/147-row inventories. Include ACT1-6 scene/content hashes,
   all protected PNGs, pivots, contacts, active shader/import flags, camera and
   generated material bindings. Identify active snow-cap renderer/material GUIDs
   before authoring; do not infer visibility from an asset existing on disk.
2. **Representative sample.** New `SourceArt/FinalProduction/` retains original
   outputs, exact prompts, references and `assets.json`. Produce only
   `QuietSnow`, snow-cap texture treatment, `ShakerBody`/`ShakerLid`, and a
   `Mem03Windmill` surface sample first. Use the retained warm interior,
   bartender/ordinary/Prince and one later guest as comparison anchors. Use the
   image tool for new bitmap art or edits; metadata inspection, exact source
   byte copies and declared import normalization are separate operations.
3. **Additive binding.** Put candidate textures/materials and a distinct
   `FinalArt.asset` under `Unity/Assets/Surlhwa/FinalArt/`. A small
   `Editor/CampaignFinalArtBuilder.cs` copies the selected ACT7 scene to
   `Scenes/Bar_Q1_Final.unity`, verifies inputs and changes only enumerated art
   bindings. It refuses dirty inputs and never saves an ACT1-6 scene or modifies
   an existing material in place. Keep current scene/history routing and
   content. Do not invoke `CampaignArtBuilder` or
   `CampaignProductionArtBuilder.UpgradeScene` against old production scenes.
4. **Extend existing presentation owners narrowly.** Optional art references in
   `CampaignArtSet`, consumed by `SliceWorkbenchView` and `CampaignMemoryView`,
   are sufficient; missing candidate references preserve existing appearance.
   No new progression/persistence component or general art framework. Shaker
   shell/lid use current anchors and actual open/closed state. Existing manual
   glass switching may cycle supported additional profiles in the final
   candidate; retain ACT1 accepted behavior, old serialized/default behavior,
   quantities and contacts. Recipe glassware metadata and visual coverage do
   not introduce a new domain acceptance requirement or recipe/HOW authority;
   no new catalog, supply or economy system is involved.
   No inference from active request, unseen recipe or expected result. P09 may
   show Known glassware from existing `RecipePresentation.Glassware` only.
5. **Expand the established sample.** Bind source-traced tall/flute and actual
   orange/zest, inspect all Stir/Build/Shake/Float tools, then finish MEM-02/04/05/
   06/07 in the same palette/cluster family. Retain all cast PNGs unless a new
   actual-candidate comparison establishes a specific defect; any such change
   gets its own bounded source output and contact proof. No replacement quota.
6. **Package the actual result.** New `Validation/review-p10-art.html`,
   `P10_ART_PRODUCTION_REVIEW.md`, and `Manifests/p10-candidate-after.json` link
   side-by-side old/new actual Player views, source outputs, exact build identity,
   measured contacts and hardware cost. Expand the final Gate 3 package after
   these samples and their normal-state regressions, without per-asset questions.

## Memory geometry contract for candidate bindings

The existing view creates untextured rectangle meshes. Assigning a texture to
its materials alone is insufficient because those meshes have no UVs. Add
optional textured presentation at the existing part transforms with bounded
UV/alpha geometry; retain the original fallback. Keep each horizontal support
and vertical piece separate. Never turn the whole memory into a billboard where
its support was deliberately horizontal/fixed-world. Compare full bounds and
support planes against the current view before using the candidate.

| Memory | Locked support / meaning | Finishing action |
|---|---|---|
| 01 | Existing Prince chair and current small star | Exact retain; no new prop. |
| 02 | Existing right-counter swing/empty place, origin `(3.45,.995,2.40)` | Weathered wood/rope edge detail; empty place remains empty. Preserve current facing. |
| 03 | Floor fragment `(-2.15,.005,2.80)`, existing restrained wing angle | Sparse wood/weathering; never a complete windmill, flag or moving mechanism. |
| 04 | Fixed world XY on shelf `(0,1.087,2.92)` above shelf top `1.085` | Small stage/skull detail and restrained existing shadow; no new light. |
| 05 | Floor `(1.65,.005,2.50)`; circle horizontal, broken glass/red point fixed world XY | Thin worn circle, broken edges; retain interrupted glass silhouette and contained red point. No intact cup or emissive lamp. |
| 06 | Corrected counter `(-1.65,.995,-.65)`; horizontal water/ripples, half-submerged lotus | Refine petals/waterline within the current small patch. Preserve submerged lower half, existing slow ripple behavior and corrected cup visibility. |
| 07 | Floor `(.65,.005,3.10)`, fixed world XY; bounds approximately x `.33..97`, y `.005..345` | Small weathered anchor plus incomplete hull fragment; no mast, sail, full ship, trigger or light. Preserve the now-observed 720p support and confirm 1080p/focus views. |

Detailed pivots/meshes in each ACT provenance and builder remain authoritative;
this table does not replace them. In particular keep 70 PPU, shared 1.12 scale,
the accepted seated thickness, Odysseus standing `(24,0)`, walking `(41,0)`,
seated pivot `(29,8)` and hip `(41,34)`, and the approved Prince contact contract.

## Verification and exit evidence

- Source checks: every new output has original source/prompt or exact licensed/
  authorized source record, hash, dimensions, alpha bounds, crop/import recipe
  and intended runtime binding. New inventories must distinguish retained,
  candidate and adopted assets. Earlier reference classifications remain in
  historical records; an affirmative final adoption record names the evidence.
- Preservation: rehash this plan's ACT1-6 scene/content/Prince baseline plus
  source PNGs; inspect all serialized diffs. No old material edits, new settings
  churn, camera/stool moves, duplicate actors, extra lights or gameplay state.
- Objective rendering: focused tests for new optional binding/fallback, bounds
  and source-plane contracts, then affected existing presentation/contact tests.
  Run real normal-material Player views at 720p and 1080p for empty/early room,
  paired service, every selected cast, full seven-memory room, focus enter/exit,
  ending-ready/recall/coda and save/Continue. Retain diagnostic tests as support,
  not aesthetic evidence. Assert guest absence throughout ending; remembered
  Prince text must not spawn a Prince actor.
- Bartending: empty/partial/full and overflow, actual ice retention, actual
  garnish, mixing/transfer/strain, shaker open/closed, Float layers and return.
  Confirm liquid stays inside each visible bowl and never fills a stem; shell
  alpha cannot hide contents. Test old two-profile/fallback scenes and manual
  new-profile selection. Recognition, FIT, Known/Partial disclosure and service
  receipts must remain unchanged.
- Contact/occlusion: ground feet, seated hip and lower boots, paired separation,
  hand/tool grip, counter/cup/face clearance, each earned memory's normal/focus
  visibility, waterline and floor/shelf support. Compare visible evidence before
  labelling any new issue a defect. Real transition clips and pixel stability
  at the accepted camera belong jointly to P11.
- Cost: record build size/texture memory, active renderer/material count,
  frame-time and GPU-memory comparison on the same machine/resolutions. Do not
  claim performance from static dimensions or introduce a new effect budget.
- Completion is a source-traced, technically checked, visibly changed and
  reviewable P10 candidate. Subjective art quality, composition, readability,
  motion and emotional effect remain `HUMAN_SIGNOFF_REQUIRED` at Gate 3. Until
  execution and actual captures exist, P10 stays planned, not PASS.

Preparation verification: 42/42 ACT2-7 cast source/destination/declared hashes
match; 43/43 ACT1 normalized PNGs match their source manifest. These checks and
the retained still inspection establish the plan's starting evidence only.
