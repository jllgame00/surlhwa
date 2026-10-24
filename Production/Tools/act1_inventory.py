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
    source_manifest=root/'SourceArt/Act1Production/assets.json'
    source_rows={row['runtime'].removeprefix('Production/'):row for row in json.loads(source_manifest.read_text(encoding='utf-8'))['assets']}
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
        if key in source_rows:
            source=source_rows[key]['source']
            origin=('Existing pixel tool copied byte-identically: ' if 'method' in source_rows[key] else 'Original built-in ImageGen atlas; declared crop/palette/nearest normalization: ')+source
        elif '/ProductionArt/' in key:
            origin='Original procedural felt/wood contact; author_act1_footstep.py' if p.suffix=='.wav' else 'ACT 1 generated mesh/material or snow-ground pixels; CampaignProductionArtBuilder, retaining C1 geometry'
        rows.append(dict(path=key,sha256=digest(p),disposition='REAUTHOR_VERSION_FOR_GATE2' if authored else 'RETAIN_CURRENT_REFERENCE_FOR_GATE2',origin=origin,approval='CURRENT_MILESTONE_REVIEW; NOT FINAL ART/AUDIO APPROVAL'))
    path.write_text(json.dumps({'rows':rows,'rightsBoundary':'Original art-pass ImageGen sources and exact prompts are retained separately from inherited reference art. Prior adoption/rights caveats remain; Noto Sans KR uses retained Noto-OFL.txt. Functional acceptance does not approve final assets.','spatialBoundary':'C1 camera, original transforms and colliders retained; production surfaces, modular dressing, character frames and tool shells integrated. Traversal includes retained disabled blockout renderers; a reference is not a claim that it is visible.'},indent=2),encoding='utf-8');print('ART_INVENTORY',len(rows))
else:raise ValueError(mode)
