<#
.SYNOPSIS
    The device run: builds the Android sample, installs it on the phone over ADB, starts it, waits
    for the page to draw, and pulls back a frame and what the device reported.

.DESCRIPTION
    Writes into one folder per run (artifacts\device\<yyyyMMdd-HHmmss>-<model> by default, which git
    ignores): frame.png, the phone's screen at its own resolution; run.json, the device, the start
    time Android measured and the sample's ready line; and logcat.txt, the run's log. Exits non-zero
    if any step fails, with logcat.txt kept.

    The phone must be the only device `adb devices` lists, with USB or wireless debugging on. A
    phone with a secure lock has to be unlocked by hand: the script wakes the screen and waits.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\android\run.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\android\run.ps1 -NoBuild -Out C:\temp\run1
#>
[CmdletBinding()]
param(
    [string]$Out,
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',
    [switch]$NoBuild,
    [int]$TimeoutSeconds = 60,
    [int]$UnlockTimeoutSeconds = 60
)

$ErrorActionPreference = 'Stop'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$Project = Join-Path $Repo 'samples\Cornflower.Sample.Android\Cornflower.Sample.Android.csproj'
$Package = 'io.github.wdcossey.cornflower.sample'
$Activity = "$Package/cornflower.sample.MainActivity"
$ReadyMarker = 'CORNFLOWER ready'

function Find-Adb {
    $candidates = @()
    foreach ($root in @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT, (Join-Path $env:LOCALAPPDATA 'Android\Sdk'))) {
        if ($root) { $candidates += (Join-Path $root 'platform-tools\adb.exe') }
    }
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }
    $onPath = Get-Command adb -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }
    throw "adb not found. Set ANDROID_HOME, or put platform-tools on PATH."
}

$Adb = Find-Adb

# Runs adb and returns its output as one string; throws if adb fails. adb writes some status to
# stderr (pull's "1 file pulled"), which PowerShell 5.1 turns into an error record under 'Stop', so
# the exit code alone decides.
function Adb {
    $ErrorActionPreference = 'Continue'
    $output = & $Adb @args 2>&1 | ForEach-Object { "$_" } | Out-String
    if ($LASTEXITCODE -ne 0) { throw "adb $($args -join ' ') failed ($LASTEXITCODE): $output" }
    return $output
}

function Step([string]$text) { Write-Host "-- $text" }

# Without a BOM, which Set-Content -Encoding utf8 adds in PowerShell 5.1 and JSON readers reject.
function Write-Utf8([string]$path, [string]$text) {
    [IO.File]::WriteAllText($path, $text, (New-Object Text.UTF8Encoding $false))
}

# Any failure from here on keeps the phone's log next to the run, if the run's folder exists yet.
trap {
    if ($Out -and (Test-Path $Out)) {
        try { Write-Utf8 (Join-Path $Out 'logcat.txt') (Adb logcat -d) } catch { }
        Write-Host "   Log kept in $Out\logcat.txt"
    }
    break
}

# 1. One phone.
$devices = @((Adb devices) -split "`r?`n" | Where-Object { $_ -match "^\S+\s+device$" })
if ($devices.Count -ne 1) {
    throw "Expected one device in 'adb devices', found $($devices.Count). Is the phone connected (USB switch) or paired for wireless debugging?"
}
$model = (Adb shell getprop ro.product.model).Trim()
$release = (Adb shell getprop ro.build.version.release).Trim()
$sdk = (Adb shell getprop ro.build.version.sdk).Trim()
$wmSize = (Adb shell wm size).Trim()
$wmDensity = (Adb shell wm density).Trim()
Step "$model, Android $release (API $sdk); $wmSize; $wmDensity"

if (-not $Out) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $Out = Join-Path $Repo ("artifacts\device\$stamp-" + ($model -replace '[^A-Za-z0-9]+', '-'))
}
New-Item -ItemType Directory -Force -Path $Out | Out-Null
$Out = (Resolve-Path $Out).Path

# 2. Build.
if (-not $NoBuild) {
    Step "Building the sample ($Configuration)"
    & dotnet build $Project -c $Configuration -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE)." }
}
$apk = Get-ChildItem (Join-Path $Repo "samples\Cornflower.Sample.Android\bin\$Configuration") -Recurse -Filter "$Package-Signed.apk" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $apk) { throw "No $Package-Signed.apk under bin\$Configuration. Build first, or drop -NoBuild." }

# 3. Install.
Step "Installing $($apk.Name)"
Adb install -r $apk.FullName | Out-Null

# 4. Awake and unlocked. A secure lock needs a hand; wait for it.
Adb shell input keyevent KEYCODE_WAKEUP | Out-Null
Adb shell wm dismiss-keyguard | Out-Null
$deadline = (Get-Date).AddSeconds($UnlockTimeoutSeconds)
$asked = $false
while ((Adb shell dumpsys window) -match 'isKeyguardShowing=true') {
    if ((Get-Date) -gt $deadline) { throw "The phone is still locked after $UnlockTimeoutSeconds s." }
    if (-not $asked) { Write-Host "   Unlock the phone; waiting up to $UnlockTimeoutSeconds s."; $asked = $true }
    Start-Sleep -Seconds 1
}
# A screen still waking from doze can bring the last task back on its own, under the launch.
while ((Adb shell dumpsys power) -notmatch 'mWakefulness=Awake') {
    if ((Get-Date) -gt $deadline) { throw "The screen is still not awake after $UnlockTimeoutSeconds s." }
    Start-Sleep -Seconds 1
}

# 5. Start it cold, from the home screen, and let Android time it. -S stops the app first; going
#    home first means no task of the app's is on top for the system to resume instead.
Step 'Starting the sample'
Adb shell input keyevent KEYCODE_HOME | Out-Null
Adb shell am force-stop $Package | Out-Null
Start-Sleep -Seconds 1
Adb logcat -c | Out-Null
$start = Adb shell am start -W -S -n $Activity
# am exits 0 even when it can't start the activity; it says so in its output.
if ($start -match '(?m)^Error' -or $start -notmatch 'Status:\s*ok') { throw "am start failed:`n$start" }
$totalTime = if ($start -match 'TotalTime:\s*(\d+)') { [int]$Matches[1] } else { $null }
$launchState = if ($start -match 'LaunchState:\s*(\w+)') { $Matches[1] } else { $null }
if ($launchState -ne 'COLD' -or -not $totalTime) {
    # Not a cold start Android timed (the activity was already on top, say): no time to record.
    Write-Warning "am start -W did not time a cold start (LaunchState $launchState, TotalTime $totalTime):`n$start"
    $totalTime = $null
}

# 6. Wait for the page to say it has drawn.
$ready = $null
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while (-not $ready) {
    $line = (Adb logcat -d -s DOTNET:I) -split "`r?`n" | Where-Object { $_ -match [regex]::Escape($ReadyMarker) } | Select-Object -First 1
    if ($line) { $ready = $line; break }
    if ((Get-Date) -gt $deadline) { throw "No '$ReadyMarker' line within $TimeoutSeconds s." }
    Start-Sleep -Milliseconds 500
}
$facts = $ready.Substring($ready.IndexOf($ReadyMarker) + $ReadyMarker.Length).Trim()
Step "Ready: $facts"

# 7. The frame. Written on the phone and pulled, because piping exec-out through PowerShell 5.1
#    would re-encode the PNG's bytes.
Start-Sleep -Milliseconds 500
$remote = '/sdcard/cornflower-frame.png'
Adb shell screencap -p $remote | Out-Null
Adb pull $remote (Join-Path $Out 'frame.png') | Out-Null
Adb shell rm $remote | Out-Null

Write-Utf8 (Join-Path $Out 'logcat.txt') (Adb logcat -d)

$run = [ordered]@{
    when          = (Get-Date).ToString('o')
    model         = $model
    android       = $release
    sdk           = [int]$sdk
    wmSize        = $wmSize
    wmDensity     = $wmDensity
    configuration = $Configuration
    apk           = $apk.Name
    apkBytes      = $apk.Length
    launchState   = $launchState
    totalTimeMs   = $totalTime
    ready         = $facts
}
Write-Utf8 (Join-Path $Out 'run.json') ($run | ConvertTo-Json)

Step "Start: $launchState, $totalTime ms (am start -W TotalTime)"
Step "Wrote $Out"
