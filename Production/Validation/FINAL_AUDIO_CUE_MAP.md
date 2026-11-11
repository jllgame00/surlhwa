# Final Production audio cue map

Recorded 2026-11-11 for P12. This is the current implementation and source-candidate record for `Bar_Q1_Final.unity`, not subjective audio approval. Canon 15 AUD01–AUD04 supplies the quiet-bar direction: room, winter, actual physical actions, silence and a sparse composed return. There is no character-theme quota, voice layer, runtime reward or new audio manager.

## Evidence and review boundary

The existing [integration-02 XML](Evidence/p09-p12-integration-02.xml) records **162/162 PASS**, zero failed/skipped, including **48/48 audio and accepted-action cases**: 23 `CampaignAudioTests` and 25 `CampaignAudioAcceptedCueTests`. The [matching log](Evidence/p09-p12-integration-02.log) accompanies that run. That Unity run was not repeated for this documentation task.

Subsequent controller-level menu regressions in [final-shelf-menu-red-01.xml](Evidence/final-shelf-menu-red-01.xml) both failed as expected: the initial menu's room gain was zero instead of 0.0975, and returning to the saved-game menu left existing source gains nonzero after mute. The controller now refreshes audio in its menu update as well as during a session. [Full Editor 03](Evidence/final-editor-full-03.xml) verifies **859/859 PASS**, including both menu regressions and all **50/50 audio/accepted-action/menu cases**. Its [log](Evidence/final-editor-full-03.log) identifies the RTX 3070 graphics renderer; the coordinating run observed process exit 0.

The preceding Full Editor 02 result remains **846 PASS / 13 FAIL**, classified in its retained [environment record](Evidence/final-editor-full-02-classification.json): an incorrect `-nographics` wrapper selected NullGfxDevice, invalidating GPU depth/render checks. The wrapper was corrected and assertions retained. This failed invocation is not relabelled as a passing run.

After the source-wording/hash correction, `python -B -m unittest test_final_audio -v` in `Production/Tools` passed **2/2** existing tests: deterministic bounded finite-tail contacts and distinct sparse arrangements with preserved authored spacing. This checks generator behavior without regenerating or replacing source WAV files.

[Tools 06](Evidence/final-tools-06.log) records **175/175 PASS**, including the source-audio tests and eight focused audio-clock packaging tests. Those packaging tests mock encoding; they do not establish actual FFmpeg compatibility.

These cases cover actual Unity AudioSource transport, accepted mutation versus accepted no-op, separate Shake reversal and Stir movement, reading ducking, master multiplication, missing optional clips, same-session completion, completed Continue/rebind, failed publication and cancellation. The accepted-action group also checks the presentation-only released-tool signal. Disabled-component callback bodies are explicitly invoked by the EditMode fixture through reflection; those assertions do not certify native Player callback delivery.

**Final Player mix analysis and encoded-media validation remain pending.** The first full media attempt completed all 47 services but failed with 13 ACT2/3 shelf-overflow errors. Its retained [failed-attempt signal observations](Evidence/final-campaign-media-720-01/failed-attempt-signal-observations.json) report stereo 48 kHz PCM16, 1997.994667 s, peak 2593, 190755068 nonzero samples and zero clipped samples. These measurements describe that failed attempt only. Its 60 Hz requests yielded a 46.997 ms median gap and 1106.323 ms maximum gap; the final wall/audio clock difference was 17.505127 s. They do not establish useful high-rate contact synchronization or passing presentation evidence.

Build 03's replacement [720p Player result](Evidence/final-campaign-media-720-02/player-result.json) is **PASS**: 373 captures, 2652 checks, 47 services, 47 departures, 9 Known recipes, 7 memories and zero errors. The [process record](Evidence/final-campaign-media-720-02/process.json) reports exit 0, no timeout and 1990.5328699 s. The coordinating run confirmed the [1700-file pre-fix inventory](Evidence/final-campaign-media-720-02/inventory.json) unchanged. This is simulated production InputSystem evidence from the preserved Build 03, not native input or evidence for later source fixes. Its [immutable build manifest](Evidence/final-build-03-manifest.json), [binary archive](Evidence/final-build-03-preserved.zip) and [archive hash/file inventory](Evidence/final-build-03-preserved.json) preserve that identity.

Detailed movie frames use actual-render JPEG quality 95 with recorded `MotionFrame.image`; required stills remain lossless PNG. No missing frame is interpolated. The first actual audio-clock packaging attempt, [encoding-01](Evidence/final-campaign-media-720-02/encoding-01.log), rejected the clock: 585 adjacent pairs among 18289 recorded frames have equal buffered AudioRenderer timestamps, while wall timestamps are strictly increasing. The helper correction is underway; no encoded-media PASS is claimed. Actual clock alignment, contact sampling and final mix measurements remain pending. See [the runtime checkpoint](P11_P12_RUNTIME_CHECKPOINT.md).

The final review also found an ACT7 cleanup-glass leak into legacy completion. [Coordinated RED](Evidence/final-review-regressions-red-01.xml) records 4 cases, 1 PASS / 3 FAIL, including both actual ACT1/ACT6 legacy-content regressions. The two-line `Content.Ending`/gesture-trigger guard is implemented. Full Editor 04 was running without a result XML at the 2026-11-11 09:50:46 UTC checkpoint; no GREEN is claimed for that fix here. A newly added actual Player menu master/mute proof is scheduled after Build 04 and has not run at this checkpoint.

The available audio listening tool was unsupported; no subjective hearing assessment was performed. Earlier ACT1 mix evidence in [ACT1_AUDIO_CUE_MAP.md](ACT1_AUDIO_CUE_MAP.md) does not validate this final mix. Native-input/lifecycle evidence remains separate from simulated production InputSystem proof. Gate3 listening, balance, timbre, grounded contact, fatigue, musical/emotional fit and final pacing remain **HUMAN_SIGNOFF_REQUIRED**.

The source manifest records final in-room mix validation as pending. Its `loudnessMethod`, `selectionLimit` and `gate` distinguish source comparison from Player output and human review.

## Selected clips and triggers

The serialized final-scene GUIDs were resolved to the following WAV files. The nine retained cues are under `Unity/Assets/Surlhwa/SliceAssets/`; the three new selected cues are under `Unity/Assets/Surlhwa/FinalArt/Audio/`. Runtime behavior is in [CampaignAudio.cs](../Unity/Assets/Surlhwa/Presentation/CampaignAudio.cs), [SliceInputAdapter.cs](../Unity/Assets/Surlhwa/Presentation/SliceInputAdapter.cs), [CampaignController.cs](../Unity/Assets/Surlhwa/Presentation/CampaignController.cs), [CharacterView.cs](../Unity/Assets/Surlhwa/Presentation/CharacterView.cs) and [CampaignCastView.cs](../Unity/Assets/Surlhwa/Presentation/CampaignCastView.cs).

All five runtime sources are non-spatial (`spatialBlend=0`) and `playOnAwake=false`. Gains below are linear source volumes before the player's master multiplier, not perceived loudness. The controller's default master is `0.65`; a loaded user setting takes precedence.

| Field / selected WAV | File duration | Source / gain | Actual trigger and ordinary stop |
| --- | ---: | --- | --- |
| Room / `RoomTone.wav` | 24 s | room loop; 0.15 work, 0.09 reading, 0.055 quiet | Starts on initialization/reenable when present; continues as the room bed until component disable. |
| Winter / `WinterAir.wav` | 32 s | winter loop; 0.08 work, 0.065 reading, 0.045 quiet | Same lifecycle as room. |
| Pour / `Pour.wav` | 2.4 s | physical loop; 0.18 | Only a step accepting actual liquid with `Brew.FlowActive`; includes non-strain flows such as Float. Stops on a step without accepted liquid, release or cancellation. |
| Stir / `Stir.wav` | 3.2 s | physical loop; 0.18 | Only `AcceptedStirThisStep` with `Brew.StirActive`, derived from an increase in actual accepted degrees. Stationary movement stops the loop. |
| Strain / `Strain.wav` | 3.1 s | physical loop; 0.18 | Accepted actual liquid while `strain` or `shaker_strain` is active and flow remains active. Same stop rule as Pour. |
| Shake / `ShakeContact.wav` | 0.24 s | physical one-shot; 0.18 | An accepted increase in actual `Brew.ShakeReversals`, while tool `shake` and `Brew.ShakeActive` are true. Each newly accepted reversal restarts the short contact. Stationary/repeated refresh does not replay; its current tail may decay while still held. Release/cancellation stops it. |
| Ice / `Ice.wav` | 0.65 s | shared cue one-shot; 0.24 | Accepted `ice` command that changes actual Brew revision. Repeated already-present ice is silent. |
| Glass / `GlassSet.wav` | 0.32 s | shared cue one-shot; 0.24 | Other accepted mutating discrete commands: cherry/orange/zest garnish, gentle stir, lid toggle and dash. This is the current generic contact selection, not separate newly authored sounds for each action. UI-only `glass` is not this event. |
| Seat / `Seat.wav` | 0.4 s | shared cue one-shot; 0.24 | Controller observes a new session/slot/phase presentation boundary at `Intro`, subject to `Session.CanInteract`. |
| Entrance / `EntranceAir.wav` | 0.8 s | shared cue one-shot; 0.24 | Same boundary observation at `Arriving`, subject to `Session.CanInteract`. |
| Foot / `FootGroundedFinal.wav` | 0.52 s | shared cue one-shot; 0.24 | A newly observed even planted walk frame, with actor/art/render visibility and interaction available. Alternating cue pitches start at 1.02, then 0.98. |
| Music / `BarReturn.wav` | 26.5 s | music one-shot; 0.12 maximum | Same live session changes from incomplete to successfully published `State.ActComplete` while interaction is available. Starts once with the envelope below; stops naturally or immediately on interruption. |

The nine retained WAVs are PCM16, mono, 44.1 kHz. The new Foot and Shake WAVs are PCM16, mono, 48 kHz; Music is PCM16, stereo, 48 kHz. Source duration differs slightly in playback for the alternating foot pitches. New selected clips are imported by [CampaignFinalArtBuilder.cs](../Unity/Assets/Surlhwa/Editor/CampaignFinalArtBuilder.cs) as PCM, `DecompressOnLoad`, preloaded. The builder assigns Foot/Shake/Music only in the separate final scene; prior ACT1 source files and scene bindings remain available.

Discrete action acceptance alone is insufficient to emit a cue: the adapter first cancels held input, then captures Brew revision, executes the command, and emits only if the accepted command changes that revision. A cancellation revision cannot masquerade as a new ice/garnish/contact event. Rejected, unsupported and accepted no-op commands stay silent. Optional missing audio remains silent and does not alter drink composition, technique, persistence or publication.

Foot contact shares the exact `CharacterMotionSample` used for the visible pose. Walk sample index is `floor(travelSeconds * 6)`; planted frames are even and repeat observations are suppressed. This gives nominal contacts every one-third second during travel, without a separate foot timer or catch-up burst after a hitch. Missing walk art emits no invented contact. Arrival travel is 1.05 s after any authored pause; departure travel begins after the authored pause and ends at 1.48 s. Paired actors observe their own samples; coincident contacts produce one accent on the shared non-spatial source. Visual/contact synchronization still requires high-rate actual Player media review.

## Envelopes, master and cancellation

“Reading” is `Session.CurrentText != null`, covering all authored text rather than a short list of dialogue phases. “Quiet” takes precedence in Farewell, EndingReady, EndingEntered, EndingReading and Complete. There is no motif during the nine EndingEntered/EndingReading lines. Successful final completion can start the motif while the completed coda remains visible; completed Continue stays silent.

| Treatment | Exact runtime rule |
| --- | --- |
| Room duck / restore | Normalized envelope moves toward `1`, `0.09/0.15`, or `0.055/0.15` using `MoveTowards` at `3 * unscaledDeltaTime`. Output is `master * 0.15 * envelope`. |
| Winter duck / restore | Same rate, with targets `1`, `0.065/0.08`, or `0.045/0.08`. Output is `master * 0.08 * envelope`. |
| Duck timing | This is a fixed normalized rate, not a fixed-duration crossfade. From work level: room to reading about 0.133 s, room to quiet about 0.211 s; winter to reading about 0.063 s, winter to quiet about 0.146 s, subject to frame sampling. Restoration uses the same rate. |
| Music attack | Envelope starts at zero and moves to one at `0.08/0.12` per unscaled second: 1.5 s to full source gain. Output is `master * 0.12 * envelope`. The WAV supplies its own sparse note attacks and decay. |
| Master / mute | Each refresh directly multiplies every source by current master; the master itself is not smoothed. The controller refreshes audio during both session and menu updates. Zero therefore zeros all five source volumes on that refresh, including an existing music envelope, without restarting it. |
| Physical / cue interruption | No runtime release fade. Physical activity stops when no longer accepted or held; unavailable interaction additionally stops shared discrete cue tails. Session replacement stops physical, cue and music. |
| Modal / focus / unavailable session | `active && Session.CanInteract` must remain true for physical/cue activity and music. Otherwise those sources stop immediately. Ambience continues toward the phase/text mix targets; modal state alone does not select a new ambience target. |
| Component disable / reenable | Disable stops all five sources. Reenable restarts present ambience clips at master-scaled stored envelopes, baselines the current completion state and clears music. |
| Completed Continue / rebind | New session reference is observed as a baseline. A bound completed save never starts the motif; rebind also clears stale physical/cue/music state. No persisted audio flag is introduced. |
| Completion while unavailable | The observed completion baseline still advances. There is no delayed reward cue when interaction/enable returns. An interrupted or naturally finished completion cue does not restart. Failed publication does not create a completion edge. |

Ordinary release stops a physical tail/loop; it does not stop unrelated discrete cue tails while the session remains available. The presentation-only `ReleasedTool` signal supplies the workbench's brief return animation and creates no additional audio event, liquid or progress. No authored audio completion advances dialogue, service or saves.

## Original arrangement and source comparison

[author_final_audio.py](../Tools/author_final_audio.py) and the [source manifest](../SourceArt/FinalProduction/Audio/manifest.json) record deterministic synthesis with no external samples. `BarReturn` is the selected sparse five-note contour plus two much quieter lower resonances. `BarReturnSparse` is retained as an alternate with the same five notes, no lower resonances and a slower 0.022 s note attack. The selected arrangement uses a 0.014 s attack. These are source candidates within the quiet-bar direction; selection does not imply listening approval.

Exact selected note events follow. Gain is the generator's linear tone coefficient before equal-power panning, partials/reflections and the runtime music gain. Pan runs from -1 left to +1 right; MIDI names use C4=60.

| Onset (s) | MIDI / note | Gain | Pan | Role |
| ---: | --- | ---: | ---: | --- |
| 0.75 | 64 / E4 | 0.105 | -0.12 | Main contour |
| 1.12 | 52 / E3 | 0.022 | -0.03 | Quiet lower resonance |
| 4.10 | 59 / B3 | 0.085 | 0.08 | Main contour |
| 8.60 | 66 / F#4 | 0.073 | 0.12 | Main contour |
| 13.00 | 64 / E4 | 0.067 | -0.08 | Main contour |
| 13.38 | 47 / B2 | 0.016 | 0.03 | Quiet lower resonance |
| 18.60 | 62 / D4 | 0.048 | 0.04 | Main contour |

The five main attacks are separated by 3.35, 4.50, 4.40 and 5.60 s, leaving space for decay rather than a continuous pad. Each synthesized note has a seven-second support with a final 0.4 s taper. Partial frequency ratios / levels / exponential decay seconds are `(1, 1, 1.95)`, `(2.003, 0.22, 0.90)`, `(3.009, 0.055, 0.48)`, `(4.012, 0.018, 0.25)`. Two channel-swapped reflections occur at 0.037 s / 0.065 gain and 0.089 s / 0.035 gain. The last note and reflections decay before the 26.5 s file ends. There is no character assignment, theme-count gate or continuous scoring layer.

The foot comparison uses preserved ACT1 `FootstepFeltWood.wav` as control and two original 0.52 s body/sole candidates. Body uses filtered noise with 42/470 Hz body-band parameters, Roll 42/360 Hz; both add 180/1800 Hz sole-band noise and a falling low sinusoid. Body heel decay is 0.052 s with sole onset 0.038 s; Roll uses 0.070 s and 0.055 s. Both use the same final taper. This is an actual source-shape comparison, not only changing runtime gain on the inherited clip.

The generator constructs identical four-second sequences with contacts at 0.3, 1.0, 1.7 and 2.4 s, measures each using FFmpeg EBU R128 integrated loudness, and writes gain-matched audition files. The LUFS column below is the **unmatched** measured sequence value; the next column is the applied matching gain. No separately measured post-match LUFS is invented.

| Source | Measured sequence LUFS | Match gain | Matched sequence peak / RMS | Status |
| --- | ---: | ---: | --- | --- |
| ACT1 control | -24.8 | 0 dB | 0.584717 / 0.046256 | Preserved reference |
| FootGroundedBody | -28.9 | +4.1 dB | 0.478960 / 0.041284 | Selected as `FootGroundedFinal` after matching |
| FootGroundedRoll | -28.0 | +3.2 dB | 0.458772 / 0.046346 | Retained alternate |

`FootSequence-{Control,Body,Roll}.wav` and `FootMatched-{Control,Body,Roll}.wav` retain the comparison. `FootGroundedFinal` is Body multiplied by `10^(4.1/20)`, not a modification of the control. All three matched sequences report zero clipped samples. The selected standalone foot has peak 0.478960 / RMS 0.057251; ShakeContact 0.115443 / 0.011607; BarReturn 0.091047 / 0.017395. These manifest source metrics do not include simultaneous room/winter/cues, runtime gain, pitch, device resampling or the actual Player mix.

ShakeContact uses original seeded band-limited noise (700/5200 Hz parameters), three contacts at 0, 0.021 and 0.049 s with gains 0.19, 0.10 and 0.055, 0.0018 s attack, 0.024 s exponential decay, and a final taper ending by 0.22 s inside its 0.24 s file. Its runtime trigger is an accepted physical reversal, not a Stir loop or an autonomous shaking timer.

## Provenance and preservation

The six original candidate/selected source WAV hashes and generator hash were checked against the manifest; all matched. The three final-scene selected WAV bytes match their source hashes below. The preserved ACT1 control also matches its recorded hash. This is byte/provenance verification, not a new listening test.

| Item | SHA-256 |
| --- | --- |
| FootGroundedFinal.wav | `33323f6bd599d5836aad47957a688c8e5d81cf343d47ac6beb6cd81918772a85` |
| ShakeContact.wav | `1375a4a04be6024c53d0eb4e7e970a54f9699730dbc59b85e428d30e89b5697e` |
| BarReturn.wav | `8bdc433161ce9272963d03867c246b4bc8ae678c2826eefbf02fdc5973099275` |
| author_final_audio.py | `e38e8eb83483626bd7f7c967a1f3e3339d7318be021f31da70b9ffaa46133a32` |
| Protected ACT1 FootstepFeltWood.wav | `6bdffc91a8816662f452a9df81a5e32810a4db7f059cdca342fdeda5e63d7c2a` |

Retained cue sources are traced by [SliceAssets/adoption.json](../Unity/Assets/Surlhwa/SliceAssets/adoption.json). Each currently bound retained file's SHA-256 matches its adoption row. Paths below are the recorded inherited source paths under `Assets/_SURLHWA/Audio/PhaseO/`, copied into the Production SliceAssets directory without changing the WAV bytes:

| Bound retained file | Recorded inherited source |
| --- | --- |
| RoomTone.wav | `Ambience/RoomTone.wav` |
| WinterAir.wav | `Ambience/WinterAir.wav` |
| Pour.wav | `Bartending/Pour.wav` |
| Stir.wav | `Bartending/Stir.wav` |
| Strain.wav | `Bartending/Strain.wav` |
| Ice.wav | `Bartending/Ice.wav` |
| GlassSet.wav | `Bartending/GlassSet.wav` |
| Seat.wav | `Foley/Seat.wav` |
| EntranceAir.wav | `Foley/EntranceAir.wav` |

The adopted `Foley/FootLeft.wav`, earlier `FootGrounded.wav`, ACT1 `FootstepFeltWood.wav` and `FirstNightMotif.wav` remain historical sources/candidates; they are not the selected final-scene Foot or Music references. Their prior provenance remains in [audio-act1-drafts.json](Manifests/audio-act1-drafts.json), [act1-art-production-inventory.json](Manifests/act1-art-production-inventory.json) and the ACT1 cue map. Existing source tracking does not establish a new licence grant or blanket legal clearance for inherited audio. The no-external-samples statement applies to the new original synthesis described here; it does not replace inherited rights review.

P12 runtime lifecycle, controller menu gain and source-candidate prerequisites have passing Editor/tool evidence. The preserved Build 03 full-campaign Player run passes its recorded checks. Final mixed-audio analysis, encoded-media validation, the new Player menu proof and validation of subsequent source fixes remain debt; Gate3 human listening remains required before final audio approval.
