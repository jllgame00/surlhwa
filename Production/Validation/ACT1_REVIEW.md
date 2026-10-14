# Gate 2 — complete ACT 1 review preparation

Status: NATIVE_CLOSURE_PENDING. Independent ACT 1 implementation, review fixes, offscreen validation and review preparation are complete. Native desktop input is paused at the owner's request until the owner signals readiness. This is not Gate 2 technical closure or owner approval. ACTs 2–7 have not started.

| Current final Player evidence | Checks | Captures | Result |
| --- | ---: | ---: | --- |
| Full open ACT 1, 720p recorded | 167 | 54 | PASS |
| Full open ACT 1, 1080p | 167 | 54 | PASS |
| Quiet farewell from actual pre-Warmth save | 70 | 28 | PASS |
| Continue at open completion | 5 | 10 | PASS |
| Continue at quiet completion | 5 | 10 | PASS |

All five processes exited 0 without errors and retained unchanged 966-file source/Player inventories. The 720p recording includes one complete playthrough plus five focused method/farewell/memory clips. Actual-resolution inspection covers method guides, character/tool visibility, Partial/Known Codex and large Korean settings text. Technical readability does not grant visual approval.

The quiet path preserves its first ten actual service receipts, records a recognized but non-fitting Warmth service, then a fitting Clarity service and the unlit star after farewell/departure. Both completed Continue runs leave both published save generations byte-identical: 17 services, 17 departures and one memory, with no duplicate grant. See [save continuity evidence](Manifests/gate2-save-continuity.json). All actions in these five runs are explicitly simulated production InputSystem actions; none replaces the pending Windows-native run.

## Current experience

The new ACT 1 Player is `Builds/Act1/SurlhwaAct1.exe`. It has its own campaign save namespace; the accepted Slice remains separate. Six authored night blocks contain 17 encounters, including the Prince's Manwol/Float first visit, Warmth return and Clarity closing. This is the complete ACT 1 campaign, not the previously approved Warmth excerpt promoted to completion.

The opening ordinary guests teach Dry Martini through consumed clues; required lessons teach Negroni, Manhattan and Gin Fizz. First-visit Manwol failure leads to a separate recovery lesson without rewriting the Prince's outcome. Manhattan is taught just before Warmth so both Seolya and Manhattan can be learned, supplied and fitting alternatives. Physical ingredients are available independently of knowledge. Unknown and Partial Codex pages do not disclose undiscovered facts merely because a recipe exists.

All four methods share actual liquid quantities, immutable service snapshots and independent recognition, preparation, quality, interpretation and FIT. Build has no separate timing grade. Shake evidence belongs to the actual closed, iced batch; late soda is measured separately. Float retains ordered portions and bad lower boundaries. Stir retains its approved controls and batch/regrip behavior.

## Prince farewell and memory

The owner-selected rule is **recognized, fitting Warmth AND Clarity services → open farewell**. The first service cannot substitute. Technique perfection and choice labels do not score the farewell. Failed services stay in history. Both farewells complete ACT 1 only after their text is consumed and natural departure finishes; both leave the empty chair and a small star. Open gently glows; quiet remains unlit. Saved receipts retain their selected policy/variant rather than changing with future tuning.

Source mapping and the decision history are in [campaign authoring](../Planning/CAMPAIGN_AUTHORING.md), [catalog adoption](../Planning/CATALOG_SCOPE.md), and [farewell decision](../Planning/PRINCE_FAREWELL_DECISION.md). Original Canon and Architecture remain read-only.

## Bundled owner judgment, when technical closure is ready

1. Six-night ordinary/special rhythm, player-driven reading, Prince portrayal and the return-visit emphasis.
2. Manwol/Float plus Build/Shake alongside the accepted Stir direction.
3. Quiet/open departure and the modest chair/star presentation.
4. Current paged Codex, discovery hierarchy, method controls and Korean reading layout.
5. C1 environment derivatives, character grounding, entry/seat/stand timing and tool projections.
6. Heavier footstep candidate, sparse memory motif, room/weather balance and silence.
7. Whether the whole experience still feels like SURLHWA.

This is one Gate 2 review package. It does not introduce separate method, UI, art or audio approval gates.

## Presentation candidate boundaries

The fixed C1 camera and bar geometry remain locked. ACT 1 changes the code-native winter-ground material, restrained counter props and ordinary-coat tint. Existing reference character sprites remain; walk/seat/stand timing is refined, while bespoke listening/reaction frames and final art adoption remain deferred. The tools now show actual shaker volume/lid/ice and Float layers without creating gameplay state.

The Codex is now paged and knowledge-filtered; this is a review candidate, not final visual Canon. The original procedural footstep adds body/transient weight and was matched to the earlier cue's RMS before mixing. Matching RMS does not prove matched perceived loudness. One sparse 22-second original motif follows actual memory completion; it does not play under farewell or replay on Continue. Final composition, instrumentation, footsteps and mix remain listening judgments.

All PD01–PD11 rows in [PRESENTATION_DEBT.md](PRESENTATION_DEBT.md) remain OPEN. Functional validation does not close visual, motion, art-provenance or audio debt.

## Evidence interpretation

The generated [review page](review-act1.html) selects only passing Player runs and lists failed/interrupted attempts separately. Offscreen evidence uses simulated production InputSystem controls against the real built Player; it is **not OS-native input**. Captures and audio come from Unity rendering/mixing. Native evidence must separately use Windows SendInput, verified exact PID/HWND/foreground ownership and current client-relative control coordinates. No callback substitution or mixing interrupted attempts into success.

Native closure remains pending. Earlier attempts `gate2-native-act1-01` and `-02` are ENVIRONMENT_INTERRUPTED, not gameplay failures. The first inherited a minimized window; the second stopped on unrelated external mouse movement at the menu. After the owner's desktop-free signal, `-03` completed four native services and stopped on a harness observation-reader error. Three new focused regressions reproduced and fixed its missing-observation handling; all 9 observer tests pass, with the Player unchanged. Fresh `-04` then stopped at the menu/Codex on an untagged mouse movement (message 512, flags/tag 0), while the same Player PID 47464 retained foreground ownership. It is retained as ENVIRONMENT_INTERRUPTED, with no continuation or refocus. No partial attempt is merged into a successful run.

Technical evidence includes the retained 149/149 standalone domain/application/content checks, 172/172 affected Unity domain/input/presentation/retained-Slice integration checks, and now 9/9 native-observer guard tests that execute no desktop input. Only the focused observer tests were rerun after the harness-only fix. It also includes actual Player services and saves, full source/Player inventories, source adoption validation and preservation hashes. All six Important findings from the [fresh code review](ACT1_FINAL_REVIEW.md) have failing-then-passing regressions. Failed offscreen attempts remain retained; a partial 17-service trace whose fixture later fails is not labelled PASS.

The [performance baseline](ACT1_PERFORMANCE.md) names the local hardware, workspace storage, instrumentation and sampling limits. Results include rendering/capture overhead. They are not a certified minimum specification, GPU profiler measurement, or final release optimization approval. Full seven-ACT, paired-character, seven-memory and long-session profiling belongs to later authorized expansion; those systems are not implemented before Gate 2.
