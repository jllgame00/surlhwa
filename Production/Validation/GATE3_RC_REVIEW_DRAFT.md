# Gate 3 release-candidate review draft

Prepared 2026-11-11 while root-owned validation continues. This is an evolving
evidence index and review outline, **not a readiness declaration, final technical
PASS, release approval or owner presentation ruling**. The inspected committed
baseline is `e558504`; the P09-P13 candidates described below are current
worktree changes. Their eventual commit and final build identity remain to be
recorded together after validation and preservation review.

This document is the new Gate 3 package preparation path. The historical
[FINAL_REVIEW_PACKAGE.md](FINAL_REVIEW_PACKAGE.md) belongs to the earlier Pass 1
review and must remain unchanged. If a frozen review page and manifest are
produced later, use new unique names such as
`review-gate3-rc-20261111-01.html` and
`Manifests/gate3-rc-20261111-01.json`; these are proposed destinations, not
artifacts asserted to exist. Preserve superseded builds, failed attempts and raw
evidence under their original names.

## 1. Authority and review scope

The [project vision](../../Docs/00_PROJECT_VISION.md),
[game design](../../Docs/01_GAME_DESIGN.md),
[bartending design](../../Docs/11_BARTENDING_2_0_DESIGN.md),
[rebuild rules](../../Docs/06_REBUILD_RULES.md), pinned Canon/Architecture and
current authored content remain authoritative. The
[production expansion plan](PRODUCTION_EXPANSION_PLAN.md),
[owner approval policy](OWNER_APPROVAL_POLICY.md),
[Q1 confirmation](Q1_OWNER_CONFIRMATION.md) and
[Gate 2 approval](GATE2_OWNER_APPROVAL.md) define the scheduled finishing work
and the single bundled Gate 3 presentation ruling.

Retain the accepted pixel-first winter exterior, warm bar, U-counter, selective
depth, Q1 camera endpoints, 1.12 world-view cast scale and exact approved Prince
seated pose. The familiar bar accumulates memories; the final pass does not add
an economy, quest framework, answer ranking, new cocktail catalogue, future
guest ending or completed-ship reward.

Technical evidence can establish source identity, correct state, disclosure,
contacts, visibility and lifecycle. The owner retains visual, motion, tactile,
audio, reading/pacing and emotional approval. No automated result closes a
presentation-debt row.

## 2. Committed campaign and common ending

The current authored campaign is `through_act_07`: 47 scheduled encounters over
20 nights, nine recipes, 22 ingredients and seven earned memories. These are the
current expansion scope, not a rewrite of the earlier bounded-demo milestone.
The [ACT7 content provenance](ACT7_CONTENT_PROVENANCE.md),
[ACT7 implementation plan](ACT7_ENDING_IMPLEMENTATION_PLAN.md) and
[runtime checkpoint](ACT7_RUNTIME_CHECKPOINT.md) pin the adopted source and
explain compatibility with the accepted prefix.

| Committed unit | Implemented scope and retained evidence |
|---|---|
| ACT2 — `184e1af` | Distinct Chunhyang/Mongryong solo and paired visits, one actual shared service, Hanky Panky discovery and earned swing/empty place. [Runtime record](ACT2_RUNTIME_CHECKPOINT.md), [cast provenance](ACT2_CAST_PROVENANCE.json). |
| ACT3 — `4790c33` | Quixote visits, earned windmill fragment, remembered Prince chair reserved for later guests, prior-state continuation. [Runtime record](ACT3_RUNTIME_CHECKPOINT.md), [content provenance](ACT3_CONTENT_PROVENANCE.md), [cast provenance](ACT3_CAST_PROVENANCE.json). |
| ACT4 — `33de7c7` | Hamlet, French 75 discovery/retry and distinct Shake finish, earned stage/skull/shadow. [Runtime record](ACT4_RUNTIME_CHECKPOINT.md), [content provenance](ACT4_CONTENT_PROVENANCE.md), [cast provenance](ACT4_CAST_PROVENANCE.json). |
| ACT5 — `2541b62` | Faust's authored contract/cost visits and receipt-driven circle, broken glass and contained red point, with no room-wide horror or economy expansion. [Runtime record](ACT5_RUNTIME_CHECKPOINT.md), [content provenance](ACT5_CONTENT_PROVENANCE.md), [cast provenance](ACT5_CAST_PROVENANCE.json). |
| ACT6 — `3c25ba0` | Sim Cheong visits, Last Word discovery and actual shaker-Strain preparation, early discovery/fallback, lotus and slow ripples. [Runtime record](ACT6_RUNTIME_CHECKPOINT.md), [content provenance](ACT6_CONTENT_PROVENANCE.md), [cast provenance](ACT6_CAST_PROVENANCE.json). |
| ACT7 — `72ee053` | Four encounters append slots 43-46, Odysseus and the earned anchor/incomplete hull, then the persistent common ending. [Runtime record](ACT7_RUNTIME_CHECKPOINT.md), [content provenance](ACT7_CONTENT_PROVENANCE.md), [cast provenance](ACT7_CAST_PROVENANCE.json). |

Recipe recognition, preparation, quality and request FIT remain separate.
Knowledge follows authored learning/discovery; supply does not grant Known.
An unsuitable final drink still permits the authored departure, memory and
ending. Main completion does not require all recipes Known or every service to
succeed. Earlier failed attempts remain historical service records.

Natural final departure publishes 47 completed visits, MEM07
`anchor_ship_fragment` and `EndingReady`. Explicit reading proceeds through
`EndingEntered` and `EndingReading` to `Complete`: eight common lines in source
order and one saved-choice Prince recall after `text_rc1_0002`. Keep/seek/absent
choice selects the existing authored recall. The Prince does not reappear as an
actor. The saved cursor and consumed receipts prevent replay of read lines;
completed Continue shows the coda without another completion write. The ending
clears guest/request/visible serving presentation while retaining historical
service truth. No new save schema, encounter 48 or night 21 is introduced.

The committed ACT7 checkpoint records 726/726 Editor cases and nine actual
simulated-input Player runs, including Open/Quiet, final FIT failure and
ending/farewell/completed reloads. Their exact identities and limits are in
[ACT7 runtime](ACT7_RUNTIME_CHECKPOINT.md) and
[the retained runtime summary](Evidence/act7-final-runtime-summary.json).
These results certify that checkpoint's bounded claims; they are not a fresh
full regression of the current final presentation candidate.

## 3. Current P09-P12 presentation candidates

| Package | Implemented candidate to review | Source, implementation and evidence entry points |
|---|---|---|
| P09 UI/Codex | Journal index/page hierarchy, aligned learned ingredients, separated preparation/finish, Known-only glass metadata, reading settings and safe layout, shared hold-control cancellation. | [Plan](P09_IMPLEMENTATION_PLAN.md), [CampaignUi](../Unity/Assets/Surlhwa/Presentation/CampaignUi.cs), [shared UI controls](../Unity/Assets/Surlhwa/Presentation/SliceUi.cs), [P09 tests](../Unity/Assets/Surlhwa/Tests/EditorIntegration/P09UiTests.cs), [hold tests](../Unity/Assets/Surlhwa/Tests/EditorIntegration/SliceHoldButtonTests.cs). |
| P10 environment/cast/tool surfaces | Separate `Bar_Q1_Final` with quieter snow/cap treatment, illustrated shaker shell, source-traced garnish/tall-glass/flute presentation and restrained later-memory edge surfaces. Existing room, cast contacts and MEM01 remain the anchors. | [Art plan](P10_ART_PRODUCTION_PLAN.md), [source inventory](P10_ART_SOURCE_INVENTORY.json), [new source manifest](../SourceArt/FinalProduction/assets.json), [builder](../Unity/Assets/Surlhwa/Editor/CampaignFinalArtBuilder.cs), [before](Manifests/p10-candidate-before.json)/[after](Manifests/p10-candidate-after.json) binding inventories. |
| P11 cast/motion/tactile | Grounded approach/standing turn/seat and stand/offscreen departure; independent paired pauses; observed planted-frame foot cues; quiet listening; one live ending-line 0007 empty-glass wipe/put-away; bounded tool contact/release and focus reveal. | [P11/P12 plan](P11_P12_IMPLEMENTATION_PLAN.md), [character motion](../Unity/Assets/Surlhwa/Presentation/CharacterView.cs), [paired presenter](../Unity/Assets/Surlhwa/Presentation/CampaignCastView.cs), [bartender gesture](../Unity/Assets/Surlhwa/Presentation/CampaignCharacterArt.cs), [motion tests](../Unity/Assets/Surlhwa/Tests/EditorIntegration/CampaignMotionTests.cs), [tool tests](../Unity/Assets/Surlhwa/Tests/EditorIntegration/P11ToolMotionTests.cs). |
| P12 audio | Accepted physical-action sound, bounded contact cues, grounded foot candidate, short Shake contact and sparse live-completion return music with cancellation/ducking lifecycle. | [Cue map](FINAL_AUDIO_CUE_MAP.md), [source/audio manifest](../SourceArt/FinalProduction/Audio/manifest.json), [authoring tool](../Tools/author_final_audio.py), [runtime](../Unity/Assets/Surlhwa/Presentation/CampaignAudio.cs), [audio tests](../Unity/Assets/Surlhwa/Tests/EditorIntegration/CampaignAudioTests.cs), [accepted-cue tests](../Unity/Assets/Surlhwa/Tests/EditorIntegration/CampaignAudioAcceptedCueTests.cs). |

Glass profiles are manually selected presentation through existing controls.
They do not choose a vessel from the requested recipe, change liquid identity,
teach untaught recipe facts or create new recipe/HOW authority. New memory
surface detail does not add a memory trigger, light, interaction or future form.
Retained source art is not relabelled as newly authored or finally approved.
The source inventories retain bounded reuse and older public-release provenance
caveats; technical integration does not create a blanket rights clearance.

## 4. Current evidence, with its actual limits

| Evidence | Observed result / boundary |
|---|---|
| [Initial combined RED](Evidence/p09-p12-presentation-red-01.xml) | 78 cases: 23 passed, 55 failed. Historical behavioural RED is retained. |
| [Focused integration](Evidence/p09-p12-integration-02.xml) | 162/162 passed. This is focused integration, not the later whole-candidate suite. |
| [Full Editor run](Evidence/final-editor-full-01.xml) | 852/853 passed; the failing `SliceIntegrationTests.InputSystemPourStopsOnReleaseModalAndFocusLoss` encountered a destroyed `SliceInputAdapter` reference. Do not publish this run as a clean aggregate. |
| [Subsequent correction subset](Evidence/final-input-font-correction-01.xml) | 79/79 passed after input/font correction. This does not retroactively change the preceding full-run result. |
| [Full Editor attempt 02](Evidence/final-editor-full-02.xml), [log](Evidence/final-editor-full-02.log) | 846/859 passed, 13 failed. The obsolete wrapper supplied `-nographics`; the log records `NullGfxDevice`. Twelve GPU depth cases therefore read zero pixels and one shelf case received `RenderTexture.Create failed`. This is a retained validation-environment/invocation failure, not evidence of thirteen gameplay regressions. |
| [Graphics-enabled full Editor attempt 03](Evidence/final-editor-full-03.xml), [log](Evidence/final-editor-full-03.log) | **859/859 passed**, zero failed/skipped, natural exit 0. The log identifies NVIDIA GeForce RTX 3070. The corrected runner uses the working graphics-enabled Editor test path; this clears the final aggregate test gap for this source checkpoint, not final Player, performance or owner approval. |
| [Earlier tools log](Evidence/final-tools-04.log) / [current tools log 06](Evidence/final-tools-06.log) | Earlier run reports 157 tests; current run reports **175 tests and `OK`**. Preserve the raw PowerShell output wrapper; do not infer a process exit code solely from its stderr formatting. Media-clock fixtures in the tool log are tool tests, not actual campaign Player runs. |
| [P13 guard tests](Evidence/p13-headless-green-01.log) | Reports 13 tests and `OK`; synthetic summary/guard tests are not Player benchmarks. |
| [Foreground summary RED](Evidence/p13-foreground-red-01.log) / [GREEN](Evidence/p13-foreground-green-01.log) | Three missing foreground-summary contracts were reproduced; the completed focused suite passes 16/16 with exit 0. Background-only input is incomplete, separate foreground metrics exclude background stalls, and delayed GPU timing waits for stable context after focus regain. Launcher syntax parsed successfully; no Player was launched for these checks. |
| [Current build manifest](Manifests/final-production-build.json) and [build03 log](Evidence/final-build-03.log) | `Bar_Q1_Final` build **Succeeded**, 193,274,993 bytes, 29.4628528 seconds, zero errors/warnings and natural exit 0. Existing product-name/save-path authority is retained. Freeze its final source/asset/tool inventory together with the eventual review artifact. Executable hash alone is insufficient to identify Unity content. |
| [720p completed sample](Evidence/final-sample-complete-720-02/player-result.json), [process](Evidence/final-sample-complete-720-02/process.json) | Report PASS, exit 0, no timeout; completed-history/coda and reading-setting/Codex coverage. This is simulated InputSystem input, not a fresh full route. |
| [1080p completed sample](Evidence/final-sample-complete-1080-01/player-result.json), [process](Evidence/final-sample-complete-1080-01/process.json) | Same bounded completed-history scope; report PASS, exit 0, no timeout. |
| [Full-route media result](Evidence/final-campaign-media-720-01/player-result.json), [process](Evidence/final-campaign-media-720-01/process.json) | Completed with 357 captures, 47 services/departures, nine Known and seven memories, but **FAIL / exit 2**, no timeout: 13 ingredient-label overflow reports across ACT2/3 slots 17-28, with no other reported errors. Raw [motion rows](Evidence/final-campaign-media-720-01/motion-times.jsonl), [frame times](Evidence/final-campaign-media-720-01/frame-times.csv), PNGs and mixed WAV remain diagnostic evidence. Corrected build03 is under separate Player verification; this retained run is not final route/mix acceptance. |
| [Corrected build03 full-route media workspace](Evidence/final-campaign-media-720-02/) | Running at this update. No terminal Player result or process result was present when inspected. **No PASS is claimed.** Keep this new attempt separate from media01 and verify its inventory, actual route, overflow result, raw motion/mix and terminal process outcome when complete. |
| [Preservation observation](Evidence/presentation-preservation-01.json) | Records its pinned-specification/RC1/index preservation check. Current final diff, inherited changes, scene/material bindings and post-validation preservation still require root's final scoped audit. |

Representative completed-candidate stills already retained are the
[720p coda](Evidence/final-sample-complete-720-02/004-earned-memory.png),
[1080p coda](Evidence/final-sample-complete-1080-01/004-earned-memory.png),
[large-text Last Word page](Evidence/final-sample-complete-720-02/038-p09-known-1-recipe_last_word.png)
and [1080p Guide](Evidence/final-sample-complete-1080-01/053-p09-guide-2.png).
These completed samples predate build03; their inventories identify their own
candidate versions. They are review targets, not a fresh build03 result or a new
aesthetic approval in this draft.

## 5. Open findings and remaining validation

These technical findings stay separate from subjective presentation sign-off.
An objective defect is not closed by assigning its family a PD row.

| Item | Current disposition and next evidence |
|---|---|
| Menu master-volume/mute lifecycle | **Implementation corrected; built-Player observation remains VALIDATION_DEBT.** Full Editor03 passes `CampaignMenuAudioTests.InitialMenuProjectsConfiguredMasterBeforeStartingAStory` and `ReturnedMenuAppliesMasterMuteAndRestoreWithoutReplayingAudio`, exercising the controller path with actual AudioSources. Retain a final actual menu mute/restore observation; the earlier finding is not rewritten as if it never occurred. |
| ACT2/3 ingredient-label overflow | **Correction has Editor evidence; actual Player verification remains VALIDATION_DEBT.** Full Editor03 passes all four shipped-font shelf cases at slots 17/21 and 720p/1080p. Preserve media01's 13 reported overflows; media02 is running against the correction. Narrative completion alone does not erase UI failures. |
| Final aggregate regression | **PASS for the inspected source checkpoint:** graphics-enabled full Editor03 is 859/859 with natural exit 0. Keep earlier 852/853, focused 79/79 and NullGfx 846/859 attempts in order. This bounded result does not certify the still-running Player or final presentation. |
| Full-route motion/mix | **VALIDATION_DEBT.** Review the retained raw media and obtain a passing corrected candidate result, including both actors, Prince contact, Hamlet stillness, all four methods, release/modal/focus cancellation and ending cleanup. Inspect raw timestamps and contact/audio alignment before using encoded playback as evidence. |
| Media sampling gaps | **VALIDATION_DEBT for exact motion coverage.** In the retained 720p media, slot 1 departure has a roughly 342 ms frame gap and no retained initial seated sample. Requested 60 Hz is not achieved 60 Hz. Do not infer a missing pose from that gap or certify contact timing from interpolated frames. The P11/P12 plan's targeted high-rate fallback remains applicable where needed. |
| Native ending pointer use | **Bounded observation retained; broad native certification remains VALIDATION_DEBT.** See the native section below. |
| Native Return key and changed hold/focus boundaries | **VALIDATION_DEBT.** Return delivery was not observed. Pointer success does not establish keyboard delivery, all tools, regrip, disabled targets, focus-loss cancellation or 1080p native coverage. |
| Dedicated performance | **Not run at this draft checkpoint / VALIDATION_DEBT.** The implemented probe and summary tests are preparation only. Follow [the measurement method](FINAL_PERFORMANCE_METHOD.md) and retain raw counters, host metadata, actual process outcome, resolution/settings and source/build identity. |
| Foreground performance interpretation | The launcher selects a visible Normal window only for `-Performance`; other proof automation remains Hidden. Each hold has a bounded sixty-second wait for application/session focus; the first has five labelled settling seconds. No automatic refocus or timer restart hides later interruptions. Batch mode is rejected, foreground/all-eligible aggregates stay separate, and no focused samples means `INCOMPLETE`. Actual focused workload coverage still requires the future run. |
| Measurement overhead and coverage | Disclose the fixed telemetry buffer and full-resolution proof RenderTexture retained for the whole run. Planned holds cover zero-memory, one-memory, paired and seven-memory rooms for 62 seconds each, four prepared methods for ten seconds each, twenty Codex cycles and large Korean Codex. Check every named focused room workload for sixty contiguous retained seconds. Missing GPU counters remain unavailable; these samples do not independently establish save-duration or minimum-hardware guarantees. |

The existing [campaign verifier](../Tools/verify_campaign_proof.py) checks
functional evidence; the [performance summary](../Tools/summarize_campaign_performance.py)
classifies uncaptured telemetry separately. A functional Player PASS cannot
replace performance validation. For the completed media run, the existing
[timestamp-preserving packager](../Tools/render_act1_media.py) accepts its
Player-result/process format and refuses a failed run: do not bypass that guard
to relabel `final-campaign-media-720-01` as passing. The old `encode_motion.ps1` fixed-rate fixture
format is not the current variable-rate campaign evidence format.

### Native ending observations

[Attempt 01](Evidence/final-native-ending-720-01/native-result.json) is retained
as `ENVIRONMENT_INTERRUPTED`: hidden launch had no targetable window and no
native events were delivered. It is not a failed gameplay run or successful
native-input test.

[Attempt 02 launch](Evidence/final-native-ending-720-02/native-launch.json)
records interactive native validation without `campaignProof` or synthetic
devices. Root-operated pointer interactions retained the Prince recall,
remaining ending, coda, Codex/settings, return to menu and completed Continue.
The [Complete observation](Evidence/final-native-ending-720-02/009-complete.json)
and [completed Continue observation](Evidence/final-native-ending-720-02/015-completed-continue.json)
both show foreground ownership, `Complete`, sequence 567, only the bartender
visible and zero visible serving liquid. These are window screenshots with
their paired observations, not a claim of exact client-only capture dimensions.
Review the [cleanup image](Evidence/final-native-ending-720-02/007-ending-cleanup.png),
[coda image](Evidence/final-native-ending-720-02/009-complete.png) and
[completed Continue image](Evidence/final-native-ending-720-02/015-completed-continue.png)
in that context.

The retained [observed-key log](Evidence/final-native-ending-720-02/input-observed.jsonl)
contained NumLock only at inspection; it does not establish Return-key delivery.
Do not convert the pointer result into a keyboard PASS. The terminal native
process/review record and broader changed-boundary checks remain to be attached.

## 6. Gate 3 presentation decision sheet

All eleven rows remain **OPEN / HUMAN_SIGNOFF_REQUIRED** after their technical
prerequisites. This is the classification of subjective approval, not a waiver
for the objective findings above. The authoritative historical ledger remains
[PRESENTATION_DEBT.md](PRESENTATION_DEBT.md).

| Debt | Owner review to present with exact candidate evidence | Status |
|---|---|---|
| PD01 | Unknown/Partial/Known journal composition, hierarchy and interaction at both resolutions. | HUMAN_SIGNOFF_REQUIRED |
| PD02 | Korean dialogue, requests, Guide, results, settings/save/Continue and control readability. | HUMAN_SIGNOFF_REQUIRED |
| PD03 | Snow accumulation/value/pattern and winter-versus-warm-bar reading in stills and motion. | HUMAN_SIGNOFF_REQUIRED |
| PD04 | Prop/material detail and accumulated room density without obscured silhouettes or memories. | HUMAN_SIGNOFF_REQUIRED |
| PD05 | Approach, turn, seating, listening, stand and departure across the cast. | HUMAN_SIGNOFF_REQUIRED |
| PD06 | Cast/environment palette, scale, source consistency and retained Prince contact. | HUMAN_SIGNOFF_REQUIRED |
| PD07 | Grounded foot weight/timbre/cadence, matched source auditions and separate in-room loudness. | HUMAN_SIGNOFF_REQUIRED |
| PD08 | Quiet/action/reading mix, fatigue, silence and lifecycle after objective mute/stop checks. | HUMAN_SIGNOFF_REQUIRED |
| PD09 | Sparse music composition, placement, exits and emotional pacing. | HUMAN_SIGNOFF_REQUIRED |
| PD10 | Tool/glass/garnish surfaces with actual liquid, ice, layering and contact states. | HUMAN_SIGNOFF_REQUIRED |
| PD11 | Tactile gesture/release/regrip and focus-transition comfort using actual controls. | HUMAN_SIGNOFF_REQUIRED |

The eventual bundled ruling must name the reviewed build/content/assets,
evidence and accepted scope. No per-row approval interruption or inferred
approval is introduced by this draft.

## 7. Final package assembly checklist

1. Freeze the final candidate's commit, source/content/asset/proof-tool inventory,
   build manifest, scene bindings and inherited-change exclusions. Preserve
   original ACT1-6 scene/content bytes and the accepted Canon/RC1 snapshots.
2. Append final technical results in execution order, retaining earlier RED,
   failed aggregate, environmental interruption and corrected-run identities.
   Link the corrected menu/shelf implementation and its final Player observations;
   do not hide either historical objective finding in aesthetic debt.
3. Index completed 720p/1080p room/UI evidence and actual motion/mix excerpts
   with raw clocks, gaps, original PNG/WAV links and achieved sampling limits.
   Clearly label simulated production input versus observed native interaction.
4. Add named-device uncaptured performance summaries and remaining unavailable
   counters/coverage; do not infer minimum PC specifications or hardware approval.
5. Publish a new uniquely named Gate 3 review artifact, then present one bundled
   owner decision covering PD01-PD11 and final pacing/emotional/game feel.
   Preserve the earlier Pass 1 and Gate 2 packages.
6. Report gameplay loop, discovery, Recommendation reachability, persistence,
   authored campaign/night lifecycle and ending, remaining validation debt,
   presentation backlog and human playtest requirements. Apply the repository's
   final closure classifications only to supported final findings. This draft
   does not assert that closure has been reached.
