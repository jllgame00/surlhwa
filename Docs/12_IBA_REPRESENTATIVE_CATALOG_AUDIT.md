# SURLHWA IBA REPRESENTATIVE CATALOG AUDIT

Last Updated: 2026-09-07

Status: I9A COMPLETE — research/design/capability audit. I9B catalog and
preparation-data implementation is pending.

---

## 1. Purpose, Scope, and Labels

This document selects the first official-IBA representative catalog for
Bartending 2.0 validation. It is not RecipeData/IngredientData authoring, a
matcher change, an implementation authorization, or a production catalog
migration. Prototype recipe assets remain prototype/tutorial evidence only.

| Label | Meaning |
| --- | --- |
| **OFFICIAL IBA FACT** | Current text from the linked official IBA cocktail page; it is not silently simplified. |
| **CURRENT SURLHWA CAPABILITY** | Verified runtime/data limitation at HEAD `9b0b13f`. |
| **RECOMMENDATION** | Proposed selection or future I9B direction, not implementation evidence. |
| **UNRESOLVED DECISION** | A product choice required before I9B work begins. |

---

## 2. Official Source Policy

### 2.1 Authority

**OFFICIAL IBA FACT:** The only external recipe authority used here is the
[IBA Official Cocktail List](https://iba-world.com/cocktails/) and the linked
individual official IBA recipe pages. The list exposes the current official
categories: *The Unforgettables*, *Contemporary Classics*, and *New Era*.

No blog, video, recipe database, Wikipedia page, or other secondary source was
used to replace an official ingredient, amount, method, glass, ice, garnish, or
optional requirement.

### 2.2 Access record and interpretation rule

- Access date: **2026-09-07**.
- Every individual source URL appears in Section 4.
- I9B must revisit each official page before authoring its assets because IBA
  specifications may change.
- `Few Drops`, `Splash`, `green olives if requested`, and `lemon or orange zest
  with cherry` remain official wording in this audit. They are not silently
  converted to a Dash, a fixed ml value, one garnish, or a generic step.

---

## 3. Current SURLHWA Baseline

### 3.1 Implemented runtime vocabulary

**CURRENT SURLHWA CAPABILITY:**

| Concern | Current boundary |
| --- | --- |
| Liquid truth | `BartendingContainerState` records continuous canonical ml per ingredient. `BartendingVolume` is the single ml/oz bridge to the legacy matcher. |
| Amounts | Arbitrary continuous Pour; retained discrete Dash count plus project compatibility 1 ml/Dash; manual TopUp target-final-volume guidance. |
| Containers | Separate runtime Serving Glass, Mixing Glass, and Shaker; each owns liquid entries and bounded `HasIce`. |
| Movement | Full Transfer preserves ingredient ml and Dash counts. Strain moves liquid to Serving Glass and retains source ice. |
| Methods | Build direct preparation; Mixing-Glass spoon Stir; Shaker rhythm Shake; ordered Float layers with per-boundary Clean/Partial/Mixed state. |
| Garnish | One optional non-liquid Serving-Glass garnish: Lemon Twist, Cherry, or Olive at Rim, InGlass, or Surface. |

### 3.2 Data-authoring reality

**CURRENT SURLHWA CAPABILITY:** `CocktailRecipeData` currently owns only a
recipe ID, one `MiniGameType` method, target cocktail, an ingredient array with
ounce-authored `ratio` values, discovery threshold, and discoverability.
`BrewComposition` is a liquid-only ounce snapshot. The unchanged
`RecipeMatchService` does not evaluate order, container, ice, glassware, Dash
count, TopUp, Float quality/order, technique quality, or garnish.

| Authoring need | Gameplay capability | Current data support | I9B implication |
| --- | --- | --- | --- |
| Canonical ml ingredient amount | Yes | Existing recipe ratio is oz only | Add a narrow canonical-ml instruction/amount seam while retaining the current matcher bridge. |
| Dash / TopUp | Yes | No expectation or preparation field | Add only selected-recipe requirements. |
| Container, Ice, Strain, Float order | Yes | One method enum cannot express a multi-action method | Add a bounded ordered preparation representation, not a generic script language. |
| Garnish expectation | Actual state exists | No expected garnish/alternative/multiple field | Decide selected garnish scope first. |
| Glassware | Generic Serving Glass runtime state exists | No authored glass metadata | Initially content/presentation metadata only. |

**RECOMMENDATION:** I9B should add the smallest recipe-owned typed preparation
step extension proven by the selected recipes. It may describe repeated `Pour`,
`AddIce`, `Stir`, `Shake`, `Strain`, `TopUp`, `Float`, and `Garnish` actions; it
must not create a universal bartending scripting engine or a second matcher.

---

## 4. Official Candidate Pool

The pool contains the eight mandatory audit candidates and three comparisons for
ingredient/measurement coverage. Each row preserves the official page as
accessed on 2026-09-07.

| Cocktail / category | OFFICIAL IBA FACT — ingredients | OFFICIAL IBA FACT — method, glass, ice, garnish | Source |
| --- | --- | --- | --- |
| Dry Martini — The Unforgettables | 60 ml Gin; 10 ml Dry Vermouth | Mixing Glass with ice cubes; stir well; strain into chilled martini cocktail glass. Squeeze oil from lemon peel onto drink, or garnish with green olives if requested. | [IBA](https://iba-world.com/iba-cocktail/dry-martini/) |
| Manhattan — The Unforgettables | 50 ml Rye Whiskey; 20 ml Sweet Red Vermouth; 1 dash Angostura Bitters | Mixing Glass with ice cubes; stir well; strain into chilled cocktail glass. Cocktail cherry. | [IBA](https://iba-world.com/iba-cocktail/manhattan/) |
| Negroni — The Unforgettables | 30 ml Gin; 30 ml Bitter Campari; 30 ml Sweet Red Vermouth | Pour all ingredients directly into chilled old fashioned glass filled with ice; stir gently. Half orange slice. | [IBA](https://iba-world.com/iba-cocktail/negroni/) |
| Last Word — The Unforgettables | 22.5 ml Gin; 22.5 ml Green Chartreuse; 22.5 ml Maraschino Luxardo; 22.5 ml Fresh Lime Juice | Add to shaker; shake with ice; strain into chilled cocktail glass. Garnish N/A. | [IBA](https://iba-world.com/iba-cocktail/last-word/) |
| Gin Fizz — The Unforgettables | 45 ml Gin; 30 ml Fresh Lemon Juice; 10 ml Simple Syrup; Splash of Soda Water | Shake all except soda with ice. **Pour** into thin tall Tumbler; top with a splash soda. Serve without ice. Lemon slice, optional lemon zest. | [IBA](https://iba-world.com/iba-cocktail/gin-fizz/) |
| French 75 — Contemporary Classics | 30 ml Gin; 15 ml Fresh Lemon Juice; 15 ml Sugar Syrup; 60 ml Champagne | Shake all except Champagne; strain into Champagne flute; top up with Champagne; stir gently. Garnish N/A. | [IBA](https://iba-world.com/iba-cocktail/french-75/) |
| New York Sour — New Era | 60 ml Rye Whiskey or Bourbon; 22.5 ml Simple Syrup; 30 ml Fresh Lemon Juice; **Few Drops of Egg White**; 15 ml Red wine (Shiraz or Malbech) | Pour all ingredients into shaker; shake vigorously with ice; strain into chilled rocks glass filled with ice; float wine on top. Lemon or orange zest with cherry. | [IBA](https://iba-world.com/iba-cocktail/new-york-sour/) |
| Bramble — New Era | 50 ml Gin; 25 ml Fresh Lemon Juice; 12.5 ml Sugar Syrup; 15 ml Crème de Mûre | Shake all except Crème de Mûre; strain into chilled old fashioned glass with **crushed ice**; pour Crème de Mûre over top in circular motion. Optional lemon slice and blackberries. | [IBA](https://iba-world.com/iba-cocktail/bramble/) |
| Americano — The Unforgettables | 30 ml Bitter Campari; 30 ml Sweet Red Vermouth; splash Soda Water | Mix ingredients directly in old fashioned glass filled with ice cubes; add splash Soda Water; stir gently. Half orange slice and lemon zest. | [IBA](https://iba-world.com/iba-cocktail/americano/) |
| Cardinale — Contemporary Classics | 40 ml Gin; 20 ml Dry Vermouth; 10 ml Bitter Campari | Mixing Glass with ice cubes; stir well; strain into chilled cocktail glass. Lemon zest. | [IBA](https://iba-world.com/iba-cocktail/cardinale/) |
| Hanky Panky — The Unforgettables | 45 ml London Dry Gin; 45 ml Sweet Red Vermouth; 7.5 ml Fernet | Mixing Glass with ice cubes; stir well; strain into chilled cocktail glass. Orange zest. | [IBA](https://iba-world.com/iba-cocktail/hanky-panky/) |

### 4.1 Candidate capability audit

| Candidate | CURRENT SURLHWA CAPABILITY — supported now | Explicit gap / data gap |
| --- | --- | --- |
| Dry Martini | Mixing Glass, ice, direct Stir, Strain, 60/10 ml, Olive or Lemon Twist proof identity | Martini-glass metadata; official alternative garnish/effect; lemon-peel oil expression; expected preparation data. |
| Manhattan | Mixing Glass, ice, direct Stir, Strain, 50/20 ml, Dash, Cherry | Exact Rye/Sweet Red/Angostura content identities; expected Dash/garnish/sequence data. |
| Negroni | Direct Build, Serving-Glass ice, 30 ml | Current direct spoon technique is Mixing-Glass-only; no actual in-glass `stir gently`; Orange Slice identity; expected sequence/glass data. |
| Last Word | 22.5 ml, Shaker, ice, Shake, Strain, no garnish | New ingredient identities and glass metadata; no new preparation mechanic. |
| Gin Fizz | 45/30/10 ml, Shaker ice, Shake, manual TopUp, no serving ice | Official `Splash` is not a fixed official ml/final volume; IBA says **Pour**, not Strain; Lemon Slice/optional Zest; glass/sequence data. |
| French 75 | Shaker, ice, Shake, Strain, manual TopUp, listed ml values | No Serving-Glass `stir gently` operation after TopUp; flute metadata; ordered data. |
| New York Sour | 60/22.5/30/15 ml, Shaker/Serving ice, Shake, Strain, final Float, Cherry/Lemon Twist proof identities | Few Drops is not Dash; egg-white semantics; whiskey alternative; the official page says both “all ingredients” in shaker and later to float wine; multi/alternate garnish. |
| Bramble | Shaker, Shake, Strain, Serving Glass, upper controlled Float-style addition | Crushed ice, circular top pour, Lemon Slice + Blackberries multi-garnish. Beyond first-set gap budget. |
| Americano | Build, Serving ice, manual TopUp, 30 ml | Splash policy, no in-glass gentle Stir, two simultaneous garnishes. Useful reserve but redundant. |
| Cardinale | Mixing Glass, ice, direct Stir, Strain, 40/20/10 ml | Lemon Zest identity; otherwise duplicates selected Stir coverage. |
| Hanky Panky | Mixing Glass, ice, direct Stir, Strain, 45/45/**7.5** ml | Fernet/Orange Zest identities and expected preparation data. |

---

## 5. Recommended Eight-Cocktail Set

**RECOMMENDATION:** adopt exactly these eight official recipes for I9B, subject
to Section 11 decisions:

1. Dry Martini
2. Manhattan
3. Negroni
4. Last Word
5. Gin Fizz
6. French 75
7. New York Sour
8. Hanky Panky

| Cocktail | Selection reason / mechanic coverage | Shared learning | Complexity |
| --- | --- | --- | --- |
| Dry Martini | Minimal canonical Stir/Strain baseline; reveals garnish/expressed-oil fidelity honestly. | Gin anchors six selected recipes. | MEDIUM |
| Manhattan | Clearest official Dash proof. | Whiskey family returns in New York Sour; Sweet Red Vermouth returns twice. | MEDIUM |
| Negroni | Direct Serving-Glass Build with ice. | Gin and Sweet Red Vermouth reuse; Campari opens a later branch. | MEDIUM |
| Last Word | Four equal 22.5 ml pours validate non-increment measure. | Gin and future Maraschino learning. | MEDIUM |
| Gin Fizz | Shake plus no serving ice and soda Splash/TopUp. | Gin/lemon/syrup recur in French 75. | MEDIUM |
| French 75 | Exact 15/15/30/60 ml after Shake; meaningful TopUp and post-TopUp operation. | Reinforces Gin Fizz instead of isolated bottles. | MEDIUM |
| New York Sour | Final Float after Shake/Strain; intentionally exposes one bounded small-amount and garnish decision. | Lemon/syrup recur; whiskey-family connection to Manhattan. | HIGH |
| Hanky Panky | Compact official 7.5 ml Stir proof. | Gin + Sweet Red Vermouth connect across three selected cocktails. | MEDIUM |

### 5.1 Deferred comparisons

| Candidate | Recommendation | Reason |
| --- | --- | --- |
| Americano | Reserve for first expansion | Strong Campari/Vermouth reuse, but duplicates selected Splash and in-glass-Stir gaps. |
| Cardinale | Reserve | Good 10 ml recipe but duplicates Dry Martini's route. |
| Bramble | Defer | A useful upper treatment but adds crushed ice and multi-garnish while New York Sour already proves Float. |

---

## 6. Mechanic Coverage Matrix

Legend: **Y** runtime action exists; **△** current action exists but exact
official route/semantic needs I9B work or decision; **—** not required.

| Cocktail | Build | Mixing Stir | Shake | Dash | TopUp/Splash | Ice | Strain | Float | Garnish | Key exact gap |
| --- | ---: | ---: | ---: | ---: | ---: | --- | ---: | ---: | ---: | --- |
| Dry Martini | — | Y | — | — | — | Mixing Glass | Y | — | △ | Oil/alternative garnish; glass. |
| Manhattan | — | Y | — | Y | — | Mixing Glass | Y | — | Y | Exact content identities and Dash instruction. |
| Negroni | Y | △ | — | — | — | Serving Glass | — | — | △ | In-glass gentle Stir, Orange Slice. |
| Last Word | — | — | Y | — | — | Shaker | Y | — | — | Content/glass metadata only. |
| Gin Fizz | — | — | Y | — | △ | Shaker; no serving ice | △ | — | △ | Splash, official Pour, Lemon Slice/Zest. |
| French 75 | — | △ | Y | — | Y | Shaker | Y | — | — | Gentle Stir after TopUp; flute. |
| New York Sour | — | — | Y | — | — | Shaker + Serving | Y | Y | △ | Few Drops, multi/alternate garnish, composite route. |
| Hanky Panky | — | Y | — | — | — | Mixing Glass | Y | — | △ | Orange Zest. |

The set collectively proves direct Build, Mixing-Glass Stir, Shaker Shake,
Dash, manual TopUp, source/serving ice, Strain, final Float, Garnish, and
7.5/10/15/20/22.5/30/45/50/60 ml quantities. It does not justify Muddle,
Blend, dry/reverse Dry Shake, double strain, rim coating, or realistic ice/fluid
simulation.

---

## 7. Ingredient Reuse Matrix

This is a planning matrix, not an instruction to create assets. Official names
are retained; generic prototype `Whiskey`/`Vermouth` assets are not permission
to collapse official variants silently.

| Official ingredient / family | Dry | Manhattan | Negroni | Last Word | Gin Fizz | French 75 | NY Sour | Hanky Panky |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Gin / London Dry Gin | Y |  | Y | Y | Y | Y |  | Y |
| Dry Vermouth | Y |  |  |  |  |  |  |  |
| Sweet Red Vermouth |  | Y | Y |  |  |  |  | Y |
| Rye Whiskey or Bourbon family |  | Y (Rye) |  |  |  |  | Y (Rye or Bourbon) |  |
| Bitter Campari |  |  | Y |  |  |  |  |  |
| Angostura Bitters |  | Y |  |  |  |  |  |  |
| Green Chartreuse |  |  |  | Y |  |  |  |  |
| Maraschino Luxardo |  |  |  | Y |  |  |  |  |
| Fresh Lime Juice |  |  |  | Y |  |  |  |  |
| Fresh Lemon Juice |  |  |  |  | Y | Y | Y |  |
| Simple / Sugar Syrup |  |  |  |  | Y | Y | Y |  |
| Soda Water |  |  |  |  | Y |  |  |  |
| Champagne |  |  |  |  |  | Y |  |  |
| Egg White |  |  |  |  |  |  | Y |  |
| Red Wine |  |  |  |  |  |  | Y |  |
| Fernet |  |  |  |  |  |  |  | Y |

```text
Gin
├─ Dry Martini       (stirred / dry)
├─ Negroni           (direct bitter build)
├─ Last Word         (equal-part shaken)
├─ Gin Fizz          (shaken citrus + soda)
├─ French 75         (shaken citrus + Champagne TopUp)
└─ Hanky Panky       (stirred herbaceous modifier)

Sweet Red Vermouth
├─ Manhattan         (whiskey + Dash)
├─ Negroni           (bitter build)
└─ Hanky Panky       (gin + Fernet)
```

---

## 8. Measurement Audit

| ml | Official candidate(s) | Selected? | Current result |
| ---: | --- | --- | --- |
| 7.5 | Hanky Panky | Y | Continuous canonical ml supports it. |
| 10 | Dry Martini; Gin Fizz; Cardinale | Y | Supported. |
| 12.5 | Bramble | No | Supported but Bramble is deferred for crushed ice. |
| 15 | French 75; New York Sour; Bramble | Y | Supported. |
| 20 | Manhattan; Cardinale | Y | Supported. |
| 22.5 | Last Word; New York Sour | Y | Core selected proof. |
| 25 | Bramble | No | Supported. |
| 30 | Negroni; Gin Fizz; French 75; New York Sour; Americano | Y | Supported. |
| 40 | Cardinale | No | Supported. |
| 45 | Gin Fizz; Hanky Panky | Y | Supported. |
| 50 | Manhattan; Bramble | Y | Supported. |
| 60 | Dry Martini; French 75; New York Sour | Y | Supported. |

| Non-ml official expression | Candidate | Current representability | Finding |
| --- | --- | --- | --- |
| 1 dash | Manhattan | Discrete Dash exists | Needs recipe-authored Dash count; current 1 ml bridge is project compatibility, not an IBA conversion. |
| Few Drops | New York Sour | No Drop semantic | **UNRESOLVED DECISION**; never convert silently to Dash. |
| Splash | Gin Fizz | TopUp is target-final-volume guidance | **UNRESOLVED DECISION**; IBA supplies no fixed official ml. |
| Top up, 60 ml Champagne listed | French 75 | Manual TopUp exists | Needs authored instruction and post-TopUp gentle Stir. |

---

## 9. Glassware, Ice, and Garnish Audit

### 9.1 Glassware

| Official glass | Selected cocktails | Finding |
| --- | --- | --- |
| Martini cocktail glass | Dry Martini | No type selection/data today. |
| Chilled cocktail glass | Manhattan, Last Word, Hanky Panky | No type selection/data today. |
| Old fashioned / rocks | Negroni, New York Sour | Generic Serving Glass can hold the actual state. |
| Thin tall Tumbler | Gin Fizz | No type selection/data today. |
| Champagne flute | French 75 | No type selection/data today. |

**RECOMMENDATION:** author official glassware as presentation/content metadata
in I9B; do not create glass-selection gameplay yet.

### 9.2 Ice

| Official requirement | Current representation |
| --- | --- |
| Mixing Glass ice cubes: Dry Martini, Manhattan, Hanky Panky | Direct `HasIce`. |
| Shaker ice: Last Word, Gin Fizz, French 75, New York Sour | Direct `HasIce`. |
| Serving Glass ice: Negroni, New York Sour | Direct `HasIce`. |
| Gin Fizz served without ice | Leave Serving Glass ice absent. |
| Bramble crushed ice | Not represented; deferred with Bramble. |

### 9.3 Garnish

| Official requirement | Selected cocktails | I8 status |
| --- | --- | --- |
| Cocktail cherry | Manhattan | Direct current identity. |
| Green olives if requested | Dry Martini | Olive exists, but singular/no alternative-rule model. |
| Lemon peel oil / twist | Dry Martini | Lemon Twist exists; oil expression is distinct. |
| Half orange slice | Negroni | New identity. |
| Lemon slice, optional lemon zest | Gin Fizz | New identity and optionality. |
| Lemon or orange zest with cherry | New York Sour | Needs Orange Zest and multi/alternate representation. |
| Orange zest | Hanky Panky | New identity. |

---

## 10. Concrete Capability Gaps

| Gap | Official evidence | Affected selected cocktail(s) | Smallest I9B seam |
| --- | --- | --- | --- |
| Ordered preparation authoring | Every official method is ordered; current recipe has one method label. | All | Narrow recipe-owned typed steps, not a generic script engine. |
| Serving-Glass gentle Stir | IBA requires it after Build/TopUp. | Negroni, French 75 | Bounded actual serving-glass Stir policy distinct from I5's Mixing-Glass technique result. |
| Drop | `Few Drops of Egg White`. | New York Sour | Explicit adopted small discrete amount semantic, or defer recipe. |
| Splash | `Splash of Soda Water`. | Gin Fizz | Content/display policy compatible with manual actual Pour. |
| Composite Shake → Float | Shake, Strain over ice, final wine Float. | New York Sour | Compose existing state/actions through ordered steps; no second composition state. |
| Multi/alternate garnish | IBA lists alternatives and zest with cherry. | Dry Martini, Gin Fizz, New York Sour | Decide smallest adopted garnish representation. |
| Exact ingredient variants | Official variants differ from generic prototypes. | All | Content identity policy; no silent asset reuse. |
| Gin Fizz `Pour` wording | Official says pour after Shake, current Shake presents Strain. | Gin Fizz | Decide whether existing liquid-only Transfer can be exposed truthfully. |
| New York Sour wine order wording | Official page says pour all ingredients into the shaker, then later says float wine on top. | New York Sour | Record a content-owner interpretation; do not silently erase either official instruction. |

---

## 11. Owner Decisions Required Before I9B

### D1 — Few Drops of Egg White

- **Why:** New York Sour specifies `Few Drops of Egg White`.
- **Choices:** adopt a bounded `Drop` semantic; defer New York Sour; or select a
  different official Float candidate after a new audit.
- **Recommendation:** adopt `Drop` only if New York Sour remains selected. It
  must stay distinct from Dash and use a project-owned value.

### D2 — Splash / TopUp policy

- **Why:** Gin Fizz says `Splash`; French 75 says TopUp and lists 60 ml.
- **Choices:** project-calibrated target-final-volume guidance; a bounded
  `Splash` instruction with actual ml still authoritative; or defer Gin Fizz.
- **Recommendation:** choose an explicit recipe-display/instruction policy;
  never invent an IBA fixed ml splash.

### D3 — Serving-Glass gentle Stir

- **Why:** Negroni and French 75 require it.
- **Choices:** a bounded actual non-scored Serving-Glass operation; presentation
  only; or defer those recipes.
- **Recommendation:** prefer the narrow actual operation while preserving I5
  Mixing-Glass Stir quality unchanged.

### D4 — First-set garnish fidelity

- **Why:** Orange Slice, Lemon Slice/Zest, Orange Zest, official alternatives,
  and New York Sour's zest plus cherry exceed I8's one-garnish proof.
- **Choices:** add only adopted identities and bounded multi/alternate state;
  document one IBA-listed alternative (Dry Martini olives) as content choice; or
  defer affected recipes.
- **Recommendation:** use Dry Martini's official olive alternative if retaining
  one garnish; add the smallest multi-garnish state only if New York Sour stays.
  Citrus-oil expression is not a placed garnish without a separate decision.

### D5 — Exact ingredient variants

- **Why:** official Rye/Bourbon, Dry/Sweet Red Vermouth, and named liqueurs must
  not silently become generic prototype ingredients.
- **Choices:** exact official identities; documented family substitutions; or a
  revised set.
- **Recommendation:** exact identity where it affects learning/matching.

### D6 — Glassware scope

- **Why:** every selected recipe specifies glassware but current runtime has one
  Serving Glass identity.
- **Recommendation:** recipe/presentation metadata only; defer glass-selection
  gameplay until playtesting proves value.

### D7 — Gin Fizz post-Shake pour

- **Why:** IBA says `Pour into thin tall Tumbler`; active Shake presents Strain.
- **Choices:** expose current liquid-only Transfer as a neutral operation; add a
  bounded pour-off action; or defer Gin Fizz.
- **Recommendation:** reuse Transfer only if its presentation truthfully differs
  from Strain; do not relabel official wording without a decision.

### D8 — New York Sour wine-order wording

- **Why:** the official page says to pour all ingredients into the shaker, then
  explicitly says to float the wine on top after Strain.
- **Choices:** follow the later explicit Float instruction and record that
  interpretation; defer New York Sour pending IBA clarification; or replace the
  candidate after a new official audit.
- **Recommendation:** if retained, author the wine as the final Float step
  because that is the page's explicit final operation, while keeping the source
  ambiguity visible in the recipe-source record.

---

## 12. I9B Proposed Scope (Pending Decisions)

If D1–D8 are adopted, I9B should only:

1. Add the smallest recipe-owned preparation expectation representation needed
   by these eight official recipes.
2. Add only their needed IngredientData, CocktailData, RecipeData, catalog,
   garnish identity, and glassware metadata assets.
3. Reuse existing Pour Core, containers, techniques, Float, Garnish, and the
   `BrewComposition` matcher bridge.
4. Add only selected decision-approved amount/operation support.
5. Validate actual player preparation against each adopted specification.

It must not mass-migrate the catalog, replace `RecipeMatchService`, implement
ingredient progression, redesign Guide/discovery, or introduce an extensible
preparation language.

---

## 13. I9A Verdict

**PASS.** The recommended eight form a coherent official-IBA representative
set: shared ingredients support learning, the already-built Bartending 2.0
actions receive broad coverage, and every fidelity limitation is explicit. The
architecture needs no wholesale rewrite; I9B requires adopted decisions and a
narrow preparation-data seam before any content assets are authored.

This audit made no runtime, serialized, persistence, matcher, catalog, scene,
prefab, or project-setting changes.
