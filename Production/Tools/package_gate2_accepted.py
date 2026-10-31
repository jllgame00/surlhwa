"""Add the accepted-pose Gate 2 entry; preserve all earlier review artifacts.

Run from any directory after the four named Player runs. No gameplay mutation.
Verification is fail-closed; reports describe simulated input, not OS input.
"""
from pathlib import Path
import hashlib
import html
import json
import re
import struct
import subprocess

P = Path(__file__).resolve().parents[1]
V = P / 'Validation'
E = V / 'Evidence'
BASELINE = 'a19a84a'
RUNS = [
    ('gate2-accepted-open-20261031-retry', 'Complete Open ACT 1', 720),
    ('gate2-accepted-quiet-20261031', 'Earned Warmth checkpoint through Quiet', 1080),
    ('gate2-accepted-continue-open-20261031', 'Completed Open Continue', 1080),
    ('gate2-accepted-continue-quiet-20261031', 'Completed Quiet Continue', 720),
]
Q1_CHECKS = [
    'Q1 Production presentation root resolves separately from controller',
    'Q1 restored after final service',
    'earned star follows Q1 facing',
    'focus reveal is created once for services, absent on completed Continue',
    'focus reveal cleaned up after service',
    'active renderers have materials and shaders',
]


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def link(path, label):
    require(path.is_file(), 'Missing review evidence: ' + str(path))
    return f'<a href="{html.escape(path.relative_to(V).as_posix())}">{html.escape(label)}</a>'


def verify_run_arguments(expected, folder, fixture, save_root):
    args = expected['arguments']

    def value(flag, default=None):
        require(args.count(flag) <= 1, 'Ambiguous argument: ' + flag)
        if flag not in args:
            return default
        index = args.index(flag) + 1
        require(index < len(args), 'Missing value: ' + flag)
        return args[index]

    require(expected['fixture'] == fixture and value('-campaignProof') == fixture, 'Wrong proof fixture')
    evidence = value('-campaignEvidenceRoot')
    require(evidence is not None and Path(evidence).resolve() == folder.resolve(), 'Wrong evidence root')
    require(Path(value('-campaignSaveRoot', evidence)).resolve() == save_root.resolve(), 'Wrong save root')


def verify_inventory(root, folders, inventory):
    paths = {p.relative_to(root).as_posix() for folder in folders for p in (root / folder).rglob('*') if p.is_file()}
    require(paths == set(inventory), 'Source/Player inventory membership changed')
    require(all(sha(root / p) == digest for p, digest in inventory.items()), 'Current source/Player mismatch')


def main():
    runtime = ['Production/Unity/Assets', 'Production/Unity/Packages',
               'Production/Unity/ProjectSettings', 'Production/Content/Campaign']
    changed = subprocess.check_output(['git', 'diff', '--name-only', BASELINE, '--', *runtime], cwd=P.parent, text=True).splitlines()
    require(not changed, 'Accepted runtime changed: ' + str(changed))
    accepted = read(E / 'q1-prince-seated-correction/inventory.json')
    require(all((P / p).is_file() and sha(P / p) == digest for p, digest in accepted.items()),
            'Accepted source/Player inventory changed')
    texture = P / 'Unity/Assets/Surlhwa/Q1Polish/PrinceSideQuarter.png'
    correction = read(V / 'Manifests/prince-seated-correction-validation.json')
    require(sha(texture) == correction['textureSha256'], 'Accepted pose changed')
    foundation = E / 'gate2-accepted-foundation-20261031-retry.log'
    require('RESULT 149 passed, 0 failed' in foundation.read_text(encoding='utf-16'), 'Contract suite incomplete')
    preservation = read(E / 'gate2-accepted-preservation-20261031.json')
    require(preservation['specificationFiles'] == 74 and preservation['specificationsUnchanged'] and preservation['frozenRc1Unchanged'], 'Frozen inputs changed')
    rows, galleries, evidence = [], [], {}
    current_inventory = None
    for name, title, height in RUNS:
        folder = E / name
        report, process, expected = [read(folder / f) for f in ('player-result.json', 'process.json', 'expected-run.json')]
        if 'continue-' in name:
            kind = 'quiet' if 'quiet' in name else 'open'
            fixture = 'quiet' if kind == 'quiet' else 'continue'
            save_root = E / ('gate2-accepted-continue-' + kind + '-save-20261031')
        else:
            fixture = 'quiet' if 'quiet' in name else 'open'
            save_root = E / 'gate2-accepted-quiet-save-20261031' if fixture == 'quiet' else folder
        verify_run_arguments(expected, folder, fixture, save_root)
        require(report['status'] == 'PASS' and not report['errors'], 'Player failed: ' + name)
        require(process['exitCode'] == 0 and not process['timedOut'], 'Process incomplete: ' + name)
        require(set(Q1_CHECKS) <= set(report['checks']), 'Missing Q1 checks: ' + name)
        require('17 actual services' in report['checks'] and 'six recipes learned by real services' in report['checks'], 'Incomplete ACT 1: ' + name)
        require(expected['height'] == height and expected['width'] == height * 16 // 9, 'Wrong resolution')
        inventory = read(folder / 'inventory.json')['files']
        if current_inventory is None:
            current_inventory = inventory
            require({p: h for p, h in inventory.items() if not p.startswith('Validation/Builds/Act1/')} == accepted,
                    'Current runtime/Player differs from the accepted correction inventory')
            folders = ('Unity/Assets', 'Unity/Packages', 'Unity/ProjectSettings', 'Content/Campaign', 'Validation/Builds/Act1', 'Validation/Builds/Q1Production')
            verify_inventory(P, folders, inventory)
        require(inventory == current_inventory, 'Run source/Player mismatch: ' + name)
        require(expected['executableSha256'].lower() == sha(V / 'Builds/Q1Production/SurlhwaQ1.exe'), 'Executable mismatch')
        images = sorted(folder.glob('*.png'))
        require(len(images) == report['captures'] and images, 'Capture count mismatch')
        for image in images:
            with image.open('rb') as stream:
                header = stream.read(24)
            require(header[:8] == b'\x89PNG\r\n\x1a\n' and struct.unpack('>II', header[16:24]) == (height * 16 // 9, height), 'Wrong PNG dimensions')
        for filename in ('player-result.json', 'process.json', 'expected-run.json', 'inventory.json'):
            path = folder / filename
            evidence[path.relative_to(V).as_posix()] = sha(path)
        rows.append(dict(run=name, label=title, checks=len(report['checks']), captures=len(images), height=height))
        galleries.append('<details><summary>' + title + ' — native stills</summary><p>' + ' · '.join(link(p, p.stem) for p in images) + '</p></details>')
    outcomes = []
    for kind, source in [('open', E / RUNS[0][0]), ('quiet', E / 'gate2-accepted-quiet-save-20261031')]:
        payload = read(source / 'slot1/current.json')['payload']
        require(payload['phase'] == 'Complete' and payload['farewell'].lower() == kind, 'Wrong outcome')
        require(len(payload['services']) == 17 and len(payload['completedOccurrences']) == 17 and len(payload['knownReceipts']) == 6, 'Incomplete progress')
        require(len(payload['memories']) == 1 and payload['memories'][0]['variant'] == ('star_glowing' if kind == 'open' else 'star_unlit'), 'Wrong memory')
        require(payload['services'][5]['outcome']['recipeId'] == 'recipe_manwol' and len(payload['services'][5]['snapshot']['layers']) == 3, 'Manwol evidence missing')
        require(payload['services'][10]['outcome']['fits'] == (kind == 'open') and payload['services'][16]['outcome']['fits'], 'Return understanding mismatch')
        copy = E / ('gate2-accepted-continue-' + kind + '-save-20261031')
        origin = read(copy / 'origin.json')
        require(Path(origin['source']).resolve() == source.resolve(), 'Wrong Continue origin')
        require(all(sha(copy / 'slot1' / name) == digest == sha(source / 'slot1' / name) for name, digest in origin['files'].items()), 'Continue changed save generations')
        outcomes.append(dict(outcome=kind, services=17, departures=17, knownRecipes=6, completedContinueByteIdentical=True))
    quiet_origin = read(E / 'gate2-accepted-quiet-save-20261031/origin.json')
    original = read(Path(quiet_origin['source']) / 'slot1/current.json')['payload']
    quiet = read(E / 'gate2-accepted-quiet-save-20261031/slot1/current.json')['payload']
    require(len(original['services']) == 10 and quiet['services'][:10] == original['services'], 'Quiet prefix changed')

    topics = [
        ('Quarter view / identity', 'Approved Q1; no camera reconsideration.'),
        ('Snow / warm bar', 'Judge warm interior and exposed snowy exterior together.'),
        ('Scale / direction / grounding', 'Accepted scale, stool, contact and counter occlusion are preserved.'),
        ('ACT 1 pacing', '17 services in the authored ACT 1 sequence; use live personal reading pace.'),
        ('Prince portrayal', 'Seated pose accepted at a19a84a. Judge the complete three-visit portrayal; no routine pose re-review.'),
        ('Four methods', 'Stir, Build, Shake and Float use actual production controls.'),
        ('Manwol / Float', 'First visit retains actual three-layer Manwol and recipe learning.'),
        ('Farewell / memory', 'Open and Quiet retain their earned variants and completed-save Continue.'),
        ('UI / Codex', 'Partial/Known recipes, Korean reading and large text; final visual polish remains open.'),
        ('Animation', 'Live entry, seat, service and departure; final cast-wide polish remains eligible.'),
        ('Audio / music / silence', 'Listen in the live Player. Retained videos do not certify audiovisual synchronization.'),
        ('Overall emotional experience', 'Listening, interpretation, return visits and memory; one bundled Gate 2 decision.'),
    ]
    table = ''.join(f'<tr><td>{r["label"]} / {r["height"]}p</td><td>{r["checks"]}</td><td>{r["captures"]}</td><td>{link(E / r["run"] / "player-result.json", "PASS report")}</td></tr>' for r in rows)
    cards = ''.join(f'<tr><td>{i:02d} · {title}</td><td>{description}</td></tr>' for i, (title, description) in enumerate(topics, 1))
    hero = E / 'q1-prince-seated-correction/Q1_Prince_Composition_1920.png'
    page = '''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>SURLHWA · accepted ACT 1 / Gate 2</title><style>
body{margin:0;background:#171b21;color:#e1d8c5;font:17px/1.65 system-ui}main{max-width:1150px;margin:auto;padding:36px 24px}a{color:#dfbc79}h1,h2{font-weight:500}.note{padding:18px;background:#20262d;border-left:3px solid #bcceb9}img{width:100%;height:auto}table{width:100%;border-collapse:collapse}td,th{text-align:left;padding:12px;border-bottom:1px solid #454b50}code{overflow-wrap:anywhere}details{margin:20px 0}small{color:#b2b9bc}</style><main>
<h1>SURLHWA · ACT 1 / Gate 2</h1><p class="note"><b>Current Production baseline: a19a84a.</b> Prince seated pose accepted by the owner on 2026-10-31. Q1, scale, stool, grounding, counter occlusion, facing and ACT 1 logic are preserved. Gate 2 remains open; ACT 2 has not begun.</p>
<p>''' + link(V / 'Q1_OWNER_CONFIRMATION.md', 'Owner acceptance and boundaries') + ' · ' + link(V / 'GATE2_ACCEPTED_BASELINE.md', 'Current verification and debt') + '''</p>
<h2>Accepted pose in the scene</h2><p>No additional pose polishing or separate pose approval is requested. Final art and animation polish remains eligible with the character set.</p>''' + link(hero, 'Native 1080p composition') + f'<img src="{hero.relative_to(V).as_posix()}" alt="Accepted Prince seated naturally at the Q1 stool">' + '''
<h2>Play the current ACT 1</h2><p><code>powershell -File Production/Tools/run_gate2_q1.ps1 -Start New -Height 1080</code></p>
<p>Earned checkpoint starts: First, Warmth, Clarity, QuietClarity, OpenMemory, QuietMemory. Each launch uses an isolated save; choose Continue for checkpoints. Live play is the source for pacing, controls, motion and listening.</p>
<h2>Current Player verification</h2><p>Simulated production InputSystem, actual native-resolution Player renders; not OS-native input. No runtime code or asset changes in this refresh.</p><table><tr><th>Run</th><th>Checks</th><th>Stills</th><th>Evidence</th></tr>''' + table + '</table>' + ''.join(galleries) + '''
<h2>One bundled Gate 2 review</h2><table>''' + cards + '''</table>
<h2>Retained recordings and technical history</h2><p>''' + link(V / 'review-gate2-q1.html', 'Full playthroughs, focused clips, Unity audio and earlier integration evidence') + '''. These recordings predate the accepted seated-pose correction: they show the previous pose. Use the current stills and live Player for accepted art. Approximately 5 Hz video and known audio-duration differences do not establish final timing or sync.</p><p>''' + link(V / 'GATE2_Q1_REVIEW.md', 'Historical integration report') + ' · ' + link(V / 'PRESENTATION_DEBT.md', 'Remaining presentation debt') + '''</p>
<p class="note">All twelve subjective areas stay in this single Gate 2 package. Routine implementation decisions do not create extra owner gates. Formal Gate 2 closure is required before ACT 2.</p></main></html>'''
    for value in re.findall(r'(?:href|src)="([^"]+)"', page):
        require((V / html.unescape(value)).is_file(), 'Broken package link: ' + value)
    manifest = dict(status='PASS', baseline=BASELINE, ownerPoseAccepted='2026-10-31', gate2='HUMAN_SIGNOFF_REQUIRED', act2='OUT_OF_SCOPE',
                    acceptedInventoryFilesVerified=len(accepted), currentInventoryFilesVerified=len(current_inventory), textureSha256=sha(texture),
                    runtimeChanges=changed, standaloneTests=149, preservation=preservation, runs=rows, outcomes=outcomes, evidenceSha256=evidence,
                    foundationLogSha256=sha(foundation), limitations=['simulated input, not OS-native', 'historical media shows previous seated pose', 'audio synchronization and minimum hardware remain validation debt'])
    (V / 'Manifests/gate2-accepted-baseline.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    (V / 'review-gate2-current.html').write_text(page, encoding='utf-8')
    print('PASS: accepted baseline, four Player runs, both outcomes/Continue, 149 contracts; current Gate 2 page written')


if __name__ == '__main__':
    main()
