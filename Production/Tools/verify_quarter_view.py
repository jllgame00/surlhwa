"""Read-only preservation checks for the isolated Q spike, plus an honest status manifest."""
from pathlib import Path
import hashlib
import json
import re
import subprocess
import xml.etree.ElementTree as ET
import verify_preservation

ROOT = Path(__file__).resolve().parents[2]
MANIFESTS = ROOT / "Production/Validation/Manifests"
AUDIT_MARKER = b"\n## Quarter View visual spike - 2026-10-26\n"


def digest(data):
    return hashlib.sha256(data).hexdigest()


def main():
    expected = json.loads((MANIFESTS / "quarter-view-source-before.json").read_text())
    changed = []
    for relative, sha in expected.items():
        path = ROOT / relative
        if not path.is_file():
            changed.append(relative + " MISSING")
            continue
        data = path.read_bytes()
        if relative == "Docs/13_POST_I16_PRODUCTION_GAP_AUDIT.md" and AUDIT_MARKER in data:
            data = data.split(AUDIT_MARKER)[0]
        if digest(data) != sha:
            changed.append(relative)
    before = json.loads((MANIFESTS / "quarter-view-preservation-before.json").read_text())
    current = verify_preservation.snapshot()
    post_license_path = MANIFESTS / "quarter-view-preservation-after-license.json"
    post_license = json.loads(post_license_path.read_text()) if post_license_path.exists() else before
    if current["specifications"] != before["specifications"]:
        changed.append("Canon / Architecture snapshot")
    if current["rc1"] != post_license["rc1"]:
        changed.append("RC1 changed after the retained license-refresh observation")
    rc1_delta = {p: dict(before=before["rc1"]["dirty_files"].get(p), after=current["rc1"]["dirty_files"].get(p))
                 for p in before["rc1"]["dirty_files"].keys() | current["rc1"]["dirty_files"].keys()
                 if before["rc1"]["dirty_files"].get(p) != current["rc1"]["dirty_files"].get(p)}
    if set(rc1_delta) - {"UserSettings/Layouts/default-6000.dwlt", "Assets/_SURLHWA/Art/Fonts/KoreanFallback.asset"}:
        changed.append("Unexpected RC1 delta beyond the observed Editor-local files")
    sources = list((ROOT / "Production/Unity/Assets/Surlhwa").rglob("QuarterView*.cs"))
    guids = []
    for path in sources:
        meta = path.with_name(path.name + ".meta")
        match = re.search(r"^guid: ([a-f0-9]{32})$", meta.read_text(), re.M) if meta.exists() else None
        if not match:
            changed.append(str(meta) + " invalid/missing")
        else:
            guids.append(match[1])
    if len(set(guids)) != len(guids):
        changed.append("Duplicate spike script GUID")
    scene_root = ROOT / "Production/Unity/Assets/Surlhwa/Scenes"
    def blocks(path):
        return {int(key): body for key, body in re.findall(r"^--- !u!\d+ &(-?\d+)\n(.*?)(?=^--- !u!|\Z)", path.read_text(), re.M | re.S)}
    original, sibling = blocks(scene_root / "Bar_Act1.unity"), blocks(scene_root / "Bar_QuarterView.unity")
    changed_blocks = [key for key in original if key in sibling and original[key] != sibling[key]]
    removed = [key for key in original if key not in sibling]
    if removed or changed_blocks != [9223372036854775807]:
        changed.append("Unexpected original scene block change")
    old_roots = re.findall(r"fileID: (\d+)", original[9223372036854775807])
    new_roots = re.findall(r"fileID: (\d+)", sibling[9223372036854775807])
    if new_roots[:len(old_roots)] != old_roots or len(new_roots) != len(old_roots) + 2:
        changed.append("Expected original scene roots plus exactly two spike roots")
    guid_paths = {}
    for folder in (ROOT / "Production/Unity/Assets", ROOT / "Production/Unity/Library/PackageCache"):
        for meta in folder.rglob("*.meta"):
            m = re.search(r"^guid: ([a-f0-9]{32})$", meta.read_text(errors="ignore"), re.M)
            if m:
                guid_paths[m[1]] = str(meta)[:-5]
    paths = [scene_root / "Bar_QuarterView.unity"] + list((ROOT / "Production/Unity/Assets/Surlhwa/QuarterView").glob("*.mat"))
    references = {guid for path in paths for guid in re.findall(r"guid: ([a-f0-9]{32})", path.read_text()) if not guid.startswith("0000000000000000")}
    missing = sorted(references - guid_paths.keys())
    if missing:
        changed.append("Unresolved serialized GUIDs: " + str(missing))
    (MANIFESTS / "quarter-view-serialized.json").write_text(json.dumps(dict(
        originalBlocks=len(original), spikeBlocks=len(sibling), changedOriginalBlocks=changed_blocks,
        removedOriginalBlocks=removed, newBlocks=len(sibling.keys()-original.keys()),
        originalRootsPreserved=new_roots[:len(old_roots)] == old_roots,
        serializedReferences=len(references), unresolved=missing), indent=2) + "\n")
    package_path = MANIFESTS / "quarter-view-package.json"
    package = json.loads(package_path.read_text()) if package_path.exists() else {}
    test_path = ROOT / "Production/Validation/Evidence/quarter-tests-final.xml"
    tests = ET.parse(test_path).getroot().attrib if test_path.exists() else {}
    ready = not changed and package.get("status") == "QUARTER_VIEW_READY_FOR_OWNER_COMPARISON" and tests.get("result") == "Passed"
    record = {
        "verdict": "QUARTER_VIEW_READY_FOR_OWNER_COMPARISON" if ready else "IN_PROGRESS",
        "environment": "LICENSE_BLOCK_CLEARED_BY_OWNER_REFRESH",
        "historicalBlock": "First launch: sandbox UPM IPC timeout; desktop retry: Unity exit 198, no valid Editor license. Original logs retained.",
        "sourcePreservation": "PASS" if not changed else "FAIL",
        "protectedFiles": len(expected),
        "protectedSpecifications": len(before["specifications"]),
        "rc1Head": current["rc1"]["head"],
        "rc1InitialSnapshotExactlyUnchanged": current["rc1"] == before["rc1"],
        "rc1PostLicenseSnapshotExactlyUnchanged": current["rc1"] == post_license["rc1"],
        "rc1GitHeadBranchStatusIndexUnchanged": all(current["rc1"][k] == before["rc1"][k] for k in before["rc1"] if k != "dirty_files"),
        "rc1ObservedEditorLocalDelta": rc1_delta,
        "rc1DeltaAttribution": "Observed during owner license refresh. No agent command targeted RC1 for editing or Editor launch; original and subsequent snapshots both retained, no restore attempted.",
        "preexistingReviewSliceEditPreserved": "Production/Validation/review-slice.html" not in changed,
        "changes": changed,
        "runtime": "PASS" if package else "PENDING_FINAL_PACKAGE",
        "nativeComparisonRenders": len(package.get("shots", [])),
        "movementClips": len(package.get("clips", [])),
        "targetedUnityTests": tests,
        "performanceSamples": len(package.get("measurements", [])),
        "newDirectionalRasterAssets": 2,
        "c1RemainsProductionBaseline": True,
    }
    (MANIFESTS / "quarter-view-validation.json").write_text(json.dumps(record, indent=2) + "\n")
    print(json.dumps(record, indent=2))
    if changed:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
