# SURLHWA RC review handoff

2026-10-04: **RC_AUTOMATED_QA_PASS_WITH_NATIVE_DEBT**.
No known P0/P1 remains in the completed bounded audit. Human approval is pending.
The detailed executable coverage, defects and evidence boundaries are in
[Docs/27](27_RC_AUTOMATED_GAMEPLAY_QA.md); historical milestones remain in
[Docs/13](13_POST_I16_PRODUCTION_GAP_AUDIT.md).

## Current package

`Builds/Windows-RC-5bdc0b1/SURLHWA.exe`

- Source: `5bdc0b10034fc44b53b502799cb6d5f58ea5dda0`.
- Unity 6000.3.7f1, Windows x64; successful build at 2026-10-04 12:09:12 UTC.
- Development QA build; instrumentation is opt-in, saves use a separate namespace.
- Keep the complete package together. `BUILD_INFO.txt` and `source-inputs.json`
  identify the code, preserved owner art/font overlay, and isolated build inputs.
- Later handoff/Editor-runner changes do not alter the packaged gameplay code.
- Start without QA flags for ordinary review. This package is for review, not
  final public distribution approval.

## What automation has already checked

| Area | Result |
| --- | --- |
| New input regressions | 31 distinct scenarios in both Editor and compiled Player; 774 key pulses and 141 Submit operations per environment |
| Bartending | All four methods, all nine campaign recipes, Jigger/ice/transfer, invalid keys, held/repeated input, reload/re-entry |
| UI delivery | 204 virtual Input System pointer cycles, each with one callback; 12 combined overlay/Back cycles across four natural services |
| Customer/campaign | 18 nights, 47 natural departures, all eight special identities, shared lovers service, seven memories, ending |
| Save/Continue | Eight representative campaign Continue entry points, New Game/reset, knowledge/visit/memory persistence; standalone Continue plus service |
| Discovery/farewell | Six J5 scenarios including failure positions, preparation/FIT negatives, popup completion, slow frames and long audio |
| Audio/motion | One listener, bounded sources, pause/mute/exit/disabled-owner cleanup, locomotion/seat/service/departure mapping and fallback |
| Screens/fonts | Key screens captured at 720p and 1080p; actual Player frames at both sizes; all 570 required Hangul characters available in the packaged font |

The former request for an owner rerun of **M -> measure -> T -> I** is superseded.
Windows SendInput reached legacy polling, transferred the liquid, placed ice
once while M remained on, and rejected repeat I. Native cursor motion also
reached Stir. Standalone incomplete-drink service naturally departed and spawned
the next guest at both review resolutions. The exact nine recipes passed through
enabled production input handlers separately.

Supported method-selection Back, guide/settings/Codex re-entry and active-route
reload have automated coverage. There is no authored in-drink Cancel command;
automation does not invent one or mislabel scene restart as keyboard Cancel.

The full 47-encounter run used Editor production/technique seams. The Player ran
31 focused/stress cases plus menu/settings/Codex, ordinary service and Continue.
This is not a claim of a full native standalone campaign.

## Remaining input limitation

Native OS mouse-button injection was obstructed by another process, PickerHost,
which owned the target hit region despite focusing/moving only the QA Player.
The safety guard stopped input to that unrelated process. Existing virtual Mouse
press/release/raycast tests passed through the real UI input module in Editor
and Player. OS pointer delivery remains **VALIDATION_DEBT**; keyboard delivery
does not inherit that limitation. A future unobstructed desktop can rerun the
native tools; no routine mechanics checklist or full campaign bug hunt is assigned
to the owner.

The two build warnings have dispositions: the optional Pipeline remote-command
server is intentionally absent, and the KoreanFallback importer cache warning
has no observed packaged glyph/render failure. The original owner font/settings
were preserved. No fatal exception or missing-glyph warning occurred in the
final compiled input suites.

## Human-only review

- **HUMAN_VISUAL:** art taste, animation naturalness, stool/contact composition,
  readability preference, tool silhouettes and memory presentation.
- **HUMAN_AUDIO:** comfortable loudness, satisfying tool sounds, ambience taste
  and repetition on the intended speakers/headphones.
- **HUMAN_WRITING:** dialogue tone, emotional meaning and reading pace.
- **HUMAN_PLAY_FEEL:** bartending satisfaction and whether the overall pacing
  becomes tiring. This is taste review, not a request to find ordinary blockers.
- **NATIVE_HARDWARE_ONLY:** real-device pointer/output behavior not proven under
  the current desktop interception; no broad manual regression is requested.

Existing final memory/glass/garnish art, animation tuning and music composition
remain presentation backlog. Two reserve recipes, future revisits, further IBA
expansion and other platforms/aspect ratios remain outside this RC scope.
Public distribution/credit approvals remain owner decisions.

Local evidence: `Logs/Review/RCAutomatedQA/`, including scenario/state matrices,
reproductions, traces, campaign/pointer/standalone summaries, screenshot index,
warning harvest and protected-file verification. Original protected state is
67 unique files (71 baseline entries); all remain byte-for-byte unchanged.
