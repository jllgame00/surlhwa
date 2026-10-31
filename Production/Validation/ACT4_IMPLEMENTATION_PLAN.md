# ACT 4 implementation plan

Execution: continue the approved inline production workflow, with independent
domain, content and continuation tasks delegated under the parallel-agents skill.
No owner gate. Baseline `4790c33`; preserve owner-local `review-slice.html` and
the inherited material stat/line-ending noise (no semantic material diffs).

Goal: a playable Hamlet ACT, including fair French 75 discovery, both visits,
ordinary pacing, natural departure memory, save/Continue and representative Q1.
Architecture: extend existing recipe, session, content and save authorities.
Tech: Unity 6000.3.7f1, C#, strict source-tracked UTF-8 JSON and current schema-1
campaign save envelope. No new persistence format or in-place conversion.

Authority: Project Vision; Final Canon 54, 40/41, 10 MEM-04, 61, 20/21/22,
31 DRINK-08 and 33; approved Architecture 03-05/11/20/21; Production Expansion
Plan, Q1 and presentation debt. Historical RC1 is reference/source evidence only.

## Fixed content and constraints

Append four rows to the accepted 29-slot prefix, retaining the documented
Production night offset of +2:

| Slot | Source | Production night | Purpose |
| --- | --- | --- | --- |
| 29 | RC1-30 | 12 | Station worker, required French 75 discovery |
| 30 | RC1-31 | 12 | Hamlet question, Clarity / Bitterness |
| 31 | RC1-32 | 13 | Repairer notices windmill, Clarity / Warmth |
| 32 | RC1-33 | 13 | Hamlet stillness, Clarity / Warmth; natural MEM-04 departure |

Use `through_act_04`, 33 slots and `campaign_through_act4_v1`. Freeze accepted
act3.json hash `9db4998cd4f3dd8fc1c0371e8396504c07c0cce253e504caf39f48ec370555f0`.
Keep all earlier IDs, definitions, authored wording, knowledge/supply and saves.
No global input/time freeze, manufactured Faust prerequisite, idle trigger,
future revisit, economy or alternate ending.

French 75: `recipe_french_75` / `cocktail_french_75`; gin30ml,
fresh_lemon_juice15ml, simple_syrup15ml, champagne60ml; Awakening; flute metadata;
no garnish. Root reverified [official IBA](https://iba-world.com/iba-cocktail/french-75/)
on2026-10-31. Nominal ml replace the source's oz-rounding artifacts; document this
adoption. Canon's game expression uses Simple Syrup and a measured Champagne Pour.
New physical supply is fresh_lemon_juice and champagne before encounter29.
Neither supply nor entering the ACT grants Known. Fresh lemon stays distinct
from accepted lemon_juice. Glass category stays metadata, not an invented glass
selection grade (Canon33 SUPPLY-005).

## Task 1: recipe-owned Shake finish and actual provenance

Files: Domain/CocktailCatalog.cs, Brewing.cs, ShakePreparation.cs;
new Tests/EditMode/French75PreparationTests.cs. Keep existing save snapshot fields.

Interface: append optional constructor parameter
`ShakeFinishPolicy shakeFinish=ShakeFinishPolicy.PourThenSplash` to StirRecipe;
read-only `ShakeFinish`. Enum values are `PourThenSplash` and
`StrainThenPourAndGentleStir`. Invalid combinations fail at content construction;
the second policy requires method shake and an actual LateIngredient.
Do not derive policy from customer intent, method tab or a cocktail ID check.

- [x] Add meaningful failing cases for shaken/strained base followed by direct
  measured Pour or Jigger Champagne transfer and fresh GentleStir; actual WHAT,
  HOW, QUALITY and FIT remain separate. Include conservation and partial transfer.
- [x] Permit actual Shaker-to-Serving Strain in Transfer and BeginTransfer, retaining
  closed-lid and other transfer guards. No control change to ACT1's existing R.
- [x] Preserve the legitimate shaken portion of this actual strained batch through
  measured late additions. Do not label unshaken additions as shaken, combine
  incompatible batches, or silently change accepted Gin Fizz outcomes. Derive any
  distinction from actual transfer provenance, never expected recipe metadata.
- [x] Validate ordered actual Shaker Strain, late Champagne Pour/accepted Jigger
  components and GentleStir after the final addition. Final snapshot.Strained
  can legitimately clear on late Pour; do not mistake it for the full trace.
- [x] Reject early/shaken Champagne, wrong transfer, Splash substituted for Pour,
  missing/stale GentleStir, extra unshaken base, incompatible batches and stale
  evidence after discard. Retain the Gin Fizz PourFromContainer + late Splash
  contract and existing tuning. Run focused RED/GREEN then domain regressions.

## Task 2: source adoption, fair learning and retry reading

Files: new Tools/adopt_act4_content.py and test_act4_adoption.py;
Content/Campaign/act4-adoption.json and act4.json; ContentIO/CampaignContentLoader.cs;
Domain/CampaignContent.cs; Application/CampaignSession.cs; new ActFourContentTests.
Source set: SRC-RC1-0030..0039, 0082..0103, 0191..0198 (40 records), exact fields,
roles, order and hashes. Source retry0039 must have an explicit runtime use.

Loader maps exact preparation array
`shaker_ice, closed_shake, strain_from_shaker, late_measured_pour, gentle_stir,
no_serving_ice` to the new policy. Existing recipe arrays retain their policies.
Permit flute metadata. Add only approved Hamlet visual/guest and MEM04 owner.

- [x] Adopt exact source blocks and preserve the 29-slot prefix. Document a Known
  French fallback using the accepted station-worker ordinary request, without
  changing that earlier definition or fabricating knowledge.
- [x] French facts are consumed with0035(identity),0036(amounts/base/method),
 0037(Champagne/preparation/garnish). Failure preserves facts and the same encounter;
  successful actual recipe/preparation/quality grants Known via existing authority.
- [x] Add optional `DiscoveryRetryTextId` / JSON `discoveryRetryTextId` to an
  encounter (default null), validated against its source. Project it from
  CampaignSession.CurrentText in Retry and consume it when advancing to Brewing.
  Older encounters retain current behavior. No new save field or phase.
- [x] Award `mem_04` with a bounded stage/skull/shadow variant at slot32's natural
  departure regardless of FIT. Prove all33 slots, ordinary pacing, fair failure
  retry, genuine recommendation alternatives and no earlier content regression.

## Task 3: continuation and publication

Files: ContentIO/CampaignContinuation.cs and the existing progress validator;
new ActFourContinuationTests. Keep `new CampaignContinuation(sourceJson,targetJson)`.

- [x] Pin accepted ACT3 bytes and preserve ACT1/2/3 source prefixes. Convert completed
  ACT3 to slot29 NightEnd in a separate destination; existing destination history
  wins even if invalid. Never overwrite original current/previous generations.
- [x] Validate new optional Retry reading receipts with the same existing codec.
  Old saves and receipts remain unchanged; converter compatibility ignores only
  absent optional null fields, never changed authored values.
- [x] Test every source phase, pending results/departures, publication faults,
  idempotence, corruption/future rejection, required-discovery retry and completion.

## Task 4: runtime, Q1 and bounded controls

Files: CampaignController, CampaignUi, SliceInputAdapter, CampaignMemoryView,
Q1ProductionView, new ActFourExpansionBuilder and Bar_Q1_Act4; ACT4 asset bindings
and provenance. Root owns integration; cast work can be delegated separately.

- [x] Add explicit Hamlet actor and accepted source history3/2/1; use existing
  scope-based namespace and ActAt projection. Separate content/font/build assets;
  preserve all accepted scenes and the Prince's empty chair. Inspect source seated
  contact independently. Missing art never owns logical progress.
- [x] Add an ACT4-available Shaker Strain command/control and GentleStir in the
  Shake panel; preserve accepted ACT1 controls. Disclosure follows recipe facts.
  Gin Fizz and French preparation instructions must not share a false universal
  finish. Actual service cup remains the single existing liquid subtree.
- [x] New supply exceeds the17-item shelf. Add bounded physical-supply paging only
  when required, with15 rows per page, visible page navigation and accessible
  discard. Preserve earlier17-item layout and hotkeys. Never filter by knowledge
  or request. Prove every supplied ingredient is selectable at720/1080, including
  all second-page items, without clipping or leaking unknown recipe names.
- [x] Add MEM04 stage fragment/skull/restrained shadow in a supported Q1 location,
  clear of three earlier memories. No permanent room-wide darkness or input lock.
  Show source retry feedback in normal reading with no extra progression authority.

## Task 5: actual Player, evidence and local checkpoint

Extend existing proof tools explicitly for ACT4, preserving previous fixtures.
Use actual isolated ACT3 completion generations from finalBuild03. Add successful
French discovery plus a missing-finish failure/retry, Hamlet FIT mismatch with
natural memory, mid-retry/farewell reload and completed Continue. Simulated input
must use actual controls/pages and real liquid, with source/Player inventories
frozen. Capture representative720/1080 cast, flute metadata disclosure, physical
shelf pages, memory and transitions. Native input remains a separately named tier.

- [x] Compile, content/source checks, meaningful domain and affected Editor tests.
- [x] Build and run actual Player service/checkpoint/reload paths; inspect normal
  renders, missing references, contact and occlusion. No fabricated runtime saves.
- [x] Inspect all serialized diffs; reject unrelated importer/generated scene churn.
  Record evidence and residual debt, commit complete bounded work locally, continue
  ACT5 automatically. Do not push or claim final presentation approval.

Review focus: measured late transfers must retain only earned technique evidence;
failed discovery must be readable and repeatable; old save history must survive;
new shelf items must be physically reachable; accumulating memories must remain
visible without occupying the Prince's empty chair.
