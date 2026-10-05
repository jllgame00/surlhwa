# SURLHWA RC1 manifest

Sealed 2026-10-05: **SURLHWA_RC1_REPRODUCIBLE_RELEASE_CANDIDATE**.
Acceptance: **HUMAN_ACCEPTANCE_PENDING**. Verdict: **CLEAN_CHECKOUT_REPRODUCIBLE**.

| Field | Canonical value / evidence |
| --- | --- |
| Canonical tested source / exact build SHA | `c443ed4e6a24b7b6169ab9f8608997cbbf97e675` |
| Canonical branch | `refactor/project-rebuild`; subsequent result seal changes documentation only |
| Starting HEAD / prior runtime baseline | `54650ed` / `da480e5`; no production code/asset change in this task |
| Canonical package | `D:/univ/surlhwa/Releases/SURLHWA_RC1_c443ed4_Windows.zip` |
| ZIP size / SHA-256 | 94,978,108 bytes / `2c900ca020229d6b688d8bf17dc7e8bfb7ccabe24f9b78e342aef6c826fb25f7` |
| Unpacked runtime | 270,068,780 bytes, 207 files; keep the complete folder |
| Unity / target | `6000.3.7f1`, Windows x86_64, Development QA build; instrumentation opt-in, isolated saves |
| Reproduction | Two independent Git-object clones; no owner overlay or pre-seeded Library/Temp; fresh imports and locked packages passed |
| Compile / build | 0 C# errors; build succeeded in 112.288 s with one classified warning |
| Automated QA | All 13 existing Editor suites; 31 focused/stress cases in Editor and compiled Player, 774 key pulses / 141 Submits per environment |
| Campaign / save | 18 nights, 47 natural departures, eight Continue checkpoints, discovery/retry, seven memories, ending/reset passed |
| Recipes / reachability | 9/9 campaign recipes, four methods, known/supplied Recommendation alternatives passed; two reserve recipes out of scope |
| Fonts / audio / animation | 570 required Hangul characters available from packaged source font; Phase O audio and Phase N animation/fallback/cleanup passed |
| Native input | Keyboard, cursor and pointer-button event paths **PASS** on the tested desktop; four ordinary services and Continue observed; no full-native-campaign claim |
| Visible render | Bar/dialogue/method selector/bartending/Settings/Codex at 720p and 1080p; direct desktop capture and extracted Player frames; no aesthetic approval |
| ZIP extraction | Separate fresh directory, CRC and every file hash verified, normal launch without QA flags, four native service/departure paths passed |
| Owner dirty state required | **NO**; 76 protected files, original dirty status and all 322 previous build files preserved |
| Known P0 / P1 | **0 / 0** |
| Remaining debt | HUMAN_VISUAL, HUMAN_AUDIO, HUMAN_WRITING, HUMAN_PLAY_FEEL |

The executable comes from the exact tested commit above. The later documentation
seal records its artifact hashes without pretending the executable was rebuilt
from that seal. Assets, Packages, ProjectSettings and build tooling remain
identical between the tested commit and the documentation seal.

The prior `Builds/Windows-RC-5bdc0b1` remains preserved. Meaningful runtime/data
properties match; 306 build files also have identical hashes. Unity binary
identity is not required. The earlier KoreanFallback importer warning did not
recur in either clean build. The sole warning is the absent optional Pipeline
remote-command server configuration; URP rendering is unaffected and verified.
The previous native-pointer limitation is resolved by the new native event proof.

Reproduction commands, warning/generated-diff classifications, source inventory,
input boundaries, package/extraction evidence and preservation details are in
[clean checkout proof](28_CLEAN_CHECKOUT_AND_RELEASE_REPRODUCIBILITY.md).
Earlier source-qualified RC history remains in [RC QA](27_RC_AUTOMATED_GAMEPLAY_QA.md).
No routine QA rerun is assigned to the owner. Human acceptance is not claimed.
