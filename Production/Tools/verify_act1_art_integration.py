"""Audit actual serialized art wiring and frozen references; never visual approval."""
from pathlib import Path
import hashlib
import importlib.util
import json
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
PRODUCTION = ROOT / 'Production'
ASSETS = PRODUCTION / 'Unity/Assets'
ART = ASSETS / 'Surlhwa/Act1Assets/ProductionArt'
SCENE = ASSETS / 'Surlhwa/Scenes/Bar_Act1.unity'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def blocks(text):
    return {m[1]: m[0] for m in re.findall(
        r'(^--- !u!\d+ &(\d+).*?)(?=^--- !u!|\Z)', text, re.S | re.M)}


before = blocks(subprocess.check_output([
    'git', 'show', 'dcc0297:' + SCENE.relative_to(ROOT).as_posix()], text=True))
scene_text = SCENE.read_text(encoding='utf-8')
after = blocks(scene_text)
assert not set(before) - set(after), 'Original scene objects were removed'
changed = [key for key in before if before[key] != after[key]]
allowed = {'MeshRenderer:', 'MeshFilter:', 'MonoBehaviour:', 'SceneRoots:'}
assert all(before[key].splitlines()[1] in allowed for key in changed), 'Unrelated original scene change'
for key, block in before.items():
    if block.splitlines()[1] in {'Transform:', 'Camera:', 'BoxCollider:'}:
        assert block == after[key], 'C1 spatial block changed: ' + key
for ref in re.findall(r'\{fileID: (\d+)\}', scene_text):
    assert ref == '0' or ref in after, 'Missing scene object: ' + ref

guid_paths = {}
for meta in ASSETS.rglob('*.meta'):
    match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8'), re.M)
    if match:
        assert match[1] not in guid_paths, 'Duplicate asset GUID: ' + match[1]
        guid_paths[match[1]] = Path(str(meta)[:-5])

files = [SCENE] + [p for p in ART.iterdir() if p.suffix in {'.mat', '.asset'}]
refs = set()
for path in files:
    text = path.read_text(encoding='utf-8')
    refs.update(re.findall(r'guid: ([a-f0-9]{32})', text))
    if path.suffix == '.mat':
        assert re.search(r'_BaseMap:\s+m_Texture: \{fileID: 2800000, guid:', text), path
    assert not re.search(r'm_Script: \{fileID: 0\}', text), 'Missing script: ' + str(path)
package_refs = {}
for guid in sorted(refs):
    if guid.startswith('0000000000000000'):
        continue  # Unity built-ins.
    if guid in guid_paths:
        assert guid_paths[guid].is_file(), guid_paths[guid]
        continue
    result = subprocess.run(['rg', '-l', '--glob', '*.meta', '^guid: ' + guid + '$',
                             str(PRODUCTION / 'Unity/Library/PackageCache')],
                            capture_output=True, text=True)
    assert result.returncode == 0, 'Unresolved imported package reference: ' + guid
    package_refs[guid] = result.stdout.splitlines()

manifest = json.loads((PRODUCTION / 'SourceArt/Act1Production/assets.json').read_text(encoding='utf-8'))
for name, expected in manifest['sourceHashes'].items():
    source = PRODUCTION / 'SourceArt/Act1Production' / name
    assert digest(source) == expected, name
    original = subprocess.check_output(['git', 'show', 'dcc0297:' + source.relative_to(ROOT).as_posix()])
    assert hashlib.sha256(original).hexdigest() == expected, name
for row in manifest['assets']:
    assert digest(ROOT / row['runtime']) == row['sha256'], row['name']
    original = subprocess.check_output(['git', 'show', 'dcc0297:' + row['runtime']])
    assert hashlib.sha256(original).hexdigest() == row['sha256'], row['name']
    meta = Path(row['runtime'] + '.meta')
    original_meta = subprocess.check_output(['git', 'show', 'dcc0297:' + meta.as_posix()])
    assert (ROOT / meta).read_bytes() == original_meta, 'Checkpoint importer changed: ' + row['name']

spec = importlib.util.spec_from_file_location('preservation', PRODUCTION / 'Tools/verify_preservation.py')
preservation = importlib.util.module_from_spec(spec)
spec.loader.exec_module(preservation)
resume = json.loads((PRODUCTION / 'Validation/Manifests/art-resume-preservation.json').read_text(encoding='utf-8'))
assert preservation.snapshot() == resume['resumeSnapshot'], 'Frozen inputs changed during resumed work'
assert digest(PRODUCTION / 'Validation/review-slice.html') == 'b18d7ca3459788e6232b3f4156990a50d45b4f0aaa495544f929ab6c3557eecf'

report = dict(status='PASS', sourceCheckpoint='dcc0297', sourceImagesUnchanged=43,
              sourceAtlasesUnchanged=len(manifest['sourceHashes']), checkpointImportersUnchanged=43,
              sceneBlocksBefore=len(before), sceneBlocksAfter=len(after),
              addedSceneBlocks=len(set(after) - set(before)), removedSceneBlocks=0,
              changedExistingBlocks=len(changed), spatialBlocksUnchanged=True,
              checkedSerializedFiles=len(files), resolvedReferenceGuids=len(refs),
              packageReferences=package_refs, resumePreservation='PASS',
              historicalBaselineDrift='Retained separately: owner layout bytes and font mtime predate resume',
              sceneSha256=digest(SCENE), scope='Serialized references/import-source contracts, not rendered or aesthetic approval')
(PRODUCTION / 'Validation/Manifests/art-integration-checks.json').write_text(
    json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps(report, indent=2))
