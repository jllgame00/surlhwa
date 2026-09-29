# Phase L source reference

`Extensions-source.png` is the unmodified built-in imagegen output generated
2026-09-29 from owner-authorized repository references. No external art was
downloaded. It is a source sheet, not directly loaded by Unity.

References (preserved, not edit targets):

- Assets/_SURLHWA/Art/Bartending/Props/shaker.png
- Assets/_SURLHWA/Art/Cocktails/설야.png
- Assets/_SURLHWA/Art/Cocktails/DryMartini-Draft.png

## Exact generation prompt

Create an original SURLHWA bartending pixel-art extension reference sheet, not a redesign of the input art. Inputs are style references only: existing shaker establishes cool slate-violet shadow, silver steel planes and pale restrained highlights; existing cocktail glasses establish cream rim glints, muted blue gray glass and warm pale liquid. Do not redraw shaker or finished cocktails. On transparent background, arrange exactly seven isolated original assets with generous separation in one horizontal row: double-ended metal jigger, elongated upright bar spoon bowl at bottom, Hawthorne strainer with handle downward, tall unlabeled green glass spirits bottle, three compact ice cubes as one cluster, curled lemon twist, small side-view hand in a gentle pinch/grip pose with dark cuff. Believable relative scale: bottle 100 units tall, spoon 110, strainer 55, jigger 35, ice 14, garnish 15, hand 30. Pixel art, crisp square pixels, limited 5-tone material palettes, sparse one-pixel highlights, no noisy gradients, no photorealism. Slightly elevated frontal view with shallow rim ellipses, quiet winter bar mood. No text, logos, scenery or shadows outside objects. This is a source art sheet for a restrained narrative bartending UI. All objects fully visible and disconnected. Keep new metal objects compatible with reference shaker, new glass with reference cocktail highlights. Original design, no named artist or game imitation.

## Preparation and provenance

`Tools/generate_pixel_bartending.py` records exact crop rectangles, authored
runtime dimensions, nearest sampling, alpha cutoff and palette reduction.
Other glass/mask/stream/garnish pixels are original deterministic raster
geometry authored in that script. These are not claimed as AI-generated art.
`assets.json` records output dimensions, pivots, method and SHA-256 hashes.
`StyleLock-board.png` is an inspection composite, not production artwork.

AI output was visually inspected beside the anchors. Metal planes, restrained
highlights and elongated tools are retained; fine source texture is reduced
for runtime. Hand anatomy, glass contour, garnish offsets and exact palette
remain HUMAN_ART_TUNING_LATER. See Docs/18 for the locked rules.
