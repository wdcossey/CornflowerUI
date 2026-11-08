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
    [int]$TimeoutMinutes = 40
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

$started = Get-Date
Status 'running: app launched, waiting for done.txt'
$deadline = $started.AddMinutes($TimeoutMinutes)
$done = $false
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 20
    $listing = (& $ise dir "deviceindex:$Device" $ProductId 2>&1) -join "`n"
    if ($listing -match 'done\.txt') {
        $done = $true
        break
    }

    $pngs = ([regex]::Matches($listing, '\.png')).Count
    Status ('running: {0} PNGs so far after {1:N0} s' -f $pngs, ((Get-Date) - $started).TotalSeconds)
}

if (-not $done) {
    Status "failed: no done.txt after $TimeoutMinutes minutes"
    exit 1
}

$out = Join-Path $Root 'out'
Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $out | Out-Null
Status "pulling the app's local folder"
& $ise ts "deviceindex:$Device" $ProductId $out 2>&1 | Add-Content -Path $log

$count = (Get-ChildItem $out -Recurse -File).Count
$summary = Get-Content (Join-Path $out 'IsolatedStore\done.txt') -ErrorAction SilentlyContinue
Status ('done: pulled {0} files; app reports {1}; {2:N0} s in the app' -f $count, $summary, ((Get-Date) - $started).TotalSeconds)
