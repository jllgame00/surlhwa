"""Exact source/Player identity around a Q capture; no writes outside its evidence file."""
import hashlib
import json
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]


def inventory():
    return {p.relative_to(root).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
            for folder in ("Unity/Assets", "Unity/Packages", "Unity/ProjectSettings", "Content/Campaign", "Validation/Builds/Q1Production")
            for p in sorted((root / folder).rglob("*")) if p.is_file()}


mode, name = sys.argv[1:]
path = Path(name)
current = inventory()
if mode == "write":
    with path.open("x", encoding="utf-8") as out:
        json.dump(current, out, indent=2)
elif mode == "verify":
    before = json.loads(path.read_text())
    changed = [p for p in before.keys() | current.keys() if before.get(p) != current.get(p)]
    if changed:
        raise SystemExit("Source/Player changed: " + str(changed))
else:
    raise ValueError(mode)
print("Q_SOURCE_PLAYER_" + mode.upper(), len(current))
