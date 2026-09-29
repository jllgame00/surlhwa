# Phase L restart — reference audit and modular bar

Baseline `8e8ac61`, `refactor/project-rebuild`, 2026-09-29.
Prior Phase L technical records remain historical, not proof of approved
whole-scene composition. The owner explicitly authorized this visual correction.

## L1R reference audit

All five files in `ArtSource/Reference/VisualDirection/` were inspected and
are preserved unchanged as owner-provided direction authority.

| Filename | Role and authoritative traits |
|---|---|
| barscene_2_5d_layout_anchor.png | Frontal/elevated U counter, central bartender inside, guests outside at stools, dense warm cabinetry, central/side displays, peripheral snow, upper-corner controls and lower reading panel. Annotations specify roles, not runtime UI art. |
| special_guest_prince_anchor.png | Chibi proportions, blond head silhouette, teal/gold coat, cream scarf/cape, burgundy accents and compact feet. |
| special_guest_chunhyang_anchor.png | Chibi proportions, cream jeogori, muted rose skirt, brown hair, burgundy ribbon and restrained soft expression. |
| ordinary_guest_male_style_ref.png | Short hair, scarf, long coat and compact proportions. Visible face is subordinated to the owner's explicit anonymity rule. |
| ordinary_guest_female_style_ref.png | Tied hair, coat/scarf and bounded build variation. Remove individualized face/skin emphasis under the same anonymity rule. |

## Current implementation audit

- BarScene camera is orthographic, size 5.4. Three existing seats are at x
  -3.2/0/3.2 along y -2.55 with separate approach/sit/exit markers.
- SnowBackground displays WinterBar-Draft.png, a monolithic eye-level painted
  room. Prototype U-counter renderers are disabled. The underlying seat plan
  and visible room no longer agree.
- Ordinary A/B and Prince prefabs use tall individualized draft art. Campaign
  presentation reuses the generic sprite for later specials and duplicates it
  for the companion. These fail the corrected visual classes.
- CampaignBarMemories already owns departure-derived visibility. Move its
  spatial line drafts into display furniture without changing unlocks.
- BartendingScene uses four physical method prefabs. Their 31 pixel sprites,
  original Shaker, Codex swatches and choreography remain useful.
- Keep the Bar canvas controllers and lifecycle; allow bounded layout changes.
  No recipe, matcher, supply, knowledge, save or technique replacement.

## L2R / L3R implementation contract

Use a small visual prefab of independently replaceable sprites, not a tile
engine or new scene. Normalize generated source atlases to a 32 PPU world grid.
Split counter pieces to place the bartender behind its foreground edge.
Retain the old painted asset as legacy, but remove its active display.

Current cast only: bartender, two ordinary silhouettes, Prince, Chunhyang,
Mongryong and five other already-reachable campaign specials. One reusable pose
per figure is the bounded first pass. No future cast/content expansion.

Preserve seats and movement/interaction authorities; align markers, stools and
feet as needed. Keep orthographic rendering, additive bartending, memories,
narrative and persistence. Rendered inspection is required; human aesthetic
approval is separate. Executed records follow implementation.

## Starting worktree exclusions

Owner config, 31 existing pixel PNG metas (whitespace-only changes) and
UserSettings/Layouts/default-6000.dwlt were already modified. Baseline bytes
are retained separately; do not stage/revert them as restart work. The five
untracked owner reference images are added unchanged as the authorized source
set. No missing-reference stop condition applies.

## L3R source and preparation milestone

Three original imagegen source atlases and exact prompts are retained in
`ArtSource/Pixel/BarRestart/`. `Tools/prepare_modular_bar.py` extracts 14
environment sprites and 11 current character sprites at 32 PPU. Counter arms,
top and face occupy disjoint pixels on an aligned canvas. Ordinary guests use
a fixed five-color charcoal ramp and featureless faces. Prepared-assets.png
was visually inspected; all outputs remain FIRST_PASS / HUMAN_ART_TUNING_LATER.
The bounded importer leaves the 100 PPU bartending art and anchors untouched.

## L4R character presentation

Existing ordinary A/B prefabs now bind the two charcoal sprites; the Prince
prefab binds its referenced chibi. Sixteen existing special encounters carry
presentation-only character/companion sprite references. Existing movement
and seating use the same pose, with feet pivots and an overhead interaction
anchor. Lovers use separate Chunhyang/Mongryong art and companion depth follows
the existing customer. No visit IDs, dialogue, orders or routing changed.
Character Editor validation passed in the live Unity Editor. Reusable poses
remain HUMAN_ART_TUNING_LATER.

## L5R–L7R integrated room and UI

BarScene now contains ModularWinterBar.prefab: repeated floor/wall sections,
two winter windows, shelves and independent bottle dressing, three rear niches,
three counter occlusion planes, bartender, three stools, two lamps, side
plants and four supporting pedestals. Seven serialized anchors place the
existing departure-derived memory contours in display furniture. Their unlock
logic is unchanged. The old SnowBackground renderer is disabled; its source
and inactive prototype geometry remain for dependency-safe history.

Character feet are at the stool line y -3.3; approach and exit markers retain
the existing relationships. The spawn line follows this seating adjustment.
The camera remains orthographic 5.4. The method panel is a compact upper
selector; dialogue stays in the lower reading band. Dark wood controls with
cream labels correct the rendered white-on-white method-button issue.
Codex/settings keep their established corner positions.

Compile: zero errors (existing eight warnings). Live Editor modular/character,
Phase L pixel/physical and integrated campaign static checks passed. Play Mode
fixtures captured and inspected empty, ordinary, Prince, Chunhyang, method-ready
and dialogue views. They verify visual staging, not a full campaign run.
The live Editor external-change Reload dialog was resolved; no environmental
validation blocker remains. Full-route and physical Play Mode evidence follows.

Phase K Play Mode passed for Build, Stir, Shake and Float after room integration.
Existing containers, amounts, streams, overflow and tool transforms remain
authoritative. L6R preserves all 31 prior pixel sprites, original Shaker, Codex
glasses and cocktail color extraction. The bartending workspace remains the
existing additive close view; its finer 100 PPU tools coexist with 32 PPU world
art rather than being rescaled through the Bar canvas.

A follow-up depth audit found the Quixote/Faust positions behind counter arms.
They now occupy two additional rear niches beside the central Hamlet niche;
shelves/windows move outward within the same camera. A pixel-alpha contract
checks all seven anchors against foreground counter occlusion. The first full
campaign attempt was interrupted through its normal cleanup/restore path; it
is not counted as a completed regression. The complete run restarts after
these view-only corrections. The separate display-layout capture is explicitly
a visibility fixture; earned-memory evidence comes from the complete campaign.

The corrected display-layout fixture shows all seven contours above their
furniture, with counter occlusion checks and Play Mode fixtures passing.
Final compile after this correction: zero errors, eight existing warnings.

The same foreground-depth audit was extended to encounter-local contours.
The lovers' shared glass now follows guest depth in front of their bodies;
Hamlet's local stillness contour follows the feet below the sprite. Reaction
bubbles/hearts use the actual sprite bounds to stay above the larger chibi
heads. These are view-only adjustments; service outcomes, timing and story
flags are unchanged. Focused fixtures cover paired guests, stillness and
reaction separation. Both early interrupted campaign attempts are excluded
from completed-run evidence; each exited via progress-restoring cleanup.
