"""Import the owner-requested pose using the existing Q1 normalization contract."""
from pathlib import Path
from PIL import Image
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'SourceArt/PrinceSeatedCorrection'
source = SOURCE / 'PrinceSeatedSource.png'
target = ROOT / 'Unity/Assets/Surlhwa/Q1Polish/PrinceSideQuarter.png'
im = Image.open(source).convert('RGBA')
bounds = im.getchannel('A').point(lambda a: 255 if a > 128 else 0).getbbox()
crop = im.crop(bounds)
width = round(crop.width * 76 / crop.height)
normalized = crop.resize((width, 76), Image.Resampling.NEAREST)
frame = Image.new('RGBA', (96, 100))
position = (round(59.5 - width / 2), 9)
frame.paste(normalized, position)
frame.save(target)
(SOURCE / 'import.json').write_text(json.dumps(dict(
    classification='PRINCE_SEATED_POSE_OWNER_REVIEW',
    source=source.relative_to(ROOT).as_posix(),
    sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
    alphaBounds=bounds, normalizedSize=normalized.size, pasteAt=position,
    target=target.relative_to(ROOT).as_posix(),
    sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
    method='Existing Q1 alpha crop and nearest-neighbor import; 76px height, 96x100 canvas, same center and bottom. No scene or mesh changes.'
), indent=2) + '\n', encoding='utf-8')
print('Prince seated correction imported:', normalized.size, position)
