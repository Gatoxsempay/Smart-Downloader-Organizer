@echo off
REM Lanzador — delega en PowerShell para evitar problemas de encoding con la ruta
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0iniciar_organizador.ps1"
