"""Normalize the one generated pose into the accepted frame format; retain alpha."""
from pathlib import Path
from PIL import Image
import hashlib, json
ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'SourceArt/Q1Polish/PrinceSideQuarterSource.png'
target=ROOT/'Unity/Assets/Surlhwa/Q1Polish/PrinceSideQuarter.png'
im=Image.open(source).convert('RGBA')
bounds=im.getchannel('A').point(lambda a:255 if a>128 else 0).getbbox()
crop=im.crop(bounds); width=round(crop.width*76/crop.height)
normalized=crop.resize((width,76),Image.Resampling.NEAREST)
frame=Image.new('RGBA',(96,100)); position=(round(59.5-width/2),9)
frame.paste(normalized,position); frame.save(target)
(ROOT/'SourceArt/Q1Polish/import.json').write_text(json.dumps(dict(
    classification='Q1_BOUNDED_POLISH_OWNER_REVIEW', source=source.relative_to(ROOT).as_posix(),
    sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(), alphaBounds=bounds,
    normalizedSize=normalized.size, pasteAt=position, target=target.relative_to(ROOT).as_posix(),
    sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
    method='Alpha bounds crop, nearest-neighbor import normalization only; accepted art retained.'),indent=2)+'\n')
print('Q1 side-quarter pose imported')
