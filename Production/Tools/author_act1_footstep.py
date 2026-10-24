"""Original damped wood/felt contact candidate; no external recordings.

Retains the previous cue for comparison. RMS matching is an audition aid,
never a claim of perceptual loudness or owner approval.
"""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'Unity/Assets/Surlhwa/Act1Assets/ProductionArt/FootstepFeltWood.wav'
RATE=48000


def read(path):
    with wave.open(str(path),'rb') as f:
        return np.frombuffer(f.readframes(f.getnframes()),dtype='<i2').astype(float)/32768


def main():
    old=read(ROOT/'Unity/Assets/Surlhwa/Act1Assets/FootGrounded.wav')
    rng=np.random.default_rng(251001)
    t=np.arange(int(.55*RATE))/RATE
    # A broad, damped contact, not a sharp shoe click or a pitched drum note.
    spectrum=np.fft.rfft(rng.normal(size=len(t)))
    hz=np.fft.rfftfreq(len(t),1/RATE)
    body=np.fft.irfft(spectrum*(hz/(hz+45))**2/(1+(hz/430)**4),n=len(t))
    body/=max(abs(body))
    friction=np.convolve(rng.normal(size=len(t)),np.ones(21)/21,mode='same')
    envelope=(1-np.exp(-t/.007))*np.exp(-t/.065)
    heel=.18*np.sin(2*np.pi*(82*t-32*t*t))*(1-np.exp(-t/.010))*np.exp(-t/.035)
    toe=np.maximum(t-.048,0)
    roll=.13*friction*(1-np.exp(-toe/.012))*np.exp(-toe/.065)
    x=.8*body*envelope+heel+roll
    x*=np.sqrt(np.mean(old*old))/np.sqrt(np.mean(x*x))
    x[0]=x[-1]=0
    if max(abs(x))>=.98:
        raise ValueError('Candidate would clip; do not silently limit it.')
    DEST.parent.mkdir(parents=True,exist_ok=True)
    with wave.open(str(DEST),'wb') as f:
        f.setnchannels(1); f.setsampwidth(2); f.setframerate(RATE)
        f.writeframes(np.round(x*32767).astype('<i2').tobytes())
    evidence=dict(source='Original deterministic noise/resonance synthesis; no external samples',
                  candidate=str(DEST.relative_to(ROOT)),sampleRate=RATE,seconds=len(x)/RATE,
                  peak=float(max(abs(x))),rms=float(np.sqrt(np.mean(x*x))),
                  previousRms=float(np.sqrt(np.mean(old*old))),clippedSamples=0,
                  sha256=hashlib.sha256(DEST.read_bytes()).hexdigest(),
                  approval='HUMAN_SIGNOFF_REQUIRED: in-room weight, cadence and final loudness',
                  lifecycle='Existing accepted walk-contact event timing and volume/pause policy retained')
    (ROOT/'Validation/Manifests/art-footstep-candidate.json').write_text(json.dumps(evidence,indent=2)+'\n')
    print(json.dumps(evidence,indent=2))


if __name__=='__main__':
    main()
