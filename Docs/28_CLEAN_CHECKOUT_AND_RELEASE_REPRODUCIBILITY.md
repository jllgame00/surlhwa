# Clean checkout and release reproducibility

This procedure uses committed Git objects and Unity 6000.3.7f1. It never copies
an owner's Library, Temp, imported assets, untracked art, or settings overlays.
Use a disposable sibling directory and a unique QA product name. Do not run the
save-reset fixtures in the owner's Editor.

## Create and import

Set `$source`, `$clone`, `$evidence`, and `$unity` to your local paths. `$clone`
and `$evidence` must be new directories outside the source project. The Unity
executable must belong to version 6000.3.7f1; Windows build support is required.

```powershell
$sha = git -C $source rev-parse HEAD
if (Test-Path -LiteralPath $clone) { throw 'Use a new destination' }
git clone --no-local --no-checkout $source $clone
git -C $clone config core.sparseCheckout true
@('/*','!/.codex/','!/UserSettings/','!/Assets/_Recovery/','!/Assets/_Recovery.meta') |
    Set-Content -Encoding ASCII -LiteralPath (Join-Path $clone '.git/info/sparse-checkout')
git -C $clone checkout --detach $sha
git -C $clone status --short # Must be empty before Unity runs.
New-Item -ItemType Directory -Path $evidence
Start-Process -FilePath $unity -Wait -PassThru -WindowStyle Hidden -ArgumentList @(
    '-batchmode','-quit','-projectPath',('"'+$clone+'"'),'-buildTarget','Win64',
    '-logFile',('"'+(Join-Path $evidence 'fresh-import.log')+'"'))
```

Require exit 0, no C# errors and resolved locked packages. Preserve the log and
inspect `git diff` after import. Unity serializes trailing spaces in generated
sprite metadata; generated UserSettings may appear because older history tracks
them. Neither belongs in a release commit. Never copy the original settings.

## Isolated validation and build

In the disposable clone only, set `companyName` in
`ProjectSettings/ProjectSettings.asset` to `SURLHWA_RC_AUTOMATED_QA` and
`productName` to a unique test name. These two explicit test settings isolate
PlayerPrefs; they are not missing production settings or changes to save keys.
Record that small diff. Keep all other committed settings, including batching.
The existing QA boot also enables background execution; Unity saves that setting
when building. This is a documented QA derivation, not an owner settings overlay.

```powershell
Set-Location $clone
Start-Process -FilePath $unity -WindowStyle Hidden -ArgumentList @(
    '-projectPath',('"'+$clone+'"'),'-executeMethod','SURLHWARCQA.Boot',
    '-logFile',('"'+(Join-Path $evidence 'qa-editor.log')+'"'))
# Wait for Logs/Review/RCAutomatedQA/ready.txt and an idle Editor.
Tools/Invoke-RcUnity.ps1 -Action Status -Project $clone
dotnet build Assembly-CSharp-Editor.csproj --no-restore
Tools/Invoke-RcRegression.ps1 -Project $clone
# Focus the isolated Game View before the pointer/modal fixtures.
Tools/Invoke-RcUnity.ps1 -Action Eval -Project $clone -Code 'var t = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView"); UnityEditor.EditorWindow.GetWindow(t).Focus(); return "focused";'
# Run modal and navigation separately.
Tools/Invoke-RcUnity.ps1 -Action Run -Suite modal -Project $clone
# Wait for PASS modal before dispatching navigation; never overlap suites.
Tools/Invoke-RcUnity.ps1 -Action Run -Suite navigation -Project $clone
# Wait for PASS navigation, inspect all logs and generated tracked diffs.
Tools/Prepare-RcBuild.ps1 -Project $clone
Tools/Invoke-RcUnity.ps1 -Action Run -Suite build -Project $clone
```

`Prepare-RcBuild.ps1` creates the provenance inputs required by the existing
`SURLHWARCBuild` entry point. It accepts only an isolated QA namespace, requires
committed code/tools, and hashes tracked Assets, Packages and ProjectSettings.
Its manifest is a pre-build input record; retain the post-build diff separately
for Unity-generated font, URP and batching serialization. Do not commit caches.

The builder creates `Builds/Windows-RC-<shortSHA>` with Development options and
`SURLHWA_RC_QA`. Instrumentation is opt-in. Normal launch remains the production
menu/gameplay path; this is a review RC, with human acceptance pending.

Run the compiled `--surlhwa-rc-focused` and `--surlhwa-rc-stress` suites one at a
time, each from its own new working directory under `$evidence`, using the
absolute executable path. This keeps Player result files separate from Editor
results. Use the existing native observer/tools for visible 720p/1080p frames,
ordinary service, Settings/Codex and Continue. Preserve the distinction between
native keyboard/cursor/buttons, simulated production input and logic fixtures.

## Package gate

Copy the complete runtime output to a new release folder outside the repository.
Keep the executable, Data directory, MonoBleedingEdge, UnityPlayer, crash handler
and produced runtime DLLs. Exclude source/debug backup folders, PDBs, build-input
manifests, QA corpora/logs and internal review documentation. Include a concise
player README, source/version metadata, repository LICENSE, the existing Noto
and Liberation Sans OFL notices and bundled EmojiOne attribution. Phase O audio
is project-created; its tracked source/hash record remains in ArtSource/Audio.

Hash package files, create a ZIP and SHA-256 sidecar, test every archive entry,
extract to another new directory, compare all extracted file hashes and launch
that extracted executable. Record actual rendered frames and a representative
interaction. ZIP creation alone is not a successful extraction test.

## Executed proof (2026-10-05)

Verdict: **CLEAN_CHECKOUT_REPRODUCIBLE**. Final artifact classification:
**SURLHWA_RC1_REPRODUCIBLE_RELEASE_CANDIDATE / HUMAN_ACCEPTANCE_PENDING**.
Known P0/P1: **0/0**. **OWNER_DIRTY_STATE_REQUIRED: NO**.

| Provenance | Result |
| --- | --- |
| Starting repository HEAD | `54650edfdad87e27184889798fba082299265390`, `refactor/project-rebuild` |
| Prior runtime baseline | `da480e5e76b9f45c18635b36d1d2e07732f7842e` |
| Canonical tested/build source | `c443ed4e6a24b7b6169ab9f8608997cbbf97e675` |
| First independent checkout | `D:/univ/surlhwa/_repro/rc1-54650ed`, branch `repro/rc1` |
| Final independent checkout | `D:/univ/surlhwa/_repro/rc1-c443ed4`, branch `repro/final` |
| Creation | `git clone --no-local --no-checkout` from local committed Git objects, followed by the documented sparse checkout; both initially clean |
| Excluded inputs | Library, Temp, Logs, Obj, UserSettings, Recovery, .codex and all original untracked/ignored files; no cache or owner overlay copied |
| Fresh imports | Exit 0; 236.461 s at the starting snapshot, 175.732 s at the final tested snapshot |
| Environment | Unity `6000.3.7f1 (696ec25a53d1)`; 69 locked packages resolved at their recorded versions |
| Package versions | Input System 1.18.0; URP 17.3.0; UGUI 2.0.0; Pipeline 0.6.0-exp.1; full environment remains in manifest/lock, unchanged |

`c443ed4` adds only the small provenance helper and reproduction instructions.
No production code, asset, package, scene, prefab, stable ID or save-key fix was
needed. The later documentation seal records these results; its Git tip is not
misreported as the executable's build SHA. Its build-input trees must remain
identical to the tested commit. Both clean copies passed the full RC suite.

### Repository and owner-state audit

All 1,498 tracked production input files (Assets, Packages, ProjectSettings,
excluding Recovery) were present. The 785 checked asset metas had no missing
file metas or duplicate GUIDs. After resolving locked packages, the GUID scan's
remaining entries were Unity's built-in reference and six legacy probe-debug
fields in `Renderer2D.asset`. Those fields are obsolete in URP 17.3's
`Deprecated.cs`; current probe resources are package-owned. They are not active
render dependencies. The serialized URP/Renderer2D pair built and rendered.

No local-file, unpinned Git, or owner-machine package dependency was found.
Build/QA commands accept the checkout path. The old pointer-review launcher's
hardcoded Phase-M directory and the review-board script's Windows Arial path
are **LOCAL_REVIEW_ONLY**, not production build dependencies. No production
reference required Library, Temp, UserSettings, Recovery or untracked content.

The original 65 dirty/untracked entries were preserved. The expanded baseline
covers **76 unique owner/recovery/IDE files**, all byte-identical afterward;
the exact porcelain-v2 status also stayed identical. All **322 previous RC
files** retained their original hashes. No original Unity launch, reset, clean,
stash, restore, recovery deletion or settings rewrite was performed.

Owner art/font differences were whitespace-only (56 sprite metas and one font
asset). The clean import regenerated the sprite metadata independently. The
owner's Standalone static-batching entry was not copied: Unity generated the
same default entry during the clean build. Config, layouts, recovered scenes,
font-cache state and batching edits are **NOT REQUIRED FOR RC REPRODUCTION**.

### Compile, QA and generated changes

- Unity compilation: **0 C# errors**, seven existing CS0252 warnings.
  `dotnet build Assembly-CSharp-Editor.csproj --no-restore`: **0 errors**, eight
  warnings, including the additional MSBuild assembly-resolution warning below.
- All 13 existing suites passed: static, focused, stress, campaign, physical,
  audio, motion, motion-safety, ui, ui-review, j5, modal and navigation.
- Production data: stable IDs, scenes/scripts/references, nine campaign recipes,
  ingredients/preparations, dialogue, guests/paired visits, campaign, seven
  memories, ending, animation, Phase O audio, Korean fallback and URP passed.
  Two retained reserve recipes remain outside the campaign scope.
- Campaign: **18 nights / 47 natural departures**, discovery/retry, reachable
  Recommendation alternatives, all special identities and shared lovers,
  seven memories, ending, reset and eight Continue checkpoints passed. This is
  Editor production/technique automation, with its existing seam boundaries.
- Editor and compiled Player each passed **27 focused + 4 stress** scenarios:
  **774 key pulses / 141 Submits** per environment. This is
  **SIMULATED_PRODUCTION_INPUT**, not a native full-campaign claim. Compiled
  processes exited 0 (120.666 s focused; 29.119 s stress).
- Modal/pointer fixtures retained 204 simulated pointer cycles, 12 combined
  overlay/Back cycles and four service routes. Audio/listener ownership,
  disabled-owner/loop cleanup and Phase N motion/fallback passed.
- All **570 required Korean characters** passed from the clean project and
  packaged Noto Sans KR source font; the production corpus did not change.
  Actual Player/ZIP text rendered without missing-character warnings.
  All **22 Phase O audio source hashes** match the tracked project-created
  audio manifest; no new audio source or attribution was introduced.

Opening/building is reproducible but does **not** leave every tracked file
unchanged. The exact generated diffs are retained, not silently discarded:

| Stage | Classified tracked modifications |
| --- | --- |
| Fresh import | 56 sprite-meta whitespace rewrites; two regenerated UserSettings files |
| QA | Same sprite metas; explicit company/product QA namespace; three generated UserSettings files |
| Build/Editor close | 63 files total: 57 whitespace-only sprite/font files; URP prefilter table; URP runtime-resource list; PlayerSettings QA namespace/background execution plus generated Standalone batching defaults; three UserSettings files |

No scene/prefab changed. No generated change is staged or needed as an owner
input. TMP's existing prebuild processor clears dynamic glyph state, and the
tracked source font regenerates it in Player. URP's existing build processors
recreate their prefilter/runtime tables. No architecture change was made.

### Exact warning disposition

| Warning/diagnostic | Classification and evidence |
| --- | --- |
| CS0252 at `SURLHWAJ4DiscoveryValidation.cs` lines 391, 584, 602, 619, 624, 636, 657 | EXISTING_KNOWN_WARNING; Editor fixture compares reflected object identity with expected assets; seven distinct locations, no compile failure |
| MSB3277, System.Threading.Tasks.Extensions 4.2.0.0 / 4.2.0.1 | EXISTING_KNOWN_WARNING; standalone MSBuild resolution of Unity facade versus Pipeline Roslyn tooling; Unity Editor/Player compile and build passed |
| Duplicate System.Runtime.CompilerServices.Unsafe 6.0 / 4.0.4 diagnostic | Editor package/tooling resolution (Collections tests versus Pipeline); no required assembly failure |
| `Pipeline: No RuntimePipelineConfig asset found (Project Settings > Pipeline > Runtime). Pipeline will be disabled in Player builds.` | Sole build warning; optional Pipeline remote-command server disabled. This is unrelated to URP rendering; actual frames and Player paths pass |
| KoreanFallback importer inconsistency | **Did not recur in either clean build**; clean import, dynamic source-font regeneration, 570-character check and visible Player/ZIP text passed; previous cache warning is no longer open technical debt |
| Negative recipe/service fixture warnings | Expected rejection/unknown-drink/popup-skip cases, not missing dependencies; exact strings are retained in `final-validation-summary.json` |

The negative-fixture warning set comprises `hearts <= 0, popup skipped`,
`No matched cocktail. Serving as unknown cocktail.`, and RecipeMatch missing-
ingredient results for Dry Martini (0.72, 1/2), Janseol (0.16, 0/3), Last Word
(0.25, 0/4), Manhattan (0.23, 0/3) and Manwol (0.32, 1/3), each against 0.85.
No C# error, required-package failure, production exception, missing production
script or unexplained shader/font/audio import failure remained. Transient
license-client token lookup and shutdown Curl-42 messages resolved with exit 0;
there is no licensing validation debt. The old relocated Search failure did not
recur in these fresh imports.

### Build, visible Player and extraction

Windows x86_64, Development, `SURLHWA_RC_QA` opt-in, built at
2026-10-05 10:54:30 UTC in **112.288 s** with **1 warning / 0 errors**.
Output: `_repro/rc1-c443ed4/Builds/Windows-RC-c443ed4` (relative to the owner's
parent directory), **322 files / 285,351,189 bytes**. The exact enabled scenes:

1. `Assets/_SURLHWA/Scenes/Gameplay/BarScene.unity`
2. `Assets/_SURLHWA/Scenes/Gameplay/BartendingScene.unity`

Visible window/client captures and Player frame captures verified Bar, Korean
dialogue, Settings, Codex, method selection and a bartending route at both
1280x720 and 1920x1080. Frames are nonblack, text is populated, and no obvious
missing-material magenta, missing sprite or catastrophic displacement was
observed. The extracted Player also rendered the recipe guide. This is technical
render evidence; aesthetic/readability preference remains human-owned.

At 1080p the source build completed Continue -> dialogue -> Brew -> native
M/held Space/T/I/repeated I -> native cursor Stir -> strain -> service -> natural
departure and next guest. Measurement transferred 52.926 ml; exactly one AddIce
was recorded while M remained on; native cursor motion reached 1,098.795 degrees.
Four consecutive ordinary services ultimately completed through native input.
These intentionally incomplete compositions test lifecycle/rejection safety;
nine exact recipes passed separately through the compiled production handlers.
Native button down/up/raycast/click delivery is now verified. Earlier concurrent
desktop activity is retained as interrupted evidence, with separate 720p
simulated-pointer results. It is not silently reclassified as native input.

The separate ZIP extraction launched first **without QA flags**, visibly rendered
the ordinary menu/Bar, then ran observation-only native QA. New Game plus four
services and natural departures reached the real Recommendation selector;
native selection/Confirm opened the route and recipe guide. No gameplay state,
supply, knowledge, save slot or scene was injected to reach that screen. Two
external-driver timing attempts stopped correctly: a short Space hold ended
before pickup/alignment and an early T arrived before `IsResting`. Allowing the
existing physical transitions to finish and using fresh native input completed
the path; no production code/assertion changed. The first source-build fifth
capture also missed the existing ordinary-guest patience window; the extracted
run captured it immediately. These are retained execution limits, not hidden
build failures or claimed successful actions.

No extracted runtime module came from either source checkout or previous build
(105 modules inspected on normal launch). No missing DLL/data, production
exception or missing-glyph warning occurred. The extracted process closed
normally; its detached-process exit code was not available. Compiled regression
exit codes, reported above, were captured directly.

### Canonical distributable

- Directory: `D:/univ/surlhwa/Releases/SURLHWA_RC1_c443ed4_Windows/`
- ZIP: `D:/univ/surlhwa/Releases/SURLHWA_RC1_c443ed4_Windows.zip`
- Size: **94,978,108 bytes ZIP**; **270,068,780 bytes / 207 files unpacked**.
- ZIP SHA-256: `2c900ca020229d6b688d8bf17dc7e8bfb7ccabe24f9b78e342aef6c826fb25f7`
- Independent extraction: `D:/univ/surlhwa/_release-extract-c443ed4/Builds/SURLHWA_RC1_c443ed4_Windows/`
- CRC, complete extracted file hashes and post-launch hashes: **PASS**.
- Includes runtime EXE/Data/MonoBleedingEdge/D3D12/DLLs/crash handler, concise
  README, RELEASE.json, SHA256SUMS and existing MIT/OFL/EmojiOne notices.
  Excludes PDBs/DoNotShip output, source, Git, caches, QA inputs/logs and private
  configuration. No source directory is needed to launch the ZIP.

The archive SHA sidecar and internal SHA256SUMS identify the exact artifact.
The adjacent `.package-verification.json` records extraction/startup checks;
it is internal engineering evidence, outside the ZIP.

### Comparison and closure

Compared with preserved `Builds/Windows-RC-5bdc0b1`, the new build retains the
same runtime layout, scene list, locked packages, production inventory,
9-recipe behavior, 18-night campaign, fonts and audio. **306 files are byte-
identical**; changed Unity serialized/binary metadata, preference identity,
provenance and the renamed excluded Burst debug folder are classified. Source
comparison finds only the already-recorded Editor/test changes, whitespace,
explicit QA settings and automatically generated build tables; six old ignored
IDE files are absent. No runtime behavior divergence was found. Binary identity
is neither promised nor used as the acceptance criterion. Warnings improved
from two to one; native pointer delivery and visible ZIP render proof now pass.

Objective source/import/compile/QA/build/package/extraction gates: **PASS**.
No remaining objective validation debt was identified within this Windows RC
scope. **HUMAN_SIGNOFF_REQUIRED** remains only HUMAN_VISUAL, HUMAN_AUDIO,
HUMAN_WRITING and HUMAN_PLAY_FEEL. Native pointer hardware-path debt from the
prior seal is cleared for the tested Windows desktop; this does not claim a full
native 18-night playthrough or physical listening approval. Final art/animation/
music taste is **DEFERRED_PRESENTATION**; reserve/future content, other platforms
and performance optimization are **OUT_OF_SCOPE**. No known implementation
defect is being hidden in those categories.

Raw local evidence: `D:/univ/surlhwa/_repro/rc1-20261005-evidence/`, each clean
checkout's `Logs/Review/RCAutomatedQA/`, and the extraction test root's equivalent
folder. Import logs, resolved-package/asset manifests, all suite logs, exact
warnings/generated diffs, native/mixed traces, frames, archive verification and
protected-file hashes are retained there. They are not production inputs.
