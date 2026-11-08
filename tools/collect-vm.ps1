<#
.SYNOPSIS
  Inventories a reference VM: what is installed, and where the design sources are.

.DESCRIPTION
  Read-only, apart from its own output folder. Works on Windows PowerShell 4.0 (Windows 8.1)
  and later. Finds the default control templates, theme resources, API metadata, emulator
  images and tools, and the Segoe fonts, and records each file's path, size, date and SHA-256.

  With -CopyReferenceFiles it also copies the XAML and .winmd files it found into the output
  folder, so they can be read from another machine. Those are Microsoft's files: keep the
  output outside any repository.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File collect-vm.ps1 -OutDir C:\cornflower\out

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File collect-vm.ps1 -OutDir C:\cornflower\out -CopyReferenceFiles
#>
param(
    [string]$OutDir = (Join-Path $env:TEMP 'cornflower-collect'),
    [switch]$CopyReferenceFiles
)

$ErrorActionPreference = 'Stop'

$pf86 = ${env:ProgramFiles(x86)}
if (-not $pf86) { $pf86 = $env:ProgramFiles }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$runDir = Join-Path $OutDir ("{0}-{1}" -f $env:COMPUTERNAME, $stamp)
New-Item -ItemType Directory -Force -Path $runDir | Out-Null

function Get-FileRecord {
    param([System.IO.FileInfo]$File, [string]$Kind)
    $hash = $null
    try { $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $File.FullName).Hash } catch { $hash = "unreadable: $($_.Exception.Message)" }
    New-Object psobject -Property ([ordered]@{
        kind     = $Kind
        path     = $File.FullName
        bytes    = $File.Length
        modified = $File.LastWriteTimeUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')
        sha256   = $hash
    })
}

# Every file under the roots, listed once. -Include is ignored with -LiteralPath in Windows
# PowerShell, so names are matched here instead.
$script:allFiles = $null
function Find-Files {
    param([string[]]$Roots, [string[]]$Names, [string]$Kind)
    if ($null -eq $script:allFiles) {
        $script:allFiles = @(foreach ($root in $Roots) {
            if (Test-Path -LiteralPath $root) {
                Get-ChildItem -LiteralPath $root -Recurse -File -ErrorAction SilentlyContinue
            }
        })
    }
    foreach ($file in $script:allFiles) {
        foreach ($name in $Names) {
            if ($file.Name -like $name) { Get-FileRecord -File $file -Kind $Kind; break }
        }
    }
}

# Where SDKs and phone tools install. A root that doesn't exist is skipped and reported.
$roots = @(
    (Join-Path $pf86 'Windows Kits'),
    (Join-Path $pf86 'Windows Phone Kits'),
    (Join-Path $pf86 'Microsoft SDKs\Windows Phone'),
    (Join-Path $pf86 'Microsoft SDKs\WindowsPhoneApp'),
    (Join-Path $pf86 'Microsoft SDKs\Windows'),
    (Join-Path $pf86 'Microsoft XDE'),
    (Join-Path $env:ProgramFiles 'Microsoft XDE')
)

$system = New-Object psobject -Property ([ordered]@{
    computer      = $env:COMPUTERNAME
    collected     = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    os            = (Get-CimInstance Win32_OperatingSystem).Caption
    osVersion     = (Get-CimInstance Win32_OperatingSystem).Version
    osBuild       = (Get-CimInstance Win32_OperatingSystem).BuildNumber
    architecture  = (Get-CimInstance Win32_OperatingSystem).OSArchitecture
    powershell    = $PSVersionTable.PSVersion.ToString()
    cpu           = ((Get-CimInstance Win32_Processor) | Select-Object -First 1).Name.Trim()
    memoryGB      = [math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 1)
    hypervisor    = (Get-CimInstance Win32_ComputerSystem).HypervisorPresent
    hyperVService = $(try { (Get-Service vmms -ErrorAction Stop).Status.ToString() } catch { 'not installed' })
    sshdService   = $(try { (Get-Service sshd -ErrorAction Stop).Status.ToString() } catch { 'not installed' })
})

$rootReport = foreach ($r in $roots) {
    New-Object psobject -Property ([ordered]@{ root = $r; exists = (Test-Path -LiteralPath $r) })
}

Write-Host 'Searching for design sources...'
$files = @()
$files += Find-Files -Roots $roots -Names 'generic.xaml', 'themeresources.xaml' -Kind 'templates'
$files += Find-Files -Roots $roots -Names 'System.Windows.xaml', 'ThemeResources.xaml' -Kind 'silverlight-design' |
    Where-Object { $_.path -match 'Windows Phone' }
$files += Find-Files -Roots $roots -Names '*.winmd' -Kind 'api-metadata'
$files += Find-Files -Roots $roots -Names 'XDE.exe', 'AppDeployCmd.exe', 'XapDeployCmd.exe' -Kind 'tool'
$files += Find-Files -Roots $roots -Names '*.vhd', '*.vhdx' -Kind 'emulator-image'
# The same template file can match twice (themeresources.xaml under a phone root).
$files = @($files | Sort-Object path -Unique)

Write-Host 'Listing fonts...'
$fontDir = Join-Path $env:WINDIR 'Fonts'
$fonts = @(Get-ChildItem -LiteralPath $fontDir -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match '^(seg|selaw)' } |
    ForEach-Object { Get-FileRecord -File $_ -Kind 'font' })

# Visual Studio: the folders that exist, without assuming a version.
$vs = @(Get-ChildItem -LiteralPath $pf86 -Directory -Filter 'Microsoft Visual Studio*' -ErrorAction SilentlyContinue |
    ForEach-Object { $_.FullName })

$inventory = New-Object psobject -Property ([ordered]@{
    system       = $system
    roots        = @($rootReport)
    visualStudio = $vs
    files        = $files
    fonts        = $fonts
})

$jsonPath = Join-Path $runDir 'inventory.json'
$inventory | ConvertTo-Json -Depth 6 | Out-File -LiteralPath $jsonPath -Encoding utf8

# A readable summary beside the JSON.
$md = New-Object System.Collections.Generic.List[string]
$md.Add("# Inventory of $($system.computer)")
$md.Add('')
$md.Add("Collected $($system.collected).")
$md.Add('')
$md.Add('| | |')
$md.Add('|---|---|')
foreach ($p in $system.psobject.Properties) { $md.Add("| $($p.Name) | $($p.Value) |") }
$md.Add('')
$md.Add('## Search roots')
$md.Add('')
foreach ($r in $rootReport) { $md.Add("- $(if ($r.exists) { 'found  ' } else { 'missing' }) ``$($r.root)``") }
$md.Add('')
$md.Add('## Visual Studio folders')
$md.Add('')
if ($vs.Count) { foreach ($v in $vs) { $md.Add("- ``$v``") } } else { $md.Add('None.') }
foreach ($kind in 'templates', 'silverlight-design', 'api-metadata', 'tool', 'emulator-image') {
    $set = @($files | Where-Object { $_.kind -eq $kind })
    $md.Add('')
    $md.Add("## $kind ($($set.Count))")
    $md.Add('')
    if (-not $set.Count) { $md.Add('None found.'); continue }
    $md.Add('| Path | Bytes | Modified | SHA-256 |')
    $md.Add('|---|---|---|---|')
    foreach ($f in $set) { $md.Add("| ``$($f.path)`` | $($f.bytes) | $($f.modified) | ``$($f.sha256)`` |") }
}
$md.Add('')
$md.Add("## Segoe and Selawik fonts ($($fonts.Count))")
$md.Add('')
foreach ($f in $fonts) { $md.Add("- ``$([System.IO.Path]::GetFileName($f.path))`` $($f.bytes) bytes") }
$md | Out-File -LiteralPath (Join-Path $runDir 'inventory.md') -Encoding utf8

if ($CopyReferenceFiles) {
    Write-Host 'Copying reference files...'
    $refDir = Join-Path $runDir 'reference'
    $n = 0
    foreach ($f in ($files | Where-Object { $_.kind -in 'templates', 'silverlight-design', 'api-metadata' })) {
        # Keep the folder structure under the drive root, so two files of one name don't collide.
        $relative = $f.path -replace '^[A-Za-z]:\\', ''
        $target = Join-Path $refDir $relative
        # robocopy, not Copy-Item: the SDK's contract paths plus the output prefix pass the
        # 260-character limit that Copy-Item enforces on Windows PowerShell (seen 2026-11-07).
        $null = & robocopy (Split-Path -Parent $f.path) (Split-Path -Parent $target) (Split-Path -Leaf $f.path) /NJH /NJS /NFL /NDL /NP /R:1 /W:1
        if ($LASTEXITCODE -ge 8) { Write-Warning "robocopy failed ($LASTEXITCODE): $($f.path)" } else { $n++ }
    }
    Write-Host "Copied $n files to $refDir"
}

Write-Host "Done: $runDir"
Write-Host ("{0} files, {1} fonts" -f $files.Count, $fonts.Count)
