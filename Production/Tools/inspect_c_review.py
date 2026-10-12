"""Validate real C Player evidence; never generate or repair native originals."""
import hashlib
import json
import sys
from pathlib import Path
from PIL import Image, ImageChops
from inspect_captures import review_outcome_failures

ROOT = Path(__file__).resolve().parents[1]
FIXTURES = ('Empty', 'OrdinarySeated', 'SpecialSeated', 'Composition', 'GroundingInspection', 'Wide')
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def load(path): return json.loads(path.read_text(encoding='utf-8-sig'))

def outcome_failures(run, expected, process, log, variant='C'):
    failures = review_outcome_failures(run, expected, process, log, variant + '_CAPTURE_FINISHED 12 files; errors 0')
    if run.get('preview') is not False: failures.append('Preview cannot be final C evidence')
    if run.get('staticRootAndFeetDrift') is not False: failures.append('C static drift not ruled out')
    desired = {(f, w, w*9//16, f'{variant}_{f}_{w}x{w*9//16}.png') for f in FIXTURES for w in (1280,1920)}
    captures = run.get('captures') or []
    actual = {(c.get('fixture'), c.get('width'), c.get('height'), c.get('file')) for c in captures}
    if len(captures) != 12 or actual != desired: failures.append('C native capture matrix incomplete or duplicated')
    m = run.get('measurement') or {}
    if m.get('durationSeconds', 0) < 60 or m.get('samples', 0) < 300: failures.append('Insufficient C performance interval')
    if m.get('drawsMean', 0) <= 0 or m.get('batchesMean', 0) <= 0: failures.append('No actual C render submissions')
    if not all(m.get(k) is True for k in ('gcRecorder', 'drawRecorder', 'batchRecorder')): failures.append('C profiler counters unavailable')
    return failures

def inspect(folder, variant='C'):
    run = load(folder / 'player-run.json'); expected = load(folder / 'expected-run.json'); process = load(folder / 'os-memory.json')
    log_path = Path(expected['logPath']); log = log_path.read_text(encoding='utf-8-sig')
    failures = outcome_failures(run, expected, process, log, variant); images = []; clips = []; diagnostics = []
    executable = Path(expected['executable'])
    if not executable.is_file() or sha(executable) != expected['executableSha256']: failures.append('C executable changed since capture')
    if not failures:
        for row in run['captures']:
            file = folder / row['file']
            with Image.open(file) as source:
                im = source.convert('RGB'); colors = im.getcolors(im.width * im.height)
                magenta = sum(n for n, (r,g,b) in colors if r > 200 and b > 200 and g < 65)
                if im.size != (row['width'], row['height']): failures.append('C wrong native dimensions: ' + file.name)
                if magenta > 20 or len(colors) < 40: failures.append('C missing shader or blank image: ' + file.name)
                images.append(dict(row, sha256=sha(file), colors=len(colors), magentaPixels=magenta))
        clips = load(folder / 'motion-clips.json')
        if {c['file'] for c in clips} != {f'{variant}_motion_1280.mp4', f'{variant}_motion_1920.mp4'} or len(clips) != 2: failures.append('Two C motion clips required')
        for clip in clips:
            stream = clip['stream']; width = int(stream['width']); height = int(stream['height'])
            if width not in (1280,1920) or height != width*9//16 or int(stream['nb_frames']) != 121 or stream['r_frame_rate'] != '12/1': failures.append('C motion stream mismatch')
            if sha(folder / clip['file']) != clip['sha256']: failures.append('C motion identity mismatch')
            frames = sorted((folder / f'{variant}_motion_{width}').glob('*.png'))
            if len(frames) != 121: failures.append('C motion frame sequence incomplete')
            for file in frames:
                with Image.open(file) as im:
                    if im.size != (width,height): failures.append('C motion frame size mismatch')
        with Image.open(folder / f'{variant}_Composition_1920x1080.png') as source, Image.open(folder / f'{variant}_CounterRemovedDiagnostic_1920x1080.png') as removed:
            row = next(r for r in run['captures'] if r['fixture'] == 'Composition' and r['width'] == 1920)
            quad = row['bartenderViewportQuad']
            rect = (round(min(v['x'] for v in quad)*1920), round((1-max(v['y'] for v in quad))*1080), round(max(v['x'] for v in quad)*1920), round((1-min(v['y'] for v in quad))*1080))
            alpha = Image.open(ROOT / 'Unity/Assets/Surlhwa/Art/Bartender.png').getchannel('A')
            alpha.paste(0, (0,0,alpha.width,alpha.height//2)); mask = alpha.resize((rect[2]-rect[0],rect[3]-rect[1]),Image.Resampling.NEAREST)
            diff = ImageChops.difference(source.convert('RGB').crop(rect), removed.convert('RGB').crop(rect))
            changed = sum(1 for rgb, a in zip(diff.getdata(), mask.getdata()) if a >= 102 and max(rgb) > 15)
            if changed < 100: failures.append('C actual counter did not reveal bartender lower body')
            diagnostics.append({'file': f'{variant}_CounterRemovedDiagnostic_1920x1080.png', 'sha256': sha(folder / f'{variant}_CounterRemovedDiagnostic_1920x1080.png'),
                                'opaqueLowerBodyChangedPixels': changed, 'bartenderRectanglePixels': rect})
    return {'schemaVersion': 1, 'result': 'FAIL' if failures else 'PASS', 'failures': failures, 'evidencePath': str(folder.resolve()),
            'images': images, 'motionClips': clips, 'counterDiagnostics': diagnostics, 'player': run,
            'processOutcome': {'runId': expected['runId'], 'exitCode': process['exitCode'], 'logSha256': sha(log_path),
                               'executableSha256': expected['executableSha256'], 'nativeAllocationWarnings': log.count('deleting an allocation that is older than its permitted lifetime')},
            'ownerGate': {g: 'READY_FOR_OWNER_REVIEW' for g in ('HG01','HG02','HG03')}, 'ownerJudgment': 'PENDING',
            'limits': ['No final camera/art selection.', 'Deterministic pose replay, not gameplay locomotion.', 'Static authored snowfall marks; production weather/audio outside scope.',
                       'Development/offscreen timing is not GPU frame time, display latency or a minimum hardware promise.']}

if __name__ == '__main__':
    result = inspect(Path(sys.argv[1]).resolve())
    (ROOT / 'Validation/Manifests/captures-c.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(result['result'], len(result['images']), 'native C captures;', result['failures'])
    sys.exit(0 if result['result'] == 'PASS' else 1)
