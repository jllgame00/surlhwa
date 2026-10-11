# Variant C — fresh final review

Reviewer: fresh context, gpt-6-astra xhigh, range `187dbc81..505a58b`; read-only, no Unity relaunch or edits. One review; no re-review.

Strengths independently confirmed: actual owner/reference image and all 12 native images, motion samples at both resolutions; world-space snow, pixel surfaces, selective depth; all 279 source / 282 payload hashes, 892 A/B history files, frozen RC1/spec state; Python 14/14, recorded native 44/44, native counter reveal 3,408 pixels; HG04 untouched and owner judgment pending.

Accepted Important findings:

1. Stool front still intersects actual opaque source row 59 in ordinary and special poses. Earlier knee-only ray missed it. Added two native cases sampling actual alpha pixels around hip/thigh plus physical support tolerance. Both RED with named source pixels before correcting the stool front edge.
2. Padded 96px walking frames used an 18px horizontal pivot, displacing the visible actor at pose transitions. Added native opaque-foot centroid/floor regression across all four walking images. RED before applying authored (48, frame0 padding 1) pivots. The centroid samples three foot rows to include both feet, rather than the single lowest toe pixel.

Fix verification: native `c-final-green.xml` is **48/48**, including both actual opaque-hip cases, all four padded walking feet, and a live memory-map regeneration regression. Windows mapped-PNG regeneration exposed during the pass was fixed by skipping unchanged source writes / replacing changed files atomically. Python evidence contracts remain 14/14. Fresh standalone foundation log `c-foundation-final.log` confirms 25/25. The C fix-pass build succeeded with zero errors/warnings; regenerated evidence is recorded in `C_REVISION_PROGRESS.md`. No second review was dispatched.

Final handoff uses `Evidence/c-reviewed-fixed`, run `4875b825a05e4db481f695901ffa731e`: exit 0, no Player errors or native allocation warnings; all 12 native stills and two clips pass. The executor visually inspected refreshed grounding and pose-transition evidence. Complete 279-source / 282-payload inventory, nine comparison-board sources, 38 local index links, 892 A/B originals and frozen RC1/specification preservation all pass. Important findings are fixed and verified; two minors remain deferred. HG01–HG03 are READY_FOR_OWNER_REVIEW, with owner aesthetic acceptance pending and HG04 untouched.

Minor findings deferred (not in this fix pass):

- C validator automatically compares only executable identity; the complete source/payload inventory was independently verified for this handoff. Future validation should consume the whole build-identity inventory.
- Failed C manifests still emit ready owner-gate fields. Their FAIL result / nonzero exit and board gate prevent package generation; future readiness fields should derive from validation result.

Declined-to-judge rulings are explicitly recorded in the progress ledger: final visual acceptance; final movement/navigation/departure; excluded production systems; unmeasured platform/performance claims; inherited-art adoption/licensing; independent standalone rerun confirmation. None grants owner PASS or expands scope.
