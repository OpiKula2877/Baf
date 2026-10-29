param([string]$Version = '1.0.0')

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

dotnet publish src\Pripominka -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true -p:Version=$Version -o publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish selhal' }

$candidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$iscc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 (ISCC.exe) nebyl nalezen' }

& $iscc "/DAppVersion=$Version" installer\Pripominka.iss
if ($LASTEXITCODE -ne 0) { throw 'Kompilace instalatoru selhala' }

Write-Host "Hotovo: dist\Pripominka-Setup-$Version.exe"
