"""Extract/import the four original ACT 1 ImageGen atlases.

Production asset normalization only: declared crops, nearest sampling, bounded
palette and binary alpha. Source pixels/identity are never procedurally drawn.
Old C1/Slice assets and metadata are not touched.
"""
from pathlib import Path
import hashlib
import json
import uuid
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Production/SourceArt/Act1Production'
DEST = ROOT / 'Production/Unity/Assets/Surlhwa/Act1Assets/ProductionArt'
records = []
previews = {}


def guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'surlhwa/act1-art/' + path.relative_to(ROOT).as_posix()).hex


def write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists() or path.read_bytes() != data:
        path.write_bytes(data)


def meta(path):
    target = Path(str(path) + '.meta')
    if target.exists():
        return
    template = (ROOT / 'Production/Unity/Assets/Surlhwa/Act1Assets/WinterGround.png.meta').read_text()
    import re
    template = re.sub(r'^guid: .*$', 'guid: ' + guid(path), template, flags=re.M)
    template = template.replace('alphaIsTransparency: 0', 'alphaIsTransparency: 1')
    # No platform override can reintroduce compression or resizing.
    template = re.sub(r'textureCompression: \d+', 'textureCompression: 0', template)
    template = '\n'.join(line.rstrip() for line in template.splitlines()) + '\n'
    write(target, template.encode())


def prepare(source, name, rect, size, colors=40, canvas=None, offset=None):
    raw = Image.open(SOURCE / source).convert('RGBA').crop(rect)
    raw.putalpha(raw.getchannel('A').point(lambda a: 255 if a >= 200 else 0))
    bounds = raw.getbbox()
    if not bounds:
        raise ValueError('Empty crop: ' + name)
    raw = raw.crop(bounds)
    if isinstance(size, int):
        size = (round(raw.width * size / raw.height), size)
    sprite = raw.resize(size, Image.Resampling.NEAREST)
    alpha = sprite.getchannel('A')
    sprite = sprite.convert('RGB').quantize(colors, method=Image.Quantize.MEDIANCUT,
                                           dither=Image.Dither.NONE).convert('RGBA')
    sprite.putalpha(alpha)
    if canvas:
        composed = Image.new('RGBA', canvas)
        at = offset or ((canvas[0] - sprite.width) // 2, canvas[1] - sprite.height)
        composed.alpha_composite(sprite, at)
        sprite = composed
    path = DEST / (name + '.png')
    from io import BytesIO
    stream = BytesIO()
    sprite.save(stream, format='PNG')
    write(path, stream.getvalue())
    meta(path)
    previews[name] = sprite
    records.append(dict(name=name, source=source, crop=rect, opaqueBounds=list(bounds),
                        dimensions=list(sprite.size), normalizedSize=list(size), canvas=canvas,
                        offset=offset, colors=colors,
                        runtime=path.relative_to(ROOT).as_posix(),
                        sha256=hashlib.sha256(path.read_bytes()).hexdigest()))


def main():
    # Atlas dimensions are the actual returned 1254px, not the requested 1536px.
    # Custom environment crop bounds isolate the visibly wider bottle shelves.
    modules = [
        ('Lantern', (785, 40, 968, 438), 100),
        ('Stool', (980, 45, 1254, 438), (48, 56)),
        ('StoolLeather', (1042, 163, 1172, 190), (64, 24)),
        ('ClothTray', (0, 450, 380, 812), (82, 46)),
        ('CeramicSet', (385, 450, 674, 812), (58, 46)),
        ('NotebookCandle', (675, 460, 956, 812), (65, 44)),
        ('EmptyNiche', (960, 450, 1254, 818), (72, 88)),
        ('SnowRocks', (0, 820, 380, 1254), (112, 70)),
        ('SnowPine', (380, 820, 660, 1254), 180),
        ('DistantPines', (660, 830, 915, 1254), 122),
        ('SnowBank', (916, 830, 1254, 1254), (124, 82)),
    ]
    for name, rect, size in modules:
        prepare('Environment.png', name, rect, size, 40)
    prepare('Shelves-generated.png', 'ShelfBottles', (0, 0, 1536, 515), (160, 45), 40)
    prepare('Shelves-generated.png', 'ShelfGlasses', (0, 515, 1536, 1024), (160, 44), 40)
    for name, rect, size in [
        ('CounterWood', (2, 2, 625, 625), (256, 96)),
        ('FloorWood', (629, 2, 1252, 625), (320, 160)),
        ('CounterPanels', (2, 629, 625, 1252), (256, 80)),
        ('RearWood', (629, 629, 1252, 1252), (256, 128)),
    ]:
        prepare('Materials.png', name, rect, size, 48)
    for row, person in enumerate(['Bartender', 'Prince', 'OrdinaryCoat', 'OrdinaryBun']):
        for col, pose in enumerate(['Standing', 'Seated', 'WalkA', 'WalkB']):
            if row == 0:
                pose = ['Standing', 'Towel', 'Blink', 'Listening'][col]
            starts, ends = [0, 320, 645, 952], [316, 642, 950, 1254]
            rect = (round(col * 1254 / 4), starts[row],
                    round((col + 1) * 1254 / 4), ends[row])
            # Fixed 96x100 canvases match existing seated / walking mesh UV bounds.
            # Hips of seated sprites sit at source pixel (50,59), above feet at y=85.
            # This maps to existing seat anchor (12/70,26/70) with pivot (38,15).
            height = 76 if col == 1 and row > 0 else 84
            prepare('Characters.png', person + pose, rect, height, 32,
                    canvas=(96, 84) if height == 84 else (96, 100),
                    offset=None if height == 84 else (32 if row == 1 else 30, 9))
    # Copy existing authored pixel tool family without modifying source bytes.
    for source, name in [
        ('Bottles/BottleTall', 'ToolBottle'), ('Tools/Jigger', 'ToolJigger'),
        ('Tools/Spoon', 'ToolSpoon'), ('Tools/SpoonHorizontal', 'ToolFloatSpoon'),
        ('Tools/Strainer', 'ToolStrainer'), ('Glassware/MixingGlass', 'ToolMixing'),
        ('Glassware/RocksGlass', 'ToolRocks'), ('Glassware/CocktailGlass', 'ToolCocktail'),
        ('Ice/IceCluster', 'ToolIce'), ('Hands/Grip', 'ToolGrip'),
    ]:
        path = ROOT / 'Assets/_SURLHWA/Art/Bartending/Pixel' / (source + '.png')
        out = DEST / (name + '.png')
        write(out, path.read_bytes())
        meta(out)
        previews[name] = Image.open(out).convert('RGBA')
        records.append(dict(name=name, source=path.relative_to(ROOT).as_posix(),
                            method='Unchanged existing authored pixel asset bytes',
                            runtime=out.relative_to(ROOT).as_posix(), dimensions=list(previews[name].size),
                            sha256=hashlib.sha256(out.read_bytes()).hexdigest()))
    manifest = dict(method='Built-in ImageGen, followed by declared crop / nearest normalization / palette reduction / binary alpha',
                    sourceHashes={p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(SOURCE.glob('*.png')) if p.name in ['Characters.png', 'Environment.png', 'Materials.png', 'Shelves-generated.png']},
                    approval='PRODUCTION_BASELINE_CANDIDATE / HUMAN_SIGNOFF_REQUIRED', assets=records)
    write(SOURCE / 'assets.json', (json.dumps(manifest, ensure_ascii=False, indent=2) + '\n').encode())
    board = Image.new('RGB', (1200, ((len(previews)+5)//6)*160), '#20282d')
    draw = ImageDraw.Draw(board)
    for i, (name, sprite) in enumerate(previews.items()):
        x, y = i % 6 * 200, i // 6 * 160
        draw.text((x + 8, y + 7), name, fill='#ddc7a3')
        tile = sprite.copy()
        tile.thumbnail((182, 126), Image.Resampling.NEAREST)
        board.paste(tile, (x + (200 - tile.width) // 2, y + 28), tile)
    board.save(SOURCE / 'Prepared-assets.png')
    print('Prepared', len(records), 'assets; historical C1/Slice art untouched.')


if __name__ == '__main__':
    main()
