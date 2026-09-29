# Modular winter bar source set

Phase L restart, 2026-09-29. Original raster source images were generated with
OpenAI imagegen using the owner references under
ArtSource/Reference/VisualDirection/. Exact generation instructions are in
prompts.json. No web artwork was downloaded and no named artist/game style
was requested. Source PNGs are retained unchanged; they are not runtime assets.

- Environment-source.png: isolated furniture and architecture on transparency.
- Characters-source.png: bartender and eight currently relevant special
  character identities, including the separately drawn Mongryong companion.
- Ordinary-source.png: two anonymous winter-coat silhouette variants.

## Reproduce

Run `python Tools/prepare_modular_bar.py` from the repository. Python 3 and
Pillow are required. Explicit crops, nearest-neighbor preparation, palette
reduction and alpha thresholds are the source recipe. Ordinary art is mapped
to five charcoal colors. Floor preparation removes the atlas frame so repeated
interior sections do not expose transparent seams. Counter arms, top and face
use disjoint parts of one canvas for exact occlusion alignment.

Preparation was executed with Python 3.13.1 and Pillow 11.2.1. After changing
tool versions, compare the output SHA-256 values in assets.json before accepting
any new pixels. A successful export alone is not a style or integration pass.

The script writes only its declared runtime PNGs, assets.json and
Prepared-assets.png. Existing .meta files are never overwritten by the script.
The bounded Unity importer enforces 32 PPU, Point, uncompressed, no mipmaps,
clamp and full rect. Character feet use bottom-center pivots; furniture uses
center pivots. These rules do not apply to the preserved 100 PPU bartending set.

In Unity use **SURLHWA / Art / Rebuild Modular Winter Bar** to rebuild the visual
prefab. Unique child names preserve Unity references. It does not save scenes,
change seats, award memories, or create game state. BarScene contains one prefab
instance plus seven display-anchor bindings in CampaignBarMemories. Existing
customer prefabs and encounter data carry character sprites directly.

Run **Modular Bar References**, **Modular Bar Play Mode Render Fixtures**,
**Physical Bartending Play Mode**, and **Run Full Campaign Play Mode** under
SURLHWA / Validation. The last two retain batch-mode exit behavior, but leave an
interactive owner Editor open. Full campaign validation backs up and restores
existing progress. Focused render fixtures never write progress.

Review the actual rendered room at 1280x720. Source sheets and import checks
cannot approve composition. FIRST_PASS / HUMAN_ART_TUNING_LATER applies to all
new assets. Do not generate future cast or environments without bounded scope.

After the focused fixtures and complete campaign run, execute
`python Tools/build_bar_review_board.py` to assemble Review-board.png from six
actual production captures. Its first five panels are staged visual fixtures;
the memory panel is earned by the full campaign test. Native 1280×720 captures
remain in Logs/Review for local inspection. The board is a review artifact,
not runtime art or a replacement background.
