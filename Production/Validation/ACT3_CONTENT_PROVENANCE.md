# ACT 3 source adoption and domain checkpoint

Baseline: stable ACT 2 commit `184e1af`. This record covers source adoption and
domain evidence only. Player, scene, continuation and rendered presentation
evidence belong to the ACT 3 implementation ledger; this document does not
declare them complete.

Authority: immutable Final Canon 53/40/41/10/61; approved Architecture 03-05;
Production Expansion Plan P07c/P08 and ACT3_IMPLEMENTATION_PLAN.md. Canon and
Architecture source files are read-only. No new cocktail, ingredient, supply
event, knowledge seed, discovery lesson, companion, secret trigger or revisit
is introduced. Other people's laughter is part of Quixote's recollection,
not a game verdict or a new crowd/laughter effect.

## Source crosswalk

The generator reads `SURLHWA_CANON/_verification/wording_registry.json` and
verifies each original text's UTF-8 SHA256. Registry SHA256:
`4313d6640f82800008b6adbc59c106965abe33b8349fa477d81e6ba81311b348`.
All numbers below are suffixes of `SRC-RC1-NNNN` / `text_rc1_NNNN`.

| RC1 row | Original night | Production night | Guest | Intro | Good / other response | Farewell | Request |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 26 / Ordinary08 | 8.1 | 10.1 | guest_postman | 0180, 0181 | 0178 / 0179 | 0176, 0177 | 0182 |
| 27 / QuixoteDeclaration | 8.2 | 10.2 | guest_quixote | 0263-0266 | 0261 / 0262 | 0259, 0260 | 0267 |
| 28 / QuixoteAlone | 9.1 | 11.1 | guest_quixote | 0253-0256 | 0251 / 0252 | 0248-0250 | 0257 |
| 29 / Ordinary09 | 9.2 | 11.2 | guest_snow_worker | 0188, 0189 | 0186 / 0187 | 0184, 0185 | 0190 |

The +2-night mapping retains all 25 accepted positions and follows ACT 2's
existing redistribution. Canon40 CAM-01/CAM-05 and Canon41 NIGHT-05 permit
documented pacing redistribution while requiring all source encounters.

All 37 records are retained: 0175-0190 and 0247-0267. Display records are
0175/0183/0247/0258; summary records are 0182/0190/0257/0267; narration records
are 0176/0184/0249/0250/0255/0260/0265; 0177/0185 are mixed speech/narration;
the remaining records are spoken. Intro and farewell arrays retain their
original field order. The package preserves value/sourceText bytes, original
field, order, source ID/hash and Canon primary home. It does not split mixed
fields or invent speaker attribution.

`text_rc1_0183` is reused byte-for-byte as an existing accepted record.
Existing ordinary guest identities remain unchanged. Visit-specific duplicate
display sources 0175 and 0247 remain in the package with explicit dispositions;
Quixote's actor uses declaration display 0258. The existing spatial-fixture
identity `guest_quixote` / `visual_quixote` is retained rather than renamed.
Source-preserved art availability is separate from final visual acceptance.

## Additive artifacts and authority

Accepted ACT 1 SHA256:
`0d347e6976825ba61ab6a9fec39c73decc8f13ac10c5f0bf4d80558b1f33e3d4`.
Accepted ACT 2 SHA256:
`e38fe3dce7cdcb004dd0731df730925c2531562379fda7fde95d922c0d620fa1`.
Both files remain untouched. The deterministic composition preserves every
accepted ACT 2 array prefix and every other definition except its new scope
and revision. Recipes, tuning, supply boundaries, Known seeds and Prince
contracts are identical.

- `act3-adoption.json`: independent four-encounter source package, SHA256
  `6461b2898d99fcebb16dda89695fd67408cc518e2e889543e756e73088add632`.
- `act3.json`: schema 2, scope `through_act_03`, revision
  `campaign_through_act3_v1`, SHA256
  `9db4998cd4f3dd8fc1c0371e8396504c07c0cce253e504caf39f48ec370555f0`.

MEM-03 is `mem_03` / `windmill_fragment`, owned only by RC1-28 natural
departure, after its complete farewell reading and regardless of FIT. It is
present for RC1-29; the final ordinary guest is not its grant boundary. The
existing session completion transaction and save receipt validation own it.
The view cannot grant it. No future Sancho/flag variant is adopted. MEM-02
must already exist at ACT 3 entry because the postman notices the swing.

`CampaignContent.ActAt(slot)` exposes the loader-validated ACT partition for
presentation without new save state. ACT start slots are 0/17/25; generic
fixtures default to ACT 1. Loader scope, ordered source rows, actor ownership,
Quixote visual identity and exact authored memory ownership are validated.

## Evidence

- Python ACT 2/3 source suites: 9 PASS; ACT 3 subset: 3 PASS. Package tests
  first failed because ACT 3 had no source package/generator, then passed
  after deterministic adoption and composition.
- `Evidence/act3-content-red.log`: 9 PASS / 2 FAIL, proving missing ACT-number
  projection and unsupported ACT 3 scope before implementation.
- `Evidence/act3-content-green-02.log`: 11 PASS / 0 FAIL. An intermediate
  compile run caught one remaining renamed local variable and is retained as
  `act3-content-green.log`; it was corrected before passing evidence.
- New actual-domain route traverses all 29 encounters, supplies real amounts
  and technique, checks at least two earned Known/supplied/prepared alternatives
  at every ACT 3 request, fails both Quixote services, reads departures and
  round-trips every checkpoint. RC1-28 interrupted departure and failed save
  publication retain the original service and award exactly one memory on retry.
- Wider ACT 1/2/3 content, learning, session, memory and save regressions:
  `Evidence/act3-content-regression.log` initially reports 55 PASS / 8 filesystem
  access failures in the sandbox. The unrestricted isolated rerun is recorded
  separately in `Evidence/act3-content-regression-unrestricted.log`: **63 PASS /
  0 FAIL**. This clears the sandbox-only save-test failures.

These are standalone .NET domain/codec tests, not Unity execution, normal
Player evidence, native input, emotional pacing approval or rendered-quality
approval. Existing test assertions were not weakened. No scene/prefab or
accepted content asset is changed by this bounded content task.
