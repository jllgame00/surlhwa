param(
 [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9_.-]+$')][string]$EvidenceName,
 [ValidateSet(720,1080)][int]$Height=720,
 [ValidateSet(2,3,4)][int]$Act=3,
 [ValidateSet('New','Prior','Legacy','Checkpoint')][string]$Start='New',
 [string]$CheckpointRoot='',
 [switch]$FailHanky,
 [switch]$QuixoteMismatch,
 [switch]$FailFrench,
 [switch]$HamletMismatch,
 [switch]$Media,
 [ValidateRange(60,7200)][int]$TimeoutSeconds=1800
)
$ErrorActionPreference='Stop'
if($Act -eq 2 -and ($Start -eq 'Prior' -or $QuixoteMismatch)){throw 'ACT 2 uses Legacy or Checkpoint; Quixote is ACT 3 only.'}
if($FailHanky -and $QuixoteMismatch){throw 'Use separate runs for required discovery retry and Quixote mismatch.'}
if(($FailFrench -or $HamletMismatch) -and $Act -ne 4){throw 'French discovery and Hamlet mismatch require ACT 4.'}
if(($FailFrench -or $HamletMismatch) -and ($FailHanky -or $QuixoteMismatch)){throw 'Keep ACT 4 fixtures separate from earlier ACT fixtures.'}
$productionPath=Split-Path $PSScriptRoot -Parent
$outputPath=Join-Path $productionPath "Validation/Evidence/$EvidenceName"
$executablePath=Join-Path $productionPath "Validation/Builds/Act${Act}Production/SurlhwaAct${Act}.exe"
$validator=Join-Path $PSScriptRoot 'verify_campaign_proof.py'
if(-not(Test-Path -LiteralPath $executablePath)){throw 'Build the selected campaign Player before running the proof.'}
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
$historyPath=Join-Path $outputPath 'history'
$legacyPath=Join-Path $historyPath 'campaign-act1'
$priorNamespace='campaign-through-act-{0:D2}' -f ($Act-1)
$priorPath=Join-Path $historyPath $priorNamespace
New-Item -ItemType Directory -Path $legacyPath | Out-Null
foreach($historyAct in 2..([Math]::Max(2,$Act-1))){
 New-Item -ItemType Directory -Path (Join-Path $historyPath ('campaign-through-act-{0:D2}' -f $historyAct)) | Out-Null
}
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
if($Start -eq 'Prior' -or $Start -eq 'Legacy'){
 $historySlot=Join-Path $(if($Start -eq 'Prior'){$priorPath}else{$legacyPath}) 'slot1'
 New-Item -ItemType Directory -Path $historySlot | Out-Null
 foreach($generation in $hashes.Keys){Copy-Item -LiteralPath (Join-Path $originSlot $generation) -Destination (Join-Path $historySlot $generation)}
}
$fixture=if($Start -eq 'New'){'open'}else{'continue'}
if($FailHanky){$fixture=if($Start -eq 'New'){'hanky-retry'}else{'continue-hanky-retry'}}
if($QuixoteMismatch){$fixture=if($Start -eq 'New'){'quixote-mismatch'}else{'continue-quixote-mismatch'}}
if($FailFrench){$fixture+='-french-retry'}
if($HamletMismatch){$fixture+='-hamlet-mismatch'}
$width=[int]($Height*16/9)
$arguments=@('-batchmode','-force-d3d11','-screen-fullscreen','0','-screen-width',[string]$width,'-screen-height',[string]$Height,
 '-campaignEvidenceRoot',$outputPath,'-campaignSaveRoot',$savePath,'-campaignProof',$fixture,'-logFile',(Join-Path $outputPath 'Player.log'))
$arguments+=@('-campaignHistoryRoot',$historyPath,'-campaignLegacySaveRoot',$legacyPath)
if($Media){$arguments+=@('-campaignMedia','1')}
& python $validator inventory (Join-Path $outputPath 'inventory.json') --act $Act
if($LASTEXITCODE -ne 0){throw "ACT $Act source/Player inventory failed"}
[ordered]@{startedUtc=[DateTime]::UtcNow.ToString('o');width=$width;height=$Height;act=$Act;start=$Start;fixture=$fixture;executableSha256=(Get-FileHash -LiteralPath $executablePath).Hash;arguments=$arguments;tier='Simulated production InputSystem; actual Player camera/UI rendering; NOT native OS input'}|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $outputPath 'expected-run.json') -Encoding utf8
$quotedArguments=$arguments|ForEach-Object {'"'+($_ -replace '(\\*)"','$1$1\"' -replace '(\\+)$','$1$1')+'"'}
$playerProcess=Start-Process -FilePath $executablePath -ArgumentList $quotedArguments -WindowStyle Hidden -PassThru
Write-Output "ACT $Act $fixture ${Height}p PID $($playerProcess.Id)"
$clock=[Diagnostics.Stopwatch]::StartNew();$samples=@();$timedOut=$false
while(-not $playerProcess.HasExited){
 $playerProcess.Refresh();if(-not $playerProcess.HasExited){$samples+=$playerProcess.WorkingSet64}
 if($clock.Elapsed.TotalSeconds -gt $TimeoutSeconds){$timedOut=$true;$playerProcess.Kill();break}
 Start-Sleep -Milliseconds 500
}
$playerProcess.WaitForExit()
[ordered]@{exitCode=$playerProcess.ExitCode;timedOut=$timedOut;seconds=$clock.Elapsed.TotalSeconds;maxWorkingSetBytes=($samples|Measure-Object -Maximum).Maximum}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $outputPath 'process.json') -Encoding utf8
& python $validator verify-inventory (Join-Path $outputPath 'inventory.json') --act $Act
if($LASTEXITCODE -ne 0){throw 'Source/Player changed during proof; preserve this attempt as incomplete evidence.'}
if($playerProcess.ExitCode -ne 0 -or $timedOut){throw "ACT $Act proof incomplete: exit $($playerProcess.ExitCode), timeout $timedOut. Evidence: $outputPath"}
& python $validator verify $outputPath --act $Act
if($LASTEXITCODE -ne 0){throw "ACT $Act evidence completeness failed: $outputPath"}
Write-Output "ACT $Act simulated-input proof verified: $outputPath"
