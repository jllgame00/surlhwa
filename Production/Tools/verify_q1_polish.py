"""Bounded source, scene, review and frozen-input audit for adopted Q1."""
from pathlib import Path
import collections, hashlib, json, re, xml.etree.ElementTree as ET
import verify_preservation
ROOT=Path(__file__).resolve().parents[2]; V=ROOT/'Production/Validation'; M=V/'Manifests'
def sha(data): return hashlib.sha256(data).hexdigest()
def blocks(path):
    return {int(k):(int(t),s) for t,k,s in re.findall(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',path.read_text(),re.M|re.S)}
def main():
    old=json.loads((M/'q1-polish-before.json').read_text()); errors=[]
    allowed={'Production/README.md','Production/Unity/Assets/Surlhwa/Editor/CampaignBuilder.cs',
      'Production/Unity/ProjectSettings/EditorBuildSettings.asset','Production/Tools/run_unity.ps1',
      'Production/Tools/run_act1_proof.ps1','Production/Tools/run_act1_native.py','Production/Tools/act1_inventory.py',
      'Production/Unity/Assets/Surlhwa/Presentation/SliceWorkbenchView.cs'}
    changed=[]
    for p,h in old['files'].items():
        file=ROOT/p
        if not file.is_file(): errors.append(p+' missing'); continue
        data=file.read_bytes()
        if p=='Docs/13_POST_I16_PRODUCTION_GAP_AUDIT.md': data=data.split(b'\n## Q1 spatial direction lock and bounded polish - 2026-10-26\n')[0]
        if sha(data)!=h:
            changed.append(p)
            if p not in allowed: errors.append('Unexpected protected change: '+p)
    frozen=verify_preservation.snapshot()
    if old['frozen']!=frozen: errors.append('Frozen Canon/Architecture/RC1 snapshot changed')
    scene=ROOT/'Production/Unity/Assets/Surlhwa/Scenes'
    a,b=blocks(scene/'Bar_QuarterView.unity'),blocks(scene/'Bar_Q1.unity')
    removed=sorted(a.keys()-b.keys()); added=sorted(b.keys()-a.keys())
    if removed!=[1353602159,1353602160]: errors.append('Unexpected removed historical scene blocks')
    changes=[]
    facing={'C_SnowfallMark','Distant west pine','Distant east pine','West winter pine','East winter pine',
      'West snow rocks','East snow rocks','West snow bank','East snow bank','Warm snow lantern',
      'Ceramic pitcher and cups','Clean cloth and glasses','Notebook and candle'}
    contacts={'C_Bartender_Contact','C_Ordinary_Contact','C_Quixote_Contact'}
    planes={'C_Bartender_Plane','C_Ordinary_Plane','C_Quixote_Plane'}
    for key in sorted(a.keys()&b.keys()):
        if a[key]==b[key]: continue
        kind,text=a[key]; go=re.search(r'm_GameObject: \{fileID: (\d+)\}',text)
        name=re.search(r'm_Name: (.*)',a.get(int(go[1]),(0,''))[1] if go else text)
        name=name[1] if name else ''
        ok=(kind==4 and name in facing|contacts|planes|{'Camera_C_PixelCutaway'} or
          kind==23 and name in contacts|planes|{'C_CounterReturn','C_SnowGround'} or
          kind==33 and name in contacts or kind==20 and name=='Camera_C_PixelCutaway' or
          kind==1 and name in {'Quarter View dressing ONLY','Quarter View disposable comparison'} or key==9223372036854775807)
        if not ok: errors.append(f'Unexpected serialized change: {key} {kind} {name}')
        changes.append(dict(id=key,type=kind,name=name))
    # Domain/controller/content/anchors/colliders are serialized identically in this sibling.
    guids={}
    for folder in ('Production/Unity/Assets','Production/Unity/Library/PackageCache'):
        for meta in (ROOT/folder).rglob('*.meta'):
            m=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(errors='ignore'),re.M)
            if m: guids[m[1]]=str(meta)
    references={g for p in [scene/'Bar_Q1.unity',*(ROOT/'Production/Unity/Assets/Surlhwa/Q1Polish').glob('*.mat')]
       for g in re.findall(r'guid: ([a-f0-9]{32})',p.read_text()) if not g.startswith('0000000000000000')}
    missing=sorted(references-guids.keys())
    if missing: errors.append('Missing GUIDs: '+str(missing))
    serial=dict(originalBlocks=len(a),currentBlocks=len(b),addedBlocks=len(added),removedBlocks=removed,
      changedBlocks=changes,types=dict(collections.Counter(str(r['type']) for r in changes)),references=len(references),missingReferences=missing)
    (M/'q1-polish-serialized.json').write_text(json.dumps(serial,indent=2)+'\n')
    package=json.loads((M/'q1-polish-package.json').read_text())
    tests=ET.parse(V/'Evidence/q1-tests-05.xml').getroot().attrib
    build=json.loads((M/'q1-polish-build.json').read_text())
    if tests.get('result')!='Passed' or int(tests.get('passed',0))!=11: errors.append('Targeted tests not passed')
    if build['result']!='Succeeded' or build['errors']: errors.append('Build not succeeded')
    for r in package['shots']+package['clips']+package['previousShots']+package['previousClips']:
        if sha((V/r['path']).read_bytes())!=r['sha256']: errors.append('Evidence hash: '+r['path'])
    for r in package['shots']:
        if r['fixture'] not in ('Seated','Composition','Wide','Grounding','Empty'): continue
        prior=next(s for s in package['previousShots'] if s['actor']==r['actor'] and s['fixture']==r['fixture'] and s['width']==r['width'])
        if any(abs(prior['position'][k]-r['position'][k])>0.00001 for k in ('x','y','z')) or prior['size']!=r['size'] or prior['rotation']!=r['rotation'] or prior['barViewport']!=r['barViewport']:
            errors.append('Q1 camera/exterior framing changed: '+r['file'])
    links=re.findall(r'(?:href|src)="([^"]+)"',(V/'review-q1-polish.html').read_text(encoding='utf-8'))
    broken=[p for p in links if not p.startswith(('http:','https:','#')) and not (V/p).exists()]
    if broken: errors.append('Broken review links: '+str(broken))
    result=dict(verdict='Q1_BOUNDED_POLISH_READY_FOR_OWNER_REVIEW' if not errors else 'IN_PROGRESS',
      activeSpatialBaseline='Q1',q2='REFERENCE_ONLY',c1='HISTORICAL_ACCEPTED_EVIDENCE',
      protectedFiles=len(old['files']),changedExistingAllowed=changed,frozenSnapshotUnchanged=old['frozen']==frozen,
      specifications=len(frozen['specifications']),rc1Head=frozen['rc1']['head'],
      preservedHistoricalNativeFiles=sum(p.startswith('Production/Validation/Evidence/') for p in old['files']),
      tests=tests,build=build,stills=len(package['shots']),clips=len(package['clips']),linksChecked=len(links),errors=errors)
    (M/'q1-polish-validation.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result,indent=2))
    if errors: raise SystemExit(1)
if __name__=='__main__': main()
