"""Preserve the approved C1 spatial assets and review evidence throughout HG04."""
import json
import sys
from c_history import ROOT, digest

MANIFEST = ROOT / 'Validation/Manifests/slice-spatial-baseline.json'

def main():
    if '--record' in sys.argv:
        paths = []
        for directory in ('Unity/Assets/Surlhwa/VariantC1', 'Validation/Builds/VariantC1'):
            paths.extend(p for p in (ROOT / directory).rglob('*') if p.is_file())
        for path in (ROOT / 'Validation/Evidence').glob('c1-*'):
            paths.extend(p for p in path.rglob('*') if p.is_file()) if path.is_dir() else paths.append(path)
        paths.extend(ROOT / p for p in ('Unity/Assets/Surlhwa/Scenes/Bar_C1.unity', 'Unity/Assets/Surlhwa/Scenes/Bar_C1.unity.meta',
            'Validation/REVIEW_C1.md', 'Validation/review-c1.html', 'Validation/C_C1_COMPARISON.png',
            'Validation/C1_PROGRESS.md', 'Validation/C1_REVIEW_BRIEF.md'))
        paths.extend(p for p in (ROOT / 'Validation/Manifests').glob('*.json') if 'c1' in p.name)
        with MANIFEST.open('x', encoding='utf-8') as out:
            json.dump({p.relative_to(ROOT).as_posix(): digest(p) for p in sorted(set(paths))}, out, indent=2)
        print('C1 baseline recorded:', len(set(paths)), 'files')
    else:
        records = json.loads(MANIFEST.read_text(encoding='utf-8'))
        failures = [p for p, sha in records.items() if not (ROOT / p).is_file() or digest(ROOT / p) != sha]
        if failures:
            raise SystemExit('C1 BASELINE CHANGED: ' + ', '.join(failures))
        print('C1 BASELINE PASS:', len(records), 'files byte-identical')

if __name__ == '__main__':
    main()
