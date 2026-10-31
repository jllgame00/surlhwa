# SURLHWA bounded Production expansion plan

> For agentic workers: GATE 1 is OWNER_APPROVED at plan commit 6a6637e; see [approval record](GATE1_OWNER_APPROVAL.md). Now execute autonomously through GATE 2 (complete ACT 1), then after GATE 2 approval continue ACTs 2–7 through GATE 3 (final Release Candidate). Use the existing inline workflow and package-specific checklists without requesting per-package permission. Follow [OWNER_APPROVAL_POLICY.md](OWNER_APPROVAL_POLICY.md); this policy revision is not plan approval.

**Status:** OWNER_APPROVED / EXECUTING_TO_GATE_3. Gate 2 formally closed at
`7c9c987`, 2026-10-31 Asia/Seoul; see [owner ruling](GATE2_OWNER_APPROVAL.md).
Q1 supersedes this plan's historical C1 spatial references. Package definitions
and historical baseline evidence below retain their original context.

**Goal:** Expand the accepted Vertical Slice into the seven-ACT main campaign through small, playable, technically verified packages with three bundled owner gates while preserving actual bartending, readable knowledge progression and the bar's accumulated memories.

**Architecture:** Extend the existing plain C# domain and Application authorities, immutable JSON content and presentation adapters. Keep the accepted C1 room and resident workbench. Campaign, memory and ending behavior must be added deliberately; the two-encounter Slice is not already a full campaign framework.

**Stack/baseline:** Production worktree `D:/univ/surlhwa/SURLHWA_PRODUCTION`, branch `production-rebuild`, accepted code `37679bf`; Unity `6000.3.7f1`, existing pinned packages, C#, JSON, uGUI/TextMesh Pro and existing render/input stack. Engine/package upgrades are separate changes, not prerequisites assumed by this plan.

**Spec:** Final Canon and approved Production Architecture at the source paths below, amended by the owner's C1, Prince excerpt, HG04 and final Slice rulings. The Slice is **VERTICAL_SLICE_OWNER_APPROVED_WITH_PRESENTATION_DEBT**. Full-game production is not implemented or finally approved.

## 1. Authority and preserved boundaries

| Source | Actual read-only location / use |
| --- | --- |
| Final Canon | `C:/Users/surlw/.codex/.chatgpt-projects/g-p-6a8cfbb439748191ac8fac094112d31b/SURLHWA_CANON` |
| Production Architecture | `C:/Users/surlw/.codex/.chatgpt-projects/g-p-6a8cfbb439748191ac8fac094112d31b/SURLHWA_ARCHITECTURE` |
| Current owner rulings | [Slice approval](VERTICAL_SLICE_OWNER_APPROVAL.md) and [approval minimization policy](OWNER_APPROVAL_POLICY.md) |
| Explicit unfinished presentation | [PRESENTATION_DEBT.md](PRESENTATION_DEBT.md) |
| Accepted implementation/evidence | [Slice review](review-slice.html), [HG04 baseline](HG04_OWNER_BASELINE.md), [completion ledger](SLICE_COMPLETION_PROGRESS.md), retained evidence/build manifests |
| Frozen RC1 | `D:/univ/surlhwa/Surlhwa`; comparison and selective content provenance only. No modification, overlay copying, cleanup or index changes. |

Core authority chapters: Canon 00/01/02, 10–12, 14/15, 20–23, 30–33, 40/41, 50–57, 60–62, 70–73 and 91; Architecture 02–05, 10–13, 20/21, 30/31, 40/41, 50/51 and 90/91. Exact adoption work must consult the relevant source fields and original hashes, not just this plan. Historical Quixote/MEM-03 Slice wording in Architecture 40/51 is superseded by the approved Prince excerpt. Architecture's provisional 3D-room interpretation and unresolved camera comparisons are superseded by C1; they do not authorize reopening those directions.

- Preserve pixel-art dominance, shallow selective 2.5D, fixed mildly elevated view, exposed snowy exterior, warm/cold contrast, U/ㄷ counter, bartender inside and guests outside, grounded shared scale. A/B/C are reference-only.
- Preserve WHAT / HOW / QUALITY / FIT / KNOWLEDGE / SUPPLY as distinct truths. No requested recipe, UI selection, animation or score creates the drink.
- Preserve original liquid amounts and exact ingredient identities. Version all new tuning and justified recipe conversions; display rounding never mutates amounts.
- Preserve seven ACT identities, important encounters and one common ending. Failure may change a response; it does not erase the guest or force perfect service throughout the campaign.
- Keep Slice saves and campaign saves separate. The approved Warmth excerpt grants no ACT, memory, prior Prince success or invented choice.
- Treat final aesthetics as open where the owner says so. Fix objective clipping, illegibility, blocked controls, hidden-information leaks and broken layout; do not block expansion merely because the current Codex is aesthetically unfinished.
- No broad new quest/economy framework, full 102-recipe import, automatic RC1 save import, mandatory future revisits, new original cocktails, cloud saves or platform expansion in this plan.

## 2. Recommended sequence and package size

Recommend **representative methods, then one complete ACT, then staged campaign growth**. A methods-only expansion delays proof that learning and story work together. Bulk authoring all seven ACTs first would lock content against unproven Float/Shake, save and knowledge assumptions. The recommended order limits rework while keeping a playable path at every internal integration checkpoint. These checkpoints do not interrupt the owner.

| Wave | Packages / exit | Dependency and boundary |
| --- | --- | --- |
| 0 | P00 content/pacing decisions, P01 selected migration, P02 campaign/save foundation | Author concrete content and D09 candidates, validate Canon coverage and escalate only a demonstrated policy exception. Bundle subjective judgment into GATE 2 where feasible. No mass content conversion. |
| 1 | P03 Build → P04 Shake → P05 Float | One representative adopted recipe per new method. Reuse Stir/liquid authority; each has its own technical evidence; collect feel questions in GATE 2, without separate method approvals. |
| 2 | P06 learning/ordinary guests → P07a complete ACT 1, integrated with P08 memories | Fresh campaign start, Prince first/follow-up/closing, fair recovery and authored farewell. Bundle with current UI/audio/music/art/animation and stop once at GATE 2. |
| 3 | P07b ACT 2; P07c ACTs 3–4; P07d ACTs 5–6; P07e ACT 7/common ending | After GATE 2, implement these packages autonomously with internal technical checks. Memory and save extension ship with the owning ACT. No per-ACT owner gates. |
| Presentation | P09 UI; P10 art; P11 animation; P12 audio/music | Develop representative samples within accepted direction, inspect them internally, then expand in bounded batches. Include current presentation in GATE 2 and final presentation in GATE 3. No separate mandatory asset approvals. |
| Candidate | P13 full-campaign regression, native and performance evidence, GATE 3 | Exact candidate identities; close objective blockers and explicitly disposition remaining presentation debt. |

No fixed calendar or release recipe count is promised before P00 establishes workload and representative production rates. Estimate packages internally from concrete content/art lists. GATE 1 authorizes autonomous execution toward complete ACT 1, including the relevant P09–P12 presentation work. GATE 2 approval authorizes continuation through ACTs 2–7 and the final candidate. Package completion does not require fresh permission; major scope increases remain exceptions.

## 3. Existing code and intended extension points

Paths below are relative to the production worktree. Proposed new paths are planning targets, not claims that files exist. Inspect current interfaces again before each package; record any justified change in its local implementation plan.

| Current files / directories | Responsibility and proposed extension |
| --- | --- |
| `Production/Unity/Assets/Surlhwa/Domain/Brewing.cs`, `StirTechnique.cs`, `ServiceEvaluation.cs`, `CocktailCatalog.cs` | Actual decimal quantities, preparation evidence, recognition and evaluation. Add method-specific `BuildPreparation.cs`, `ShakeTechnique.cs`, `FloatLayers.cs` only as their representative content requires. Do not fork liquid authority. |
| `Domain/RecipeKnowledge.cs`, `SliceContent.cs`; `ContentIO/SliceContentLoader.cs` | Filtered knowledge and strict definitions. Extend typed facts/discovery policies and add campaign definition loading without duplicating catalog/knowledge state. |
| `Application/SliceSession.cs`, `Domain/SliceCheckpoint.cs` | Accepted two-encounter excerpt. Currently indexed encounters and deliberately false ACT/empty memory state. Keep that mode valid; introduce `Application/CampaignSession.cs` and `Domain/CampaignProgress.cs` for authored blocks/receipts. Extract only proven shared transactions, not a second gameplay implementation. |
| `ContentIO/SliceSaveStore.cs`, `SliceSaveCodec.cs` | Existing durable save behavior. Add explicit campaign DTO/codec and reuse the verified publication mechanism where compatible. Preserve the old Slice reader and namespace. |
| `Application/InputGate.cs`, `ModalStack.cs`; `Presentation/SliceInputAdapter.cs`, `SliceController.cs`, `SliceWorkbenchView.cs` | Input ownership, held-state cancellation, commands and visible state. Extend normal controls for each method; test seams never become production authority. |
| `Presentation/SliceUi.cs`, `CharacterView.cs`, `PixelCutawayStage.cs`, `SliceAudio.cs` | Functional UI, grounding, room and audio lifetimes. Refine/adapt these owners; do not create competing UI, camera or persistent audio managers. |
| `Production/Content/{Recipes,Ingredients,Guests,Encounters,Dialogue,Requests,Profiles}` | Existing authored JSON. Add `Campaign`, `Memories`, `Text/ko-KR` only with their first adopted content and validators. Generated bundles remain derived output. |
| `Production/Validation`, `Production/Tools`, `Assets/Surlhwa/Tests` | Source adoption, preservation, evidence and regression. New checks belong to affected domain/content/integration suites. Keep QA fixtures outside campaign progress. |

Each implementation package must first define the exact changed fields, command signatures, save version and tests from these boundaries. This roadmap intentionally does not invent final APIs or numeric gesture thresholds before representative design review.

Current implementation limits matter: `StirRecipe`, `ServiceSnapshot` and `ServiceEvaluator` are Stir-specific; evaluation currently requires Stir/strain and `DrinkRequest` holds one recommendation tag. P03 must introduce the smallest method-aware recipe/snapshot/evaluation contract, preserving the existing Stir result fixtures, before Build content can run. P04/P05 then add their own evidence variants. P06 must add Specific and authored any/all/exclusion requests and required-discovery policies; those architecture contracts must not be mistaken for already implemented features. Persisted old Slice results require compatible decoding as these types evolve.

## 4. Bounded work packages

### P00 Content and pacing design before implementation

Authority: Canon CAM-01–05, NIGHT-05, SG-01, LP-03, CAT-D05, KNOW-005–007; Architecture 03/20/21.

- [ ] Produce a campaign authoring matrix with one disposition for every `RC1-01` through `RC1-47`: source fields, preserved event/learning purpose, proposed ACT/block/encounter, ordinary spacing, supply/knowledge prerequisites, memory connection and change reason. Explicit split/merge mapping must preserve meaning; no silent omission or indefinite deferral of required main events.
- [ ] Propose pacing in authored blocks: listening, preparation, response, departure, a quiet interval and next encounter. Review an early, middle and late sequence, with ordinary guests serving real daily contexts rather than padding. Choose the night count after reading these sequences; exact 18 nights and 47 new runtime encounters are not mandates.
- [ ] Write the Prince D09 decision table from cumulative interpretation and service history, including mixed outcomes and absent records. It must permit failure, preserve two valid farewells and explain the MEM-01 chair/star variant fairly. Prepare concrete candidate conditions and examples for the bundled GATE 2 review; do not adopt strict 3/3 or invent thresholds in this roadmap. A reversible preview is not final Canon. If an unresolved meaningful product choice must be settled before a reviewable ACT 1 can be built, document that exception and bundle it with all other known subjective decisions; keep independent work moving.
- [ ] Produce a launch-candidate recipe/adoption list and learning map. Preserve Seolya and Manwol as Signature Originals. Consider the existing nine campaign recipes as traceable candidates, not an automatically approved release list. No bulk 102-recipe import.

**Deliverables:** proposed `Production/Planning/CAMPAIGN_AUTHORING.md`, `PRINCE_FAREWELL_POLICY.md`, `CATALOG_SCOPE.md`, with source crosswalk tables. These files are future package outputs; this request does not author the final schedule or farewell formula.

**Technical exit:** complete 47-row disposition and seven-ACT coverage, learning/supply feasibility and documented reversible authoring choices. Pacing and D09 are reviewed with the complete ACT 1 package at GATE 2 unless a demonstrated exception makes an earlier bundled decision necessary. Routine recipe selection/migration within scope proceeds autonomously. Unverified external recipes block their adoption, not all planning.

### P01 Recipe, ingredient and cocktail adoption

Depends on selected P00 representatives. Authority: Canon 20/30–33, Architecture 03/20/21.

Use a small first set: retain approved Stir recipes; propose **Negroni for Build, Gin Fizz for Shake and Manwol for Float**. These are planned bounded representatives, not claims of new external verification. After GATE 1, adopt them autonomously when source, gameplay and migration checks pass; no per-recipe owner approval is required. Hanky Panky, French 75 and Last Word are later campaign-linked batches after selection and evidence. Yeomyeong/Janseol remain historical prototypes.

- [ ] For each adopted item, record source/hash, old literal ID, new destination, six-category migration disposition, exact units/conversion, preparation, supply, disclosure policy, rights and review state. Verify real cocktail facts against the official individual source at adoption time; separate documented game approximations from external facts.
- [ ] Preserve identity distinctions such as Gin/London Dry Gin, Whiskey/Rye, legacy/Sweet Red/Dry Vermouth, Syrup/Simple Syrup and fruit/juice. Do not rename `recipe_test_stir` merely for style. Do not turn approximate dash/splash values into universal conversions.
- [ ] Validate supported actions, capacities, mandatory/optional garnish and location, ingredient availability and achievable quantities before content publication. Unsupported Drop, crushed ice, multiple garnish or composite preparation is a scoped dependency, not a silent substitute.
- [ ] Check all retained historical/selected catalog records have a disposition, and each launch recipe has source → definition → supply → learning → normal preparation → service evidence.

**Exit:** selected JSON and source ledger pass strict schema/reference/quantity checks. A recipe enters the campaign only when its method package passes. Future IBA catalogue membership does not grant knowledge, supply or implementation status.

### P02 Campaign progression and Save/Continue foundation

Depends on P00 block/identity design; precedes actual campaign grants. Authority: Architecture 03–05, Canon 71/72.

- [ ] Add ordered ACT blocks, explicit required beat IDs, bounded ordinary pools/fallbacks and stable occurrence IDs. Save the actual selected guest before entry; Continue cannot reroll the pool. Keep one command order and one state writer.
- [ ] Define campaign save DTOs for selected plan/content revisions, consumed line IDs, choices, original service outcomes, knowledge receipts, supply grants, memory variants and ending stage. Separate ServiceCommitted from EncounterCompleted.
- [ ] Retain two validated save generations, single-writer publication, checksum/schema/ID validation and surfaced errors. Reject unsupported future schema without silently rolling back; preserve corrupt sources during recovery. Version any migration and retain original files.
- [ ] Test interruption at line/fact, selection, service, result/farewell, departure/memory and ending boundaries; duplicate/late callbacks, delayed/failed writes, content aliases and split/merge dispositions. Rehydrate committed outcomes without re-evaluating them under new tuning.

**Continue contract:** same selected guest and durable next unread line; mid-brew returns to empty tools with learned facts intact; committed service resumes reading without another drink; completed departure restores memory and next cursor atomically; ending reached/read remain separate. Settings remain independent.

**Exit:** campaign namespace round trips with no Slice mutation or fabricated history. Test fixtures remain disclosed fixtures. No automatic RC1 PlayerPrefs importer, Slice-to-campaign promotion or mid-pour save restoration.

### P03 Build representative

Depends on P01 Build adoption. Extend `Brewing.cs`, preparation evaluation and normal workbench controls; add `BuildPreparation.cs` only if a distinct cohesive unit is needed.

- [ ] Prove direct accumulation in the serving glass, actual ice, authored order, GentleStir where required and mandatory/optional garnish semantics with the adopted representative.
- [ ] First pin the current Stir evaluation/serialized snapshot as regression fixtures; replace unconditional Stir/strain checks with explicit method-specific preparation and evidence evaluation. A Build recipe must not fail for lacking mixing-glass Stir, nor receive an invented quality score. Version and validate changed content/save representations.
- [ ] Test conservation/overflow/cancel, wrong container/action, omitted/incorrect garnish, correct WHAT with wrong HOW/FIT and normal failure progression. Build completion ends preparation; it never grants an automatic technique win or a detached timing game.
- [ ] Capture the actual Player path and complete one Windows-native representative service, including recognition, reaction and supported departure. Review visible liquid/contact and ease of use.

**Technical exit:** real Build preparation and independent evaluations, no Stir regression caused by shared changes. Continue bounded recipe integration and include method feel in GATE 2; no separate method approval.

### P04 Shake representative

Depends on P01 Shake adoption; use P03's reviewed control conventions. Add `ShakeTechnique.cs` and a shaker role with explicit open/closed state and actual contents.

- [ ] Define short, readable direction/reversal/rhythm input, supported accessibility range and versioned quality thresholds. RC1's eight-note timing is reference, not a compulsory rhythm game.
- [ ] Bind evidence to the closed shaker's current batch; reject empty/open/wrong-target actions. New liquid invalidates prior evidence. Transfer provenance cannot duplicate good quality.
- [ ] Preserve authored Strain versus PourFromContainer, actual amounts/ice policy and later Splash/Pour steps. Do not infer dirty-dump behavior from a button name.
- [ ] Test absent/wrong/extra input, cancellation/focus/modal release, batch change, partial transfer and different frame sampling; complete one native representative service and retain it for the bundled GATE 2 feel review.

**Exit:** Shake is visibly and mechanically distinct, with normal recognition/service evidence. No animation-only completion or reuse of Stir grade.

### P05 Float representative

Depends on P01 Manwol adoption. Add `FloatLayers.cs` and an explicitly selected layer-aware transfer policy; preserve uniform Stir transfer for its existing context.

- [ ] Keep actual per-ingredient ml authoritative while recording layer order and each adjacent boundary's mixing state. Define how interrupted/partial transfers preserve layers and what later mixing does; check the policy internally against Canon before implementation; escalate only a meaningful unresolved product choice.
- [ ] Let accepted flow and authored height/tool-stability interaction determine boundary evidence. The first layer is ordinary addition. A good last boundary cannot repair a mixed lower boundary, wrong order or missing amount.
- [ ] Test clean/partial/mixed boundaries, wrong order, overflow, stop/resume, partial transfer and post-layer mixing. Treat simplified layering as a game model, not a density simulation.
- [ ] Complete native Manwol preparation/service through normal controls; inspect layers at 720p/1080p and assess precision/fatigue internally before ACT 1 learning integration. Bundle Manwol/Float feel with GATE 2.

**Exit:** usable Float and fair feedback; no detached success meter, auto-filled layer or imported RC1 flow threshold without tuning evidence.

### P06 Ordinary guests and knowledge progression

Depends on P00 learning plan, P01 selected catalog, P02 receipts and the method used by each encounter. Authority: Canon 22/23/60–62; Architecture 02–04/20.

- [ ] Adopt small ordinary encounter groups serving distinct roles: daily life, practice, actionable discovery clues and breathing space between special visits. Preserve postman/repairer/snow worker/station worker source roles where selected; retain charcoal anonymity and limited familiarity without a relationship-stat system or invented long routes.
- [ ] Add typed fact grants on consumed lines/choices and explicit knowledge projections. Quantity implies its ingredient fact; full Partial does not automatically become Known. Supply unlock is independent from knowledge.
- [ ] Implement NormalRecognition versus RequiredDiscovery as authored policies. RequiredDiscovery needs target WHAT, FIT, HOW and QUALITY, preserves Partial on retry and uses a same-encounter recovery path. Already-Known content uses the authored fallback without downgrading knowledge.
- [ ] Map LEARN-SEOLYA/DRY/MANWOL/NEGRONI/MANHATTAN/GINFIZZ/HANKY/FRENCH75/LASTWORD into the reviewed schedule or an explicit source-preserving disposition. Manwol recovery must not overwrite the original Prince failure.
- [ ] Test fresh knowledge, all-facts-but-not-Known, wrong valid drink discovery, impossible supply, ordinary success/failure departure, required retry, known fallback and every hidden-information surface. Where a request promises alternatives, prove multiple normally supplied and learnable responses.

**Exit:** no outside-guide guessing, hidden-name leaks, fake knowledge or retry softlocks. Integrate ordinary guests autonomously; include voice variety, concise teaching and spacing in GATE 2, then the final campaign review at GATE 3.

### P07 Seven ACTs and common ending

Depends on P00 reviewed authoring, P02/P06 and relevant methods. Each package includes its ordinary spacing, supply/learning, response/reading, memory transaction, Continue and native input changes. Do not batch-import all dialogue first.

| Package | Fixed ACT content to preserve | Acceptance emphasis |
| --- | --- | --- |
| P07a ACT 1 — 낭만의 문, 어린왕자 | First questions, incomplete understanding, continued journey; first Manwol/Float encounter, Warmth follow-up and Clarity closing as source comparisons. Apply the reviewed D09 policy. | Genuine campaign start; choice/history distinctions, valid open/quiet farewells, missing-history neutrality, failed first service plus separate Manwol recovery. The approved excerpt is not promoted into a completed ACT. |
| P07b ACT 2 — 사랑의 온도, 춘향과 몽룡 | Two perspectives, departure, Chunhyang's silence, reunion sharing one drink. | Two actor identities/anchors, one encounter/request/brew/service; no duplicate cup or invented speaker assignment. Different silence and speech remain meaningful. |
| P07c ACT 3 — 모험이라는 병, 돈키호테 | Declaration, others' laughter versus conviction, ideals remaining and being alone. | No drunken caricature; the old visual fixture provides no narrative completion. |
| P07c ACT 4 — 생각이 사람을 죽일 때, 햄릿 | Question, cup raised/lowered, stillness and unresolved action. | Local stillness/silence without idle-trigger progression locks or diagnosing/correcting the character. |
| P07d ACT 5 — 거래의 대가, 파우스트 | Desire for knowledge, signed contract, lateness and cost. | No invented rescue/condemnation or secret recipe gate to the main ACT. |
| P07d ACT 6 — 돌아오지 않을 각오, 심청 | Emotional before-state, concise self-determined resolve, calmer return after meeting her father. | Distinct visits; no reward score for sacrifice. |
| P07e ACT 7 — 그래도 돌아가는 인간, 오디세우스 | Wandering silence, recognized return, staying longer, then departure into the bartender/bar's time. | Correct identity, earned memory, common ending with history-sensitive small callbacks. |

- [ ] For each package, complete its Canon-checked crosswalk rows and source-to-launch text ledger: speaker/narration/request/choice roles, preserved original, edited wording, reason and review state. The Canon's existing text is source material; the owner permits editorial polish, not silent source replacement.
- [ ] Verify success and ordinary service failure continue; adopted RequiredDiscovery exceptions stay explicit. No hidden clicks/waiting, trophy counts or perfect-service chain blocks the main story.
- [ ] Include a fresh start-to-package path and checkpoint resumes using actual transactions. Use developer checkpoints only as labelled isolated fixtures, never evidence that a full campaign was played.
- [ ] After P07e, prove required-main-beat completion → EndingReady → entered/reading → read complete → stable coda/Continue. The ending is one common ending, not another guest or an infinite generated next night. Missing Prince star/success remains absent; ending variants never fabricate it.

**Technical exit per package:** authored events preserved, no silent missing RC1 row and complete affected evidence. ACT 1 goes to the single bundled GATE 2 review. After approval, ACTs 2–7 continue autonomously; retain their emotional-sequence review material for GATE 3 unless a true policy exception arises. Full seven-ACT closure waits for P13.

### P08 Memories and bar evolution

Starts with P07a; ships incrementally with each ACT. Authority: Canon MEM-R01–03/MEM-01–07, Architecture 02/03/05/11.

Introduce `Domain/MemoryProgress.cs` and a presentation binding only when the first real campaign grant is ready. Memory receipts store ID/variant/source occurrence/transaction alongside encounter completion. A prop missing from the scene cannot remove the earned receipt; changing current rules cannot retroactively re-evaluate old grants.

| Canon group | Meaningful spatial trace |
| --- | --- |
| MEM-01 Prince | Empty chair and small star; exact production variant condition waits for reviewed D09. |
| MEM-02 Chunhyang/Mongryong | Swing and adjacent empty space; one shared memory. |
| MEM-03 Quixote | Windmill fragment. |
| MEM-04 Hamlet | Stage fragment, skull, restrained light/shadow. |
| MEM-05 Faust | Faint circle, broken glass and small red light. |
| MEM-06 Sim Cheong | Half-submerged lotus and slow ripples. |
| MEM-07 Odysseus | Anchor and ship fragment. |

- [ ] Author unobscured anchors within C1 and review early/middle/full accumulated room states. Empty places must not become a trophy checklist. Ordinary guests receive no invented formal special-memory grants.
- [ ] Test exactly-once grant at completed departure, failure-service narrative completion, missing presentation fallback, duplicate callbacks and Continue before/after grant. Test all seven groups for visual overlap and performance using clearly labelled stress fixtures.
- [ ] Keep optional reactions separate from main progression. Future revisits and memory transformations remain preserved future content with separate scope; no click/idle labor or completionist ending lock.

**Exit:** authentic earned traces survive reload and remain legible; bundle ACT 1 memory presentation at GATE 2 and the accumulated room at GATE 3 without spatial redesign.

### P09 Dedicated UI and Codex refinement

Depends on P06 disclosure states and representative four-method controls. PD01/PD02 remain open until explicitly dispositioned in GATE 2 or GATE 3. This is not a separate UI owner gate.

- [ ] Develop one bounded UI design proposal using real Korean dialogue, ingredient quantities, long names, Unknown/Partial/Known, unavailable supply and an expanded catalog. Inspect layout/card composition, spacing, chrome, hierarchy and color/material treatment internally before bounded implementation. Bundle owner judgment with GATE 2/GATE 3; pause earlier only for a demonstrated artistic-direction exception.
- [ ] Implement within the existing UI/knowledge projection and modal ownership. Shared styling may be extracted from `SliceUi.cs` when required; do not change toolkit or introduce a second UI input system for cosmetic convenience.
- [ ] Verify no clipped essential text/overlap, readable contrast and text scaling, navigable targets, non-color focus, safe restoration after selected-object destruction/component disable, no pointer blocking decoration and no hidden-information leakage.
- [ ] Capture actual 720p/1080p surfaces and altered pointer/keyboard interactions; include current visual and interaction treatment in GATE 2 and final polish in GATE 3. Functional checks alone cannot change HG05's visual status.

**Technical exit:** objective usability passes and reviewable captures are retained. Work continues without a separate UI approval. Debt closes only through a named ruling in the bundled gates. Aesthetic iteration stays bounded to UI; C1 and HG04 are not reopened.

### P10 Environment, character and tool art production

Depends on C1 and selected cast/tools/memory anchors. Covers PD03/PD04/PD06/PD10.

- [ ] Produce a licensed/source-tracked art inventory: retain, replace or reauthor each provisional asset. Reference art and generated placeholders do not become release assets merely by appearing in an accepted Slice.
- [ ] Develop and internally assess one representative art family in the existing C1 composition: snow ground/accumulation/shadows, warm wood/lighting, bartender/ordinary/special character, one method tool and one memory. Preserve counter shape, exposed exterior and shared physical scale.
- [ ] Expand to the selected cast, paired scene, props, all method tools and seven memories after internal sample checks establish consistency with C1. GATE 2 reviews the current ACT 1 family; after its approval expand autonomously within that direction. Use actual container state for liquid/ice/garnish visibility; material depth remains selective.
- [ ] Validate real foot/seat/hand contacts, pixel stability, alpha/depth/occlusion, faces/cup/UI safe areas and early/full room captures at both resolutions; check asset rights and hardware costs before final adoption.

**Technical exit:** source-cleared, internally checked art family and batch-specific evidence. Bundle current family presentation at GATE 2 and final art at GATE 3; no per-batch approval. No globally 3D room, strong perspective, realistic material drift or unnecessary prop density.

### P11 Animation and tactile refinement

Depends on P10 anchor/art contracts and each method's accepted interaction. Covers PD05/PD11 and timing dependency for PD07.

- [ ] Author entry/approach/sit/listen/react/stand/depart for bartender, ordinary and special families; add paired shared-cup handling and character-specific restraint such as Hamlet's stillness. Check one family internally before expanding clips, with current motion included in GATE 2 and final motion in GATE 3.
- [ ] Refine tool pickup/alignment/pour/transfer/ice/garnish/return motion and feedback. Preserve actual accepted quantities, batch evidence and release/regrip behavior; no animation event awards technique, memory or service completion.
- [ ] Test missing/cancelled clips, interrupted visits, one missing paired actor, seat release and normal fallback without blocking story. Align foot contacts to the audible step cadence; do not demand a fixed cue rate independent of motion.
- [ ] Review motion and input response in the actual Player. Native reruns target changed gesture boundaries; unchanged broad HG04 suites remain historical accepted evidence.

**Technical exit:** objective cancellation/lifecycle checks pass and grounded movement/control response are reviewable. Bundle subjective judgment with GATE 2/GATE 3, not individual animation approvals.

### P12 Audio polish and music composition

Depends on P11 contact timing, P07 scene contexts and P08 memory moments. Covers PD07/PD08/PD09; HG06 direction stays accepted.

- [ ] Replace/tune footsteps from light 타박타박 toward grounded 뚜벅뚜벅. Compare body/weight, transient, low-mid presence, surface, cadence and character/world integration in the actual bar. Compare candidates at matched loudness and assess final mix/loudness as a distinct criterion within the same bundled review; louder alone is not heavier.
- [ ] Refine ambience and physical action cues, variation/fatigue, transitions, silence, dialogue ducking and settings. Accepted movement/flow owns loop activity; release/modal/focus/cancel/dispose stops it, and a missing clip never blocks progression.
- [ ] Author a sparse music/motif cue map with narrative purpose, start/stop/fade and silence boundaries. Compose and clear rights for selected cues; review audition in context before recording final arrangements. No fixed track count, instrument palette, all-character themes or voice-acting mandate is assumed.
- [ ] Test clipping, loop seams, concurrency, stale loops, mute/settings persistence and missing-device/clip fallbacks; listen through representative ACT and ending passages. Technical signal checks do not close owner audio polish debt.

**Technical exit:** lifecycle/signal checks pass and in-context listening evidence is packaged. Current audio/music, including any still-deferred composition, is disclosed at GATE 2; final footsteps/mix/compositions are judged at GATE 3. No separate mandatory listening gate. A bundled ruling closes only its named debt scope, not all audio by association.

## 5. P13 Regression, native input and performance strategy

Evidence tiers remain Architecture 30's V1 domain, V2 static/content, V3 integration, V4 simulated production input, V5 Windows-native input, V6 actual renders, V7 named-device measurements and V8 human rulings. Keep them separate in manifests. Current accepted 93/93 Unity, 86/86 standalone, 18/18 Player and later 2/2 focus tests/16 UI captures are historical baseline results, not full-game totals or permission to rerun them now.

| Trigger | Required affected evidence |
| --- | --- |
| Quantity/method/evaluation changes | Domain conservation, revisions/provenance, invalid actions, frame/clock boundaries; affected normal Player service and one native representative per new method. |
| New recipe/guest/ACT | Strict schema/identity/source coverage, supported preparation, learning/supply/retry reachability; affected fresh flow, outcome variants and Continue checkpoints. |
| Save/schema/transaction change | Round trips and failure injection for changed checkpoints, old supported saves/aliases, future-schema rejection, two-generation preservation and no duplicate grants. |
| UI/input change | Modal/held/rearm/focus lifecycle and hidden-information tests; affected native controls and both-resolution captures. |
| Art/animation/audio change | Actual changed output, interruption/disposal, contact/silence checks and performance measurements for changed costs; relevant owner gate only. |
| Full campaign candidate | Complete main-beat/source matrix, fresh beginning-to-common-ending run via production input, key failure/discovery/Continue variants, retained plus targeted fresh native coverage and named-hardware performance report. |

Future native strategy extends coverage as the game expands; it does not reopen accepted HG04 closure. At minimum, prove each new method with a real complete service, paired encounter controls, required discovery retry, changed UI actions, major campaign Continue boundaries and ending transition. Full-campaign traversal may use clearly identified V4 automation plus targeted V5 runs; do not call it a native full campaign without actually performing that run.

For V5 retain executable/hash, one PID/HWND, timestamps, foreground owner, expected control, current client rectangle, native events, production observations/counts, liquid/trace/technique/result/departure. Before discrete actions re-query the verified HWND and convert client coordinates; a window move between actions may re-anchor. During a gesture stop on HWND/client-size/foreground/window-position changes. Unrelated physical input or focus change is **ENVIRONMENT_INTERRUPTED**; stop, retain separately and never silently refocus or repair with callbacks. Direct callbacks, simulated devices and test state injection are not native evidence. Stop at the first missing input boundary; do not repeat whole suites to diagnose one boundary.

Performance uses the accepted C1 view, not renewed A/B architectural experiments. Record CPU/GPU/VRAM/RAM/OS/driver/storage, build/source/content/tuning, resolution, power/VSync/cap, profiler overhead, warm/cold state, sample count and P50/P95/P99/worst. Measure four methods, long Korean/expanded Codex, paired actors, 0/1/7 memory groups, save/Continue and 20 lifecycle cycles. Start from Architecture 31's investigation budgets: 60 fps, P95 frame 16.7 ms, main/GPU each 12 ms, working-memory 1.5 GB, save main-thread 4 ms/transaction 100 ms, warm transition 250 ms and cold interactive 5 s. These remain hypotheses pending named-device acceptance, not certified minimum specs. Use its initial 60-second steady and ten-repeat transient sampling, recording justified deviations.

A failed evidence manifest must not retain misleading ready/approved fields; packaging must select passing, current-identity artifacts and mark superseded attempts. Extend freshness checks to full source/Player inventory, not executable-only checks. No new performance package, pooling or architecture shift without an observed bottleneck.

## 6. Review focus and stop rules

These cross-cutting cases are mandatory in their owning package, not deferred vague QA:

| Likely failure case | Owning package and expected behavior |
| --- | --- |
| Cancel/regrip, new liquid or partial transfer after valid technique | P03–P05: no ghost quantity/evidence; preserve lineage; require fresh technique when composition invalidates it. |
| Missing/failed historical service or repeated discovery | P02/P06/P07a: retain original outcome; no fabricated choice/star; same-policy retry and idempotent facts. |
| Two actors but one shared service, with an absent presenter | P07b/P08/P11: one cup/outcome/completion; independent actor cleanup, no stranded seat or duplicate memory. |
| Expanded/long Korean UI with hidden recipe facts and destroyed focus | P06/P09: readable controls, no identity leak, safe focus recovery and single input consumption. |
| Memory/departure/ending save interruption after content revision | P02/P08/P07e: atomic durable receipts, next unread position, no duplicate grants, no unintended re-evaluation. |

Stop the affected package on duplicated authority, impossible normal supply/learning, lost core event, corrupted/ambiguous saves, native evidence mislabelling, serious usability defects or reproducible performance stalls. Fix the narrow cause and rerun affected evidence. Do not reclassify these as presentation debt. Aesthetic dissatisfaction with still-open debt does not undo Slice approval; a proposed spatial architecture change still needs new owner approval.

## 7. Three owner gates and exception handling

The owner's [approval minimization policy](OWNER_APPROVAL_POLICY.md) supersedes the earlier nine proposed expansion gates and per-package permission requirements. HG01–HG08 remain historical Slice rulings; they do not become mandatory repeat gates for every new asset or ACT.

| Gate | Single review package | What follows approval |
| --- | --- | --- |
| GATE 1 | Production Expansion Plan. OWNER_APPROVED at 6a6637e. | Autonomous work through complete ACT 1; no separate P00, recipe, method, migration, save or technical approvals. |
| GATE 2 | Complete ACT 1 Production Milestone: campaign pacing, Little Prince ACT, Manwol/Float, memory presentation, current UI, current audio/music, character/environment presentation and overall SURLHWA identity. Bundle all currently known subjective decisions, including any other new method feel questions. | Continue ACTs 2–7 and presentation refinement autonomously unless a true exception occurs. |
| GATE 3 | Final Release Candidate human signoff: whole campaign, final presentation, evidence limits and remaining debt disposition. | Owner release acceptance; no claim that technical PASS substitutes for it. |

Routine deterministic gameplay, Canon wiring, approved-architecture choices, data migration, save/progression, ordinary guests, QA/regressions, measured performance fixes, packaging, objective UI/audio-lifecycle fixes, minor reversible tuning and technical details require no owner approval. Make reasonable reversible decisions and document them. Internal technical failures require diagnosis and repair, not automatic owner escalation.

Only stop for owner input when: (1) Canon does not determine a meaningful product choice; (2) viable alternatives materially change artistic experience; (3) approved narrative/theme would change; (4) approved visual/audio direction would be invalidated; (5) migration is destructive or irreversible; (6) the working implementation no longer feels like SURLHWA; or (7) major scope growth is necessary. Do not ask trivial preferences or add mandatory gates without demonstrated reason.

If an exception genuinely requires judgment, prepare one concrete package containing all currently known subjective decisions, alternatives, consequences and the exact exception reason. Use the next planned gate when safe; otherwise pause only dependent work and continue independent authorized work. Do not silently settle an unresolved product choice, or use provisional previews to claim final Canon/art acceptance.

At GATE 3 classify outstanding items as PASS, VALIDATION_DEBT, HUMAN_SIGNOFF_REQUIRED, DEFERRED_PRESENTATION or OUT_OF_SCOPE. Objective defects remain blockers for the affected deliverable and are fixed autonomously where possible. Presentation debt closes only by explicit named acceptance or deferral within a bundled ruling. Permission to continue is not blanket final presentation approval.

## 8. Coverage and handoff

| Requested scope | Package |
| --- | --- |
| Shake / Build / Float | P03–P05 |
| Seven ACTs and pacing re-authoring | P00, P07a–e |
| Ordinary guest expansion | P06 and each P07 batch |
| Recipe / ingredient / cocktail migration | P01 |
| Discovery / knowledge progression | P06 |
| Memory and bar evolution | P08 |
| Save / Continue expansion | P02 and each owning feature |
| UI / Codex visual refinement | P09 |
| Environment and character art | P10 |
| Animation | P11 |
| Audio polish / music | P12 |
| Regression / native / performance | P13, affected checks per package |
| Final human gates | GATE 1, GATE 2, GATE 3 only; documented exceptions under the owner policy |

Planning decisions and costs: staged methods/ACT batches add internal verification points but contain rework; bundling owner decisions reduces interruptions while leaving some reversible presentation work provisional until GATE 2/3; source-preserving re-authoring adds ledger work but prevents silent narrative loss; keeping Slice as a separate mode preserves acceptance evidence but requires explicit shared-code regression; deferred aesthetics permit continued production while retaining a real later polish workload. No calendar, recipe count, D09 formula or final mix target is invented to hide those decisions.

**Handoff:** GATE 1 is approved. Begin P00 and continue autonomously through the complete ACT 1 milestone, including its integrated presentation review material. Do not request separate package approvals. After GATE 2 approval continue ACTs 2–7 autonomously unless one of the seven true exceptions occurs. Execution is now authorized through Gate 2 under GATE1_OWNER_APPROVAL.md.
