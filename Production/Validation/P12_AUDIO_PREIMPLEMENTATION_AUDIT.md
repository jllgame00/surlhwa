# P12 audio preimplementation audit

Date: 2026-10-31. Baseline: `2541b62` plus the in-progress ACT6 integration.
This is a read-only audit recorded for the post-ACT7 presentation pass. It does
not modify the current audio, content, scene or runtime inventory. No tests,
builds, audio measurements or listening audition were performed for this audit.
No final audio or music approval is claimed.

## Authority and scope

- Final Canon `15_AUDIO_DIRECTION.md`: AUD-01/02 preserve quiet indoor air,
  winter outside, physical bartending sounds, intentional silence and rare
  musical motifs. AUD-03/04 require action correspondence, interruption cleanup,
  consistent volume settings and progression independent of audio availability.
  AUD-D01 distinguishes signal/lifecycle evidence from human listening approval.
- Approved [production expansion plan](PRODUCTION_EXPANSION_PLAN.md), P11,
  P12 and P13: contact timing precedes final footstep work; P12 covers original
  compositions, provenance, actual mix and listening evidence; affected lifecycle,
  contact and performance evidence belongs in P13.
- [Presentation debt](PRESENTATION_DEBT.md): PD07 footsteps, PD08 mix and PD09
  composition remain open for the named Gate3 ruling. Approved quiet direction
  does not prohibit composed music or require continuous background music,
  character themes, voice production, a fixed instrument palette or track count.
- Final Canon `54_HAMLET.md` and `10_WORLD_AND_BAR_MEMORY.md`: Hamlet permits
  restrained local quiet; old global effects are not mandatory. Faust's subtle
  environmental sound ideas remain explicitly deferred presentation work.

Canon was read from the read-only source root
`C:/Users/surlw/.codex/.chatgpt-projects/g-p-6a8cfbb439748191ac8fac094112d31b/SURLHWA_CANON`.

The smallest implementation extends the existing `CampaignAudio`, input and
contact presentation owners. It needs no second audio manager, new progression
state, save format, reward cue system or speculative adaptive-music framework.

## Current implementation and evidence

[`CampaignAudio.cs`](../Unity/Assets/Surlhwa/Presentation/CampaignAudio.cs)
uses five sources: room, winter, physical action, one-shots and music. Room and
winter are looped; the physical source follows accepted input activity. All
sources stop on component disable. Musical playback is suppressed on initial
Continue into completion and stopped for modal/focus/session interruption.

The current reading mix covers Intro, Reaction, Farewell and ChoiceResponse.
Farewell receives the quietest bed. This remains an ACT1 candidate carried into
later scenes, rather than a final campaign-wide mix.

[`CharacterView.cs`](../Unity/Assets/Surlhwa/Presentation/CharacterView.cs)
projects poses and walk frames without owning audio or story progression.
[`CampaignController.cs`](../Unity/Assets/Surlhwa/Presentation/CampaignController.cs)
and [`CampaignCastView.cs`](../Unity/Assets/Surlhwa/Presentation/CampaignCastView.cs)
own solo/paired contact cue timing. Current walking uses four-frame pose sets and
contact cadence derived from the presentation clock; seated waiting stays quiet.

The active footstep is `Act1Assets/ProductionArt/FootstepFeltWood.wav`, an original
0.55-second deterministic felt/wood synthesis. The runtime alternates pitch
between .98 and 1.02. Its [candidate manifest](Manifests/art-footstep-candidate.json)
records source peak/RMS, zero clipped samples and SHA256
`6bdffc91a8816662f452a9df81a5e32810a4db7f059cdca342fdeda5e63d7c2a`.
The source candidate matches the prior cue's RMS for audition; that is not
perceptual-loudness equivalence or final weight/loudness approval.

`Act1Assets/FirstNightMotif.wav` is an original 22-second procedural draft with
five attacks. Its source MIDI events are 64, 59, 66, 64 and 62. It plays at the
current milestone's `Complete` state, not at every memory acquisition:
`CampaignAudio.cs:24-27` reads `ActComplete`, whose current definition in
[`CampaignProgress.cs`](../Unity/Assets/Surlhwa/Domain/CampaignProgress.cs):31
requires `Phase == Complete` and MEM01. It is the only composed candidate.

Source and provenance records:

- [`author_act1_audio.py`](../Tools/author_act1_audio.py): original footstep and
  motif synthesis; [draft manifest](Manifests/audio-act1-drafts.json).
- [`author_act1_footstep.py`](../Tools/author_act1_footstep.py): original felt/wood
  candidate, preserving its predecessor for comparison.
- [Current linked inventory](Manifests/act1-art-production-inventory.json) and
  [ACT1 audio cue map](ACT1_AUDIO_CUE_MAP.md).
- [`SliceAssets/adoption.json`](../Unity/Assets/Surlhwa/SliceAssets/adoption.json):
  inherited ambience/tool/entrance sources. Their earlier provenance must remain
  distinct from newly authored material.

Existing integration checks largely establish clip bindings and capture
validity. The [ACT1 art review](ACT1_ART_GATE2_REVIEW.md) records stereo 48 kHz
Player audio with zero clipped PCM16 samples, but approximately 5 Hz video cannot
certify foot-contact synchronization. These are historical results, not new P12
measurements or final listening acceptance.

## Concrete follow-ups

| Finding | Current source evidence | Smallest correction |
| --- | --- | --- |
| Paired departure emits a footstep before walking starts. | `CampaignCastView.cs:61-63` emits contact 0 immediately on Departing; `Present` at lines 87-92 holds actors still for .12/.18 seconds. | Derive the cue from the same actual walking/contact transition used for presentation, including missing-actor fallback. Audio must not become the movement clock. |
| No-op actions emit contact sounds. | `SliceInputAdapter.cs:67-76` publishes `AcceptedCue` whenever `ExecuteBrew` returns true. Repeated AddIce and GentleStir on an empty serving container return without mutation at `Brewing.cs:229-233`. | Publish contact cues only for an actual accepted domain action/revision. Preserve shared Slice behavior through focused regressions. |
| Shake uses repeatedly interrupted Stir audio. | `SliceInputAdapter.cs:137` sets the Stir activity flag only on accepted Shake reversals; `CampaignAudio.cs:29-31` starts the Stir clip and stops it on the next inactive frame. | Use a restrained original shaker-contact cue driven by actual accepted motion, retaining immediate release/modal/focus cleanup. Do not grant technique from sound or animation. |
| Reading treatment omits authored text states. | `CampaignAudio.cs:20` omits Request, Choice and Retry; `CampaignSession.cs:35-38` provides `CurrentText` in those states. | Apply the established quieter reading treatment consistently, with short presentation fades. Preserve deliberate silence. |
| Master scaling is coupled to musical fade. | `CampaignAudio.cs:26-27` ramps actual source volume toward `master * .12`; a direct change to zero during playback can leave a short audible ramp. | Fade a normalized envelope, then multiply by master. The current settings modal stops the motif first, so normal UI mute masks this source-level contract gap; it is not an observed Player mute failure. |

## Bounded proposed sequence

1. **Settle P11 contact timing, then repair lifecycle behavior.** Correct the
   reachable contact/no-op issues above. Keep ordinary seated waiting silent.
   Hamlet may receive a restrained local reduction; do not introduce global
   freezing, room-wide distortion or new narrative triggers. Faust's optional
   environmental audio remains separate unless deliberately included in the
   bounded presentation scope.

2. **Produce a revised footstep comparison and deliberate original music
   arrangement.** Retain historical WAVs. Compare identical walking passages at
   matched audition loudness, then assess final mix loudness separately. Extend
   the current motif's original source vocabulary if useful; there is no mandate
   for per-character themes or continuous music. After ACT7, align musical onset
   with the authoritative ending-reading boundary, avoiding a completion cue
   before the ending has actually been read. Document purpose, onset, duration,
   fade, interruption and silence boundaries in the cue map.

3. **Record exact provenance.** Preserve composition events/notes, synthesis
   parameters and seed, generator revision, rendered WAV hash, cue/scene binding
   and inherited-source lineage. Existing procedural originals use no imported
   recordings or third-party samples. Do not relabel inherited references as new
   compositions or infer blanket rights clearance from internal authorship.

4. **Verify actual output and lifecycle.** Cover accepted/rejected actions,
   stationary tools, repeated ice, interrupted motion, solo/paired departure,
   modal/focus loss, disposal, missing clips, mute/settings persistence and
   completed Continue without musical replay. Missing device/capture capability
   must be distinguished from gameplay failure. Measure source and combined mix
   clipping, peaks, DC, loop seams, tail truncation and concurrency. Use metrics
   as evidence, not arbitrary owner-approved EQ or loudness targets.

5. **Package actual listening and P13 evidence.** Record Player stereo mix plus
   synchronized higher-rate contact footage for arrival/departure. Include
   dialogue, all four methods, Hamlet stillness, farewell, accumulated memories
   and the ending, with repeat-fatigue listening. Reuse the existing
   [`CampaignPlayerProof`](../Unity/Assets/Surlhwa/Presentation/CampaignPlayerProof.cs)
   `AudioRenderer` capture and
   [`render_act1_media.py`](../Tools/render_act1_media.py) packaging where suitable;
   preserve exact build/source identities and distinguish simulated input from
   native evidence. Measure changed audio costs under P13 rather than claiming
   performance from capture runs. The historical 5 Hz media is insufficient for
   contact certification.

## Acceptance boundary

This document records actionable audit findings and a proposed implementation
sequence only. No tests, builds, source-cue measurements, Player recordings or
listening were performed for it. Objective lifecycle defects should be corrected
within the authorized production pass. PD07-PD09 remain open until the named
Gate3 listening ruling; signal checks and technical PASS cannot substitute for
human judgment of weight, naturalness, fatigue, silence or musical pacing.
