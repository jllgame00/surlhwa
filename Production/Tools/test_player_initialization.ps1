param([string]$EvidenceName='initialization-mismatch')
$ErrorActionPreference='Stop'
$productionPath=Split-Path $PSScriptRoot -Parent
$authoredPath=Join-Path $productionPath 'Content/spatial-proof.json'
$originalBytes=[IO.File]::ReadAllBytes($authoredPath)
$originalHash=(Get-FileHash -LiteralPath $authoredPath -Algorithm SHA256).Hash.ToLowerInvariant()
$outputPath=Join-Path $productionPath "Validation/Evidence/$EvidenceName"
if(Test-Path -LiteralPath $outputPath){throw "Regression evidence already exists: $outputPath"}
try {
 $changed=[Text.Encoding]::UTF8.GetString($originalBytes).Replace('"visualBindingId": "visual_bartender"','"visualBindingId": "visual_ordinary"')
 if($changed -eq [Text.Encoding]::UTF8.GetString($originalBytes)){throw 'Binding mismatch probe did not change its intended source field'}
 [IO.File]::WriteAllText($authoredPath,$changed,[Text.UTF8Encoding]::new($false))
 & (Join-Path $PSScriptRoot 'run_unity.ps1') -Operation Build -EvidenceName "$EvidenceName-build"
 if($LASTEXITCODE -ne 0){throw 'Regression Player build failed'}
 & (Join-Path $PSScriptRoot 'run_player_review.ps1') -EvidenceName $EvidenceName
 $processExit=$LASTEXITCODE
 $run=Get-Content -LiteralPath (Join-Path $outputPath 'player-run.json') -Raw | ConvertFrom-Json
 if($processExit -eq 0 -or $run.initialized -or $run.captures.Count -ne 0 -or -not ($run.errors -match 'Authored guest/visual binding mismatch')){
  throw 'Failed initialization was not rejected before rendering'
 }
 & python (Join-Path $PSScriptRoot 'inspect_captures.py') $outputPath
 if($LASTEXITCODE -ne 1){throw 'Capture inspector did not reject the failed initialization'}
 $inspection=Get-Content -LiteralPath (Join-Path $productionPath 'Validation/Manifests/captures.json') -Raw | ConvertFrom-Json
 [pscustomobject]@{result='PASS';regression='Valid JSON with a declared but wrong bartender visual binding';originalAuthoredSha256=$originalHash;playerExitCode=$processExit;initialized=$run.initialized;captureCount=$run.captures.Count;collectedErrors=$run.errors;inspectorResult=$inspection.result;inspectorFailures=$inspection.failures;evidencePath=$outputPath} |
  ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $productionPath 'Validation/Manifests/initialization-regression.json') -Encoding utf8
 Write-Output 'INITIALIZATION REGRESSION PASS: nonzero Player exit, startup exception collected, zero captures, inspector FAIL'
} finally {
 [IO.File]::WriteAllBytes($authoredPath,$originalBytes)
 if((Get-FileHash -LiteralPath $authoredPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $originalHash){throw 'Failed to restore the Production authored probe bytes'}
 Write-Output 'Original Production authored JSON bytes restored. Rebuild the valid Player before positive captures.'
}
