# ACT 1 art source review — 2026-10-25

One fresh read-only reviewer inspected baseline f6d83d8 through this art-pass working diff, plan, source art and existing presentation implementation. No Unity, Player or desktop input was executed by the reviewer. Five Important findings and one Minor finding were corrected at source level. No unresolved source-level finding remained; this is not integrated runtime acceptance.

| Finding | Source correction | Required verification |
| --- | --- | --- |
| Cocktail shell and liquid bowl disagreed | ACT 1 shape-specific fill bounds; legacy Slice bounds retained | Actual partial/full/Float/served glass render; art regression |
| Opaque jigger hid actual measured fill | Restrained upper-cup cutaway projection in front of metal shell | Empty/partial/full jigger render; no side spill |
| Float stream bypassed new spoon | Source mouth moves above spoon; accepted-only stream and runoff meet spoon and actual surface | Real Float input/render and focus cancellation regression |
| Cocktail ice lay below bowl | Shape-aware actual serving-ice projection | Iced rocks/cocktail render and art regression |
| Editor upgrader could discard unsaved scene | Refuse before OpenScene if any open scene is dirty | Dirty-scene preservation regression |
| Preview board clipped final asset | Height derives from asset count | Regenerated board includes ToolGrip |

Tests were authored and compiled, but their Unity execution remains NOT_RUN because the Editor exits before project load with no valid license. Do not call these RED→GREEN runtime regressions until execution provides that evidence. Static C# compile and source-level review are narrower claims.

Reviewer declined-to-judge disposition:

- Unity import/shaders/occlusion: VALIDATION_DEBT until licensed Editor execution.
- Rendered seating and walk continuity: VALIDATION_DEBT; anchors are preserved but visible contacts still need actual frames.
- 720p/1080p environment composition and Korean UI readability: VALIDATION_DEBT for captures, then HUMAN_SIGNOFF_REQUIRED for taste.
- Upgrade serialization/repeatability and Continue: VALIDATION_DEBT; current scene has not been rewritten while blocked.
- Audio, motion, dialogue feel and overall art acceptance: HUMAN_SIGNOFF_REQUIRED in the bundled Gate 2 package.
- Owner-local review-slice.html and historical project closure: OUT_OF_SCOPE and preserved.

No additional reviewer is commissioned. The executor owns the pending integration, failing-then-passing evidence where available, final actual-resolution inspection and intentional serialized diff review.

## Licensed execution follow-up

The source checkpoint above remains historical. After the owner activated Unity,
the executor ran the new art regressions and affected presentation/input/Slice
suite: final result 29/29 PASS (`art-integration-final-green.xml`). The pre-apply
run retained three genuine missing-art failures and one passing dirty-scene guard;
it is an integration RED→GREEN, not a claim that each earlier source-coordinate
bug was executed before its source correction.

Actual 720p Player images and active Float frames now exercise the imported art,
character contacts, empty/filled/layered containers, spoon support and Korean UI.
All 17 recorded services pass the unchanged recognition/preparation/quality
assertions. The executor additionally corrected render-discovered legacy proxy
duplicates, ground patches, bottle/face occlusion and a disappearing thin spoon
handle. Original C1 transforms/camera/colliders and all 43 source images remain
unchanged. Full evidence and remaining owner judgments are in
[the Gate 2 art package](ACT1_ART_GATE2_REVIEW.md).

The final visual/animation/audio judgments are still HUMAN_SIGNOFF_REQUIRED.
Recorded media has the disclosed simulated-input and capture-frequency limits.
No second reviewer or new native-input approval is implied by this follow-up.
