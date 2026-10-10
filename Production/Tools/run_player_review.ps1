param([string]$EvidenceName='captures')
$ErrorActionPreference='Stop'
$productionPath=Split-Path $PSScriptRoot -Parent
$outputPath=Join-Path $productionPath "Validation/Evidence/$EvidenceName"
$logPath=Join-Path $productionPath "Validation/Evidence/$EvidenceName-player.log"
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$playerProcess=Start-Process -FilePath (Join-Path $productionPath 'Validation/Builds/Windows/SurlhwaSpatialProof.exe') -ArgumentList @(
 '-batchmode','-force-d3d11','-screen-width','1920','-screen-height','1080','-reviewCapture','-reviewOutput',$outputPath,'-logFile',$logPath
) -WindowStyle Hidden -PassThru
Write-Output "Production review Player PID $($playerProcess.Id)"
$samples=[System.Collections.Generic.List[object]]::new()
while(-not $playerProcess.WaitForExit(1000)){
 $playerProcess.Refresh()
 $samples.Add([pscustomobject]@{utc=[DateTime]::UtcNow.ToString('O');workingSetBytes=$playerProcess.WorkingSet64;privateBytes=$playerProcess.PrivateMemorySize64})
 if($samples.Count % 30 -eq 0){Write-Output "Player active: $($samples.Count) OS memory samples; captures $((Get-ChildItem -LiteralPath $outputPath -Filter '*.png').Count)"}
}
[pscustomobject]@{pid=$playerProcess.Id;exitCode=$playerProcess.ExitCode;measurement='External PowerShell process counters sampled about once per second; entire review run, not camera-specific';samples=$samples.ToArray()} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outputPath 'os-memory.json') -Encoding utf8
Write-Output "Player exit $($playerProcess.ExitCode)"
exit $playerProcess.ExitCode
