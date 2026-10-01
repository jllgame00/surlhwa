"""Owned Phase L pixels -> bounded Phase N poses. No model call or external art.

Nearest pixel cutouts keep heads/costumes/palettes intact. Feet alternate around
the original hip; seated legs bend toward the bar instead of lowering a standing
card. Source recipes and hashes remain reviewable outside Assets.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import hashlib
import json
import uuid
import argparse

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/_SURLHWA/Art/Characters/Chibi'
OUT = ROOT / 'Assets/_SURLHWA/Art/Characters/Motion'
RECORD = ROOT / 'ArtSource/Pixel/CharacterMotion'
SIZE = (96, 100)


def guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'surlhwa-phase-n/' + path).hex


def meta(path, text):
    target = Path(str(path) + '.meta')
    if not target.exists():
        target.write_text('fileFormatVersion: 2\nguid: ' + guid(path.relative_to(ROOT).as_posix()) + '\n' + text, encoding='utf-8')


def canvas():
    return Image.new('RGBA', SIZE)


def paste(dst, part, x, y):
    dst.alpha_composite(part, (int(x), int(y)))


def make(source):
    w, h = source.size
    x, y = (SIZE[0] - w) // 2, SIZE[1] - h
    hip = h - 27
    split = w // 2
    frames = {}
    for index, stride in enumerate((-3, 0, 3, 0)):
        dest = canvas()
        # Keep face and torso exact. Only the lower legs and coat hem step.
        for row in range(hip, h):
            t = (row - hip) / max(1, h - hip - 1)
            for left, right, sign in ((0, split, 1), (split, w, -1)):
                shift = round(stride * t * sign)
                lift = 1 if stride * sign > 0 and row > h - 10 else 0
                paste(dest, source.crop((left, row, right, row + 1)), x + left + shift, y + row - lift)
        paste(dest, source.crop((0, 0, w, hip)), x, y)
        frames['Walk' + str(index)] = dest
    for name, lift, bend in (('Turn', 3, 3), ('Seated', 7, 9)):
        dest = canvas()
        # Hip rests at 34 px above the feet anchor, aligned with the existing stool.
        # Knees project to the facing (left) side; shins hang below the folded thigh.
        leg = source.crop((0, hip, w, h))
        for row in range(leg.height):
            t = row / max(1, leg.height - 1)
            shift = -round(bend * min(1, t * 2))
            target_y = y + hip - lift + round(row * .68)
            paste(dest, leg.crop((0, row, w, row + 1)), x + shift, target_y)
        paste(dest, source.crop((0, 0, w, hip)), x, y - lift)
        frames[name] = dest
    # One-pixel head inclination, no invented face, costume, or extra limbs.
    for name, dx, dy in (('Listening', -1, 0), ('Reaction', -1, 1)):
        dest = frames['Seated'].copy()
        head_bottom = y - 7 + round(h * .35)
        head = dest.crop((0, 0, SIZE[0], head_bottom))
        dest.paste((0, 0, 0, 0), (0, 0, SIZE[0], head_bottom))
        paste(dest, head, dx, dy)
        frames[name] = dest
    return frames


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--all', action='store_true')
    parser.add_argument('--ordinary-only', action='store_true')
    args = parser.parse_args()
    full = not args.ordinary_only
    names = [p.stem for p in sorted(SOURCE.glob('*.png')) if p.stem != 'Bartender'] if full else ['OrdinaryCoat']
    OUT.mkdir(parents=True, exist_ok=True)
    RECORD.mkdir(parents=True, exist_ok=True)
    meta(OUT, 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    template = (SOURCE / 'OrdinaryCoat.png.meta').read_text(encoding='utf-8')
    import re
    records, entries = [], []
    board = Image.new('RGB', (8 * 108, len(names) * 124), '#5a565b')
    draw = ImageDraw.Draw(board)
    for row, name in enumerate(names):
        source = SOURCE / (name + '.png')
        frames = make(Image.open(source).convert('RGBA'))
        refs = {}
        for col, (pose, im) in enumerate(frames.items()):
            path = OUT / (name + '-' + pose + '.png')
            im.save(path)
            ident = guid(path.relative_to(ROOT).as_posix())
            m = Path(str(path) + '.meta')
            if not m.exists():
                content = re.sub(r'(?m)^(guid:|    spriteID:) .*$', lambda match: match.group(1) + ' ' + ident, template)
                content = content.replace('PhaseLRestart', 'PhaseNMotion')
                m.write_text(content, encoding='utf-8')
            refs[pose] = '{fileID: 21300000, guid: ' + ident + ', type: 3}'
            board.paste(im, (col * 108, row * 124 + 20), im)
            draw.text((col * 108, row * 124), name + '/' + pose, fill='white')
            records.append({'character': name, 'pose': pose, 'source': source.relative_to(ROOT).as_posix(),
                            'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                            'runtime': path.relative_to(ROOT).as_posix(), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                            'dimensions': SIZE, 'ppu': 32, 'pivot': [.5, 0]})
        entries.append('  - character: ' + name + '\n    walk:\n' + ''.join('    - ' + refs['Walk' + str(i)] + '\n' for i in range(4)) +
                       ''.join('    ' + field + ': ' + refs[pose] + '\n' for field, pose in [('turn','Turn'),('seated','Seated'),('listening','Listening'),('reaction','Reaction')]))
    library_script = ROOT / 'Assets/Code/Customer/CustomerMotionLibrary.cs'
    for script in [library_script, ROOT / 'Assets/Code/Customer/CustomerMotionPresentation.cs']:
        meta(script, 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    library = ROOT / 'Assets/Resources/CustomerMotionLibrary.asset'
    library.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: ' + guid(library_script.relative_to(ROOT).as_posix()) + ', type: 3}\n  m_Name: CustomerMotionLibrary\n  m_EditorClassIdentifier:\n  characters:\n' + ''.join(entries), encoding='utf-8')
    meta(library, 'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    if full:
        bartender = Image.open(SOURCE / 'Bartender.png').convert('RGBA')
        for pose in ('Rest', 'Acknowledge'):
            im = bartender.copy()
            # Shift only a bounded hand/head cutout by one source pixel.
            box = (0, 0, im.width, 30) if pose == 'Acknowledge' else (0, 34, im.width // 2, 55)
            piece = im.crop(box)
            im.paste((0, 0, 0, 0), box)
            im.alpha_composite(piece, (box[0], box[1] + 1))
            path = OUT / ('Bartender-' + pose + '.png'); im.save(path)
            ident = guid(path.relative_to(ROOT).as_posix())
            m = Path(str(path) + '.meta')
            if not m.exists():
                content = re.sub(r'(?m)^(guid:|    spriteID:) .*$', lambda match: match.group(1) + ' ' + ident, template)
                m.write_text(content.replace('PhaseLRestart', 'PhaseNMotion'), encoding='utf-8')
            with library.open('a', encoding='utf-8') as f:
                f.write('  bartender' + pose + ': {fileID: 21300000, guid: ' + ident + ', type: 3}\n')
            records.append({'character': 'Bartender', 'pose': pose, 'runtime': path.relative_to(ROOT).as_posix(),
                            'source': (SOURCE / 'Bartender.png').relative_to(ROOT).as_posix(),
                            'source_sha256': hashlib.sha256((SOURCE / 'Bartender.png').read_bytes()).hexdigest(),
                            'dimensions': im.size,
                            'sha256': hashlib.sha256(path.read_bytes()).hexdigest(), 'ppu': 32, 'pivot': [.5, 0]})
        stool_meta = (ROOT / 'Assets/_SURLHWA/Art/Environment/ModularBar/Stool.png.meta').read_text()
        with library.open('a', encoding='utf-8') as f:
            f.write('  companionStool: {fileID: 21300000, guid: ' + re.search(r'guid: (\w+)', stool_meta)[1] + ', type: 3}\n')
        meta(ROOT / 'Assets/Code/Customer/BartenderMotionPresentation.cs', 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    (RECORD / 'assets.json').write_text(json.dumps(records, indent=2) + '\n', encoding='utf-8')
    board.save(RECORD / 'Pose-board.png')
    print(f'Prepared {len(records)} poses for {len(names)} characters; source pixels unchanged.')


if __name__ == '__main__':
    main()
