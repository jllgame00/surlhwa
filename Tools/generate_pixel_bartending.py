"""Phase L: reproducible sprite preparation, original pixel masks and garnish.
Run from any directory with Python 3 + Pillow. No network or random seed.
Only writes its declared Phase L runtime outputs and reference board.
"""
from pathlib import Path
import hashlib
import json
import uuid
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'ArtSource/Pixel/StyleReference'
OUT = ROOT / 'Assets/_SURLHWA/Art/Bartending/Pixel'
SHEET = SOURCE / 'Extensions-source.png'
METAL = ['#292b3b', '#535675', '#858d99', '#becbd6', '#eee5cd']
GLASS = ['#303e4e', '#718b99', '#becbd6', '#eee5cd']
records = []


def guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'surlhwa/phase-l/' + str(path.relative_to(ROOT)).replace('\\', '/')).hex


def meta(path, pivot=(.5, .5)):
    target = Path(str(path) + '.meta')
    if target.exists():
        return  # Unity GUIDs are stable, never rewrite existing import metadata.
    target.write_text(f'''fileFormatVersion: 2
guid: {guid(path)}
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
  spritePivot: {{x: {pivot[0]}, y: {pivot[1]}}}
  spritePixelsToUnits: 100
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
    spriteID: {guid(path)}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  userData: PhaseL
  assetBundleName:
  assetBundleVariant:
''', encoding='utf-8')


def save(category, name, image, method, pivot=(.5, .5)):
    folder = OUT / category
    folder.mkdir(parents=True, exist_ok=True)
    path = folder / (name + '.png')
    image.save(path)
    meta(path, pivot)
    records.append(dict(id=name, category=category, path=path.relative_to(ROOT).as_posix(),
                        dimensions=list(image.size), ppu=100, pivot=list(pivot), method=method,
                        sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    return image


def blank(w, h):
    return Image.new('RGBA', (w, h))


def crop(box, size, palette=None):
    src = Image.open(SHEET).convert('RGBA')
    # Coordinates recorded on the inspected 1920x800 sheet, scale if encoded differently.
    box = tuple(round(v * src.size[i % 2] / (1920 if i % 2 == 0 else 800)) for i, v in enumerate(box))
    src = src.crop(box)
    src = src.crop(src.getbbox()).resize(size, Image.Resampling.NEAREST)
    alpha = src.getchannel('A').point(lambda a: 255 if a >= 128 else 0)
    if palette:
        colors = [tuple(bytes.fromhex(c[1:])) for c in palette]
        px = src.load()
        for y in range(src.height):
            for x in range(src.width):
                r, g, b, a = px[x, y]
                px[x, y] = (*min(colors, key=lambda c: sum((v-t)**2 for v,t in zip(c,(r,g,b)))), a)
    else:
        src = src.quantize(colors=16, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE).convert('RGBA')
    src.putalpha(alpha)
    return src


def glass(name, size, bowl, stem=None):
    shell = blank(*size)
    d = ImageDraw.Draw(shell)
    d.line(bowl + [bowl[0]], fill=GLASS[0], width=2)
    d.line([(x+1,y) for x,y in bowl], fill=GLASS[2], width=1)
    x0, y0 = bowl[0]; x1, _ = bowl[-1]
    d.ellipse((x0,y0-2,x1,y0+3), outline=GLASS[2], width=1)
    d.line((x0+2,y0,x1-3,y0),fill=GLASS[3])
    if stem:
        cx, top, bottom, half = stem
        d.line((cx,top,cx,bottom), fill=GLASS[2],width=2)
        d.ellipse((cx-half,bottom-2,cx+half,bottom+2),outline=GLASS[2])
        d.line((cx-half+3,bottom,cx+half-3,bottom),fill=GLASS[3])
    else:
        y = max(p[1] for p in bowl)
        d.line((min(p[0] for p in bowl)+5,y-2,max(p[0] for p in bowl)-5,y-2),fill=GLASS[3])
    mask = blank(*size)
    ImageDraw.Draw(mask).polygon(bowl,fill='white')
    save('Glassware', name, shell, 'original deterministic pixel geometry')
    save('Glassware', name+'Mask', mask, 'original deterministic bowl mask')


def main():
    save('Tools','Jigger',crop((85,455,260,750),(28,48),METAL),'imagegen source; nearest crop; locked metal palette')
    spoon=crop((310,65,445,753),(12,128),METAL)
    save('Tools','Spoon',spoon,'imagegen source; nearest crop; locked metal palette',(.5,0))
    save('Tools','SpoonHorizontal',spoon.transpose(Image.Transpose.ROTATE_270),'same spoon rotated for Phase K left bowl pivot',(0,.5))
    save('Tools','Strainer',crop((470,335,745,754),(44,64),METAL),'imagegen source; nearest crop; locked metal palette')
    bottle=crop((800,82,1000,752),(32,112))
    for name,w in [('BottleTall',32),('BottleBroad',40),('BottleSmall',24)]:
        h=112 if name!='BottleSmall' else 80
        b=bottle.resize((w,h),Image.Resampling.NEAREST)
        d=ImageDraw.Draw(b)
        d.rectangle((4,h//2,w-5,h//2+20),fill='#d7ccb0',outline='#535675')
        d.line((7,h//2+5,w-8,h//2+5),fill='#858d99',width=2)
        save('Bottles',name,b,'imagegen source; deterministic family and blank label')
    save('Ice','IceCluster',crop((1070,555,1285,754),(26,24),GLASS+['#edf1e5']),'imagegen source; nearest crop; locked ice palette')
    save('Hands','Grip',crop((1515,475,1865,753),(40,32)),'imagegen source; nearest crop; 16 colors')
    save('Garnishes','LemonTwist',crop((1340,552,1495,754),(14,20)),'imagegen source; nearest crop; 16 colors')
    for name,base in [('OrangeSlice','#c98b48'),('LemonSlice','#dbbf75')]:
        im=blank(18,18);d=ImageDraw.Draw(im)
        d.ellipse((1,1,16,16),fill=base,outline='#eee5cd',width=1)
        for p in [(3,3),(14,3),(3,14),(14,14)]:d.line((9,9,*p),fill='#eee5cd')
        save('Garnishes',name,im,'original deterministic pixel garnish')
    im=blank(14,18);d=ImageDraw.Draw(im);d.ellipse((1,7,12,17),fill='#903f45',outline='#492e3d');d.line((7,8,9,1,12,0),fill='#71805d');d.point((4,10),fill='#ce8280')
    save('Garnishes','Cherry',im,'original deterministic pixel garnish')
    im=blank(16,12);d=ImageDraw.Draw(im);d.ellipse((1,2,14,10),fill='#7f8651',outline='#39483d');d.ellipse((9,4,12,7),fill='#903f45');d.line((4,4,7,3),fill='#c6bc82')
    save('Garnishes','Olive',im,'original deterministic pixel garnish')
    im=blank(18,12);d=ImageDraw.Draw(im);d.line([(2,10),(4,3),(11,1),(16,4),(13,8),(8,7)],fill='#c98b48',width=3);d.line([(3,9),(5,3),(11,2)],fill='#eee5cd')
    save('Garnishes','OrangeZest',im,'original deterministic pixel garnish')
    glass('MixingGlass',(72,104),[(3,5),(10,98),(62,98),(68,5)])
    glass('RocksGlass',(56,56),[(3,5),(7,51),(49,51),(52,5)])
    glass('CocktailGlass',(80,88),[(3,5),(39,46),(76,5)],(39,46,83,20))
    glass('TallGlass',(48,88),[(3,5),(6,82),(42,82),(44,5)])
    glass('Flute',(32,112),[(3,5),(7,60),(16,70),(25,60),(28,5)],(16,70,107,12))
    for name,w in [('NormalStream',3),('ThinFloatStream',2),('TransferStream',4),('OverflowStream',3)]:
        im=blank(w,16);d=ImageDraw.Draw(im);d.rectangle((0,0,w-1,15),fill='#becbd6');d.line((0,0,0,15),fill='#eee5cd')
        if name=='OverflowStream':d.rectangle((0,11,0,14),fill=(0,0,0,0))
        save('Liquids',name,im,'original deterministic pixel stream',(.5,0))
    im=blank(4,6);ImageDraw.Draw(im).polygon([(2,0),(3,3),(3,4),(2,5),(1,5),(0,4),(0,3)],fill='white')
    save('Liquids','SmallDroplet',im,'original deterministic pixel droplet')
    im=Image.new('RGBA',(2,2),'white');save('Liquids','Fill',im,'neutral tintable pixel fill')
    for folder in [OUT,*OUT.iterdir()]:
        if folder.is_dir() and not Path(str(folder)+'.meta').exists():
            Path(str(folder)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {guid(folder)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')
    (SOURCE/'assets.json').write_text(json.dumps(records,indent=2)+'\n',encoding='utf-8')
    board=Image.new('RGBA',(1000,700),'#20282c');d=ImageDraw.Draw(board)
    d.text((20,12),'SURLHWA / Phase L - preserved anchors + runtime extensions',fill='#eee5cd')
    for p,x,y,w,h in [('Assets/_SURLHWA/Art/Bartending/Props/shaker.png',20,50,140,280),('Assets/_SURLHWA/Art/Cocktails/설야.png',180,50,220,250),('Assets/_SURLHWA/Art/Cocktails/DryMartini-Draft.png',420,50,220,250)]:
        im=Image.open(ROOT/p).convert('RGBA');im.thumbnail((w,h),Image.Resampling.NEAREST);board.alpha_composite(im,(x,y))
    x,y=20,370
    for r in records:
        if r['id'].endswith('Mask') or r['id'] in ('SpoonHorizontal','Fill'):continue
        im=Image.open(ROOT/r['path']);im=im.resize((im.width*2,im.height*2),Image.Resampling.NEAREST)
        if x+im.width>970:x=20;y+=270
        if y+im.height>700:continue
        board.alpha_composite(im,(x,y));d.text((x,y+im.height+3),r['id'],fill='#eee5cd');x+=max(im.width+16,80)
    board.save(SOURCE/'StyleLock-board.png')
    print(f'Prepared {len(records)} deterministic runtime sprites; anchors unchanged.')


if __name__=='__main__':main()
