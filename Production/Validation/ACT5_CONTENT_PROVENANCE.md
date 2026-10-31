# ACT 5 source adoption and content checkpoint

Baseline: accepted ACT 4 commit `33de7c7`. This record covers exact source
adoption and content/domain validation. Continuation, scene and actual Player
evidence belong to the ACT 5 runtime checkpoint. Dialogue feel, pacing and
emotional impact remain human-owned.

Authority: Project Vision; immutable Final Canon55,40/41,10 MEM-05,60/61 and72;
Architecture03-05/11/20/21; Production Expansion Plan P07d and the approved
ACT5_IMPLEMENTATION_PLAN.md. Canon, Architecture and historical RC1 sources
remain read-only. Faust's desire, contract and cost are authored events; this
adoption adds no rescue, judgment, secret recipe entrance, delayed unlock,
economy or future revisit.

## Exact source crosswalk

The generator reads `SURLHWA_CANON/_verification/wording_registry.json`, checks
each original text's UTF-8 SHA256 and retains exact text, source field, original
order, primary home and source ID. Registry SHA256:
`4313d6640f82800008b6adbc59c106965abe33b8349fa477d81e6ba81311b348`.
All numbers below are suffixes of `SRC-RC1-NNNN` / `text_rc1_NNNN`.

| Production slot | RC1 row | Production night | Guest | Intro | Good / other response | Farewell | Summary |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 33 | 34 | 14.1 | Faust contract | 0066-0069 | 0064 / 0065 | 0061-0063 | 0070 |
| 34 | 35 | 14.2 | Station worker | 0204, 0205 | 0202 / 0203 | 0200, 0201 | 0206 |
| 35 | 36 | 15.1 | Postman | 0212, 0213 | 0210 / 0211 | 0208, 0209 | 0214 |
| 36 | 37 | 15.2 | Faust cost | 0077-0080 | 0075 / 0076 | 0072-0074 | 0081 |

The established +2-night mapping preserves the accepted 33 slots. All **38
records** are retained: 0060-0081 and 0199-0214. Narration is
0063/0068/0073/0074/0200/0208; 0201/0209 remain mixed speech and narration;
display is 0060/0071/0199/0207; summaries are 0070/0081/0206/0214. Other records
remain spoken. Mixed fields are not split or given invented speaker attribution.
The registry's original `onEnterLines` field names remain in provenance even
when Canon places those lines in the common farewell sequence.

`guest_faust` / `visual_faust` uses display0060. Duplicate visit display0071 is
retained with an explicit disposition. Existing station-worker and postman
identities stay unchanged; duplicate displays0199/0207 also have explicit
dispositions. Every new encounter is a Recommendation: Clarity/Awakening,
Awakening/Clarity, Warmth and Bitterness/Warmth respectively.

## Gameplay authority and artifact boundary

ACT 5 introduces **no recipe, ingredient, supply grant, knowledge fact,
discovery lesson, technique, phase or save format**. All eight recipes,
nineteen physical ingredients, knowledge seeds, tuning, Prince policy and
earlier encounter definitions remain identical to accepted ACT 4. The existing
generic `CampaignContent.ActAt` receives the additional start slot33; the
domain API and session do not need a new authority or state.

MEM-05 is `mem_05` / `faint_circle_broken_glass_red_light`, owned only by
RC1-37's natural departure after all farewell reading, irrespective of FIT.
The session's existing save transaction owns publication; failure cannot
partially grant memory or duplicate the completed service. Source0074 provides
the final local circle, broken glass and restrained red-light narration. The
view cannot grant memory or affect progression.

The generator pins accepted ACT 4 bytes with SHA256
`fcc2aeb9c4269c2c9fd34018e4c82b79553ffc3645ad2b8be3970abd673ad6fe`.
Earlier source files are untouched. Composition appends only one guest,
38 sourced texts, four encounters and four slots, plus scope/revision changes.

- `act5-adoption.json`: independent source package SHA256
  `c47c67ee8c5953f173fa0fa325f2cceef92f09aca44d472290887a154097b5d6`.
- `act5.json`: schema2, scope `through_act_05`, revision
  `campaign_through_act5_v1`, 37 slots, SHA256
  `bc76a5546c7f30d6dfa417fd3754a81fe4c52f8040b15bc86b42c1a954f34339`.

## Focused evidence

- Python adoption tests first produced **3 FAIL** because the ACT 5 generator
  was absent. `Evidence/act5-content-python-green.log` records **3 PASS** after
  implementation, checking all38 source bytes/fields/order/roles, exact row
  mapping and preservation of every accepted definition. The generator also
  rechecks both artifacts against deterministic composition and the pinned
  accepted ACT4 hash.
- `Evidence/act5-content-red.log`: **17 PASS / 3 FAIL**, with positive ACT5
  loading and both full-route cases rejected at the previously unsupported
  ACT5 scope. Negative malformed-content cases cannot establish their specific
  guards until the valid bundle loads.

- `Evidence/act5-content-green-01.log`: **20 PASS / 0 FAIL**. The valid bundle
  loads, the new ACT boundary projects at33, and all17 malformed ownership,
  identity, reading, request and memory cases are rejected. Both complete
  37-service routes consume every new intro, request, reaction and farewell
  reading, preserve earned knowledge/facts, and find at least two actually
  Known, supplied, prepared alternatives for each new recommendation. Final
  Faust is tested with fitting Negroni and non-fitting French75; both drinks
  retain Good preparation and quality. Both routes naturally grant MEM-05.
  Checkpoint roundtrip/Continue is validated at each departure. The failed-fit
  route also rejects an injected final publication failure without advancing
  sequence, losing the original service or partially granting memory, then
  retries successfully. Completed Continue adds no duplicate departure/memory.

These standalone checks do not establish Unity execution, actual Player
behavior, rendered art quality or subjective dialogue and pacing approval.
Root owns aggregate and normal Editor/Player validation after integration.
