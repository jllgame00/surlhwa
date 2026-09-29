"""Compose captured production views for human review; never writes runtime art.

Run after Modular Bar Play Mode Render Fixtures and a complete Full Campaign
Play Mode run. Inputs are local validator captures, not generated concept art.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
captures = ROOT / 'Logs/Review'
views = [
    ('Empty bar', 'restart-empty'),
    ('Ordinary silhouette', 'restart-ordinary'),
    ('Little Prince', 'restart-prince'),
    ('Chunhyang', 'restart-chunhyang'),
    ('Bartending method selection', 'restart-bartending-ready'),
    ('Earned campaign memories', 'restart-earned-memories'),
]
board = Image.new('RGB', (1280, 1164), '#202331')
draw = ImageDraw.Draw(board)
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 18)
for index, (title, name) in enumerate(views):
    path = captures / (name + '-1280x720.png')
    if not path.exists():
        raise SystemExit(f'Missing capture {path}; run the documented validation first.')
    x, y = (index % 2) * 640, (index // 2) * 388
    draw.text((x + 12, y + 4), title, font=font, fill='#ead5ad')
    image = Image.open(path).convert('RGB').resize((640, 360), Image.Resampling.NEAREST)
    board.paste(image, (x, y + 28))
output = ROOT / 'ArtSource/Pixel/BarRestart/Review-board.png'
board.save(output)
print(output.relative_to(ROOT).as_posix())
