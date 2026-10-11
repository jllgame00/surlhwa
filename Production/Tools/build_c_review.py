"""Build a labelled comparison board/index from unmodified native A/B/C originals."""
import hashlib
import html
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
VAL = ROOT / 'Validation'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def font(size): return ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',size)
def main():
    c = read(VAL / 'Manifests/captures-c.json'); assert c['result'] == 'PASS', c['failures']
    folder = Path(c['evidencePath']); ab = Path(read(VAL / 'Manifests/captures.json')['evidencePath'])
    board = Image.new('RGB',(1956,1354),'#111c29'); draw = ImageDraw.Draw(board)
    draw.text((18,12),'SURLHWA  /  A vs B vs C',font=font(30),fill='#ede3cf')
    draw.text((18,52),'HG01–HG03 · historical A/B preserved · C pixel cutaway · OWNER JUDGMENT PENDING',font=font(21),fill='#b8c8d7')
    rows = ('Empty','Composition','GroundingInspection'); sources=[]
    for col,(variant,title) in enumerate((('OrthographicA','A · Orthographic / historical'),('PerspectiveB','B · Perspective / historical'),('C','C · Pixel Cutaway Hybrid / provisional'))):
        for row,fixture in enumerate(rows):
            source = (folder if variant == 'C' else ab) / f'{variant}_{fixture}_1280x720.png'
            x=18+col*646; y=95+row*401
            draw.text((x,y),title,font=font(20),fill='#f1d39f')
            with Image.open(source) as original: board.paste(original.convert('RGB').resize((640,360),Image.Resampling.NEAREST),(x,y+30))
            sources.append({'variant':variant,'fixture':fixture,'source':str(source),'sourceSha256':sha(source),'sourceSize':[1280,720],
                            'boardSize':[640,360],'processing':'50% nearest-neighbour thumbnail only; native original remains separate'})
    draw.text((18,1308),'Rows: EMPTY / BARTENDER + SPECIAL / GROUNDING. Each variant retains its own framing. No camera or art winner selected.',font=font(20),fill='#b8c8d7')
    destination = VAL / 'ABC_COMPARISON.png'; board.save(destination)
    anchor = ROOT.parent / 'ArtSource/Reference/VisualDirection/barscene_2_5d_layout_anchor.png'
    (VAL / 'Manifests/c-review-board.json').write_text(json.dumps({'board':str(destination),'sha256':sha(destination),'sources':sources,
        'layoutAnchor':str(anchor),'layoutAnchorSha256':sha(anchor),'annotationUIReproduced':False},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    rel=folder.relative_to(VAL).as_posix(); panels=[]
    descriptions={
        'Empty':'Open bar embedded in visible snow ground, with a quiet dark outer winter field.',
        'OrdinarySeated':'Charcoal ordinary silhouette; the same seat and world scale as the special fixture.',
        'SpecialSeated':'Don Quixote is VISUAL_FIXTURE_ONLY. No Canon ACT-order implication.',
        'Composition':'Bartender inside the U counter; guest outside. Compare head scale and physical occlusion.',
        'GroundingInspection':'Native camera close inspection of foot, seat, counter, alpha depth and visible knee.',
        'Wide':'Wider fixed framing exposes the relationship between warm bar and cold exterior.'}
    for fixture in descriptions:
        one=f'{rel}/C_{fixture}_1280x720.png'; two=f'{rel}/C_{fixture}_1920x1080.png'
        panels.append(f'<section><h2>{fixture}</h2><p>{descriptions[fixture]}</p><p><a href="{one}">Native 1280×720</a> · <a href="{two}">Native 1920×1080</a></p><a href="{two}"><img src="{one}" alt="C {fixture}"></a></section>')
    measurement=c['player']['measurement']
    document='''<!doctype html><html lang="en"><meta charset="utf-8"><title>SURLHWA C — owner review</title>
<style>body{margin:40px auto;max-width:1320px;padding:0 22px;background:#101b28;color:#e9e0d1;font:18px/1.6 system-ui}h1,h2{color:#edc58a}a{color:#b7d7ec}img,video{max-width:100%;height:auto}section{margin:45px 0;border-top:1px solid #485363;padding-top:15px}.note{color:#becddd}code{font-size:16px}</style>
<h1>C — Pixel Cutaway Hybrid</h1><p>HG01 · HG02 · HG03 — READY_FOR_OWNER_REVIEW. Owner judgment pending. HG04 has not started.</p>
<p>Separate C scene / build / native originals. A and B remain historical comparison evidence. C is provisional; no final camera or visual identity is selected.</p>
<p><a href="REVIEW_C.md">Validation notes</a> · <a href="review.html">Historical A/B native review</a> · <a href="ABC_COMPARISON.png">Open A/B/C overview board</a></p>
<img src="ABC_COMPARISON.png" alt="A versus B versus C overview; thumbnails only">
<p class="note">The board uses labelled 50% nearest-neighbour thumbnails. The links below open untouched native Player captures; no global post-pixelation or UI filter is used.</p>'''
    document+=''.join(panels)
    document+=f'<section><h2>Movement and depth diagnostics</h2><p>Deterministic reference-pose replay, not native gameplay input or final locomotion. Static authored snowfall marks are a spatial fixture, not a production weather system.</p><p><a href="{rel}/C_motion_1280.mp4">720p motion</a> · <a href="{rel}/C_motion_1920.mp4">1080p motion</a> · <a href="{rel}/C_CounterRemovedDiagnostic_1920x1080.png">Counter removed diagnostic</a></p><video controls preload="none" src="{rel}/C_motion_1280.mp4"></video></section>'
    document+=f'<section><h2>Technical evidence</h2><p>Unity native suite: 44/44. Capture validator: 12 native stills + 2 clips, matching run/process identity, exit 0, no Player errors. 60-second explicit offscreen 1080p Development Player sample: mean {measurement["frameMeanMs"]:.2f} ms, P95 {measurement["frameP95Ms"]:.2f} ms, {measurement["drawsMean"]:.0f} draws/frame, idle GC mean {measurement["idleGCMeanBytes"]:.0f} bytes/frame.</p><p class="note">These are local Development/offscreen measurements, not GPU frame time, display latency or a minimum hardware promise. Native allocation warnings in this run: {c["processOutcome"]["nativeAllocationWarnings"]}.</p><p><a href="Manifests/captures-c.json">Capture manifest</a> · <a href="Manifests/build-identity-c.json">Source/Player identity</a> · <a href="Manifests/c-history.json">892-file A/B preservation baseline</a></p></section></html>'
    (VAL / 'review-c.html').write_text(document,encoding='utf-8')
    notes=f'''# C — Pixel Cutaway Hybrid / HG01–HG03

HG01 — READY_FOR_OWNER_REVIEW<br>
HG02 — READY_FOR_OWNER_REVIEW<br>
HG03 — READY_FOR_OWNER_REVIEW

Owner judgment remains pending. C is not selected as final. HG04 / Stir / bartending A/B / narrative production UI / audio / campaign / full Slice remain unimplemented.

Start with [A/B/C overview](ABC_COMPARISON.png), then [native C review index](review-c.html). [Historical A/B review](review.html) and all native originals remain separate and byte-identical to the historical hash inventory.

C follows the actual owner anchor: exposed snowy world ground around an open U-counter bar, a low rear plane and no enclosing side walls. Large surfaces use palette-limited point-sampled source textures; counter, stools, selected shelves, bottles and lamps retain depth. No screen filter, realistic PBR or smooth lighting pass. The warm wall light patches are quantized source-texture regions, not runtime light bloom. Snowfall marks are static authored world quads; no production weather system is claimed.

Camera: fixed orthographic, pitch 28°, yaw/roll 0°. Half-height 4.1 m normally, 1.65 m inspection, 4.9 m wide; aspect 16:9, near/far 0.1/60 m. No post effects, HDR, MSAA or camera-facing sprite scripts. Native output is 1280×720 and 1920×1080.

Character scale was rebuilt: bartender 56 PPU, ordinary/special 64 PPU, shared within each family including poses; root scale 1. Seat anchors use the rotated source hip, feet meet the real floor, the seat lip clears the visible knee, the real counter blocks the bartender's lower body. Don Quixote remains **VISUAL_FIXTURE_ONLY** and does not establish the first canonical special guest or alter ACT order. All supplied sprites remain provenance-pending reference assets.

Native suite 44/44; Python evidence contracts 14/14; standalone .NET foundation 25/25. C build has zero errors/warnings. Native stills 12, plus counter removal diagnostic and two 121-frame / 12fps motion clips. Source/Player/capture/run hashes are recorded in C-specific manifests. Frozen RC1 dirty state and all 74 specification hashes are unchanged; A/B preservation is checked against 892 original files.

Measured on {c['player']['cpu']} / {c['player']['gpu']}, Unity {c['player']['unity']}: 60s native offscreen 1080p sample, mean {measurement['frameMeanMs']:.2f} ms / P95 {measurement['frameP95Ms']:.2f} ms; draws {measurement['drawsMean']:.0f}; idle GC mean {measurement['idleGCMeanBytes']:.0f} bytes/frame. GPU time, display latency, VRAM pressure and minimum-spec support were not measured. Pose replay is deterministic fixture evidence, not final animation or gameplay navigation.

## Reproduce C without replacing A/B evidence

```powershell
./Production/Tools/run_unity.ps1 -Operation Tests -EvidenceName c-tests-new
./Production/Tools/run_unity.ps1 -Operation Build -EvidenceName c-build-new -BuildMethod Surlhwa.Editor.PixelCutawayBuilder.BuildPlayer
python Production/Tools/record_build_identity.py --variant-c
./Production/Tools/run_player_review.ps1 -EvidenceName c-review-new -Executable Validation/Builds/VariantC/SurlhwaPixelCutaway.exe
./Production/Tools/encode_motion.ps1 -EvidenceName c-review-new -CameraVariants C
python Production/Tools/inspect_c_review.py Production/Validation/Evidence/c-review-new
python Production/Tools/build_c_review.py
python Production/Tools/c_history.py
python Production/Tools/verify_preservation.py
```

Use fresh evidence names. `-CPreview` is deliberately rejected by the final validator. Review-only controls appear when launching the C Player interactively; automatic captures contain no review UI. The historical materials can be auto-upgraded by Unity while opening A/B test fixtures; the completed C revision reinstates their original recorded bytes before final historical verification.
'''
    (VAL / 'REVIEW_C.md').write_text(notes,encoding='utf-8')
    print('C review index, notes and A/B/C board created from native originals')

if __name__ == '__main__': main()
