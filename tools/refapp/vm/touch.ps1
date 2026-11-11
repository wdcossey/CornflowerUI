# Windows only: runs on the reference VM, in its Windows PowerShell, not on the machine that drives it.
# Runs on the reference VM in the signed-in desktop session, started by session.ps1 -Probe.
#
# Plays a probe's gestures.txt on the emulator's display as mouse input, which the emulator turns into touches
# (its default input mode). The file's first line is "view <width> <height>", the app's window in view pixels;
# then one step per line, in view pixels in window coordinates:
#   down <x> <y>    move there and press
#   move <x> <y>    move there, pressed or not
#   up <x> <y>      move there and release
#   wait <ms>
# A view point lands on the display window (class OPWindowClass), which shows the phone's whole display at the
# window's zoom, so one window pixel is (view width / window width) view pixels: about 0.82 at the zoom that
# fits a 1440-high screen. The app logs the positions it actually got.
#
# -Title picks the emulator by its profile's name (its XDE process's /displayName), since more than one can be
# running, one per profile, and their windows are all titled "XDE". -Mode post (the default) posts the mouse messages to that window, so it needn't be in front;
# -Mode real moves the real cursor and clicks, which needs the window in front and unobscured.
# -RectOnly brings the display window to the front, prints "rect <left> <top> <width> <height>" (its client area on the
# screen) and plays nothing: session.ps1 -Record records that area.
param(
    [Parameter(Mandatory = $true)][string]$Gestures,
    [Parameter(Mandatory = $true)][string]$Log,
    [string]$Title = '',
    [ValidateSet('post', 'real')][string]$Mode = 'post',
    [switch]$RectOnly
)

$ErrorActionPreference = 'Stop'
Add-Type @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public static class XdeMouse {
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc f, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr p, EnumProc f, IntPtr l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr h, uint flags);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr h, ref POINT p);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint flags, int dx, int dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();

    const uint LEFTDOWN = 0x0002, LEFTUP = 0x0004;
    const uint WM_MOUSEMOVE = 0x0200, WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202;
    const int MK_LBUTTON = 0x0001;

    static string Cls(IntPtr h) { var s = new StringBuilder(256); GetClassName(h, s, 256); return s.ToString(); }
    static string Text(IntPtr h) { var s = new StringBuilder(512); GetWindowText(h, s, 512); return s.ToString(); }

    /// <summary>Every visible display window, with the id of the process that owns it.</summary>
    public static List<KeyValuePair<IntPtr, uint>> All() {
        var found = new List<KeyValuePair<IntPtr, uint>>();
        EnumWindows((top, l) => {
            EnumChildWindows(top, (c, l2) => {
                if (Cls(c) == "OPWindowClass" && IsWindowVisible(c)) {
                    uint pid;
                    GetWindowThreadProcessId(c, out pid);
                    found.Add(new KeyValuePair<IntPtr, uint>(c, pid));
                }
                return true;
            }, IntPtr.Zero);
            return true;
        }, IntPtr.Zero);
        return found;
    }

    /// <summary>The display window's client area: left and top on the screen, width, height.</summary>
    public static int[] Client(IntPtr h) {
        RECT r; GetClientRect(h, out r);
        var p = new POINT();
        ClientToScreen(h, ref p);
        return new[] { p.X, p.Y, r.R - r.L, r.B - r.T };
    }

    static IntPtr Point(int x, int y) { return (IntPtr)((y << 16) | (x & 0xFFFF)); }

    // Posted: client coordinates, the button state in wParam.
    static bool _down;
    public static void PostMove(IntPtr h, int x, int y) { PostMessage(h, WM_MOUSEMOVE, (IntPtr)(_down ? MK_LBUTTON : 0), Point(x, y)); }
    public static void PostDown(IntPtr h, int x, int y) { _down = true; PostMessage(h, WM_LBUTTONDOWN, (IntPtr)MK_LBUTTON, Point(x, y)); }
    public static void PostUp(IntPtr h, int x, int y) { _down = false; PostMessage(h, WM_LBUTTONUP, IntPtr.Zero, Point(x, y)); }

    // Real: screen coordinates.
    public static void Front(IntPtr h) { SetForegroundWindow(GetAncestor(h, 2)); }
    public static void RealMove(int x, int y) { SetCursorPos(x, y); }
    public static void RealDown() { mouse_event(LEFTDOWN, 0, 0, 0, UIntPtr.Zero); }
    public static void RealUp() { mouse_event(LEFTUP, 0, 0, 0, UIntPtr.Zero); }
}
'@

[void][XdeMouse]::SetProcessDPIAware()
function Log([string]$Text) { Add-Content $Log "$(Get-Date -Format 'HH:mm:ss.fff') $Text" }
Set-Content $Log "$(Get-Date -Format s) touch: $Gestures, mode $Mode, title '$Title'"

$xde = @(Get-CimInstance Win32_Process -Filter "Name='XDE.exe'" | Where-Object { $_.CommandLine -like "*/displayName ""$Title""*" })
if ($xde.Count -ne 1) { Log "failed: $($xde.Count) emulators are running '$Title'"; exit 1 }
$all = [XdeMouse]::All()
foreach ($w in $all) { Log "display window $($w.Key), process $($w.Value)" }
$chosen = @($all | Where-Object { $_.Value -eq $xde[0].ProcessId })
if ($chosen.Count -ne 1) { Log "failed: $($chosen.Count) display windows belong to process $($xde[0].ProcessId)"; exit 1 }
$window = $chosen[0].Key
$left, $top, $width, $height = [XdeMouse]::Client($window)
if ($RectOnly) {
    [XdeMouse]::Front([IntPtr]$window)
    Log "display window at $left,$top, $width x $height"
    "rect $left $top $width $height"
    exit 0
}

$lines = Get-Content $Gestures
$inv = [Globalization.CultureInfo]::InvariantCulture
$viewW, $viewH = ($lines[0] -split ' ')[1..2] | ForEach-Object { [double]::Parse($_, $inv) }
$sx = $width / $viewW
$sy = $height / $viewH
Log ("display window at {0},{1}, {2} x {3}; view {4} x {5}; {6:0.####} x {7:0.####} window px per view px" -f $left, $top, $width, $height, $viewW, $viewH, $sx, $sy)

# Each step runs at its own time from the start, so the time the steps take doesn't add up; the log is kept in
# memory and written at the end, for the same reason.
$steps = New-Object System.Collections.Generic.List[string]
if ($Mode -eq 'real') { [XdeMouse]::Front([IntPtr]$window); Start-Sleep -Milliseconds 300 }
Log "playing $($lines.Count - 1) lines"
$clock = [Diagnostics.Stopwatch]::StartNew()
$at = 0.0
foreach ($line in $lines[1..($lines.Count - 1)]) {
    $parts = $line -split ' '
    if ($parts[0] -eq 'wait') { $at += [double]$parts[1]; continue }
    while ($clock.Elapsed.TotalMilliseconds -lt $at) {
        if ($at - $clock.Elapsed.TotalMilliseconds -gt 3) { Start-Sleep -Milliseconds 1 }
    }

    $x = [int][Math]::Round([double]::Parse($parts[1], $inv) * $sx)
    $y = [int][Math]::Round([double]::Parse($parts[2], $inv) * $sy)
    if ($Mode -eq 'post') {
        switch ($parts[0]) {
            'down' { [XdeMouse]::PostMove($window, $x, $y); [XdeMouse]::PostDown($window, $x, $y) }
            'move' { [XdeMouse]::PostMove($window, $x, $y) }
            'up' { [XdeMouse]::PostMove($window, $x, $y); [XdeMouse]::PostUp($window, $x, $y) }
        }
    }
    else {
        if ($parts[0] -eq 'down') { [XdeMouse]::Front([IntPtr]$window) }
        [XdeMouse]::RealMove($left + $x, $top + $y)
        if ($parts[0] -eq 'down') { [XdeMouse]::RealDown() }
        elseif ($parts[0] -eq 'up') { [XdeMouse]::RealUp() }
    }

    $steps.Add(('{0,8:0.0} ms (planned {1,6:0}) {2} -> {3},{4}' -f $clock.Elapsed.TotalMilliseconds, $at, $line, $x, $y))
}

Add-Content $Log $steps
Log 'done'
