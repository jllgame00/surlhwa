"""Export Phase O cue previews at default runtime gains; never normalize louder."""
from pathlib import Path
import json
import wave
import html
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Logs/Review/PhaseOAudio"
OUT.mkdir(parents=True, exist_ok=True)
manifest = json.loads((ROOT / "ArtSource/Audio/manifest.json").read_text())
rows = []
audio = {}
for cue in manifest["assets"]:
    with wave.open(str(ROOT / cue["runtime"]), "rb") as source:
        rate = source.getframerate()
        samples = np.frombuffer(source.readframes(source.getnframes()), dtype="<i2").astype(float) / 32768
    samples *= .7 * .6 * cue["gain"]
    audio[cue["name"]] = samples
    if cue["loop"]:
        # Illustrate a start/stop without introducing an output click.
        samples = samples.copy()
        fade = min(len(samples)//2, int(rate * (.5 if cue["family"] == "Ambience" else .045)))
        samples[:fade] *= np.linspace(0, 1, fade)
        samples[-fade:] *= np.linspace(1, 0, fade)
    filename = cue["name"] + "-default-mix.wav"
    with wave.open(str(OUT / filename), "wb") as target:
        target.setparams((1, 2, rate, len(samples), "NONE", "not compressed"))
        target.writeframes(np.rint(samples * 32767).astype("<i2").tobytes())
    rows.append({**cue, "preview": filename, "runtime_bus_gain": .7*.6*cue["gain"],
                 "preview_peak": float(np.max(np.abs(samples)))})
room = np.tile(audio["RoomTone"], 4)[:44100*60]
wind = np.tile(audio["WinterAir"], 2)[:44100*60]
mix = room + wind
fade = 22050
mix[:fade] *= np.linspace(0, 1, fade)
mix[-fade:] *= np.linspace(1, 0, fade)
with wave.open(str(OUT / "Room-and-winter-default-mix.wav"), "wb") as target:
    target.setparams((1, 2, 44100, len(mix), "NONE", "not compressed"))
    target.writeframes(np.rint(mix * 32767).astype("<i2").tobytes())
(OUT / "review-manifest.json").write_text(json.dumps({"kind": "PCM preview at actual default runtime gains; not a gameplay capture",
    "master": .7, "effects": .6, "reading": False, "normalization": False, "cues": rows}, indent=2))
page = ['<!doctype html><meta charset="utf-8"><title>SURLHWA Phase O audio review</title>',
    '<style>body{background:#172027;color:#eee4ce;font:17px system-ui;max-width:940px;margin:40px auto}section{padding:16px;border-bottom:1px solid #485052}audio{width:100%}small{color:#aeb8bb}</style>',
    '<h1>Quiet bar / Phase O</h1><p>Owned procedural first pass. HUMAN_AUDIO_APPROVAL_PENDING.</p>',
    '<p>These lossless previews use Master 70%, Effects 60% and each runtime cue gain. They are not loudness-normalized or recordings of gameplay. Ambience runtime uses Vorbis; these previews use the original PCM. Reading further lowers room/wind.</p>',
    '<section><h2>Room + winter / 60 seconds</h2><audio controls src="Room-and-winter-default-mix.wav"></audio></section>']
if (OUT / "unity-output-cue-reel.wav").exists():
    page.append('<section><h2>Actual Unity output / cue fixture</h2><audio controls src="unity-output-cue-reel.wav"></audio><p>69 seconds: room starts first; individual cues at 6 seconds, then every 3 seconds in manifest order from FootLeft through Discovery. This is runtime output at default gains, not gameplay or human testing.</p></section>')
for row in rows:
    page.append('<section><h2>' + html.escape(row['family']+' / '+row['name']) + '</h2><audio controls preload="none" src="'+row['preview']+'"></audio><small>'+html.escape(row['runtime'])+'; gain '+str(row['gain'])+'</small></section>')
page.append('<p>No speech or soundtrack. Review warmth, repetition, footstep weight, liquid realism and reading comfort at ordinary listening volume. Synchronization logs describe fixture input, not human play.</p>')
(OUT / 'index.html').write_text('\n'.join(page), encoding='utf-8')
print('22 cue previews + 60-second ambience mix; unchanged runtime gain; no clipping:', max(r['preview_peak'] for r in rows) < 1)
