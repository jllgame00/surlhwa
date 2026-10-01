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

## Event mapping and quiet behavior

| Family | Existing presentation seam | Playback |
| --- | --- | --- |
| Room / winter | Scene-owned controller Awake/disable | 24/32-second loops, half-second entry fade; dialogue ducks the exterior more than the room |
| Entrance / departure | Existing CustomerSpawner PlayDoor calls | Brief filtered exterior air; no invented bell or visible door hardware |
| Footsteps | CustomerFoleyPresentation observes authoritative root distance while MovingToSeat/Leaving | Alternating .22/.25-second samples, .56 world-unit stride, pan limited to .25; companion half-stride offset and .55 level |
| Seat / stand | Existing Sitting/Standing state entry | Quiet contact once; simultaneous paired contacts coalesce through per-cue debounce |
| Service | Root CustomerMotionPresentation.React | One GlassSet; companion reaction never duplicates shared service |
| Bottle / direct pour / Build | BartendingPhysicalPresentation rest transition and actual StreamVisible | Handle accent; Pour loop only during visible accepted flow |
| Jigger | Actual vessel transfer and return | Transfer loop and small return contact |
| Float | Existing surface-container destination | FinePour loop, including measured gentle Float |
| Ice | Accepted AddIce trace after physical placement | One ice accent |
| Stir | Actual active spoon displacement | Stir loop; stops on stillness, pause, finish or removal |
| Shake | Actual shaker displacement while its existing motion coroutine is active | Shake loop; no sound for an idle rhythm timeline |
| Strain | Visible stream with existing strainer visible | Strain loop; no clip-duration completion path |
| Dash / Splash / Top-up | Their existing physical stream | Short duration follows actual flow; no canned full-length pour tail |
| Settings / navigation | Existing open/close and accepted button callbacks | Select / Back, no hover tone or slider-update preview spam |
| Codex / guide | Existing open, close, detail selection | BookOpen / BookClose / Select / Back, with state guards |
| Discovery | Popup root becomes visible / CompletePresentation | Dedicated accent voice; visual completion/cancellation requests a .04-second fade |

BartendingAudioPresentation is installed through the existing explicit runner
handoff. BartendingToolAudio observes the local gentle-stir spoon through its
workspace owner. Read-only tool getters do not alter liquid amount, preparation,
input policy or method grading. Missing clips are silent fallbacks.

The existing customer/controller/spawner, seat, order, recipe knowledge,
campaign and persistence owners remain unchanged. CustomerMotionPresentation
only installs a visual foley observer and emits the shared glass contact.
AudioSource completion never advances those owners. J5's actual visual-feedback
completion predicate remains untouched; there is no wait based on audio length.

The bartender's recurring idle and guest seated idles remain silent. Hamlet is
intentionally quiet. Garnish placement has no added arbitrary reward sound.
No voice, character grunts, success fanfare, soundtrack or adaptive music system
was introduced. Old draft music/door/foley assets remain available but unused.

## Tuning and voice limits

The cue library stores all per-cue gains; the exact values and measured PCM
properties are in the source manifest. Master/Effects scale ambience, foley and
UI; Music retains its original preference and a silent reserved source. There
is no new AudioMixer or second preference store. Six reusable one-shot sources,
two physical loops, two ambience sources, discovery and reserved music total 12.
A seventh simultaneous accent is dropped. Per-cue gaps are .07 seconds for feet
and .12 seconds otherwise. No component is added per click/step.

Footstep right pitch is .99, left 1.0; pan is clamped to +/- .25. Periodic tool
textures start at the current unscaled-time offset, so repeated short Shake
gestures do not replay the identical noise fragment. Physical fades
are .045 seconds. Workspace cancellation and normal visible flow endings use the fade; scene-owner
disable/removal stops playback immediately. These are first-pass presentation values,
not timing inputs to gameplay. Final comfort, repetition and tactile credibility
remain HUMAN_AUDIO_TUNING_LATER.

## Review package

`Tools/build_audio_review.py` writes `Logs/Review/PhaseOAudio/index.html`, 22
individual default-gain PCM previews, a 60-second room/winter mix and a manifest.
These are not loudness-normalized and are labeled as previews rather than
captured gameplay. Master .70 x Effects .60 x the actual cue gain is retained.
Runtime ambience uses Vorbis; the previews use its lossless source PCM.

`SURLHWAAudioReviewCapture` is a bounded Editor-only cue reel through Unity's
main AudioRenderer output. It sets only the fixture's in-memory mix, restores
capture timing on disable, and does not drive a customer or save progress. Its
output is an imported/runtime mix fixture, not human listening or normal play.

### Integrated connection checks

The final extended Audio/Reading Pause fixture passes: actual bottle setup and
stream start/stop, single ice contact, Shake motion/return, Strain stream,
Settings/Guide pause and additive cleanup. A same-frame ice/Guide boundary first
revealed a dropped committed contact; that presentation regression is fixed.
The validator now uses the physical pour gate and waits for the existing
ToolsReady state instead of bypassing bottle movement. Its old reflection
assertion follows the explicitly bound new observer. No gameplay policy was
changed to obtain a pass.

The extended Phase K fixture passes all four methods, direct release, finite
Jigger transfer, Float intensity, Dash/Splash/Top-up, ice, primary Stir and Build
gentle-stir motion/removal. No extra listener is installed in the fixture; it
uses the actual method prefab listener. Phase N safety fixtures pass Hamlet,
paired guests at three seats, missing-frame and missing-presenter fallbacks.
There are no seated footstep starts or orphan observers; the pair emits one
seat, stand and shared service contact, with 12 scene-owned sources throughout.
A deliberately absent entire presenter remains silent and cannot block play.

All six focused J5 scenarios pass, including no discovery, preparation/FIT
negatives, cancellation, knowledge persistence, one farewell, natural stand /
departure and seat release. The fixture replaces only the in-memory discovery
clip with 30 seconds of silence and restores it afterward. Each visible popup
starts one sound; visual completion cuts it and farewell never waits for the
clip duration. Prior J5 evidence and historical Settings/Brew incidents remain
separate. These are programmatic fixtures, not OS pointer or human listening.

Fresh Windows packaging and the final review-output/campaign checks are recorded
in the handoff below after execution; this section does not claim their results.

### Cleanup follow-up and final integration

A direct presentation-only check reproduced playback accepted by a disabled
Soundscape. Play/StartDiscovery now reject disabled or destroyed owners. The
existing audio fixture explicitly verifies this boundary. Live workspace
cancellation uses the existing short fade; owner disable still stops all voices.
Periodic texture offsets vary repeated short Shake fragments without varying
movement timing. The complete extended Audio/Reading Pause fixture passes after
these changes, including cancellation, disable, one ice cue, physical Shake /
Strain, listener ownership, preferences and unload. Compile: zero errors,
eight existing warnings. Evidence: `cleanup-final.log`, `compile-cleanup.log`.

The single full campaign regression passed on `2ebd24b`: New Game, 47 natural
departures, ending, 16 new special visits, ordinary voices, discovery/retry /
alternatives, weak-service continuation, checkpoint/mid-dialogue/ending Continue,
seven memories, reset and legacy handoff. Later changes affect only audio
availability/fades/sample position and review packaging; those received focused
validation rather than repeating all 47 encounters. The existing Phase M
visual-after callback/raycast fixture also completed with preference restoration.
No OS foreground-input or human listening claim is made.

Measured physical traces contain 62 paired visual/audio loop transitions:
starts in the same frame, stops 0-65 ms after observed visual stop. The short
fade and frame cadence explain the bounded stop tail. These measurements do
not establish tactile naturalness or artistic approval.

## Final package and evidence

Runtime source: `f5bb936499da73ed6251d686c87c9a47d8081327`. Windows x64 review
package: `Builds/Windows-PhaseO-f5bb936/SURLHWA.exe`. Existing RunPhaseO extension
of SURLHWAWindowsBuild completed with Unity 6000.3.7f1 at 2026-10-01 16:24:02 UTC,
in 30.500 seconds. 216,170,141 reported build bytes; two warnings, zero errors.
The warnings are the KoreanFallback NativeFormatImporter consistency warning
and the existing missing RuntimePipelineConfig warning. No font fix is claimed.
The package was copied from the existing isolated project and hash-verified.
Committed PlayerSettings were used for the build; original batching settings
and all other protected files were not modified.

The final actual Unity-output cue reel has 68.992 seconds of 48 kHz stereo PCM,
peak .065094 and RMS .005672 full scale, no clipped samples. It includes imported
ambience and all cue families at default gains. Earlier `2ebd24b` capture is
retained separately. This is a scripted mix fixture, not captured human play.

Review: `Logs/Review/PhaseOAudio/index.html`, individual WAVs, provenance/review
manifests, `unity-output-metrics.json`, `visual-audio-transitions.json`,
`audio-cleanup-final.txt`, four focused synchronization logs,
`j5-after-timeline.txt`, `campaign-2ebd24b.txt`, compile/static records,
`startup-result.json`, and `editor-preservation.txt`.

Standalone startup was alive/responsive after 93 seconds and logged no exception
or audio error. The task player closed normally through WM_CLOSE. Its private
registry preferences were restored exactly. A hidden-window PrintWindow capture
was black: it is retained as unsuccessful capture evidence, not a rendered bar
or Korean-text check. No second foreground/focus campaign was attempted. Visible
standalone review, Korean font/rendering confirmation, full standalone gameplay
and native pointer input remain VALIDATION_DEBT. The Editor production-scene
and audio-output evidence does not imply human listening or aesthetic approval.

After tests/capture/build, the independent private Editor snapshot matched
progress, recipe knowledge and all audio preferences. The idle task Editor had
no dirty scene and exited normally. Four original dirty-file hashes remain
unchanged. No scene, prefab, animation art, cocktail art, save format, recipe
identity, gameplay lifecycle or audio preference key was changed by Phase O.

Classification: PASS for implemented/automated scope; VALIDATION_DEBT as listed;
HUMAN_AUDIO_TUNING_LATER / HUMAN_AUDIO_APPROVAL_PENDING;
MUSIC_COMPOSITION_DEFERRED. Voice acting, external sample acquisition, adaptive
music and further campaign/content expansion are OUT_OF_SCOPE. Historical
Settings/Brew incidents remain separate. Human review still owns final warmth,
physical credibility, balance and silence around dialogue.
