# P11 animation and P12 audio implementation plan

> **For agentic workers:** Use `superpowers:executing-plans` for the existing inline workflow, task by task. This is preparation for implementation after the ACT7 campaign proof and commit. It authorizes no concurrent Assets edits during the root's Unity/Player freeze. Continue within the approved Gate3 scope without routine owner questions or a push.

**Goal:** Make the accepted cast, workbench and quiet bar feel physically connected through restrained movement, truthful contact sounds and a deliberately composed sparse musical cue, while preserving every gameplay and save authority.

**Architecture:** Extend the existing `CharacterView`, controller/cast presentation, Q1 view, workbench/input adapter and `CampaignAudio`. Use current poses, anchors, accepted action evidence and the existing five audio sources. Presentation may observe campaign transitions; it must never advance them or create technique, knowledge, service, memory or completion.

**Tech Stack:** Unity `6000.3.7f1`, existing C#/NUnit/InputSystem/uGUI/render packages, original Python/NumPy WAV authoring, current Player evidence and FFmpeg packaging tools. No engine/package upgrade, new animation framework or audio manager.

**Spec:** [Approved expansion plan P11/P12/P13](PRODUCTION_EXPANSION_PLAN.md), [P09/P11 audit](P09_P11_PREIMPLEMENTATION_AUDIT.md), [P12 audit](P12_AUDIO_PREIMPLEMENTATION_AUDIT.md), [Q1 owner ruling](Q1_OWNER_CONFIRMATION.md), [presentation debt](PRESENTATION_DEBT.md), repository `AGENTS.md` and Project Vision. Canon chapters 11 VIS03, 15 AUD01–04, 40, 50–57 and Architecture 11–13 apply. Their read-only roots are recorded in the expansion plan; they are not files to migrate into Production.

Planning baseline: 2026-11-11, HEAD `ac3aaf3`, with shared ACT7 implementation and inherited local changes present. The implementation checkpoint must record the subsequent ACT7 commit and exact candidate hashes. This document builds on the existing audits; it does not replace their historical findings or claim fresh runtime evidence.

## Global constraints

- Preserve approved Q1 camera endpoints, character scale `1.12`, roots, floor/seat/hip contacts, selective depth, pixel-first rendering, U-shaped counter and exposed winter exterior. Preserve the approved ACT1/Prince seated pose and its source identity.
- Keep `CampaignSession.TickAsync`'s existing 1.5-second logical arrival/departure boundaries. Complete visible staging within those boundaries; missing presentation cannot delay progression. No saved animation state or save schema change.
- Preserve all content/source bytes, encounter/guest/recipe/ingredient IDs, ending receipts, knowledge, supply and FIT. Do not reinterpret old outcomes or gate the ending on knowledge or audio.
- Paired guests remain two people and one encounter/request/Brew/service/cup. Their tracks may differ; no invented line attribution or second service appears. Hamlet's listening remains intentionally still.
- Workbench effects use actual accepted state. Input projection changes immediately at Q1 focus switch; any visual reveal is nonblocking. No pickup delay, imaginary stream, animation-awarded action, camera easing during a gesture or hit-target drift.
- Physical activity stops on release, rejection, modal/focus loss, session removal and disable/disposal. Missing clips/device never block story. Master zero silences every source immediately when the setting is applied.
- Use original audio synthesis/composition or retained, separately identified inherited sources. Keep historical WAVs and manifests. No character-theme quota, voice mandate, continuous score, new reward sound or runtime music-unlock state.
- Keep P09's method-only Codex label, hold-button availability and reading/focus UI work in its own package. Confirm its hold-button fix before final native control acceptance. The ACT6 glassware disclosure repair is already implemented; do not manufacture a second fix.
- Only this new plan document is changed during preparation. At execution, root coordinates compilation, Unity, Player, evidence and commits. Inspect every intentional scene/material diff; preserve owner-local changes and never regenerate all scenes for convenience.

## Review focus

1. A new session loaded at Complete after another run must remain silent, even when `CampaignAudio` survives the session replacement. Pin this in Task 4, not just the first-launch Continue case.
2. Paired departure begins with different stationary pauses; no foot contact can precede an actor's actual visible movement. Missing either presenter must leave the other track and the single logical departure intact (Tasks 1–2).
3. Q1's late actor placement and the new pose treatment must have one final transform composition; a second writer must not reset direction or move the seated hip each frame (Tasks 1–3).
4. No-op commands, stationary Shake and a held action across a modal must stay silent without losing legitimate liquid/technique evidence (Tasks 3–4).
5. A nominal high-rate recording with missing frames or an unmatched audio clock cannot certify contact or shimmer. Record actual timestamps and gaps, separate capture overhead from device performance, and label simulated/native evidence precisely (Task 6).

## File ownership and intended scope

Paths below are relative to `Production/`. New Unity files require their own stable `.meta` files.

| Files | Planned responsibility |
| --- | --- |
| `Unity/Assets/Surlhwa/Presentation/CharacterView.cs`, `CampaignController.cs`, `CampaignCastView.cs` | Existing pose selection, visible travel/staging and observed contact transitions. A small shared presentation sample in `CharacterView.cs` is acceptable to avoid two travel/contact clocks; no new authoritative FSM. |
| `Unity/Assets/Surlhwa/Presentation/CampaignCharacterArt.cs`, `Q1ProductionView.cs` | Existing art selection, restrained bartender response and final Q1 pose composition; nonblocking focus reveal. |
| `Unity/Assets/Surlhwa/Presentation/SliceWorkbenchView.cs`, `SliceInputAdapter.cs` | Presentation-only grip/alignment/return and accepted liquid/contact/Shake signals. Shared Slice callers must remain correct. |
| `Unity/Assets/Surlhwa/Presentation/CampaignAudio.cs`, `SliceAudio.cs`, `CampaignController.cs` | Existing source lifecycle/mix, separate Shake contact, master envelope and observed completion cue. Add only the necessary optional shaker clip binding. |
| `Unity/Assets/Surlhwa/Editor/CampaignProductionArtBuilder.cs`, `ActSevenExpansionBuilder.cs` | Narrow selected cue binding and reproducible final scene preparation. Update an earlier builder only when its current asset binding actually changes. |
| `Unity/Assets/Surlhwa/Tests/EditorIntegration/CampaignMotionTests.cs`, `CampaignAudioTests.cs` (new) | Focused pose/contact/fallback and actual Unity audio lifecycle regressions. |
| Existing `CampaignCastTests.cs`, `CampaignInputTests.cs`, `Q1ProductionTests.cs`, `SliceIntegrationTests.cs` in the same test directory | Changed paired/input/focus/shared-Slice contracts. Keep assertions against public behavior and actual domain state. |
| `Tools/author_act1_footstep.py`, `author_act1_audio.py`; new `Tools/test_presentation_audio_assets.py` | Reproducible original candidates, explicit musical events and numerical asset/provenance checks. Preserve existing outputs by writing versioned new candidates. |
| `Unity/Assets/Surlhwa/Presentation/CampaignPlayerProof.cs`, `Tools/render_act1_media.py`, `run_campaign_proof.ps1` only where needed | Opt-in short high-rate capture windows and timestamp-aware packaging; retain existing full-campaign proof behavior. |
| New `Validation/P11_P12_AUDIO_CUE_MAP.md`, `Manifests/p11-p12-audio.json`, `P11_P12_IMPLEMENTATION_RECORD.md` | Selected cue map, provenance/measurements and append-only implementation evidence. Existing historical records remain historical. |

The domain, save codec, campaign JSON and source adoption pipeline are read-only dependencies for these tasks. If a presentation fix seems to require changing them, first demonstrate the authority issue rather than broadening this plan.

## Task 1 — One ordinary guest's complete grounded staging

**Files:** `CharacterView.cs`, `CampaignController.cs`, `Q1ProductionView.cs`; new `CampaignMotionTests.cs`.

**Interface:** Consume existing `CampaignPhase`, phase/slot change, controller `poseClock`, current seat and existing standing/seated/walk meshes/materials. A compact `CharacterMotionSample` may contain root position, selected pose/frame, walking flag, direction and contact sequence; derive all of them from the same phase clock. Consume that sample once for the visible pose and once for contact observation. Reset observation on phase/slot/session changes; do not persist it.

- [ ] Add failing focused tests for the ordinary coat family: arrival/approach/turn/sit and stand/turn/depart remain distinct, stay on the floor and finish within 1.5 seconds. Check real root/feet/seat projection and visibility, not only enum values. Include pause/resume and a jump across several frames.
- [ ] Confirm RED for the missing staging/contact contract before implementation. Retain unique XML/log evidence; a compile error is not behavior RED.
- [ ] Implement with existing art. Reserve a short stationary turn/settle interval at the seat, then use the existing seated pose. Departure first stands/turns, then travels outward; no mid-room disappearance. Keep root travel on its current path and plane depth. Direction belongs to the art surface about its contact pivot, never a negative root scale that moves anchors. Inspect asymmetric art before choosing an art-only facing treatment; no edge-on billboard turn or replacement Prince pose.
- [ ] Map intro/listening and reaction/farewell to a quiet seated hold plus, where the existing pose allows it, one restrained settle/reorientation at the phase boundary. Do not synthesize a repeating nod or a success/failure celebration. Use the sample to compose final plane placement with Q1 once, so LateUpdate does not overwrite it.
- [ ] Replace clock-only foot cadence with observed contact-frame transitions while `walking` and visibly present. For the four-frame family, inspect which existing frames represent planted contact and declare those contacts explicitly. No step while standing, turning, seated, paused or after travel ends. A hitch observes the current contact once; it does not play a burst of skipped footsteps. Reset contact observation on cancellation/rebind.
- [ ] When walk frames are absent, retain the standing/simple-move fallback and diagnosis. Do not invent planted-frame sounds for absent animation. Missing mesh/material or actor cannot block logical arrival/departure or leave an enabled stale renderer.
- [ ] Run focused GREEN and record a short actual 720p Player sample with visible feet/seat before extending the family. Also inspect the offscreen departure endpoint in Q1; if visible pixels remain there, correct only the presentation exit segment inside the current boundary. This is an internal sample checkpoint, not owner approval.
- [ ] Review/commit the internally complete sample and contact correction with its evidence, e.g. `feat: ground guest staging and contact timing`.

## Task 2 — Bounded cast coverage and paired cleanup

**Files:** `CampaignCastView.cs`, `CampaignCharacterArt.cs`, `CharacterView.cs`; existing `CampaignCastTests.cs`, new `CampaignMotionTests.cs`.

**Interface:** Reuse Task 1's pose/contact sample; each visible actor has its own pause, direction and contact observation. `CampaignCastView` continues to own seat selection and the single `SharedCupAnchor`; `CampaignController` retains encounter routing.

| Family | Bounded treatment and required comparison |
| --- | --- |
| Ordinary coat/bun | Same verified stage sequence, existing stable material variety; no new identities or faces. |
| Prince | Travel/stand/turn only as needed; preserve approved seated art, stool contact, reserved chair and MEM01 composition. |
| Chunhyang/Mongryong | Independent solo/paired tracks and pauses, explicit silence, both leave; one existing actual cup between them after service. Do not fabricate a second cup or unsupported hand-off. |
| Quixote, Faust, Sim Cheong, Odysseus | Reuse current standing/seated/walk surfaces with character-appropriate restrained holds; verify each silhouette, anchor and exit. No generic triumphant reaction or new dialogue semantics. |
| Hamlet | Entry/sit/stand/depart as required; seated listening/silence remains still. No generic listening loop, global slow motion or input/audio freeze. |
| Bartender | Existing resident standing/towel/blink surfaces and contact position; reduce distracting repetition under reading, allow one quiet response and the bounded ending wipe/put-away below. Do not invent guest travel/seating stages for the resident bartender. |

- [ ] Add RED cases for a paired departure before each .12/.18-second existing pause, one missing actor in either position, missing/cancelled walk frames, interrupted visit, repeated refresh and one shared cup. Assert one logical service/departure/memory and no stranded actor/seat after completion.
- [ ] Apply the sample independently to both actors. Emit only contacts from a present moving actor; deduplicate the same observation. If two contacts coincide, inspect their combined level rather than doubling gain by default. Keep surviving-actor fallback, shared-cup reset and cleanup idempotent.
- [ ] Add the ending bartender gesture from the exact adopted field: `CampaignSession.CurrentText.Id == "text_rc1_0007"`, `SRC-RC1-0007`, Canon40 `onEnterLines[6]`, narration `오늘의 마지막 잔을 닦아 제자리에 놓는다.` This is resolved reading index 7 after the inserted Prince recall; bind to the text identity, not a hardcoded index or timed wait. On a live transition to this line, use the existing towel/standing art and a small empty cleaning-glass presentation to wipe, then put it back at its resident storage position. Keep hand/cloth/glass contact and existing Q1 occlusion; this is an empty cleanup prop, never the old serving liquid or another served drink. `CampaignCharacterArt` owns this bounded override of its idle selector; use existing cloth/glass art where available and no new animation framework. Keep it silent unless a separately justified physical cue is authored.
- [ ] Pin ending gesture lifecycle: repeated Refresh of line0007 does not restart; advancing immediately to line0008/coda settles the put-away without delaying the text; modal/focus pause cannot generate progress. Rebinding at Complete presents the stable put-away/quiet pose without replaying the wipe. Rebinding mid-ending establishes the appropriate pose from the current text without replaying earlier actions. Session replacement/disable/missing prop or art leaves a safe static fallback. No animation callback changes service snapshots, Brew, ending receipts, memory, knowledge, the coda or Complete publication. Assert `ServingFillMl == 0` throughout and no customer/request reappears.
- [ ] Pin feet/hip/seat contacts, Q1 orientation/scale and renderer cleanup across all eight special people, both ordinary presentations and bartender. Check later reserved seating and early/full-memory room occlusion at 720p and 1080p.
- [ ] Run focused GREEN, retain actual paired/solo/Hamlet clips, inspect intentional serialized diffs and commit, e.g. `feat: carry grounded staging across the campaign cast`.

Ending baseline inspected for this plan: root's actual 720p [recall capture](Evidence/act7-player-open-720-01/038-act7-ending-2.png) and [last-line capture](Evidence/act7-player-open-720-01/044-act7-ending-8.png) show the bartender and accumulated room with the final service cleared. They establish the current composition, not a timed wipe or motion PASS. Root reports nine ending reads and MEM07 verified; this planning task does not rerun or independently certify that campaign proof. Capture the actual line0007 gesture and completed Continue after implementation.

## Task 3 — One truthful tool sample, then four methods and measured reveal

**Files:** `SliceWorkbenchView.cs`, `SliceInputAdapter.cs`, `Q1ProductionView.cs`; `CampaignInputTests.cs`, `Q1ProductionTests.cs`, `SliceIntegrationTests.cs`.

**Interface:** Keep `SliceWorkbenchView.Refresh(BrewingSession, SliceInputAdapter, float, ServiceSnapshot)` and actual `ActiveTool`/accepted signals. Local presentation offsets can ease; container transforms used by input projection, actual liquid state, timing and command dispatch cannot. Apply offset/grip/stream endpoints together; stream starts/stops on the same accepted step.

- [ ] Add focused RED cases for pickup/return cancellation, repeated regrip, empty/rejected flow, focus switch during a held action and modal/disable cleanup. Record pre/post `ServiceSnapshot`, liquid quantities, accepted technique and transaction counts to establish unchanged truth.
- [ ] Start with bottle-to-jigger pour/transfer. Add brief art-only pickup/contact/return using existing tool and hand art. Accepted pour begins immediately; the visible source mouth, hand and stream endpoint agree that frame. On release, liquid/technique acceptance and stream stop immediately while a harmless return pose may settle. A new grip cancels the old return without duplicate effects.
- [ ] Extend the same bounded treatment to Build (ice/dash/garnish/gentle action), Stir/strain, Shake/lid/strain and Float/spoon contact/runoff. Show only the actual accepted garnish/ice/layers; no garnish reward flourish. Check all actual tool paths, not four labels over the same pour fixture. Preserve rocks/cocktail glass presentation from the selected physical glass.
- [ ] Keep `Q1ProductionView.SwitchFocus`'s immediate `ApplyView` and fixed endpoints. Refine only the existing nonblocking .22-second reveal's opacity/timing after capture. `raycastTarget` stays false; required controls and actual work remain visible. Rapid toggle, return and disabled/re-enabled view must leave neither stale veil nor stale input projection.
- [ ] Measure shimmer before changing filtering. Capture static and moving regions for feet/silhouette, counter edge, tools/stream and memory props at both resolutions; retain raw frames and camera/transform timestamps. Register moving regions when comparing pixel change so legitimate motion is not counted as flicker. Compare the same scene/path before and after.
- [ ] Correct only demonstrated offending material/UV/pose sampling locally. Do not quantize authoritative roots/input or add global blur, antialiasing, camera/scale changes. If no defect is demonstrated, record that result and leave the surface unchanged. Report measurements without claiming aesthetic acceptance.
- [ ] Run shared Slice and campaign input GREEN; retain one representative tool/focus sample before the full four-method clips. Commit the bounded tactile/reveal change, e.g. `feat: refine truthful workbench contact and return`.

## Task 4 — Accepted cues, reading mix and completion lifecycle

**Files:** `SliceInputAdapter.cs`, `CampaignAudio.cs`, `SliceAudio.cs`, `CampaignController.cs`; new `CampaignAudioTests.cs`, existing input/Slice integration tests.

**Interface:** Keep `AcceptedCue` for accepted discrete contacts. In `Command`, compare `Brew.Revision` around the command as well as its accepted result, after the existing `Stop` so cancellation records do not masquerade as the new command; `BrewingSession.Record` is the existing revision authority. Introduce `AcceptedShakeThisStep` (or an equally small separate accepted-reversal signal), reset with the other step flags in `Step`/`Stop`, and stop treating Shake as Stir. Add one optional `CampaignController.Shake` clip. No second technique counter.

- [ ] Reproduce no-op ice, empty GentleStir, repeated lid state, rejected command, stationary Shake and actual reversal. Assert zero cue on no mutation; one cue for a new accepted contact; unchanged domain action history. Keep ordinary Slice's accepted Stir/flow behavior intact.
- [ ] Give actual Shake reversal a short nonlooping shaker contact on the existing physical source; do not restart the Stir loop on alternating frames. Gate its tail on active Shake context, with immediate stop on release/modal/focus/session removal. Held-stationary frames must not trigger new contacts; a permitted brief tail is not a new motion event. Genuine Stir and pour/strain loops retain their accepted-motion/flow rule.
- [ ] Test all text states, including Intro, Request, Choice, ChoiceResponse, Reaction, Retry, Farewell, EndingReady, EndingEntered, EndingReading and Complete coda. Derive reading treatment from actual nonempty `CurrentText` with explicit quieter farewell/ending policy; inspect any state without text rather than assuming every nonbrewing phase is reading. Use short normalized ambience envelopes; preserve deliberate silence and Hamlet restraint.
- [ ] Separate musical/ambience envelope from master scaling: `source.volume = master * baseGain * envelope`. Fade envelope, never master. Test master 0 and restoration during active cue/loop/motif, setting persistence and paused/menu/disabled cases. Stop stale one-shots as well as loops on cancellation; no delayed action sound after reopening.
- [ ] Replace `musicRun`'s first-session-only initialization with per-bound-session completion observation. On every `CampaignSession` bind/replacement, establish the current completion as baseline without playing. Trigger once only on that same session's observed incomplete-to-Complete transition. For full ACT7, this follows the ninth ending-line publication; EndingReady/Entered/Reading, MEM07 acquisition and failed publication cannot trigger it. Do not inspect/reaward memories or add saved music state.
- [ ] Add RED/GREEN cases for fresh Complete Continue, Complete Continue after an incomplete prior session, another completed RunId, same RunId rebind, NewGame, return-to-menu, repeated Refresh, interrupted motif and Continue from each ending boundary. No reopening/rebinding/focus restoration restarts a consumed motif. Preserve old-scope load compatibility and existing bounded completion semantics without introducing new music triggers.
- [ ] Cover missing clips, missing device/capture capability, repeated disable/dispose and session removal. Assert progression/encoding unchanged. Unity source start/stop tests and actual mixed samples are complementary; a pure helper test alone cannot establish silence.
- [ ] Run focused GREEN plus affected existing input/audio integration, inspect diffs and commit, e.g. `fix: align campaign audio with accepted actions and completion`.

## Task 5 — Original foot contact, shaker and deliberate sparse composition

**Files:** existing audio authoring scripts; new `test_presentation_audio_assets.py`, `P11_P12_AUDIO_CUE_MAP.md`, `Manifests/p11-p12-audio.json`; versioned WAV candidates under `Unity/Assets/Surlhwa/Act1Assets/ProductionArt/` or a narrowly named P12 audio subfolder.

**Interface:** Authoring emits PCM WAV plus a manifest linking each cue ID to source events/parameters, generator revision, seed, sample rate/channel count, peak/RMS/DC/clipping/seam/tail measurements and SHA256. Builder clip fields remain the runtime binding. Cue IDs are presentation inventory, not persistent gameplay identity.

- [ ] Preserve `FootGrounded.wav`, `FootstepFeltWood.wav`, `FirstNightMotif.wav` and their old manifests. Add versioned outputs; never silently regenerate an approved/historical file in place. Prevent the existing scripts from rewriting unrelated outputs when authoring one candidate.
- [ ] Produce two original grounded-foot candidates around the existing damped wood/felt family: vary body/transient/low-mid balance and heel-to-toe roll, not merely gain. Include the current cue as control. Render identical solo/paired contact sequences with the same cadence and pitch policy. Record peak/RMS and a consistent loudness estimate, then provide separately gain-matched audition files with exact gains recorded; RMS alone is not perceptual equivalence. Retain a final in-room mix comparison separately from this matched audition.
- [ ] Author a restrained original shaker/ice contact for Task 4's actual reversal signal, with a short decaying tail and no pitched reward quality. Test worst actual reversal cadence/concurrency before choosing its gain/tail. Review inherited Pour/Stir/Strain/ice/glass/entrance/seat and room/winter references in context; tune/replace only a demonstrated mismatch and retain exact inherited lineage.
- [ ] Compose an actual final bar-return motif as a deliberate revision of the existing original vocabulary, with explicit timed notes, rests, dynamics, attack/release and timbre parameters. Starting material is the current 22-second five-attack idea (MIDI 64,59,66,64,62); its reuse is optional, not an acceptance target. Prepare a sparse arrangement and a restrained alternate, audition in the ending context, choose one internally and render the selected asset. Silence between gestures and an unresolved decay matter more than a track quota. Do not call an unarranged oscillator sketch a finished composition merely because it generates a WAV.
- [ ] Use this minimal runtime cue map; write final durations/gains/fades and selected hashes into the new map after audition. These are proposed presentation uses, not new story events:

| Context | Selected material / onset | Stop, fade and intentional silence |
| --- | --- | --- |
| Room and winter | Existing traced ambience; selected local refinements only | Low bed, quieter during authored reading/farewell/ending; master applies immediately. No required continuous music. |
| Visible planted walking contact | Selected original foot candidate from Task 1/2 contact observation | No contact during standing pause/idle, missing walk frames or canceled track; compare paired overlap. |
| Accepted Build/Stir/Float flow/contact | Existing or narrowly revised traced physical cues | Actual movement/flow owns activity; stop on release/rejection/cancel. No generic click for every UI action. |
| Accepted Shake reversal | New original shaker contact | Short decay only in active Shake context; no Stir substitution and no repeated stationary trigger. |
| Dialogue, Hamlet listening, farewells, intermediate memories, all nine ending lines | Deliberate musical silence | No theme-per-character or memory reward fanfare; room/physical treatment follows current context. |
| Full ACT7 live ending read completes | Selected original bar-return motif once after successful Complete publication, under the existing coda | Gentle normalized onset and authored decay; stop on modal/focus/menu/session removal. No cue at Ready or MEM07. No replay on completed Continue. |
| Retained bounded ACT1 completion presentation | Preserve its already-existing completion use and historical provenance; change its arrangement only if deliberately included and independently captured | Do not propagate the final-campaign arrangement into old scenes by broad regeneration. All completed Continue paths remain silent. |

- [ ] Record original authorship/no external samples where true, composition events and rationale, synthesis parameters/seed, generator Git identity, rendered asset/import settings/hash, cue/scene bindings and audition selection. Never relabel inherited source audio as newly composed or infer blanket rights approval.
- [ ] Test deterministic generation, finite samples, clipping/DC, tail termination and loop seams for looping assets; compare source and combined-mix measurements. Avoid arbitrary EQ/loudness thresholds presented as owner approval. Commit chosen sources, bindings, provenance and retained comparison evidence, e.g. `audio: author grounded contacts and sparse bar return motif`.

## Task 6 — Actual output, targeted native input and Gate3 evidence

**Files:** focused tests and existing Player proof/capture tools only as required; new implementation record/evidence manifests. Scene changes are limited to final selected bindings/staging needed for the approved candidate.

- [ ] At the post-ACT7 start, record Git status/last five commits, inherited-change inventory, selected content/source hashes and relevant scene/import baselines. Before each Unity batch, root freezes all asset writers; record actual process exit, XML totals and errors. Use unique evidence names and preserve failed attempts. Do not start a second Unity/Player against an active project/process.
- [ ] Run focused tests through the existing runner, for example `Production/Tools/run_unity.ps1 -Operation Tests -EvidenceName p11-motion-red-01 -TestFilter Surlhwa.SpatialTests.CampaignMotionTests`, then unique GREEN names and the audio/input/cast/Q1 subsets. Run the repository's normal compile/aggregate checks once the affected packages are stable. A compile-only pass is not Player evidence, and environmental blockage remains explicit debt.
- [ ] Build the exact final candidate and record executable, source/content/assets, scene and proof-tool identities. Extend existing `CampaignPlayerProof` capture only with opt-in short contact/tool/focus windows targeting actual 60 Hz capture. Keep raw frame/audio timestamps and actual achieved rate/gaps. If capture cannot sustain useful contact timing, use a separate short instrumented window at at least 30 actual frames/sec with documented uncertainty; do not interpolate 5 Hz images and call them high-rate evidence.
- [ ] Record actual Player stereo mix with those windows: ordinary approach/turn/sit/stand/depart, paired unequal departure pauses, Hamlet stillness, all four method paths, release/regrip/modal/focus stop, farewell, early/full memory rooms and the full ending/coda including line0007 wipe/put-away, rapid advance and quiet completed Continue. Include matched foot source auditions and final in-room comparisons as different artifacts. Use actual imported clips and production AudioSources, not an offline substitute for the Unity mix.
- [ ] Verify audio/video synchronization from capture clocks and contact observations; include native and slowed playback of the same original frames without synthesizing missing frames. Measure source and combined mix peaks, RMS, DC, clipping, loop seam/tail behavior, worst cue overlap and immediate mute/stop. Retain an interval of intended silence as evidence, not a failed all-silent-capture assertion.
- [ ] Use targeted real OS input on changed boundaries: pointer and keyboard pickup/release/regrip; modal during held pour, Stir, Shake and Float; focus loss/regain; Q1 focus transition; disabled/noninteractable hold target after P09 fix; master/settings/menu/Continue. Report actual native observations separately from `InputSystem.QueueStateEvent` simulated tests. Do not rerun unrelated broad HG04 suites merely to rename their evidence.
- [ ] Capture 720p and 1080p early and accumulated rooms and the changed motion/focus paths. Check visible foot/seat/hand/cup contact, readable text/heads/memories, no stale actor/stream, absence of ending customer/request/serving visuals, and measured shimmer comparison. Every special family needs coverage, but one full-campaign replay per tiny visual change is unnecessary.
- [ ] Measure changed CPU/frame/allocation/audio cost in an uncaptured representative Player run; capture overhead cannot establish device performance. Check no per-frame source creation or growing cue backlog, bounded simultaneous sources and harmless missing-device fallback. Preserve exact hardware/resolution/settings and actual sampled distributions under P13.
- [ ] Append implementation results, exact test totals, native/simulated distinctions, candidate identities, remaining debt and provenance to the new record. Link earlier audits without rewriting their historical claims. Inspect status/diff/diff-check and each scene/prefab/material change, then commit each internally complete package. Never include incidental Unity-local churn or another worker's unrelated edits.

## Completion boundary

Technical completion requires no known objective defect in staging, input truth, lifecycle, contacts, serialization/bindings or audio output; focused regressions and actual reviewable media must support the claims. Record any environmental `VALIDATION_DEBT` and whether it blocks the next step. Do not hide a failed implementation under presentation debt.

Gate3 still owns subjective foot weight/naturalness, tactile and animation feel, pixel stability as perceived, reading/focus comfort, mix fatigue, silence, musical composition/pacing and emotional impact. PD05/PD07/PD08/PD09/PD11 remain `HUMAN_SIGNOFF_REQUIRED` until the named ruling. This plan does not assert their PASS, does not reopen approved Q1/ACT1 direction, and adds no intermediate owner gate.

Preparation verification: relevant existing callers, audits, authoring tools and Canon/Architecture were read, and two existing root-owned ending screenshots were inspected; only this plan is authored. No tests, Unity/Player launches, new audio generation, listening acceptance or performance measurements were performed for this planning task.
