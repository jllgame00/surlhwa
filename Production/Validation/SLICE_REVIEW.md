# Bounded Vertical Slice — HG05–HG08 owner review

**Current owner ruling, 2026-10-14: VERTICAL_SLICE_OWNER_APPROVED_WITH_PRESENTATION_DEBT.** HG01–HG04 APPROVED; HG05 FUNCTIONAL / VISUALLY_NOT_APPROVED; HG06 APPROVED WITH AUDIO POLISH DEBT; HG07 and HG08 APPROVED. See the [exact approval scope](VERTICAL_SLICE_OWNER_APPROVAL.md), [open presentation debt](PRESENTATION_DEBT.md) and [proposed expansion plan](PRODUCTION_EXPANSION_PLAN.md). The review submission below is retained as historical evidence; its pending-owner wording predates this ruling. No expansion implementation has started.

HG01–HG04 are owner-approved. The implemented Slice is the disclosed ACT 1 Warmth-follow-up excerpt: postman, then Little Prince, through actual drinks, reaction, result, farewell and departure. Completing the excerpt does not complete ACT 1 or grant MEM-01 or any memory. Don Quixote remains historical visual-fixture-only material.

## Review surfaces

Open [the focused review index](review-slice.html), [the Windows Player](Builds/Slice/SurlhwaSlice.exe), or [the controls](HG04_PLAY_GUIDE.md). The index separates the new HG05 UI captures from accepted HG04 complete-flow/audio footage and actual native input records. [The approved HG04 Player](Builds/HG04Approved/SurlhwaSlice.exe) remains available for exact baseline comparison.

| Gate | Current state | Owner judgment still needed |
| --- | --- | --- |
| HG01–HG03 | APPROVED / C1 LOCKED | No spatial architecture reopening. Environment art remains provisional. |
| HG04 | APPROVED | Native Stir and bartending baseline accepted; final tool art, motion, sound and tactile polish may change. |
| HG05 | FUNCTIONAL / VISUALLY_NOT_APPROVED | Korean readability, composition and restraint. Codex layout, spacing, chrome and aesthetic explicitly require a deferred dedicated refinement pass. Functional captures do not approve it. |
| HG06 | READY_FOR_OWNER_REVIEW | Listen to the actual room/winter/physical mix, pauses, repetition and contact timing. Technical playback/stop evidence is not listening approval. |
| HG07 | READY_FOR_OWNER_REVIEW | Postman's hesitation and Prince's disclosed follow-up should feel distinct. Judge source selection, roles and reading rhythm; no new dialogue or final Prince farewell is invented. |
| HG08 | READY_FOR_OWNER_REVIEW, final acceptance pending | Judge the integrated reading → interpretation → actual drink → reaction → departure experience. Full Slice acceptance remains pending HG05–HG08 owner decisions; no campaign expansion. |

## HG05 bounded functional change

The existing runtime button factory now adds a thin focus outline. It follows real EventSystem selection, disappears on deselection or when a control becomes non-interactable, and never intercepts pointer input or activates a control. This closes Canon UI-01's non-color focus requirement for current production controls. A focused test reproduced the absent outline before implementation; the corrected Play Mode fixture exercises the real EventSystem lifecycle. The first Edit Mode follow-up was a test setup failure, retained separately.

Visual inspection of the first UI captures exposed automatic keyboard navigation escaping Settings into a background Stir control. A separate test reproduced it. Modal buttons now have directional neighbors only within their own panel, including a safe boundary when no neighbor exists. The final Player capture checks both visible focus and modal ownership. The first captures are retained as superseded evidence, not selected as the final review.

The Codex panel geometry, columns, typography and chrome are unchanged. No new information projection, hidden recipe disclosure, gameplay shortcut semantics, liquid authority, batch logic, evaluation, audio, narrative source or spatial change is included. The build uses the existing scene without regenerating C1 or Slice assets. New 720p/1080p UI captures and their exact inventory are indexed separately from the approved baseline.

## HG06 sound and silence

The recorded 720p complete path includes actual Unity Player audio: room/winter ambience, accepted physical interaction and short cues. It contains no continuous music, invented voice performance, success fanfare or memory reward sound. Loops follow accepted flow/motion; established interruption/cleanup evidence remains accepted. Master volume and mute are separate from New Game progress.

The 58.432-second stereo recording is a compressed review walk-through using simulated production input. It demonstrates audible output, not comfortable final pacing, mixing or tactile quality. The sound direction, inherited synthesized clips and motion timing remain provisional pending listening. No new audio claim is inferred from the focus-outline change.

## HG07 narrative and HG08 integration

The review index lists the exact adopted text IDs, source fields and roles for both encounters. Postman intro uses SRC-RC1-0212/0213; departure uses 0208/0209. Prince intro uses 0389/0390 and the existing 0395 departure field as a neutral excerpt departure. Narration/mixed text has no guest speech attribution. Request summaries remain distinct from spoken lines. Existing common responses distinguish weak preparation, mismatched interpretation and unknown liquid without duplicate reaction playback.

Normal success and failure both continue. Seolya and Manhattan remain valid Warmth interpretations; Dry Martini can be recognized/discovered while FIT mismatches. Knowledge and supply remain independent. Save/recovery resumes the actual durable reading/outcome; transient brewing restarts empty. No final farewell/memory condition, campaign schedule, ending or invented prior service success is filled in.

## Evidence boundaries

- Accepted baseline: Unity 93/93, standalone 86/86 and 18/18 selected Player runs. These are retained evidence, not newly rerun totals.
- Native A: accepted Enter observation/one action in hg04-sendinput-02. Native B: one complete Seolya service in hg04-native-stir-b-01, 766 native cursor samples, 1475.237727° accepted motion, stableFraction 0.88004636, 118.00676 ml served, good preparation/quality/FIT, one completed occurrence. No native two-guest/Continue claim.
- The earlier moved-window run remains ENVIRONMENT_INTERRUPTED; the earlier short gesture remains an incomplete harness attempt. Neither is merged into the successful native record.
- New checks are limited to the focus change and affected UI output. Exact results and build identities are in the completion ledger and manifests. No broad/native suite is reopened.
- Performance measurements remain qualified local Windows/offscreen observations. GPU/main-thread budgets, physical latency, platform coverage, final art provenance/adoption and subjective quality remain unapproved.

Remaining presentation work: Codex visual refinement, environment snow/material/prop/lighting consistency, final tool/character art, motion timing, sound and tactile polish. These are explicit debt, not permission to redesign C1 or retroactively undo HG04 approval.

Technical review found no significant blocker. Deferred minor: disabling only the Button component can leave its outline visible; current production uses interactable or GameObject activation. Component-only disable and selected-object destruction are not independently covered by the focused tests.
