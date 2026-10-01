# Phase O - audio and ambient presentation

Starting HEAD `5b8ea9078726d275ecb2e10e1a8df4efa58c17a0`, branch
`refactor/project-rebuild`, Unity 6000.3.7f1. Four original dirty files are
hash-protected: config, ProjectSettings batching and both UserSettings files.
The original Editor is not opened; validation uses the existing isolated copy.

## Current audio audit

`DemoAudioController` already owned Settings, four preference keys, four dynamic
AudioSources and an explicit runner handoff from BartendingFlowCoordinator.
Seven original procedural draft WAVs came from `Tools/generate_demo_audio.mjs`.
BarScene bound all seven: WinterRoom harmony, DistantWind, Pour, Ice, Glass,
Door and Page. The controller started harmony/wind, estimated pouring from
accepted ingredient/volume changes, played a generic contact for preparation
records, and attached a generic page cue to pointer/submit on all bound buttons.
CustomerSpawner called Door on spawn/departure. No mixer or customer walking,
seat, Stir, Shake or dedicated discovery sound existed. There was no accepted
soundtrack sign-off; the old tonal door cue also implied an unshown bell.

The existing additive coordinator enables one listener and restores the Bar
listener on exit. The controller is scene-owned, not DontDestroyOnLoad.
Audio preferences are integer percentages: Master 70, Music 35, Effects 60,
Mute false. Existing keys, save behavior and Settings modal/pause semantics
remain authoritative. Gameplay New Game must preserve them.

## Routing and foundation

DemoAudioController retains preference/UI ownership. SurlhwaSoundscape owns
twelve scene-local AudioSources: room, winter, reserved music, pour, technique,
discovery and six one-shot voices. Internal gains provide the existing minimal
bus model; no AudioMixer asset or second preference system is needed. Room,
winter, physical, character and UI gains all map behind Effects; future accepted
music maps behind Music. Master and Mute apply to every source immediately.
An occupied six-voice pool drops further accents instead of creating sources.

The new SurlhwaAudioLibrary references 22 original procedural clips with authored
gains. `ArtSource/Audio/README.md` and `manifest.json` record provenance/imports;
the recipe is `Tools/prepare_phase_o_audio.py`. All older draft WAVs and GUIDs
remain unchanged. Their serialized references are retained as historical data;
runtime playback now uses the new library. Music is intentionally silent:
MUSIC_COMPOSITION_DEFERRED.

Room/winter loops are 24/32 seconds. Room gain .40, exterior .24, both multiplied
by existing Master/Effects. The existing dialogue panel's visible state ducks
room to .55 and exterior to .3025 of their normal gain, using a gentle transition.
Physical loop starts/stops fade over .045 seconds. Discovery has its own bounded
voice and .04-second cancellation fade. No audio duration advances gameplay.

## Validation boundaries

Implementation and executed evidence are appended as each bounded part finishes.
Generated PCM metrics and runtime state checks cannot judge perceived warmth,
natural Foley, comfortable loudness or emotional pacing. Those remain
HUMAN_AUDIO_TUNING_LATER and HUMAN_AUDIO_APPROVAL_PENDING.

### Foundation result

Compile passed with zero errors and eight existing warnings. The existing audio
and reading-pause Play Mode fixture passed: all 22 library references, bounded
12-source ownership, one listener before/during/after additive bartending,
Master/Mute controls, New Game preference preservation, accepted pour start/stop,
Guide/Settings pause, Shake resume and unload. PCM analysis found no clipping,
bounded DC and endpoint/loop joins below .0001 full scale. This validates the
routing foundation; detailed motion-to-sound coverage follows below. Evidence:
`Temp/PhaseOAudio/foundation.log` and `compile-foundation.log`.
