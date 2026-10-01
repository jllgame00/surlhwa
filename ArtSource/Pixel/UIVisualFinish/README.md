# Utility glyphs and countertop surface accent

Owner-authorized visual scope, 2026-10-01; starting source `eb8e198`.

## Provenance

Original deterministic code authored for SURLHWA. **No image generation, external
art, new font or new runtime bitmap** was used. The owner explicitly authorized
replacement of the Settings/Codex utility pictograms. Codex content illustrations
and Shaker source pixels were not edited.

- `Assets/Code/UI/SurlhwaUITheme.cs`: `SurlhwaUtilityGlyph` constructs an open
  book with a spine, page edges and four page strokes, and an eight-tooth gear
  with a transparent center. Geometry is normalized to 28×28 UI units. The gear
  has 64 angular samples, radii 10/13 and inner radius 6.5. Book strokes are 2
  units, inner page strokes 1.5. Both use existing ivory `#F1E8D5`, charcoal
  `#20272B`, hover `#303A3E` and warm accent `#B9A06C`.
- `Assets/Resources/CountertopEdge.shader`: original local uGUI shader. Four
  one-display-pixel alpha samples detect actual material edges; alpha .40–.75
  defines material coverage. The glass interior retains its source alpha.
  Existing bright planes receive up to .18 warm tint, inner edges .34 and outer
  edge opacity .64 at full selection. Emphasis is supplied by the existing
  setup's hover/focus/selection projection, never a second method owner.
- `Assets/Code/UI/SurlhwaMethodSetupView.cs`: retains original tool sprites,
  grouping, anchors and contact shadows. Removes oval illumination and uGUI
  duplicated-sprite outlines. Creates/releases one local material per setup.

The shader lives in Resources for an explicit build-retained reference; only
this new asset has a new GUID. No source sprite or import setting changed.

## Reproduction and review

Use Unity 6000.3.7f1 and the existing isolated validation project. The
`SURLHWAPhaseMUIValidation.RunVisualBefore/RunVisualAfter` entries extend the
existing snapshot/restore runner with bounded production-screen fixtures.
They use direct UI events/callbacks and raycast checks, **not pointer delivery**.
BEFORE uses the four starting visual source files in the isolated copy; the
main repository stays on the current branch. Both use the same capture helper,
which refreshes counter projections after changing offscreen camera dimensions.

Run `python Tools/build_ui_visual_review.py` after collecting both capture sets
in `Logs/Review/UIVisualFinish/`. This creates an HTML comparison, a native-crop
PNG board, separate native crops and SHA-256 manifest. It only crops actual
Unity screenshots; it does not fabricate screens or modify runtime art.

All source/full-resolution review PNGs remain individual files at 1280×720 and
1920×1080. Initial captures with the batch Editor's stale 4:3 counter projection
are archived under `initial-projection/` and are not final visual evidence.

HUMAN_VISUAL_APPROVAL_PENDING. CHARACTER_ANIMATION_DEFERRED. DEFERRED_AUDIO.
