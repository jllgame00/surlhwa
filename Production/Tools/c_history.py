"""Immutable A/B comparison guard. Record once before C, verify thereafter."""
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / 'Validation/Manifests/c-history.json'

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    if '--record' in sys.argv:
        paths = []
        for directory in ('Validation/Evidence/captures-reviewed', 'Validation/Builds/Windows',
                          'Unity/Assets/Surlhwa/Art', 'Unity/Assets/Surlhwa/Meshes', 'Unity/Assets/Surlhwa/Materials'):
            paths.extend(p for p in (ROOT / directory).rglob('*') if p.is_file())
        paths.extend(p for p in (ROOT / 'Validation/Manifests').glob('*.json') if p != MANIFEST)
        paths.extend(ROOT / p for p in ('Validation/REVIEW_HG01_HG03.md', 'Validation/review.html',
                     'Validation/HG01_HG03_COMPARISON.png', 'Unity/Assets/Surlhwa/Scenes/Bar.unity',
                     'Unity/Assets/Surlhwa/Scenes/Bootstrap.unity'))
        with MANIFEST.open('x', encoding='utf-8') as out:
            json.dump({p.relative_to(ROOT).as_posix(): digest(p) for p in sorted(set(paths))}, out, indent=2)
        print(f'A/B historical baseline recorded: {len(set(paths))} files')
    else:
        records = json.loads(MANIFEST.read_text(encoding='utf-8'))
        failures = [p for p, sha in records.items() if not (ROOT / p).is_file() or digest(ROOT / p) != sha]
        if failures:
            raise SystemExit('A/B HISTORY CHANGED: ' + ', '.join(failures))
        print(f'A/B HISTORY PASS: {len(records)} original files byte-identical')

if __name__ == '__main__':
    main()
