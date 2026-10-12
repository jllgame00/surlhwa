"""Package C1 native evidence without replacing A/B/C originals or their review files."""
import json
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image, ImageDraw
from build_c_review import ROOT, VAL, read, sha, font

def main():
    c1 = read(VAL / 'Manifests/captures-c1.json')
    assert c1['result'] == 'PASS', c1['failures']
    folder = Path(c1['evidencePath']); c = Path(read(VAL / 'Manifests/captures-c.json')['evidencePath'])
    native = ET.parse(VAL / 'Evidence/c1-native-final.xml').getroot().attrib
    assert native['failed'] == '0' and native['result'] == 'Passed'
    total = native['total']; board = Image.new('RGB', (1314, 1354), '#111c29'); draw = ImageDraw.Draw(board)
    draw.text((18,12), 'SURLHWA  /  C to C1 bounded polish', font=font(30), fill='#ede3cf')
    draw.text((18,52), 'HG01 / HG02 APPROVED · C direction retained · HG03 C1 OWNER REVIEW PENDING', font=font(20), fill='#b8c8d7')
    sources = []
    for col, (variant, sourceFolder, title) in enumerate((('C',c,'C · accepted direction / original evidence'), ('C1',folder,'C1 · bounded polish / HG03 pending'))):
        for row, fixture in enumerate(('Empty','Composition','GroundingInspection')):
            source = sourceFolder / f'{variant}_{fixture}_1280x720.png'
            x = 18 + col * 646; y = 95 + row * 401
            draw.text((x,y), title, font=font(19), fill='#f1d39f')
            with Image.open(source) as original:
                board.paste(original.convert('RGB').resize((640,360), Image.Resampling.NEAREST), (x,y+30))
            sources.append({'variant':variant,'fixture':fixture,'source':str(source),'sourceSha256':sha(source),
                'nativeSize':[1280,720],'boardSize':[640,360],'processing':'50% nearest-neighbour thumbnail; original unchanged'})
    draw.text((18,1308), 'Rows: EMPTY / COMPOSITION / GROUNDING · same 28° camera · C1 framing 3.7m vs C 4.1m', font=font(19), fill='#b8c8d7')
    output = VAL / 'C_C1_COMPARISON.png'; board.save(output)
    (VAL / 'Manifests/c1-review-board.json').write_text(json.dumps({'board':str(output),'sha256':sha(output),'sources':sources},indent=2)+'\n',encoding='utf-8')
    rel = folder.relative_to(VAL).as_posix()
    panels=[]
    for fixture in ('Empty','OrdinarySeated','SpecialSeated','Composition','GroundingInspection','Wide'):
        one=f'{rel}/C1_{fixture}_1280x720.png'; two=f'{rel}/C1_{fixture}_1920x1080.png'
        panels.append(f'<section><h2>{fixture}</h2><p><a href="{one}">Native 1280×720</a> · <a href="{two}">Native 1920×1080</a></p><a href="{two}"><img src="{one}" alt="C1 {fixture}"></a></section>')
    m=c1['player']['measurement']
    document=f'''<!doctype html><html lang="en"><meta charset="utf-8"><title>SURLHWA C1 owner review</title>
<style>body{{margin:40px auto;max-width:1320px;padding:0 22px;background:#101b28;color:#e9e0d1;font:18px/1.6 system-ui}}h1,h2{{color:#edc58a}}a{{color:#b7d7ec}}img,video{{max-width:100%;height:auto}}section{{margin:40px 0;border-top:1px solid #485363;padding-top:15px}}</style>
<h1>C1 — Pixel Cutaway Hybrid / bounded polish</h1><p>HG01 APPROVED · HG02 APPROVED · HG03 READY_FOR_OWNER_REVIEW. HG04 has not started.</p>
<p>C is the accepted Production camera/spatial direction: pixel-art first, selective depth second. A/B are REFERENCE_ONLY. C1 strengthens the U counter, quiets the winter ground, rebalances character scale and gives the bar more of the frame. It introduces no competing architecture.</p>
<p><a href="REVIEW_C1.md">C1 notes and reproduction</a> · <a href="C_C1_COMPARISON.png">C versus C1 board</a> · <a href="review-c.html">Original C native evidence</a> · <a href="ABC_COMPARISON.png">Historical A/B/C board</a></p>
<img src="C_C1_COMPARISON.png" alt="C versus C1 comparison, labelled thumbnails"><p>The board uses 50% nearest-neighbour thumbnails. All native originals remain separate and unchanged. Framing differs as requested; both use the same fixed 28° orthographic camera.</p>'''+''.join(panels)
    document+=f'''<section><h2>Motion and occlusion</h2><p>Deterministic pose replay, not final gameplay locomotion. Don Quixote remains VISUAL_FIXTURE_ONLY.</p><p><a href="{rel}/C1_motion_1280.mp4">720p clip</a> · <a href="{rel}/C1_motion_1920.mp4">1080p clip</a> · <a href="{rel}/C1_CounterRemovedDiagnostic_1920x1080.png">Counter removed diagnostic</a></p><video controls preload="none" src="{rel}/C1_motion_1280.mp4"></video></section>
<section><h2>Objective evidence</h2><p>Native tests {total}/{total}; evidence contracts 22/22. Fresh C1 process exited 0 with no Player errors or native allocation warnings. Twelve native stills and two 121-frame/12fps clips pass. Explicit offscreen 1080p sample: {m['durationSeconds']:.2f}s, mean {m['frameMeanMs']:.2f}ms, P95 {m['frameP95Ms']:.2f}ms, {m['drawsMean']:.0f} draws/frame, idle GC {m['idleGCMeanBytes']:.0f} bytes/frame.</p><p>Development/offscreen measurements do not establish GPU time, display latency or minimum hardware support. Snowfall marks remain static spatial fixtures. Final HG03 polish acceptance is the owner's decision.</p><p><a href="Manifests/captures-c1.json">Capture manifest</a> · <a href="Manifests/build-identity-c1.json">Source/Player identity</a> · <a href="C1_PROGRESS.md">Progress and technical review record</a></p></section></html>'''
    (VAL / 'review-c1.html').write_text(document,encoding='utf-8')
    notes=f'''# C1 — bounded Production polish

HG01 — APPROVED (C / Pixel Cutaway Hybrid)<br>
HG02 — APPROVED (pixel-art first, selective 2.5D second)<br>
HG03 — READY_FOR_OWNER_REVIEW

Owner ruling: 2026-10-12. A/B are REFERENCE_ONLY. C1 retains the accepted C architecture; HG04 is on hold.

[C versus C1 overview](C_C1_COMPARISON.png) · [All six native fixtures and clips](review-c1.html) · [Original C evidence](review-c.html) · [Historical A/B/C overview](ABC_COMPARISON.png)

## Focused changes

- U counter: top width 8.0m, returns 3.7m long / 1.1m wide, top thickness 0.13m. Bartender stands within the working area; guests sit outside the front seating edge. Rear wall remains a 0.06m plane and the bar stays open.
- Snow: removed C's nested blue bands. Broken neutral snow banks and cold shadows now cross dark winter ground; lighter accumulation stays on the structure. Quiet static snowfall marks remain. No production weather simulation.
- Scale: bartender 52 PPU (C: 56); both guest families including walking/seated poses 70 PPU (C: 64), all root scales 1. Seat height/support was recomputed for the new source density; opaque hip, knee, floor contact and real counter occlusion checks remain active.
- Density: normal orthographic half-height 3.7m (C: 4.1m); wide 4.55m (C: 4.9m); inspection remains 1.65m. Playable floor spans approximately 73% of normal frame width with exterior margins. Fixed pitch 28°, yaw/roll 0°, 16:9, clip 0.1/60m; no camera tracking, perspective, HDR, MSAA or post effects.

Native captures are 1280×720 and 1920×1080 for Empty, OrdinarySeated, SpecialSeated, Composition, GroundingInspection and Wide. Don Quixote is VISUAL_FIXTURE_ONLY and establishes no ACT-order claim. Existing source art remains provenance-pending; no new campaign/memory content is adopted.

## Evidence and limits

Full native suite {total}/{total}; Python evidence contracts 22/22. The C1 Player build succeeds with zero errors/warnings. Run `{c1['player']['runId']}` exits 0, errors `[]`; capture validator PASS. Actual counter removal reveals {c1['counterDiagnostics'][0]['opaqueLowerBodyChangedPixels']} opaque lower-body pixels. Two native clips each contain 121 frames at 12fps; replay demonstrates pose/contact behavior, not final navigation or animation acceptance.

On {c1['player']['cpu']} / {c1['player']['gpu']}, Unity {c1['player']['unity']}: {m['durationSeconds']:.3f}s explicit offscreen 1080p sample, {m['samples']:,} requests; mean {m['frameMeanMs']:.2f}ms / P95 {m['frameP95Ms']:.2f}ms, {m['drawsMean']:.0f} draws/batches, zero idle GC. This is a Development/offscreen baseline; GPU time/display latency/minimum hardware/cross-platform quality are unmeasured. No optimization claim.

Separate C1 scene/assets/build/captures/manifests preserve A/B/C review evidence. `c_history.py`, `c1_history.py` and `verify_preservation.py` guard original files and frozen owner/specification state. The prior C source identity remains a historical record of its implementation commit; current shared source changes belong to C1's new identity inventory.

## Reproduce using fresh evidence names

```powershell
./Production/Tools/run_unity.ps1 -Operation Tests -EvidenceName c1-tests-new
./Production/Tools/run_unity.ps1 -Operation Build -EvidenceName c1-build-new -BuildMethod Surlhwa.Editor.PixelCutawayBuilder.BuildC1Player
python Production/Tools/record_build_identity.py --variant-c1
./Production/Tools/run_player_review.ps1 -EvidenceName c1-review-new -Executable Validation/Builds/VariantC1/SurlhwaPixelCutaway.exe
./Production/Tools/encode_motion.ps1 -EvidenceName c1-review-new -CameraVariants C1
python Production/Tools/inspect_c1_review.py Production/Validation/Evidence/c1-review-new
python Production/Tools/build_c1_review.py
python Production/Tools/c_history.py
python Production/Tools/c1_history.py
python Production/Tools/verify_preservation.py
```

`-CPreview` is never accepted as final capture evidence. Historical C native tests regenerate its scene and Unity may upgrade old material serialization; exact recorded historical bytes must be reinstated before the final guards pass. The C1 package builder reads the completed `c1-native-final.xml` evidence.
'''
    (VAL / 'REVIEW_C1.md').write_text(notes,encoding='utf-8')
    print('C1 notes, native index and C/C1 board created; historical review files untouched')

if __name__ == '__main__': main()
