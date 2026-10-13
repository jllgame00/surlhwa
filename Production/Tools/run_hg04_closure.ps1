param([ValidateSet('paths','recovery')][string]$Group='paths',[ValidatePattern('^[a-z][a-z0-9-]+$')][string]$Prefix='hg04-final')
$ErrorActionPreference='Stop'
$runner=Join-Path $PSScriptRoot 'run_slice_proof.ps1'
$storage='C:/Users/surlw/.codex/visualizations/2026/10/10/01a1278f-2182-70e1-b942-d2436ca59b10/SliceValidation'
if($Group -eq 'paths'){
 $jobs=@(
  @{Fixture='save-error';Height=720;Name="$Prefix-save-error-720"},
  @{Fixture='missing-actor';Height=720;Name="$Prefix-missing-actor-720"},
  @{Fixture='dry';Height=1080;Name="$Prefix-dry-1080"},
  @{Fixture='seolya';Height=720;Name="$Prefix-seolya-720"},
  @{Fixture='manhattan';Height=1080;Name="$Prefix-manhattan-1080-no-media"},
  @{Fixture='manhattan';Height=720;Name="$Prefix-manhattan-media-720";Media=$true},
  @{Fixture='weak';Height=720;Name="$Prefix-weak-720"},
  @{Fixture='unknown';Height=720;Name="$Prefix-unknown-720"},
  @{Fixture='lifecycle';Height=720;Name="$Prefix-lifecycle-720"}
 )
 foreach($job in $jobs){
  $params=@('-NoProfile','-File',$runner,'-Fixture',$job.Fixture,'-Height',$job.Height,'-EvidenceName',$job.Name,'-SaveRoot',"$storage/$($job.Name)")
  if($job.Media){$params+='-Media'}
  & pwsh @params
  if($LASTEXITCODE -ne 0){throw "Player evidence failed: $($job.Name)"}
 }
}else{
 foreach($phase in @('intro','brew','result','farewell')){
  $name="$Prefix-checkpoint-$phase"; $save="$storage/$name"
  $params=@('-NoProfile','-File',$runner,'-Fixture',$(if($phase -eq 'brew'){'checkpoint-brew'}else{'manhattan'}),'-EvidenceName',$name,'-SaveRoot',$save)
  if($phase -ne 'brew'){$params+=@('-StopAt',$phase)}
  & pwsh @params
  if($LASTEXITCODE -ne 0){throw "Checkpoint evidence failed: $phase"}
  & pwsh -NoProfile -File $runner -Fixture continue -EvidenceName "$Prefix-continue-$phase" -SaveRoot $save
  if($LASTEXITCODE -ne 0){throw "Continue evidence failed: $phase"}
 }
 & pwsh -NoProfile -File $runner -Fixture continue -EvidenceName "$Prefix-continue-complete" -SaveRoot "$storage/$Prefix-dry-1080"
 if($LASTEXITCODE -ne 0){throw 'Completed-run Continue failed'}
}

