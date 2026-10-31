# P07b ACT 2 implementation plan

Execution: inline integration plus bounded independent implementation under
superpowers:dispatching-parallel-agents, without routine owner gates.
Spec: approved PRODUCTION_EXPANSION_PLAN.md P07b/P08, Final Canon 52 and MEM-02,
Production Architecture 03/04/05. Gate 2 closed at 7c9c987.

Goal: preserve the two lovers' separate perspectives, departure, silence and
reunion around one shared drink, followed by one swing/empty-place memory.
Architecture: extend immutable encounter definitions and existing session/save
transactions; presentation consumes actor identities and cannot award progress.
Stack: existing Unity 6000.3.7f1, C#, strict JSON, Q1, existing input and tests.

Global constraints: no changed accepted ACT 1 JSON, IDs, outcomes or tuning;
no destructive save migration, invented speaker attribution, second request,
two-cup service, future revisits, or reclassification of presentation debt.

## Tasks

- [x] P07b.1 Domain transaction foundation. Add optional companionGuestId and
  authored memoryId/memoryVariant to CampaignEncounter; retain GuestId as lead.
  Validate distinct existing actor IDs and unique memory grant ownership in
  CampaignContent. Default ACT 1 contracts remain identical. CampaignSession
  grants the authored memory only with completed departure, regardless of FIT;
  CampaignSaveCodec verifies every receipt against its owning completed encounter.
  Tests in CampaignExpansionTests.cs: one service with two actors; failed-service
  reunion still grants one memory; failed publication/Continue never duplicates;
  unknown/duplicate actors and forged/early/wrong-variant memories fail. Run
  run_foundation_checks.ps1 RED then GREEN, inspect diff, commit bounded foundation.
- [x] P07b.2 Source adoption and reachable lessons. Read exact Canon registry
  records for RC1-18–25, record original field/order/hash and displayed role.
  Adopt Hanky Panky with verified official source and exact ingredient identities;
  preserve failed required-discovery retry and Known fallback. Extend the content
  schema explicitly, keeping the ACT 1 loader and fixtures valid. Test source
  coverage, supply, multiple reachable Recommendation choices and complete flow.
- [x] P07b.3 Additive campaign continuity. Retain validated legacy campaign bytes
  and exact accepted definition. Specify and test reversible continuation from
  every accepted ACT 1 checkpoint into the expanded content, preserving original
  results and both generations. Never silently accept arbitrary content hashes.
- [x] P07b.4 Q1 presentation. Bind distinct lovers to two grounded anchors, show
  one shared cup, preserve source speakers and silence; add MEM-02's swing and
  adjacent empty place. Missing actor/clip cannot block logical departure.
  Inspect actual 720p/1080p output, retain provenance and presentation debt.
- [x] P07b.5 Validate/commit integrated ACT 2. Compile Production, strict content
  checks, full domain regressions, affected Unity tests, Player fresh/Continue and
  failed-service/discovery evidence; native input where practical. Update ledger
  and next-package context; proceed to P07c without asking routine approval.

## Review focus

Unknown or duplicate companions must fail authoring validation. A paired encounter
must never create a second service or outcome. Failed FIT cannot suppress Canon
departure/memory. Publication failure retains the prior checkpoint and retries the
same receipt. Receipt validation must reject forged identity, variant, owner and
early grants even with a recomputed checksum. Legacy ACT 1 decoding remains valid.

## Execution ledger

- Runtime continuation from 2277ef0: three independent file scopes cover
  source-preserving save continuation, paired Q1 staging/build assets, and actual
  Player input/evidence automation. Root integrates controller, UI, zest input
  and the existing shared serving-glass presentation. Only root runs Unity.
  The owner-local review-slice.html edit remains outside the implementation.
  ACT 2 completion is an implementation checkpoint, never an owner gate.
- Runtime integration: ACT 2 has an additive `Bar_Q1_Act2` scene and separate
  Player/save namespace. Continue validates the exact accepted ACT 1 package and
  every preserved receipt, then publishes a create-only copy. Legacy current and
  previous generations remain unchanged. Existing expanded history takes
  precedence, including invalid/future-format evidence; no silent rollback.
  Completed ACT 1 maps to the next authored slot at NightEnd. Incomplete phases,
  original results, consumed lines, recipe knowledge and memory variants survive.
- Fresh-context runtime review found two implementation defects: inherited
  accessibility/audio preferences were not read from the legacy namespace, and
  real process termination could leave a pending file that blocked first import.
  Both were reproduced and fixed. Legacy preferences now supply a read-only
  fallback when no expanded settings exist. Exact matching interrupted-import
  byte prefixes with strict pending filenames are retained while a new file is
  published create-only; foreign or unrelated entries remain rejected. Reviewer
  recheck closed both findings with no new findings.
- Standalone final: 204/204 PASS, `Evidence/act2-continuation-orphan-final.log`.
  Full normal Unity Editor regression: 289/289 PASS,
  `Evidence/act2-runtime-tests-02.xml`. Initial full run was 278/281, retaining
  the preferences RED case plus an obsolete single-build-callback assumption
  and InputSystem EditMode fixture-isolation failure. Assertions were preserved;
  the harness now invokes the ordered callbacks and resets its test-only static
  input fixture between scenes. Python tooling: 62/62 PASS.
- Full historical spatial tests regenerate Bar_C/Bar_C1 and serialize unrelated
  material defaults. Both runs' complete churn diffs are retained in
  `Evidence/act2-editor-generated-churn-01.diff` and `-02.diff`; only the 67
  previously clean generated changes were restored. Accepted Bar_Q1, ACT 1 JSON,
  RC1 and all 74 Canon/Architecture files remain unchanged. Evidence:
  `Evidence/act2-runtime-preservation.json`.
- First additive Player build succeeded with zero errors/warnings. The corrected
  Player is rebuilding; actual Player route, restart and rendered validation
  remain pending. These compile/test results do not close presentation debt or
  imply final human signoff.

- Baseline: 32a0e52; only pre-existing review-slice.html is dirty. Canon/Architecture
  are read-only. Existing linked worktree is used; no new worktree or push.
- Baseline sandbox: 132 PASS / 17 save failures (access denied); unrestricted
  rerun underway. This is not Unity runtime evidence.
- Ruling: keep the package plan/ledger here beside existing Production records,
  using their native PowerShell commands instead of skill Bash wrappers.
- Pre-flight: P07b.1 provides optional actor/memory definition fields consumed by
  P07b.2 and P07b.4; P07b.3 must validate source content before continuing history.
  No change to accepted content is needed for the first bounded transaction task.
- P07b.1 complete: 159/159 standalone contract tests and 10/10 focused normal
  Unity Editor tests, including paired failed service, natural departure,
  Continue, failed publication retry and forged receipt checks. Fresh-context
  review found one real defect (a memory owner scheduled twice); reproduced
  as 158 PASS / 1 FAIL, fixed schedule validation, then both suites passed.
  No remaining critical/important or deferred minor review findings.
- Evidence: `Evidence/act2-foundation-final.log`,
  `Evidence/act2-domain-editor-final.xml` and corresponding Editor log.
  The first Editor run overlapped the intentionally failing repeated-slot test
  and reports 9/10; it is retained as superseded RED evidence. Headless exit 198
  reflects missing headless entitlement; normal licensed Editor execution works.
  Unity import-generated GraphicsSettings/QualitySettings and the newly generated
  PackageManagerSettings were inspected and removed from this phase's changes.
  No scene/prefab changes. Existing Player has not been rebuilt or certified.
- Preservation: the original `preservation-baseline.json` predates recorded
  license-era RC1 changes and fails on RC1. Comparing to the existing
  `quarter-view-preservation-after-license.json` shows all 74 specification files
  and every RC1 field/dirty-file hash unchanged. Neither baseline was rewritten.
- P07b.2 underway: separate source-adoption package for RC1-18–25; it deliberately
  cannot load as a campaign. Runtime schema, scoped supply, OrangeZest control,
  save continuation and paired presentation remain subsequent implementation.
- P07b.2 domain/content checkpoint: authored ACT 2 source package plus separate
  schema-2 composed candidate now load and traverse all 25 encounters through
  actual domain preparation, consumption, service, retry, departure and codec
  round trips. New ingredient supply is projected from the durable selected-slot
  cursor; no second knowledge authority or save format is introduced. Unknown
  content scope remains rejected. The accepted ACT 1 default build is unchanged.
- Fresh-context content review: fixed RC1-22 Known fallback retaining the wrong
  ordinary actor (failing Python regression before correction); added explicit
  multiple Known/supplied/prepared answers at each ACT 2 Recommendation. Added
  failing-then-passing rejection of discovery before supply and delays to accepted
  ACT 1 physical supply. No deferred minor review findings.
- Final standalone evidence: `Evidence/act2-content-final-02.log`, 168 PASS /
  0 FAIL. Python source/composition tests: 6 PASS. Normal Editor affected suite
  initially 47/47 PASS; final post-guard run **48/48 PASS** in
  `Evidence/act2-content-editor-final.xml`, with corresponding compilation log.
  No new Player/native/visual evidence is claimed. Editor-generated project
  settings were inspected and excluded again; accepted scenes/prefabs untouched.
- Ruling: the composed `act2.json` is a separate, generator-verified candidate.
  Keep the accepted ACT 1 bundle/build selected until reversible save continuation,
  new ingredient/garnish controls and paired presentation are integrated. Cost:
  two candidate scopes coexist temporarily; no premature Player readiness claim.
  [Content checkpoint and remaining dependencies](ACT2_CONTENT_CHECKPOINT.md).

- Final runtime corrections: the actual Player exposed seated clipping, an
  occluded MEM-02 despite an active object, and an intermittent serving-child
  shutdown exception. Corrected source-pelvis contact, moved the memory to the
  existing window-side counter, and guarded the destroyed presentation child.
  The teardown regression reproduced before the fix. Affected normal Editor
  tests are 23/23 PASS (`Evidence/act2-runtime-correction-tests.xml`), including
  real URP pixel visibility for both memory parts at 720p/1080p. Build 04 has zero
  errors/warnings. Final farewell/departure and completed-Continue Player runs
  pass at both resolutions; normal rendered images were inspected. The final
  lesson Retry reload is tracked in [the runtime checkpoint](ACT2_RUNTIME_CHECKPOINT.md).
  Final bounded code review reports no findings. Native app access timed out
  before any native action; no native certification or final aesthetic approval
  is claimed. All existing presentation debt stays open.
- Final serialized inspection: the additive scene adds 37 objects and changes
  only four existing blocks (bundle/font/cast/component/anchor references); no
  accepted object or transform is removed or moved. URP import normalized 28
  previously clean material defaults and two project settings; the complete
  diff is retained as `act2-editor-generated-churn-03.diff` and those own changes
  were excluded. Canon/Architecture (74 files), frozen RC1, accepted ACT 1 JSON
  and approved Q1 scene hashes remain unchanged.

- Final Build 04 Retry reload: PASS, 26 services (including the original failed
  lesson attempt), 25 departures, seven Known, two memories, no runtime errors;
  source/Player inventory unchanged. Completed Continue preserves both saved
  generations byte for byte at both resolutions. P07b runtime work is internally
  complete; append ACT 3 next. The actual ACT 2?3 transition is validated with
  that next content boundary, not claimed from a 25-slot package. Nothing pushed.
