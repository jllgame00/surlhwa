# ACT7 visits and common ending source adoption

Baseline: accepted ACT6 `3c25ba0`, implementation plan `ac3aaf3`.
Authority: Final Canon40 CAM01–04/07,41,57 OD01–03,10 MEM07,60/61/72;
Architecture03/05/20/21; ACT7_ENDING_IMPLEMENTATION_PLAN.md. Canon,
Architecture and historical RC1 remain read-only. This is content/standalone
evidence; actual Player, rendered presentation and subjective pacing are separate.

## Exact source and schedule

The generator verifies the registry's UTF-8 hash and copies each value, source
ID, primary home, source field and original order exactly. Registry SHA256:
`4313d6640f82800008b6adbc59c106965abe33b8349fa477d81e6ba81311b348`.
There are **47 new records**:0136–0158,0231–0246,0001–0008. They do not overlap
accepted ACT6 source records. Existing Prince recalls0412–0414 are referenced,
not duplicated or changed. Numbers below are SRC-RC1/text_rc1 suffixes.

| Slot | RC1 / original night | Production night | Guest | Intro | Good / other | Farewell | Summary |
| --- | --- | --- | --- | --- | --- | --- | --- |
|43|44 /17.1|19.1|Night station worker|0236,0237|0234 /0235|0232,0233|0238|
|44|45 /17.2|19.2|Odysseus return|0142–0145|0140 /0141|0137–0139|0146|
|45|46 /18.1|20.1|Postman|0244,0245|0242 /0243|0240,0241|0246|
|46|47 /18.2|20.2|Odysseus stay|0154–0157|0152 /0153|0148–0151|0158|

The +2-night mapping retains all43 accepted slots and the ordinary guests'
authored placement before their associated special visits. Return requests
Warmth/Bitterness; Stay requests Warmth/Clarity. No exact cocktail is required.
All nine recipes,22 physical ingredients, supply timings, known seeds and
learning definitions remain unchanged. No conditional ACT7 visit or new fact
grant is introduced.

Narration:0137,0139,0142,0148,0151,0154,0156,0232,0240 and0001–0008.
Mixed speech/narration:0233,0241. Displays:0136,0147,0231,0239.
Summaries:0146,0158,0238,0246. Other new fields remain spoken. Mixed source
fields are not split or assigned invented speakers. Original `onEnterLines`
ownership is retained when these source fields are used as farewell reading.

`guest_odysseus`/`visual_odysseus` uses display0136; display0147 is preserved
with an explicit disposition. Existing station-worker/postman identities remain
unchanged; their additional display fields0231/0239 are preserved in the ledger.

`mem_07`/`anchor_ship_fragment` belongs only to natural departure46 after the
farewell, regardless of FIT. It appears before the separate ending reading.
No victory, cure, shared SimCheong incident, hidden wait/click condition, future
completed ship, trophy or completionist gate is added.

## Bounded remembered quotation

The eight common ending records0001–0008 retain their original order and exact
`CampaignEnding.asset:onEnterLines[0..7]` source ownership. After common0002,
the ending resolves one already adopted Prince recall from saved choice:

- keep →0412, `keepTheLightClosingRecall`.
- seek →0413, `seekTheStarClosingRecall`.
- null/none →0414, `legacyClosingRecall`.

This is an **explicit new placement** of existing source text, not an assertion
that historical RC1 authored an ending callback. Canon40 permits brief memories
within the single ending, and Architecture03 permits bounded callbacks reading
owned choice/history. It is presented as a remembered Prince quotation, without
spawning a returned guest. The saved Open/Quiet farewell and memory variant
remain unchanged. A promise to seek a star does not synthesize a star, success
or new choice receipt. Coda0008 remains the final common line.

`CampaignEnding` is optional, immutable authored data with `Lines`,
`RecallAfterLine`, `RecallByChoice`, `CodaTextId` and `ResolveLines(choice)`.
It copies inputs and produces nine text IDs without changing runtime history.
Accepted ACT1–6 definitions omit it. The ending is not a48th encounter.
Runtime/session/codec work owns ending phases and durable reading; no separate
progress, knowledge or persistence authority is created here.

## Artifact preservation

Accepted ACT6 SHA256:
`e6bb7131fc5424186bc2cf4b1b15160b8cead275bcf7c9824ee1ccd1bfb5dfcf`.
Every earlier definition and array prefix is preserved; only scope/revision,
four visits, one guest,47 text records and the ending definition are appended.

- `act7-adoption.json`: `c05fe3d8ab3bb5b3883cb6176744a9673883a1cfe48f8e4a145966ac85d0b5eb`.
- `act7.json`: `7a82a84626f00830484a33c4ea00dde6d72a872a87ee3c5db5dee4f6894773ce`.

## Focused evidence

- `Evidence/act7-content-python-red.log`:4 expected failures for absent adoption.
- `Evidence/act7-content-python-green.log`:4 PASS, covering all47 records,
  authored reading and request ownership, memory owner, bounded callback and
  full43-slot prefix with unchanged recipes/supply/knowledge.

## Resumed loader and route verification — 2026-11-11

Resumed from the inherited ACT7 artifacts without regenerating or discarding
them. The optional immutable `CampaignEnding` API is unchanged. The loader now
accepts `through_act_07`, preserves the first43 slots and ACT1–6 boundaries,
and starts ACT7 at slot43. Only this scope permits the ending field; accepted
earlier scopes still have no ending.

The existing strict loader now checks all four ACT7 visits' actor, night,
request, authored reading, zero learning grants, and natural MEM07 owner. It
retains source IDs, Canon homes, original field ownership/order and roles for
all47 adopted records, verifies verbatim displayed-value hashes, and checks the
three existing Prince recall sources. The common eight ending IDs must remain
ordered, with exactly one saved-choice recall after common line0002 and coda0008
last. No generic dialogue graph, content authority or persistence format was
added. Source-registry byte/hash correspondence remains the adoption tool's
responsibility; runtime source checks do not replace that pipeline evidence.

Retained evidence in `Evidence/act7-content-resume-20261111-01/`:

- `red-loader-01.log`:1 PASS/1 expected failure (`Unknown field: ending`).
- `green-loader-01.log`:2 PASS after loading the bounded ACT7 definition.
- `red-ownership-01.log`:18 PASS/20 expected failures exposing missing ACT7
  source/ownership guards; no compile error is counted as behavioral RED.
- `green-ownership-01.log`:38 PASS, including all30 malformed-content cases,
  immutable choice resolution and all six earlier scopes' absent-ending contract.
- `green-routes-01.log`:68 PASS/0 failures (40 ACT7 content tests and28 ACT6
  content regressions), compiled standalone .NET; no warning/error output.
- `python-green-02.log`:4 PASS against the exact registry and all accepted
  prefixes. `adoption-check-01.log`: deterministic generator check PASS without
  rewriting either existing ACT7 JSON artifact.
- `preservation-01.json`: accepted ACT1–6 checkout bytes match HEAD `ac3aaf3`
  under their existing `.gitattributes` line endings. ACT6 and both ACT7 hashes
  remain those recorded above.

The route tests reuse actual full campaign command histories, including authored
discovery failures/retries and the real knowledge/supply state. For slots43–44,
Hanky Panky, Manhattan, Negroni and Seolya each produce a fitting, properly
prepared drink. For slots45–46, Dry Martini, Gin Fizz, Last Word, Manhattan,
Manwol and Seolya do so. Both tested histories retain all nine learned recipes
and22 supplied ingredients with no ACT7 knowledge grant. A genuine Quiet Prince
history and failed terminal Odysseus service still read the authored farewell,
leave exactly one `mem_07` owned by occurrence46, and preserve prior knowledge.
No save checkpoint was fabricated to establish these route results.

These are standalone domain/content and deterministic source results. Root owns
aggregate regression, Unity, Player/input proofs and rendered presentation.
Dialogue tone, pacing and emotional effect remain human-owned signoff; no
Unity/Player or subjective PASS is asserted by this content record.
