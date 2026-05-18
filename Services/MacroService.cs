using OrganizadorDescargas.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrganizadorDescargas.Services;

public sealed class MacroService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OrganizadorDescargas", "macros_config.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public List<MacroDefinition> Load()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return Defaults();
            var json = File.ReadAllText(ConfigPath);
            var list = JsonSerializer.Deserialize<List<MacroDefinition>>(json, JsonOpts);
            return list is { Count: > 0 } ? list : Defaults();
        }
        catch { return Defaults(); }
    }

    public void Save(List<MacroDefinition> macros)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(macros, JsonOpts));
        }
        catch { }
    }

    public async Task<(int ExitCode, string Output)> RunAsync(MacroDefinition macro)
    {
        var sb       = new StringBuilder();
        int exitCode = 0;

        var commands = macro.Actions
            .Select(a => a.ToPowerShell())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        await Task.Run(() =>
        {
            foreach (var cmd in commands)
            {
                try
                {
                    // Use EncodedCommand to avoid escaping issues
                    var bytes   = Encoding.Unicode.GetBytes(cmd);
                    var encoded = Convert.ToBase64String(bytes);

                    var psi = new ProcessStartInfo("powershell.exe",
                        $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}")
                    {
                        UseShellExecute        = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true,
                        CreateNoWindow         = true,
                    };

                    using var proc = Process.Start(psi)!;
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();
                    exitCode = proc.ExitCode;

                    if (!string.IsNullOrWhiteSpace(stdout)) sb.AppendLine(stdout.TrimEnd());
                    if (!string.IsNullOrWhiteSpace(stderr)) sb.AppendLine($"[stderr] {stderr.TrimEnd()}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"[error] {ex.Message}");
                    exitCode = -1;
                }
            }
        });

        return (exitCode, sb.ToString().TrimEnd());
    }

    private static List<MacroDefinition> Defaults() =>
    [
        new MacroDefinition
        {
            Name    = "Limpiar temporales",
            Glyph   = "",
            Actions =
            [
                new MacroAction { Type = MacroActionType.CleanTemp },
            ],
        },
        new MacroDefinition
        {
            Name    = "Vaciar papelera",
            Glyph   = "",
            Actions =
            [
                new MacroAction { Type = MacroActionType.EmptyRecycleBin },
            ],
        },
    ];
}
