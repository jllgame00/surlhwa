"""Protect accepted Q1/C1 evidence and frozen specifications during integration."""
from pathlib import Path
import json, sys
import q1_preservation

ROOT=q1_preservation.ROOT
OUT=ROOT/'Production/Validation/Manifests'
def snapshot():
    result=q1_preservation.snapshot()
    for folder in ('q1-final-ordinary-04','q1-final-prince-04'):
        for p in (ROOT/'Production/Validation/Evidence'/folder).rglob('*'):
            if p.is_file(): result['files'][p.relative_to(ROOT).as_posix()]=q1_preservation.sha(p)
    return result

if __name__=='__main__':
    path=OUT/'gate2-q1-before.json'
    if sys.argv[1]=='record':
        with path.open('x',encoding='utf-8') as f: json.dump(snapshot(),f,indent=2)
        print('Recorded',path)
    else:
        old=json.loads(path.read_text()); now=snapshot()
        changed=[p for p,h in old['files'].items() if now['files'].get(p)!=h]
        result=dict(changedExisting=changed,frozenUnchanged=old['frozen']==now['frozen'])
        (OUT/'gate2-q1-preservation.json').write_text(json.dumps(result,indent=2)+'\n')
        print(json.dumps(result,indent=2))
