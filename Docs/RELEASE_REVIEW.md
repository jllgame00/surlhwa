# SURLHWA RC review handoff

2026-10-05: **SURLHWA_RC1_REPRODUCIBLE_RELEASE_CANDIDATE** /
**HUMAN_ACCEPTANCE_PENDING**. Known P0/P1: **0/0**.
Committed content alone reproduced the Windows RC. No owner-dirty input was needed.

## Current package

`D:/univ/surlhwa/Releases/SURLHWA_RC1_c443ed4_Windows.zip`

- Exact source: `c443ed4e6a24b7b6169ab9f8608997cbbf97e675`; Unity 6000.3.7f1, Windows x86_64.
- ZIP: 94,978,108 bytes; SHA-256 `2c900ca020229d6b688d8bf17dc7e8bfb7ccabe24f9b78e342aef6c826fb25f7`.
- Extract the entire archive, then launch `SURLHWA.exe`. Keep all runtime files.
- Development RC with opt-in QA instrumentation and a separate save namespace.
  Ordinary launch needs no flags, source project or internal QA inputs.
- CRC/file checksums passed before and after a separate extraction/launch test.
  The extracted package completed New Game, four ordinary services/departures,
  native method selection and the recipe guide. Human acceptance remains pending.

The documentation seal does not change the executable's source SHA. The previous
`Builds/Windows-RC-5bdc0b1` is preserved; no old package file was overwritten.

## Automated evidence

| Area | Result |
| --- | --- |
| Source and toolchain | Two fresh Git-object checkouts; no copied Library/Temp/private state; 69 locked packages resolved; Unity and dotnet compilation passed with 0 errors |
| Production data | Stable IDs, scenes/scripts, 9/9 campaign recipes, preparation/supply/knowledge, dialogue, guests, memories, ending and render assets passed |
| Input/bartending | 31 distinct scenarios in both Editor and compiled Player; 774 key pulses / 141 Submits per environment; all four methods, M/measure/T/I and D/J, invalid/held/re-entry cases |
| UI | Existing 204 simulated pointer cycles, modal collision/cleanup, guide/discovery/farewell, 12 combined Back/overlay cycles and four service routes |
| Campaign/save | 18 nights / 47 natural departures; special guests/shared lovers; eight early/mid/late/ending Continue checkpoints; discovery/retry, seven memories, ending/reset |
| Audio/animation | One listener, bounded/cleaned sources and loops, disabled-owner cleanup, Phase N fallback and motion; 22 original Phase O audio hashes match |
| Font/render | All 570 required Korean characters from packaged Noto Sans KR; actual visible Player screens at 720p/1080p, including selector from the ZIP; no missing-glyph warning or obvious broken material/sprite |
| Standalone | Native Continue, Settings/Codex, dialogue/Brew, measurement/transfer/ice, cursor Stir, strain/service and natural departure; four native ordinary services, plus the independent ZIP path |

Native Windows pointer down/up/click delivery is now proven on this desktop;
**NATIVE_POINTER_HARDWARE_DEBT is cleared for this tested path**. Interrupted
concurrent-desktop attempts and the earlier simulated-pointer fallback retain
separate evidence. They are not counted as native successes. Native preparation
respects physical pickup/return timing; early actions were retried after the
existing guards cleared, with no change to gameplay or the repository QA harness.

The full campaign remains Editor production/technique automation. Compiled
focused/stress scenarios use simulated production input. The native service
smoke intentionally uses incomplete drinks to verify result/departure safety;
exact recipe success is covered separately. No full native standalone campaign,
physical listening or subjective play-feel approval is claimed.

Build warning count is **one**: the optional Pipeline remote-command server is
unconfigured; URP renders correctly. The previous KoreanFallback importer warning
did not recur in either clean build, and packaged font proof passed. Exact
compile/tooling warnings and generated Unity diffs are classified in [Docs/28](28_CLEAN_CHECKOUT_AND_RELEASE_REPRODUCIBILITY.md).

## Human-only review

- **HUMAN_VISUAL:** art, composition, animation naturalness, readability preference
  and memory presentation.
- **HUMAN_AUDIO:** listening on the intended device, loudness, sound character,
  ambience and repetition.
- **HUMAN_WRITING:** dialogue tone, emotional meaning and reading pace.
- **HUMAN_PLAY_FEEL:** bartending satisfaction and overall pacing.

Final art, animation tuning and music composition remain presentation backlog.
Reserve recipes, future revisits/IBA expansion, other platforms/aspect ratios and
performance optimization are outside this release-engineering task. No routine
mechanics checklist or campaign bug hunt is assigned to the owner.

All 76 protected owner files and the original dirty-status record are unchanged;
all 322 previous RC files are unchanged. Current artifact/provenance summary:
[RC1 manifest](RC1_MANIFEST.md). Full reproduction and local evidence locations:
[clean checkout proof](28_CLEAN_CHECKOUT_AND_RELEASE_REPRODUCIBILITY.md). Earlier
QA history and production findings remain in [Docs/27](27_RC_AUTOMATED_GAMEPLAY_QA.md)
and [Docs/13](13_POST_I16_PRODUCTION_GAP_AUDIT.md).
