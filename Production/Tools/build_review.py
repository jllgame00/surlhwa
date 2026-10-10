"""Build the local owner review index from measured Player evidence."""
import html
import json
from pathlib import Path
from build_comparison_board import build_board

PRODUCTION = Path(__file__).resolve().parents[1]
VALIDATION = PRODUCTION / 'Validation'
manifest = json.loads((VALIDATION / 'Manifests/captures.json').read_text(encoding='utf-8'))
if manifest['result'] != 'PASS':
    raise SystemExit('Cannot label the package ready: capture checks failed')
folder = Path(manifest['evidencePath']).resolve()
relative = folder.relative_to(VALIDATION).as_posix()
run = manifest['player']
board = build_board(manifest, VALIDATION)
fixtures = ['Empty', 'OrdinarySeated', 'QuixoteSeated', 'Composition', 'GroundingInspection', 'Standing']
rows = []
for fixture in fixtures:
    links = []
    for camera in ('OrthographicA', 'PerspectiveB'):
        for width in (1280, 1920):
            filename = f'{camera}_{fixture}_{width}x{width * 9 // 16}.png'
            links.append(f'[{camera}/{width}]({relative}/{filename})')
    rows.append('| ' + fixture + ' | ' + ' · '.join(links) + ' |')
performance = []
for m in run['measurements']:
    performance.append(f"| {m['variant']} | {m['samples']} / {m['durationSeconds']:.2f}s | {m['frameP50Ms']:.3f} / {m['frameP95Ms']:.3f} / {m['frameP99Ms']:.3f} / {m['frameMaxMs']:.3f} | {m['allocatedBytes']/1048576:.1f} / {m['reservedBytes']/1048576:.1f} | {m['drawsMean']:.0f} / {m['batchesMean']:.0f} | {m['idleGCMeanBytes']:.3f} / {m['idleGCMaxBytes']} |")
os_evidence = json.loads((folder / 'os-memory.json').read_text(encoding='utf-8-sig'))
os_peak = max((s['workingSetBytes'] for s in os_evidence['samples']), default=0)
os_private = max((s['privateBytes'] for s in os_evidence['samples']), default=0)
camera = next(c for c in run['captures'] if c['camera'] == 'OrthographicA' and c['fixture'] == 'Composition' and c['width'] == 1920)
position = ', '.join(f"{camera['position'][axis]:.6f}" for axis in 'xyz')
inspection = next(c for c in run['captures'] if c['camera'] == 'PerspectiveB' and c['fixture'] == 'GroundingInspection')
report = f'''# HG01–HG03 owner review

Verdict: **HYBRID_SPATIAL_PROOF_READY_FOR_OWNER_REVIEW**. HG01/HG02/HG03 are each **READY_FOR_OWNER_REVIEW**, with artistic acceptance **OWNER_REVIEW_PENDING**. No camera winner is selected. This is a spatial fixture, with no service success, visit completion or memory awarded.

Start with the [six-frame overview board](HG01_HG03_COMPARISON.png), then use [the comparison index](review.html) to compare each fixture at native 720p and 1080p. Inspect OrdinarySeated and QuixoteSeated, then GroundingInspection and the movement clips. Scaled previews and clips do not replace native originals. Raw outputs are local ignored evidence: preserve this folder if sharing or moving the checkout.

**Don Quixote is VISUAL_FIXTURE_ONLY.** His use here does not alter Canon ACT order or imply that he is the first canonical special guest. This pass authors no campaign sequencing.

## What to judge

| Gate | Question | Evidence | Provisional / tradeoff |
|---|---|---|---|
| HG01 — camera/depth | Which projection gives the desired intimate room and readable spatial relationships? | Empty, Composition, Standing, motion clips | A keeps equal world-size at depth; B changes scale with depth. Same transform, room, light, art and anchor staging. Neither is an artistic winner yet. |
| HG02 — coherence | Do the pixel characters belong in this restrained 3D winter room? Does charcoal remain anonymous/readable while Quixote stays distinct? | OrdinarySeated, QuixoteSeated, Composition | Low-detail primitive environment; unchanged reference art, point sampling and shared warm zone tint. Wood/window/light palette needs owner judgment. |
| HG03 — grounding/scale | Do feet meet the floor, seated hips meet the stool, and bodies belong on the correct side of the counter? | GroundingInspection, Standing, clips, counter diagnostic | Fixed 18° planes, 44 source px/world unit, cutout depth and contact patches. Walk/seat pose swaps are limited reference animation; no final hand animation. |

## Exact provisional configurations

All spatial/render values are **ARCH_SLICE_PROVISIONAL**. Normal A and B position: **({position})**, rotation **(18°, 0°, 0°)**. Both look along the same fixed direction toward **(0, 1.2, 0.7)** at distance **17 m**. A orthographic half-height **3.3 m**; B vertical FOV **{camera['fieldOfView']:.6f}°**. Near/far **0.1 / 60 m**. Aspect **16:9**, native render scale **1**, Linear color, URP Universal Renderer **Forward**, Depth Priming disabled, HDR/MSAA/post-processing/AA disabled. No free camera or runtime billboard.

GroundingInspection uses a separately authored fixed lens: target **(-0.4, 0.95, 0.1)**, same angle/distance, position **(-0.4, 6.203289, -16.067961)**, A half-height **1.85**, B FOV **{inspection['fieldOfView']:.6f}°**. It deliberately clips room edges to inspect contact. Normal room framing is preserved in the other fixtures.

Floor top **y=0**. Seated actor roots/feet: ordinary **(-0.973, 0, -1.15)**; Quixote **(-1.109, 0, -1.15)**. Bartender **(0.3, 0, 1.3)**. Seat top **26/44 × cos(18°) = 0.561988 m**; stool center **(-0.7, 0.501988, -0.69)**. Seat contact is at the front of the supporting slab; its geometry sits behind the opaque torso. Feet and hip markers are separate from transparent canvas corners. Source pivots: bartender **(17,0)**, ordinary standing **(17.5,0)** / seated **(38,15)**, Quixote standing **(20,0)** / seated **(28,15)** in source pixel coordinates from bottom left. Walk pivots **(48,1/0/0/0)** compensate source transparent margins. Source art is unchanged.

Material: URP Unlit sprite, opaque alpha clip **0.4**, depth write/test enabled, two-sided plane, no long cast or received character shadows. Shared zone tint RGBA **(0.92,0.84,0.74,1)**. Contacts are flat 24-segment ellipses **0.76 × 0.34 m**, at **y=0.012**, dark tint alpha **0.32**. They support contact; they are not character silhouette shadows. Environment uses Simple Lit, smoothness **0**, restrained shared palette. One directional key RGB **(1,0.83,0.65)**, intensity **0.85**, rotation **(42,-28,0)**, shadow strength **0.45**, bias **0.03**, map **1024**, distance **18 m**. Two warm non-shadow point fills RGB **(1,0.68,0.38)**, intensity **2.1**, range **4.2**; one cool window fill RGB **(0.45,0.65,1)**, intensity **1.6**, range **3.2**. Ambient RGB **(0.34,0.32,0.30)**. No bloom, DOF, motion blur or global pixelation.

## Original capture matrix

| Fixture | Native originals |
|---|---|
{chr(10).join(rows)}

## Movement and depth evidence

Each [A/720p]({relative}/OrthographicA_motion_1280.mp4), [B/720p]({relative}/PerspectiveB_motion_1280.mp4), [A/1080p]({relative}/OrthographicA_motion_1920.mp4), [B/1080p]({relative}/PerspectiveB_motion_1920.mp4) is 121 actual Player frames encoded at 12fps. This is a deterministic entry → front aisle → seat/hold → departure replay. Route: **(-3.9,0,2.5) → (-3.75,0,-1.15) → ordinary seat**, then reverse. Feet stay on y=0; plane angle stays fixed. Sitting uses an authored pose swap, departure reuses the reference walk cycle; no continuous sit animation, door animation, head turn or native gameplay input is claimed. These remain HUMAN_ANIMATION_TUNING_LATER.

Counter removed diagnostic images [A]({relative}/OrthographicA_CounterRemovedDiagnostic_1920x1080.png) / [B]({relative}/PerspectiveB_CounterRemovedDiagnostic_1920x1080.png) change only counter renderer visibility for an objective depth check. They are excluded from aesthetic A/B comparison. The opaque bartender lower-body source mask reveals **{manifest['counterDiagnostics'][0]['opaqueLowerBodyChangedPixels']} / {manifest['counterDiagnostics'][1]['opaqueLowerBodyChangedPixels']}** changed pixels compared with counter-present Composition. Physical ray tests independently hit the counter in front of the lower body. Ordinary/special image references, seat/feet support, static root stability and shader references are tested; automated checks do not establish artistic coherence.

## Performance baseline

Actual device: **{run['cpu']} / {run['gpu']} / {run['memoryMB']} MB RAM / {run['os']} / {run['graphicsAPI']}**. Windows Development Player, no attached profiler, hidden batch mode. One explicit native **1920×1080 offscreen URP render request per frame**, 90 rendered warmup frames, uncapped/VSync off, then 60 seconds each camera. PNG readback/encoding and motion captures occur outside the sample interval. These are Player CPU/frame-loop measurements, not GPU latency or interactive foreground performance. A non-development foreground comparison, GPU time/VRAM/driver/power/storage characterization remain unmeasured; no minimum hardware or 60fps guarantee is claimed.

| Variant | Samples / duration | Frame P50/P95/P99/max ms | Unity allocated/reserved MiB | Draws/batches | Idle GC mean/max bytes |
|---|---|---|---|---|---|
{chr(10).join(performance)}

Common inventory: **{run['measurements'][0]['objects']} scene transforms, {run['measurements'][0]['renderers']} renderers, {run['measurements'][0]['enabledRenderers']} enabled in Composition, {run['measurements'][0]['materials']} scene material references**, texture runtime estimate **{run['measurements'][0]['textureBytes']} bytes** for renderer-referenced textures. This is not a total build/loaded-asset footprint. External process sampling (about 1Hz, entire run) peak working set **{os_peak/1048576:.1f} MiB**, peak private bytes **{os_private/1048576:.1f} MiB**. Mono's raw process working-set counter returned 0 and is treated as unavailable. [Raw Player evidence]({relative}/player-run.json), [OS samples]({relative}/os-memory.json), [machine-readable inspection](Manifests/captures.json), [build result](Manifests/build.json).

## Technical limits and next decision

Capture trust requires a successfully initialized stage, matching unique run identity, zero process exit, a completion marker and a complete Player log without errors/exceptions/assertions. An actual built-Player binding-mismatch probe exited 1 with zero captures and was rejected by the inspector; [negative regression evidence](Manifests/initialization-regression.json). Native Unity Build / Build And Run preprocessing also validates/regenerates the current authored JSON. Changed and invalid source regressions passed with the same generated asset GUID. [Whole-branch review and dispositions](FINAL_REVIEW_RESULT.md).

FIRST_PASS room/art staging only. The window is a layered planar winter panel with snow marks, not a complete exterior. The shared density/seat height/camera/zone tint/palette/contacts are provisional. Reference art adoption and final rights/art approval remain pending. No hand/counter gameplay contact, acquired memory prop, paired guests, bartending, production UI, narrative, save or audio system was built. The workstation and future memory display are empty spatial reservations. This capture log contains **{manifest['processOutcome']['nativeAllocationWarnings']}** native temporary-allocation lifetime warnings; no Player Error/Exception or missing-shader pixels were recorded. Earlier valid capture runs recorded two lifetime warnings during repeated render requests. A clean latest run does not establish their cause or resolution; investigate the harness before using this path as a shipping benchmark.

Owner records accept/revise/reject for HG01/HG02/HG03 and selects a camera or requests visual revision. Stop before HG04; no artistic approval is inferred from the technical PASS.
'''
(VALIDATION / 'REVIEW_HG01_HG03.md').write_text(report, encoding='utf-8')
options = ''.join(f'<option>{f}</option>' for f in fixtures)
page = f'''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>SURLHWA · HG01–HG03 owner review</title><style>
body{{margin:0;background:#141619;color:#eee3d4;font:16px/1.55 system-ui,sans-serif}}main{{max-width:1600px;margin:auto;padding:30px}}h1{{font-size:30px;margin-bottom:0}}.sub{{color:#baa992}}.controls{{display:flex;gap:18px;align-items:center;flex-wrap:wrap;margin:24px 0}}select,a{{color:#eac79b}}select{{background:#24282e;padding:9px;border:1px solid #655445;border-radius:5px}}.pair{{display:grid;grid-template-columns:1fr 1fr;gap:18px}}figure{{margin:0;background:#202328;padding:12px;border-radius:8px}}img,video{{width:100%;height:auto;display:block;margin:10px 0}}.gates{{display:grid;grid-template-columns:repeat(3,1fr);gap:18px;margin:28px 0}}article{{background:#202328;padding:18px;border-top:2px solid #a47749}}code{{color:#eac79b}}@media(max-width:850px){{.pair,.gates{{grid-template-columns:1fr}}}}
</style><main><p class="sub">PRODUCTION / PASS 1 / ARCH_SLICE_PROVISIONAL</p><h1>A winter bar, two projections</h1><p>HG01–HG03 · READY_FOR_OWNER_REVIEW · Artistic acceptance pending. No camera winner selected.</p>
<p class="sub">Don Quixote: VISUAL_FIXTURE_ONLY. Canon ACT order unchanged; this fixture does not imply the first canonical special guest.</p>
<p>Start with the <a href="HG01_HG03_COMPARISON.png" target="_blank">six-frame overview board</a>, then compare OrdinarySeated / QuixoteSeated, GroundingInspection and the movement clips below. Use native originals to judge pixels.</p>
<div class="controls"><label>Fixture <select id="fixture">{options}</select></label><label>Native resolution <select id="resolution"><option value="1280">1280 × 720</option><option value="1920">1920 × 1080</option></select></label><a href="REVIEW_HG01_HG03.md">Review notes / exact values / limits</a></div>
<div class="pair"><figure><b>A · Orthographic</b><img id="a" alt="Real native Orthographic A Player capture"><a id="alink" target="_blank">Open original at native size</a></figure><figure><b>B · Restrained perspective</b><img id="b" alt="Real native Perspective B Player capture"><a id="blink" target="_blank">Open original at native size</a></figure></div>
<p class="sub">Previews fit this page. Use original links to judge pixels. Same room, art, lighting, staging and camera angle. The inspection fixture uses a separately recorded closer lens.</p>
<div class="gates"><article><b>HG01 · Camera / depth</b><p>Which projection makes the room intimate and spatial relationships clear?</p><small>Equal scale at depth vs restrained perspective scaling.</small></article><article><b>HG02 · Coherence</b><p>Do these pixel characters inhabit this wood-and-winter 3D room?</p><small>Look at charcoal readability, Quixote distinction and palette.</small></article><article><b>HG03 · Grounding / scale</b><p>Do feet, stool, counter and bartender belong at the same scale?</p><small>Inspect close contact and moving depth before accepting.</small></article></div>
<h2>Bounded movement replay</h2><p>Actual Player frames, 121 frames at 12fps. Reference walk cycle and pose swaps; final sitting/turn/hand animation remains pending. No gameplay input is claimed.</p><div class="pair"><figure><b>A · Motion</b><video id="avideo" controls muted loop preload="none"></video></figure><figure><b>B · Motion</b><video id="bvideo" controls muted loop preload="none"></video></figure></div>
<p><a href="Manifests/captures.json">Capture / measurement manifest</a> · <a href="Manifests/build.json">Windows build result</a> · <a href="Manifests/preservation-result.json">Frozen source preservation</a></p><p class="sub">Owner action: accept/revise/reject each gate; select camera or request visual revision. This pass stops before HG04.</p></main><script>
const root={json.dumps(relative)}; const fixture=document.querySelector('#fixture'), resolution=document.querySelector('#resolution');fixture.value='Composition';
function update(){{const w=+resolution.value,h=w*9/16;for(const [id,camera] of [['a','OrthographicA'],['b','PerspectiveB']]){{const path=`${{root}}/${{camera}}_${{fixture.value}}_${{w}}x${{h}}.png`;document.getElementById(id).src=path;document.getElementById(id+'link').href=path;document.getElementById(id+'video').src=`${{root}}/${{camera}}_motion_${{w}}.mp4`;}}}}fixture.onchange=update;resolution.onchange=update;update();
</script></html>'''
(VALIDATION / 'review.html').write_text(page, encoding='utf-8')
print('Owner review index written:', VALIDATION / 'review.html')
