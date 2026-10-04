param([Parameter(Mandatory=$true)][int]$ProcessId, [float]$X, [float]$Y)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Invoke-RcNativeInput.ps1') -ProcessId $ProcessId -Action Focus | Out-Null
$folder = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Logs/Review/RCAutomatedQA/standalone-native'))
$request = Join-Path $folder 'ui-device-input.json'
$ack = Join-Path $folder 'ui-device-ack.txt'
foreach ($edge in @(@(($X-3),$Y,$false),@($X,$Y,$false),@($X,$Y,$true),@($X,$Y,$false))) {
    if (Test-Path -LiteralPath $request) { throw 'Prior device packet is still pending.' }
    $id = [Guid]::NewGuid().ToString('N')
    $packet = @{ id=$id; x=[float]$edge[0]; y=[float]$edge[1]; pressed=[bool]$edge[2] } | ConvertTo-Json -Compress
    [IO.File]::WriteAllText($request+'.tmp', $packet)
    [IO.File]::Move($request+'.tmp', $request)
    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    do {
        Start-Sleep -Milliseconds 30
        $received = if(Test-Path -LiteralPath $ack) { [IO.File]::ReadAllText($ack) } else { '' }
        if([DateTime]::UtcNow -gt $deadline) { throw 'No UI device acknowledgement. Use the explicit --surlhwa-rc-ui-device QA flag.' }
    } until ($received -eq $id)
    Start-Sleep -Milliseconds 130
}
Write-Output "SIMULATED_POINTER_DEVICE click ($X,$Y); inspect live raycast, down/up and callback evidence."
