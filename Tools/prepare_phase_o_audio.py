"""Original procedural Phase O audio. No recordings, samples or external audio.
Run with Python + NumPy: python Tools/prepare_phase_o_audio.py
Recipe, imports, runtime library and review copies are deterministic.
"""
from pathlib import Path
import hashlib, json, uuid, wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
RATE = 44100
OUT = ROOT / 'Assets/_SURLHWA/Audio/PhaseO'
SOURCE = ROOT / 'ArtSource/Audio'

def guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'surlhwa-owned-phase-o/' + path).hex

def noise(n, rng, low, high):
    f = np.fft.rfftfreq(n, 1 / RATE)
    spectrum = np.fft.rfft(rng.normal(size=n))
    shape = (f / np.maximum(f, low)) ** 2 / (1 + (f / high) ** 4)
    spectrum *= shape
    spectrum[0] = 0
    x = np.fft.irfft(spectrum, n)
    return x / max(1e-9, np.sqrt(np.mean(x*x)))

def make(name, family, seconds, gain, loop=False, style='paper'):
    n = round(seconds * RATE)
    rng = np.random.default_rng(int(hashlib.sha256(name.encode()).hexdigest()[:8], 16))
    t = np.arange(n) / RATE
    if style in ('room', 'wind'):
        x = noise(n, rng, 35, 220 if style == 'room' else 650)
        # Periodic slow variation; no artificial lull/fade at every loop seam.
        x *= .8 + .13*np.sin(2*np.pi*t/seconds) + .07*np.cos(4*np.pi*t/seconds)
    elif style in ('pour', 'fine', 'transfer', 'strain', 'stir', 'shake'):
        x = .5*noise(n, rng, 160, 2400 if style in ('fine','strain') else 1500)
        for _ in range(50 if style == 'shake' else 24):
            start = rng.uniform(0, seconds); age = (t-start) % seconds
            freq = rng.uniform(700, 3500) if style in ('stir','shake') else rng.uniform(170, 850)
            x += rng.uniform(.2,.7)*np.sin(2*np.pi*freq*age)*np.exp(-age*rng.uniform(35,100))
        x *= .8 + .12*np.sin(2*np.pi*t/seconds) + .08*np.cos(6*np.pi*t/seconds)
    else:
        high = {'step':700, 'seat':1100, 'air':900, 'glass':3200, 'ice':4000, 'paper':2600, 'discovery':2200}[style]
        x = noise(n, rng, 90, high) * np.exp(-t*(9 if style != 'air' else 4))
        if style in ('glass','ice','discovery'):
            for hz, decay in [(1240,13),(2093,19),(3310,27)]:
                x += .7*np.sin(2*np.pi*hz*t)*np.exp(-t*decay)
        if style == 'step':
            x += .7*np.sin(2*np.pi*95*t)*np.exp(-t*32)
        if style == 'seat':
            x += .25*np.sin(2*np.pi*(145*t+14*t*t))*np.exp(-t*9)
        envelope = np.minimum(1,t/.012)*np.minimum(1,(seconds-t)/.035)
        x *= np.maximum(0,envelope)
    x -= np.mean(x)
    # Loop join blends a short neighborhood, without dipping the whole bed.
    if loop:
        k = round(.025*RATE)
        bridge = (x[:k]+x[-k:])*.5
        w = np.linspace(0,1,k)
        x[:k] = bridge*(1-w)+x[:k]*w
        x[-k:] = x[-k:]*(1-w)+bridge*w
        edge = (x[0]+x[-1])*.5
        x[:k] += (edge-x[0])*(1-w)**2
        x[-k:] += (edge-x[-1])*w**2
    else:
        x *= np.minimum(1,t/.006)*np.minimum(1,(seconds-t)/.015)
        x[0]=x[-1]=0
    rms = .035 if style in ('room','wind') else .085
    x *= min(rms/max(1e-9,np.sqrt(np.mean(x*x))), .32/max(1e-9,np.max(np.abs(x))))
    pcm=np.round(x*32767).astype('<i2')
    path=OUT/family/(name+'.wav');path.parent.mkdir(parents=True,exist_ok=True)
    with wave.open(str(path),'wb') as f:
        f.setparams((1,2,RATE,n,'NONE','not compressed'));f.writeframes(pcm.tobytes())
    rel=path.relative_to(ROOT).as_posix(); g=guid(rel)
    path.with_suffix('.wav.meta').write_text(f'''fileFormatVersion: 2
guid: {g}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: {1 if seconds > 10 else 0}
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: {1 if seconds > 10 else 0}
    quality: 0.75
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {{}}
  forceToMono: 1
  normalize: 0
  loadInBackground: 0
  ambisonic: 0
  3D: 0
  userData:
  assetBundleName:
  assetBundleVariant:
''',encoding='utf-8')
    return dict(name=name,family=family,runtime=rel,guid=g,seconds=seconds,loop=loop,gain=gain,
        peak=float(np.max(np.abs(pcm.astype(float)))/32768),rms=float(np.sqrt(np.mean((pcm.astype(float)/32768)**2))),
        dc=float(np.mean(pcm)/32768),seam=float(abs(int(pcm[-1])-int(pcm[0]))/32768),sha256=hashlib.sha256(path.read_bytes()).hexdigest())

SPECS=[
 ('RoomTone','Ambience',24,.40,True,'room'),('WinterAir','Ambience',32,.24,True,'wind'),
 ('FootLeft','Foley',.22,.5,False,'step'),('FootRight','Foley',.25,.5,False,'step'),
 ('Seat','Foley',.4,.4,False,'seat'),('Stand','Foley',.3,.35,False,'seat'),('EntranceAir','Foley',.8,.3,False,'air'),
 ('BottleHandle','Bartending',.18,.38,False,'glass'),('GlassSet','Bartending',.32,.5,False,'glass'),
 ('JiggerContact','Bartending',.2,.38,False,'glass'),('Ice','Bartending',.65,.6,False,'ice'),
 ('Pour','Bartending',2.4,.7,True,'pour'),('FinePour','Bartending',3.2,.5,True,'fine'),
 ('Transfer','Bartending',2.7,.65,True,'transfer'),('Strain','Bartending',3.1,.55,True,'strain'),
 ('Stir','Bartending',3.2,.45,True,'stir'),('Shake','Bartending',3.5,.7,True,'shake'),
 ('Select','UI',.12,.25,False,'paper'),('Back','UI',.18,.25,False,'paper'),
 ('BookOpen','UI',.35,.35,False,'paper'),('BookClose','UI',.25,.3,False,'paper'),
 ('Discovery','UI',.7,.32,False,'discovery')]

if __name__ == '__main__':
    records=[make(*s) for s in SPECS]
    SOURCE.mkdir(parents=True,exist_ok=True)
    (SOURCE/'manifest.json').write_text(json.dumps(dict(method='Original deterministic NumPy synthesis; no samples, recordings or external assets',generator='Tools/prepare_phase_o_audio.py',sample_rate=RATE,format='mono PCM16 WAV',assets=records),indent=2)+'\n',encoding='utf-8')
    # Stable project-owned importer identity, never regenerate an existing meta.
    for directory in [OUT,*sorted(p for p in OUT.rglob('*') if p.is_dir())]:
        meta=Path(str(directory)+'.meta')
        if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+guid(directory.relative_to(ROOT).as_posix())+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')
    script='Assets/Code/UI/SurlhwaAudioLibrary.cs'
    script_guid=guid(script)
    Path(ROOT/(script+'.meta')).write_text('fileFormatVersion: 2\nguid: '+script_guid+'\n',encoding='utf-8')
    lib=ROOT/'Assets/Resources/SurlhwaAudioLibrary.asset'
    lib.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+script_guid+', type: 3}\n  m_Name: SurlhwaAudioLibrary\n  m_EditorClassIdentifier:\n  cues:\n'+''.join(f'  - id: {i}\n    clip: {{fileID: 8300000, guid: {r["guid"]}, type: 3}}\n    gain: {r["gain"]}\n    loop: {int(r["loop"])}\n' for i,r in enumerate(records)),encoding='utf-8')
    Path(str(lib)+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid(lib.relative_to(ROOT).as_posix())+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')
    print(f'Prepared {len(records)} original clips, import settings, library and source manifest.')
