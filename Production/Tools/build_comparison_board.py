"""Assemble a labeled overview from verified native captures; never alter originals."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, PngImagePlugin


def build_board(manifest, validation):
    folder = Path(manifest['evidencePath'])
    records = {row['file']: row for row in manifest['images']}
    def camera(variant, fixture):
        return next(row for row in manifest['player']['captures']
                    if row['camera'] == variant and row['fixture'] == fixture and row['width'] == 1920)
    a = camera('OrthographicA', 'Composition')
    b = camera('PerspectiveB', 'Composition')
    close_a = camera('OrthographicA', 'GroundingInspection')
    close_b = camera('PerspectiveB', 'GroundingInspection')
    fixtures = [('Composition', 'Bartender + Don Quixote / VISUAL_FIXTURE_ONLY'),
                ('OrdinarySeated', 'Ordinary charcoal guest / seated'),
                ('GroundingInspection', f"Seated contact / closer lens: A half-height {close_a['orthographicSize']:g} m; B FOV {close_b['fieldOfView']:.6f} deg")]
    tile_w, tile_h, margin, header, row_h = 640, 360, 16, 116, 408
    board = Image.new('RGB', (tile_w * 2 + margin * 3, header + row_h * 3 + 100), '#141619')
    draw = ImageDraw.Draw(board)
    font_path = 'C:/Windows/Fonts/segoeui.ttf'
    title = ImageFont.truetype(font_path, 28)
    label = ImageFont.truetype(font_path, 20)
    small = ImageFont.truetype(font_path, 17)
    draw.text((margin, 12), 'SURLHWA / HG01-HG03 / comparison overview', font=title, fill='#eee3d4')
    draw.text((margin, 54), 'Same room, art, lighting and staging. No camera winner selected.', font=small, fill='#baa992')
    draw.text((margin, 84), f"A / Orthographic / half-height {a['orthographicSize']:g} m", font=label, fill='#eac79b')
    draw.text((tile_w + margin * 2, 84), f"B / Perspective / vertical FOV {b['fieldOfView']:.6f} deg", font=label, fill='#eac79b')
    sources = []
    for row, (fixture, description) in enumerate(fixtures):
        y = header + row * row_h
        draw.text((margin, y), description, font=label, fill='#eee3d4')
        for column, variant in enumerate(('OrthographicA', 'PerspectiveB')):
            name = f'{variant}_{fixture}_1920x1080.png'
            path = folder / name
            digest = hashlib.sha256(path.read_bytes()).hexdigest()
            if digest != records[name]['sha256']:
                raise ValueError('Native source changed since validation: ' + name)
            with Image.open(path) as source:
                if source.size != (1920, 1080): raise ValueError('Wrong native size: ' + name)
                board.paste(source.convert('RGB').resize((tile_w, tile_h), Image.Resampling.LANCZOS),
                            (margin + column * (tile_w + margin), y + 32))
            sources.append({'file': name, 'sha256': digest, 'width': 1920, 'height': 1080})
    y = header + row_h * 3
    for offset, line in enumerate((
            'Overview only: uniformly scaled frames. Use native 720p / 1080p originals to judge pixels and contact.',
            'Don Quixote is VISUAL_FIXTURE_ONLY. Canon ACT order is unchanged.',
            'HG01 / HG02 / HG03: OWNER_REVIEW_PENDING. Exact configurations and limits: REVIEW_HG01_HG03.md')):
        draw.text((margin, y + offset * 26), line, font=small, fill='#baa992')
    destination = validation / 'HG01_HG03_COMPARISON.png'
    metadata = PngImagePlugin.PngInfo()
    metadata.add_itxt('Purpose', 'Scaled overview of actual native Player captures; no artistic approval.')
    metadata.add_itxt('Sources', json.dumps(sources))
    board.save(destination, pnginfo=metadata)
    result = {'result': 'DERIVED_FROM_VERIFIED_NATIVE_CAPTURES', 'file': destination.name,
              'sha256': hashlib.sha256(destination.read_bytes()).hexdigest(), 'width': board.width, 'height': board.height,
              'runId': manifest['processOutcome']['runId'], 'sources': sources,
              'limits': 'Uniformly scaled overview. Originals retained. No retouching, crop, relighting or artistic approval.'}
    (validation / 'Manifests/review-board.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    return result
