"""Copy only actual published save generations into an isolated review run."""
from pathlib import Path
import argparse, hashlib, json, shutil
p=argparse.ArgumentParser()
p.add_argument('source',type=Path); p.add_argument('destination',type=Path)
a=p.parse_args()
root=Path(__file__).resolve().parents[1]/'Validation/Evidence'
source=a.source.resolve(); destination=a.destination.resolve()
if not source.is_relative_to(root) or not destination.is_relative_to(root):
    raise SystemExit('Save copies must stay inside Production/Validation/Evidence')
if destination.exists(): raise SystemExit('Preserve existing save copy')
if not (source/'slot1/current.json').is_file(): raise SystemExit('Actual published source save required')
(destination/'slot1').mkdir(parents=True)
files={}
for name in ('current.json','previous.json'):
    file=source/'slot1'/name
    if file.exists():
        shutil.copy2(file,destination/'slot1'/name)
        files[name]=hashlib.sha256(file.read_bytes()).hexdigest()
(destination/'origin.json').write_text(json.dumps(dict(source=str(source),files=files,method='Byte-identical published generations; no construction or payload edits'),indent=2))
print(destination)
