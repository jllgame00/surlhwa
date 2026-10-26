param(
 [ValidateSet('Import','Tests','Build')][string]$Operation='Tests',
 [string]$EvidenceName='unity-tests',
 [string]$TestFilter='',
 [string]$BuildMethod='Surlhwa.Editor.CampaignBuilder.BuildExistingPlayer'
)
$ErrorActionPreference='Stop'
$productionPath=Split-Path $PSScriptRoot -Parent
$unityPath=Join-Path $productionPath 'Unity'
$evidencePath=Join-Path $productionPath 'Validation/Evidence'
New-Item -ItemType Directory -Path $evidencePath -Force | Out-Null
$unityArguments=@('-batchmode','-nographics','-projectPath',$unityPath,'-logFile',(Join-Path $evidencePath "$EvidenceName.log"))
if($Operation -eq 'Tests'){
 $unityArguments+=@('-runTests','-testPlatform','EditMode','-testResults',(Join-Path $evidencePath "$EvidenceName.xml"))
 if($TestFilter){$unityArguments+=@('-testFilter',$TestFilter)}
}elseif($Operation -eq 'Build'){$unityArguments+=@('-quit','-executeMethod',$BuildMethod)}
else{$unityArguments+='-quit'}
$unityProcess=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/6000.3.7f1/Editor/Unity.exe' -ArgumentList $unityArguments -WindowStyle Hidden -PassThru
Write-Output "Production Unity $Operation PID $($unityProcess.Id)"
$unityProcess.WaitForExit()
Write-Output "Unity exit $($unityProcess.ExitCode)"
exit $unityProcess.ExitCode
