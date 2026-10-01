# Character motion source record

Phase N derives a small pose set from the accepted Phase L runtime Chibi PNGs.
Those are prepared outputs of the retained original source atlases and owner
references documented in `../BarRestart/prompts.json` and `../BarRestart/assets.json`.
No external download or new image-generation call was used for these frames.

Reproduce from the repository root:

```powershell
python Tools/prepare_character_motion.py --all
```

The Python/Pillow recipe performs integer pixel cutouts/translation and nearest
row mapping. Head/costume identity and source palette remain; lower limbs and
hems form alternating steps and a bent seated pose. Four walk slots use two
strides plus a repeated neutral contact pose. Turn and stand reuse the limited
intermediate pose. Listening and reaction shift the head by one source pixel.
Two bartender variants shift only a bounded hand or head cutout.

Runtime: `Assets/_SURLHWA/Art/Characters/Motion/`. `assets.json` records each
source and output; `Pose-board.png` shows the guest set at native pixel scale.
Guest canvases are 96×100, bottom-center anchor, 32 PPU. Bartender variants keep
the original canvas. New importer metadata is deterministic; existing GUIDs
are never regenerated. Original character/environment/cocktail art is untouched.

The default and `--all` reproduce the full current library. `--ordinary-only`
retains the bounded foundation recipe; it intentionally writes an ordinary-only
library and is not the integrated production set.

Actual Unity frame sequences are indexed by `Logs/Review/PhaseN/review.html`.
Source pose boards and successful import are not aesthetic approval. Limited
three-quarter facing, foot cadence, cloth deformation and seat anatomy remain
HUMAN_ANIMATION_TUNING_LATER. Bespoke directional redraws may improve the current
limited articulation later without changing gameplay state ownership.
