"""Package passing ACT 1 Player renders/audio; input remains explicitly simulated."""
import argparse, array, hashlib, json, shutil, subprocess, wave
from pathlib import Path

p=argparse.ArgumentParser(); p.add_argument('folder',type=Path); args=p.parse_args()
folder=args.folder.resolve()
report=json.loads((folder/'player-result.json').read_text(encoding='utf-8'))
process=json.loads((folder/'process.json').read_text(encoding='utf-8-sig'))
if report['status']!='PASS' or process['exitCode']!=0 or process['timedOut']:
    raise SystemExit('Refusing failed/incomplete evidence')
target=folder/'player-playthrough.mp4'
if target.exists(): raise SystemExit('Refusing to overwrite historical media')
frames=[(int(n),float(t)) for n,t in (line.split(',') for line in (folder/'frame-times.csv').read_text().splitlines())]
if not frames or any(b[1]<=a[1] for a,b in zip(frames,frames[1:])): raise SystemExit('Invalid frame timestamps')
with wave.open(str(folder/'unity-mix.wav')) as wav:
    audio=dict(channels=wav.getnchannels(),rate=wav.getframerate(),seconds=wav.getnframes()/wav.getframerate())
    peak=nonzero=clipped=0
    while data:=wav.readframes(65536):
        values=array.array('h',data); peak=max(peak,max(map(abs,values),default=0)); nonzero+=sum(x!=0 for x in values); clipped+=sum(abs(x)>=32767 for x in values)
audio.update(peakPcm16=peak,nonzeroSamples=nonzero,clippedSamples=clipped)
if not nonzero or clipped: raise SystemExit('Silent/clipped capture: '+str(audio))
lines=[]
for i,(n,t) in enumerate(frames):
    lines += [f"file 'frames/{n:05d}.png'",f'duration {frames[i+1][1]-t if i+1<len(frames) else .2:.6f}']
lines += [f"file 'frames/{frames[-1][0]:05d}.png'"]
sequence=folder/'player-frames.ffconcat'; sequence.write_text('\n'.join(lines),encoding='utf-8')
ffmpeg=shutil.which('ffmpeg') or 'ffmpeg'
subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-f','concat','-safe','1','-i',str(sequence),'-i',str(folder/'unity-mix.wav'),'-fps_mode','vfr','-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-c:a','aac','-b:a','160k','-shortest','-movflags','+faststart',str(target)],check=True)
captures=[(int(n),label,float(t)) for n,label,t in (line.split(',') for line in (folder/'capture-times.csv').read_text().splitlines())]
clips=[]
for label in ['prepared-5-float','prepared-6-build','prepared-13-shake','prince-farewell-0','earned-memory']:
    match=next((x for x in captures if x[1]==label),None)
    if not match: continue
    start=max(0,match[2]-(38 if label.startswith('prepared') else 1)); duration=48 if label.startswith('prepared') else 26
    out=folder/(label+'.mp4')
    subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-ss',str(start),'-i',str(target),'-t',str(duration),'-c:v','libx264','-crf','18','-c:a','aac','-movflags','+faststart',str(out)],check=True)
    clips.append(dict(file=out.name,startSeconds=start,durationRequested=duration))
(folder/'media.json').write_text(json.dumps(dict(status='PASS',tier=report['tier'],presentation='Actual Unity camera/UI renders at recorded timestamps and AudioRenderer mix. Simulated production input; NOT OS-native input. Capture overhead included.',frames=len(frames),videoSeconds=frames[-1][1],audio=audio,clips=clips,sha256=hashlib.sha256(target.read_bytes()).hexdigest()),indent=2),encoding='utf-8')
print(target)
