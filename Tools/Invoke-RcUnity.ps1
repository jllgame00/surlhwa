param(
    [ValidateSet('Status','Refresh','Run','Eval')][string]$Action = 'Status',
    [string]$Suite,
    [string]$Code,
    [string]$Project = (Join-Path $PSScriptRoot '../Logs/Review/RCAutomatedQA/ValidationProject')
)
$ErrorActionPreference = 'Stop'
$projectPath = [IO.Path]::GetFullPath($Project)
$descriptor = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectPath 'Library/Pipeline/.unity-pipeline-port') | ConvertFrom-Json
if ([IO.Path]::GetFullPath($descriptor.projectPath) -ne $projectPath) { throw 'Pipeline descriptor belongs to another project.' }
$headers = @{ Authorization = ('Bearer ' + $descriptor.evalToken) }
$endpoint = 'http://127.0.0.1:' + $descriptor.port
if ($Action -eq 'Status') {
    Invoke-RestMethod -Uri ($endpoint + '/api/editor_status') -Headers $headers | ConvertTo-Json -Depth 6
    $statusPath = Join-Path $projectPath 'Logs/Review/RCAutomatedQA/status.txt'
    if (Test-Path -LiteralPath $statusPath) { Get-Content -LiteralPath $statusPath }
    return
}
if ($Action -eq 'Run') {
    if ($Suite -notin @('static','focused','measurement','stress','campaign','quiet','audio','motion','motion-safety','physical','ui','ui-review','modal','navigation','j5','build','exit')) { throw 'Unknown suite.' }
    $requestPath = Join-Path $projectPath 'Logs/Review/RCAutomatedQA/request.txt'
    if (Test-Path -LiteralPath $requestPath) { throw 'An RC request is already pending.' }
    # A previous PASS with the same suite name must not satisfy a fresh run.
    Set-Content -Encoding UTF8 -LiteralPath (Join-Path $projectPath 'Logs/Review/RCAutomatedQA/status.txt') -Value "QUEUED $Suite"
    Set-Content -Encoding UTF8 -LiteralPath $requestPath -Value $Suite
    return
}
if ($Action -eq 'Refresh') { $Code = 'UnityEditor.AssetDatabase.Refresh(); return UnityEditor.EditorApplication.isCompiling;' }
if ([string]::IsNullOrWhiteSpace($Code)) { throw 'Missing diagnostic code.' }
$body = @{ command='eval'; parameters=@{ code=$Code } } | ConvertTo-Json -Depth 6
# Credentials are sent only to the validated local Editor endpoint, never printed.
Invoke-RestMethod -Uri ($endpoint + '/api/exec') -Headers $headers -Method Post -ContentType 'application/json' -Body ([Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 45 | ConvertTo-Json -Depth 8
