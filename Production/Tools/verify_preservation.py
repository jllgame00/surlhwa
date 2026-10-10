"""Read-only integrity checks for frozen inputs. --record writes only our baseline."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess

PRODUCTION = Path(__file__).resolve().parents[1]
RC1 = Path(r"D:\univ\surlhwa\Surlhwa")
MIRROR = Path(r"C:\Users\surlw\.codex\.chatgpt-projects\g-p-6a8cfbb439748191ac8fac094112d31b")
BASELINE = PRODUCTION / "Validation/Manifests/preservation-baseline.json"


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git(*args):
    env = dict(os.environ, GIT_OPTIONAL_LOCKS="0")
    return subprocess.check_output(
        ["git", "--no-optional-locks", "-c", "core.fsmonitor=false", "-C", str(RC1), *args],
        env=env,
    )


def snapshot():
    sources = {}
    for corpus in ("SURLHWA_CANON", "SURLHWA_ARCHITECTURE"):
        root = MIRROR / corpus
        if not root.is_dir():
            raise FileNotFoundError(str(root))
        for path in sorted(root.rglob("*")):
            if path.is_file():
                sources[str(path)] = {"sha256": digest(path), "bytes": path.stat().st_size}
    dirty = {}
    paths = git("ls-files", "--modified", "--others", "--exclude-standard", "-z").decode("utf-8").split("\0")
    for relative in sorted(set(filter(None, paths))):
        path = RC1 / relative
        stat = path.stat()
        dirty[relative] = {"sha256": digest(path), "bytes": stat.st_size,
                           "mtime_ns": stat.st_mtime_ns, "attributes": stat.st_file_attributes}
    index = Path(git("rev-parse", "--path-format=absolute", "--git-path", "index").decode().strip())
    return {
        "specifications": sources,
        "rc1": {"path": str(RC1), "branch": git("branch", "--show-current").decode().strip(),
                "head": git("rev-parse", "HEAD").decode().strip(),
                "status": git("status", "--short", "--untracked-files=all").decode("utf-8"),
                "staged": git("diff", "--cached", "--name-only").decode("utf-8"),
                "index_sha256": digest(index), "dirty_files": dirty},
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--record", action="store_true")
    args = parser.parse_args()
    current = snapshot()
    if args.record:
        BASELINE.parent.mkdir(parents=True, exist_ok=True)
        with BASELINE.open("x", encoding="utf-8", newline="\n") as out:
            json.dump(current, out, ensure_ascii=False, indent=2)
            out.write("\n")
        print(f"RECORDED: {len(current['specifications'])} specification files; {len(current['rc1']['dirty_files'])} RC1 dirty paths")
        return
    baseline = json.loads(BASELINE.read_text(encoding="utf-8"))
    if current != baseline:
        changed = []
        for group in ("specifications", "rc1"):
            if current[group] != baseline[group]:
                changed.append(group)
        raise SystemExit("PRESERVATION FAILED: " + ", ".join(changed))
    print(f"PRESERVATION PASS: {len(current['specifications'])} specification hashes; RC1 branch/HEAD/status/index and {len(current['rc1']['dirty_files'])} dirty paths unchanged")


if __name__ == "__main__":
    main()
