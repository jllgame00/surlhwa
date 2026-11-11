# P11/P12 runtime checkpoint

Recorded 2026-11-11 09:50:46 UTC after `final-campaign-media-720-02` completed and while Full Editor 04 was running. This is an interim technical checkpoint, not final Production closure or human approval. It supplements the [implementation plan](P11_P12_IMPLEMENTATION_PLAN.md), [audio cue map](FINAL_AUDIO_CUE_MAP.md) and [performance method](FINAL_PERFORMANCE_METHOD.md). No Unity, Player, encoding or tests were launched for this documentation update.

## Verified technical evidence

| Evidence | Observed result | Scope and limit |
| --- | --- | --- |
| [Full Editor 03 XML](Evidence/final-editor-full-03.xml) / [log](Evidence/final-editor-full-03.log) | **859/859 PASS**, zero failed/skipped; coordinating run observed exit 0. Log identifies NVIDIA GeForce RTX 3070. | Editor technical validation with graphics. Includes 23 CampaignAudio, 25 accepted-action and 2 controller menu-audio cases: **50/50**. Does not certify native input or subjective feel. |
| [Tools 06](Evidence/final-tools-06.log) | **175/175 PASS** in 4.214 s. | Existing aggregate tool suite, including 2 original-audio tests and 8 packaging tests. Encoder calls are mocked in the packaging tests. |
| [Build 03 log](Evidence/final-build-03.log) / [preserved build manifest](Evidence/final-build-03-manifest.json) | **Succeeded**, 193274993 bytes, 29.4628528 s, 0 errors / 0 warnings, Unity 6000.3.7f1. | Separate `Bar_Q1_Final` Windows Development Player. The immutable manifest and archived binary identify the pre-review-fix build used for the passing media run below. |

The menu regression followed meaningful RED: [final-shelf-menu-red-01.xml](Evidence/final-shelf-menu-red-01.xml) observed initial room gain 0 instead of 0.0975 and retained nonzero source gains after menu mute. The existing controller now calls `Audio.Refresh(Input,false)` in its no-session update branch. Editor GREEN verifies initial projection, returned-menu mute/restore and no ambience restart or music replay. It adds no save field or second audio owner. The newly added actual Player menu master/mute proof has not run at this checkpoint; it is scheduled after Build 04.

The earlier [Full Editor 02 XML](Evidence/final-editor-full-02.xml) remains **859 total / 846 PASS / 13 FAIL**. Its [classification](Evidence/final-editor-full-02-classification.json) is `VALIDATION_BLOCKED_BY_ENVIRONMENT`: the wrapper incorrectly used `-nographics` and selected NullGfxDevice. Twelve GPU depth checks saw zero visible pixels and one shipped-font test reported RenderTexture creation failure. The graphics-capable wrapper was restored without weakening assertions; Full Editor 03 supplies the subsequent passing result. This was not a licensing failure and the earlier run is not retroactively PASS.

## Retained failed Player attempt

[final-campaign-media-720-01](Evidence/final-campaign-media-720-01/player-result.json) reached all **47 services** but remained **FAIL** with **13 ACT2/3 shelf text-overflow errors**. Its files are retained. The [signal-observation record](Evidence/final-campaign-media-720-01/failed-attempt-signal-observations.json) explicitly says `FAILED_PLAYER_ATTEMPT_SIGNAL_OBSERVATIONS_ONLY`.

| Measurement from that failed attempt | Recorded value |
| --- | ---: |
| Actual recorded movie frames | 13060 |
| Requested-60-Hz frame intervals | 4749 |
| Median / maximum requested-60 interval | 0.0469970703125 s / 1.1063232421875 s |
| Wall minus audio clock, minimum / maximum / final | 0.04374504089355469 s / 17.8153076171875 s / 17.505126953125 s |
| Actual AudioRenderer PCM16 mix | 2 channels, 48000 Hz, 1997.9946666666667 s |
| Peak / clipped / nonzero samples | 2593 / 0 / 190755068 |

Zero clipped samples does not repair the overflow failure or approve the mix. Requested 60 Hz is not achieved 60 Hz; the approximately 47 ms median gap does not establish the plan's useful contact sampling of at least 30 actual frames/sec. There is no frame-accurate contact or subjective listening claim. A wall-clock-only mux would misrepresent the observed audio timing divergence. The packager continues to reject failed or incomplete Player evidence.

## Replacement capture and packaging

Build 03's [replacement run request](Evidence/final-campaign-media-720-02/expected-run.json) specifies ACT7, final candidate, a fresh open-route campaign, 1280x720, detailed media and isolated save/history paths. The finalized [Player result](Evidence/final-campaign-media-720-02/player-result.json) is **PASS** with **373 captures / 2652 checks / zero errors**, **47 services / 47 departures / 9 Known recipes / 7 memories**. Its [process record](Evidence/final-campaign-media-720-02/process.json) reports **exit 0**, no timeout, **1990.5328699 s** and maximum working set **645939200 bytes**. This is a capture-heavy run, not the normal-camera performance benchmark.

The coordinating run confirmed the [1700-file pre-fix inventory](Evidence/final-campaign-media-720-02/inventory.json) unchanged. Build 03 is preserved in [final-build-03-preserved.zip](Evidence/final-build-03-preserved.zip), with [archive/file hashes](Evidence/final-build-03-preserved.json) and its [immutable build manifest](Evidence/final-build-03-manifest.json). Archive SHA-256 is `9111ebefb825047112c014cf853f8f7201441678f1364afbacc6d4957ab09a10`; the archive record contains 284 files. These artifacts identify the exact pre-review-fix build. Passing this run does not validate the later legacy-ending guard or newly added menu proof. Its declared tier is simulated production InputSystem in the built Player, explicitly not native OS input.

Detailed movie frames now encode the actual rendered image as JPEG quality 95 and record its safe relative path in `MotionFrame.image`. Required proof stills remain lossless PNG; ordinary non-detailed 5 Hz movie capture remains PNG. This change addresses measured PNG encoding cost. It does not create frames, interpolate motion or justify a nominal frame-rate claim. Actual recorded timestamps must demonstrate useful contact intervals in the new run.

The existing [media packager](../Tools/render_act1_media.py) adds an explicit `--audio-clock` option. Its default wall-clock behavior remains available for older evidence. The opt-in mode:

- requires the existing passing Player/process gate;
- joins every frame ID to its recorded audio timestamp and validates finite clocks against the WAV; the first implementation incorrectly required strictly increasing audio timestamps, as recorded below;
- accepts only matching relative `frames/NNNNN.jpg` or `.png` files inside the frame directory, with legacy PNG fallback when `image` is absent;
- uses original recorded images and AudioRenderer timing, including a nonzero initial offset, with variable frame-rate output;
- maps selected clip-window boundaries piecewise between recorded wall/audio clock pairs and retains that mapping in metadata.

The [eight focused tests](../Tools/test_act1_media.py) cover drift mapping, default compatibility, initial offset, JPEG/PNG filenames, missing/duplicate/reordered frame IDs, malformed or out-of-bounds clocks, unsafe/missing images, capture bounds and failed/incomplete runs. They mock FFmpeg. The first actual packaging attempt, [encoding-01](Evidence/final-campaign-media-720-02/encoding-01.log), failed with `Invalid audio-clock evidence: non-finite, negative or non-increasing clock`. Inspection of 18289 motion records found 585 adjacent equal buffered AudioRenderer timestamps; wall timestamps are strictly increasing. The helper correction is underway. No encoded-media PASS is claimed: actual encoder compatibility, measured audio/video alignment and useful contact sampling remain **VALIDATION_DEBT**. No failed-run media is promoted by the option.

## Review regression awaiting current validation

R01 is a real legacy-content regression: the ACT7 cleanup glass appeared at Complete in actual ACT1 and ACT6 content whose `Ending` is null. [Coordinated RED](Evidence/final-review-regressions-red-01.xml) records **4 total / 1 PASS / 3 FAIL**; both `LegacyCompletionAndContinueDoNotCreateTheActSevenCleanupProp` cases (ACT1 and ACT6) failed on that unwanted prop. They exercise completion and completed Continue without changing domain state or save authority.

The minimal two-line [CampaignCharacterArt.cs](../Unity/Assets/Surlhwa/Presentation/CampaignCharacterArt.cs) fix now requires `Content.Ending != null` for ending presentation and gates the wipe trigger on that same ending condition. Full Editor 04 was **RUNNING**, with no result XML at this checkpoint. Its GREEN, Build 04 and current-build Player validation are pending. The passing Build 03 artifacts remain evidence for their own earlier binary; they are not relabelled as proof of this fix.

## Remaining boundaries

- **VALIDATION_DEBT:** correct the audio-clock helper, package the passing Build 03 media and inspect actual achieved contact sampling, clock alignment, mix peaks/RMS/DC, loop/tail behavior and relevant release/modal/focus/mute/Continue boundaries. Complete Full Editor 04, Build 04 and the new actual Player menu master/mute proof before claiming current-build validation. A passing Editor fixture is not native Player lifecycle delivery.
- **VALIDATION_DEBT:** retain normal-camera performance runs separately from capture-heavy media. The proof now includes bounded early, one-memory and paired request holds, prepared-method holds and the seven-memory coda; no benchmark or hardware acceptance is claimed here.
- **VALIDATION_DEBT:** changed pointer/keyboard/focus/menu boundaries require their own actual OS-input evidence. Simulated production InputSystem commands are not native certification.
- **HUMAN_SIGNOFF_REQUIRED:** Gate3 owner review of bartending and animation feel, dialogue pacing, composition/readability, audio timbre/balance, musical placement, emotional impact and overall pacing. The audio listening tool was unsupported; no subjective listening PASS was produced.

The proposed temporary product-name override was removed before use in a build, preserving the existing default save/history path. The platform product name remains `Surlhwa Spatial Proof`; this legacy name is intentional compatibility preservation at this checkpoint, not a save migration. No stable content ID, recipe knowledge authority or save format is changed by the presentation proof or media packaging.
