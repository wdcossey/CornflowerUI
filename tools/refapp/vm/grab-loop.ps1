# Runs on the reference VM in the signed-in desktop session, started by session.ps1 -Grab.
#
# Watches the emulator's output window (class OPWindowClass) and, each time the picture has changed and then
# held still for -SteadyMs, saves a screenshot with the emulator's own Screenshot tool (Additional Tools >
# Screenshot > Capture, then Save...), driven through UI Automation. That tool reads the phone's whole
# display at its own resolution (1080 x 1920 on the 1080p profile) whatever the window's zoom; a grab of
# the window doesn't: at 100% zoom the part of the window below the VM's screen is never drawn, and at the
# zoom that fits it is scaled. The window is only watched, to know when to shoot.
#
# Each shot is <Out>\<UTC yyyyMMddTHHmmssfff>.png, the time the picture last changed. Runs until the stop
# file appears. It opens the Additional Tools window if it isn't (the toolbar's >> button).
param(
    [Parameter(Mandatory = $true)][string]$Out,
    [Parameter(Mandatory = $true)][string]$StopFile,
    [int]$IntervalMs = 200,
    [int]$SteadyMs = 1200
)

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $Out | Out-Null
$log = Join-Path $Out 'grab.log'
function Log([string]$Text) { Add-Content $log "$(Get-Date -Format 'HH:mm:ss.fff') $Text" }
Set-Content $log "$(Get-Date -Format s) start"

Add-Type -AssemblyName System.Drawing, UIAutomationClient, UIAutomationTypes
Add-Type -ReferencedAssemblies System.Drawing @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
public static class Watch {
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc f, IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr p, EnumProc f, IntPtr l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();

    static string Cls(IntPtr h) { var s = new StringBuilder(256); GetClassName(h, s, 256); return s.ToString(); }

    public static IntPtr Find() {
        var found = IntPtr.Zero;
        EnumWindows((top, l) => {
            EnumChildWindows(top, (c, l2) => {
                if (found == IntPtr.Zero && Cls(c) == "OPWindowClass" && IsWindowVisible(c)) found = c;
                return true;
            }, IntPtr.Zero);
            return true;
        }, IntPtr.Zero);
        return found;
    }

    // A hash of the window's pixels, 0 if it can't be read.
    public static long Hash(IntPtr h) {
        RECT r; GetClientRect(h, out r);
        int w = r.R - r.L, ht = r.B - r.T;
        if (w <= 0 || ht <= 0) return 0;
        using (var bmp = new Bitmap(w, ht, PixelFormat.Format32bppArgb)) {
            using (var g = Graphics.FromImage(bmp)) {
                var hdc = g.GetHdc();
                PrintWindow(h, hdc, 3); // PW_CLIENTONLY | PW_RENDERFULLCONTENT
                g.ReleaseHdc(hdc);
            }
            var d = bmp.LockBits(new Rectangle(0, 0, w, ht), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var px = new int[w * ht];
            Marshal.Copy(d.Scan0, px, 0, px.Length);
            bmp.UnlockBits(d);
            long hash = 1469598103934665603;
            foreach (var p in px) { hash ^= p; hash *= 1099511628211; }
            return hash;
        }
    }
}
'@
[void][Watch]::SetProcessDPIAware()
Add-Type -Path (Join-Path $PSScriptRoot 'XdeWin32.cs')

$AE = [System.Windows.Automation.AutomationElement]
$Scope = [System.Windows.Automation.TreeScope]
function ById($root, [string]$Id) {
    $root.FindFirst($Scope::Descendants, (New-Object System.Windows.Automation.PropertyCondition($AE::AutomationIdProperty, $Id)))
}
function Invoke-El($el) { $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }

# The emulator's Screenshot tab, selected; $null if the Additional Tools window isn't open.
function Get-Tools {
    $xde = Get-Process XDE -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $xde) { return $null }
    $cond = New-Object System.Windows.Automation.PropertyCondition($AE::ProcessIdProperty, $xde.Id)
    foreach ($top in $AE::RootElement.FindAll($Scope::Children, $cond)) {
        $tools = ById $top 'XDEToolsWindow'
        if ($tools) {
            $tab = ById $tools 'TabItem_3'
            $tab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
            return [pscustomobject]@{ Process = $xde.Id; Tools = $tools }
        }
    }
    return $null
}

# Capture, then Save... to $Path. The save dialog is filled with Win32 messages (XdeWin32.cs): through UI
# Automation, a modal file dialog is slow to walk and can hang the search. Returns $true if the file appeared.
function Save-Shot($t, [string]$Path) {
    Invoke-El (ById $t.Tools 'SS_Capture')
    Start-Sleep -Milliseconds 400
    $save = ById $t.Tools 'SS_SaveBtn'
    # Invoke can block until the modal dialog closes; press it from another runspace.
    $ps = [PowerShell]::Create().AddScript({ param($b) $b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }).AddArgument($save)
    $async = $ps.BeginInvoke()
    try {
        $dialog = [SaveDialog]::Wait($t.Process, 5000)
        if ($dialog -eq [IntPtr]::Zero) { Log 'no save dialog'; return $false }
        $err = [SaveDialog]::Save($dialog, $Path)
        if ($err) { Log "save dialog: $err"; [SaveDialog]::Cancel($dialog); return $false }
        for ($i = 0; $i -lt 50 -and -not (Test-Path $Path); $i++) { Start-Sleep -Milliseconds 100 }
        return (Test-Path $Path)
    }
    finally {
        $async.AsyncWaitHandle.WaitOne(3000) | Out-Null
        $ps.Dispose()
    }
}

$saved = 0
$lastHash = 0
$changedAt = [DateTime]::UtcNow
$shot = $true
$t = $null
while (-not (Test-Path $StopFile)) {
    Start-Sleep -Milliseconds $IntervalMs
    $h = [Watch]::Find()
    if ($h -eq [IntPtr]::Zero) { continue }

    $hash = [Watch]::Hash($h)
    if ($hash -ne $lastHash) {
        $lastHash = $hash
        $changedAt = [DateTime]::UtcNow
        $shot = $false
        continue
    }

    if ($shot -or ([DateTime]::UtcNow - $changedAt).TotalMilliseconds -lt $SteadyMs) { continue }
    $shot = $true
    if (-not $t) {
        $t = Get-Tools
        if (-not $t) {
            # A restarted emulator has it closed: press >> on the toolbar.
            $xde = Get-Process XDE -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($xde -and [XdeToolbar]::OpenTools($xde.Id)) { Start-Sleep 3; $t = Get-Tools }
        }
        if (-not $t) { Log 'the Additional Tools window is not open, and pressing >> on the toolbar did not open it'; continue }
    }

    $path = Join-Path $Out ($changedAt.ToString('yyyyMMddTHHmmssfff') + '.png')
    try {
        if (Save-Shot $t $path) { $saved++ } else { Log "not saved: $path" }
    }
    catch {
        Log "shot failed: $_"
        $t = $null
    }
}

Log "stop: $saved shots"
