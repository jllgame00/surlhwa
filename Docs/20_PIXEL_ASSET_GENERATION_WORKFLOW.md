# SURLHWA generated pixel asset workflow

This is the repeatable Phase L pipeline. Read Docs/18 before generating.
Docs/19 and ArtSource/Pixel/StyleReference/assets.json identify existing assets.
Current gameplay needs control scope; generated art never unlocks content.

## Need → reference → author → integrate

1. Verify Git status/log and read AGENTS.md. Exclude owner changes, especially
   `.codex/config.toml`. Read current vision, gameplay, physical design and audit.
2. Prove the asset is needed by reachable gameplay. Inspect actual recipe,
   ingredient, garnish and glass metadata. Reuse a bottle/tool family first.
3. Inspect the original Shaker, Codex illustrations and StyleLock-board.png.
   Keep their source pixels intact. Use the metal/glass/liquid rules in Docs/18.
4. Determine proportions before choosing canvas dimensions. At the 1280×720
   reference Canvas use roughly two UI units per source pixel. A 28×48 Jigger
   is deliberately smaller than a 32×112 bottle and preserved Shaker.
5. Generate original source with the built-in imagegen skill when appropriate.
   Use the exact reference prompt in ArtSource/Pixel/StyleReference/README.md as
   a specification template. Explicitly name references as references, not edit
   targets; forbid named artist/game imitation, text, logos, noisy gradients,
   extra objects and baked liquid amounts. Save the unmodified output locally.
6. If imagegen is unavailable, continue with original deterministic raster or
   owned project components. Record the actual method. Never scrape art or
   describe procedural output as AI generation.
7. Prepare the sprite at its authored pixel dimensions. Store crop coordinates,
   palette, alpha policy and any matrix/geometry in the source recipe. Current
   script uses nearest sampling, binary source alpha and fixed material ramps;
   glass shell interior uses low alpha. Do not process the preserved anchors.
8. Save runtime PNGs only in the appropriate Pixel category folder. Preserve
   existing GUIDs. The Phase L importer enforces Point/100 PPU/uncompressed/no
   mipmaps/alpha/clamp. It does not touch other art.
9. In Unity run **SURLHWA → Art → Rebuild Phase L Sprite References**. This
   updates only the presentation library at Assets/Resources/PixelBartendingArt.asset.
   Use the existing Phase K presenter; never introduce a new composition,
   matcher, supply or knowledge authority. Assign UI pivots explicitly.
10. Check reference-size rendering, then movement: grip, neck/rim contact,
    spoon bowl pivot, fill/mask/surface alignment, ice retention, garnish,
    rotation, hierarchy order and reading-panel overlap. Decorative Images must
    not intercept raycasts. Do not infer acceptance from a source PNG alone.
11. Run the checks below. Inspect screenshots. Update Docs/19 and source
    provenance/hashes. Record environmental debt without claiming runtime PASS.
12. Inspect all diffs, including every serialized asset/scene/prefab touched.
    Exclude Editor-local/generated font churn, compile, and commit bounded work.
    Check the complete phase range as well as unstaged changes. Unity can add
    trailing spaces to empty meta fields; trim only new Phase L metadata and
    verify an ignore-end-of-line-whitespace diff is empty before committing.

## Reproduce existing runtime pixels

Requirements: Python 3 and Pillow; Unity 6000.3.7f1 for import/validation.
No network call, secret, API key or model rerun is needed to reproduce pixels.

```powershell
python Tools/generate_pixel_bartending.py
dotnet build Assembly-CSharp-Editor.csproj --no-restore
git diff --check
```

The script writes only declared Phase L outputs, their first-time metas and
source reference board/JSON. Existing metas are preserved. Re-running without
source changes must leave PNG SHA-256 hashes unchanged. Source-generation
imagegen is not deterministic; committed pixel preparation is deterministic.

Unity entry points (use the licensed Editor, hidden process when automated):

- `SURLHWAPixelArtImport.RebuildReferences`: import registry and project sync.
- `SURLHWAPixelArtValidation.Run`: asset/import/pivot/scale/fill contracts,
  final drawing snapshot, Phase K and integrated campaign Editor regressions.
- `SURLHWAPhysicalBartendingPlayMode.Run`: four real method prefabs, actual
  motion/transfer/ice/garnish, Float ingredient color, final snapshot and captures.
- `SURLHWACampaignPlayModeValidation.Run`: complete 18-night route and ending,
  discovery/retry, Recommendation, Continue, memories and reset.
- `SURLHWAServiceFeedbackPlayModeValidation.Run`: production feedback timing
  and served-drink drawing capture, using its explicit completed-drink fixtures.

When intentionally extending the declared sprite set, update the manifest and
the validator's expected library membership in the same bounded change. Do not
weaken state, import, scale or authority checks to accept an accidental asset.

Play Mode entry points manage their own exit; do not pass `-quit` to them.
Omit `-nographics` when capturing. Captures/logs are ignored local evidence in
Logs/, not runtime dependencies. A normal licensed run may be needed when the
sandbox licensing/package-manager process cannot initialize.

## Color ownership and final serving

`IngredientData.liquidColor/bottleShape` owns ingredient presentation.
`CocktailData.liquidColor` is a first-pass swatch derived from its existing
Codex illustration. Do not recolor Codex PNGs to fit a generated runtime asset.
`CocktailData.glassware` remains presentation metadata. Generic routes use a
neutral method-family glass; explicit recipe requests may use their authored
glass family. This never changes what the liquid is or how it is evaluated.

During preparation, mixed color is weighted from actual liquid entries. Float
uses actual ordered ingredient colors. The final existing service-result panel
can use the resolved cocktail's swatch and glass only after OrderManager has
recognized it and permitted identity disclosure. Its small drawing also uses
the immutable actual fill/layer/ice/garnish snapshot captured before unloading.
The drawing cannot evaluate or restore gameplay state; it is never persisted.

## Reject drift

Reject glass with opaque interiors or liquid in stems, overly large Jiggers,
short thick spoons, neon Float stripes, hidden volume, disconnected hands,
blurred imports, replaced anchors, scene serialization churn and expected
recipe contents painted over actual player contents. Compare at display size.
Minor hues, hand anatomy, small offsets and apparent attractiveness are
HUMAN_ART_TUNING_LATER; they do not require redefining the style each session.

Future cocktails, ingredients and artwork require their own bounded scope.
Do not generate the full IBA collection or new guest/environment art here.
