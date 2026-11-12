# Windows only: runs on the reference VM. Builds the reference app with MSBuild 14 into app\AppPackages\.
# Called over SSH by ..\run.ps1. Needs Visual Studio 2015 with the Windows Phone 8.1 tools.
param([string]$Root = 'C:\cornflower\refapp')

$ErrorActionPreference = 'Continue'
$ProgressPreference = 'SilentlyContinue'
$app = Join-Path $Root 'app'
Remove-Item (Join-Path $app 'AppPackages') -Recurse -Force -ErrorAction SilentlyContinue

$msbuild = 'C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe'
& $msbuild (Join-Path $app 'RefApp.csproj') /nologo /v:m /p:Configuration=Debug /p:Platform=x86 /p:VisualStudioVersion=14.0
$code = $LASTEXITCODE

$appx = Get-ChildItem (Join-Path $app 'AppPackages') -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Extension -eq '.appx' } | Select-Object -First 1
if ($code -ne 0 -or -not $appx) {
    "build failed (MSBuild exit $code)"
    exit 1
}

"built $($appx.FullName)"
exit 0
