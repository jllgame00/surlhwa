# Phase O original audio

Source recipe: `Tools/prepare_phase_o_audio.py`, Python + NumPy 2.2.5.
`manifest.json` records every runtime path, SHA-256, duration, gain and PCM
peak/RMS/DC/seam measurement. Reproduction uses fixed per-name seeds.

All 22 clips are original procedural synthesis: filtered noise, short damped
resonances and slow periodic envelopes. No recordings, downloaded material,
sample packs, generated speech or external sound licenses are involved. This is
deterministic synthesis, not recorded Foley or an AI audio-generation service.
The lossless masters are the runtime WAV files; duplicate masters are unnecessary.

Runtime: `Assets/_SURLHWA/Audio/PhaseO/{Ambience,Foley,Bartending,UI}`.
Mono 44.1 kHz PCM16 sources. Short clips/physical loops use PCM, Decompress On
Load and preload. Long room/winter beds use Vorbis quality .75, Compressed In
Memory and preload. No normalization or unrelated importer changes.

The seven older `Audio/Drafts` WAVs and `Tools/generate_demo_audio.mjs` remain
unchanged as historical owned drafts. Their music is not an accepted soundtrack;
the music preference/routing remains available, with no active composition.
No bell, crowd, rain, fireplace, voice acting or reward fanfare is introduced.

The room and exterior beds use long periodic noise rather than chord loops.
One-shots have tapered endpoints. Short physical loops have blended joins and
runtime start/stop fades. Sound quality and final gains require owner listening:
HUMAN_AUDIO_TUNING_LATER, HUMAN_AUDIO_APPROVAL_PENDING.
