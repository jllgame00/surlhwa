# ACT 4 source adoption and domain checkpoint

Baseline: accepted ACT 3 commit `4790c33`. This record covers source adoption,
content/session integration and standalone domain evidence. Player, scene,
continuation publication and rendered presentation evidence belong to the ACT 4
implementation ledger. Final dialogue feel and pacing remain human-owned.

Authority: Project Vision; immutable Final Canon 54, 40/41, 10 MEM-04, 61,
20/21/22, 31 DRINK-08 and 33; Architecture 03-05; approved Production Expansion
Plan and ACT4_IMPLEMENTATION_PLAN.md. Canon, Architecture and historical RC1
sources are read-only. No global input/time freeze, Faust prerequisite, idle
trigger, future revisit, economy or alternate ending is introduced.

## Source crosswalk

The generator reads `SURLHWA_CANON/_verification/wording_registry.json`, checks
each original text's UTF-8 SHA256 and retains source field, original order,
primary home and source ID. Registry SHA256:
`4313d6640f82800008b6adbc59c106965abe33b8349fa477d81e6ba81311b348`.
All numbers below are suffixes of `SRC-RC1-NNNN` / `text_rc1_NNNN`.

| Production slot | RC1 row / original night | Production night | Guest | Intro | Good / other response | Farewell | Summary / retry |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 29 | 30 / 10.1 | 12.1 | Station worker | 0035-0037 | 0033 / 0034 | 0031, 0032 | 0038 / 0039 |
| 30 | 31 / 10.2 | 12.2 | Hamlet question | 0088-0091 | 0086 / 0087 | 0083-0085 | 0092 |
| 31 | 32 / 11.1 | 13.1 | Repairer | 0196, 0197 | 0194 / 0195 | 0192, 0193 | 0198 |
| 32 | 33 / 11.2 | 13.2 | Hamlet stillness | 0099-0102 | 0097 / 0098 | 0094-0096 | 0103 |

The +2-night mapping preserves the accepted 29 slots and follows the existing
Production redistribution allowed by Canon40 CAM-01/CAM-05 and Canon41 NIGHT-05.

All **40 records** are retained: 0030-0039, 0082-0103 and 0191-0198.
Display records are 0030/0082/0093/0191; summary records are 0038/0092/0103/0198;
narration is 0032/0083/0085/0089/0094/0096/0192; 0193 remains mixed speech and
narration. Other records are spoken. The package does not split mixed fields
or invent speaker attribution. Station-worker and repairer identities remain
unchanged. Visit-specific display records 0030, 0093 and 0191 have explicit
dispositions; Hamlet uses question display 0082 as its actor label.

`enc_rc1_30_known` is a narrow ordinary fallback: it copies the accepted
`enc_rc1_14_known` station-worker request unchanged except for ID, source-row and
night (`RC1-30`, night12). It uses the existing Warmth request, reading and
reactions. Its source disposition is explicit. The earlier encounter definition
is untouched. Selection uses existing recipe knowledge; it neither manufactures
Known nor repeats the discovery lesson after it is Known.

## Recipe, learning and retry authority

`recipe_french_75` / `cocktail_french_75` adds Gin30ml, Fresh Lemon Juice15ml,
Simple Syrup15ml and Champagne60ml, with Awakening interpretation. The root
agent reverified [official IBA French 75](https://iba-world.com/iba-cocktail/french-75/)
on2026-10-31. Nominal millilitres replace RC1's oz-rounding artifacts
(29.999998/14.999999/14.999999/59.999999). Canon's documented game expression uses
Simple Syrup for Sugar Syrup and a measured Champagne Pour after Shaker Strain,
followed by GentleStir. It uses no garnish; flute remains metadata under
SUPPLY-005, without a new glass-selection grade.

`fresh_lemon_juice` remains distinct from accepted `lemon_juice`. New physical
supply consists only of `fresh_lemon_juice` and `champagne`, starting before
encounter29. Earlier ingredients, recipes, supply, Known seeds and tuning are
unchanged. Physical supply and ACT entry reveal no recipe knowledge.

French knowledge follows consumed reading, using the existing fact authority:

- 0035 grants identity.
- 0036 grants method and amounts for Gin/Fresh Lemon Juice/Simple Syrup.
- 0037 grants Champagne amount, preparation and garnish information.

Facts remain Partial until actual target recognition, FIT, preparation and
quality meet the existing required-discovery policy. A failed attempt retains
the same encounter and facts. Optional `DiscoveryRetryTextId` defaults to null;
only the new lesson supplies `text_rc1_0039`. `CampaignSession.CurrentText`
projects it during Retry. Advancing consumes the existing receipt form
`<occurrence>:Retry:0:text_rc1_0039` before returning to Brewing. The checkpoint
schema and phase enum are unchanged. Accepted encounters with no retry text
retain their prior behavior.

The loader maps the exact six-step preparation array to recipe-owned
`StrainThenPourAndGentleStir`. Existing preparation arrays retain
`PourThenSplash`; no customer request or cocktail-ID branch chooses technique.
Actual transfer/technique evaluation remains domain-owned and separately tested.

## Memory and artifact boundary

MEM-04 is `mem_04` / `stage_skull_shadow`, owned solely by RC1-33's natural
departure after all farewell reading, irrespective of FIT. Its stage/skull and
restrained shadow are local presentation. It grants no input or progression
authority. The existing session publication transaction owns the memory; the
view cannot grant it. Earlier memories remain intact.

Accepted ACT 3 SHA256:
`9db4998cd4f3dd8fc1c0371e8396504c07c0cce253e504caf39f48ec370555f0`.
The file is untouched, as are accepted ACT 1 and ACT 2. Composition preserves
every accepted array prefix and definition, with only the new scope/revision
and explicitly appended resources.

- `act4-adoption.json`: independent source package SHA256
  `832e4c629d61a6968fa5e6ba73692d88a62f887660e65811b66bc4c46b8c5b86`.
- `act4.json`: schema2, scope `through_act_04`, revision
  `campaign_through_act4_v1`, 33 slots, SHA256
  `fcc2aeb9c4269c2c9fd34018e4c82b79553ffc3645ad2b8be3970abd673ad6fe`.

## Evidence

- Python ACT2/3/4 source suites: **12 PASS**; ACT4 subset: **3 PASS**. The new
  tests first failed because the ACT4 generator/package were absent, then passed
  after source adoption. The generator rechecks both committed artifacts against
  deterministic composition and the pinned accepted ACT3 hash.
- `Evidence/act4-content-red.log`: **13 PASS / 2 FAIL**, proving unsupported
  ACT4 scope before loader/session implementation.
- `Evidence/act4-content-green-01.log`: **14 PASS / 1 FAIL** at the expected
  in-progress codec integration boundary: publishing the new Retry receipt.
  The real route reached missing-GentleStir failure and the sourced retry reading.
  This intermediate result is retained; it is not a final PASS claim.
- `Evidence/act4-content-green-03.log`: **15 PASS / 0 FAIL**, including the
  full33-slot domain route, consumed French facts, missing-finish failure,
  sourced retry reading, reload, successful discovery, at least two actually
  Known/supplied/prepared alternatives for each new recommendation and natural
  MEM-04 after failed Hamlet services. The second intermediate run exposed an
  incorrect test fixture: accepted Gin Fizz includes Clarity and therefore fits
  Hamlet. The failed-service fixture now uses earned French75 (Awakening only);
  product tags and the mismatch assertion remain unchanged.
- `Evidence/act4-content-regression-01.log`: **36 PASS / 0 FAIL** for earlier
  ACT1/2/3 content, actual campaign routes, reading facts and session boundaries.
- `Evidence/act4-content-python-regression-unrestricted.log`: **79 PASS** for
  the entire Python tools suite. Two existing inventory tests
  (`test_changed_bytes_invalidate_exact_inventory`,
  `test_new_file_invalidates_exact_inventory`) first failed at temporary-directory
  writes under sandbox ACLs, including workspace-local scratch. The same tests
  passed outside that restriction with isolated workspace scratch storage and
  without changing their assertions. Initial evidence remains in
  `act4-content-python-regression.log` and
  `act4-content-python-regression-workspace.log`.

- `Evidence/act4-content-full-regression.log`: **316 PASS / 0 FAIL** after
  domain/codec integration, running the complete standalone domain, application,
  content and save suite with isolated build output and actual temporary-file
  operations. No assertions were weakened.

These tests do not establish Unity execution, actual Player behavior, native
input, rendered art quality, dialogue feel or emotional pacing approval.
