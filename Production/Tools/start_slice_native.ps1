param([Parameter(Mandatory=$true)][ValidatePattern('^[a-z][a-z0-9-]+$')][string]$EvidenceName)
$ErrorActionPreference='Stop'
$productionPath=Split-Path $PSScriptRoot -Parent
$executablePath=Join-Path $productionPath 'Validation/Builds/Slice/SurlhwaSlice.exe'
if(Get-Process -Name SurlhwaSlice -ErrorAction SilentlyContinue){throw 'An existing Slice Player must be closed before a native attempt'}
$outputPath=Join-Path $productionPath "Validation/Evidence/$EvidenceName"
if(Test-Path -LiteralPath $outputPath){throw "Preserve existing attempt: $outputPath"}
New-Item -ItemType Directory -Path $outputPath | Out-Null
$saveRoot="C:/Users/surlw/.codex/visualizations/2026/10/10/01a1278f-2182-70e1-b942-d2436ca59b10/SliceValidation/$EvidenceName"
$runId=[guid]::NewGuid().ToString('N')
& python (Join-Path $PSScriptRoot 'slice_inventory.py') --write (Join-Path $outputPath 'inventory.json')
if($LASTEXITCODE -ne 0){throw 'Native attempt inventory failed'}
$arguments=@('-force-d3d11','-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-sliceEvidenceRoot',$outputPath,'-sliceSaveRoot',$saveRoot,'-sliceRunId',$runId,'-logFile',(Join-Path $outputPath 'Player.log'))
# Explicitly requested interactive/native check: visible Player, observational probe only.
$playerProcess=Start-Process -FilePath $executablePath -ArgumentList $arguments -WindowStyle Normal -PassThru
[ordered]@{runId=$runId;processId=$playerProcess.Id;executable=$executablePath;saveRoot=$saveRoot;startedUtc=[DateTime]::UtcNow.ToString('o');tier='V5 native OS input; observational probe only';status='IN_PROGRESS';width=1280;height=720} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'native-attempt.json') -Encoding utf8
Write-Output "Native Slice Player PID $($playerProcess.Id); evidence $outputPath"
$playerProcess.WaitForExit()
[ordered]@{processId=$playerProcess.Id;exitCode=$playerProcess.ExitCode;endedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'process-ended.json') -Encoding utf8
