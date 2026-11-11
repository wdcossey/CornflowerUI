# Opens the real desktop sample window and uses it with real mouse and keyboard input. Checks what it can,
# saves a frame of every page, and exits 1 if a check fails. See README.md here.
#
# It takes over the cursor and focus for a few seconds a page, so ask before running it on someone's PC.
# Windows only, and PowerShell 5.1 or later: powershell -ExecutionPolicy Bypass -File tools\real-window\drive.ps1
#
# Ported from Avonic's tools/real-window/drive.ps1 (wdcossey/Avonic, v2 at edf87b8): the window, the input,
# the frames and the every-page loop. Avonic's per-control sections come back here as Cornflower's controls do.
param(
    # The sample to run. Left out, the script builds the Debug sample first and runs that. Given, it runs the
    # file as it is: build it first. A Debug build also has the performance line to check.
    [string]$Exe,
    # Where the frames go. By default a new folder for each run, so that an old frame can't pass for a new one.
    [string]$Out = (Join-Path ([IO.Path]::GetTempPath()) ('cornflower-real-window\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))),
    # Run the Debug sample as it is, without building it first.
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
if (-not $Exe) {
    $project = Join-Path $PSScriptRoot '..\..\samples\Cornflower.Sample.Desktop'
    $Exe = Join-Path $project 'bin\Debug\net10.0\Cornflower.Sample.Desktop.exe'
    # A stale build runs the old code and its failures look like the new code's.
    if (-not $NoBuild) {
        & dotnet build $project -c Debug --nologo -v quiet
        if ($LASTEXITCODE -ne 0) { throw "The sample didn't build (dotnet build exited $LASTEXITCODE)." }
    }
}
if (-not (Test-Path $Exe)) { throw "No sample at $Exe. Build it first." }
New-Item -ItemType Directory -Force $Out | Out-Null
$Out = (Resolve-Path $Out).Path
$Exe = (Resolve-Path $Exe).Path
$debugBuild = $Exe -match '\\Debug\\'
$stdout = Join-Path $Out 'stdout.txt'

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class RealInput {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
  public static void Down() { mouse_event(2, 0, 0, 0, IntPtr.Zero); }
  public static void Up() { mouse_event(4, 0, 0, 0, IntPtr.Zero); }
}
'@
[void][RealInput]::SetProcessDPIAware()
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$SendKeys = [System.Windows.Forms.SendKeys]
$cursor = [System.Windows.Forms.Cursor]::Position
$script:failures = 0
$script:checks = 0

function Check($name, [bool]$ok, $detail = '') {
    $script:checks++
    if ($ok) { "  ok    $name" } else { $script:failures++; "  FAIL  $name $detail" }
}

# From the screen, not PrintWindow, which offsets a window with no frame of its own by its invisible border.
function Shot($name) {
    $r = $win.Current.BoundingRectangle
    $bmp = New-Object Drawing.Bitmap ([int]$r.Width), ([int]$r.Height)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen([int]$r.X, [int]$r.Y, 0, 0, $bmp.Size)
    $g.Dispose()
    $bmp.Save((Join-Path $Out "$name.png"))
    $bmp.Dispose()
}
function All($type) {
    $cond = New-Object Windows.Automation.PropertyCondition $AE::ControlTypeProperty, $type
    $win.FindAll($TS::Descendants, $cond) | ForEach-Object { $_ }
}
# By the exact name, case and all: a page's title is a name, not a pattern.
function Named($type, $name) { All $type | Where-Object { $_.Current.Name -ceq $name } | Select-Object -First 1 }
# A title as a -like pattern that matches only itself.
function Literal($text) { [Management.Automation.WildcardPattern]::Escape($text) }
function Texts { (All $CT::Text | ForEach-Object { $_.Current.Name } | Where-Object { $_ }) }
function FirstText($like) { Texts | Where-Object { $_ -like $like } | Select-Object -First 1 }
# A scroll bar's own buttons, which have no size at its ends and aren't the app's.
$scrollParts = 'Line up', 'Page up', 'Page down', 'Line down', 'Line left', 'Page left', 'Page right', 'Line right'
function AppButtons { All $CT::Button | Where-Object { $_.Current.Name -and $_.Current.Name -notin $scrollParts } }
# Throws rather than aim at the corner of the screen, where a click could land in someone's app.
function Centre($el) {
    if (-not $el) { throw 'Nothing to aim at: the element was not found' }
    $b = $el.Current.BoundingRectangle
    if ([double]::IsInfinity($b.Width) -or [double]::IsInfinity($b.Height) -or [double]::IsNaN($b.Width) -or $b.Width -le 0 -or $b.Height -le 0) { throw "Nothing to aim at: $($el.Current.Name) has no size" }
    [int]($b.X + $b.Width / 2), [int]($b.Y + $b.Height / 2)
}
# Windows lets a process that isn't in front only ask, so it also asks through UI Automation, which the app itself acts on.
function Fore {
    [void][RealInput]::SetForegroundWindow($p.MainWindowHandle)
    if ($win -and [RealInput]::GetForegroundWindow() -ne $p.MainWindowHandle) { try { $win.SetFocus() } catch { } }
    Start-Sleep -Milliseconds 150
}
# Called before any input, so that nothing lands in another app if something else takes the focus.
function Guard {
    # A few tries: just after it starts, Windows can be slow to bring the sample forward.
    for ($try = 0; $try -lt 5 -and [RealInput]::GetForegroundWindow() -ne $p.MainWindowHandle; $try++) { Fore }
    $front = [RealInput]::GetForegroundWindow()
    if ($front -ne $p.MainWindowHandle) {
        $owner = 0; [void][RealInput]::GetWindowThreadProcessId($front, [ref]$owner)
        $name = try { (Get-Process -Id $owner).ProcessName } catch { "process $owner" }
        throw "The sample is not in front ($name is), so no input was sent"
    }
}
function Key($text, $ms = 400) { Guard; $SendKeys::SendWait($text); Start-Sleep -Milliseconds $ms }
function Press { Guard; $script:held = $true; [RealInput]::Down() }
function Release { [RealInput]::Up(); $script:held = $false }
# Lets go of whatever an exception left held, on the user's desktop as well as in the sample.
function LetGo { if ($script:held) { Release } }
# Refuses a point outside the window, such as a button below the fold of a long list, which would land in whatever is under it.
function InWindow($x, $y) { $w = $win.Current.BoundingRectangle; $x -gt $w.X -and $x -lt $w.Right -and $y -gt $w.Y -and $y -lt $w.Bottom }
function Click($el) {
    $x, $y = Centre $el
    if (-not (InWindow $x $y)) { throw "Not clicking $($el.Current.Name) at ${x}, ${y}: that's outside the window" }
    Guard
    [void][RealInput]::SetCursorPos($x, $y); Start-Sleep -Milliseconds 120
    Press; Start-Sleep -Milliseconds 50; Release; Start-Sleep -Milliseconds 600
}
function Invoke-Element($el) { $el.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 600 }
# What a page reports comes after layout, so it can take a moment.
function WaitForText($like, $seconds = 5) {
    $until = (Get-Date).AddSeconds($seconds)
    do {
        $found = FirstText $like
        if ($found) { return $found }
        Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $until)
}
# The sample's standard output, which the app is still writing to.
function ReadyLines {
    try {
        $stream = [IO.File]::Open($stdout, 'Open', 'Read', 'ReadWrite')
        try { $reader = New-Object IO.StreamReader $stream; @($reader.ReadToEnd() -split "`r?`n" | Where-Object { $_ -like 'CORNFLOWER ready*' }) }
        finally { $stream.Dispose() }
    }
    catch { @() }
}
# The next ready line after the $seen-th: the shell writes one for each page it opens, once the page has drawn:
#   CORNFLOWER ready open=<ms>ms scaling=... view=WxH raw=WxH safe=l,t,r,b page=WxH title=<title>
# Only page=, scaling= and title= are read here.
function WaitForReady($seen, $seconds = 5) {
    $until = (Get-Date).AddSeconds($seconds)
    do {
        $lines = @(ReadyLines)
        if ($lines.Count -gt $seen) { return $lines[$seen] }
        Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $until)
}
# The page's own size, which a shell's chrome would take from; the window's client area is view=.
function PageOf($line) { if ($line -match ' page=([\d.]+)x([\d.]+)') { [double]$Matches[1], [double]$Matches[2] } }
function ScalingOf($line) { if ($line -match 'scaling=([\d.]+) ') { [double]$Matches[1] } else { 1 } }
# The page the line is for: last on the line, as a title can have spaces.
function TitleOf($line) { if ($line -match ' title=(.+)$') { $Matches[1].Trim() } }
# Windows gives a window whole pixels, so at 1.25 a 450 view is 562 pixels, 449.6: within a pixel is the size.
function WithinAPixel($view, $want, $scaling) { [math]::Abs($view - $want) * $scaling -le 1 }
function OnIndex { [bool](Named $CT::Text 'samples') -and -not (Named $CT::Button 'Back') }
function Index { $back = Named $CT::Button 'Back'; if ($back) { Invoke-Element $back } }
function Open($title) {
    Index
    $link = Named $CT::Button $title
    if (-not $link) { throw "$title isn't on the list" }
    Invoke-Element $link
}
function Section($title, [scriptblock]$body) {
    "== $title"
    try { & $body }
    # Counted as a check, so the summary's count of checks includes it.
    catch { LetGo; $script:checks++; $script:failures++; "  FAIL  the section stopped: $($_.Exception.Message) at line $($_.InvocationInfo.ScriptLineNumber)" }
    if ($p.HasExited) { throw "The app exited during $title, with code $($p.ExitCode)" }
}

$p = Start-Process $Exe -PassThru -RedirectStandardOutput $stdout
try {
    $win = $null
    for ($i = 0; $i -lt 40 -and -not $win; $i++) {
        Start-Sleep -Milliseconds 500
        $p.Refresh()
        if ($p.MainWindowHandle -ne 0) { $win = $AE::FromHandle($p.MainWindowHandle) }
    }
    if (-not $win) { throw 'The window never appeared' }
    Fore
    Start-Sleep -Milliseconds 500
    $r = $win.Current.BoundingRectangle
    $area = [System.Windows.Forms.Screen]::FromHandle($p.MainWindowHandle).WorkingArea
    if ($r.Bottom -gt $area.Bottom -or $r.Right -gt $area.Right -or $r.Y -lt $area.Y -or $r.X -lt $area.X) {
        throw "The window ($r) doesn't fit on the screen ($area), so clicks would miss and frames would be cut off. Move it, or if it's taller than the screen, lower the display scale."
    }
    "$Exe"
    "window $($r.Width) x $($r.Height) on the screen, frames in $Out"
    $titles = @(AppButtons | ForEach-Object { $_.Current.Name })
    Shot 'index'
    Check 'the list has a page' ($titles.Count -gt 0)

    Section 'Every page on the list, opened with a click and left with Back' {
        foreach ($title in $titles) {
            Index
            $link = Named $CT::Button $title
            $x, $y = Centre $link
            $seen = @(ReadyLines).Count
            # A list longer than the window: what's below the fold is opened through UI Automation instead.
            if (InWindow $x $y) { Click $link } else { "        $title is below the fold: opened through UI Automation"; Invoke-Element $link }
            Check "a click on '$title' opens it" ([bool](Named $CT::Button 'Back'))
            # The shell times every page and writes its ready line, with no code in the page.
            $ready = WaitForReady $seen
            Check "$title reports ready, under its title" ((TitleOf $ready) -ceq $title) "$ready"
            Shot $title
            # A control with no size is invisible, as Avonic's fixed slot's button once was.
            $empty = @(AppButtons | Where-Object {
                $b = $_.Current.BoundingRectangle
                [double]::IsInfinity($b.Width) -or [double]::IsInfinity($b.Height) -or $b.Width -eq 0 -or $b.Height -eq 0
            } | ForEach-Object { $_.Current.Name })
            Check "$title has no button without a size" ($empty.Count -eq 0) ($empty -join ', ')
            if ($debugBuild) {
                $line = WaitForText "$(Literal $title): * ms, *"
                Check "$title reports its cost" ([bool]$line) ''
                if ($line) { "        $line" }
            }
            Click (Named $CT::Button 'Back')
            Check "Back from '$title' shows the list" (OnIndex)
        }
    }

    Section 'The page opens at the reference view, 450 x 800' {
        $seen = @(ReadyLines).Count
        Open 'theme'
        $line = WaitForReady $seen
        if ($line) { "        $line" }
        $w, $h = PageOf $line
        $scaling = ScalingOf $line
        Check 'the ready line is the theme page''s' ((TitleOf $line) -ceq 'theme') "$line"
        Check 'the theme page reports ready, laid out at 450 x 800 to the pixel' ((WithinAPixel $w 450 $scaling) -and (WithinAPixel $h 800 $scaling)) "$line"
    }

    Section 'Alt+Left goes back' {
        Open 'theme'
        Key '%{LEFT}'
        Check 'Alt+Left on a page shows the list' (OnIndex)
    }

    Section 'The window can be resized' {
        $transform = $win.GetCurrentPattern([Windows.Automation.TransformPattern]::Pattern)
        Check 'the window says it can be resized' ($transform.Current.CanResize)
        # The pattern sets the window's outer size, title bar and all, so only the width is checked.
        $r = $win.Current.BoundingRectangle
        $transform.Resize($r.Width + 300, $r.Height)
        Start-Sleep -Milliseconds 500
        $seen = @(ReadyLines).Count
        Open 'theme'
        $line = WaitForReady $seen
        if ($line) { "        $line" }
        $w, $h = PageOf $line
        # 300 pixels wider, less what the frame takes, which is far less than 100.
        Check 'once wider, the page is laid out wider' ($w -gt 450 + 200 / (ScalingOf $line)) "$line"
        Shot 'theme-resized'
    }

}
finally {
    LetGo
    [void][RealInput]::SetCursorPos($cursor.X, $cursor.Y)
    if (-not $p.HasExited) { [void]$p.CloseMainWindow(); Start-Sleep -Milliseconds 500 }
    if (-not $p.HasExited) { $p.Kill() }
}

"$($script:checks - $script:failures) of $($script:checks) checks passed"
if ($script:failures -gt 0) { exit 1 }
