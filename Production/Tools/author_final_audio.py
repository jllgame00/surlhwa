"""Original final-candidate contacts and bar-return arrangements; no external samples.

Writes new source assets only. Historical audio and runtime bindings are untouched.
"""
import hashlib
import json
import re
import shutil
import subprocess
import wave
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'SourceArt/FinalProduction/Audio'
RATE = 48000
NOTES = [(0.75, 64, .105, -.12), (4.10, 59, .085, .08),
         (8.60, 66, .073, .12), (13.0, 64, .067, -.08), (18.6, 62, .048, .04)]


def noise_band(rng, length, low, high):
    hz = np.fft.rfftfreq(length, 1 / RATE)
    spectrum = np.fft.rfft(rng.normal(size=length))
    x = np.fft.irfft(spectrum * (hz / (hz + low)) ** 2 / (1 + (hz / high) ** 4), n=length)
    return x / max(abs(x))


def foot(rolling=False):
    rng = np.random.default_rng(261111 + int(rolling))
    t = np.arange(int(.52 * RATE)) / RATE
    body = noise_band(rng, len(t), 42, 360 if rolling else 470)
    sole = noise_band(rng, len(t), 180, 1800)
    heel = (1 - np.exp(-t / .006)) * np.exp(-t / (.070 if rolling else .052))
    toe = np.maximum(t - (.055 if rolling else .038), 0)
    roll = (1 - np.exp(-toe / .009)) * np.exp(-toe / .055)
    x = .62 * body * heel + .14 * sole * roll
    x += .08 * np.sin(2 * np.pi * (76 * t - 24 * t * t)) * heel
    x *= np.minimum(1, np.maximum(0, (.50 - t) / .025))
    return x


def shaker():
    rng = np.random.default_rng(261112)
    t = np.arange(int(.24 * RATE)) / RATE
    x = np.zeros(len(t))
    for delay, gain in [(0, .19), (.021, .10), (.049, .055)]:
        u = np.maximum(0, t - delay)
        x += gain * noise_band(rng, len(t), 700, 5200) * (1 - np.exp(-u / .0018)) * np.exp(-u / .024)
    return x * np.minimum(1, np.maximum(0, (.22 - t) / .025))


def motif(alternate=False):
    result = np.zeros((int(26.5 * RATE), 2))
    notes = NOTES + ([] if alternate else [(1.12, 52, .022, -.03), (13.38, 47, .016, .03)])
    for index, (start, midi, gain, pan) in enumerate(notes):
        t = np.arange(7 * RATE) / RATE
        hz = 440 * 2 ** ((midi - 69) / 12)
        tone = sum(level * np.sin(2 * np.pi * hz * partial * t) * np.exp(-t / decay)
                   for partial, level, decay in [(1, 1, 1.95), (2.003, .22, .90), (3.009, .055, .48), (4.012, .018, .25)])
        tone *= gain * (1 - np.exp(-t / (.022 if alternate else .014)))
        tone *= np.minimum(1, np.maximum(0, (7 - t) / .4))
        stereo = tone[:, None] * np.array([np.sqrt((1 - pan) / 2), np.sqrt((1 + pan) / 2)])
        at = round(start * RATE); end = min(len(result), at + len(t))
        result[at:end] += stereo[:end-at]
        # Two quiet, dark reflections; no continuous pad beneath the reading.
        for delay, level in [(.037, .065), (.089, .035)]:
            offset = at + round(delay * RATE); stop = min(len(result), offset + len(t))
            result[offset:stop] += stereo[:stop-offset, ::-1] * level
    return result


def read(path):
    with wave.open(str(path), 'rb') as f:
        if f.getframerate() != RATE or f.getsampwidth() != 2:
            raise ValueError('Comparison must retain its actual PCM16/48k format')
        return np.frombuffer(f.readframes(f.getnframes()), dtype='<i2').astype(float) / 32768


def write(path, samples):
    samples = np.array(samples, copy=True); samples[0] = samples[-1] = 0
    if not np.isfinite(samples).all() or np.max(abs(samples)) >= .98:
        raise ValueError('Non-finite/clipping candidate; no silent limiter')
    with wave.open(str(path), 'wb') as f:
        f.setnchannels(1 if samples.ndim == 1 else samples.shape[1]); f.setsampwidth(2); f.setframerate(RATE)
        f.writeframes(np.round(samples * 32767).astype('<i2').tobytes())
    return dict(sha256=hashlib.sha256(path.read_bytes()).hexdigest(), seconds=len(samples)/RATE,
                channels=1 if samples.ndim == 1 else samples.shape[1], peak=float(np.max(abs(samples))),
                rms=float(np.sqrt(np.mean(samples*samples))), dc=float(np.mean(samples)), clippedSamples=0)


def contact_sequence(samples):
    out = np.zeros(RATE * 4)
    for start in [.3, 1.0, 1.7, 2.4]:
        at = round(start * RATE); out[at:at+len(samples)] += samples
    return out


def integrated_loudness(path):
    command = [shutil.which('ffmpeg') or 'ffmpeg', '-hide_banner', '-nostats', '-i', str(path), '-af', 'ebur128=peak=true', '-f', 'null', '-']
    run = subprocess.run(command, capture_output=True, text=True, check=True)
    values = re.findall(r'I:\s*([-\d.]+) LUFS', run.stderr)
    if not values: raise ValueError('No measured EBU R128 integrated loudness')
    return float(values[-1])


def main():
    if OUT.exists(): raise SystemExit('Preserve existing original candidates; use a versioned source directory for revisions')
    OUT.mkdir(parents=True)
    control = read(ROOT/'Unity/Assets/Surlhwa/Act1Assets/ProductionArt/FootstepFeltWood.wav')
    candidates = {'FootGroundedBody':foot(), 'FootGroundedRoll':foot(True), 'ShakeContact':shaker(),
                  'BarReturn':motif(), 'BarReturnSparse':motif(True)}
    assets = {name:write(OUT/(name+'.wav'), x) for name, x in candidates.items()}
    comparison = {'Control':control, 'Body':candidates['FootGroundedBody'], 'Roll':candidates['FootGroundedRoll']}
    levels = {}
    for name, x in comparison.items():
        path=OUT/('FootSequence-'+name+'.wav'); write(path,contact_sequence(x)); levels[name]=integrated_loudness(path)
    matching = {}
    for name, x in comparison.items():
        gain_db = levels['Control'] - levels[name]; gain=10**(gain_db/20)
        matching[name]=dict(measuredLufs=levels[name], auditionGainDb=gain_db,
                           **write(OUT/('FootMatched-'+name+'.wav'),contact_sequence(x)*gain))
    assets['FootGroundedFinal']=write(OUT/'FootGroundedFinal.wav',candidates['FootGroundedBody']*10**(matching['Body']['auditionGainDb']/20))
    manifest = dict(authorship='Original deterministic synthesis and authored note/rest/dynamic arrangement; no external samples',
                    sampleRate=RATE, assets=assets, noteEvents=NOTES,
                    selectedFoot='FootGroundedFinal: Body at control-matched sequence loudness; final in-room mix validation remains pending',selectedMusic='BarReturn: sparse five-note contour with two quiet lower resonances',
                    selectionLimit='Source shape/level and authored arrangement assessed; this agent cannot hear tool audio. Listening and emotional approval remain explicit Gate3 work, not an automated PASS.',
                    bassEvents=[(1.12,52,.022,-.03),(13.38,47,.016,.03)],
                    matchedAuditions=matching, loudnessMethod='FFmpeg EBU R128 integrated level of identical four-contact sequences; audition matching only, not final mix approval',
                    generatorSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
                    protectedControlSha256=hashlib.sha256((ROOT/'Unity/Assets/Surlhwa/Act1Assets/ProductionArt/FootstepFeltWood.wav').read_bytes()).hexdigest(),
                    gate='HUMAN_SIGNOFF_REQUIRED at Gate3; no subjective PASS')
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(candidates=list(assets),matchedLufs=levels),indent=2))


if __name__ == '__main__': main()
