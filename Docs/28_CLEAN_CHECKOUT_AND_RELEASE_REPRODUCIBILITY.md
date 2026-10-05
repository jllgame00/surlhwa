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

```powershell
Set-Location $clone
Start-Process -FilePath $unity -WindowStyle Hidden -ArgumentList @(
    '-projectPath',('"'+$clone+'"'),'-executeMethod','SURLHWARCQA.Boot',
    '-logFile',('"'+(Join-Path $evidence 'qa-editor.log')+'"'))
# Wait for Logs/Review/RCAutomatedQA/ready.txt and an idle Editor.
Tools/Invoke-RcUnity.ps1 -Action Status -Project $clone
dotnet build Assembly-CSharp-Editor.csproj --no-restore
Tools/Invoke-RcRegression.ps1 -Project $clone
# Run modal and navigation separately, with the isolated Game View focused.
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
time. Use the existing native observer/tools for visible 720p/1080p frames,
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

Execution results for the 2026-10-05 proof are recorded below after the gates.
