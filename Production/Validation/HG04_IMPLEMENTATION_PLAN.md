# Core Domain, Stir and ACT 1 excerpt Slice implementation plan

> For agentic workers: use superpowers:executing-plans task by task. The repository's autonomous working contract and the owner's proceed instruction authorize native execution here. Preserve this tracked ledger and evidence rather than deleting them.

**Goal:** A playable two-encounter Slice that lets the player interpret, make and serve actual drinks, read distinct reactions, leave and Continue safely within the accepted C1 bar.

**Architecture:** Existing immutable ContentIO → pure Domain → Application → Unity Presentation boundaries. Decimal containers own liquid; trace owns actions; actual circular motion owns technique. Application owns ordered flow and durable checkpoints. UI/audio/models read accepted state. No recipe selection generates liquid.

**Tech stack:** Existing Unity 6000.3.7f1/URP, Input System, uGUI/TMP, Json.NET; standalone .NET/NUnit contracts plus native Unity/Player checks. No new framework.

**Spec:** Read-only SURLHWA_ARCHITECTURE 02/03/04/05/12/13/20/30/31/40/41/51; Final Canon 20/21/22/23/31/33/51/61/62/70/71/72/73; HG04_OWNER_BASELINE.md contains the latest owner override.

## Global constraints

- Work only in D:\univ\surlhwa\SURLHWA_PRODUCTION, production-rebuild, under Production/. No frozen RC1/specification mutation.
- C1 fixed 28° orthographic composition is locked. No competing camera/spatial design. Workbench is resident in Bar; 2D versus shallow 2.5D tool views share identical domain/input.
- Eight exact liquid IDs: whiskey, vermouth, water, rye_whiskey, sweet_red_vermouth, angostura_bitters, gin, dry_vermouth.
- Recipes: recipe_test_stir 59.14706/29.57353/29.57353ml; recipe_manhattan 50/20/1ml game Dash and required Cherry/InGlass; recipe_dry_martini nominal60/10ml with optional garnish omitted. No global matching by request or knowledge.
- Decimal6-place ml; capacities60/300/180ml; flow30/5ml/s, transfer40/8ml/s; HOW360°; QUALITY1080° and75% valid motion60–300°/s. All provisional versioned tuning, not final feel approval.
- Ordinary13 → LittlePrince legacy Warmth follow-up; explicit fixture context, no fake past choices/successes. No ACT-complete, MEM-01, MEM-03 or ending grant.
- Seolya/Manhattan Known seeds; Dry Partial facts without identity. Supply independent. Normal recognition discovery independent of FIT.
- Save schema1 in separate Slice namespace, current/previous validated generations; no mid-pour restore or RC1 import. Settings separate. Errors cannot fabricate successful progress.
- V1 domain, V4 simulated adapter, V5 native OS input, V6 rendered evidence and human judgment stay distinct. Missing evidence is reported accurately.

## Review focus

1. Extra liquid after interruption, fine-flow quantization, finite mixed-source overflow: literal mass-conservation tests and release/rearm input regression.
2. Stale good Stir transferred into a new mixture: lineage/revision tests, including partial transfers and dilution after mixing.
3. Hidden Dry identity through Guide/result/accessibility/tool labels: safe projection before/after one actual discovery; native captures.
4. Duplicate reading, submit/departure and out-of-order saves: occurrence/transaction/input identity tests and crash/failure injection.
5. Replacing Quixote with Prince accidentally granting completion or memory: source/fixture validation and both successful/failed full flows; all memories and ACT completion remain unearned.

### Task 1: Actual liquids, Stir and independent service evaluation

**Files:** Domain/Brewing.cs, StirTechnique.cs, CocktailCatalog.cs, ServiceEvaluation.cs, RecipeKnowledge.cs; Tests/EditMode/BartendingDomainTests.cs. All relative to Production/Unity/Assets/Surlhwa/.

**Interfaces:** Produces BrewingSession(catalog,tuning,supply), Pour(ingredient,target,ml,kind), Transfer(source,target,ml,kind), AddIce(target), BeginStir/MoveStir/EndStir, Garnish, Freeze; ServiceEvaluator.Evaluate(snapshot,request); KnowledgeState.GrantFact/Discover/Project. Containers expose read-only amounts and derive totals. Snapshots are immutable copies; rejected commands leave state unchanged.

- [x] Write failing behavior tests: finite source10ml→target7ml plus3ml spill; mixed-source proportion; frame partition/6-place residue; empty/invalid/unavailable source; stale/partial provenance; valid/weak/reversed/off-circle Stir; 1ml Pour versus Dash; Cherry required/optional; exact identities; unknown/ambiguous WHAT; two equal Warmth answers; Dry mismatch/discovery; Partial does not auto-Known; immutable snapshot.
- [x] Run standalone contracts and retain RED evidence.
- [x] Implement the minimal real domain and versioned tuning contracts.
- [x] Run full standalone contracts; inspect diff; commit internally complete domain.

### Task 2: Strict content and owner-reconciled source adoption

**Files:** Content/{Ingredients,Recipes,Profiles,Guests,Dialogue,Requests,Encounters}/ selected JSON; ContentIO/SliceContentLoader.cs; Editor/SliceContentGenerator.cs; Tools/adopt_slice_content.py; Tests/EditMode/SliceContentTests.cs.

**Interfaces:** Consumes Task1 definitions. Produces immutable SliceContent registry, typed lines with sourceId/hash/field/role, explicit start grants and encounter sequence. Generated runtime bundle is derived from authored JSON and hashes.

- [x] Write failing strict-schema/reference/source/Unicode/knowledge-supply reachability tests; validate malformed numbers, duplicate keys/IDs, unknown fields/enums, unsupported method/constraints.
- [x] Adopt source fields verbatim from Canon/reference, including Ordinary13, neutral Prince follow-up and existing farewell/reaction fields; retain exact text hash/role/order. Reconfirm selected IBA facts; document game simplifications.
- [x] Implement loader/bundle generator; run contracts/content validation; commit.

### Task 3: Durable Application Slice flow

**Files:** Application/SliceSession.cs, InputGate.cs, ModalStack.cs; ContentIO/SliceSaveStore.cs, SliceSaveCodec.cs; Tests/EditMode/SliceFlowTests.cs, SliceSaveTests.cs.

**Interfaces:** Consumes SliceContent/Task1. Produces ordered occurrence-scoped commands, immutable read model, logical stage clock, ServiceCommitted outcome and pending reading cursor. ISliceSaveStore publishes one schema1 checkpoint or reports failure; UI remains recoverable and cannot claim saved progress on failure.

- [ ] RED: consumed line/facts; distinct reaction/result/discovery/farewell; normal failure progression; repeated submit/departure; no ACT/memory grants; modal held-input cancellation/rearm; transient Brew restart; durable outcome/cursor; current/previous corruption/future-schema/unknown-ID and each write boundary failure.
- [ ] Implement transactional flow and two-generation local store using actual file operations in isolated tests. Preserve original failed generations; no silent future-version rollback.
- [ ] Full contracts and native file-store checks; commit.

### Task 4: Resident workbench, input, UI, sound and Prince presentation

**Files:** Presentation/SliceInputAdapter.cs, SliceWorkbenchView.cs, SliceUi.cs, SliceAudio.cs, SliceController.cs; Editor/SliceBuilder.cs; separate Scenes/Bar_Slice and SliceAssets bindings; Tests/EditorIntegration/SliceIntegrationTests.cs.

**Interfaces:** Consumes Task3 read model and semantic commands. Input System events pass through production adapter; UI has one modal/focus owner. Tool views show actual volume/tint/ice/garnish/stir angle. Audio follows accepted flow/motion and stops on release/cancel/dispose. Logical arrival/departure progresses without animation/audio completion callbacks.

- [ ] RED: real adapter Pour/release/modal/focus; one physical event consumes one line; UI projection; missing presentation fallback; one EventSystem/AudioListener; same domain result for flat/shallow tool views; sprite contacts; no clipping/counterfeit fill.
- [ ] Build isolated Slice scene from locked C1 composition; bind identifiable Little Prince source art with provenance, 2D hands/grips, shallow tools, Korean licensed font, restrained room/winter/physical audio and silence. No final-art claim.
- [ ] Simulated production-input recipe/flow checks, native Unity suite/build; actual rendered review at720p/1080p. Commit.

### Task 5: Complete evidence and owner handoff

**Files:** Tools Slice validation/capture/native input support; Validation/HG04_PROGRESS.md, review index, acceptance matrix/manifests.

**Interfaces:** Consumes exact built Player/content/tuning/fixture hashes and observable production diagnostic snapshots; never writes gameplay state after fixture setup.

- [ ] Fresh domain and simulated production-input F01–F05 with three recipes/two guests/save boundaries; preserve failures separately.
- [ ] Native OS input representative flows and release/modal/reading/Continue regressions, accurately labelled;720p/1080p UI/tool/character captures and clips.
- [ ] Record Player frame/memory/transition/save distributions,60s idle and repeated lifecycle samples; disclose hardware/limits. No inherited A/B rerun or direction reopening.
- [ ] Verify A/B/C/C1 evidence, RC165 dirty paths/index and74 specification hashes. Source/Player full inventory and reproducible build identity.
- [ ] One fresh technical whole-branch review, fix objective significant findings with RED→GREEN; record human gates as review-pending. Commit handoff; keep branch local; no campaign expansion.
