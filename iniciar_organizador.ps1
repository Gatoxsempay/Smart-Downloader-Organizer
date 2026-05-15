$pythonw = "$env:LOCALAPPDATA\Microsoft\WindowsApps\pythonw.exe"
$script  = Join-Path $PSScriptRoot "organizador_descargas.py"

if (-not (Test-Path $pythonw)) {
    [System.Windows.Forms.MessageBox]::Show("No se encontró pythonw.exe`n$pythonw", "Organizador")
    exit 1
}

# Detener instancia anterior
Get-Process -Name pythonw -ErrorAction SilentlyContinue | Stop-Process -Force

Start-Sleep -Milliseconds 500

# Iniciar nueva instancia sin ventana
Start-Process -FilePath $pythonw -ArgumentList "`"$script`"" -WindowStyle Hidden
