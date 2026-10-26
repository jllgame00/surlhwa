# SURLHWA — Q1 spatial direction lock / bounded polish

## Verdict

**Q1_BOUNDED_POLISH_READY_FOR_OWNER_REVIEW**. Q1 is the active Production spatial
baseline by owner ruling. This package requests final spatial confirmation of the
bounded polish, not reconsideration of C1/Q2. Q2 is REFERENCE_ONLY; C1 remains
historical accepted evidence. ACT 2 has not begun.

Open [the comparison page](review-q1-polish.html) or
[the concise before/after board](Q1_POLISH_COMPARISON.png).

## Locked camera and character presence

The fixed orthographic Q1 camera remains exactly at pitch 38°, yaw -28°, size 4.85,
target (0, .50, .65). Normal, wide and grounding camera transforms and projected
bar bounds are compared numerically with the previous Q1 evidence. The floor and
bar footprint do not grow; the visible exterior is not consumed by zooming in.

All three character planes gain 12% scale about their existing ground/seat
contact pivots. Actor roots, seat/feet anchors and movement paths remain unchanged.
The seated 76-source-pixel silhouette is approximately 90 display pixels at 720p
and 135 at 1080p, versus about 81/121 previously. Actual silhouettes vary by pose;
recorded world-AABB viewport bounds include transparent canvas.

The Prince uses one restrained side-quarter seated pose. His eye/cheek remain
visible while his body faces into the bar. Standing/walking frames remain accepted
art. Ordinary coat guests retain the anonymous rear-quarter study; bun guests
retain their existing pose rather than being silently converted into coat guests.
The existing 0.14-unit seated surface offset remains presentation-only. A small
stepped floor contact replaces the exposed rectangular patch in the new scene.

The new pose is **Q1_BOUNDED_POLISH_OWNER_REVIEW**, not an approved directional
animation set. Built-in imagegen source, exact prompt and reproducible import are
in [provenance](../SourceArt/Q1Polish/PROVENANCE.md) and `import.json`. Accepted
sprites and the original rear-quarter study are preserved.

## Quiet winter and lived-in bar

The existing snow world, wind bands, drifts, sparse fence, trees, rocks, snowfall,
lamps and warm ground accents remain. Ten small approach prints connect the
existing path to the entrance. Two low snow/rock silhouettes balance the edges;
they reuse accepted pixel art with cold tints. No overworld or extra gameplay.

Inside the same footprint: two spare bottles, a resting jigger, a folded cloth/
glass tray on the right return, and a small candle/notebook on the shelf. Their
art comes from existing assets. The service glass area, bartender face, readable
countertop and empty memory niche remain clear. No realtime lights, PBR materials,
free camera, new shadow system or screen-wide pixel filter were introduced.

## Production integration and focused bartending

`Bar_Q1.unity` is the active scene. Normal Unity Build/Build And Run, default
`run_unity.ps1 -Operation Build`, both CampaignBuilder Player entry points and
ACT 1 proof/native launchers now target Q1. Historical scenes, Players and review
pages remain separate. The small EditorBuildSettings scene-list change is
intentional baseline adoption, not unrelated settings churn.

The Q1 scene has no camera-choice GUI or Q2 runtime path. Existing focused
bartending retains its previous projection and tool input anchor; afterward the
world returns to Q1. Preparation renderers are hidden in normal world presentation.
The existing serving subtree remains visible during reactions, reading actual
ServiceSnapshot liquid, ice, layers and garnish. Only that serving plane turns
for Q1. No new drink-state authority or bartending architecture was added.

The Production Player uses the existing ACT 1 save namespace and format. The
review launcher supplies a fresh isolated save. Content generation continues
through CampaignBuilder.Sync, including ordinary Unity build preprocessing;
missing/stale generated data is regenerated without changing its tracked GUID.

## Updated evidence

26 fresh native stills, four fresh movement clips and their original PNG frames
are retained at both **1280×720** and **1920×1080**. The page pairs them with the
previous Q1 package, verifies historical hashes and provides direct native links.
Empty bar, seated guests, bartender/guest, wide exterior, grounding, normal
reading UI, focused tools and reaction UI are included.

Motion replays actual controller poses with their sampled timing (~12 Hz).
Ordinary covers entrance, sitting, reaction and departure. Prince starts from the
same earned checkpoint and covers seated/reaction/departure. His polished clip
uses the new seated pose; his historical clip used original art. The original
Prince ContextUI was a forced diagnostic of tools in Q1, so it is explicitly not
presented as an equivalent phase to current normal focused brewing.

Final runs: `Evidence/q1-final-ordinary-04/` and `Evidence/q1-final-prince-04/`.
Earlier previews/runs remain retained. Visual review caught a replay-only stale
next-guest/visibility issue; the final harness isolates actors and waits for the
Player frame before each offscreen render. That correction changed no gameplay.

## Validation

- Unity Player build succeeded with zero build errors/warnings.
- **11/11 targeted Unity tests passed**: Q1 anchors/cameras/default scene,
  generated-content recovery, served-state visibility and unchanged input anchor,
  existing input/Float tests, and existing Production art/contact tests.
- Actual Ordinary and Prince runs load Q1, resolve characters, open/close Codex,
  preserve checkpoint bytes through presentation changes, recognize actual service,
  preserve preparation, restore Q1 after focus, retain actual served liquid,
  reload saves and Continue the encoded checkpoint.
- Each final run verifies the 1,271-file source/Player inventory before/after.
- Standalone source compile passed; the inherited CampaignPlayerProof.audio
  CS0108 warning remains. No broad campaign QA was rerun.
- A separate read-only code review found content-generation and served-glass
  visibility issues. Both were corrected and covered by targeted tests.
- Code/prose whitespace checks pass. The full diff check records 212 spaces
  after valid empty Unity YAML fields in the new scene/assets/metas. Exact tested
  serialization is retained; these findings are not suppressed or called clean.
  See `Manifests/q1-polish-diff-check.json`.

`Manifests/q1-polish-validation.json` records fresh preservation, references,
camera comparison, media hashes and link checks. The sibling scene audit records
all changed blocks: fixed-facing transforms, camera, contact mesh/materials,
counter-side materials, Q1 activation and replacement of the two review
components. Controller/content/anchor/collider blocks remain unchanged. No
historical scene or prefab is rewritten.

## Performance scope

The comparison page contains all previous/new Q1 measurements. New samples use
10 seconds after 60 warmup frames per actor/resolution, uncapped Development
Player with explicit offscreen URP rendering, UI/workbench hidden and recorded
pose held. They include capture overhead, not native desktop/GPU timing. Historical
samples were recorded earlier; this is not a controlled hardware benchmark.

The new capture reserves sample storage before timing; historical GC included
list growth. Unity allocated/reserved memory is recorded; process working-set
counter zero is unavailable, not zero memory. No minimum-machine or zero-GC
guarantee is implied. Realtime lights/shadow lights remain zero.

Across the four new rows, median frame time is 0.490–0.504 ms (previous Q1:
0.481–0.491 ms); P95 is 0.571–0.630 ms (previous: 0.584–0.593 ms). Active
renderers/materials increase from 208/39 to 225/46; draws/batches are about
429–431 versus 422–423. Allocated memory is about 67.70–67.74 MiB; reserved
memory remains 211 MiB. This held-pose harness reports zero sampled GC after
preallocation; it does not establish zero allocations in normal gameplay.

## Preserved authority and evidence

Final Canon and approved Architecture files are unchanged. ACT 1 JSON, domain,
application, recognition, discovery, supply/knowledge, Stir/Float rules, endings,
save structure and dialogue remain unchanged. Shared workbench changes only mask
renderers/orient the existing serving plane; actual liquid remains authoritative.

The entire frozen RC1 snapshot taken at the start of this pass remains unchanged,
including its already-existing local edits. The previous spike's license-refresh
exception remains in that historical report; no new exception occurred here.
Original C1 and Q review evidence, source art, scenes and build lineage remain.
The owner's pre-existing review-slice.html edit is preserved and excluded.

## Remaining debt

| Classification | Remaining scope |
| --- | --- |
| PASS | Targeted technical prerequisites and protected authority boundaries described above. |
| HUMAN_SIGNOFF_REQUIRED | Final spatial confirmation: presence, seated expression/contact, warmth/loneliness, snow balance and pixel coherence. |
| DEFERRED_PRESENTATION | Authored turn/sit/stand transitions, subtle reaction variants, oblique pixel shimmer and the hard cut into/out of focused bartending. No full directional set was added. |
| VALIDATION_DEBT | Fresh OS-input/audio/full-duration performance certification was not run in this bounded visual tier; historical broader evidence is retained, not relabeled current. |
| OUT_OF_SCOPE | ACTs 2–7, Q2 exploration, new narrative/mechanics, a new bartending architecture and wholesale asset conversion. |

## Reproduction / review

```powershell
powershell -NoProfile -File Production/Tools/run_unity.ps1 -Operation Build -EvidenceName q1-owner-build-01
powershell -NoProfile -File Production/Tools/run_q1_polish.ps1 -EvidenceName q1-owner-01 -Interactive
```

Use fresh evidence names. Add `-Actor Prince` to the review launcher to start from
the copied earned Prince checkpoint. Direct Player launching uses the existing
Production save; the launcher isolates review. No push is part of this pass.

Stop at the requested final spatial-direction confirmation. Q1 remains the active
baseline while that bounded-polish judgment is pending; ACT 2 remains unstarted.
