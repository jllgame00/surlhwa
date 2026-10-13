# HG04 closure — 2026-10-13

**HG04_BLOCKED — native full-playthrough evidence remains incomplete.**

Final spatial verification, Player evidence and technical review are complete within the bounded scope. Native pointer checks passed under confirmed foreground ownership, but the requested native Enter did not appear in the Player input log. The available Computer Use API also cannot sustain a key hold while tracing the circular Stir gesture. No direct callback or simulated event replaced missing native input. A full native F01 and native keyboard delivery must be verified before presenting HG04 as ready for owner review.

This is validation debt, not a finding that the owner-interrupted attempt failed. That original attempt remains **OWNER_INTERFERENCE / INTERRUPTED_EVIDENCE**. No architecture revision is requested. C1 stays LOCKED; HG05+ has not begun.

Open [the evidence index](review-hg04.html), [the Windows Player](Builds/Slice/SurlhwaSlice.exe), [controls](HG04_PLAY_GUIDE.md), or [the 720p playthrough with native Player audio](Evidence/hg04-final-manhattan-media-720/native-playthrough.mp4).

## Verified evidence

| Area | Result and boundary |
| --- | --- |
| Existing unit validation | Standalone 86/86 retained. The pending pre-interruption Unity/spatial result completed 93/93 and was retrieved; no broad suite was rerun because of owner interference. |
| Final build | Unity 6000.3.7f1, Windows/D3D11; 0 reported errors/warnings. Runtime implementation is local commit `b3eed4f2cd029cef44e1f9a99a2913d7e2cd4ad9`. Final full source 592-file and Player 284-file identities are recorded in each run. |
| Focused review fixes | Actual Player RED→GREEN for keyboard save-error recovery and missing walk materials. The same fresh reviewer cleared both fixes and found no remaining significant source issue in the reviewed scope. |
| Player paths | **18/18 selected current-build runs PASS; 161 actual rendered stills**, including 720p and 1080p. These are simulated Input System events through the production adapter, never labelled native OS input. |
| Full flow | Seolya and Manhattan each work as valid Warmth responses. Dry is recognized, mismatches Warmth and is discovered exactly once. Weak and Unknown outcomes still proceed through the second guest and departure. |
| Recovery | Distinct Player processes restore exact actual checkpoints after consumed intro, transient brewing, committed result, farewell, and completed excerpt. Brewing resumes empty; outcome/cursor/knowledge are not fabricated or recomputed. Save-error keyboard Retry and Return retain the last durable service exactly once. |
| Lifecycle | 60s idle, 20 distinct New Game runs, 40 actual modal transitions, large-text Guide capture without overflow. Allocated-memory samples 66.086–66.339MB across the 20 restarts; no long-duration leak claim. |
| Spatial | Fixed orthographic size 3.7/28-degree composition, seat contact and relative scale covered by the retrieved spatial test. Final serialized audit retains 1457 C1 blocks; only root children, AudioListener reference and SceneRoots change among those blocks. Geometry/camera are retained. |
| Preservation | A/B 892, C 967 and C1 680 historical files remain byte-identical; 74 specification files and RC1's branch/HEAD/65 dirty paths/index remain unchanged. 24 text/title adoptions and 18 inherited asset blobs match their authoritative committed sources. |
| Evidence validator | 11/11 focused artifact tests, including a passing baseline before ten rejection mutations. Handoff validation independently matches all current source/Player files. |

[Player manifest](Manifests/hg04-player-evidence.json) · [spatial audit](Manifests/hg04-spatial-retention.json) · [technical review](HG04_REVIEW.md) · [execution ledger](HG04_PROGRESS.md).

## Native input boundary

Attempt `hg04-native-owned-01`, PID 3752, ended before window selection or an observation was established. No input was sent; launch lifetime/root cause was not established. It is retained without a gameplay verdict.

Attempt `hg04-native-owned-02` used only PID 39584. Every recorded action was preceded by a fresh observation confirming both foreground PID 39584 and focused Player state. The observational probe recorded no ownership interruption. The process exited normally with code 0 after an intentional window-close action.

Verified native pointer scope: New Game, arrival, one dialogue line per click, request→brewing, a short held-pointer Pour producing 0.50001 ml, release remaining stopped, Guide open/close without extra liquid, and discard preserving checkpoint 6 and knowledge. Native window captures and the complete before/after observation log are retained.

The single requested native Return left the menu unchanged and no Enter was observed. Raw logs later contain LeftAlt/NumLock entries whose origin is not attributed; they are not treated as the requested command or gameplay success. No repeated key attempt, callback injection, synthetic Input System device or state injection was used in this native run. The root cause of keyboard delivery remains unresolved; existing simulated adapter validation does not settle it.

Required remaining evidence: an uninterrupted native F01 using real keyboard/pointer delivery and a complete circular Stir, followed by service/read/departure and Continue. This can be performed by the owner in a dedicated observed Player session, or by a native-input facility that supports the required gesture. Preserve this partial run rather than extending its claims.

[Native manifest](Manifests/hg04-native-evidence.json) · [ownership/action observations](Evidence/hg04-native-owned-02/ownership-actions.jsonl) · [native window: Guide](Evidence/hg04-native-owned-02/native-pointer-guide.png) · [native window: discard](Evidence/hg04-native-owned-02/native-pointer-discard.png).

## Measurement and failed-attempt limits

Hardware: Windows 11 Home 10.0.26200, i7-12700KF, RTX 3070, driver 32.0.15.9186, high-performance power plan; Parsec virtual display is present. Final isolated save roots use the C: Samsung 980 PRO SSD. Earlier D: SATA HDD runs and their roughly 300 ms durable-flush outliers remain preserved.

Non-media Seolya 720p / Dry 1080p / Manhattan 1080p runs measured frame P95 about 16.668 ms at a 60 fps cap. Every automated run includes an extra offscreen camera/UI render; these are observation-cost samples, not CPU main-thread/GPU profiling or proof of headroom. Maximum save times in those three runs were 40.99–43.14 ms. Lifecycle modal queue-to-render P50/P95/max was 18.81/21.79/53.89 ms; this excludes intentional scripted waits but is not physical input-to-photon latency.

The final 720p movie contains 527 original Player frames and 58.432 s of 48 kHz stereo AudioRenderer mix; encoded video preserves recorded frame timing. Nonzero audio and peak checks establish recorded output, not listening quality or owner acceptance. Recording increased P95 to 26.54 ms, so the movie is excluded from the non-media baseline.

The `hg04-final-manhattan-1080` recording **failed** its expected good QUALITY: WHAT/HOW/FIT were correct, but stableFraction 0.68475 was below 0.75 despite 1318 degrees. PNG recording coincided with P95 ~50 ms. The same build passed 1080p without recording and 720p with recording. Recording overhead is the working explanation, not proof that all frame-stall/input behavior is correct. No gameplay threshold was weakened. Failed files remain outside the selected green manifest.

## Acceptance scope

VS-A01–12 and 14–15/18/20–21 have domain/content/integration/Player evidence within this excerpt. VS-A13 has a representative native single-click reading observation, but native keyboard/full F01 remains debt. VS-A19 has loop-state and actual recorded audio evidence; real listening quality is human-owned. VS-A22 has both resolutions and bounded cost observations; GPU/main-thread/device/platform budgets and subjective readability remain unapproved.

The owner's Little Prince ruling supersedes historical Quixote/MEM-03 clauses VS-A16/17. ACT completion, MEM-01, MEM-03 and every other memory remain unearned in this excerpt. Don Quixote remains visual-fixture-only in historical evidence. No campaign or four-night completion is implemented or claimed.

The environment is provisional art. Deferred snow/material/prop/lighting/consistency work does not reopen the approved spatial direction. Bartending feel remains owner judgment after native evidence is complete. HG05–HG08 and broader production completion remain outside this closure.
