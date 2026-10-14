"""Original procedural ACT 1 audio drafts; no recordings or third-party samples."""
from pathlib import Path
import wave, math, json
import numpy as np
root=Path(__file__).resolve().parents[1]
out=root/'Unity/Assets/Surlhwa/Act1Assets'
out.mkdir(exist_ok=True)
rate=48000
rng=np.random.default_rng(1401)
def save(name,x):
    x=np.asarray(x); x[0]=x[-1]=0
    with wave.open(str(out/(name+'.wav')),'wb') as f:
        f.setnchannels(1); f.setsampwidth(2); f.setframerate(rate); f.writeframes((np.clip(x,-.99,.99)*32767).astype('<i2').tobytes())
    return dict(seconds=len(x)/rate,peak=float(np.max(np.abs(x))),rms=float(np.sqrt(np.mean(x*x))),source='Original procedural synthesis, authored for ACT 1; provisional owner listening')
t=np.arange(int(.55*rate))/rate
noise=rng.normal(0,1,len(t)); smooth=np.convolve(noise,np.ones(45)/45,mode='same')
attack=1-np.exp(-t/.004)
x=attack*(.55*np.sin(2*np.pi*(108*t-20*t*t))*np.exp(-t/ .075)+.26*np.sin(2*np.pi*207*t)*np.exp(-t/.042)+.25*smooth*np.exp(-t/.10)+.055*noise*np.exp(-t/.018))
with wave.open(str(root/'Unity/Assets/Surlhwa/SliceAssets/FootLeft.wav'),'rb') as f:
    old=np.frombuffer(f.readframes(f.getnframes()),dtype='<i2').astype(float)/32768
oldrms=np.sqrt(np.mean(old*old)); x*=oldrms/np.sqrt(np.mean(x*x))
meta={'FootGrounded':save('FootGrounded',x)}
# A small unresolved three-note idea, with air between phrases and no reward cadence.
y=np.zeros(rate*22)
notes=[(0,64,.28),(3.3,59,.22),(6.7,66,.20),(11.2,64,.16),(15.1,62,.12)]
for start,midi,gain in notes:
    n=np.arange(rate*6)/rate; hz=440*2**((midi-69)/12)
    tone=(np.sin(2*np.pi*hz*n)+.18*np.sin(2*np.pi*2*hz*n)+.035*np.sin(2*np.pi*3*hz*n))
    tone*=gain*(1-np.exp(-n/.018))*np.exp(-n/1.65)
    at=int(start*rate); end=min(len(y),at+len(tone)); y[at:end]+=tone[:end-at]
y*=.36
meta['FirstNightMotif']=save('FirstNightMotif',y)
meta['footstepComparison']={'historicalRms':float(oldrms),'candidateRms':meta['FootGrounded']['rms'],'matchedRmsBeforeProductionMix':True,'notFinalLoudnessApproval':True}
(root/'Validation/Manifests/audio-act1-drafts.json').write_text(json.dumps(meta,indent=2),encoding='utf-8')
print(json.dumps(meta,indent=2))
