"""Guard the accepted C handoff and earlier A/B evidence; record once before C1."""
import json
import sys
from c_history import ROOT, digest

MANIFEST = ROOT / 'Validation/Manifests/c1-history.json'

def main():
    if '--record' in sys.argv:
        paths = []
        for directory in ('Unity/Assets/Surlhwa/VariantC', 'Validation/Builds/VariantC'):
            paths.extend(p for p in (ROOT / directory).rglob('*') if p.is_file())
        for path in (ROOT / 'Validation/Evidence').glob('c-*'):
            paths.extend(p for p in path.rglob('*') if p.is_file()) if path.is_dir() else paths.append(path)
        paths.extend(ROOT / p for p in ('Unity/Assets/Surlhwa/Scenes/Bar_C.unity', 'Unity/Assets/Surlhwa/Scenes/Bar_C.unity.meta',
            'Validation/REVIEW_C.md', 'Validation/review-c.html', 'Validation/ABC_COMPARISON.png',
            'Validation/C_REVISION_PROGRESS.md', 'Validation/C_FINAL_REVIEW_RESULT.md'))
        paths.extend(p for p in (ROOT / 'Validation/Manifests').glob('*.json') if 'c1' not in p.name)
        with MANIFEST.open('x', encoding='utf-8') as out:
            json.dump({p.relative_to(ROOT).as_posix(): digest(p) for p in sorted(set(paths))}, out, indent=2)
        print('C historical baseline recorded:', len(set(paths)), 'files')
    else:
        records = json.loads(MANIFEST.read_text(encoding='utf-8'))
        failures = [p for p, sha in records.items() if not (ROOT / p).is_file() or digest(ROOT / p) != sha]
        if failures: raise SystemExit('C HISTORY CHANGED: ' + ', '.join(failures))
        print('C HISTORY PASS:', len(records), 'original files byte-identical')

if __name__ == '__main__': main()
