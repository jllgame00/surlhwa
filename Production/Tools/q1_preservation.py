"""Snapshot this bounded pass without rewriting historical evidence."""
from pathlib import Path
import hashlib, json, subprocess, sys
import verify_preservation
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Production/Validation/Manifests'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def snapshot():
    paths = subprocess.check_output(['git','ls-files','-z'], cwd=ROOT).decode().split('\0')
    files = {p:sha(ROOT/p) for p in paths if p and (ROOT/p).is_file()}
    for folder in ('quarter-final-ordinary-02','quarter-final-prince-02'):
        for p in (ROOT/'Production/Validation/Evidence'/folder).rglob('*'):
            if p.is_file(): files[p.relative_to(ROOT).as_posix()] = sha(p)
    return dict(files=files, frozen=verify_preservation.snapshot())
if __name__ == '__main__':
    path=OUT/'q1-polish-before.json'
    if sys.argv[1]=='record':
        with path.open('x',encoding='utf-8') as f: json.dump(snapshot(),f,indent=2)
        print('Recorded baseline',path)
    else:
        old=json.loads(path.read_text()); current=snapshot()
        changes=[p for p,h in old['files'].items() if current['files'].get(p)!=h]
        result=dict(changedExisting=changes,frozenUnchanged=old['frozen']==current['frozen'])
        (OUT/'q1-polish-preservation.json').write_text(json.dumps(result,indent=2)+'\n')
        print(json.dumps(result,indent=2))
