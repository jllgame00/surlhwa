"""Compare fresh Q1 polish with retained native Q1 evidence; never rewrite the old package."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import argparse, hashlib, html, json, subprocess
V=Path(__file__).resolve().parents[1]/'Validation'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    p=argparse.ArgumentParser(); p.add_argument('ordinary'); p.add_argument('prince'); a=p.parse_args()
    old=json.loads((V/'Manifests/quarter-view-package.json').read_text())
    oldshots=[dict(r,version='Previous Q1') for r in old['shots'] if r['candidate']=='Q1']
    oldclips=[dict(r,version='Previous Q1') for r in old['clips'] if r['candidate']=='Q1']
    shots=[]; clips=[]; measures=[]; checks={}
    for actor,name in [('Ordinary',a.ordinary),('Prince',a.prince)]:
        folder=V/'Evidence'/name
        result=json.loads((folder/'result.json').read_text(encoding='utf-8-sig'))
        process=json.loads((folder/'process.json').read_text(encoding='utf-8-sig'))
        assert process['exitCode']==0 and not process['timedOut'] and not result['errors']
        assert result['actor']==actor and len(result['measurements'])==2 and result['motion']
        checks[actor]=result['checks']
        assert any('Continue resumes' in c for c in result['checks'])
        for s in result['shots']:
            file=folder/s['file']
            with Image.open(file) as im: assert im.size==(s['width'],s['height'])
            shots.append(dict(s,actor=actor,version='Polished Q1',path=file.relative_to(V).as_posix(),sha256=sha(file)))
        measures.extend(dict(m,actor=actor,version='Polished Q1') for m in result['measurements'])
        for width in (1280,1920):
            frames=folder/f'Q1_{actor}_motion_{width}'; files=sorted(frames.glob('*.png'))
            assert len(files)==len(result['motion'])
            lines=['ffconcat version 1.0']
            for i,file in enumerate(files):
                with Image.open(file) as im: assert im.size==(width,width*9//16)
                pose=result['motion'][i]; following=result['motion'][i+1] if i+1<len(files) else None
                duration=following['time']-pose['time'] if following and following['phase']==pose['phase'] else 1/12
                lines.extend([f"file '{file.resolve().as_posix()}'",f'duration {max(.01,duration):.6f}'])
            lines.append(f"file '{files[-1].resolve().as_posix()}'")
            concat=frames.with_suffix('.ffconcat'); concat.write_text('\n'.join(lines)+'\n')
            movie=frames.with_suffix('.mp4')
            subprocess.run(['ffmpeg','-hide_banner','-loglevel','error','-y','-safe','0','-i',str(concat),'-vsync','vfr','-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(movie)],check=True)
            stream=json.loads(subprocess.check_output(['ffprobe','-v','error','-select_streams','v:0','-show_entries','stream=width,height,nb_frames,duration','-of','json',str(movie)]))['streams'][0]
            assert (stream['width'],stream['height'])==(width,width*9//16)
            clips.append(dict(actor=actor,version='Polished Q1',width=width,path=movie.relative_to(V).as_posix(),sha256=sha(movie),stream=stream))
    # Old originals are hash-verified, not copied, resized in place or relabeled.
    for r in oldshots+oldclips: assert sha(V/r['path'])==r['sha256']
    board=Image.new('RGB',(1648,1576),'#172329'); d=ImageDraw.Draw(board)
    font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',25); small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',18)
    d.text((24,16),'SURLHWA / Q1 locked direction / bounded polish',font=font,fill='#edddbc')
    d.text((24,52),'Same fixed camera. Native Unity evidence. Final spatial confirmation remains with the owner.',font=small,fill='#bac7cd')
    fixtures=[('Prince','Composition'),('Ordinary','Wide'),('Prince','Grounding')]
    for col,(label,rows) in enumerate([('Previous Q1',oldshots),('Polished Q1',shots)]):
        x=24+col*812; d.text((x,90),label,font=font,fill='#ddb57e')
        for row,(actor,fixture) in enumerate(fixtures):
            r=next(r for r in rows if r['actor']==actor and r['fixture']==fixture and r['width']==1920)
            with Image.open(V/r['path']) as im: board.paste(im.resize((788,443),Image.Resampling.NEAREST),(x,130+row*480))
            d.text((x,576+row*480),f'{actor} / {fixture}',font=small,fill='#bac7cd')
    board.save(V/'Q1_POLISH_COMPARISON.png')
    style='body{margin:0;background:#172329;color:#e9dec7;font:16px/1.6 system-ui}main{max-width:1600px;margin:auto;padding:28px}a{color:#e1bd84}.grid{display:grid;grid-template-columns:1fr 1fr;gap:16px}figure{margin:0}img,video{width:100%}figcaption{font-size:14px;color:#bcc9cf}td,th{padding:9px;text-align:left;border-bottom:1px solid #456}table{border-collapse:collapse;width:100%}'
    body=['<!doctype html><html lang="en"><meta charset="utf-8"><title>SURLHWA / Q1 polish</title><style>'+style+'</style><main>',
      '<h1>SURLHWA / Q1 bounded polish</h1><p>Q1_BOUNDED_POLISH_READY_FOR_OWNER_REVIEW · Q1 is the active Production spatial baseline. Q2 is reference only.</p>',
      '<p><a href="Q1_POLISH_REVIEW.md">Full report, validation and remaining debt</a> · <a href="review-quarter-view.html">Preserved historical comparison</a></p>',
      '<a href="Q1_POLISH_COMPARISON.png"><img src="Q1_POLISH_COMPARISON.png" alt="Previous and polished Q1 comparison board"></a>',
      '<p>World plates hide UI/tools equally. Camera angle, orthographic size and exterior framing are unchanged. Polished actors are 12% larger around their contact pivots. Prince uses a side-quarter seated pose. Every native original remains linked.</p>']
    def figure(r,rows):
        large=next(x for x in rows if x['actor']==r['actor'] and x['fixture']==r['fixture'] and x['width']==1920)
        return f'<figure><a href="{r["path"]}"><img loading="lazy" src="{r["path"]}" alt="{html.escape(r["version"]+" / "+r["actor"]+" / "+r["fixture"])}"></a><figcaption>{r["version"]} · <a href="{r["path"]}">720p original</a> · <a href="{large["path"]}">1080p original</a></figcaption></figure>'
    for actor in ('Ordinary','Prince'):
        for fixture in ('Empty','Seated','Composition','Wide','Grounding','ContextUI'):
            matches=[r for r in oldshots+shots if r['actor']==actor and r['fixture']==fixture and r['width']==1280]
            if not matches: continue
            body.append(f'<h2>{actor} / {fixture}</h2><div class="grid">')
            for r in matches: body.append(figure(r,oldshots if r['version']=='Previous Q1' else shots))
            body.append('</div>')
            if actor=='Prince' and fixture=='ContextUI': body.append('<p>Historical Prince ContextUI was a forced Q1 tool diagnostic, not normal brewing. Current actual focused tools and Q1 reaction reading are below; they are not equivalent gameplay phases.</p>')
        body.append(f'<h2>{actor} / current normal-play UI</h2><div class="grid">')
        for r in shots:
            if r['actor']==actor and r['fixture'] in ('ReactionUI','FocusedTools') and r['width']==1280: body.append(figure(r,shots))
        body.append('</div>')
    body.append('<h2>Representative ACT 1 movement</h2><p>Recorded controller poses, replayed at sampled timing (~12 Hz). Ordinary includes entrance; Prince starts from the retained earned checkpoint. Both include reaction/departure. Polished Prince uses the new seated pose; the historical Prince clip retained original art. No new turn animation, native-input, audio or 60 fps certification.</p><div class="grid">')
    for actor in ('Ordinary','Prince'):
        for rows in (oldclips,clips):
            r=next(c for c in rows if c['actor']==actor and c['width']==1280); large=next(c for c in rows if c['actor']==actor and c['width']==1920)
            body.append(f'<figure><video controls preload="none" src="{r["path"]}"></video><figcaption>{actor} / {r["version"]} · <a href="{large["path"]}">1080p original</a></figcaption></figure>')
    body.append('</div><h2>Bounded performance</h2><p>10 seconds after 60 warmup frames. Uncapped Development Player, offscreen URP world plates. Historical samples were collected earlier; this is not a controlled hardware benchmark or GPU-time claim. New harness reserves sample storage before measurement; old GC includes list growth.</p><table><tr><th>View / actor / width</th><th>P50 / P95 ms</th><th>Renderers / materials</th><th>Draws / batches</th><th>GC mean bytes</th><th>Allocated MiB</th></tr>')
    measurements=[dict(m,version='Previous Q1') for m in old['measurements'] if m['candidate']=='Q1']+measures
    for m in measurements: body.append(f'<tr><td>{m["version"]} / {m["actor"]} / {m["width"]}</td><td>{m["p50"]:.3f} / {m["p95"]:.3f}</td><td>{m["renderers"]} / {m["materials"]}</td><td>{m["draws"]:.1f} / {m["batches"]:.1f}</td><td>{m["gcMean"]:.2f}</td><td>{m["allocated"]/1048576:.2f}</td></tr>')
    body.append('</table><p><a href="Manifests/q1-polish-package.json">Hashes, camera bounds, checks and measurements</a></p></main></html>')
    (V/'review-q1-polish.html').write_text('\n'.join(body),encoding='utf-8')
    (V/'Manifests/q1-polish-package.json').write_text(json.dumps(dict(status='Q1_BOUNDED_POLISH_READY_FOR_OWNER_REVIEW',shots=shots,clips=clips,previousShots=oldshots,previousClips=oldclips,measurements=measurements,checks=checks),indent=2)+'\n')
    print(f'Packaged {len(shots)} new native stills / {len(clips)} new clips / {len(measures)} new performance rows')
if __name__=='__main__': main()
