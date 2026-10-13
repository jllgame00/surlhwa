"""Create a local review index only from already validated current Player evidence."""
import html
import json
from pathlib import Path
from slice_inventory import inventory, PRODUCTION

root = PRODUCTION / 'Validation'
report = json.loads((root / 'Manifests/hg04-player-evidence.json').read_text(encoding='utf-8'))
current = inventory()
assert report['status'] == 'PASS'
assert report['sourceSha256'] == current['source']['sha256']
assert report['playerSha256'] == current['player']['sha256']
esc = html.escape
parts = ['''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width">
<title>SURLHWA — HG04 evidence</title><style>
body{margin:0;background:#151c20;color:#e2d8bd;font:17px/1.6 system-ui}main{max-width:1200px;margin:auto;padding:40px 24px}
h1,h2{font-weight:500}a{color:#c4dce4}video,img{max-width:100%;height:auto}details{border-top:1px solid #405056;padding:16px 0}summary{cursor:pointer}
.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(310px,1fr));gap:20px}figure{margin:0}figcaption{font-size:14px}p{max-width:850px}
</style><main><h1>SURLHWA · HG04</h1>
<p>C1 / Pixel Cutaway Hybrid stays locked. The postman and Little Prince appear in a disclosed ACT 1 Warmth-follow-up excerpt. ACT completion and all memories remain unearned.</p>
<p><a href="HG04_CLOSURE.md">Closure status and limits</a> · <a href="HG04_PLAY_GUIDE.md">Controls</a> · <a href="HG04_REVIEW.md">Technical review</a> · <a href="Builds/Slice/SurlhwaSlice.exe">Windows Player</a></p>
<p>These runs use simulated events through the production input adapter. Images and sound come from the actual Player. They do not establish native OS input or owner approval of feel. Interrupted native attempts are separate.</p>''']
native = json.loads((root/'Manifests/hg04-native-evidence.json').read_text(encoding='utf-8'))
parts.append(f'<p><strong>{esc(native["gate"])}</strong> — Native pointer controls were observed under verified foreground ownership. Keyboard delivery and a complete native Stir playthrough remain unverified. <a href="Manifests/hg04-native-evidence.json">Native coverage record</a> · <a href="Evidence/{esc(native["attempt"])}/native-pointer-guide.png">Native window capture</a></p>')
for row in report['runs']:
    folder = root / 'Evidence' / row['folder']
    parts.append(f'<details><summary>{esc(row["folder"])} · {row["captures"]} captures · {row["resolution"][1]}p</summary>')
    prefix = 'Evidence/' + row['folder'] + '/'
    for name in ('run.json','expected-run.json','process.json','inventory.json'):
        assert (folder/name).is_file()
        parts.append(f'<a href="{prefix+name}">{name}</a>')
    clip = folder / 'native-playthrough.mp4'
    if clip.is_file():
        media = json.loads((folder/'media.json').read_text(encoding='utf-8'))
        parts.append(f'<p>Native frame sequence with the captured Player audio mix.</p><video controls preload="metadata" src="{prefix}native-playthrough.mp4"></video>')
    parts.append('<div class="grid">')
    for picture in sorted(folder.glob('[0-9][0-9]-*.png')):
        parts.append(f'<figure><a href="{prefix+picture.name}"><img loading="lazy" src="{prefix+picture.name}" alt="{esc(picture.stem)}"></a><figcaption>{esc(picture.stem)} · <a href="{prefix+picture.stem}.json">observed state</a></figcaption></figure>')
    parts.append('</div></details>')
parts.append('<p>Full inventories and run identities are in <a href="Manifests/hg04-player-evidence.json">the Player evidence manifest</a>. Earlier failures and superseded builds remain in Evidence and are excluded from this current-build index.</p></main></html>')
(root/'review-hg04.html').write_text('\n'.join(parts),encoding='utf-8')
print(f'HG04 REVIEW INDEX: {len(report["runs"])} validated current-build runs')
