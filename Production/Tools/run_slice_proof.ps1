param(
 [ValidateSet('seolya','manhattan','dry','weak','unknown','checkpoint-brew','continue','lifecycle','missing-actor','save-error','ui-review')][string]$Fixture='manhattan',
 [string]$SaveRoot,
 [switch]$Media,
 [ValidateSet('intro','result','farewell')][string]$StopAt,
 [ValidateSet(720,1080)][int]$Height=720,
 [Parameter(Mandatory=$true)][ValidatePattern('^[a-z][a-z0-9-]+$')][string]$EvidenceName
)
$ErrorActionPreference='Stop'
$productionPath=Split-Path $PSScriptRoot -Parent
$outputPath=Join-Path $productionPath "Validation/Evidence/$EvidenceName"
if(Test-Path -LiteralPath $outputPath){throw "Evidence already exists: $outputPath"}
New-Item -ItemType Directory -Path $outputPath | Out-Null
$executablePath=Join-Path $productionPath 'Validation/Builds/Slice/SurlhwaSlice.exe'
$runId=[guid]::NewGuid().ToString('N')
$width=[int]($Height*16/9)
$expected=[ordered]@{runId=$runId;fixture=$Fixture;width=$width;height=$Height;executableSha256=(Get-FileHash -LiteralPath $executablePath -Algorithm SHA256).Hash.ToLowerInvariant();startedUtc=[DateTime]::UtcNow.ToString('o');tier='V4 simulated Input System events; V6 native offscreen camera/UI render'}
$expected['stopAt']=$StopAt
$expected['saveRoot']=if($SaveRoot){$SaveRoot}else{$outputPath}
$inventoryPath=Join-Path $outputPath 'inventory.json'
$inventorySummary=& python (Join-Path $PSScriptRoot 'slice_inventory.py') --write $inventoryPath
if($LASTEXITCODE -ne 0){throw 'Source/Player inventory failed'}
$expected['inventory']=$inventorySummary | ConvertFrom-Json
$expected | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'expected-run.json') -Encoding utf8
$arguments=@('-batchmode','-force-d3d11','-screen-fullscreen','0','-screen-width',$width,'-screen-height',$Height,'-sliceEvidenceRoot',$outputPath,'-sliceProof',$Fixture,'-sliceRunId',$runId,'-logFile',(Join-Path $outputPath 'Player.log'))
if($SaveRoot){$arguments+=@('-sliceSaveRoot',$SaveRoot)}
if($Media){$arguments+=@('-sliceMedia','1')}
if($StopAt){$arguments+=@('-sliceStopAt',$StopAt)}
$playerProcess=Start-Process -FilePath $executablePath -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Slice $Fixture ${Height}p proof PID $($playerProcess.Id)"
$clock=[Diagnostics.Stopwatch]::StartNew(); $samples=@(); $timedOut=$false; $firstReady=$null
while(-not $playerProcess.HasExited){
 $playerProcess.Refresh(); if(-not $playerProcess.HasExited){$samples+=$playerProcess.WorkingSet64}
 if($null -eq $firstReady -and (Test-Path -LiteralPath (Join-Path $outputPath 'observation.json'))){$firstReady=$clock.Elapsed.TotalSeconds}
 if($clock.Elapsed.TotalSeconds -gt 180){$timedOut=$true; $playerProcess.Kill(); break}
 Start-Sleep -Milliseconds 500
}
$playerProcess.WaitForExit()
& python (Join-Path $PSScriptRoot 'slice_inventory.py') --verify $inventoryPath
if($LASTEXITCODE -ne 0){throw 'Source/Player changed during proof'}
[ordered]@{runId=$runId;exitCode=$playerProcess.ExitCode;timedOut=$timedOut;seconds=$clock.Elapsed.TotalSeconds;firstObservationSeconds=$firstReady;workingSetSamples=$samples;maxWorkingSetBytes=($samples|Measure-Object -Maximum).Maximum} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $outputPath 'process.json') -Encoding utf8
Write-Output "Slice proof exit $($playerProcess.ExitCode): $outputPath"
exit $playerProcess.ExitCode
