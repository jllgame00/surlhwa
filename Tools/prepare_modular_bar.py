"""Prepare Phase L restart imagegen sources; Python 3 + Pillow, no network.

Only declared new outputs are written. Existing importer metadata is preserved.
The source atlases and prompts remain the provenance record, not runtime art.
"""
from pathlib import Path
import hashlib
import json
import uuid
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/Pixel/BarRestart'
ENV = ROOT / 'Assets/_SURLHWA/Art/Environment/ModularBar'
CAST = ROOT / 'Assets/_SURLHWA/Art/Characters/Chibi'
records = []
prepared = {}


def stable_guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'surlhwa/phase-l-restart/' + path.relative_to(ROOT).as_posix()).hex


def metadata(path, character):
    target = Path(str(path) + '.meta')
    if target.exists():
        return
    target.write_text(f'''fileFormatVersion: 2
guid: {stable_guid(path)}
TextureImporter:
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  textureSettings:
    filterMode: 0
    aniso: 1
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  maxTextureSize: 2048
  spriteMode: 1
  spriteMeshType: 0
  alignment: 9
  spritePivot: {{x: 0.5, y: {0 if character else 0.5}}}
  spritePixelsToUnits: 32
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: 8
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
    crunchedCompression: 0
    overridden: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {stable_guid(path)}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  userData: PhaseLRestart
  assetBundleName:
  assetBundleVariant:
''', encoding='utf-8')


def crop(source, rect, size, colors=40, threshold=200):
    image = Image.open(SOURCE / source).convert('RGBA').crop(rect)
    image.putalpha(image.getchannel('A').point(lambda a: 255 if a >= threshold else 0))
    image = image.crop(image.getbbox())
    if isinstance(size, int):
        size = (round(image.width * size / image.height), size)
    image = image.resize(size, Image.Resampling.NEAREST)
    alpha = image.getchannel('A')
    image = image.convert('RGB').quantize(colors, method=Image.Quantize.MEDIANCUT,
                                        dither=Image.Dither.NONE).convert('RGBA')
    image.putalpha(alpha)
    return image


def save(name, image, source, rect, character=False, method='imagegen source; crop, nearest resample, palette reduction, binary alpha'):
    path = (CAST if character else ENV) / (name + '.png')
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path)
    metadata(path, character)
    prepared[name] = image
    records.append(dict(id=name, source='ArtSource/Pixel/BarRestart/' + source,
                        crop=rect, runtime=path.relative_to(ROOT).as_posix(),
                        dimensions=list(image.size), ppu=32, pivot=[.5, 0 if character else .5],
                        method=method, provenance='Original OpenAI imagegen output; owner references; prompts.json',
                        quality='FIRST_PASS', tuning='HUMAN_ART_TUNING_LATER',
                        sha256=hashlib.sha256(path.read_bytes()).hexdigest()))


modules = [
    ('WallPanel', (38, 48, 370, 375), (160, 120)),
    ('WinterWindow', (418, 42, 715, 380), (82, 96)),
    ('Shelf', (765, 60, 1070, 383), (110, 104)),
    ('ShelfBottles', (1090, 183, 1422, 337), (88, 34)),
    ('Stool', (650, 485, 825, 735), (34, 48)),
    ('Lantern', (915, 421, 1055, 750), (28, 70)),
    ('Pedestal', (1168, 479, 1390, 723), (48, 54)),
    ('Floor', (29, 797, 475, 1048), (256, 128)),
    ('SidePlant', (523, 747, 733, 1056), (48, 72)),
    ('DisplayNiche', (795, 796, 1070, 1047), (70, 64)),
    ('CounterDressing', (1118, 835, 1425, 1038), (47, 30)),
]
for name, rect, size in modules:
    image = crop('Environment-source.png', rect, size)
    if name == 'Floor':
        # Repeated floor sections use interior planks, excluding the atlas frame.
        image = image.crop((3, 3, image.width - 3, image.height - 3)).resize(size, Image.Resampling.NEAREST)
        image.putalpha(255)
    save(name, image, 'Environment-source.png', rect)

# Separate occlusion planes share one canvas and transform, with no duplicated pixels.
counter_rect = (15, 434, 600, 740)
counter = crop('Environment-source.png', counter_rect, (448, 168), colors=48)
for name, top, bottom in [('CounterArms', 0, 85), ('CounterTop', 85, 118), ('CounterFace', 118, 168)]:
    part = Image.new('RGBA', counter.size)
    part.paste(counter.crop((0, top, 448, bottom)), (0, top))
    save(name, part, 'Environment-source.png', counter_rect,
         method='imagegen U counter; crop/resample/palette; non-overlapping horizontal occlusion planes')

cast = [
    ('Bartender', (110, 0, 405, 445), 84),
    ('LittlePrince', (455, 0, 859, 445), 84),
    ('Chunhyang', (860, 0, 1235, 445), 84),
    ('Mongryong', (100, 445, 420, 866), 84),
    ('DonQuixote', (445, 445, 860, 866), 88),
    ('Hamlet', (861, 445, 1240, 866), 84),
    ('Faust', (100, 866, 420, 1254), 84),
    ('SimCheong', (445, 866, 860, 1254), 84),
    ('Odysseus', (861, 866, 1240, 1254), 88),
]
for name, rect, height in cast:
    save(name, crop('Characters-source.png', rect, height, colors=32),
         'Characters-source.png', rect, True)

palette = [(23, 25, 35), (34, 38, 51), (49, 53, 66), (65, 73, 89), (87, 93, 103)]
for name, rect in [('OrdinaryCoat', (245, 0, 770, 1024)), ('OrdinaryPonytail', (785, 0, 1340, 1024))]:
    image = crop('Ordinary-source.png', rect, 84, colors=16, threshold=225)
    pixels = []
    for r, g, b, a in image.getdata():
        # Deliberately anonymous: all opaque pixels use this bounded charcoal ramp.
        value = (r + g + b) // 3
        rgb = palette[min(4, value // 30)]
        pixels.append((*rgb, a))
    image.putdata(pixels)
    save(name, image, 'Ordinary-source.png', rect, True,
         method='imagegen silhouette; crop/resample/binary alpha; fixed five-color charcoal ramp')

SOURCE.joinpath('assets.json').write_text(json.dumps(records, indent=2) + '\n', encoding='utf-8')

# Review artifact only. Actual production uses separate SpriteRenderers in the prefab.
board = Image.new('RGBA', (960, 700), '#202331')
draw = ImageDraw.Draw(board)
for i, (name, sprite) in enumerate(prepared.items()):
    x, y = (i % 6) * 160, (i // 6) * 140
    draw.text((x + 5, y + 5), name, fill='#ead5ad')
    preview = sprite.copy()
    preview.thumbnail((148, 108), Image.Resampling.NEAREST)
    board.alpha_composite(preview, (x + (160 - preview.width) // 2, y + 27))
board.convert('RGB').save(SOURCE / 'Prepared-assets.png')
print(f'Prepared {len(records)} modular bar and character assets at 32 PPU.')
