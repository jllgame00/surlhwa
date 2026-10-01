"""Index actual Unity motion frames; retain full-resolution PNGs and native crops."""
from pathlib import Path
from PIL import Image, ImageDraw
import html
import json
import hashlib

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Logs/Review/PhaseN'
SAMPLES = OUT / 'motion-samples.tsv'
groups = {}
for line in SAMPLES.read_text().splitlines():
    filename, seconds, state, pose, frame = line.split('\t')
    kind = filename.rsplit('-', 1)[0]
    groups.setdefault(kind, []).append((filename, float(seconds), state, pose, frame))

parts = ['<!doctype html><meta charset="utf-8"><title>SURLHWA Phase N motion</title>',
         '<style>body{background:#20272b;color:#f1e8d5;font:16px sans-serif;margin:24px}img{max-width:100%}a{color:#d5b987}figure{margin:12px 0}</style>',
         '<h1>Phase N — production-scene motion fixtures</h1>',
         '<p>Actual Unity renders. Spawn/service/departure setup uses fixture callbacks. '
         'These are not physical-input recordings or human animation approval. '
         'GIF timing uses sampled game timestamps. Full-size frames remain linked below.</p>']
selected = []
for kind, rows in groups.items():
    frames = [Image.open(OUT / (row[0] + '-1280x720.png')).convert('RGB') for row in rows]
    durations = [max(20, round((rows[i + 1][1] - row[1]) * 1000)) if i + 1 < len(rows) else 250 for i, row in enumerate(rows)]
    frames[0].save(OUT / (kind + '.gif'), save_all=True, append_images=frames[1:], duration=durations, loop=0)
    parts.append('<h2>' + html.escape(kind) + '</h2><img src="' + kind + '.gif">')
    for pose in ('Walking', 'Turning', 'Sitting', 'SeatedIdle', 'ServiceReaction', 'Standing', 'Leaving'):
        match = next(((i, row) for i, row in enumerate(rows) if row[3] == pose), None)
        if match is None:
            continue
        i, row = match
        # Native scale; no enlargement or resampling. Includes furniture/feet and bartender.
        crop = frames[i].crop((100, 255, 820, 610))
        name = kind + '-' + pose + '-native.png'
        crop.save(OUT / name)
        parts.append('<figure><a href="' + row[0] + '-1280x720.png"><img src="' + name + '"></a><figcaption>' +
                     pose + ' — ' + str(row[1]) + ' s; native-scale crop, click for full 720p frame</figcaption></figure>')
        if kind in ('OrdinaryCoat', 'LittlePrince', 'Lovers', 'Hamlet') and pose in ('Walking', 'SeatedIdle', 'ServiceReaction', 'Leaving'):
            selected.append((kind + ' / ' + pose, crop))
    parts.append('<p><a href="' + kind + '-seated-1920x1080.png">1080p seated still</a></p>')
    parts.append('<details><summary>Individual full-resolution motion frames</summary>' + '<br>'.join(
        '<a href="' + r[0] + '-1280x720.png">' + r[0] + ' / ' + r[3] + ' / ' + str(r[1]) + ' s</a>' for r in rows) + '</details>')
(OUT / 'review.html').write_text('\n'.join(parts), encoding='utf-8')
board = Image.new('RGB', (1440, ((len(selected) + 1) // 2) * 380), '#20272b')
draw = ImageDraw.Draw(board)
for i, (label, crop) in enumerate(selected):
    x, y = (i % 2) * 720, (i // 2) * 380
    draw.text((x + 8, y + 4), label, fill='#f1e8d5')
    board.paste(crop, (x, y + 24))
board.save(OUT / 'review-board.png')
(OUT / 'capture-sha256.json').write_text(json.dumps({p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(OUT.glob('*.png'))}, indent=2))
print('Indexed', len(groups), 'motion sequences and', sum(map(len, groups.values())), 'full-resolution frames.')
