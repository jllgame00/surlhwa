"""Source/import checks only. Does not claim Unity or visual acceptance."""
from pathlib import Path
import hashlib
import json
import re
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'Production/SourceArt/Act1Production'
manifest=json.loads((SOURCE/'assets.json').read_text(encoding='utf-8'))
checks=[]
for name, expected in manifest['sourceHashes'].items():
    assert hashlib.sha256((SOURCE/name).read_bytes()).hexdigest()==expected, name
    checks.append('source '+name)
for row in manifest['assets']:
    path=ROOT/row['runtime']; image=Image.open(path)
    assert hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'], path
    assert list(image.size)==row['dimensions'] and image.mode=='RGBA', path
    assert image.getchannel('A').getbbox(), path
    meta=Path(str(path)+'.meta').read_text()
    assert 'filterMode: 0' in meta and 'enableMipMap: 0' in meta and 'nPOTScale: 0' in meta, path
    assert set(re.findall(r'textureCompression: (\d+)',meta))=={'0'}, path
    if row['name'].startswith(('Bartender','Prince','Ordinary')):
        assert image.size==((96,100) if row['name'].endswith('Seated') else (96,84)), path
        bottom=image.getchannel('A').getbbox()[3]
        assert bottom==(85 if row['name'].endswith('Seated') else 84), (path,bottom)
    checks.append('asset '+row['name'])
guids={}
for meta in (ROOT/'Production/Unity/Assets').rglob('*.meta'):
    match=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8'),re.M)
    if not match:
        continue
    assert match[1] not in guids, (meta,guids.get(match[1]))
    guids[match[1]]=str(meta)
checks.append('unique production asset GUIDs')
report=dict(status='SOURCE_CHECKS_PASS',checks=checks,assets=len(manifest['assets']),
            unityImport='NOT_RUN: license unavailable',player='NOT_REBUILT',
            scope='Hashes, nonempty RGBA, explicit import settings, actor baselines, GUID uniqueness; not rendered quality')
(ROOT/'Production/Validation/Manifests/art-source-checks.json').write_text(json.dumps(report,indent=2)+'\n')
print('SOURCE_CHECKS_PASS',len(checks),'checks;',len(manifest['assets']),'assets; Unity/Player NOT_RUN')
