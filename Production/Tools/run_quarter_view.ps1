param(
 [Parameter(Mandatory=$true)][string]$EvidenceName,
 [ValidateSet('Ordinary','Prince')][string]$Actor='Ordinary',
 [switch]$Preview,
 [switch]$Interactive
)
$ErrorActionPreference='Stop'
$productionPath=Split-Path $PSScriptRoot -Parent
if($EvidenceName -notmatch '^quarter-[a-zA-Z0-9_-]+$'){throw 'Use a fresh quarter-* evidence name without path separators.'}
$outputPath=Join-Path $productionPath "Validation/Evidence/$EvidenceName"
if(Test-Path -LiteralPath $outputPath){throw "Preserve existing evidence: $outputPath"}
$executablePath=Join-Path $productionPath 'Validation/Builds/QuarterView/SurlhwaQuarterView.exe'
if(-not (Test-Path -LiteralPath $executablePath)){throw 'Build QuarterViewBuilder.Build in licensed Unity first.'}
New-Item -ItemType Directory -Path $outputPath | Out-Null
$savePath=Join-Path $outputPath 'save'
New-Item -ItemType Directory -Path $savePath | Out-Null
if($Actor -eq 'Prince'){
 $sourcePath=Join-Path $productionPath 'Validation/Evidence/gate2-player-art-open-720-final/checkpoints/before-service-10/slot1'
 $slotPath=Join-Path $savePath 'slot1'
 New-Item -ItemType Directory -Path $slotPath | Out-Null
 foreach($file in @('current.json','previous.json')){Copy-Item -LiteralPath (Join-Path $sourcePath $file) -Destination (Join-Path $slotPath $file)}
 Get-ChildItem -LiteralPath $sourcePath -File | Get-FileHash | Select-Object Path,Hash | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'checkpoint-origin.json') -Encoding utf8
}
$arguments=@('-force-d3d11','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-campaignSaveRoot',$savePath,'-logFile',(Join-Path $outputPath 'Player.log'))
if(-not $Interactive){$arguments+=@('-batchmode','-quarterCapture',$outputPath,'-quarterActor',$Actor)}
if($Preview){$arguments+=@('-quarterPreview','1')}
& python (Join-Path $PSScriptRoot 'quarter_inventory.py') write (Join-Path $outputPath 'inventory.json')
if($LASTEXITCODE -ne 0){throw 'Source/Player inventory failed'}
[ordered]@{startedUtc=[DateTime]::UtcNow.ToString('o');actor=$Actor;executableSha256=(Get-FileHash -LiteralPath $executablePath).Hash;arguments=$arguments;tier='Actual ACT 1; presentation comparison; isolated save; explicit offscreen URP renders; not native input validation'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'expected-run.json') -Encoding utf8
if($Interactive){Start-Process -FilePath $executablePath -ArgumentList $arguments;exit 0}
$playerProcess=Start-Process -FilePath $executablePath -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Quarter View $Actor PID $($playerProcess.Id)"
$clock=[Diagnostics.Stopwatch]::StartNew();$timedOut=$false
while(-not $playerProcess.HasExited){
 if($clock.Elapsed.TotalSeconds -gt 480){$timedOut=$true;$playerProcess.Kill();break}
 Start-Sleep -Milliseconds 250
}
$playerProcess.WaitForExit()
& python (Join-Path $PSScriptRoot 'quarter_inventory.py') verify (Join-Path $outputPath 'inventory.json')
if($LASTEXITCODE -ne 0){throw 'Source/Player changed during capture'}
[ordered]@{exitCode=$playerProcess.ExitCode;timedOut=$timedOut;seconds=$clock.Elapsed.TotalSeconds} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'process.json') -Encoding utf8
if($timedOut){throw 'Quarter View capture timeout; partial evidence retained.'}
exit $playerProcess.ExitCode
