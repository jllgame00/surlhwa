"""Reproducible import normalization for two explicitly provisional generated poses."""
from pathlib import Path
from PIL import Image
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "SourceArt/QuarterView/SeatedBackQuarterAtlasAnonymous.png"
TARGET = ROOT / "Unity/Assets/Surlhwa/QuarterView"

atlas = Image.open(SOURCE).convert("RGBA")
rows = []
for index, name in enumerate(("PrinceBackQuarter", "OrdinaryBackQuarter")):
    half = (index * atlas.width // 2, 0, (index + 1) * atlas.width // 2, atlas.height)
    part = atlas.crop(half)
    bounds = part.getchannel("A").point(lambda a: 255 if a > 128 else 0).getbbox()
    crop = part.crop(bounds)
    width = round(crop.width * 76 / crop.height)
    normalized = crop.resize((width, 76), Image.Resampling.NEAREST)
    frame = Image.new("RGBA", (96, 100))
    position = (round(59.5 - width / 2), 9)
    frame.paste(normalized, position)
    path = TARGET / (name + ".png")
    frame.save(path)
    rows.append(dict(path=path.relative_to(ROOT).as_posix(), sourceHalf=half, alphaBounds=bounds,
                     normalizedSize=normalized.size, pasteAt=position,
                     sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
(ROOT / "SourceArt/QuarterView/import.json").write_text(json.dumps(dict(
    classification="SPIKE_ONLY_UNAPPROVED_DIRECTIONAL_STUDY",
    source=SOURCE.relative_to(ROOT).as_posix(), sourceSha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
    method="Alpha-bounds crop, nearest-neighbor normalization to the existing seated frame canvas. No accepted source overwritten.",
    assets=rows), indent=2) + "\n")
print("Prepared two provisional seated directions; existing assets preserved")
