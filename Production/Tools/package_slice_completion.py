"""Curate retained evidence and affected HG05 renders; never launches Player/tests."""
import hashlib
import html
import json
from pathlib import Path

root=Path(__file__).resolve().parents[1]
review=root/'Validation'
read=lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
esc=html.escape
a=review/'Evidence/hg04-sendinput-02'
b=review/'Evidence/hg04-native-stir-b-01'
assert read(a/'enter-result.json')['status']=='PASS'
assert read(b/'native-result.json')['status']=='PASS'
approval={
    'gate':'HG04','status':'OWNER_APPROVED','ownerDate':'2026-10-14','timezone':'Asia/Seoul',
    'scope':['actual liquid authority','measurement and transfer','Stir interaction','native input completion','batch and regrip','recognition and service flow'],
    'notApproved':'Final visual polish; Codex FUNCTIONAL / VISUALLY_NOT_APPROVED; dedicated refinement explicitly deferred',
    'nativeEnter':'Evidence/hg04-sendinput-02/enter-result.json',
    'nativeStir':'Evidence/hg04-native-stir-b-01/native-result.json',
    'nativeScope':'One complete service and supported departure, not native two-guest F01 or native Continue',
    'baselinePlayer':'Builds/HG04Approved/SurlhwaSlice.exe',
    'retainedInterruptions':['Evidence/hg04-sendinput-02/native-result.json'],
    'files':{str(p.relative_to(review)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest()
             for folder in (a,b) for p in sorted(folder.iterdir()) if p.is_file()}
}
(review/'Manifests/hg04-owner-approval.json').write_text(json.dumps(approval,indent=2),encoding='utf-8')
ui=read(review/'Manifests/hg05-ui-evidence.json')
assert ui['status']=='PASS' and len(ui['runs'])==2
assert {r['resolution'][1] for r in ui['runs']}=={720,1080}
assert all(r['fixture']=='ui-review' for r in ui['runs'])
parts=['''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width">
<title>SURLHWA · Bounded Slice review</title><style>
body{margin:0;background:#141c21;color:#e7dfce;font:17px/1.65 system-ui}main{max-width:1150px;margin:auto;padding:40px 24px}a{color:#bddeec}h1,h2{font-weight:500}p{max-width:900px}.notice{border-left:3px solid #c8ad75;padding:12px 22px;background:#202a2e}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:20px}img,video{width:100%;height:auto}figure{margin:0}figcaption{font-size:14px;color:#c4c8c5}details{border-top:1px solid #465359;padding:18px 0}summary{cursor:pointer}table{width:100%;border-collapse:collapse}td,th{text-align:left;border-bottom:1px solid #455057;padding:12px;vertical-align:top}.role{font-size:13px;color:#b9c3c5}audio{width:100%}
</style><main><h1>SURLHWA · Bounded Vertical Slice</h1>
<p>HG01–HG04 are owner-approved. The postman and Little Prince share a disclosed ACT 1 Warmth-follow-up excerpt. ACT completion and all memories stay unearned.</p>
<p class="notice"><strong>Codex: FUNCTIONAL / VISUALLY_NOT_APPROVED.</strong> Its dedicated visual refinement is explicitly deferred. Functional checks do not approve its layout, composition, spacing, chrome or aesthetic. HG05–HG08 subjective acceptance remains with the owner.</p>
<p><a href="Builds/Slice/SurlhwaSlice.exe">Play current Slice</a> · <a href="HG04_PLAY_GUIDE.md">Controls</a> · <a href="SLICE_REVIEW.md">Scope and remaining judgments</a> · <a href="SLICE_COMPLETION_PROGRESS.md">Completion evidence</a></p>
<h2>HG05 · Readability and focus</h2><p>The new build adds a non-color keyboard focus outline and keeps directional navigation inside the current modal. These current-build 720p/1080p captures use simulated production input. They do not repeat native Enter/Stir proof or claim owner visual acceptance. The Codex design is unchanged.</p>''']
for row in ui['runs']:
    folder=review/'Evidence'/row['folder']
    parts.append(f'<details open><summary>{row["resolution"][1]}p · current HG05 UI</summary><div class="grid">')
    for image in sorted(folder.glob('[0-9][0-9]-*.png')):
        prefix='Evidence/'+folder.name+'/'+image.name
        parts.append(f'<figure><a href="{prefix}"><img loading="lazy" src="{prefix}" alt="{esc(image.stem)}"></a><figcaption>{esc(image.stem)} · <a href="{prefix[:-4]}.json">observed state</a></figcaption></figure>')
    parts.append('</div></details>')
media='Evidence/hg04-final-manhattan-media-720/'
assert (review/media/'native-playthrough.mp4').is_file()
parts.append(f'''<h2>HG06 · Sound and silence</h2><p>Accepted HG04 baseline before the focus outline: actual Player frames and captured audio, driven by simulated production input. Listen for room/winter contrast, contact timing and fatigue. Recording/output checks are not final mix approval. This compressed walkthrough does not dictate human reading speed.</p><video controls preload="metadata" src="{media}native-playthrough.mp4"></video><p><a href="{media}media.json">Audio provenance and recording limits</a> · <a href="Builds/HG04Approved/SurlhwaSlice.exe">Exact preserved HG04 Player</a></p>
<h2>HG07 · Source-bound voices and reading</h2><p>Existing source fields and roles are retained. This is a Warmth-follow-up excerpt, not the Prince's first Manwol/Float visit or ACT ending. Narration is not attributed as guest speech; no new final farewell or memory condition was invented.</p><table><tr><th>Moment / source</th><th>Adopted text</th></tr>''')
texts={t['id']:t for t in read(root/'Content/Dialogue/slice.json')['texts']}
encounters=read(root/'Content/Encounters/slice.json')['encounters']
for encounter in encounters:
    for moment, ids in [('intro',encounter['intro']),('good reaction',[encounter['responses']['good']]),('departure',encounter['farewell'])]:
        for id in ids:
            t=texts[id]
            parts.append(f'<tr><td>{esc(encounter["guestId"])} · {moment}<br><span class="role">{esc(t["sourceId"])} · {esc(t["sourceField"])} · {esc(t["role"])}</span></td><td lang="ko">{esc(t["value"])}</td></tr>')
parts.append('''</table><p><a href="../../Production/Content/Dialogue/slice.json">Exact text provenance</a> · <a href="HG04_CONTENT_ADOPTION.md">Adoption decisions</a></p>
<h2>HG08 · Integrated experience</h2><p>The complete simulated two-guest flow and recovery paths remain accepted technical evidence. The new UI build adds the focus outline and modal navigation containment, with separate affected checks. Judge the full reading → interpretation → actual drink → reaction → departure experience in the Player. Completing this excerpt grants no ACT or memory. Campaign expansion is not started.</p>
<p><a href="Manifests/hg04-player-evidence.json">Accepted 18-run baseline</a> · <a href="Manifests/hg05-ui-evidence.json">New UI evidence</a> · <a href="Manifests/hg04-owner-approval.json">HG04 approval and native evidence</a></p>
<h2>Accepted native input boundary</h2><p>Enter is retained from hg04-sendinput-02; one complete native Seolya service from hg04-native-stir-b-01. Windows SendInput drove actual production controls, with 1475.237727° accepted Stir and one natural departure. This is not a claim of a native two-guest F01 or native Continue. Interrupted attempts remain separate.</p><div class="grid">''')
for name in ('05-native-stirred','07-reaction','09-departure-completed'):
    path='Evidence/hg04-native-stir-b-01/'+name+'.png'
    assert (review/path).is_file()
    parts.append(f'<figure><a href="{path}"><img loading="lazy" src="{path}" alt="{name}"></a><figcaption>{name} · accepted HG04 native baseline</figcaption></figure>')
parts.append('</div><p>Deferred: Codex visual redesign, final environment/tool/character art, sound and motion/tactile polish. Functional completion is not final visual/audio/narrative/identity approval.</p></main></html>')
(review/'review-slice.html').write_text('\n'.join(parts),encoding='utf-8')
old=review/'review-hg04.html'
text=old.read_text(encoding='utf-8')
if 'Current owner ruling: HG04 APPROVED' not in text:
    text=text.replace('<main>','<main><p><strong>Current owner ruling: HG04 APPROVED (2026-10-14).</strong> This historical index predates native closure. <a href="review-slice.html">Open current Slice review and exact approval boundaries.</a></p>',1)
    old.write_text(text,encoding='utf-8')
print('Packaged HG05-HG08 review; approved native records remain separate from simulated full flows.')
