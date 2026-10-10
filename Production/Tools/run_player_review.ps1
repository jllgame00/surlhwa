param([string]$EvidenceName='captures-reviewed')
$ErrorActionPreference='Stop'
$productionPath=Split-Path $PSScriptRoot -Parent
$outputPath=Join-Path $productionPath "Validation/Evidence/$EvidenceName"
$logPath=Join-Path $productionPath "Validation/Evidence/$EvidenceName-player.log"
if((Test-Path -LiteralPath $outputPath) -or (Test-Path -LiteralPath $logPath)){throw "Refusing to overwrite existing review evidence: $outputPath / $logPath. Choose a new EvidenceName."}
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$runId=[Guid]::NewGuid().ToString('N')
$executablePath=Join-Path $productionPath 'Validation/Builds/Windows/SurlhwaSpatialProof.exe'
[pscustomobject]@{runId=$runId;startedUtc=[DateTime]::UtcNow.ToString('O');executable=$executablePath;executableSha256=(Get-FileHash -LiteralPath $executablePath -Algorithm SHA256).Hash.ToLowerInvariant();logPath=$logPath} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'expected-run.json') -Encoding utf8
$playerProcess=Start-Process -FilePath $executablePath -ArgumentList @(
 '-batchmode','-force-d3d11','-screen-width','1920','-screen-height','1080','-reviewCapture','-reviewOutput',$outputPath,'-reviewRunId',$runId,'-logFile',$logPath
) -WindowStyle Hidden -PassThru
Write-Output "Production review Player PID $($playerProcess.Id)"
$samples=[System.Collections.Generic.List[object]]::new()
while(-not $playerProcess.WaitForExit(1000)){
 $playerProcess.Refresh()
 $samples.Add([pscustomobject]@{utc=[DateTime]::UtcNow.ToString('O');workingSetBytes=$playerProcess.WorkingSet64;privateBytes=$playerProcess.PrivateMemorySize64})
 if($samples.Count % 30 -eq 0){Write-Output "Player active: $($samples.Count) OS memory samples; captures $((Get-ChildItem -LiteralPath $outputPath -Filter '*.png').Count)"}
}
[pscustomobject]@{runId=$runId;pid=$playerProcess.Id;exitCode=$playerProcess.ExitCode;measurement='External PowerShell process counters sampled about once per second; entire review run, not camera-specific';samples=$samples.ToArray()} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outputPath 'os-memory.json') -Encoding utf8
Write-Output "Player exit $($playerProcess.ExitCode)"
exit $playerProcess.ExitCode
