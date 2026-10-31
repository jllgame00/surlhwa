param(
 [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9_.-]+$')][string]$EvidenceName,
 [ValidateSet(720,1080)][int]$Height=720,
 [ValidateSet('New','Legacy','Checkpoint')][string]$Start='New',
 [string]$CheckpointRoot='',
 [switch]$FailHanky,
 [switch]$Media,
 [ValidateRange(60,7200)][int]$TimeoutSeconds=1800
)
$ErrorActionPreference='Stop'
$productionPath=Split-Path $PSScriptRoot -Parent
$outputPath=Join-Path $productionPath "Validation/Evidence/$EvidenceName"
$executablePath=Join-Path $productionPath 'Validation/Builds/Act2Production/SurlhwaAct2.exe'
$validator=Join-Path $PSScriptRoot 'verify_act2_proof.py'
if(-not(Test-Path -LiteralPath $executablePath)){throw 'Build the dedicated ACT 2 Player before running the proof.'}
if(Test-Path -LiteralPath $outputPath){throw "Preserve existing evidence: $outputPath"}
if($Start -eq 'New' -and $CheckpointRoot){throw 'New runs cannot take a checkpoint.'}
if($Start -ne 'New'){
 if(-not $CheckpointRoot){throw 'Continue requires an actually earned checkpoint root containing slot1/current.json.'}
 $checkpointPath=(Resolve-Path -LiteralPath $CheckpointRoot).Path
 if(-not(Test-Path -LiteralPath (Join-Path $checkpointPath 'slot1/current.json'))){throw 'No actual published current checkpoint.'}
}
New-Item -ItemType Directory -Path $outputPath | Out-Null
$savePath=Join-Path $outputPath 'save'
New-Item -ItemType Directory -Path $savePath | Out-Null
$legacyPath=Join-Path $outputPath 'legacy-empty'
New-Item -ItemType Directory -Path $legacyPath | Out-Null
if($Start -ne 'New'){
 $originPath=Join-Path $outputPath 'origin'
 $originSlot=Join-Path $originPath 'slot1'
 New-Item -ItemType Directory -Path $originSlot | Out-Null
 $hashes=[ordered]@{}
 foreach($generation in @('current.json','previous.json')){
  $source=Join-Path $checkpointPath "slot1/$generation"
  if(Test-Path -LiteralPath $source){
   $hashes[$generation]=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
   Copy-Item -LiteralPath $source -Destination (Join-Path $originSlot $generation)
  }
 }
 [ordered]@{source=$checkpointPath;files=$hashes;kind=$Start;note='Byte-identical copies of actual published generations; no constructed campaign state.'}|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $outputPath 'checkpoint-origin.json') -Encoding utf8
 if($Start -eq 'Checkpoint'){
  $saveSlot=Join-Path $savePath 'slot1'
  New-Item -ItemType Directory -Path $saveSlot | Out-Null
  foreach($generation in $hashes.Keys){Copy-Item -LiteralPath (Join-Path $originSlot $generation) -Destination (Join-Path $saveSlot $generation)}
 }
}
$fixture=if($Start -eq 'New'){'open'}else{'continue'}
if($FailHanky){$fixture=if($Start -eq 'New'){'hanky-retry'}else{'continue-hanky-retry'}}
$width=[int]($Height*16/9)
$arguments=@('-batchmode','-force-d3d11','-screen-fullscreen','0','-screen-width',[string]$width,'-screen-height',[string]$Height,
 '-campaignEvidenceRoot',$outputPath,'-campaignSaveRoot',$savePath,'-campaignProof',$fixture,'-logFile',(Join-Path $outputPath 'Player.log'))
$arguments+=@('-campaignLegacySaveRoot',$(if($Start -eq 'Legacy'){$originPath}else{$legacyPath}))
if($Media){$arguments+=@('-campaignMedia','1')}
& python $validator inventory (Join-Path $outputPath 'inventory.json')
if($LASTEXITCODE -ne 0){throw 'ACT 2 source/Player inventory failed'}
[ordered]@{startedUtc=[DateTime]::UtcNow.ToString('o');width=$width;height=$Height;start=$Start;fixture=$fixture;executableSha256=(Get-FileHash -LiteralPath $executablePath).Hash;arguments=$arguments;tier='Simulated production InputSystem; actual Player camera/UI rendering; NOT native OS input'}|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $outputPath 'expected-run.json') -Encoding utf8
$quotedArguments=$arguments|ForEach-Object {'"'+($_ -replace '(\\*)"','$1$1\"' -replace '(\\+)$','$1$1')+'"'}
$playerProcess=Start-Process -FilePath $executablePath -ArgumentList $quotedArguments -WindowStyle Hidden -PassThru
Write-Output "ACT 2 $fixture ${Height}p PID $($playerProcess.Id)"
$clock=[Diagnostics.Stopwatch]::StartNew();$samples=@();$timedOut=$false
while(-not $playerProcess.HasExited){
 $playerProcess.Refresh();if(-not $playerProcess.HasExited){$samples+=$playerProcess.WorkingSet64}
 if($clock.Elapsed.TotalSeconds -gt $TimeoutSeconds){$timedOut=$true;$playerProcess.Kill();break}
 Start-Sleep -Milliseconds 500
}
$playerProcess.WaitForExit()
[ordered]@{exitCode=$playerProcess.ExitCode;timedOut=$timedOut;seconds=$clock.Elapsed.TotalSeconds;maxWorkingSetBytes=($samples|Measure-Object -Maximum).Maximum}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $outputPath 'process.json') -Encoding utf8
& python $validator verify-inventory (Join-Path $outputPath 'inventory.json')
if($LASTEXITCODE -ne 0){throw 'Source/Player changed during proof; preserve this attempt as incomplete evidence.'}
if($playerProcess.ExitCode -ne 0 -or $timedOut){throw "ACT 2 proof incomplete: exit $($playerProcess.ExitCode), timeout $timedOut. Evidence: $outputPath"}
& python $validator verify $outputPath
if($LASTEXITCODE -ne 0){throw "ACT 2 evidence completeness failed: $outputPath"}
Write-Output "ACT 2 simulated-input proof verified: $outputPath"
