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
