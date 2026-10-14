# ACT 1 local performance baseline

Scope: the current ACT 1 Development Player, all four methods, six-night/17-encounter playthrough, Korean reading and Codex, and the first earned memory. This is a local instrumented baseline, not release performance acceptance. Native input closure remains separate.

## Named environment

- Windows 11 Home 10.0.26200; Intel i7-12700KF (12 cores / 20 logical processors); 33,363,376 KB visible RAM; High Performance power plan.
- Actual Player renderer: NVIDIA RTX 3070, 8,018 MB VRAM, driver 32.0.15.9186, Direct3D 11, non-threaded graphics-device mode. The Windows CIM AdapterRAM field truncates this card to about 4 GB; it is not used as the VRAM claim.
- Production workspace D: is disk 0, ST2000DM008-2UB102, SATA, 2,000,398,934,016 bytes. The separate Samsung SSD is not the workspace disk. Evidence/save/PNG IO therefore uses the HDD.
- Unity 6000.3.7f1. Controller frame target 60; Standalone quality index 5 configures VSync 0. These are source/settings values, not measured display synchronization.
- Source/Player inventories contain 966 files. Each run retains the exact source, content, tuning, build hashes, command line and before/after inventory verification. Build executable hash alone is not used as freshness proof.

Hardware records: [host](Manifests/gate2-hardware.json), [disk inventory](Manifests/gate2-storage.json), [D: mapping](Manifests/gate2-workspace-storage.json). Current measurements are also emitted in [the review manifest](Manifests/gate2-review.json).

## Measurement boundaries

Both runs start a fresh process and empty isolated campaign namespace. Prior filesystem/driver caches are not cleared, so these are process-cold launches, not cold-machine trials. The Editor and other proof Players are kept out of the run. OS scheduling and unrelated desktop applications are not controlled.

`CampaignPlayerProof` records `Time.unscaledDeltaTime` once per frame while explicitly submitting an offscreen camera/UI render. These measurements include rendering, readonly observation publication, save IO and capture costs. The 720p media run also records PNG frames at approximately 5 fps and captures Unity audio. The 1080p non-media run still performs offscreen renders and 54 checkpoint PNG captures; it is not an uninstrumented gameplay benchmark. Working-set peaks are sampled every 500 ms and are not exact allocation peaks. No separate main-thread/GPU profiler timing or save transaction latency is claimed.

Full-playthrough distributions replace the initial 60-second steady / ten-repeat transient protocol for this bounded milestone because the aim is to retain representative methods, reading, learning, save/departure and memory in one actual campaign trace. This deviation does not certify any of those investigation budgets. The full P13 protocol, including paired actors, seven memory groups and 20 lifecycle cycles, remains later authorized expansion; those systems are outside ACT 1.

## Results

Final 720p recorded run: `gate2-player-open-720-04`, PASS, 167 checks, 54 stills, 17 services and departures, six Known recipes, one earned glowing-star memory, process exit 0, unchanged 966-file inventory. 35,086 frame samples; P50/P95/P99/worst = 16.67 / 22.31 / 27.83 / 1209.88 ms. Duration 630.16 s; peak sampled working set 575,315,968 bytes (548.66 MiB).

Final 1080p non-media run: `gate2-player-open-1080-02`, PASS, 167 checks, 54 stills, 17 services and departures, six Known recipes, one earned glowing-star memory, exit 0, unchanged 966-file inventory. 27,457 frame samples; P50/P95/P99/worst = 16.67 / 16.67 / 16.82 / 487.61 ms. Duration 463.63 s; peak sampled working set 587,517,952 bytes (560.30 MiB). Earlier failed or superseded runs are excluded from current evidence.

Architecture investigation thresholds remain 60 fps / P95 16.7 ms, main/GPU 12 ms each, working memory 1.5 GB, save main-thread 4 ms / transaction 100 ms, warm transition 250 ms and cold interactive 5 s. The recorded-run P95 exceeds 16.7 ms and includes capture/probe overhead; this is explicitly not a demonstrated gameplay-frame-budget pass. The 1080p non-media P95 is within 16.7 ms on this device, while still including instrumentation. The whole-run maxima cannot identify a gameplay stall without a profiler trace. Memory remains below the investigation ceiling in both runs. Main/GPU, transient, cold-machine and save latency budgets remain unmeasured. No speculative pooling, rendering architecture change or performance package is justified by these instrumented distributions alone.

## Recorded media limits

The passing 720p source has 2,981 rendered frames, final frame timestamp 627.8496 s, and 625.4933 s of stereo 48 kHz Unity audio. Peak PCM16 is 1,917; 59,687,996 nonzero samples; zero clipped samples. The muxed video uses recorded frame timestamps and the original captured mix. The approximately 2.36-second duration difference is retained and disclosed: this is a presentation/listening record, not sample-synchronized foot-contact or input-latency proof. Raw PNG timestamps and WAV are retained. No final loudness, timbre, footstep weight or composition approval follows from a non-clipping result.
