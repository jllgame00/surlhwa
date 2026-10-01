# Pixel bartending asset manifest — Phase L

Runtime assets are FIRST_PASS; aesthetic review is HUMAN_ART_TUNING_LATER.
Preserved source anchors remain ANCHOR_PRESERVE. No final human approval is implied.

## Per-record defaults

Each row inherits: 100 PPU, UI/Default material, existing Canvas hierarchy
(back fill/mask → ice → shell → garnish/grip), Point/uncompressed/no mipmaps,
RGBA alpha, Single sprite/full rect/zero border. All runtime paths start with
`Assets/_SURLHWA/Art/Bartending/Pixel/`. Runtime dependency is
`Assets/Resources/PixelBartendingArt.asset`, consumed by the existing physical
presenter, spoon/garnish adapters and final result drawing. Integration: ACTIVE.

G = unmodified imagegen source `ArtSource/Pixel/StyleReference/Extensions-source.png`,
prepared by the committed crop/palette recipe. R = original deterministic raster
authored in `Tools/generate_pixel_bartending.py`. Exact dimensions, pivots, provenance,
import defaults and output SHA-256 are also in `ArtSource/Pixel/StyleReference/assets.json`.
No downloaded art or named style imitation. Source generation prompt is in its README.

## Runtime records

| Asset ID / use | Category / runtime PNG | Source | Pixels | Pivot | Relative scale / display | Required states | Quality |
|---|---|---|---|---|---|---|---|
| Jigger | Tools/Jigger.png | G | 28×48 | 0.5, 0.5 | 2 UI/pixel | empty, partial, full, overflow, tilted transfer | FIRST_PASS |
| Spoon | Tools/Spoon.png | G | 12×128 | 0.5, 0 | GentleStir 18×192 | idle, held, technique/transfer | FIRST_PASS |
| SpoonHorizontal | Tools/SpoonHorizontal.png | G | 128×12 | 0, 0.5 | Stir 256×24; Float 200×19 | idle, held, technique/transfer | FIRST_PASS |
| Strainer | Tools/Strainer.png | G | 44×64 | 0.5, 0.5 | 2 UI/pixel | idle, held, technique/transfer | FIRST_PASS |
| BottleTall | Bottles/BottleTall.png | G | 32×112 | 0.5, 0.5 | 2 UI/pixel | selected, held, pour, return | FIRST_PASS |
| BottleBroad | Bottles/BottleBroad.png | G | 40×112 | 0.5, 0.5 | 2 UI/pixel | selected, held, pour, return | FIRST_PASS |
| BottleSmall | Bottles/BottleSmall.png | G | 24×80 | 0.5, 0.5 | 2 UI/pixel | selected, held, pour, return | FIRST_PASS |
| IceCluster | Ice/IceCluster.png | G | 26×24 | 0.5, 0.5 | 52×48 attached; 24×24 held | placement, attached, retained | FIRST_PASS |
| Grip | Hands/Grip.png | G | 40×32 | 0.5, 0.5 | 32–72 UI high; object contact | held/placement only | FIRST_PASS |
| LemonTwist | Garnishes/LemonTwist.png | G | 14×20 | 0.5, 0.5 | 20–36 UI footprint | Rim, InGlass, Surface | FIRST_PASS |
| OrangeSlice | Garnishes/OrangeSlice.png | R | 18×18 | 0.5, 0.5 | 20–36 UI footprint | Rim, InGlass, Surface | FIRST_PASS |
| LemonSlice | Garnishes/LemonSlice.png | R | 18×18 | 0.5, 0.5 | 20–36 UI footprint | Rim, InGlass, Surface | FIRST_PASS |
| Cherry | Garnishes/Cherry.png | R | 14×18 | 0.5, 0.5 | 20–36 UI footprint | Rim, InGlass, Surface | FIRST_PASS |
| Olive | Garnishes/Olive.png | R | 16×12 | 0.5, 0.5 | 20–36 UI footprint | Rim, InGlass, Surface | FIRST_PASS |
| OrangeZest | Garnishes/OrangeZest.png | R | 18×12 | 0.5, 0.5 | 20–36 UI footprint | Rim, InGlass, Surface | FIRST_PASS |
| MixingGlass | Glassware/MixingGlass.png | R | 72×104 | 0.5, 0.5 | 2 UI/pixel; mask follows shell | empty, actual fill, ice, garnish, transfer | FIRST_PASS |
| MixingGlassMask | Glassware/MixingGlassMask.png | R | 72×104 | 0.5, 0.5 | 2 UI/pixel; mask follows shell | empty, actual fill, ice, garnish, transfer | FIRST_PASS |
| RocksGlass | Glassware/RocksGlass.png | R | 56×56 | 0.5, 0.5 | 2 UI/pixel; mask follows shell | empty, actual fill, ice, garnish, transfer | FIRST_PASS |
| RocksGlassMask | Glassware/RocksGlassMask.png | R | 56×56 | 0.5, 0.5 | 2 UI/pixel; mask follows shell | empty, actual fill, ice, garnish, transfer | FIRST_PASS |
| CocktailGlass | Glassware/CocktailGlass.png | R | 80×88 | 0.5, 0.5 | 2 UI/pixel; mask follows shell | empty, actual fill, ice, garnish, transfer | FIRST_PASS |
| CocktailGlassMask | Glassware/CocktailGlassMask.png | R | 80×88 | 0.5, 0.5 | 2 UI/pixel; mask follows shell | empty, actual fill, ice, garnish, transfer | FIRST_PASS |
| TallGlass | Glassware/TallGlass.png | R | 48×88 | 0.5, 0.5 | 2 UI/pixel; mask follows shell | empty, actual fill, ice, garnish, transfer | FIRST_PASS |
| TallGlassMask | Glassware/TallGlassMask.png | R | 48×88 | 0.5, 0.5 | 2 UI/pixel; mask follows shell | empty, actual fill, ice, garnish, transfer | FIRST_PASS |
| Flute | Glassware/Flute.png | R | 32×112 | 0.5, 0.5 | 2 UI/pixel; mask follows shell | empty, actual fill, ice, garnish, transfer | FIRST_PASS |
| FluteMask | Glassware/FluteMask.png | R | 32×112 | 0.5, 0.5 | 2 UI/pixel; mask follows shell | empty, actual fill, ice, garnish, transfer | FIRST_PASS |
| NormalStream | Liquids/NormalStream.png | R | 3×16 | 0.5, 0 | state length; 2–4 UI wide | accepted transfer/overflow only | FIRST_PASS |
| ThinFloatStream | Liquids/ThinFloatStream.png | R | 2×16 | 0.5, 0 | state length; 2–4 UI wide | accepted transfer/overflow only | FIRST_PASS |
| TransferStream | Liquids/TransferStream.png | R | 4×16 | 0.5, 0 | state length; 2–4 UI wide | accepted transfer/overflow only | FIRST_PASS |
| OverflowStream | Liquids/OverflowStream.png | R | 3×16 | 0.5, 0 | state length; 2–4 UI wide | accepted transfer/overflow only | FIRST_PASS |
| SmallDroplet | Liquids/SmallDroplet.png | R | 4×6 | 0.5, 0.5 | state length; 2–4 UI wide | accepted transfer/overflow only | FIRST_PASS |
| Fill | Liquids/Fill.png | R | 2×2 | 0.5, 0.5 | mask width; height from actual ml | empty, partial, full | FIRST_PASS |

## Preserved anchor records

| ID / use | Source = runtime path | Dimensions / PPU / pivot | Sorting / material | States and dependency | Quality / integration |
|---|---|---|---|---|---|
| Shaker | Assets/_SURLHWA/Art/Bartending/Props/shaker.png | 720×720 source; existing 304×605 slice; 100 PPU; original slice identity; UI center pivot | existing Canvas; UI/Default | open preparation/transfer crop, original closed Shake; ShakeMiniGame prefab | ANCHOR_PRESERVE; bounded 130×240 UI display, lower 60% crop for open cup |
| Codex cocktail illustrations | Assets/_SURLHWA/Art/Cocktails/ | Existing original dimensions/imports/pivots retained per meta | Codex UI/Default | Known card, detail/unlock; CocktailData.cocktailSprite | ANCHOR_PRESERVE; unchanged |

## Current cocktail composition coverage

No new cocktail PNGs are required. Nine currently reachable recipes use actual
liquid, optional layers, retained ice and actual garnish with a glass shell.
Final known-result color is CocktailData.liquidColor, derived visually from
the preserved Codex artwork; these are swatches, not edits to those illustrations.

| Cocktail | Glass metadata | Codex-derived swatch | Existing garnish / layers |
|---|---|---|---|
| Seolya | Old-fashioned | #d7a553 | none; mixed |
| Manwol | Old-fashioned | #e9dec0 upper cream | actual ordered Float layers, no garnish |
| Dry Martini | Cocktail | #ddd5b5 | authored twist/olive alternatives |
| Negroni | Old-fashioned | #bd653f | orange slice; serving ice |
| Manhattan | Cocktail | #a5653f | cherry |
| Gin Fizz | Tall tumbler | #e0d8a9 | optional lemon slice/twist |
| Hanky Panky | Cocktail | #98603b | orange zest |
| French 75 | Flute | #ded09e | none |
| Last Word | Cocktail | #b5ba7d | none |

Cocktail glass is a generic stemmed family in this pass; Codex coupe versus V
subtleties remain preserved in the illustrations. A generic Recommendation
uses its chosen method's neutral glass until the resolved known service drawing.
It never changes liquid recognition or exposes a hidden exact answer.

## Presentation data and scale exceptions

- 26 existing IngredientData assets receive tint and one of three bottle shapes.
  This includes already supplied compatibility ingredients, not future content.
- 9 current CocktailData assets receive a liquid swatch. No IDs, recipe contents,
  supply days, knowledge, catalog membership or garnish requirements change.
- Bottle family is 64×224 / 80×224 / 48×160 UI; Jigger 56×96; Shaker 130×240;
  mixing glass 144×208; strainer 88×128; rocks 112×112; cocktail 160×176;
  tall 96×176; flute 64×224. Tools retain different proportions.
- Jigger uses an intentional top-cup volume cutaway. Shaker is opaque steel;
  attached internal fill/ice is not painted through its outer shell.
- Garnishes keep native aspect at 1.4× during preparation and 0.9× in the small
  service drawing. The Shaker opening uses a transient original-pixel crop and
  two small rim regions; no additional asset file or gameplay state is needed.
- Fill and surface lines are state-sized UI regions constrained by bowl masks.
  They are intentional compositional primitives; no second liquid simulation.
- All new assets use UI Images, so SpriteRenderer sorting groups are not
  applicable. No camera, render pipeline, scene or prefab reserialization is needed.

## Baseline remnants

Legacy Props/glass.png, glass2.png, shot.png, jigger.png, barspoon.png and
floatbarspoon.png and inactive legacy Jigger panels are retained. Active spoon
presentation is replaced at runtime. UNKNOWN art remains untouched. Do not
delete these files without a separate dependency audit.

## Drift and review

Reference board and rendered 1280×720 production views were inspected for
relative scale, anchor preservation, outline/glass/metal language and contact.
Objective import/state/transform checks are in SURLHWAPixelArtValidation and
SURLHWAPhysicalBartendingPlayMode. Source art and all new assets remain
FIRST_PASS / HUMAN_ART_TUNING_LATER; attractive glass, hands and final emotional
fit require owner review. Logs/screenshots are local validation evidence.

## Modular world and current chibi cast — Phase L restart

The 31 bartending sprites above remain PRESERVED. World sprites below use
32 PPU, Point/uncompressed/no mipmaps, binary alpha, zero borders and the
existing world SpriteRenderer material. Character pivot is bottom-center;
environment pivot is center. Source preparation/provenance/crop/hash are in
`ArtSource/Pixel/BarRestart/assets.json`; original generation prompts and
atlases remain beside it. All are FIRST_PASS with HUMAN_ART_TUNING_LATER.
Integration: ACTIVE in BarScene through ModularWinterBar.prefab, the existing
ordinary/Prince customer prefabs and sixteen current special encounters.
Sorting layer: Default. The existing world sprite material is retained;
per-piece sorting orders are in Docs/18. The prefab contains 40 independent
SpriteRenderers and seven memory anchors. Character states reuse one pose;
existing movement and seating remain authoritative.

| Asset ID / use | Runtime path | Pixels | Physical size in world units | States |
|---|---|---|---|---|
| WallPanel | Assets/_SURLHWA/Art/Environment/ModularBar/WallPanel.png | 160 × 120 | 5.00 × 3.75 | static modular furniture |
| WinterWindow | Assets/_SURLHWA/Art/Environment/ModularBar/WinterWindow.png | 82 × 96 | 2.56 × 3.00 | static modular furniture |
| Shelf | Assets/_SURLHWA/Art/Environment/ModularBar/Shelf.png | 110 × 104 | 3.44 × 3.25 | static modular furniture |
| ShelfBottles | Assets/_SURLHWA/Art/Environment/ModularBar/ShelfBottles.png | 88 × 34 | 2.75 × 1.06 | static modular furniture |
| Stool | Assets/_SURLHWA/Art/Environment/ModularBar/Stool.png | 34 × 48 | 1.06 × 1.50 | static modular furniture |
| Lantern | Assets/_SURLHWA/Art/Environment/ModularBar/Lantern.png | 28 × 70 | 0.88 × 2.19 | static modular furniture |
| Pedestal | Assets/_SURLHWA/Art/Environment/ModularBar/Pedestal.png | 48 × 54 | 1.50 × 1.69 | static modular furniture |
| Floor | Assets/_SURLHWA/Art/Environment/ModularBar/Floor.png | 256 × 128 | 8.00 × 4.00 | static modular furniture |
| SidePlant | Assets/_SURLHWA/Art/Environment/ModularBar/SidePlant.png | 48 × 72 | 1.50 × 2.25 | static modular furniture |
| DisplayNiche | Assets/_SURLHWA/Art/Environment/ModularBar/DisplayNiche.png | 70 × 64 | 2.19 × 2.00 | static modular furniture |
| CounterDressing | Assets/_SURLHWA/Art/Environment/ModularBar/CounterDressing.png | 47 × 30 | 1.47 × 0.94 | static modular furniture |
| CounterArms | Assets/_SURLHWA/Art/Environment/ModularBar/CounterArms.png | 448 × 168 | 14.00 × 5.25 | static modular furniture |
| CounterTop | Assets/_SURLHWA/Art/Environment/ModularBar/CounterTop.png | 448 × 168 | 14.00 × 5.25 | static modular furniture |
| CounterFace | Assets/_SURLHWA/Art/Environment/ModularBar/CounterFace.png | 448 × 168 | 14.00 × 5.25 | static modular furniture |
| Bartender | Assets/_SURLHWA/Art/Characters/Chibi/Bartender.png | 34 × 84 | 1.06 × 2.63 | idle/walk/seat reusable pose |
| LittlePrince | Assets/_SURLHWA/Art/Characters/Chibi/LittlePrince.png | 61 × 84 | 1.91 × 2.63 | idle/walk/seat reusable pose |
| Chunhyang | Assets/_SURLHWA/Art/Characters/Chibi/Chunhyang.png | 45 × 84 | 1.41 × 2.63 | idle/walk/seat reusable pose |
| Mongryong | Assets/_SURLHWA/Art/Characters/Chibi/Mongryong.png | 44 × 84 | 1.38 × 2.63 | idle/walk/seat reusable pose |
| DonQuixote | Assets/_SURLHWA/Art/Characters/Chibi/DonQuixote.png | 60 × 88 | 1.88 × 2.75 | idle/walk/seat reusable pose |
| Hamlet | Assets/_SURLHWA/Art/Characters/Chibi/Hamlet.png | 54 × 84 | 1.69 × 2.63 | idle/walk/seat reusable pose |
| Faust | Assets/_SURLHWA/Art/Characters/Chibi/Faust.png | 44 × 84 | 1.38 × 2.63 | idle/walk/seat reusable pose |
| SimCheong | Assets/_SURLHWA/Art/Characters/Chibi/SimCheong.png | 51 × 84 | 1.59 × 2.63 | idle/walk/seat reusable pose |
| Odysseus | Assets/_SURLHWA/Art/Characters/Chibi/Odysseus.png | 62 × 88 | 1.94 × 2.75 | idle/walk/seat reusable pose |
| OrdinaryCoat | Assets/_SURLHWA/Art/Characters/Chibi/OrdinaryCoat.png | 36 × 84 | 1.13 × 2.63 | idle/walk/seat reusable pose |
| OrdinaryPonytail | Assets/_SURLHWA/Art/Characters/Chibi/OrdinaryPonytail.png | 39 × 84 | 1.22 × 2.63 | idle/walk/seat reusable pose |

### Active dependencies and preservation decisions

| Category | Restart decision | Active dependency / quality |
|---|---|---|
| All 14 environment modules | NEW, separated architecture/furniture | ModularWinterBar.prefab / FIRST_PASS |
| Bartender | NEW, center inside counter | Same visual prefab / FIRST_PASS |
| OrdinaryCoat / OrdinaryPonytail | Replace active colorful ordinary drafts | Existing Customer_A / Customer_B prefabs / FIRST_PASS |
| LittlePrince | Replace active tall draft with referenced chibi | Existing LittlePrinceCustomer prefab / FIRST_PASS |
| Chunhyang / Mongryong | NEW distinct identities and companion | Existing CampaignEncounterData sprite fields / FIRST_PASS |
| DonQuixote / Hamlet / Faust / SimCheong / Odysseus | NEW current reachable cast only | Existing CampaignEncounterData sprite fields / FIRST_PASS |
| Bottles, Jigger, spoon, strainer | PRESERVED | Existing PixelBartendingArt and Phase K presenters / FIRST_PASS |
| Glass shells/masks, fill, streams, overflow | PRESERVED | Actual container state and existing composition / FIRST_PASS |
| Ice, authored garnishes, grip | PRESERVED | Existing motion/placement adapters / FIRST_PASS |
| Shaker | PRESERVED, no restart source/import changes | Original approved asset and prior bounded technical adaptation / ANCHOR_PRESERVE |
| Codex glass/cocktail art and liquid colors | PRESERVED | CocktailData, existing swatch extraction and served drawing / ANCHOR_PRESERVE |
| Final cocktails | PRESERVED runtime composition; no bespoke bulk PNGs | Recognized identity plus actual fill/layers/ice/garnish / FIRST_PASS |
| Shared glass / stillness | BOUNDED_ADJUSTMENT to depth/attachment | Existing CampaignGuestPresentation, no story-state changes / HUMAN_ART_TUNING_LATER |
| Reaction bubble / hearts | BOUNDED_ADJUSTMENT to head clearance; smaller warm hearts | Existing UI prefabs/components, same results and lifetimes / HUMAN_ART_TUNING_LATER |
| Painted WinterBar-Draft and previous character drafts | LEGACY retained | Old background renderer disabled; previous PNGs not deleted |

All new visual quality remains subject to HUMAN_ART_TUNING_LATER. Seven display
zones hold existing memory contours; no new statue unlocks or story content were
introduced. Source crops, precise per-asset dimensions/pivots and hashes are in
assets.json. Runtime art is separate from original imagegen source sheets.

## Phase N character motion extension (2026-10-01)

The reusable standing art above remains the identity/fallback source. New
`Assets/_SURLHWA/Art/Characters/Motion/` contains 80 guest frame assets (ten
characters × four walk slots plus turn, seated, listening and reaction) and two
bartender accents. Guest canvases are 96×100 with stable feet anchors; visible
character size retains the source proportions. All use 32 PPU, Point and
uncompressed binary alpha. No original Chibi, environment or cocktail PNG changed.

Preparation: deterministic cutout/row translation using owned source pixels,
`Tools/prepare_character_motion.py --all`. Exact paths, source/output hashes,
dimensions and pose names: `ArtSource/Pixel/CharacterMotion/assets.json`.
`Pose-board.png` is a source-art check; actual Unity motion review is linked in
Docs/25. `CustomerMotionLibrary.asset` holds presentation references only.
Four walk slots contain two alternating strides and a repeated neutral contact
pose. Seated knees/hip differ from the standing shape; no clip controls gameplay.

The existing stool sprite is reused for the paired companion's visual seat.
Hamlet omits the repeating listening accent. All first-pass cadence and pose
quality remain HUMAN_ANIMATION_TUNING_LATER; no new public art clearance is claimed.
