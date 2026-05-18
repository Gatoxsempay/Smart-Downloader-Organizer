using System;
using System.Collections.Generic;
using System.IO;

namespace OrganizadorDescargas.Models;

public enum MacroActionType
{
    OpenFolder,
    OpenApp,
    OpenUrl,
    CleanTemp,
    EmptyRecycleBin,
    Shutdown,
    Restart,
    Sleep,
    LockScreen,
}

public sealed class MacroAction
{
    public MacroActionType Type  { get; set; }
    public string          Param { get; set; } = "";

    public string GetLabel() => Type switch
    {
        MacroActionType.OpenFolder      => string.IsNullOrEmpty(Param) ? "Abrir carpeta" : $"Abrir carpeta: {Path.GetFileName(Param.TrimEnd('\\', '/'))}",
        MacroActionType.OpenApp         => string.IsNullOrEmpty(Param) ? "Abrir programa" : $"Abrir: {Path.GetFileNameWithoutExtension(Param)}",
        MacroActionType.OpenUrl         => string.IsNullOrEmpty(Param) ? "Abrir web" : $"Abrir web: {Param}",
        MacroActionType.CleanTemp       => "Limpiar archivos temporales",
        MacroActionType.EmptyRecycleBin => "Vaciar papelera de reciclaje",
        MacroActionType.Shutdown        => "Apagar el equipo",
        MacroActionType.Restart         => "Reiniciar el equipo",
        MacroActionType.Sleep           => "Suspender el equipo",
        MacroActionType.LockScreen      => "Bloquear la pantalla",
        _                               => "Acción desconocida",
    };

    public string GetGlyph() => Type switch
    {
        MacroActionType.OpenFolder      => "",
        MacroActionType.OpenApp         => "",
        MacroActionType.OpenUrl         => "",
        MacroActionType.CleanTemp       => "",
        MacroActionType.EmptyRecycleBin => "",
        MacroActionType.Shutdown        => "",
        MacroActionType.Restart         => "",
        MacroActionType.Sleep           => "",
        MacroActionType.LockScreen      => "",
        _                               => "",
    };

    public string ToPowerShell() => Type switch
    {
        MacroActionType.OpenFolder      => $"Start-Process 'explorer.exe' -ArgumentList '\"{Param}\"'",
        MacroActionType.OpenApp         => $"Start-Process '{Param}'",
        MacroActionType.OpenUrl         => $"Start-Process '{Param}'",
        MacroActionType.CleanTemp       => @"Remove-Item -Path ""$env:TEMP\*"" -Recurse -Force -ErrorAction SilentlyContinue",
        MacroActionType.EmptyRecycleBin => "Clear-RecycleBin -Force -ErrorAction SilentlyContinue",
        MacroActionType.Shutdown        => "Stop-Computer -Force",
        MacroActionType.Restart         => "Restart-Computer -Force",
        MacroActionType.Sleep           => @"& ""$env:windir\System32\rundll32.exe"" powrprof.dll,SetSuspendState 0,1,0",
        MacroActionType.LockScreen      => @"& ""$env:windir\System32\rundll32.exe"" user32.dll,LockWorkStation",
        _                               => "",
    };
}

public sealed class MacroDefinition
{
    public string            Id      { get; set; } = Guid.NewGuid().ToString();
    public string            Name    { get; set; } = "Nueva acción rápida";
    public string            Glyph   { get; set; } = "";
    public List<MacroAction> Actions { get; set; } = new();
}
