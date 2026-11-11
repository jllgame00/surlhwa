# Final candidate performance measurement

Prepared during P13 on 2026-11-11. This describes the measurement path, not a
completed benchmark or hardware acceptance. Actual results must name their
Player inventory, raw telemetry, host metadata and process outcome.

`run_campaign_proof.ps1 -Act 7 -FinalCandidate -Performance` launches a visible
Normal Windows Player window without `-batchmode`; other proof automation keeps
its Hidden launch. `expected-run.json` records the selected window style.
Motion/audio recording is mutually
exclusive. The regular screenshot proof still uses headless execution and its
frame samples are not normal rendering measurements. Unity documents batch mode
as headless execution in its [Player argument reference](https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/command-line-arguments/player).

The development candidate already supports frame timing collection; no inherited
ProjectSettings change is needed. Availability and actual positive GPU samples
are still checked at runtime. See Unity's [frame timing guidance](https://docs.unity.com/en-us/engine/6000.3/manual/analysis/graphics-performance-profiling/profile-rendering/frame-timing-manager/enable).

The opt-in `CampaignPerformanceProbe` observes the normal camera. The proof's
per-frame offscreen render and the separate 10 Hz evidence observer are disabled
in this mode. Individual required stills render once inside marked capture
intervals. Checkpoint-copy I/O is also marked; actual gameplay save operations
remain unchanged. Neither observer writes campaign state.

Raw rows retain wall-frame intervals, Unity delta, available main-thread/GC/memory
counters, delayed CPU/GPU timings, sampling overhead, phase, reading position,
method, tool, paired guests, memory count, modal, settings, focus and resolution.
Missing counters remain unavailable rather than becoming zero milliseconds or
zero bytes. FrameTiming timestamps are deduplicated; delayed timing is attributed
to a workload only after eight stable frames. The fixed 216,000-row buffer never
overwrites earlier data; exhaustion is reported as incomplete evidence.

The summary preserves raw counts and uses nearest-rank P50/P95/P99/worst values.
It excludes the first five seconds and explicitly marked capture intervals from
one frame before the operation through eight frames after it. It does not remove
ordinary stalls, GC or save activity merely because they are slow. Any save that
overlaps a capture exclusion cannot establish a separate save-duration bound.
Probe buffer memory and CPU overhead are disclosed, not silently subtracted.
The proof also retains its full-resolution RenderTexture for its entire
lifetime, even though that texture is rendered only during marked still
captures. Memory measurements include that proof surface as well as the fixed
telemetry buffer and simulated-input harness. They are not measurements of a
probe-free shipping build.

Before each timed hold, the existing proof coroutine labels
`awaiting-focus:<scenario>` and waits up to sixty seconds for both
`Application.isFocused` and the session's `Focused` value. A timeout fails the
proof with the scenario name. It does not activate/refocus the window or write
session focus. The first acquired hold also records a one-time five-second
`initial-settling` interval before starting its requested timer. This clears the
initial probe warmup without disguising the wait as measured steady work.
The guard deliberately does not use `CanInteract`: the Codex hold has an open
modal. Later focus loss, including during settling, remains in raw telemetry;
the timed hold is not silently restarted or extended.

The full-route protocol includes 62-second holds in the initial zero-memory
request, the first one-memory request, the paired request and the seven-memory
coda; ten seconds with each of the four distinct prepared methods; twenty Codex
open/close cycles; and a ten-second large Korean Codex hold. These are planned
workloads, not completed measurements. The extra two seconds provide margin
for the capture guard, but actual retained coverage must still be checked.
Normal progression supplies arrivals/departures, paired guests,
discovery, all ACT boundaries and actual saves. Runs at 720p and 1080p must be
identified separately; an incomplete route cannot be presented as full coverage.

The summary exposes all eligible measurements and a separate foreground-only
aggregate. A run with no focused eligible samples is `INCOMPLETE`; batch-mode
evidence is also rejected. Delayed GPU/CPU timing needs eight stable context
frames before inclusion in the foreground aggregate, so timing from just before
focus regain is not called foreground work. Inspect each named focused
workload's `steadySeconds` for at least sixty contiguous seconds where required.
`foregroundHasSixtySecondSteadyWindow` means that at least one workload meets
that duration; it does not certify every room scenario. Background samples stay
available in the all-eligible results and raw rows.

Host metadata records CPU, OS, physical memory, GPU driver, storage and the
existing power scheme without changing them. Unity's graphics API string is not
treated as a driver version. Foreground/focus, frame cap, VSync, development-build
overhead and counter limitations remain part of the result. This named-device
baseline does not establish a minimum supported PC specification.

Performance input remains simulated production InputSystem input. Native
keyboard/pointer certification and subjective responsiveness require their
separate evidence. The summary intentionally never grants hardware or owner
acceptance by itself.

Status after build03 preparation: dedicated performance has **not run**.
[The current build manifest](Manifests/final-production-build.json) and
[build03 log](Evidence/final-build-03.log) identify the candidate prepared for
the next actual run. The graphics-enabled Editor regression is separate
evidence, not a Player performance result. See the evolving
[Gate 3 draft](GATE3_RC_REVIEW_DRAFT.md) for its verification chronology and
remaining native/media work.
