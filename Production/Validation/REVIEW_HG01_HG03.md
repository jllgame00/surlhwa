# HG01–HG03 owner review

Verdict: **HYBRID_SPATIAL_PROOF_READY_FOR_OWNER_REVIEW**. HG01/HG02/HG03 are each **READY_FOR_OWNER_REVIEW**, with artistic acceptance **OWNER_REVIEW_PENDING**. No camera winner is selected. This is a spatial fixture, with no service success, visit completion or memory awarded.

Open [the comparison index](review.html). Open the individual original PNG links at native size when judging pixel readability; scaled previews and clips do not replace them. Raw outputs are local ignored evidence: preserve this folder if sharing or moving the checkout.

## What to judge

| Gate | Question | Evidence | Provisional / tradeoff |
|---|---|---|---|
| HG01 — camera/depth | Which projection gives the desired intimate room and readable spatial relationships? | Empty, Composition, Standing, motion clips | A keeps equal world-size at depth; B changes scale with depth. Same transform, room, light, art and anchor staging. Neither is an artistic winner yet. |
| HG02 — coherence | Do the pixel characters belong in this restrained 3D winter room? Does charcoal remain anonymous/readable while Quixote stays distinct? | OrdinarySeated, QuixoteSeated, Composition | Low-detail primitive environment; unchanged reference art, point sampling and shared warm zone tint. Wood/window/light palette needs owner judgment. |
| HG03 — grounding/scale | Do feet meet the floor, seated hips meet the stool, and bodies belong on the correct side of the counter? | GroundingInspection, Standing, clips, counter diagnostic | Fixed 18° planes, 44 source px/world unit, cutout depth and contact patches. Walk/seat pose swaps are limited reference animation; no final hand animation. |

## Exact provisional configurations

All spatial/render values are **ARCH_SLICE_PROVISIONAL**. Normal A and B position: **(0.000000, 6.453290, -15.467961)**, rotation **(18°, 0°, 0°)**. Both look along the same fixed direction toward **(0, 1.2, 0.7)** at distance **17 m**. A orthographic half-height **3.3 m**; B vertical FOV **21.970995°**. Near/far **0.1 / 60 m**. Aspect **16:9**, native render scale **1**, Linear color, URP Universal Renderer **Forward**, Depth Priming disabled, HDR/MSAA/post-processing/AA disabled. No free camera or runtime billboard.

GroundingInspection uses a separately authored fixed lens: target **(-0.4, 0.95, 0.1)**, same angle/distance, position **(-0.4, 6.203289, -16.067961)**, A half-height **1.85**, B FOV **12.421379°**. It deliberately clips room edges to inspect contact. Normal room framing is preserved in the other fixtures.

Floor top **y=0**. Seated actor roots/feet: ordinary **(-0.973, 0, -1.15)**; Quixote **(-1.109, 0, -1.15)**. Bartender **(0.3, 0, 1.3)**. Seat top **26/44 × cos(18°) = 0.561988 m**; stool center **(-0.7, 0.501988, -0.69)**. Seat contact is at the front of the supporting slab; its geometry sits behind the opaque torso. Feet and hip markers are separate from transparent canvas corners. Source pivots: bartender **(17,0)**, ordinary standing **(17.5,0)** / seated **(38,15)**, Quixote standing **(20,0)** / seated **(28,15)** in source pixel coordinates from bottom left. Walk pivots **(48,1/0/0/0)** compensate source transparent margins. Source art is unchanged.

Material: URP Unlit sprite, opaque alpha clip **0.4**, depth write/test enabled, two-sided plane, no long cast or received character shadows. Shared zone tint RGBA **(0.92,0.84,0.74,1)**. Contacts are flat 24-segment ellipses **0.76 × 0.34 m**, at **y=0.012**, dark tint alpha **0.32**. They support contact; they are not character silhouette shadows. Environment uses Simple Lit, smoothness **0**, restrained shared palette. One directional key RGB **(1,0.83,0.65)**, intensity **0.85**, rotation **(42,-28,0)**, shadow strength **0.45**, bias **0.03**, map **1024**, distance **18 m**. Two warm non-shadow point fills RGB **(1,0.68,0.38)**, intensity **2.1**, range **4.2**; one cool window fill RGB **(0.45,0.65,1)**, intensity **1.6**, range **3.2**. Ambient RGB **(0.34,0.32,0.30)**. No bloom, DOF, motion blur or global pixelation.

## Original capture matrix

| Fixture | Native originals |
|---|---|
| Empty | [OrthographicA/1280](Evidence/captures-reviewed/OrthographicA_Empty_1280x720.png) · [OrthographicA/1920](Evidence/captures-reviewed/OrthographicA_Empty_1920x1080.png) · [PerspectiveB/1280](Evidence/captures-reviewed/PerspectiveB_Empty_1280x720.png) · [PerspectiveB/1920](Evidence/captures-reviewed/PerspectiveB_Empty_1920x1080.png) |
| OrdinarySeated | [OrthographicA/1280](Evidence/captures-reviewed/OrthographicA_OrdinarySeated_1280x720.png) · [OrthographicA/1920](Evidence/captures-reviewed/OrthographicA_OrdinarySeated_1920x1080.png) · [PerspectiveB/1280](Evidence/captures-reviewed/PerspectiveB_OrdinarySeated_1280x720.png) · [PerspectiveB/1920](Evidence/captures-reviewed/PerspectiveB_OrdinarySeated_1920x1080.png) |
| QuixoteSeated | [OrthographicA/1280](Evidence/captures-reviewed/OrthographicA_QuixoteSeated_1280x720.png) · [OrthographicA/1920](Evidence/captures-reviewed/OrthographicA_QuixoteSeated_1920x1080.png) · [PerspectiveB/1280](Evidence/captures-reviewed/PerspectiveB_QuixoteSeated_1280x720.png) · [PerspectiveB/1920](Evidence/captures-reviewed/PerspectiveB_QuixoteSeated_1920x1080.png) |
| Composition | [OrthographicA/1280](Evidence/captures-reviewed/OrthographicA_Composition_1280x720.png) · [OrthographicA/1920](Evidence/captures-reviewed/OrthographicA_Composition_1920x1080.png) · [PerspectiveB/1280](Evidence/captures-reviewed/PerspectiveB_Composition_1280x720.png) · [PerspectiveB/1920](Evidence/captures-reviewed/PerspectiveB_Composition_1920x1080.png) |
| GroundingInspection | [OrthographicA/1280](Evidence/captures-reviewed/OrthographicA_GroundingInspection_1280x720.png) · [OrthographicA/1920](Evidence/captures-reviewed/OrthographicA_GroundingInspection_1920x1080.png) · [PerspectiveB/1280](Evidence/captures-reviewed/PerspectiveB_GroundingInspection_1280x720.png) · [PerspectiveB/1920](Evidence/captures-reviewed/PerspectiveB_GroundingInspection_1920x1080.png) |
| Standing | [OrthographicA/1280](Evidence/captures-reviewed/OrthographicA_Standing_1280x720.png) · [OrthographicA/1920](Evidence/captures-reviewed/OrthographicA_Standing_1920x1080.png) · [PerspectiveB/1280](Evidence/captures-reviewed/PerspectiveB_Standing_1280x720.png) · [PerspectiveB/1920](Evidence/captures-reviewed/PerspectiveB_Standing_1920x1080.png) |

## Movement and depth evidence

Each [A/720p](Evidence/captures-reviewed/OrthographicA_motion_1280.mp4), [B/720p](Evidence/captures-reviewed/PerspectiveB_motion_1280.mp4), [A/1080p](Evidence/captures-reviewed/OrthographicA_motion_1920.mp4), [B/1080p](Evidence/captures-reviewed/PerspectiveB_motion_1920.mp4) is 121 actual Player frames encoded at 12fps. This is a deterministic entry → front aisle → seat/hold → departure replay. Route: **(-3.9,0,2.5) → (-3.75,0,-1.15) → ordinary seat**, then reverse. Feet stay on y=0; plane angle stays fixed. Sitting uses an authored pose swap, departure reuses the reference walk cycle; no continuous sit animation, door animation, head turn or native gameplay input is claimed. These remain HUMAN_ANIMATION_TUNING_LATER.

Counter removed diagnostic images [A](Evidence/captures-reviewed/OrthographicA_CounterRemovedDiagnostic_1920x1080.png) / [B](Evidence/captures-reviewed/PerspectiveB_CounterRemovedDiagnostic_1920x1080.png) change only counter renderer visibility for an objective depth check. They are excluded from aesthetic A/B comparison. The opaque bartender lower-body source mask reveals **12049 / 10794** changed pixels compared with counter-present Composition. Physical ray tests independently hit the counter in front of the lower body. Ordinary/special image references, seat/feet support, static root stability and shader references are tested; automated checks do not establish artistic coherence.

## Performance baseline

Actual device: **12th Gen Intel(R) Core(TM) i7-12700KF / NVIDIA GeForce RTX 3070 / 32581 MB RAM / Windows 11  (10.0.26200) / Direct3D11**. Windows Development Player, no attached profiler, hidden batch mode. One explicit native **1920×1080 offscreen URP render request per frame**, 90 rendered warmup frames, uncapped/VSync off, then 60 seconds each camera. PNG readback/encoding and motion captures occur outside the sample interval. These are Player CPU/frame-loop measurements, not GPU latency or interactive foreground performance. A non-development foreground comparison, GPU time/VRAM/driver/power/storage characterization remain unmeasured; no minimum hardware or 60fps guarantee is claimed.

| Variant | Samples / duration | Frame P50/P95/P99/max ms | Unity allocated/reserved MiB | Draws/batches | Idle GC mean/max bytes |
|---|---|---|---|---|---|
| OrthographicA | 132779 / 60.00s | 0.443 / 0.572 / 0.712 / 3.793 | 43.3 / 183.1 | 131 / 131 | 0.000 / 0 |
| PerspectiveB | 132907 / 60.00s | 0.444 / 0.569 / 0.710 / 2.448 | 43.5 / 199.1 | 131 / 131 | 0.000 / 0 |

Common inventory: **121 scene transforms, 96 renderers, 94 enabled in Composition, 11 scene material references**, texture runtime estimate **89568 bytes** for renderer-referenced textures. This is not a total build/loaded-asset footprint. External process sampling (about 1Hz, entire run) peak working set **365.2 MiB**, peak private bytes **647.9 MiB**. Mono's raw process working-set counter returned 0 and is treated as unavailable. [Raw Player evidence](Evidence/captures-reviewed/player-run.json), [OS samples](Evidence/captures-reviewed/os-memory.json), [machine-readable inspection](Manifests/captures.json), [build result](Manifests/build.json).

## Technical limits and next decision

Capture trust requires a successfully initialized stage, matching unique run identity, zero process exit, a completion marker and a complete Player log without errors/exceptions/assertions. An actual built-Player binding-mismatch probe exited 1 with zero captures and was rejected by the inspector; [negative regression evidence](Manifests/initialization-regression.json). Native Unity Build / Build And Run preprocessing also validates/regenerates the current authored JSON. Changed and invalid source regressions passed with the same generated asset GUID. [Whole-branch review and dispositions](FINAL_REVIEW_RESULT.md).

FIRST_PASS room/art staging only. The window is a layered planar winter panel with snow marks, not a complete exterior. The shared density/seat height/camera/zone tint/palette/contacts are provisional. Reference art adoption and final rights/art approval remain pending. No hand/counter gameplay contact, acquired memory prop, paired guests, bartending, production UI, narrative, save or audio system was built. The workstation and future memory display are empty spatial reservations. This capture log contains **0** native temporary-allocation lifetime warnings; no Player Error/Exception or missing-shader pixels were recorded. Earlier valid capture runs recorded two lifetime warnings during repeated render requests. A clean latest run does not establish their cause or resolution; investigate the harness before using this path as a shipping benchmark.

Owner records accept/revise/reject for HG01/HG02/HG03 and selects a camera or requests visual revision. Stop before HG04; no artistic approval is inferred from the technical PASS.
