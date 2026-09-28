# Cocktail illustration drafts — 2026-09-28

Status: integrated drafts; final artistic approval is HUMAN_SIGNOFF_REQUIRED.

The owner requested drafts for all remaining cocktails in the style of the existing Seolya and Manwol illustrations. Seven previously unassigned CocktailData assets now have artwork. Existing Seolya, Manwol, Yeomyeong and Janseol images are preserved. Reserve artwork does not enroll those drinks in the four-night production catalog.

## Provenance and integration

Generated with the built-in imagegen tool, one generation per asset, using both repository images as visual references:

- `Assets/_SURLHWA/Art/Cocktails/설야.png`
- `Assets/_SURLHWA/Art/Cocktails/만월.png`

Generated PNGs are copied unchanged from the tool output. No pixel processing or alpha reconstruction was performed. New sprite GUIDs belong only to new image assets. Cocktail, recipe and ingredient IDs, knowledge, supply and progression remain unchanged. CocktailData.cocktailSprite remains the presentation authority. Actual PNG alpha and Unity single-sprite references were validated. These are AI-generated drafts; repository reference-art provenance and final shipping selection still require owner review.

## Files and intended subjects

| Asset | Intended subject | Scope |
|---|---|---|
| DryMartini-Draft.png | Clear pale martini, V-shaped stem glass, olive | Production |
| Negroni-Draft.png | Ruby-orange drink, rocks glass, ice, orange half-slice | Production |
| Manhattan-Draft.png | Reddish amber drink, coupe, cherry | Production |
| GinFizz-Draft.png | Cloudy pale lemon drink, tall tumbler, fizz, lemon slice | Production |
| HankyPanky-Draft.png | Dark amber drink, coupe, orange zest | Reserve art only |
| French75-Draft.png | Pale golden bubbles, champagne flute, no garnish | Reserve art only |
| LastWord-Draft.png | Muted chartreuse drink, coupe, no garnish | Reserve art only |

All files live in `Assets/_SURLHWA/Art/Cocktails/`. Each is assigned to the correspondingly named `Cocktail_<Name>.asset`. Subject details follow the existing recipe presentation; artwork does not change technical recipe truth. Existing Seolya artwork is retained even though its depicted vessel differs from current gameplay glassware.

## Human review

Review texture and highlights beside Seolya/Manwol, small card readability, apparent drink size, silhouette/garnish distinction, transparent edges against the actual UI, and visual consistency in the Codex detail and unlock popup. The generated set is more finely shaded than the coarse edge texture in Seolya; accept or refine that difference as part of draft review. French 75's narrow silhouette reads smaller than the broad coupes on an equal-size square card. No final visual or in-game composition PASS is claimed.

## Final generation prompts

Both reference images above were supplied to every call. Prompts below are the exact final prompts.

### DryMartini

```text
Use case: stylized-concept. Create ONE new game cocktail inventory illustration for SURLHWA: DRY MARTINI. The two input images are STYLE REFERENCES only (Seolya and Manwol), not edit targets. Match their quiet illustrated winter-bar mood: carefully drawn glass contours, warm cream highlights, muted silver-blue glass reflections, textured painterly shading and restrained luminous liquid. Favor an illustrated sprite, not product photography or smooth 3D. Subject: one elegant classic V-shaped stemmed cocktail glass with crystal-clear nearly colorless pale straw liquid, one small green olive on a short pick resting within the rim. No ice, no foam. Slightly elevated frontal view showing the rim ellipse. Entire glass, stem and foot visible, centered on a square 1024x1024 canvas, drink occupies about 76% of canvas height, comfortable empty margins. Truly transparent RGBA background outside the glass, no backdrop, vignette, tabletop, cast shadow, label, letters, logo, snowflake decorations or additional objects. Keep cocktail silhouette legible at small Guide-card size. Deliver a single clean isolated PNG asset.
```

### Negroni

```text
Use case: stylized-concept. Create ONE new isolated cocktail inventory illustration for SURLHWA. Input images 1 (Seolya) and 2 (Manwol) are style references only, not edit targets. Match their quiet winter-bar illustration: drawn glass contours, warm cream highlights, muted silver-blue glass reflections, fine painterly texture and restrained glow. Consistent with the existing sprites, not photography, smooth 3D or flat vector. Slightly elevated frontal view with visible rim ellipse, entire glass and foot in frame, centered square canvas, object about 76% of canvas height. Truly transparent RGBA background outside the object, comfortable empty margins, no backdrop, vignette, tabletop, cast shadow, snowflakes, text, labels, logos or unrelated props. Legible at small Guide-card size. Single PNG asset. Subject: NEGRONI, a short wide heavy-bottomed old-fashioned rocks glass containing clear deep ruby-orange cocktail, three large angular translucent ice cubes, and one half-slice of fresh orange tucked at the rim. The orange-red drink, visible ice and orange half-slice distinguish it. No stem, no straw, no spoon.
```

### Manhattan

```text
Use case: stylized-concept. Create ONE new isolated cocktail inventory illustration for SURLHWA. Input images 1 (Seolya) and 2 (Manwol) are style references only, not edit targets. Match their quiet winter-bar illustration: drawn glass contours, warm cream highlights, muted silver-blue glass reflections, fine painterly texture and restrained glow. Consistent with the existing sprites, not photography, smooth 3D or flat vector. Slightly elevated frontal view with visible rim ellipse, entire glass and foot in frame, centered square canvas, object about 76% of canvas height. Truly transparent RGBA background outside the object, comfortable empty margins, no backdrop, vignette, tabletop, cast shadow, snowflakes, text, labels, logos or unrelated props. Legible at small Guide-card size. Single PNG asset. Subject: MANHATTAN, a graceful stemmed rounded coupe cocktail glass containing clear deep warm reddish amber-brown liquid and one dark red cocktail cherry sitting inside near the rim, with a short slender cherry stem. No ice or foam. Different silhouette from a V-shaped martini glass. Warm garnet liquid with gentle light passing through it, not luminous magic.
```

### GinFizz

```text
Use case: stylized-concept. Create ONE new isolated cocktail inventory illustration for SURLHWA. Input images 1 (Seolya) and 2 (Manwol) are style references only, not edit targets. Match their quiet winter-bar illustration: drawn glass contours, warm cream highlights, muted silver-blue glass reflections, fine painterly texture and restrained glow. Consistent with the existing sprites, not photography, smooth 3D or flat vector. Slightly elevated frontal view with visible rim ellipse, entire glass and foot in frame, centered square canvas, object about 76% of canvas height. Truly transparent RGBA background outside the object, comfortable empty margins, no backdrop, vignette, tabletop, cast shadow, snowflakes, text, labels, logos or unrelated props. Legible at small Guide-card size. Single PNG asset. Subject: GIN FIZZ, a tall straight-sided tumbler containing pale cloudy lemon-colored cocktail with small fine rising carbonation bubbles, a modest thin white bubbly surface cap (not whipped cream), and one small yellow lemon slice at the rim. No ice cubes, straw or spoon. Thick clear glass base, restrained fresh silver-yellow palette.
```

### HankyPanky

```text
Use case: stylized-concept. Create ONE new isolated cocktail inventory illustration for SURLHWA. Input images 1 (Seolya) and 2 (Manwol) are style references only, not edit targets. Match their quiet winter-bar illustration: drawn glass contours, warm cream highlights, muted silver-blue glass reflections, fine painterly texture and restrained glow. Consistent with the existing sprites, not photography, smooth 3D or flat vector. Slightly elevated frontal view with visible rim ellipse, entire glass and foot in frame, centered square canvas, object about 76% of canvas height. Truly transparent RGBA background outside the object, comfortable empty margins, no backdrop, vignette, tabletop, cast shadow, snowflakes, text, labels, logos or unrelated props. Legible at small Guide-card size. Single PNG asset. Subject: HANKY PANKY, an elegant stemmed shallow rounded cocktail coupe containing clear rich dark amber-brown liquid with ruby undertones, and a small curled strip of orange zest resting at the rim. No ice, no foam, no fruit slice, no cherry. Restrained warm orange highlight on a contemplative dark drink.
```

### French75

```text
Use case: stylized-concept. Create ONE new isolated cocktail inventory illustration for SURLHWA. Input images 1 (Seolya) and 2 (Manwol) are style references only, not edit targets. Match their quiet winter-bar illustration: drawn glass contours, warm cream highlights, muted silver-blue glass reflections, fine painterly texture and restrained glow. Consistent with the existing sprites, not photography, smooth 3D or flat vector. Slightly elevated frontal view with visible rim ellipse, entire glass and foot in frame, centered square canvas, object about 76% of canvas height. Truly transparent RGBA background outside the object, comfortable empty margins, no backdrop, vignette, tabletop, cast shadow, snowflakes, text, labels, logos or unrelated props. Legible at small Guide-card size. Single PNG asset. Subject: FRENCH 75, an elegant tall narrow champagne flute with a long fine stem, holding transparent pale golden liquid with delicate tiny rising champagne bubbles. No garnish, no ice, no foam cap, no lemon or fruit. Fine sparkling bubbles and the slender flute silhouette define this drink.
```

### LastWord

```text
Use case: stylized-concept. Create ONE new isolated cocktail inventory illustration for SURLHWA. Input images 1 (Seolya) and 2 (Manwol) are style references only, not edit targets. Match their quiet winter-bar illustration: drawn glass contours, warm cream highlights, muted silver-blue glass reflections, fine painterly texture and restrained glow. Consistent with the existing sprites, not photography, smooth 3D or flat vector. Slightly elevated frontal view with visible rim ellipse, entire glass and foot in frame, centered square canvas, object about 76% of canvas height. Truly transparent RGBA background outside the object, comfortable empty margins, no backdrop, vignette, tabletop, cast shadow, snowflakes, text, labels, logos or unrelated props. Legible at small Guide-card size. Single PNG asset. Subject: LAST WORD, a stemmed softly rounded cocktail coupe containing translucent muted pale chartreuse-green liquid, subtly cloudy with soft golden-green highlights. No garnish, no ice, no foam, no fruit or herbs. A restrained herbaceous green drink, not neon or magic.
```
