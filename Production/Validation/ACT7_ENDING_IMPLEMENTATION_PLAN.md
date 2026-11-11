# ACT 7 and primary ending implementation plan

Read-only preparation began during ACT 6 validation. Implementation starts after
the internally complete ACT 6 checkpoint. Continue P07e/P08 autonomously under
the approved expansion plan and owner approval policy. No owner gate or push.
Preserve accepted ACT 1–6 bytes, source identities, schema 1, current/previous
generations, the Q1 composition, and owner-local changes. RC1 is source and
behavioral evidence, not an implementation template.

Implementation baseline: `3c25ba0`, completed ACT6 with eight current-identity
Player proofs. Accepted ACT6 JSON SHA256:
`e6bb7131fc5424186bc2cf4b1b15160b8cead275bcf7c9824ee1ccd1bfb5dfcf`.
No Unity or Player process remains from that checkpoint. Root will record fresh
pre-Editor hashes before ACT7 serialized work.

Authority: Project Vision; Final Canon40 CAM01–04/07,41,57 OD01–03,10 MEM07,
60/61/72; Architecture03/04/05/11/20/21; approved Production Expansion Plan.
Odysseus's silence, recognized return and longer stay are distinct authored
events. No victory, cure, new shared Sim Cheong plot, hidden waiting trigger,
future revisit, economy or completionist ending is introduced.

## Content contract

Append four rows to the exact43-slot ACT6 prefix, retaining the established+2
night offset. No recipe, supply, knowledge or bartending-method expansion.

| Slot | Source | Production night | Encounter / request |
| --- | --- | --- | --- |
|43|RC1-44|19|Ordinary17 night station worker; Warmth/Bitterness|
|44|RC1-45|19|OdysseusReturn; Warmth/Bitterness|
|45|RC1-46|20|Ordinary18 postman; Warmth/Clarity|
|46|RC1-47|20|OdysseusStay; Warmth/Clarity; natural MEM07 departure|

Use `through_act_07`, `campaign_through_act7_v1`, `guest_odysseus`,
`visual_odysseus`, `mem_07`, variant `anchor_ship_fragment`.
Adopt47 exact registry records:0136–0158,0231–0246,0001–0008.

| Row | Display | Intro | Good / other | Farewell | Summary |
| --- | --- | --- | --- | --- | --- |
|44|0231|0236,0237|0234 /0235|0232,0233|0238|
|45|0136|0142–0145|0140 /0141|0137–0139|0146|
|46|0239|0244,0245|0242 /0243|0240,0241|0246|
|47|0147|0154–0157|0152 /0153|0148–0151|0158|

Preserve narration0142,0137,0139,0154,0156,0148,0151,0232,0240 and the ordinary
mixed speech/narration roles0233,0241, plus exact original field ownership/order/
hashes. Ordinary17's terminus/home distinction and Ordinary18's
recognition of accumulated objects remain before their associated special visits.
MEM07 belongs to actual departure46 regardless of FIT, before ending reading.
Its future completed-ship/fixed-anchor variant remains out of scope.

## One ending, durable reading

Adopt the eight exact `CampaignEnding.asset:onEnterLines[0..7]` records0001–0008
as narration. One bounded remembered quotation follows common line0002: select
existing Prince recall0412/0413/0414 from the saved keep/seek/none choice. This is
an explicit new ending placement of previously adopted source text, not a claim
that RC1 authored a separate ending callback. Label it as the remembered Prince;
do not spawn a returning guest. Record this placement and reason in provenance.
The saved Open/Quiet memory variant remains visible unchanged. No absent success
or glowing-star history is synthesized. The common eight lines stay in order.

Use a small optional immutable ending definition on CampaignContent, absent from
accepted ACT1–6. It contains the eight line IDs, the bounded callback insertion
after line index1, keep/seek/none text mapping, and coda0008. Resolve the nine-line
reading deterministically from saved choice; it owns no new progression state.
The loader validates exact source ownership, permitted roles, ordering and
existing remembered-speaker text. Do not build a general dialogue graph.

Append `EndingReady`, `EndingEntered`, `EndingReading` to CampaignPhase. Existing
`Complete` means read-complete/stable-coda only for full ACT7; earlier scopes keep
their accepted Complete behavior. Retain terminal slot46 and47 selected visits:
the ending is not encounter48.

1. Departure46 atomically publishes completion47, MEM07 and EndingReady through
   existing Publish/store. Only completed authored visits trigger this boundary.
2. Explicit advance enters EndingEntered with first common line visible.
3. Each explicit advance durably consumes the displayed line and moves to the
   next cursor under EndingReading. The last line publishes Complete.
4. Continue resumes Ready, first-unread ending line, or stable coda as saved.
   Complete does not consume coda again, create service48 or generate night21.

Reuse phase, lineIndex and owned consumed-line receipts. No payload/envelope
field, schema, stable identity or irreversible migration is required. Existing
storage namespaces remain unchanged; ACT7 uses its own campaign-through-act-07
destination and preserves ACT1–6 histories. Codec must reject premature/foreign/
skipped/repeated ending receipts,
wrong branch, incomplete departure state and contradictory ending cursor while
retaining all old-scope validation. Failed writes keep a validated generation;
retry publishes the same pending transaction without duplicate grants.

## Independent implementation and validation

Agreed narrow seams for parallel execution after ACT6 commit:

- Content owns `Domain/CampaignContent.cs` and the loader. Add optional
  `CampaignEnding Ending` at the end of the existing constructor. The immutable
  definition exposes `Lines`, `RecallAfterLine`, `RecallByChoice`, `CodaTextId`
  and `ResolveLines(string choice)`. The resolver inserts exactly one selected
  recall after common index1, returning nine sourced text IDs. Old content has
  null Ending. Publish this small API before runtime compilation; a missing-API
  compiler error is not meaningful behavioral RED evidence.
- Runtime/save owns `Domain/CampaignProgress.cs`, `Application/CampaignSession.cs`,
  `ContentIO/CampaignSaveCodec.cs`, `CampaignContinuation.cs` and its new tests.
  Content owns new source/data/loader tests; older continuation enum expectations
  belong to runtime/save. Root owns controller/UI/proof and all Unity launches.
- Cast owns Q1ProductionView, CampaignMemoryView, ACT7 builder/assets/scene and
  presentation tests. Public receipt projections are `OdysseusVisible`,
  `OdysseusAnchor`, `OdysseusShipFragment`; controller binding is `Odysseus`.
  [Independent cast audit](ACT7_CAST_PREPARATION.json) supplies exact source
  contacts and a conservative support candidate, not a GPU/visual PASS.
- Ending receipt ownership uses terminal occurrence46. First consumed line is
  `EndingEntered:0`; subsequent lines use `EndingReading:1..8`. Ready and Entered
  have no consumed ending prefix; Reading's cursor is the next unread index;
  Complete requires all nine receipts and resets ordinary cursor lineIndex0.
  A full campaign's Complete is invalid without its complete reading prefix.
  Earlier scopes retain their original Complete contract and reject new phases.

- [x] Content: adoption tool/data/provenance and strict loader scope7/boundary43,
  immutable optional ending definition, bounded source validation; full routes
  with actual service failures and unchanged nine recipes/twenty-two ingredients.
- [x] Runtime/save: session ending transitions and text projection, appended
  phases, exact codec reading ownership, source1–6 continuation pinning and strict
  prefix/destination precedence. Tests first for every ending boundary, each
  line resume, callbacks, publication failure and completed coda. Freeze old
  tests' pre-ending phase expectations rather than assigning new phases to old
  campaigns. Preserve historical service outcomes without reevaluation.
- [x] Cast: independently inspect exact Odysseus standing/seated/walk source art,
  supported Q1 contact and depth, separate ACT7 scene/font/history6..2+legacy1.
  Receipt-only anchor and ship fragment on an unobscured physical support; no
  completed-ship revisit art or trophy UI. Preserve reserved Prince chair.
- [x] Root UI/proof: all departed actors hidden during ending; restrained Ready,
  remembered-speaker and narration presentation; persistent coda/bar re-entry.
  Real input proofs for ACT6 completion→43..46→ending, FIT failure, Open/Quiet,
  Ready/Entered/mid-reading/last-line/Complete resumes and exactly-once MEM07.
  No source save fabricated for runtime evidence. Both720p/1080p and all seven
  memories visible with actual materials, no missing reference or overflow.
- [x] Run focused RED/GREEN, aggregate .NET/Tools/Unity regression, separate
  Player build and actual proofs, independent review, source/asset preservation,
  minimal serialized diff audit. Root alone launches Unity/build/Players; freeze
  C# for Editor and inventories for Player. Record objective evidence separately
  from native-input and final subjective signoff.
- [ ] Commit internally complete ACT7/ending, then proceed to scheduled P09–P12
  presentation/audio production and P13 full-campaign/native/performance work.
  No presentation debt closes merely because the campaign reaches its ending.
