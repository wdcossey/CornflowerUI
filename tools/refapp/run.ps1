<#
.SYNOPSIS
  Builds the Cornflower reference app on the reference VM, runs it in the Windows Phone 8.1
  emulator, and pulls every capture back to this PC.

.DESCRIPTION
  Each step is its own SSH command; one long remote script that also polled a scheduled task
  once hung the session.
    1. copy app\ and vm\ to C:\cornflower\refapp on the VM
    2. build with MSBuild 14 (vm\build.ps1)
    3. register and start a scheduled task in the signed-in desktop that deploys the app, waits
       for its done.txt and pulls its local folder with ISETool (vm\session.ps1)
    4. poll C:\cornflower\refapp\status.txt until done or failed
    5. copy the results to -Out\<timestamp>-dev<Device>, write index.html, remove the task

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\refapp\run.ps1
  powershell -ExecutionPolicy Bypass -File tools\refapp\run.ps1 -Device 13
  powershell -ExecutionPolicy Bypass -File tools\refapp\run.ps1 -Device 11 -Only 'Flyout|Dialog|ComboBox'

  -Only keeps the specimens and popups whose name matches the regex, for a quick run.
  -Popups also or -Popups only adds the popups (flyouts, dialogs, pickers), which are left out by default.
  -Demo deploys the app with its demo page (a button per popup, and dark or light) and leaves it
  open in the emulator, for screenshots by hand. It captures nothing.
  -Grab holds each popup open for 5 s while vm\grab-loop.ps1 grabs the emulator's window, and
  writes <id>__screen.png beside each popup's dumps. Use it with -Popups only, with the
  emulator's zoom at 100%, so the window is 1080 x 1920, one pixel per phone pixel.

  Device indexes come from AppDeployCmd /EnumerateDevices: 7 to 13 are the 8.1 Update 1 profiles,
  10 is 720p 4.7", 13 is qHD 5". See R:\cornflower-reference\wp81-emulator-profiles.md.
#>
param(
    [int]$Device = 11,
    [string]$Only = '',
    [ValidateSet('', 'also', 'only')][string]$Popups = '',
    [switch]$Demo,
    [switch]$Grab,
    [string]$HostAlias = 'cornflower-ref',
    [string]$Out = 'R:\cornflower-reference\refapp',
    [int]$TimeoutMinutes = 40
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$remote = 'C:\cornflower\refapp'
$task = 'cornflower-refapp'
[xml]$manifest = Get-Content -Raw (Join-Path $here 'app\Package.appxmanifest')
$productId = $manifest.Package.PhoneIdentity.PhoneProductId

function Remote([string]$Script) {
    $full = "`$ProgressPreference = 'SilentlyContinue'`n" + $Script
    $b64 = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($full))
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $output = & ssh -o LogLevel=ERROR $HostAlias "powershell -NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand $b64"
    $code = $LASTEXITCODE
    $ErrorActionPreference = $previous
    if ($code -ne 0) {
        $output | ForEach-Object { Write-Host "  $_" }
        throw "remote step failed with exit code $code"
    }

    return $output
}

function Copy-Scp([string[]]$Arguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & scp -q -o LogLevel=ERROR -r @Arguments
    $code = $LASTEXITCODE
    $ErrorActionPreference = $previous
    if ($code -ne 0) { throw "scp failed with exit code $code" }
}

Write-Host "1/5 copying the app to ${HostAlias}:$remote"
Remote "Remove-Item '$remote\app','$remote\vm','$remote\out','$remote\status.txt' -Recurse -Force -ErrorAction SilentlyContinue; New-Item -ItemType Directory -Force '$remote' | Out-Null" | Out-Null
$options = Join-Path $here 'app\run-options.txt'
$lines = @()
if ($Only) { $lines += "only=$Only" }
if ($Popups) { $lines += "popups=$Popups" }
if ($Demo) { $lines += 'demo' }
if ($Grab) { $lines += 'hold=5000' }
if ($lines) { Set-Content -Path $options -Value $lines -Encoding ASCII } else { Remove-Item $options -ErrorAction SilentlyContinue }
try {
    Copy-Scp @((Join-Path $here 'app'), (Join-Path $here 'vm'), "${HostAlias}:C:/cornflower/refapp/")
}
finally {
    Remove-Item $options -ErrorAction SilentlyContinue
}

Write-Host '2/5 building with MSBuild 14'
$build = @(Remote "& '$remote\vm\build.ps1' -Root '$remote'; exit `$LASTEXITCODE")
if (-not ($build -match '^built ')) {
    $build | ForEach-Object { Write-Host "  $_" }
    throw 'the build did not produce an .appx'
}

$build | Select-Object -Last 3 | ForEach-Object { Write-Host "  $_" }

Write-Host "3/5 starting the session task for device $Device"
$taskArgs = "-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File $remote\vm\session.ps1 -Device $Device -ProductId $productId -Root $remote -TimeoutMinutes $TimeoutMinutes$(if ($Demo) { ' -Demo' })$(if ($Grab) { ' -Grab' })"
Remote @"
`$a = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument '$taskArgs'
`$p = New-ScheduledTaskPrincipal -UserId `$env:USERNAME -LogonType Interactive -RunLevel Highest
`$s = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Minutes $($TimeoutMinutes + 15))
Register-ScheduledTask -TaskName '$task' -Action `$a -Principal `$p -Settings `$s -Force | Out-Null
Start-ScheduledTask -TaskName '$task'
"@ | Out-Null

Write-Host '4/5 waiting for the app (a cold emulator takes about 2.5 minutes to boot)'
$deadline = (Get-Date).AddMinutes($TimeoutMinutes + 10)
$last = ''
do {
    Start-Sleep -Seconds 20
    $status = (Remote "Get-Content '$remote\status.txt' -ErrorAction SilentlyContinue") -join ' '
    if ($status -ne $last) {
        Write-Host "  $(Get-Date -Format HH:mm:ss) $status"
        $last = $status
    }
} until ($status -match '^(done|failed)' -or (Get-Date) -gt $deadline)

Remote "Unregister-ScheduledTask -TaskName '$task' -Confirm:`$false -ErrorAction SilentlyContinue" | Out-Null
if ($Demo -and $status -match '^done') {
    Write-Host '  the demo is open in the emulator on the VM'
    return
}

if ($status -notmatch '^done') {
    if ($status -match 'partial results pulled') {
        $partial = Join-Path $Out ("{0}-dev{1}-partial" -f (Get-Date -Format 'yyyyMMdd-HHmm'), $Device)
        New-Item -ItemType Directory -Force $partial | Out-Null
        Copy-Scp @("${HostAlias}:C:/cornflower/refapp/out", "${HostAlias}:C:/cornflower/refapp/session.log", $partial)
        Write-Host "  partial results in $partial"
    }

    Write-Host '  session.log on the VM:'
    Remote "Get-Content '$remote\session.log' -Tail 20" | ForEach-Object { Write-Host "    $_" }
    throw "the run did not finish: $status"
}

$dest = Join-Path $Out ("{0}-dev{1}" -f (Get-Date -Format 'yyyyMMdd-HHmm'), $Device)
Write-Host "5/5 copying the results to $dest"
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Scp @("${HostAlias}:C:/cornflower/refapp/out", "${HostAlias}:C:/cornflower/refapp/session.log", $dest)
$store = Join-Path $dest 'out\IsolatedStore'
Get-ChildItem $store | Move-Item -Destination $dest
Remove-Item (Join-Path $dest 'out') -Recurse -Force

if ($Grab) {
    # Each held popup gets the last distinct frame grabbed inside its hold, the steady frame, grabbed after the previous popup closed.
    Copy-Scp @("${HostAlias}:C:/cornflower/refapp/frames", $dest)
    $frames = Get-ChildItem (Join-Path $dest 'frames') -Filter '*.png' | ForEach-Object {
        [pscustomobject]@{ File = $_.FullName; At = [DateTimeOffset]::ParseExact($_.BaseName, 'yyyyMMddTHHmmssfff', [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AssumeUniversal) }
    } | Sort-Object At
    $picked = 0; $prevTo = [DateTimeOffset]::MinValue
    foreach ($line in (Get-Content (Join-Path $dest 'held.txt') -ErrorAction SilentlyContinue)) {
        $id, $from, $to = $line -split "`t"
        $hi = [DateTimeOffset]::Parse($to).AddSeconds(-1)
        $frame = $frames | Where-Object { $_.At -le $hi } | Select-Object -Last 1
        if ($frame -and $frame.At -gt $prevTo) { Copy-Item $frame.File (Join-Path $dest "$id`__screen.png"); $picked++ }
        else { Write-Host "  no frame for $id" }
        $prevTo = [DateTimeOffset]::Parse($to)
    }
    Write-Host "  $picked screen grabs picked from $(@($frames).Count) frames"
}

$index = Get-Content -Raw (Join-Path $dest 'index.json') | ConvertFrom-Json
$html = New-Object System.Text.StringBuilder
[void]$html.Append(@"
<!doctype html>
<meta charset="utf-8">
<title>Cornflower refapp $(Split-Path -Leaf $dest)</title>
<style>
body { background: #1e1e1e; color: #ddd; font: 13px "Segoe UI", sans-serif; margin: 24px; }
h2 { margin: 28px 0 6px; font-weight: 600; }
td, th { vertical-align: top; padding: 3px 8px; text-align: left; }
img { width: 240px; border: 1px solid #444; display: block; }
.missing { color: #f66; }
code { color: #9cdcfe; }
</style>
<h1>Cornflower reference app: $(Split-Path -Leaf $dest)</h1>
<p>Raw pixels per view pixel <code>$($index.display.rawPixelsPerViewPixel)</code> (<code>ResolutionScale</code> $($index.display.resolutionScale)), window <code>$($index.display.windowBounds -join ' x ')</code>,
captures <code>$($index.captureCount)</code>, errors <code>$($index.errors.Count)</code>.
Accent dark <code>$($index.resources.dark.PhoneAccentBrush)</code>,
page background dark <code>$($index.resources.dark.ApplicationPageBackgroundThemeBrush)</code>
light <code>$($index.resources.light.ApplicationPageBackgroundThemeBrush)</code>.
Each PNG has a JSON beside it with the full template tree.</p>
"@)
foreach ($e in $index.errors) { [void]$html.Append("<p class=`"missing`">$([System.Net.WebUtility]::HtmlEncode($e))</p>`n") }

$groups = $index.captures | Group-Object { "$($_.specimen)|$($_.variant)" }
foreach ($g in $groups) {
    $first = $g.Group[0]
    [void]$html.Append("<h2>$($first.specimen) <small>$($first.variant)</small></h2>`n<table><tr><th>state</th><th>dark</th><th>light</th></tr>`n")
    $rows = $g.Group | Group-Object { if ($_.state) { "$($_.group).$($_.state)" } else { 'base' } }
    foreach ($r in $rows) {
        $dark = ($r.Group | Where-Object { $_.theme -eq 'Dark' } | Select-Object -First 1)
        $light = ($r.Group | Where-Object { $_.theme -eq 'Light' } | Select-Object -First 1)
        $cell = { param($c) if ($c) { "<a href=`"$($c.file -replace '\.png$', '.json')`"><img src=`"$($c.file)`" loading=`"lazy`"></a>" } else { '<span class="missing">missing</span>' } }
        $note = if ($r.Group | Where-Object { $_.state -and -not $_.stateApplied }) { ' <span class="missing">(GoToState returned false)</span>' } else { '' }
        [void]$html.Append("<tr><td>$($r.Name)$note</td><td>$(& $cell $dark)</td><td>$(& $cell $light)</td></tr>`n")
    }

    [void]$html.Append("</table>`n")
}

Set-Content -Path (Join-Path $dest 'index.html') -Value $html.ToString() -Encoding UTF8
Write-Host "  $($index.captureCount) captures, $($index.errors.Count) errors"
Write-Host "  open $(Join-Path $dest 'index.html')"
