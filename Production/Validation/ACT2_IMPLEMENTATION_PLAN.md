# P07b ACT 2 implementation plan

Execution: inline under superpowers:executing-plans, without routine owner gates.
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
- [ ] P07b.2 Source adoption and reachable lessons. Read exact Canon registry
  records for RC1-18–25, record original field/order/hash and displayed role.
  Adopt Hanky Panky with verified official source and exact ingredient identities;
  preserve failed required-discovery retry and Known fallback. Extend the content
  schema explicitly, keeping the ACT 1 loader and fixtures valid. Test source
  coverage, supply, multiple reachable Recommendation choices and complete flow.
- [ ] P07b.3 Additive campaign continuity. Retain validated legacy campaign bytes
  and exact accepted definition. Specify and test reversible continuation from
  every accepted ACT 1 checkpoint into the expanded content, preserving original
  results and both generations. Never silently accept arbitrary content hashes.
- [ ] P07b.4 Q1 presentation. Bind distinct lovers to two grounded anchors, show
  one shared cup, preserve source speakers and silence; add MEM-02's swing and
  adjacent empty place. Missing actor/clip cannot block logical departure.
  Inspect actual 720p/1080p output, retain provenance and presentation debt.
- [ ] P07b.5 Validate/commit integrated ACT 2. Compile Production, strict content
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
