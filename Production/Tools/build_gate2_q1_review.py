"""Publish the consolidated review only from completed, explicitly classified runs."""
from pathlib import Path
import html, json

ROOT=Path(__file__).resolve().parents[1]
V=ROOT/'Validation'; E=V/'Evidence'
RUNS=[('gate2-q1-open-720-03','Full ACT 1 / Open / 720p'),
      ('gate2-q1-quiet-1080-05','Warmth return through Quiet / 1080p'),
      ('gate2-q1-continue-open-05','Open completed-save Continue / 1080p'),
      ('gate2-q1-continue-quiet-05','Quiet completed-save Continue / 720p')]
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def href(p): return html.escape(p.relative_to(V).as_posix(),quote=True)
def link(p,label): return f'<a href="{href(p)}">{html.escape(label)}</a>'
def shot(folder,label):
    paths=list(folder.glob('*-'+label+'.png'))
    if len(paths)!=1: raise ValueError((folder,label,paths))
    return paths[0]
def image(p,label): return f'<figure><a href="{href(p)}"><img loading="lazy" src="{href(p)}" alt="{html.escape(label)}"></a><figcaption>{html.escape(label)} · open native original</figcaption></figure>'
records=[]; sections=[]; galleries=[]
for name,label in RUNS:
    folder=E/name; report=read(folder/'player-result.json'); process=read(folder/'process.json')
    if report['status']!='PASS' or process['exitCode']!=0 or process['timedOut']: raise ValueError('Incomplete '+name)
    records.append(dict(run=name,label=label,checks=len(report['checks']),captures=report['captures'],process=process,tier=report['tier']))
    galleries.append('<details><summary>'+html.escape(label)+' ? all native stills</summary><p>'+ ' ? '.join(link(p,p.stem) for p in sorted(folder.glob('*.png')))+'</p></details>')
    sections.append(f'<tr><td>{html.escape(label)}</td><td>{len(report["checks"])}</td><td>{report["captures"]}</td><td>{link(folder/"player-result.json","PASS / report")}</td></tr>')

open_dir=E/RUNS[0][0]; quiet_dir=E/RUNS[1][0]
for folder in (open_dir,quiet_dir):
    if read(folder/'media.json')['status']!='PASS': raise ValueError('Missing media')

topics=[
('01 · Quarter view and overall visual identity','Q1 and the bounded polish are approved. Assess the integrated ACT 1 experience within that locked direction.',open_dir,'prince-intro-First'),
('02 · Snow and the warm bar','Visible snow and exterior negative space remain part of the normal-play composition.',open_dir,'reaction-0'),
('03 · Character scale, direction and grounding','Approved +12% presence, side-quarter Prince, stools and counter occlusion. Directional walk/turn/sit transitions remain debt.',open_dir,'prince-intro-Warmth'),
('04 · ACT 1 pacing','The complete record contains all 17 services and natural arrival/departure timers. Automated reading waits are 1.5 seconds; use the interactive launcher to judge personal reading pace.',open_dir,'prince-intro-Clarity'),
('05 · Little Prince portrayal','First Manwol visit, Warmth return and Clarity closing use existing authored content. No narrative lines or outcomes were edited.',open_dir,'prince-choice'),
('06 · Four available methods','Stir, Build, Shake and Float remain the existing production interactions. No rule, quantity, timing or quality threshold changed.',open_dir,'prepared-13-shake'),
('07 · Manwol / Float','Actual first-visit Manwol: three authored layers, production controls, existing recognition and preparation evaluation.',open_dir,'prepared-5-float'),
('08 · Farewell and memory','Return-Visit Understanding remains authoritative. Open has the glowing star; Quiet retains the quiet star. Both preserve the empty chair and survive Continue.',quiet_dir,'earned-memory'),
('09 · UI and Codex','Current Korean UI, earned recipe knowledge, partial clues, Codex and large text are included. Final UI/Codex polish remains debt.',open_dir,'settings-large'),
('10 · Animation','Existing entrance, seating, service reaction, departure and bartender animation remain. Recorded video samples about 5 Hz; interactive review is required for motion feel.',open_dir,'reaction-5'),
('11 · Audio, music and silence','Videos carry the actual Unity mix. Existing room/winter balance, accepted-tool sounds, footsteps, quiet farewell and sparse earned-memory motif remain.',quiet_dir,'prince-farewell-0'),
('12 · Overall SURLHWA identity','One quiet bar, listening, return visits and a remembered guest. This package is the single final ACT 1 owner gate; ACT 2 has not begun.',open_dir,'earned-memory')]
cards=[]
for title,description,folder,label in topics:
    cards.append(f'<section><h2>{title}</h2><p>{description}</p>{image(shot(folder,label),title)}</section>')
videos=[]
for folder,title in ((open_dir,'Complete Open ACT 1 · 720p'),(quiet_dir,'Quiet return and closing · 1080p')):
    videos.append(f'<section><h2>{title}</h2><video controls preload="metadata" src="{href(folder/"player-playthrough.mp4")}"></video><p>{link(folder/"unity-mix.wav","Original Unity mix (WAV)")} · {link(folder/"media.json","Media provenance")}</p><p>'+ ' · '.join(link(folder/c['file'],c['file'].removesuffix('.mp4')) for c in read(folder/'media.json')['clips'])+'</p></section>')
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>SURLHWA · Q1 ACT 1 Gate 2</title><style>
:root{color-scheme:dark}*{box-sizing:border-box}body{margin:0;background:#171b21;color:#e1d8c5;font:16px/1.6 system-ui,sans-serif}main{max-width:1300px;margin:auto;padding:36px 28px}h1{font-size:38px;line-height:1.2}h2{font-size:21px}a{color:#dfbc79}p{max-width:95ch}.status{color:#bcceb9;font-weight:650}.note,section{background:#20262d;border:1px solid #3b4145;padding:20px;margin:20px 0}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px}.grid section{margin:0}img,video{display:block;width:100%;height:auto;image-rendering:auto}figure{margin:18px 0 0}figcaption,small{font-size:13px;color:#b2b9bc}table{width:100%;border-collapse:collapse}td,th{text-align:left;border-bottom:1px solid #444;padding:10px}code{background:#111820;padding:4px;overflow-wrap:anywhere}@media(max-width:850px){.grid{grid-template-columns:1fr}main{padding:18px}h1{font-size:29px}}</style><main>
<small>SURLHWA / 설화 · PRODUCTION · ACT 1</small><h1>Q1, lived through ACT 1</h1><p class="status">GATE2_Q1_ACT1_READY_FOR_OWNER_REVIEW</p>
<p>Q1 spatial direction and bounded polish are already approved. This is the single final Gate 2 review of the integrated ACT 1 experience. C1 and Q2 remain historical/reference evidence. ACT 2 has not begun.</p>
<div class="note"><b>Evidence boundary</b><p>Actual Player renders and actual Unity audio; actions use simulated production InputSystem devices. This is not OS-native input proof. Recorded reading cadence and approximately 5 Hz video do not establish final pacing, animation quality or latency. Audio is shorter than wall-clock frame timestamps by 3.845 s (Open) and 1.985 s (Quiet), so these recordings do not certify audiovisual sync. The isolated interactive Player is the source for subjective feel.</p></div>
<h2>Owner play review</h2><p>From the repository root:</p><p><code>powershell -File Production/Tools/run_gate2_q1.ps1 -Start New -Height 1080</code></p><p>Start options: <code>New</code>, <code>First</code>, <code>Warmth</code>, <code>Clarity</code>, <code>QuietClarity</code>, <code>OpenMemory</code>, <code>QuietMemory</code>. Each launch creates a separate save. For checkpoint starts choose Continue in the Player. Checkpoints are byte-identical copies of actually earned saves.</p>
<p><a href="GATE2_Q1_REVIEW.md">Technical report and debt</a> · <a href="Q1_OWNER_CONFIRMATION.md">Recorded Q1 approval</a> · <a href="review-q1-polish.html">Preserved approved polish</a> · <a href="review-quarter-view.html">Historical C1/Q comparison</a></p>
<h2>Current Player evidence</h2><table><tr><th>Run</th><th>Checks</th><th>Native stills</th><th>Result</th></tr>'''+''.join(sections)+'''</table>'''+''.join(galleries)+'''<h2>One bundled owner review</h2><div class="grid">'''+''.join(cards)+'''</div><h2>Playthroughs, methods and listening</h2>'''+''.join(videos)+'''<div class="note">Gate 2 subjective acceptance remains with the owner. Remaining presentation debt is recorded; it does not reopen Q1. No separate asset, camera or method approval gates are being introduced.</div></main></html>'''
(V/'review-gate2-q1.html').write_text(page,encoding='utf-8')
links=[]
import re
for value in re.findall(r'(?:href|src)="([^"]+)"',page):
    path=V/value
    if not path.is_file(): raise ValueError('Broken review link: '+value)
    links.append(value)
manifest=dict(status='GATE2_Q1_ACT1_READY_FOR_OWNER_REVIEW',runs=records,linksVerified=len(links),evidenceClasses=['targeted automated tests','simulated production input with actual native-resolution renders/audio','pose-held bounded performance','historical native-input environment debt','pending owner subjective review'])
(V/'Manifests/gate2-q1-package.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Wrote',V/'review-gate2-q1.html')
