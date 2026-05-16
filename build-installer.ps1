param(
    [string]$Version = "2.3.0"
)

$ErrorActionPreference = "Stop"
$ProjectDir   = $PSScriptRoot
$PublishDir   = "$ProjectDir\publish\win-x64"
$InstallerIss = "$ProjectDir\installer\setup.iss"
$ReleaseDir   = "$ProjectDir\release"
$Iscc         = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Write-Host ""
Write-Host "=== Build Instalador v$Version ===" -ForegroundColor Cyan

# 1. Publicar la app
Write-Host ""
Write-Host "[1/2] Publicando la app (win-x64, self-contained)..." -ForegroundColor Yellow
dotnet publish "$ProjectDir\OrganizadorDescargas.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $PublishDir `
    /p:PublishSingleFile=false

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error en la publicación." -ForegroundColor Red
    exit 1
}

# 2. Compilar installer con Inno Setup
Write-Host ""
Write-Host "[2/2] Compilando installer con Inno Setup..." -ForegroundColor Yellow

if (-not (Test-Path $Iscc)) {
    Write-Host ""
    Write-Host "Inno Setup no encontrado en: $Iscc" -ForegroundColor Red
    Write-Host "Descárgalo gratis desde: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    exit 1
}

New-Item -ItemType Directory -Force -Path $ReleaseDir | Out-Null
& $Iscc $InstallerIss /DAppVersion=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error compilando el installer." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Listo!" -ForegroundColor Green
Write-Host "Installer: $ReleaseDir\OrganizadorDescargas-Setup-v$Version.exe" -ForegroundColor Green
