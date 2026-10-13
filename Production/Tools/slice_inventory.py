"""Inventory the complete Slice build and its authored Unity/content inputs."""
import argparse
import hashlib
import json
from pathlib import Path

PRODUCTION = Path(__file__).resolve().parents[1]

def inventory():
    groups = {}
    for name, roots in {
        'source': ['Content', 'Unity/Assets', 'Unity/Packages', 'Unity/ProjectSettings'],
        'player': ['Validation/Builds/Slice'],
    }.items():
        files = {}
        for root in roots:
            for path in sorted((PRODUCTION / root).rglob('*')):
                if path.is_file():
                    files[path.relative_to(PRODUCTION).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
        digest = hashlib.sha256(json.dumps(files, sort_keys=True).encode()).hexdigest()
        groups[name] = {'sha256': digest, 'count': len(files), 'files': files}
    return groups

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--write', type=Path)
    parser.add_argument('--verify', type=Path)
    args = parser.parse_args()
    current = inventory()
    if args.write:
        if args.write.exists():
            raise SystemExit(f'Refusing to replace inventory: {args.write}')
        args.write.write_text(json.dumps(current, indent=2), encoding='utf-8')
    if args.verify:
        prior = json.loads(args.verify.read_text(encoding='utf-8-sig'))
        if prior != current:
            raise SystemExit('FAIL: complete source/Player inventory differs')
    print(json.dumps({key: {k: v for k, v in group.items() if k != 'files'} for key, group in current.items()}))
