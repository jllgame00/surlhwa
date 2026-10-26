param(
 [ValidateSet('New','First','Warmth','Clarity','QuietClarity','OpenMemory','QuietMemory')][string]$Start='New',
 [ValidateSet(720,1080)][int]$Height=1080
)
$ErrorActionPreference='Stop'
$productionPath=Split-Path $PSScriptRoot -Parent
$reviewPath=Join-Path $productionPath ('Validation/Evidence/gate2-q1-owner-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
if(Test-Path -LiteralPath $reviewPath){throw 'Review folder already exists'}
$savePath=Join-Path $reviewPath 'save'
New-Item -ItemType Directory -Path $savePath | Out-Null
$sourcePath=$null
if($Start -in @('First','Warmth','Clarity')) {
 $sourcePath=Join-Path $productionPath "Validation/Evidence/gate2-q1-open-720-03/checkpoints/prince-intro-$Start/slot1"
} elseif($Start -eq 'QuietClarity') {
 $sourcePath=Join-Path $productionPath 'Validation/Evidence/gate2-q1-quiet-1080-05/checkpoints/prince-intro-Clarity/slot1'
} elseif($Start -eq 'OpenMemory') {
 $sourcePath=Join-Path $productionPath 'Validation/Evidence/gate2-q1-open-720-03/slot1'
} elseif($Start -eq 'QuietMemory') {
 $sourcePath=Join-Path $productionPath 'Validation/Evidence/gate2-q1-quiet-save-05/slot1'
}
if($sourcePath) {
 if(-not(Test-Path -LiteralPath (Join-Path $sourcePath 'current.json'))){throw 'Earned review checkpoint is unavailable; generate the Gate 2 evidence first.'}
 $slotPath=Join-Path $savePath 'slot1'
 New-Item -ItemType Directory -Path $slotPath | Out-Null
 foreach($file in @('current.json','previous.json')) {
  $sourceFile=Join-Path $sourcePath $file
  if(Test-Path -LiteralPath $sourceFile){Copy-Item -LiteralPath $sourceFile -Destination (Join-Path $slotPath $file)}
 }
 Get-ChildItem -LiteralPath $sourcePath -File | Get-FileHash | Select-Object Path,Hash | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $reviewPath 'checkpoint-origin.json') -Encoding utf8
}
$executablePath=Join-Path $productionPath 'Validation/Builds/Q1Production/SurlhwaQ1.exe'
$arguments=@('-force-d3d11','-screen-fullscreen','0','-screen-width',[int]($Height*16/9),'-screen-height',$Height,'-campaignSaveRoot',$savePath,'-logFile',(Join-Path $reviewPath 'Player.log'))
[ordered]@{start=$Start;saveRoot=$savePath;executableSha256=(Get-FileHash -LiteralPath $executablePath).Hash;tier='Interactive owner review; no simulated-input/capture arguments';arguments=$arguments}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $reviewPath 'review-run.json') -Encoding utf8
Write-Output "Isolated Q1 review save: $savePath"
Write-Output 'Choose New Game for New; choose Continue for earned checkpoint starts.'
Start-Process -FilePath $executablePath -ArgumentList $arguments
