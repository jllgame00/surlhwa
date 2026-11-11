"""Package passing ACT 1 Player renders/audio; input remains explicitly simulated."""
import argparse, array, bisect, hashlib, json, math, re, shutil, subprocess, wave
from pathlib import Path


def audio_clock_frames(folder, frames, audio):
    """Join recorded images to AudioRenderer time; never generate missing frames."""
    try:
        rows=[json.loads(line) for line in (folder/'motion-times.jsonl').read_text(encoding='utf-8').splitlines()]
        if len(rows)!=len(frames) or len(rows)<2: raise ValueError('missing or extra frame records')
        ids=[n for n,_ in frames]
        if ids!=list(range(1,len(frames)+1)): raise ValueError('missing or invalid raw frame IDs')
        if any(type(r['frame']) is not int for r in rows) or [r['frame'] for r in rows]!=ids: raise ValueError('frame IDs differ')
        wall=[float(r['time']) for r in rows]; clock=[float(r['audioTime']) for r in rows]
        for values in (wall,[t for _,t in frames]):
            if any(not math.isfinite(t) or t<0 for t in values) or any(b<=a for a,b in zip(values,values[1:])):
                raise ValueError('non-finite, negative or non-increasing clock')
        if any(not math.isfinite(t) or t<0 for t in clock) or any(b<a for a,b in zip(clock,clock[1:])):
            raise ValueError('non-finite, negative or backward audio clock')
        tolerance=max(1/audio['rate'],.0001)  # Float timestamps can round the final sample slightly upward.
        if clock[-1]>audio['seconds']+tolerance: raise ValueError('audio clock exceeds the WAV')
        images=[]
        for n,row in zip(ids,rows):
            name=row.get('image',f'frames/{n:05d}.png')
            if not isinstance(name,str) or not re.fullmatch(r'frames/'+f'{n:05d}'+r'\.(png|jpg)',name):
                raise ValueError('unsafe or mismatched recorded image')
            path=folder/name
            if not path.is_file() or not path.resolve().is_relative_to(folder/'frames'):
                raise ValueError('missing image or image outside frames')
            images.append(name)
        return list(zip(ids,clock)),images,wall
    except (OSError,ValueError,KeyError,TypeError) as error:
        raise SystemExit('Invalid audio-clock evidence: '+str(error)) from error


def map_wall_time(time, wall, clock, seconds):
    # Interpolate clock coordinates only. Images remain the original observations.
    # Requested clip handles can reach the recording bounds; never extrapolate.
    if time<wall[0]: return 0
    if time>wall[-1]: return seconds
    if time==wall[-1]: return clock[-1]
    i=bisect.bisect_right(wall,time)-1
    return clock[i]+(clock[i+1]-clock[i])*(time-wall[i])/(wall[i+1]-wall[i])


p=argparse.ArgumentParser(); p.add_argument('folder',type=Path)
p.add_argument('--audio-clock',action='store_true',help='Use recorded motion frame/audioTime pairs; requires complete detailed media.')
args=p.parse_args()
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
images=[f'frames/{n:05d}.png' for n,_ in frames]
wall=None;raw_frame_count=len(frames);omitted=[]
if args.audio_clock:
    frames,images,wall=audio_clock_frames(folder,frames,audio)
    # Validate every raw row/image first. AudioRenderer advances in buffered
    # chunks, so several real images can share one observed audio frontier.
    # Keep the last image of each plateau without inventing intermediate times.
    clock=[t for _,t in frames]
    keep=[i for i,t in enumerate(clock) if i==len(clock)-1 or t!=clock[i+1]]
    omitted=[frames[i][0] for i in range(len(clock)-1) if clock[i]==clock[i+1]]
    frames=[frames[i] for i in keep];images=[images[i] for i in keep]
captures=[(int(n),label,float(t)) for n,label,t in (line.split(',') for line in (folder/'capture-times.csv').read_text().splitlines())]
if args.audio_clock and any(not math.isfinite(t) or not wall[0]<=t<=wall[-1] for _,_,t in captures):
    raise SystemExit('Invalid audio-clock evidence: capture outside the observed wall clock')
clips=[]
for label in ['prepared-0-stir','prepared-5-float','prepared-6-build','prepared-13-shake','prince-farewell-0','earned-memory']:
    match=next((x for x in captures if x[1]==label),None)
    if not match: continue
    start=max(0,match[2]-(38 if label.startswith('prepared') else 1)); duration=48 if label.startswith('prepared') else 26
    clip=dict(file=label+'.mp4',startSeconds=start,durationRequested=duration)
    if args.audio_clock:
        end=map_wall_time(start+duration,wall,clock,audio['seconds'])
        mapped=map_wall_time(start,wall,clock,audio['seconds'])
        clip.update(startSeconds=mapped,durationRequested=end-mapped,captureWallSeconds=match[2],
                    sourceWallStartSeconds=start,sourceWallDurationSeconds=duration)
        if end<=mapped: raise SystemExit('Invalid audio-clock evidence: empty mapped clip')
    clips.append(clip)
for name in ['player-frames.ffconcat','media.json']+[clip['file'] for clip in clips]:
    if (folder/name).exists(): raise SystemExit('Refusing to overwrite historical media: '+name)
lines=[]
for i,(n,t) in enumerate(frames):
    duration=frames[i+1][1]-t if i+1<len(frames) else max(1/audio['rate'],audio['seconds']-t) if args.audio_clock else .2
    lines += [f"file '{images[i]}'"]
    if args.audio_clock: lines += [f"option framerate {audio['rate']}"]
    lines += [f'duration {duration:.9f}' if args.audio_clock else f'duration {duration:.6f}']
lines += [f"file '{images[-1]}'"]
if args.audio_clock: lines += [f"option framerate {audio['rate']}"]
sequence=folder/'player-frames.ffconcat'; sequence.write_text('\n'.join(lines),encoding='utf-8')
ffmpeg=shutil.which('ffmpeg') or 'ffmpeg'
offset=['-itsoffset',str(frames[0][1])] if args.audio_clock else []
precision=['-enc_time_base:v',f"1:{audio['rate']}",'-video_track_timescale',str(audio['rate'])] if args.audio_clock else []
# safe=0 is needed for per-image framerate options; the opt-in path validates every filename above.
subprocess.run([ffmpeg,'-hide_banner','-loglevel','error',*offset,'-f','concat','-safe','0' if args.audio_clock else '1','-i',str(sequence),'-i',str(folder/'unity-mix.wav'),'-fps_mode','vfr','-c:v','libx264','-crf','18','-pix_fmt','yuv420p',*precision,'-c:a','aac','-b:a','160k','-shortest','-movflags','+faststart',str(target)],check=True)
for clip in clips:
    clip_timing=['-fps_mode','vfr',*precision] if args.audio_clock else []
    subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-ss',str(clip['startSeconds']),'-i',str(target),'-t',str(clip['durationRequested']),'-c:v','libx264','-crf','18',*clip_timing,'-c:a','aac','-movflags','+faststart',str(folder/clip['file'])],check=True)
result=dict(status='PASS',tier=report['tier'],presentation='Actual Unity camera/UI renders at recorded timestamps and AudioRenderer mix. Simulated production input; NOT OS-native input. Capture overhead included.',frames=len(frames),videoSeconds=frames[-1][1],audio=audio,clips=clips,sha256=hashlib.sha256(target.read_bytes()).hexdigest())
if args.audio_clock:
    ffprobe=shutil.which('ffprobe') or 'ffprobe'
    encoded=json.loads(subprocess.check_output([ffprobe,'-v','error','-select_streams','v:0','-show_entries',
        'stream=codec_name,profile,level,time_base,r_frame_rate,avg_frame_rate,nb_frames,start_time,duration','-of','json',str(target)],text=True))['streams'][0]
    result.update(clock='AudioRenderer',videoSeconds=audio['seconds'],videoStartSeconds=frames[0][1],
                  observedWallEndSeconds=wall[-1],observedAudioEndSeconds=frames[-1][1],
                  rawFrames=raw_frame_count,selectedRecordedFrames=len(frames),encodedFrames=int(encoded['nb_frames']),encodedVideo=encoded,
                  omittedFrameIds=omitted,omittedFrameCount=len(omitted),
                  audioClockLimitation='AudioRenderer exposes a buffered, quantized sample frontier. Equal audioTime observations retain their last recorded frame; omitted raw images remain unchanged. Encoded frame count includes any terminal repeat of the last recorded image used to hold its duration. No intermediate image timestamps are invented.',
                  clockMapping='Recorded frame/audioTime pairs; clip wall boundaries mapped piecewise linearly between observations. No synthetic or interpolated images.')
(folder/'media.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(target)
