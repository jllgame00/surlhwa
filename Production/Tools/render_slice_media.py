"""Package unedited native Unity frames at their recorded timestamps with its mix."""
import argparse
import array
import json
from pathlib import Path
import shutil
import subprocess
import wave

parser = argparse.ArgumentParser()
parser.add_argument('folder', type=Path)
args = parser.parse_args()
folder = args.folder.resolve()
report = json.loads((folder / 'run.json').read_text())
if not report['passed']:
    raise SystemExit('Refusing to package a failed run')
target = folder / 'native-playthrough.mp4'
if target.exists():
    raise SystemExit('Refusing to overwrite a historical clip')
frames = [(int(n), float(t)) for n, t in (line.split(',') for line in (folder / 'frame-times.csv').read_text().splitlines())]
lines = []
for i, (n, timestamp) in enumerate(frames):
    lines += [f"file 'frames/{n:05d}.png'", f'duration {frames[i+1][1]-timestamp if i+1<len(frames) else 0.1:.6f}']
lines += [f"file 'frames/{frames[-1][0]:05d}.png'"]
sequence = folder / 'native-frames.ffconcat'
sequence.write_text('\n'.join(lines), encoding='utf-8')
subprocess.run([shutil.which('ffmpeg') or 'ffmpeg', '-hide_banner', '-loglevel', 'error', '-f', 'concat', '-safe', '1', '-i', str(sequence), '-i', str(folder / 'unity-mix.wav'), '-fps_mode', 'vfr', '-c:v', 'libx264', '-crf', '18', '-pix_fmt', 'yuv420p', '-c:a', 'aac', '-b:a', '160k', '-shortest', '-movflags', '+faststart', str(target)], check=True)
with wave.open(str(folder / 'unity-mix.wav')) as wav:
    samples = array.array('h', wav.readframes(wav.getnframes()))
    audio = {'channels': wav.getnchannels(), 'rate': wav.getframerate(), 'seconds': wav.getnframes()/wav.getframerate(), 'peakPcm16': max(map(abs, samples), default=0), 'nonzeroSamples': sum(x != 0 for x in samples)}
if audio['nonzeroSamples'] == 0 or audio['peakPcm16'] >= 32767:
    raise SystemExit('Invalid silent or clipped native mix')
(folder / 'media.json').write_text(json.dumps({'nativeFrames':len(frames),'presentation':'Unedited Unity renders at recorded real timestamps; native AudioRenderer mix; capture overhead is included in this run only','audio':audio},indent=2),encoding='utf-8')
print(target)
