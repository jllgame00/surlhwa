param(
    [Parameter(Mandatory=$true)][int]$ProcessId,
    [ValidateSet('Focus','Click','Move','Wheel','Key','Close')][string]$Action = 'Focus',
    [float]$X, [float]$Y,
    [int]$ViewportWidth = 1280, [int]$ViewportHeight = 720,
    [int]$VirtualKey = 0x4D,
    [int]$WheelDelta = -120,
    [switch]$Topmost,
    [int]$HoldMilliseconds = 160
)
$ErrorActionPreference = 'Stop'
$process = Get-Process -Id $ProcessId
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Builds')) + [IO.Path]::DirectorySeparatorChar
if (-not $process.Path.StartsWith($root, [StringComparison]::OrdinalIgnoreCase) -or $process.ProcessName -ne 'SURLHWA') {
    throw 'Native QA input may only target this workspace RC player.'
}
if (-not ('RcWindowsInput' -as [type])) {
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class RcWindowsInput {
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int x,y; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int left,top,right,bottom; }
    [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT { public int dx,dy; public uint data,flags,time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT { public ushort vk,scan; public uint flags,time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Explicit)] struct UNION { [FieldOffset(0)] public MOUSEINPUT mouse; [FieldOffset(0)] public KEYBDINPUT key; }
    [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint type; public UNION value; }
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr window,int command);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint from,uint to,bool attach);
    [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr window);
    [DllImport("user32.dll")] static extern IntPtr SetFocus(IntPtr window);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr window,out RECT rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr window,ref POINT point);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT point);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] static extern uint MapVirtualKey(uint code,uint kind);
    [DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint count,INPUT[] inputs,int size);
    public static uint Mouse(bool down) {
        var input = new INPUT { type=0, value=new UNION { mouse=new MOUSEINPUT { flags=down?2u:4u } } };
        return SendInput(1,new[]{input},Marshal.SizeOf(typeof(INPUT)));
    }
    public static uint Key(ushort vk,bool down) {
        uint extended = vk>=0x21 && vk<=0x2E ? 1u : 0u;
        var input = new INPUT { type=1, value=new UNION { key=new KEYBDINPUT {
            scan=(ushort)MapVirtualKey(vk,0), flags=8u|extended|(down?0u:2u) } } };
        return SendInput(1,new[]{input},Marshal.SizeOf(typeof(INPUT)));
    }
    public static uint Wheel(int delta) {
        var input = new INPUT { type=0, value=new UNION { mouse=new MOUSEINPUT { data=unchecked((uint)delta),flags=0x800u } } };
        return SendInput(1,new[]{input},Marshal.SizeOf(typeof(INPUT)));
    }
    public static bool FocusPlayer(IntPtr window) {
        uint ignored; uint current=GetCurrentThreadId();
        uint target=GetWindowThreadProcessId(window,out ignored);
        uint foreground=GetWindowThreadProcessId(GetForegroundWindow(),out ignored);
        bool a=foreground!=0 && foreground!=current && AttachThreadInput(current,foreground,true);
        bool b=target!=current && target!=foreground && AttachThreadInput(current,target,true);
        try { ShowWindow(window,9); BringWindowToTop(window); SetForegroundWindow(window); SetFocus(window); }
        finally { if(b) AttachThreadInput(current,target,false); if(a) AttachThreadInput(current,foreground,false); }
        return GetForegroundWindow()==window;
    }
}
'@
}
[RcWindowsInput]::SetProcessDPIAware() | Out-Null
$window = $process.MainWindowHandle
if ($window -eq 0) { throw 'RC player has no visible main window.' }
if ($Topmost) { [RcWindowsInput]::SetWindowPos($window, [IntPtr](-1), 32, 32, 0, 0, 0x41) | Out-Null }
[RcWindowsInput]::ShowWindow($window, 9) | Out-Null
[RcWindowsInput]::SetForegroundWindow($window) | Out-Null
Start-Sleep -Milliseconds 150
if ([RcWindowsInput]::GetForegroundWindow() -ne $window) {
    [RcWindowsInput]::FocusPlayer($window) | Out-Null
    Start-Sleep -Milliseconds 150
}
if ([RcWindowsInput]::GetForegroundWindow() -ne $window) { throw 'RC window could not acquire foreground; no input sent.' }
if ($Action -eq 'Close') { $process.CloseMainWindow() | Out-Null; return }
if ($Action -eq 'Click' -or $Action -eq 'Move' -or $Action -eq 'Wheel') {
    if ($X -lt 0 -or $X -ge $ViewportWidth -or $Y -lt 0 -or $Y -ge $ViewportHeight) { throw 'Target is outside the player viewport.' }
    $rect = New-Object RcWindowsInput+RECT
    [RcWindowsInput]::GetClientRect($window, [ref]$rect) | Out-Null
    $point = New-Object RcWindowsInput+POINT
    $point.x = [int]($X * ($rect.right-$rect.left) / $ViewportWidth)
    $point.y = [int](($ViewportHeight-$Y) * ($rect.bottom-$rect.top) / $ViewportHeight)
    [RcWindowsInput]::ClientToScreen($window, [ref]$point) | Out-Null
    $pointerProcess = [uint32]0
    [RcWindowsInput]::GetWindowThreadProcessId([RcWindowsInput]::WindowFromPoint($point), [ref]$pointerProcess) | Out-Null
    if ($pointerProcess -ne $ProcessId) { throw "Pointer region belongs to process $pointerProcess; no pointer input sent." }
    [RcWindowsInput]::SetCursorPos($point.x-3, $point.y) | Out-Null
    Start-Sleep -Milliseconds 80
    [RcWindowsInput]::SetCursorPos($point.x, $point.y) | Out-Null
    Start-Sleep -Milliseconds 150
    if ($Action -eq 'Click') {
        $press = [RcWindowsInput]::Mouse($true)
        Start-Sleep -Milliseconds $HoldMilliseconds
        $release = [RcWindowsInput]::Mouse($false)
        [pscustomobject]@{Action=$Action; ProcessId=$ProcessId; PressAccepted=$press; ReleaseAccepted=$release; ScreenX=$point.x; ScreenY=$point.y}
    } elseif ($Action -eq 'Wheel') {
        [pscustomobject]@{Action=$Action; ProcessId=$ProcessId; Accepted=[RcWindowsInput]::Wheel($WheelDelta)}
    }
} elseif ($Action -eq 'Key') {
    $press = [RcWindowsInput]::Key([uint16]$VirtualKey, $true)
    Start-Sleep -Milliseconds $HoldMilliseconds
    $release = [RcWindowsInput]::Key([uint16]$VirtualKey, $false)
    [pscustomobject]@{Action=$Action; ProcessId=$ProcessId; VirtualKey=$VirtualKey; PressAccepted=$press; ReleaseAccepted=$release}
} else { [pscustomobject]@{Action=$Action; ProcessId=$ProcessId; Foreground=$true} }
