# Gate 3 Release Candidate — 2026-11-21

Status: **GATE3_FINAL_RC_READY_FOR_OWNER_REVIEW** with the bounded limitations below. This is not release acceptance.

The candidate is the approved Q1 Soft Orthographic Quarter View in `Bar_Q1_Final`, built with Unity 6000.3.7f1. Gameplay/source commit: `983b7b3ace846aaec86813c5049234c6c7d19e94` on `production-rebuild`. The presentation checkpoint was already committed when this RC continuation began; the worktree was clean. This phase changes only review documentation and retained result manifests. No game source, assets, content, save schema or IDs were changed. No push was performed in this RC phase. The local origin/production-rebuild reflog records baseline 983b7b3 as updated by push on 2026-11-11 19:46:41 +09:00; the earlier reported never-pushed baseline was stale. No remote fetch or push was used for this inspection.

## Verified candidate and campaign

| Area | Evidence and result |
|---|---|
| Editor regression | Retained `Evidence/final-editor-full-04.xml`: 863/863 passed, zero failed/skipped. Not rerun during this documentation/native/performance phase. |
| Tooling | Retained `Evidence/final-tools-08.log`: 182 tests, OK. |
| Corrected build04 | `Manifests/final-production-build.json`: Succeeded, 0 errors, 0 warnings, 193,279,236 bytes. `Evidence/final-build-04.log` ends with process return code 0. |
| Fresh exact-candidate route | `Evidence/portfolio-1080-20261121-01`: PASS, natural exit 0, 47 newly played services and departures across 20 nights, nine learned recipes, seven earned memories, 376 captures, 2,664 passed checks. 279 checkpoints / 557 saved generations. |
| Identity | Its complete source/content/asset/tool/Player SHA-256 inventory was checked against the current repository and build again during RC assembly: unchanged. Executable hash alone is not used as build identity. |
| Save protection | Every new run has its own save, history and legacy-history roots. Native seeds are byte-identical copies of earned checkpoints from the fresh current-build route. Original checkpoint hashes remain unchanged. Default user saves were not used. |

The route's input is simulated production InputSystem input in the actual Unity Player. Its offscreen PNG/motion captures are actual camera/UI output, not native input or performance evidence. The earlier 373-capture/2,652-check `final-campaign-media-720-02` remains historical build03 evidence, not a relabelled build04 proof.

The current campaign's 47 encounters / 20 nights supersede the older four-night bounded demo. WHAT, HOW, QUALITY, FIT, KNOWLEDGE and SUPPLY remain distinct. The passing route covers ordinary and special guests, authored discovery, actual Stir/Build/Shake/Float composition, service/reaction, accumulated memories and the common ending. Historical ACT2–ACT7 retry/mismatch/reload matrices remain indexed in their runtime checkpoint records; they are not newly executed failure branches of this fresh successful route. Full campaign/native/subjective acceptance are distinct claims.

## Save / Continue and native reload

[Native reload report](Manifests/gate3-native-reload-20261121.json) records three successful exact-state comparisons across five real Player processes. No `campaignProof` was supplied. Actual OS pointer input selected Continue, advanced visible reading controls, and closed each window using its native titlebar. All five exits were normal, code 0. Saved-state publication completed without save errors before close. The recorded observations remained foreground-owned and uninterrupted.

| Case | Before close and after a new process + Continue |
|---|---|
| ACT3 natural farewell → next guest | `Intro`, slot 28 (visit 29), line 0, 28 prior services, seven known recipes, three memories, sequence 314, text `text_rc1_0188`. The third memory was newly earned by the native farewell/departure. |
| Ending reading | `EndingReading`, final slot 46, line 1, 47 services, nine known recipes, seven memories, sequence 555, text `text_rc1_0002`. |
| Post-ending | `Complete`, final slot 46, line 0, 47 services, nine known recipes, seven memories, sequence 563, text `text_rc1_0008`. |

The published current save was compared byte-for-byte immediately after each Continue, before further input; Continue did not write it again. The fresh automated route separately verifies both published generations remain unchanged on completed menu Continue. Its archived ending checkpoints validate receipt/cursor order and all seven memories. Earlier ACT7 standalone-process runs retain EndingReady, entered, recall, last-line, farewell and completed coverage under their original build identities.

Native certification is **PASS for these bounded pointer/reload cases**, not full native campaign certification. F12 was attempted once but neither a Unity key observation nor a game capture appeared. Keyboard, held bartending gestures/regrip and changed focus-loss boundaries therefore remain VALIDATION_DEBT. Native evidence images include the window frame around a 1280×720 client. No crash/forced-kill recovery is claimed. Prior sandbox access-denied and physical-Escape interruptions remain preserved in their original directories.

## Foreground performance

**MEASURED_WITH_BOUNDED_LIMITATIONS.** Required steady foreground coverage passes; stutter-free and minimum-hardware certification are not granted. [Structured results](Manifests/gate3-performance-20261121.json) include host metadata, all measured counters, workloads and exact hitch frames.

Full run `rc-performance-1080-20261121-01`: visible, activated normal-camera 1920x1080 Windows Development Player; i7-12700KF / RTX 3070, D3D11 driver 32.0.16.1088, target 60 fps, VSync 0. Natural exit 0; 47 newly played services, seven memories, 376 captures and 2,665 checks. The 1,715-second telemetry retains 101,490 raw samples and 96,784 eligible foreground samples, with zero dropped/missing frame IDs. Seven eligible background samples remain separate. Capture/checkpoint intervals and warmup are explicitly excluded; ordinary stalls remain.

Foreground frame p50/p95/p99: **16.665 / 16.822 / 17.227 ms** (approximately 60 fps cadence). Delayed active main-thread p95 is 1.487 ms; GPU p95 is 0.583 ms. The main-thread recorder/total CPU interval include the frame-cap wait and are not pure render cost.

| Foreground workload | Longest retained interval (s) | Frame p95 / p99 (ms) | GPU p95 (ms) |
|---|---:|---:|---:|
| Q1 ordinary guest, no memories | 62.01 | 16.80 / 17.21 | 0.166 |
| Ordinary guest, one memory | 62.01 | 16.81 / 17.14 | 0.169 |
| Paired special guests | 62.00 | 16.80 / 17.16 | 0.172 |
| Completed seven-memory room | 61.89 | 16.78 / 17.12 | 0.492 |
| Focused Stir | 9.88 | 16.79 / 17.16 | 0.199 |
| Focused Build | 9.88 | 16.82 / 17.16 | 0.197 |
| Focused Shake | 9.88 | 16.81 / 17.11 | 0.199 |
| Focused Float | 9.88 | 16.83 / 17.18 | 0.194 |
| Large Korean Codex | 10.02 | 16.79 / 17.16 | 0.766 |

All four required room holds exceed 60 contiguous focused seconds. Prepared-method holds retain about 9.88 seconds after capture guards. Later live guest states are also retained: final-slot Shake has a 22.25-second interval with frame p95 16.79 ms. Four supplementary native window images document the room, tools, late guest and coda; external snapshot overhead was not subtracted.

**Hitch debt:** six full-route eligible frames exceeded 33.34 ms: 83, 159, 222, 378, 388 and 807 ms, during Brewing at slots 8/21/35/37/40. They are outside capture exclusions and remain in the percentiles. Delayed CPU timings corroborate main-thread stalls, but their cause is unassigned. No GC, host-contention, shader or specific implementation cause is claimed; no optimization or threshold weakening was applied.

A separate earned-checkpoint repeat from before service 40 (`rc-performance-late-1080-20261121-02`) exited 0 after 435 seconds with 23,789 eligible foreground samples. Frame p95/p99: 16.835 / 17.314 ms. The original slot-40 159/222 ms stalls did not recur; the repeat retained 66.042 ms during slot-42 brewing and 40.355 ms during a Codex transition. Its worst was 66.042 ms. This bounds the reproduction attempt without erasing the original stalls.

External process peak working set: full run **716.50 MiB**, repeat **775.40 MiB**. Full-run Profiler system-used p95 is 565.18 MiB; total-used p95 286.66 MiB. GC allocated-in-frame p95 is 27.91 KiB and p99 5.58 MiB; collection counts/cause were not measured. Unity `Process.WorkingSet64` returned constant zero and is **unavailable**, not zero memory. Overhead includes a 50,114,560-byte managed sample buffer, retained full-resolution proof RenderTexture, simulated-input harness and development instrumentation. Probe p95 overhead is 0.0076 ms. Draw/batch counters were not collected.

The [accepted Q1 samples](Manifests/gate2-q1-performance.json) used the same named CPU/GPU, but an **uncapped pose-held offscreen harness with controller/UI held**: 1080p p95 0.676-0.765 ms, about 429.5-429.7 draws. No speed ratio or draw-count regression can be inferred from comparison with capped normal play. Current active CPU/GPU cost and sustained room coverage show no sustained rendering bottleneck on this device. Isolated hitches remain validation debt. No fresh 720p foreground, minimum-spec or probe-free shipping benchmark is claimed.


## Presentation corrections and remaining disposition

| Item | Classification | Current disposition |
|---|---|---|
| Campaign loop, authored discovery and Recommendation reachability | PASS | Current complete route and retained branch/retry coverage. No hidden emotional answer-key or supply-to-knowledge shortcut introduced. |
| Persistence, 20-night lifecycle, seven memories and ending | PASS | Fresh saved checkpoints plus bounded native process-boundary comparisons above. |
| Korean shelf/reading overflow | PASS | Full Editor04 and current 1080p route report no overflow or invalid renderers; historical failing captures remain. Older 720p corrected-run evidence retains its own build identity. |
| Menu master mute/restore and completed Continue | PASS | Current route's actual Player checks verify all five AudioSource gains at mute, configured ambience gains on restore, no completion/physical audio replay, and unchanged save generations. This is simulated-input lifecycle proof, not subjective listening approval. |
| Review findings R01–R04 | PASS for implemented scope | Editor04 verifies the legacy ending-prop guard and MEM07 band exclusion correction. Current ACT7 route/coda supplies the corrected final-build presentation. Strengthened reading/recipe-card verifier passes the fresh final run. Legacy ACT1/6 rebuilt Player visual replay was not repeated; its guard is covered in actual-bundle Editor regression. |
| Broad native interaction | VALIDATION_DEBT | Keyboard delivery, full native tool gestures, held-input/regrip/focus cancellation and 1080p native interaction are not certified by pointer reload. |
| Isolated foreground brewing stalls | VALIDATION_DEBT | Six eligible full-route frames exceeded 33.34 ms (83?807 ms), outside capture exclusions. Root cause is unassigned; the bounded late repeat is retained separately. No stutter-free certification or unsupported GC/host diagnosis. |
| Minimum hardware / shipping performance | VALIDATION_DEBT | Named local Windows development-build results are not minimum-spec or probe-free shipping certification. |
| Frame-exact animation/audio contact and final listening | VALIDATION_DEBT | Recorded clips have measured variable sampling gaps. No interpolation or nominal 60 fps claim fills missing samples. Subjective audio/feel review remains separate. |
| PD01–PD11 and overall pacing/emotional/game feel | HUMAN_SIGNOFF_REQUIRED | P09–P12 candidates are implemented and reviewable. None of the eleven named presentation rows is closed without the owner's bundled accept/defer ruling. |
| Additional art/audio/tactile refinement requested by owner | DEFERRED_PRESENTATION | Retain current bounded candidates until the Gate 3 ruling identifies any remaining refinement scope. This is not a classification for a known objective defect. |
| Future catalogue/guest content, new systems and platform expansion | OUT_OF_SCOPE | No speculative implementation or redesign in this RC phase. |

The Q1 direction, accepted Prince seat/scale and existing authored content remain intact. Review PD01 Codex, PD02 UI/readability, PD03 snow, PD04 room details, PD05 motion, PD06 art consistency, PD07 footsteps, PD08 mix, PD09 music, PD10 tools and PD11 tactile feel as one Gate 3 package. Source provenance/reuse limitations in the art/audio inventories remain applicable; technical integration is not blanket public-release rights clearance.

## Review media and operation

Current 1080p original captures, BEST 8 and five timestamp-preserving MP4 excerpts are in `D:\portfolio_captures\SURLHWA\`, with `capture_manifest.md`, `video_manifest.md` and per-artifact evidence. Originals are unretouched. The videos cover Stir, Float/Prince, Build/Negroni, Shake/Gin Fizz and the ending. They use actual recorded frame/audio clocks; requested capture rate is not achieved fixed-rate playback. The verified export records 32 original PNGs, eight byte-identical BEST copies, six explicitly labelled motion-derived stills and five decodable MP4s.

The sealed local review folder includes the unchanged Player, selected original media, build/source inventories, native observations, performance summary and a unique isolated owner-review save path. Complete raw evidence stays in its existing directories; no failed or superseded run is overwritten. Use the included isolated launch instructions rather than double-clicking the Player against default user saves.

The [sealed review folder](Evidence/gate3-final-rc-20261121-01/SURLHWA_Gate3_RC/README.md) and [ZIP](Evidence/gate3-final-rc-20261121-01/SURLHWA_Gate3_RC.zip) contain the final documentation commit/history, clean-worktree record and SHA-256 payload manifest. The [seal result](Evidence/gate3-final-rc-20261121-01/seal-result.json) identifies the archive hash. Gate 3 readiness means ready for the owner's review, not approval to release. No push or public distribution was performed in this RC phase; the historical baseline push is recorded above.
