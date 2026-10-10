"""Inspect real native Player captures. No image is generated or repaired here."""
import hashlib
import json
import re
import sys
from pathlib import Path
from PIL import Image, ImageChops, ImageStat

PRODUCTION = Path(__file__).resolve().parents[1]

def review_outcome_failures(run, expected, process, log):
    failures = []
    if not expected.get('runId') or any(x.get('runId') != expected['runId'] for x in (run, process)):
        failures.append('Capture/process identity does not match this requested run')
    if process.get('exitCode') != 0: failures.append('Player process did not exit successfully')
    if run.get('initialized') is not True: failures.append('Stage initialization did not succeed')
    if run.get('errors'): failures.append('Player reported errors')
    if 'SPATIAL_CAPTURE_FINISHED 24 files; errors 0' not in log: failures.append('Complete capture marker missing from Player log')
    if re.search(r'(?im)(?:^[\w.]*Exception:|^Error:|^Assertion failed|^Fatal error|^Crash!!!|^Shader error |UnityEngine\.Debug:LogError|UnityEngine\.Debug:LogAssertion|UnityEngine\.DebugLogHandler:LogException)', log):
        failures.append('Complete Player log contains an error, exception, assertion or crash')
    return failures

def inspect(folder):
    folder = folder.resolve()
    run = json.loads((folder / 'player-run.json').read_text(encoding='utf-8-sig'))
    expected_run = json.loads((folder / 'expected-run.json').read_text(encoding='utf-8-sig'))
    process = json.loads((folder / 'os-memory.json').read_text(encoding='utf-8-sig'))
    log = Path(expected_run['logPath']).read_text(encoding='utf-8-sig')
    failures = review_outcome_failures(run, expected_run, process, log)
    if failures:
        return {'schemaVersion': 1, 'result': 'FAIL', 'failures': failures, 'images': [], 'evidencePath': str(folder), 'player': run}
    images, diagnostics = [], []
    expected = {(c, f, w, w * 9 // 16) for c in ('OrthographicA', 'PerspectiveB')
                for f in ('Empty', 'OrdinarySeated', 'QuixoteSeated', 'Composition', 'GroundingInspection', 'Standing') for w in (1280, 1920)}
    actual = {(c['camera'], c['fixture'], c['width'], c['height']) for c in run['captures']}
    if expected != actual: failures.append('Capture matrix mismatch')
    if run['staticRootAndFeetDrift']: failures.append('Static actor drift')
    clips = json.loads((folder / 'motion-clips.json').read_text(encoding='utf-8-sig'))
    if len(clips) != 4: failures.append('Four native motion clips required')
    for clip in clips:
        stream = clip['stream']; file = folder / clip['file']
        if int(stream['nb_frames']) != 121 or stream['r_frame_rate'] != '12/1': failures.append('Motion stream mismatch: ' + file.name)
        if hashlib.sha256(file.read_bytes()).hexdigest() != clip['sha256']: failures.append('Motion fingerprint mismatch: ' + file.name)
    for capture in run['captures']:
        file = folder / capture['file']
        with Image.open(file) as source:
            image = source.convert('RGB')
            if image.size != (capture['width'], capture['height']): failures.append(f'Wrong resolution: {file.name}')
            colors = image.getcolors(image.width * image.height)
            magenta = sum(n for n, (r, g, b) in colors if r > 200 and b > 200 and g < 65)
            if magenta > 20: failures.append(f'Missing-shader magenta: {file.name}')
            if len(colors) < 40: failures.append(f'Blank/near-uniform render: {file.name}')
            images.append({'file': file.name, 'sha256': hashlib.sha256(file.read_bytes()).hexdigest(),
                           'width': image.width, 'height': image.height, 'colors': len(colors), 'magentaPixels': magenta})
    for measurement in run['measurements']:
        if measurement.get('durationSeconds', 0) < 60 or measurement['samples'] < 300:
            failures.append('Insufficient rendered performance interval: ' + measurement['variant'])
        if measurement['drawsMean'] <= 0 or measurement['batchesMean'] <= 0:
            failures.append('No actual render submissions in baseline: ' + measurement['variant'])
    for variant in ('OrthographicA', 'PerspectiveB'):
        baseline = Image.open(folder / f'{variant}_Composition_1920x1080.png').convert('RGB')
        diagnostic = Image.open(folder / f'{variant}_CounterRemovedDiagnostic_1920x1080.png').convert('RGB')
        row = next(c for c in run['captures'] if c['camera'] == variant and c['fixture'] == 'Composition' and c['width'] == 1920)
        quad = row['bartenderViewportQuad']
        rect = (round(min(v['x'] for v in quad) * 1920), round((1 - max(v['y'] for v in quad)) * 1080),
                round(max(v['x'] for v in quad) * 1920), round((1 - min(v['y'] for v in quad)) * 1080))
        # Measure opaque lower-body source pixels revealed when real counter geometry is removed.
        alpha = Image.open(PRODUCTION / 'Unity/Assets/Surlhwa/Art/Bartender.png').getchannel('A')
        lower = alpha.copy(); lower.paste(0, (0, 0, alpha.width, alpha.height // 2))
        mask = lower.resize((rect[2] - rect[0], rect[3] - rect[1]), Image.Resampling.NEAREST)
        diff = ImageChops.difference(baseline.crop(rect), diagnostic.crop(rect))
        changed = sum(1 for rgb, a in zip(diff.getdata(), mask.getdata()) if a >= 102 and max(rgb) > 15)
        if changed < 100: failures.append('Counter removal did not reveal bartender lower body: ' + variant)
        diagnostics.append({'variant': variant, 'bartenderRectanglePixels': rect, 'opaqueLowerBodyChangedPixels': changed,
                            'method': 'Same Player camera and actors. Only counter renderers disabled for diagnostic; real depth relationship, not artistic quality.'})
        for width in (1280, 1920):
            frames = sorted((folder / f'{variant}_motion_{width}').glob('*.png'))
            if len(frames) != 121: failures.append(f'Motion frame count {variant}/{width}: {len(frames)}')
            for file in frames:
                with Image.open(file) as frame:
                    if frame.size != (width, width * 9 // 16): failures.append('Motion resolution: ' + file.name)
    fingerprint_files = sorted(p for root in ('Content', 'Unity/Assets/Surlhwa', 'Unity/Packages')
                               for p in (PRODUCTION / root).rglob('*') if p.is_file())
    fingerprint = hashlib.sha256(''.join(f'{p.relative_to(PRODUCTION).as_posix()}:{hashlib.sha256(p.read_bytes()).hexdigest()}\n'
                                       for p in fingerprint_files).encode()).hexdigest()
    result = {'schemaVersion': 1, 'result': 'PASS' if not failures else 'FAIL', 'failures': failures,
              'evidenceTier': run['tier'], 'evidencePath': str(folder), 'sourceContentAndAssetFingerprint': fingerprint,
              'images': images, 'motionClips': clips, 'counterDiagnostics': diagnostics, 'player': run,
              'processOutcome': {'runId': expected_run['runId'], 'exitCode': process['exitCode'],
                                 'playerLogSha256': hashlib.sha256(Path(expected_run['logPath']).read_bytes()).hexdigest(),
                                 'nativeAllocationWarnings': log.count('deleting an allocation that is older than its permitted lifetime')},
              'humanGateStatus': {'HG01': 'OWNER_REVIEW_PENDING', 'HG02': 'OWNER_REVIEW_PENDING', 'HG03': 'OWNER_REVIEW_PENDING'},
              'limits': ['Automated replay is not native gameplay input.', 'Pose swaps are reference animation, not final seated motion.',
                         'No GPU frame-time/VRAM telemetry; Development/offscreen timing does not establish minimum specs.',
                         'Raw Player Mono working-set value 0 is unavailable; use external os-memory.json.']}
    return result

if __name__ == '__main__':
    result = inspect(Path(sys.argv[1]) if len(sys.argv) > 1 else PRODUCTION / 'Validation/Evidence/captures')
    (PRODUCTION / 'Validation/Manifests/captures.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(result['result'], len(result['images']), 'native captures;', result['failures'])
    sys.exit(0 if result['result'] == 'PASS' else 1)
