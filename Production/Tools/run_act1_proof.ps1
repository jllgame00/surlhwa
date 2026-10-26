param([string]$EvidenceName,[ValidateSet(720,1080)][int]$Height=720,[string]$Fixture='open',[string]$SaveRoot='',[switch]$Media)
$ErrorActionPreference='Stop'
$productionPath=Split-Path $PSScriptRoot -Parent
$outputPath=Join-Path $productionPath "Validation/Evidence/$EvidenceName"
if(Test-Path -LiteralPath $outputPath){throw "Preserve existing evidence: $outputPath"}
New-Item -ItemType Directory -Path $outputPath | Out-Null
$executablePath=Join-Path $productionPath 'Validation/Builds/Q1Production/SurlhwaQ1.exe'
$width=[int]($Height*16/9)
$arguments=@('-batchmode','-force-d3d11','-screen-fullscreen','0','-screen-width',$width,'-screen-height',$Height,'-campaignEvidenceRoot',$outputPath,'-campaignProof',$Fixture,'-logFile',(Join-Path $outputPath 'Player.log'))
if($SaveRoot){$arguments+=@('-campaignSaveRoot',$SaveRoot)}
if($Media){$arguments+=@('-campaignMedia','1')}
& python (Join-Path $PSScriptRoot 'act1_inventory.py') write (Join-Path $outputPath 'inventory.json')
if($LASTEXITCODE -ne 0){throw 'Inventory failed'}
[ordered]@{startedUtc=[DateTime]::UtcNow.ToString('o');width=$width;height=$Height;fixture=$Fixture;executableSha256=(Get-FileHash -LiteralPath $executablePath).Hash;arguments=$arguments;tier='Simulated production InputSystem; native offscreen camera/UI rendering; NOT OS input'}|ConvertTo-Json|Set-Content (Join-Path $outputPath 'expected-run.json')
$playerProcess=Start-Process -FilePath $executablePath -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "ACT 1 $Fixture ${Height}p PID $($playerProcess.Id)"
$clock=[Diagnostics.Stopwatch]::StartNew();$samples=@();$timedOut=$false
while(-not $playerProcess.HasExited){
 $playerProcess.Refresh();if(-not $playerProcess.HasExited){$samples+=$playerProcess.WorkingSet64}
 if($clock.Elapsed.TotalSeconds -gt 900){$timedOut=$true;$playerProcess.Kill();break}
 Start-Sleep -Milliseconds 500
}
$playerProcess.WaitForExit()
& python (Join-Path $PSScriptRoot 'act1_inventory.py') verify (Join-Path $outputPath 'inventory.json')
if($LASTEXITCODE -ne 0){throw 'Source/Player changed during proof'}
[ordered]@{exitCode=$playerProcess.ExitCode;timedOut=$timedOut;seconds=$clock.Elapsed.TotalSeconds;maxWorkingSetBytes=($samples|Measure-Object -Maximum).Maximum}|ConvertTo-Json|Set-Content (Join-Path $outputPath 'process.json')
Write-Output "ACT 1 proof exit $($playerProcess.ExitCode): $outputPath"
exit $playerProcess.ExitCode
