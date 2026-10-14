"""Exact ACT 1 source/Player inventory and referenced art dispositions. Read-only inputs."""
from pathlib import Path
import hashlib,json,re,sys,datetime
root=Path(__file__).resolve().parents[1]
def digest(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def inventory():
    files=[]
    for folder in ['Unity/Assets','Unity/Packages','Unity/ProjectSettings','Content/Campaign','Validation/Builds/Act1']:
        files += [p for p in (root/folder).rglob('*') if p.is_file()]
    return {str(p.relative_to(root)).replace('\\','/'):digest(p) for p in sorted(files)}
mode,path=sys.argv[1:3]; path=Path(path)
if mode=='write':
    entries=inventory(); path.write_text(json.dumps({'utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'files':entries},indent=2),encoding='utf-8');print('INVENTORY',len(entries))
elif mode=='verify':
    old=json.loads(path.read_text(encoding='utf-8'))['files'];now=inventory();changes=[p for p in old.keys()|now.keys() if old.get(p)!=now.get(p)]
    if changes: print(json.dumps(changes,indent=2));sys.exit(2)
    print('INVENTORY_UNCHANGED',len(now))
elif mode=='art':
    assets=root/'Unity/Assets';meta={}
    for p in assets.rglob('*.meta'):
        m=re.search(r'^guid: ([a-f0-9]{32})$',p.read_text(encoding='utf-8'),re.M)
        if m: meta[m[1]]=Path(str(p)[:-5])
    queue=[assets/'Surlhwa/Scenes/Bar_Act1.unity'];seen=set()
    while queue:
        p=queue.pop()
        if p in seen or not p.is_file():continue
        seen.add(p)
        if p.suffix in ['.unity','.prefab','.mat','.asset']:
            for guid in re.findall(r'guid: ([a-f0-9]{32})',p.read_text(encoding='utf-8',errors='ignore')):
                if guid in meta:queue.append(meta[guid])
    rows=[]
    for p in sorted(seen):
        if p.suffix not in ['.png','.wav','.ttf','.mat','.asset']:continue
        key=str(p.relative_to(root)).replace('\\','/')
        authored='/Act1Assets/' in key and p.name!='Act1Korean.asset'
        origin='ACT 1 code-authored derivative or original procedural audio; see CampaignArtBuilder / author_act1_audio.py' if authored else 'Retained C1/Slice reference; historical provenance remains applicable'
        rows.append(dict(path=key,sha256=digest(p),disposition='REAUTHOR_VERSION_FOR_GATE2' if authored else 'RETAIN_CURRENT_REFERENCE_FOR_GATE2',origin=origin,approval='CURRENT_MILESTONE_REVIEW; NOT FINAL ART/AUDIO APPROVAL'))
    path.write_text(json.dumps({'rows':rows,'rightsBoundary':'Internal procedural/source-tracked candidates. Inherited imagegen references remain pending final asset adoption/rights review; Noto Sans KR uses retained Noto-OFL.txt. No claim of legal clearance from functional acceptance.','spatialBoundary':'C1 camera and geometry preserved; winter ground material, restrained props and ordinary coat tint are ACT 1 derivatives.'},indent=2),encoding='utf-8');print('ART_INVENTORY',len(rows))
else:raise ValueError(mode)
