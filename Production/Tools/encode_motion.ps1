param([string]$EvidenceName='captures-final', [string[]]$CameraVariants=@('OrthographicA','PerspectiveB'))
$ErrorActionPreference='Stop'
$productionPath=Split-Path $PSScriptRoot -Parent
$outputPath=Join-Path $productionPath "Validation/Evidence/$EvidenceName"
$clips=[System.Collections.Generic.List[object]]::new()
foreach($cameraVariant in $CameraVariants){foreach($renderWidth in @(1280,1920)){
 $sourceFrames=Join-Path $outputPath "${cameraVariant}_motion_${renderWidth}/%04d.png"
 $destinationClip=Join-Path $outputPath "${cameraVariant}_motion_${renderWidth}.mp4"
 & ffmpeg -hide_banner -loglevel error -y -framerate 12 -i $sourceFrames -c:v libx264 -crf 18 -pix_fmt yuv420p -movflags +faststart $destinationClip
 if($LASTEXITCODE -ne 0){throw "Motion encoding failed: $destinationClip"}
 $probe=& ffprobe -v error -select_streams v:0 -show_entries 'stream=width,height,nb_frames,r_frame_rate,duration' -of json $destinationClip
 if($LASTEXITCODE -ne 0){throw "Motion probe failed: $destinationClip"}
 $stream=($probe | ConvertFrom-Json).streams[0]
 if($stream.width -ne $renderWidth -or $stream.height -ne ($renderWidth*9/16) -or [int]$stream.nb_frames -ne 121){throw "Motion stream mismatch: $destinationClip"}
 $clips.Add([pscustomobject]@{file=(Split-Path $destinationClip -Leaf);sha256=(Get-FileHash -LiteralPath $destinationClip -Algorithm SHA256).Hash.ToLowerInvariant();stream=$stream})
}}
$clips.ToArray() | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outputPath 'motion-clips.json') -Encoding utf8
Write-Output "$($clips.Count) native motion clips encoded/probed: 121 frames each, 12fps, original resolution."
