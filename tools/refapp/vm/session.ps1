# Windows only: runs on the reference VM, in its Windows PowerShell, not on the machine that drives it.
# Runs on the reference VM, in the signed-in desktop session, as a scheduled task that ..\run.ps1
# registers over SSH. The emulator, AppDeployCmd and ISETool only see each other from that session:
# run from an SSH session, ISETool starts a hidden second emulator instead.
#
# Deploys and launches the app (starting the emulator if it isn't running), waits for the app to
# write done.txt, pulls its local folder into out\, and reports progress in status.txt.
param(
    [int]$Device = 10,
    [Parameter(Mandatory = $true)][string]$ProductId,
    [string]$Root = 'C:\cornflower\refapp',
    [int]$TimeoutMinutes = 40,
    [switch]$Demo,
    [switch]$Grab,
    [switch]$Probe,
    [ValidateSet('post', 'real')][string]$TouchMode = 'real',
    [switch]$Record,
    [switch]$Native
)

$ErrorActionPreference = 'Continue'
$ProgressPreference = 'SilentlyContinue'
$status = Join-Path $Root 'status.txt'
$log = Join-Path $Root 'session.log'

function Status([string]$Text) {
    Set-Content -Path $status -Value $Text -Encoding ASCII
    Add-Content -Path $log -Value "$(Get-Date -Format s) $Text"
}

Set-Content -Path $log -Value "$(Get-Date -Format s) session: device $Device, product $ProductId"
$tools = 'C:\Program Files (x86)\Microsoft SDKs\Windows Phone\v8.1\Tools'
$deploy = Join-Path $tools 'AppDeploy\AppDeployCmd.exe'
$ise = Join-Path $tools 'IsolatedStorageExplorerTool\ISETool.exe'

$appx = Get-ChildItem (Join-Path $Root 'app\AppPackages') -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension -eq '.appx' } | Select-Object -First 1
if (-not $appx) {
    Status 'failed: no .appx; run vm\build.ps1 first'
    exit 1
}

Status "deploying $($appx.Name) to device $Device (this starts the emulator if needed)"
& $deploy /installlaunch $appx.FullName /targetdevice:$Device 2>&1 | Add-Content -Path $log
if ($LASTEXITCODE -ne 0) {
    Status "failed: AppDeployCmd exit $LASTEXITCODE"
    exit 1
}

if ($Demo) {
    Status "done: demo launched on device $Device; the app stays open"
    exit 0
}

# A probe lays its page out, then writes the touches it wants as gestures.txt and ready.txt. Pull them, and play
# them on the emulator's display with vm\touch.ps1. The app writes done.txt once the last touch has settled, or when
# its timeout (in ready.txt) runs out; when touch.ps1 fails, wait for that too, so that the app's log is pulled.
$out = Join-Path $Root 'out'
function Pull {
    Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force $out | Out-Null
    & $ise ts "deviceindex:$Device" $ProductId $out 2>&1 | Add-Content -Path $log
}

# Stops ffmpeg (a 'q' on its input, then the kill if it hasn't gone in a minute) and lists the frames' times.
$recorder = $null
function Stop-Recorder {
    if (-not $recorder) { return }
    if (-not $recorder.HasExited) {
        try { $recorder.StandardInput.Write('q'); $recorder.StandardInput.Close() } catch { }
        if (-not $recorder.WaitForExit(60000)) { $recorder.Kill() }
    }

    if (Test-Path $video) {
        & (Join-Path $links 'ffprobe.exe') -v error -select_streams v -show_entries frame=pts_time -of csv=p=0 $video | Set-Content (Join-Path $Root 'frames.csv') -Encoding ASCII
        Add-Content -Path $log -Value "$(Get-Date -Format s) recorded $((Get-Item $video).Length) bytes, $((Get-Content (Join-Path $Root 'frames.csv')).Count) frames"
    }
    else {
        Add-Content -Path $log -Value "$(Get-Date -Format s) ffmpeg wrote no recording"
    }

    $script:recorder = $null
}

$appTimeoutMinutes = 0
$touchFailed = 0
if ($Probe) {
    # A probe that can't run (an unknown name, a template without the part, a touch that would miss) writes done.txt and no ready.txt.
    Status 'probing: waiting for the app to write ready.txt'
    $listing = ''
    for ($i = 0; $i -lt 40 -and $listing -notmatch '(ready|done)\.txt'; $i++) {
        Start-Sleep -Seconds 3
        $listing = (& $ise dir "deviceindex:$Device" $ProductId 2>&1) -join "`n"
    }

    if ($listing -notmatch 'ready\.txt') {
        Pull
        $why = Get-Content (Join-Path $out 'IsolatedStore\done.txt') -ErrorAction SilentlyContinue
        Status "failed: the probe didn't start: $(if ($why) { $why } else { 'no ready.txt in 2 minutes' }); partial results pulled"
        exit 1
    }

    $in = Join-Path $Root 'probe-in'
    Remove-Item $in -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force $in | Out-Null
    # ready.txt's "timeout <ms>": how long after it the app finishes and writes its log, whether the touches came or not.
    # The app writes it whole (under another name, then renamed); pull again if the line isn't there yet.
    $timeoutLine = $null
    for ($i = 0; $i -lt 3 -and -not $timeoutLine; $i++) {
        if ($i -gt 0) { Start-Sleep -Seconds 2 }
        & $ise ts "deviceindex:$Device" $ProductId $in 2>&1 | Add-Content -Path $log
        $timeoutLine = Get-Content (Join-Path $in 'IsolatedStore\ready.txt') -ErrorAction SilentlyContinue | Where-Object { $_ -match '^timeout \d' } | Select-Object -First 1
    }

    $parsed = 0.0
    if ($timeoutLine -and [double]::TryParse(($timeoutLine -split ' ')[1], [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$parsed)) {
        $appTimeoutMinutes = $parsed / 60000
    }
    else {
        Add-Content -Path $log -Value "$(Get-Date -Format s) ready.txt gives no timeout ('$timeoutLine'); a probe may take 8 minutes"
    }
    # More than one emulator can be running, one per profile: touch the one this device is, by its window title.
    $name = (& $deploy /EnumerateDevices 2>&1 | Where-Object { $_ -match "^\s*$Device\s+(.+?)\s*$" } | ForEach-Object { $Matches[1] } | Select-Object -First 1)
    if (-not $name) {
        Status "failed: AppDeployCmd /EnumerateDevices names no device $Device"
        exit 1
    }

    # -Record: ffmpeg's gdigrab records the emulator's display window from before the touches until done.txt, at the
    # times the frames were grabbed (record.mkv, lossless), and ffprobe lists each frame's time (frames.csv).
    if ($Record) {
        $rect = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Root 'vm\touch.ps1') -Gestures none -Log (Join-Path $Root 'rect.log') -Title $name -RectOnly | Where-Object { $_ -match '^rect ' }
        if (-not $rect) {
            Status "failed: no display window to record for '$name'; see rect.log"
            exit 1
        }

        $l, $t, $w, $h = ($rect -split ' ')[1..4]
        $video = Join-Path $Root 'record.mkv'
        Remove-Item $video, (Join-Path $Root 'frames.csv') -ErrorAction SilentlyContinue
        $links = Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Links'
        $psi = New-Object Diagnostics.ProcessStartInfo (Join-Path $links 'ffmpeg.exe')
        $psi.Arguments = "-hide_banner -nostats -loglevel error -f gdigrab -framerate 60 -draw_mouse 0 -offset_x $l -offset_y $t -video_size ${w}x${h} -i desktop -c:v libx264 -preset ultrafast -qp 0 -pix_fmt yuv444p -fps_mode passthrough -y `"$video`""
        $psi.UseShellExecute = $false
        $psi.RedirectStandardInput = $true
        try { $recorder = [Diagnostics.Process]::Start($psi) } catch { $recorder = $null; Add-Content -Path $log -Value "$(Get-Date -Format s) ffmpeg: $_" }
        Start-Sleep -Seconds 2
        if (-not $recorder -or $recorder.HasExited) {
            Status "failed: ffmpeg didn't start recording $rect; see session.log"
            exit 1
        }

        Add-Content -Path $log -Value "$(Get-Date -Format s) recording $rect, pid $($recorder.Id)"
    }

    Status "probing: playing the touches on '$name'"
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Root 'vm\touch.ps1') -Gestures (Join-Path $in 'IsolatedStore\gestures.txt') -Log (Join-Path $Root 'touch.log') -Title $name -Mode $TouchMode
    if ($LASTEXITCODE -ne 0) {
        $touchFailed = $LASTEXITCODE
        Stop-Recorder
        # touch.ps1 logs "playing" just before its first step. Failed before it, nothing was touched: say so at once.
        if (-not (Select-String -Path (Join-Path $Root 'touch.log') -Pattern ' playing ' -SimpleMatch -Quiet)) {
            Pull
            Status "failed: touch.ps1 exit $touchFailed before playing any touch; see touch.log; partial results pulled"
            exit 1
        }

        # Failed mid-run: the app holds its log until it finishes, so wait below for its done.txt, which its timeout
        # bounds, then pull.
        Add-Content -Path $log -Value "$(Get-Date -Format s) touch.ps1 exit $touchFailed mid-run; waiting for the app to time out and write its log"
    }
}

$grabber = $null
$stopGrab = Join-Path $Root 'grab.stop'
if ($Grab -and -not $touchFailed) {
    Remove-Item $stopGrab, (Join-Path $Root 'frames') -Recurse -Force -ErrorAction SilentlyContinue
    $grabber = Start-Process powershell.exe -PassThru -WindowStyle Hidden -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $Root 'vm\grab-loop.ps1'), '-Out', (Join-Path $Root 'frames'), '-StopFile', $stopGrab)
    Add-Content -Path $log -Value "$(Get-Date -Format s) grabbing the emulator window, pid $($grabber.Id)"
}

$started = Get-Date
Status 'running: app launched, waiting for done.txt'
# A probe that wrote no done.txt by its limit has stalled: 8 minutes, or a minute past the app's own timeout from
# ready.txt, which started before this; never past -TimeoutMinutes, which run.ps1 and the task's time limit allow for.
$probeLimit = [Math]::Max(8, $appTimeoutMinutes + 1)
if ($Probe -and $probeLimit -gt $TimeoutMinutes) {
    Add-Content -Path $log -Value "$(Get-Date -Format s) the app's timeout passes -TimeoutMinutes $TimeoutMinutes; waiting $TimeoutMinutes"
    $probeLimit = $TimeoutMinutes
}

$deadline = $started.AddMinutes($(if ($Probe) { $probeLimit } else { $TimeoutMinutes }))
$done = $false
$stalled = $false
$lastPngs = -1
$lastChange = Get-Date
# Every 20 s, or every 5 s while recording, so that ffmpeg stops soon after the probe is done.
$poll = if ($recorder) { 5 } else { 20 }
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds $poll
    $listing = (& $ise dir "deviceindex:$Device" $ProductId 2>&1) -join "`n"
    if ($listing -match 'done\.txt') {
        $done = $true
        break
    }

    $pngs = ([regex]::Matches($listing, '\.png')).Count
    if ($pngs -ne $lastPngs) { $lastPngs = $pngs; $lastChange = Get-Date }
    elseif (-not ($Probe -or $Native) -and ((Get-Date) - $lastChange).TotalMinutes -ge 3) { $stalled = $true; break }
    elseif ($Native -and ((Get-Date) - $started).TotalMinutes -ge 8) { $stalled = $true; break }
    $elapsed = ((Get-Date) - $started).TotalSeconds
    if ($Native) {
        # A native run makes no PNGs; its progress.txt names the step it is on. Pulling the folder takes a few seconds,
        # and the folder holds only progress.txt until the end.
        $peek = Join-Path $Root 'peek'
        Remove-Item $peek -Recurse -Force -ErrorAction SilentlyContinue
        New-Item -ItemType Directory -Force $peek | Out-Null
        & $ise ts "deviceindex:$Device" $ProductId $peek 2>&1 | Out-Null
        $step = Get-Content (Join-Path $peek 'IsolatedStore\progress.txt') -ErrorAction SilentlyContinue
        Status ('running: {0} after {1:N0} s' -f $(if ($step) { $step } else { 'starting' }), $elapsed)
    }
    elseif ($Probe -and $touchFailed) {
        Status ('running: touch.ps1 failed, waiting for the app to time out and write its log, {0:N0} s of up to {1:N0}' -f $elapsed, ($probeLimit * 60))
    }
    elseif ($Probe) {
        # A probe writes nothing until its touches have settled; its log comes back with done.txt.
        Status ('running: touches played, waiting for the probe to settle, {0:N0} s' -f $elapsed)
    }
    else {
        Status ('running: {0} PNGs so far after {1:N0} s' -f $pngs, $elapsed)
    }
}

if ($Probe -and -not $done) { $stalled = $true }

if ($grabber) {
    Set-Content -Path $stopGrab -Value stop
    $grabber.WaitForExit(30000) | Out-Null
}

Stop-Recorder

Status "pulling the app's local folder"
Pull

if ($touchFailed) {
    $summary = Get-Content (Join-Path $out 'IsolatedStore\done.txt') -ErrorAction SilentlyContinue
    Status "failed: touch.ps1 exit $touchFailed; see touch.log; app reports $(if ($summary) { $summary } else { 'no done.txt' }); partial results pulled"
    exit 1
}

# A run that stalls or runs out of time still leaves what it wrote, including progress.txt, the last item started.
if (-not $done) {
    $why = if ($stalled -and $Probe) { "the app wrote no done.txt in $probeLimit minutes" } elseif ($stalled -and $Native) { 'the app wrote no done.txt in 8 minutes' } elseif ($stalled) { "no new PNG for 3 minutes at $lastPngs" } else { "no done.txt after $TimeoutMinutes minutes" }
    $last = Get-Content (Join-Path $out 'IsolatedStore\progress.txt') -ErrorAction SilentlyContinue
    Status "failed: $why; last started: $last; partial results pulled"
    exit 1
}

$count = (Get-ChildItem $out -Recurse -File).Count
$summary = Get-Content (Join-Path $out 'IsolatedStore\done.txt') -ErrorAction SilentlyContinue
Status ('done: pulled {0} files; app reports {1}; {2:N0} s in the app' -f $count, $summary, ((Get-Date) - $started).TotalSeconds)
