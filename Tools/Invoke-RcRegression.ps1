param(
    [string[]]$Suites = @('static','focused','stress','campaign','physical','audio','motion','motion-safety','ui','ui-review','j5'),
    [string]$AfterSuite,
    [string]$Project = (Join-Path $PSScriptRoot '../Logs/Review/RCAutomatedQA/ValidationProject')
)
$ErrorActionPreference = 'Stop'
$statusPath = Join-Path $Project 'Logs/Review/RCAutomatedQA/status.txt'
function Wait-RcSuite([string]$name) {
    $deadline = [DateTime]::UtcNow.AddMinutes(41)
    do {
        Start-Sleep -Seconds 2
        $status = if (Test-Path -LiteralPath $statusPath) { (Get-Content -LiteralPath $statusPath -Raw).Trim() } else { '' }
        if ($status -eq "FAIL $name") { throw "RC suite failed: $name. Preserve and inspect its log before continuing." }
        if ($status -eq "PASS $name") { Write-Output $status; return }
    } while ([DateTime]::UtcNow -lt $deadline)
    throw "RC orchestration watchdog: $name has no terminal result."
}
& (Join-Path $PSScriptRoot 'Invoke-RcUnity.ps1') -Action Status -Project $Project
if ($AfterSuite) { Wait-RcSuite $AfterSuite }
foreach ($suite in $Suites) {
    # Pointer suites require deliberate Game View focus and are run separately.
    if ($suite -in @('modal','navigation','build','exit')) { throw 'Run focused UI/device/build operations explicitly.' }
    & (Join-Path $PSScriptRoot 'Invoke-RcUnity.ps1') -Action Run -Suite $suite -Project $Project
    Wait-RcSuite $suite
}
