param([string]$Configuration='Debug')
$ErrorActionPreference='Stop'
$productionPath=Split-Path $PSScriptRoot -Parent
$packageRoots=@((Join-Path $productionPath 'Unity/Library/PackageCache'),'D:/univ/surlhwa/Surlhwa/Library/PackageCache')
function Find-LocalAssembly([string]$Pattern,[string]$Relative) {
 foreach($packageRoot in $packageRoots){
  $package=Get-ChildItem -LiteralPath $packageRoot -Directory -ErrorAction SilentlyContinue | Where-Object{$_.Name -like $Pattern} | Select-Object -First 1
  if($package){$file=Join-Path $package.FullName $Relative;if(Test-Path -LiteralPath $file){return $file}}
 }
 throw "Missing local assembly $Pattern/$Relative. Resolve the new Unity project's packages first."
}
$newtonsoftAssembly=Find-LocalAssembly 'com.unity.nuget.newtonsoft-json@*' 'Runtime/Newtonsoft.Json.dll'
$nunitAssembly=Find-LocalAssembly 'com.unity.ext.nunit@*' 'net40/unity-custom/nunit.framework.dll'
Write-Output 'Evidence tier: standalone .NET; no Unity runtime or rendered-quality claim.'
& dotnet run --project (Join-Path $PSScriptRoot 'FoundationChecks/FoundationChecks.csproj') --configuration $Configuration --property:NewtonsoftAssembly="$newtonsoftAssembly" --property:NunitAssembly="$nunitAssembly"
exit $LASTEXITCODE

