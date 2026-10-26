# Little Prince seated pose correction — 2026-10-27

Status: **TECHNICAL_CHECKS_PASS / HUMAN_SIGNOFF_REQUIRED**.

[Before/after review](review-prince-seated.html). Bounded owner feedback: the
previous seated Prince read too prim, with close knees/boots and folded hands.
The revised sprite separates the hands (one on the thigh, one beside the hip),
staggers the knees/boots modestly and relaxes the shoulder line. Identity, costume,
youthful quiet expression and screen-right quarter-facing style remain the target.
Final naturalness and emotional reading require owner approval.

## Scope and preservation

Only runtime asset changed: `Unity/Assets/Surlhwa/Q1Polish/PrinceSideQuarter.png`.
The existing Q1 material consumes it with the same GUID and importer settings.
96×100 transparent canvas, 76px opaque silhouette height, top/bottom alignment and
normalization center are retained. Width changes from 53px to 56px for the relaxed
posture. Original source, sprite copy and historical evidence are retained.

No C# gameplay/presentation logic, scene, prefab, mesh, material, camera, scale,
stool, contact anchor, counter geometry, standing/walking frame, focused-view art,
ACT 1 content, save identity or progression changes. User's pre-existing edit in
`review-slice.html` is excluded. The active Q1 Player was rebuilt; historical
Players/evidence remain unchanged.

## Validation

- Unity 6000.3.7f1 Q1 build succeeded: **0 errors, 0 warnings**.
- Existing `Surlhwa.SpatialTests.Q1ProductionTests`: **7/7 passed**, including
  camera/scale/contact anchors and depth-writing seated material.
- Actual rebuilt Q1 Player, isolated earned Prince checkpoint: **16 checks
  passed, zero errors**, exit 0. Covers selected Prince, bindings, Codex state,
  focused preparation/service, return to Q1, served liquid, save reload and Continue.
- Native 720p/1080p seated/composition/grounding captures were inspected. Same
  camera, character mesh viewport and bar viewport values as retained Q1 captures.
  The corrected pose stays at the existing stool with readable separated hands and
  boots. This visual inspection is not owner aesthetic acceptance.
- Source/Player inventory was identical before and after capture (1,271 files).
  Import rerun is byte-reproducible. Runtime tracked diff contains only the PNG;
  no scene/prefab serialization churn. `git diff --check` passed.

First sandboxed Unity attempt failed during local Package Manager IPC startup
before tests. The authorized unrestricted retry passed; no outstanding validation
debt from that attempt. This texture-only phase did not rerun campaign-wide QA.
There is no generated `Assembly-CSharp-Editor.csproj` in this active project;
successful native Unity compilation/build supplies compile evidence.

Evidence: `Evidence/prince-seated-correction-tests-retry.xml`,
`Evidence/prince-seated-correction-build.log`,
`Evidence/q1-prince-seated-correction/` and the tracked
[validation manifest](Manifests/prince-seated-correction-validation.json).
Native evidence/Players remain local ignored artifacts under repository policy.

Source and exact built-in imagegen prompt:
[provenance](../SourceArt/PrinceSeatedCorrection/PROVENANCE.md).
Reproduce import with `python Production/Tools/prepare_prince_seated_correction.py`.
The original `prepare_q1_art.py` remains a historical import of the prior pose.
