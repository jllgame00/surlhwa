# SURLHWA pixel art style bible — Phase L

First-pass lock, 2026-09-29. Product authority remains Docs/00 and Docs/01;
physical state and choreography remain Docs/11 and Docs/17. This document owns
presentation rules, never recipes, knowledge, supply, technique or persistence.
Human aesthetic approval remains HUMAN_ART_TUNING_LATER.

## 1. Preserve → analyze → normalize → extend

Baseline: `9d052cb`, `refactor/project-rebuild`. Only the owner's
`.codex/config.toml` was modified before work; it is excluded from this phase.

Primary anchors:

- `Assets/_SURLHWA/Art/Bartending/Props/shaker.png`: preserve original pixels,
  silhouette, slate/violet steel planes, pale silver vertical glints and dark
  collars. The existing sliced sprite and GUID remain. Phase K currently clears
  its runtime Image; restoring that assignment is required, not a redesign.
- `Assets/_SURLHWA/Art/Cocktails/`: preserve every existing Codex illustration.
  Seolya has coarse warm glass glints; Manwol has cool glass and cream over dark
  liquid; the seven generated drafts use finer illustrative shading. They are
  material/color references, not a claim that existing art shares one pixel grid.
- Codex uses `CocktailData.cocktailSprite`. Recipe Guide currently presents
  learned recipe text; it does not contain an independent liquid renderer.

Existing reference art is retained under the owner's explicit Phase L direction.
Its historical provenance is recorded in Docs/CREDITS.md; this internal pass
does not claim new public-distribution clearance. New source art is generated
without external downloads or named artist/game imitation.

## 2. Baseline visual audit

| Active asset/path | Classification | Decision |
|---|---|---|
| Shaker original, Shake prefab Image | ANCHOR_PRESERVE | Restore at runtime; preserve source and sliced identity |
| Codex cocktail sprites and colors | ANCHOR_PRESERVE | Preserve all; extract material/color direction |
| BartendingScene and four method prefabs | PRODUCTION_USABLE | Keep Canvas/cameras, input and choreography |
| Physical bottle, neck, Jigger, vessel walls | RUNTIME_PRIMITIVE | Replace view graphics only |
| StateLiquid, stream, overflow, ice, strainer | RUNTIME_PRIMITIVE | State-driven pixel composition |
| Stir/Float legacy spoon sprites | STYLE_MISMATCH | Replace active visual while preserving tip orbit |
| GentleStir spoon and garnish markers | RUNTIME_PRIMITIVE | Shared new sprites; preserve actual placement |
| Manipulating hands | MISSING | One reusable grip, small and secondary |
| Legacy glass/glass2/shot/jigger; inactive Jigger panels | LEGACY | Retain; no deletion/migration |
| Other untraced artwork | UNKNOWN | No edits or deletion |

Source and rendered Phase K captures confirm rectangular vessels, uniform amber
fill, colored rectangle tools, and no hand. Shaker was hidden by the physical
presenter, not missing from the prefab. Existing props use 100 PPU, bilinear
filtering and compressed import; new assets must use the bounded rules below.
The production scene instantiates the four method prefabs through existing
MiniGameManager routing. New runtime art must be used through that same route.

## 3. Visual goals and atmosphere

Quiet winter interior, cool surrounding values, localized warm highlights.
Objects explain listening and careful preparation. No glowing rarity colors,
score particles, exaggerated squash, comic proportions or visual reward loops.
Front view with a slight elevated view of shallow rim ellipses. Do not mix
isometric tools with frontal glassware. Silhouette readability comes first.

## 4. Technical standard and scale

- New sprites: 100 PPU, RGBA PNG, Point, uncompressed, no mipmaps, clamp,
  sRGB, alpha transparency, Single sprite, full rect mesh, zero borders.
- Author at real pixel dimensions; typically 8–128 pixels per axis. Tall spoon
  and narrow flute are not enlarged to fill square canvases. Canvas dimensions
  follow silhouette; two-pixel transparent margins where useful.
- At 1280×720 reference Canvas, default two UI units per source pixel. Keep
  transforms at unit scale and set RectTransform dimensions. Shaker is an
  explicit preserved high-resolution exception, displayed at 130×240 UI units
  to keep the pouring bottle below the technique readout. Its existing 304×605
  slice has coarse pixel clusters; do not pretend it is native low-resolution art.
- Jigger about 40% of closed Shaker height; bottle around its height;
  spoon elongated; strainer head fits a working mouth; garnish 16–32 UI units.
- Existing ScreenSpaceOverlay Canvas and camera settings remain. No global
  PixelPerfectCamera replacement. Static positions should use integer UI units.
  Fractional motion/rotation is allowed for Phase K continuity; Point filtering
  cannot eliminate rotational shimmer or all noninteger Canvas scaling.
- UI Images use default UI material and hierarchy order, not SpriteRenderer
  sorting layers. Future world sprites use Sprites/Default on existing layers;
  do not add a render pipeline or sorting system for this pass.

## 5. Palette, outline, shade and highlight rules

Metal: charcoal #292b3b, violet slate #535675, steel #858d99,
silver #becbd6, restrained cream #eee5cd. Use 4–6 material tones, broad planes,
one-pixel glints and at most a two-pixel edge. Avoid full-object gloss/noise.
Glass: cool dark edge, sparse silver/cream rim, interrupted side highlight,
visible base, transparent interior. Never fill empty glass with opaque gray.
Outlines are material edges, not uniformly black cartoon borders.
Warm liquid may be more saturated than glass; no neon rainbow layers.

## 6. Material and object contracts

- **Glass:** distinct rocks, cocktail, tall, flute and mixing silhouettes.
  Bowl masks constrain actual fill; stem/foot never contain liquid. Surface
  height follows actual volume. Rendering metadata does not select a recipe.
- **Metal:** new Jigger, spoon and strainer reuse the Shaker's cool planes and
  narrow glints. Never regenerate the approved Shaker to match new tools.
- **Liquid:** retain Codex reference colors. IngredientData owns ingredient
  visual tint; CocktailData owns recognized-drink tint. Use accepted recognition
  results for identity; do not match expected orders or invent a color matcher.
  Unrecognized mixtures use volume-weighted actual ingredients. Float colors
  follow each actual ingredient, not its index or an expected layer recipe.
- **Bottle:** a small family, colored contents, contrasting label/accent and
  cap. Existing shelf names carry precise identity; no tiny illegible lettering.
- **Jigger:** smaller than working containers; one shell plus observed fill.
  EMPTY/PARTIAL/FULL/OVERFLOW/TILTED_TRANSFER use existing state and transforms.
- **Spoon:** continuous shaft, readable bowl, bowl-end pivot. No windmill spin.
- **Strainer:** recognizable perforated head/coil impression, compact handle;
  attached to source through tilt. No separate transfer authority.
- **Ice:** compact cool clusters, attached to actual HasIce owner; retain source
  ice through Strain. No duplicate state or simulated melting/refraction.
- **Garnish:** six existing selectable types only: twist, cherry, olive, orange
  slice, lemon slice, orange zest. Rim/InGlass/Surface positions follow state.
- **Hand:** one reusable grip/pinch and dark cuff, secondary to the object.
  Place away from liquid readout, preserve believable contact. No rigs or arms.

## 7. Animation, pivots and sorting

Reuse Phase K Transform motion. One frame per shell, no redundant pouring
frames. Bottle pivot is grip; a named neck determines stream origin. Vessel
root is center with rim at upper edge. Spoon is bowl-end; horizontal source
with left pivot for Stir, upright bottom pivot for GentleStir. Garnish pivots
center. PNG pivots and UI RectTransform pivots must both be checked.

The Shaker's open loading/transfer view crops the lower 60% of its existing
slice at runtime and adds a restrained dark mouth/silver rim. Closed Shake uses
the exact original sprite. Mouth alignment follows that visible opening; this
is a bounded technical adjustment, not a new Shaker design. Opaque steel hides
internal fill/ice. Spoon orbits use the actual liquid region as their visual
center, retaining Phase K input, direction reversal and restrained shaft lean.

Back-to-front within vessel: bowl mask → fill/surface → ice → shell highlights
→ garnish/tool grip. Opaque metal hides internal contents; contents remain
attached. Streams sit above workspace objects but below reading/selection UI.
Never put decorative Images in the raycast path. Hand follows its owner.

## 8. Source, names and generation

Source: `ArtSource/Pixel/StyleReference/`, executable authoring recipes in
`Tools/`. Runtime: `Assets/_SURLHWA/Art/Bartending/Pixel/` by category.
Use descriptive PascalCase names, no random suffixes in runtime paths.
Keep original source, exact prompt, crop/normalization parameters, generated
pixel matrices and hashes. Record actual method: imagegen source versus
deterministic raster/mask, never label procedural pixels as AI generation.
Only newly generated Phase L assets receive the new importer rules.

## 9. Drift gate

Compare at intended display size beside original Shaker and Codex art:
pixel/detail density, 1–2 pixel edges, perspective, saturation, highlights,
glass transparency, metal planes, liquid identity and real-world scale.
Reject blurred pixels, huge Jiggers, tiny spoons, liquid in stems, opaque
glass, detached hands, unrelated layer colors, noisy sparks, and output that
only looks good on a source sheet. Validate actual production prefab rendering.
Do not silently re-lock the style on each generation.

## 10. HUMAN_ART_TUNING_LATER

Exact hues, one-pixel cleanup, glass curvature, Jigger proportions, hand pose,
garnish offsets, stream width, rotational shimmer, apparent metal/glass quality,
and complete workspace composition require later owner review. This lock is
a coherent implementation direction, not subjective approval or shipping art.
