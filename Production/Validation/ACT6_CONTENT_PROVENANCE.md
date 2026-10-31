# ACT6 source adoption and content checkpoint

Baseline: accepted ACT5 commit `2541b62`. This record covers exact source
adoption, loader integration and standalone content/domain routes. Continuation,
scene and actual Player evidence belong to the ACT6 runtime checkpoint. Dialogue
feel, pacing and emotional impact remain human-owned.

Authority: Project Vision; immutable Final Canon56,40/41,10 MEM-06,31 DRINK-09,
33 SUPPLY-N15 and60/61; Architecture03-05/11/20/21; Production Expansion Plan
P07d and ACT6_IMPLEMENTATION_PLAN.md. Canon, Architecture and historical RC1
sources remain read-only. SimCheong's emotional before-state, self-determined
resolve and calmer return remain three distinct visits. No sacrifice score,
bartender rescue, exclusive cocktail answer, father revisit, hidden trigger,
economy or ending transition is introduced.

## Exact source crosswalk

The generator reads `SURLHWA_CANON/_verification/wording_registry.json`, checks
each original text's UTF-8 SHA256, and retains exact text, source field, original
order, primary home and source ID. Registry SHA256:
`4313d6640f82800008b6adbc59c106965abe33b8349fa477d81e6ba81311b348`.
All numbers below are suffixes of `SRC-RC1-NNNN` / `text_rc1_NNNN`.

| Production slot | RC1 row / original night | Production night | Guest | Intro | Good / other response | Farewell | Summary / retry |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 37 | 38 / 14.1 | 16.1 | Repairer | 0220,0221 | 0218 /0219 | 0216,0217 | 0222 |
| 38 | 39 / 14.2 | 16.2 | SimCheong before | 0274-0277 | 0272 /0273 | 0269-0271 | 0278 |
| 39 | 40 / 15.1 | 17.1 | SimCheong resolve | 0285-0288 | 0283 /0284 | 0280-0282 | 0289 |
| 40 | 41 / 15.2 | 17.2 | Snow worker / Last Word | 0055-0057 | 0053 /0054 | 0051,0052 | 0058 /0059 |
| 41 | 42 / 16.1 | 18.1 | SimCheong return | 0296-0299 | 0294 /0295 | 0291-0293 | 0300 |
| 42 | 43 / 16.2 | 18.2 | Snow worker | 0228,0229 | 0226 /0227 | 0224,0225 | 0230 |

The established +2-night mapping preserves the accepted37-slot prefix. All
**59 records** are retained:0050-0059,0215-0230,0268-0300. Narration is
0052/0216/0224/0271/0280/0282/0293/0297;0217/0225 remain mixed speech and
narration; displays are0050/0215/0223/0268/0279/0290; summaries are
0058/0222/0230/0278/0289/0300. Other records remain spoken. Mixed fields are
not split or assigned invented speakers. Original `onEnterLines` field names
remain in provenance when Canon places those lines in common farewell reading.

`guest_simcheong` / `visual_simcheong` uses display0268. Duplicate displays
0279/0290 remain with explicit dispositions; existing repairer/snow-worker actor
labels remain authoritative and visit displays0050/0215/0223 remain in the ledger.

`enc_rc1_41_known` copies accepted `enc_rc1_29` exactly except ID, source row and
night:RC1-41,night17. This authored ordinary snow-worker Bitterness request
describes a cleared road covered again without a new campaign fact. Multiple
earned, supplied Bitterness drinks remain available. The fallback grants no
facts and repeats no discovery lesson. Ordinary16's returning-footsteps reading
stays at its authored position after SimCheongReturn; it is not reused before
her return. Source and implementation dispositions are explicit in the package.

## Recipe, supply and learning authority

`recipe_last_word` / `cocktail_last_word` is the ninth recipe, with authored
Clarity interpretation. Exact Gin, Green Chartreuse, Maraschino Luxardo and Fresh
Lime Juice each contribute22.5ml,total90ml. Canon31 and the original
`Recipe_LastWord.asset` agree. The root task reverified the
[official IBA Last Word](https://iba-world.com/iba-cocktail/last-word/) on
2026-10-31. Gin remains distinct from London Dry Gin and Fresh Lime Juice from
legacy Lime. No garnish is authored; cocktail glass remains metadata.

The loader maps only the exact preparation array
`shaker_ice,closed_shake,strain_from_shaker,no_serving_ice` to recipe-owned
`ShakeFinishPolicy.StrainOnly`. Gin Fizz and French75 retain their earlier
policies and definitions. Actual batch and transfer evidence remains owned by
the bartending domain; no guest intent or cocktail-ID branch chooses technique.

New physical supply is `green_chartreuse`, `maraschino_luxardo` and
`fresh_lime_juice`. Canon33 SUPPLY-N15 maps to production night17/slot39,
before SimCheongResolve and one visit before the slot40 lesson. Earlier supply
timing is unchanged. Twenty-two ingredients are physically available then, but
the ninth recipe remains Unknown with no facts until earned reading/service.
Known seeds remain identical to accepted ACT5.

Consumed source reading grants the existing facts:

- 0055: identity.
- 0056: the four amounts; the existing fact authority also exposes each exact
  ingredient identity from its amount fact.
- 0057: method, preparation and garnish.

The recipe stays Partial until target recognition, FIT, HOW and quality pass the
existing required-discovery policy. Actual Shaker Pour is recognized as Last
Word but fails preparation; it retains the lesson and exposes authored0059.
Retry/reload consumes the existing receipt
`<occurrence>:Retry:0:text_rc1_0059` before returning to an empty Brewing state.
Correct Shaker Strain then earns Known. Experimental recognition before the
lesson can also earn knowledge through the established general discovery
authority; the ordinary fallback handles this without a new seed or exception.
No session phase, save format or knowledge authority changes.

## Memory and artifact boundary

MEM-06 is `mem_06` / `half_submerged_lotus_slow_ripples`, owned solely by
RC1-42/slot41's natural departure after all farewell reading, irrespective of
FIT. Source0293 gives the half-submerged lotus and very slow ripples. The
existing publication transaction owns its receipt; the view cannot grant memory.
The final ordinary encounter at slot42 still runs afterward. All five earlier
memories remain intact.

The original plan's proposed Gin Fizz mismatch was corrected during source
audit: accepted Gin Fizz has Clarity and fits SimCheongReturn's Warmth/Clarity.
The mismatch route instead uses earned French75, whose sole tag is Awakening.
No cocktail or request tags changed; the fitting route uses earned Last Word.

Accepted ACT5 SHA256 remains
`bc76a5546c7f30d6dfa417fd3754a81fe4c52f8040b15bc86b42c1a954f34339`.
Composition preserves every accepted array prefix and non-appended definition.
Only new scope/revision and explicitly appended resources change.

- `act6-adoption.json`: independent package SHA256
  `d73cca2f9cb64948e77b9666da609393f45e6efe2210b5cbcef8bc2a9e651093`.
- `act6.json`: schema2,scope `through_act_06`,revision
  `campaign_through_act6_v1`,43slots,SHA256
  `e6bb7131fc5424186bc2cf4b1b15160b8cead275bcf7c9824ee1ccd1bfb5dfcf`.

## Focused evidence

- `Evidence/act6-content-python-red.log`: **4 FAIL**, because the source
  generator did not exist. `Evidence/act6-content-python-green-02.log`:
  **4 PASS**, checking all59 exact source records/fields/order/roles, authored
  sequences, recipe/facts, fallback source identity, supply39 and the complete
  accepted37-slot prefix. The generator independently rechecks deterministic
  package/composition and the accepted ACT5 hash. The first GREEN preceded the
  fallback source decision; the retained second GREEN covers the final mapping.
- `Evidence/act6-content-red.log`: **24 PASS /4 FAIL** at unsupported ACT6
  scope. The negative cases alone did not yet establish their specific guards.
- `Evidence/act6-content-green-01.log`: **28 PASS /0 FAIL**, including valid
  loading and all24 malformed actor, reading, request, memory, lesson, retry,
  fallback, supply and preparation cases. Three full43-visit routes cover fitting
  return, non-fitting return, and early experimental discovery/fallback. Each
  Recommendation has at least two actually Known, supplied and correctly
  prepared fitting alternatives. All new intro/request/reaction/farewell reading
  receipts are checked. Required-discovery routes retain one failed Shaker Pour
  attempt and successful retry, so44 services still produce43 departures.
- The routes roundtrip every departure through the real codec and Continue.
  The lesson is also reloaded while Retry text is unconsumed and after consuming
  it into Brewing. Final SimCheong departure injects publication failure, proves
  sequence/service/five-memory retention, then retries once. MEM-06 appears with
  exactly42 departures and slot42 still pending; reloading that checkpoint
  preserves the final ordinary visit. Completed Continue leaves save bytes
  unchanged. Earlier learned receipts and five memory IDs remain unchanged.

Only focused standalone/source checks ran in this subtask. They do not establish
Unity execution, actual Player behavior, native input, rendered art quality or
subjective dialogue/pacing approval. Root owns aggregate and normal Editor/Player
validation after integration. No earlier scene or content file was rewritten.
