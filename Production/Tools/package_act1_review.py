"""Build the local Gate 2 review index without promoting failed/stale evidence."""
import datetime, hashlib, html, json, statistics, subprocess, sys
from pathlib import Path
import xml.etree.ElementTree as ET

root=Path(__file__).resolve().parents[1]; validation=root/'Validation'; evidence=validation/'Evidence'
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def href(p): return html.escape(p.relative_to(validation).as_posix(),quote=True)
def link(p,label): return f'<a href="{href(p)}">{html.escape(label)}</a>'
records=[]; sections=[]; performance=[]
for folder in sorted(evidence.glob('gate2-player-*')):
    if not folder.is_dir(): continue
    report=folder/'player-result.json'; proc=folder/'process.json'; inv=folder/'inventory.json'
    if not report.exists(): continue
    r=read(report); p=read(proc) if proc.exists() else {}; status=r['status']
    current=False
    if status=='PASS' and p.get('exitCode')==0 and inv.exists():
        audit=subprocess.run([sys.executable,str(root/'Tools/act1_inventory.py'),'verify',str(inv)],capture_output=True,text=True)
        current=audit.returncode==0
        status='PASS_CURRENT' if current else 'SUPERSEDED_PASS'
    elif status=='PASS': status='INCOMPLETE_PROCESS_EVIDENCE'
    expected=read(folder/'expected-run.json') if (folder/'expected-run.json').exists() else {}
    records.append(dict(name=folder.name,status=status,fixture=expected.get('fixture'),height=expected.get('height'),checks=len(r.get('checks',[])),captures=r.get('captures',0),errors=r.get('errors',[]),tier=r.get('tier'),current=current))
    if not current: continue
    stills=[x for x in sorted(folder.glob('*.png')) if any(t in x.name for t in ['codex-initial','codex-clues','prince-intro','prince-choice','prepared-5-','prepared-6-','prepared-13-','prince-farewell','earned-memory','codex-known','settings-large'])]
    cards=''.join(f'<figure><a href="{href(x)}"><img loading="lazy" src="{href(x)}" alt="{html.escape(x.stem)}"></a><figcaption>{html.escape(x.stem)}</figcaption></figure>' for x in stills)
    media=folder/'media.json'; clips=''
    if media.exists() and read(media).get('status')=='PASS':
        for x in sorted(folder.glob('*.mp4')):
            clips+=f'<figure><video controls preload="metadata" src="{href(x)}"></video><figcaption>{html.escape(x.stem)} · Unity renders/audio; simulated production input</figcaption></figure>'
    sections.append(f'<section><h2>{html.escape(folder.name)}</h2><p>{len(r.get("checks",[]))} checks · {r.get("captures",0)} captures · exact current source/Player inventory</p><p>{link(report,"Result")} · {link(inv,str(len(read(inv)["files"]))+"-file inventory")}</p><div class="grid">{cards}{clips}</div></section>')
    frames=sorted(r.get('frames',[]))
    if frames:
        q=lambda f:frames[min(len(frames)-1,int((len(frames)-1)*f))]
        performance.append(dict(run=folder.name,samples=len(frames),p50Ms=q(.5),p95Ms=q(.95),p99Ms=q(.99),worstMs=max(frames),seconds=p.get('seconds'),maxWorkingSetBytes=p.get('maxWorkingSetBytes'),mediaCapture=media.exists(),scope='Development offscreen Player including explicit render/capture and probe overhead; not GPU/main-thread profiler timing'))
native=[]
for folder in sorted(evidence.glob('gate2-native-act1-*')):
    result=folder/'native-result.json'
    if result.exists():
        r=read(result); native.append(dict(name=folder.name,status=r['status'],boundary=r.get('lastConfirmedBoundary'),reason=r.get('firstMissingOrIncorrectBoundary')))
native_ready=False
for row in native:
    folder=evidence/row['name']; inv=folder/'inventory.json'; audit=folder/'inventory-verification.json'
    if row['status']=='PASS' and inv.exists() and audit.exists() and read(audit).get('exitCode')==0:
        native_ready=subprocess.run([sys.executable,str(root/'Tools/act1_inventory.py'),'verify',str(inv)],capture_output=True).returncode==0
tests=[]
for name in ['gate2-final-review-green-02']:
    path=evidence/(name+'.xml')
    if path.exists():
        x=ET.parse(path).getroot(); tests.append(dict(name=name,total=int(x.attrib['total']),passed=int(x.attrib['passed']),failed=int(x.attrib['failed'])))
requirements={key:any(r['current'] and r['fixture']==fixture and (height is None or r['height']==height) for r in records) for key,fixture,height in [('open720','open',720),('open1080','open',1080),('quietFarewell','quiet',None),('completedContinue','continue',None)]}
requirements['finalReviewRegressions']=bool(tests) and all(t['failed']==0 and t['passed']==t['total'] for t in tests)
requirements['recordedMedia']=any(r['current'] and (evidence/r['name']/'media.json').exists() and read(evidence/r['name']/'media.json').get('status')=='PASS' for r in records)
status='READY_FOR_OWNER_REVIEW' if all(requirements.values()) and native_ready else 'NATIVE_CLOSURE_PENDING' if all(requirements.values()) else 'INDEPENDENT_VALIDATION_IN_PROGRESS'
manifest=dict(generatedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),status=status,ownerGate='NOT_PRESENTED',independentRequirements=requirements,nativeReady=native_ready,playerRuns=records,nativeRuns=native,unityTests=tests,performance=performance,presentationDebt='PD01–PD11 OPEN; no final owner approval',scope='ACT 1 only; ACTs 2–7 not started')
(validation/'Manifests/gate2-review.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
rows=''.join(f'<tr><td>{html.escape(r["name"])}</td><td>{html.escape(r["status"])}</td><td>{r["checks"]}</td><td>{r["captures"]}</td></tr>' for r in records)
nrows=''.join(f'<tr><td>{html.escape(r["name"])}</td><td>{html.escape(r["status"])}</td><td>{html.escape(r.get("reason") or "")}</td></tr>' for r in native)
body=f'''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>SURLHWA · ACT 1 review preparation</title>
<style>body{{background:#172127;color:#eee0c7;font:17px/1.6 system-ui;max-width:1400px;margin:48px auto;padding:0 28px}}a{{color:#d9ba79}}h1,h2{{font-weight:500}}.notice{{border-left:4px solid #c89c53;padding:16px 24px;background:#253139}}.grid{{display:grid;grid-template-columns:repeat(auto-fit,minmax(420px,1fr));gap:20px}}figure{{margin:0;background:#0c1318;padding:8px}}img,video{{width:100%;display:block}}figcaption{{font-size:14px;margin:8px}}section{{margin:50px 0}}table{{border-collapse:collapse;width:100%;font-size:14px}}td,th{{border:1px solid #40515a;padding:10px;text-align:left}}.small{{font-size:14px;color:#bbc5c8}}</style>
<h1>SURLHWA · Complete ACT 1</h1><p class="notice">Review preparation. Native input remains paused until the owner's signal. Gate 2 is not technically closed or owner-approved. ACTs 2–7 have not started.</p>
<p>{link(validation/'ACT1_REVIEW.md','Review notes')} · {link(validation/'PRESENTATION_DEBT.md','Open presentation debt')} · {link(validation/'ACT1_AUDIO_CUE_MAP.md','Audio / silence')} · {link(validation/'GATE2_PROGRESS.md','Decisions and validation ledger')}</p>
<p>Six nights, 17 encounters, three Prince visits, four methods, six learned recipes, and an earned chair/star memory. C1 camera and bar composition remain locked. Current UI, art, animation and audio are candidates for one bundled owner review.</p>
<p class="small">The images and clips below are rendered by the actual built Unity Player. Automated actions use simulated production InputSystem devices and are not Windows-native input. Failed, incomplete or stale runs are excluded from the gallery.</p>
{''.join(sections)}<section><h2>Retained evidence status</h2><table><tr><th>Player run</th><th>Status</th><th>Checks</th><th>Captures</th></tr>{rows}</table><h2>Native attempts</h2><table>{nrows}</table></section>
<p>{link(validation/'Manifests/gate2-review.json','Evidence manifest and local performance limits')} · {link(validation/'Builds/Act1/SurlhwaAct1.exe','ACT 1 Player')}</p></html>'''
(validation/'review-act1.html').write_text(body,encoding='utf-8')
print(json.dumps(dict(currentPlayerRuns=sum(x['current'] for x in records),nativeReady=native_ready,unityTests=tests)))
