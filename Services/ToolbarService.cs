using OrganizadorDescargas.Models;
using System;
using System.IO;
using System.Text.Json;

namespace OrganizadorDescargas.Services;

public sealed class ToolbarService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OrganizadorDescargas", "toolbar_config.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public ToolbarConfig Load()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return new();
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<ToolbarConfig>(json, JsonOpts) ?? new();
        }
        catch { return new(); }
    }

    public void Save(ToolbarConfig config)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, JsonOpts));
        }
        catch { }
    }
}
