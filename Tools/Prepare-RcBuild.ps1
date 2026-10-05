param([string]$Project = (Join-Path $PSScriptRoot '..'))
$ErrorActionPreference = 'Stop'
$projectPath = [IO.Path]::GetFullPath($Project).TrimEnd('\', '/')
function Invoke-ReproGit([string[]]$Arguments) {
    $result = & git.exe -C $projectPath @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Git failed: $($Arguments -join ' ')" }
    return $result
}
$root = (Invoke-ReproGit @('rev-parse', '--show-toplevel')).Replace('/', '\').TrimEnd('\')
if ($root -ne $projectPath.Replace('/', '\')) { throw 'Project must be the checkout root.' }
$settings = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectPath 'ProjectSettings/ProjectSettings.asset')
if ($settings -notmatch '(?m)^  companyName: SURLHWA_RC_AUTOMATED_QA\r?$') {
    throw 'Only an isolated RC QA checkout is allowed. See Docs/28_CLEAN_CHECKOUT_AND_RELEASE_REPRODUCIBILITY.md.'
}
if (Invoke-ReproGit @('status', '--porcelain', '--', 'Assets/Code', 'Tools')) {
    throw 'Commit code/tool changes before recording build inputs.'
}
$version = Get-Content -Raw -LiteralPath (Join-Path $projectPath 'ProjectSettings/ProjectVersion.txt')
if ($version -notmatch '(?m)^m_EditorVersion: 6000\.3\.7f1\r?$') { throw 'Unexpected Unity version.' }
$revision = Invoke-ReproGit @('rev-parse', 'HEAD')
$inputs = foreach ($relative in (Invoke-ReproGit @('-c', 'core.quotepath=false', 'ls-files', '--', 'Assets', 'Packages', 'ProjectSettings'))) {
    # Recovery is intentionally excluded from the disposable sparse checkout.
    if ($relative -match '^Assets/_Recovery(?:/|\.meta$)') { continue }
    $path = Join-Path $projectPath $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing tracked build input: $relative" }
    $file = Get-Item -LiteralPath $path
    [ordered]@{ path=$relative; sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant(); bytes=$file.Length }
}
$folder = Join-Path $projectPath 'Logs/Review/RCAutomatedQA'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$encoding = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $folder 'source-root.txt'), $projectPath, $encoding)
$manifest = [ordered]@{
    revision=$revision; unity='6000.3.7f1'; utc=[DateTime]::UtcNow.ToString('O')
    scope='Tracked checkout inputs before BuildPipeline; no owner overlay. Import/build-generated changes must be reviewed separately.'
    tracked_changes=@(Invoke-ReproGit @('diff', '--name-only')); inputs=@($inputs)
}
[IO.File]::WriteAllText((Join-Path $folder 'source-inputs.json'), ($manifest | ConvertTo-Json -Depth 5), $encoding)
Write-Output "Recorded $($inputs.Count) tracked inputs at $revision. Run the static suite before building to generate korean-required.txt."
