# instalar_inicio_automatico.ps1
# Ejecutar UNA sola vez como usuario normal (no requiere administrador).
# Crea un acceso directo en la carpeta Startup de Windows para que el
# organizador arranque automáticamente al iniciar sesión.

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$pywFile    = Join-Path $scriptDir "organizador_descargas.pyw"
$startupDir = [Environment]::GetFolderPath("Startup")
$shortcut   = Join-Path $startupDir "OrganizadorDescargas.lnk"

if (-not (Test-Path $pywFile)) {
    Write-Error "No se encontró organizador_descargas.pyw en $scriptDir"
    exit 1
}

$pythonw = (Get-Command pythonw.exe -ErrorAction SilentlyContinue)?.Source
if (-not $pythonw) {
    # Intentar ruta estándar del instalador de usuario
    $pythonw = "$env:LOCALAPPDATA\Programs\Python\Python3*\pythonw.exe" |
               Resolve-Path -ErrorAction SilentlyContinue |
               Select-Object -ExpandProperty Path -First 1
}
if (-not $pythonw) {
    Write-Error "pythonw.exe no encontrado. Asegúrate de que Python esté instalado y en el PATH."
    exit 1
}

$wsh  = New-Object -ComObject WScript.Shell
$link = $wsh.CreateShortcut($shortcut)
$link.TargetPath       = $pythonw
$link.Arguments        = "`"$pywFile`""
$link.WorkingDirectory = $scriptDir
$link.WindowStyle      = 7   # Minimized
$link.Description      = "Organizador de Descargas"
$link.Save()

Write-Host "Acceso directo creado en:" -ForegroundColor Green
Write-Host "  $shortcut"
Write-Host ""
Write-Host "El organizador se iniciará automáticamente la próxima vez que abras sesión."
Write-Host "Para desactivarlo, elimina el acceso directo de esa carpeta o ejecuta:"
Write-Host "  Remove-Item '$shortcut'"
