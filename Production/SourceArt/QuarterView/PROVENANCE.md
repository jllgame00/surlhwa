# Quarter View seated-direction study

Classification: **SPIKE_ONLY_UNAPPROVED_DIRECTIONAL_STUDY**. These are two static
back-quarter seated poses, not Production replacements or a new animation set.
Accepted bartender, Prince, ordinary, environment and tool art is preserved.

Built-in `image_gen.imagegen`, transparent alpha, 2026-10-26. No CLI/API fallback.
Reference inputs: existing `ProductionArt/PrinceSeated.png` and
`ProductionArt/OrdinaryCoatSeated.png` from the accepted ACT 1 art lineage.
The first atlas remains as generation evidence; the second corrects an unwanted
skin-colored ear on the anonymous guest. No outside commercial-game art was used.

Generation is not seed-reproducible. Original outputs are retained byte-for-byte.
`Production/Tools/prepare_quarter_art.py` reproduces the crop and nearest-neighbor
normalization into the existing 96×100 seated canvas. `import.json` records crop
bounds, placements and source/runtime hashes. Unity uses point filtering, no
mipmaps/compression, alpha clipping and real depth writes. No final-art approval
is implied. New quiet-snow pixels are deterministic code-authored material in
`QuarterViewBuilder.Snow`; other new shapes use code-authored shallow geometry.

## Initial prompt

Use case: identity-preserve / bounded game sprite direction test. Create a NEW transparent comparison atlas containing exactly two seated BACK-QUARTER character sprites, side by side, widely separated. Image 1 is the existing Little Prince identity/style reference. Image 2 is the existing anonymous charcoal ordinary winter guest identity/style reference. Preserve each character's clothing, palette, proportions, silhouette family and restrained pixel-art language. Change only viewpoint/pose: BOTH are seated on an invisible bar stool, facing AWAY from the viewer toward the UPPER RIGHT of the image, seen from above at a soft 38-degree quarter-view angle over their back-left shoulders. Their backs, back of hair, and shoulder blades must dominate; only a tiny edge of right cheek may be visible. Little Prince: tousled golden hair, dark green coat with pale warm trim, cream scarf, brown boots, small chibi proportions exactly recognizable from reference. Ordinary guest: anonymous charcoal coat silhouette, muted gray dark hair, face fully unreadable, no colored skin or expressive eyes. Feet visible below seated hips, knees directed upper-right toward unseen counter, arms quietly resting forward. No stools, no furniture, no shadows, no floor, no labels, no scenery. Crisp chunky pixel clusters, restricted value bands, no painted anti-aliasing, no glossy smooth render. Layout: Prince centered in left half, ordinary centered in right half, equal seated scale, full bodies uncropped, broad transparent margins and separate alpha silhouettes. Transparent background with true alpha. Spike-only reusable raster sprites, no commercial game imitation.

## Correction prompt

Edit this sprite atlas with ONE precise correction only: on the RIGHT anonymous ordinary guest, replace the tiny visible peach/brown ear/skin pixels beside the right side of the head with the same dark charcoal-gray shadow color as the adjacent hair/coat. No visible skin, ear, face, eyes or identifiable facial features on the ordinary guest. Keep both sprites, positions, back-quarter seated poses, all Little Prince colors and pixels, clothing and proportions otherwise unchanged. Keep the transparent background with true alpha. No other new marks.
