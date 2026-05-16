using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace OrganizadorDescargas.Services;

public class AppConfig
{
    public string MonitoredFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    public bool AutoStartMonitoring { get; set; } = true;
    public bool OrganizeOnStart { get; set; } = true;
    public string Theme { get; set; } = "dark";
    public string UpdateCheckUrl { get; set; } = "https://api.github.com/repos/Gatoxsempay/Smart-Downloader-Organizer/releases/latest";
    public Dictionary<string, string> ExtensionRules { get; set; } = DefaultRules();
    public List<string> ExcludedExtensions { get; set; } = [".crdownload", ".tmp", ".part", ".download", ".opdownload"];
    public List<string> ExcludedNames { get; set; } = ["desktop.ini", "thumbs.db", ".ds_store", "organizador.log"];
    public List<string> ExcludedPatterns { get; set; } = [];
    public AppStats Stats { get; set; } = new();

    public static Dictionary<string, string> DefaultRules() => new()
    {
        { ".pdf", "Documentos" }, { ".docx", "Documentos" }, { ".doc", "Documentos" },
        { ".txt", "Documentos" }, { ".pptx", "Documentos" }, { ".ppt", "Documentos" },
        { ".xlsx", "Documentos" }, { ".xls", "Documentos" }, { ".csv", "Documentos" },
        { ".odt", "Documentos" }, { ".rtf", "Documentos" },
        { ".jpg", "Imágenes" }, { ".jpeg", "Imágenes" }, { ".png", "Imágenes" },
        { ".gif", "Imágenes" }, { ".webp", "Imágenes" }, { ".svg", "Imágenes" },
        { ".bmp", "Imágenes" }, { ".ico", "Imágenes" }, { ".tiff", "Imágenes" },
        { ".mp4", "Videos" }, { ".mkv", "Videos" }, { ".mov", "Videos" },
        { ".avi", "Videos" }, { ".wmv", "Videos" }, { ".flv", "Videos" }, { ".webm", "Videos" },
        { ".mp3", "Audio" }, { ".wav", "Audio" }, { ".flac", "Audio" },
        { ".aac", "Audio" }, { ".ogg", "Audio" }, { ".wma", "Audio" },
        { ".exe", "Instaladores" }, { ".msi", "Instaladores" }, { ".msix", "Instaladores" },
        { ".appx", "Instaladores" }, { ".dmg", "Instaladores" }, { ".deb", "Instaladores" },
        { ".zip", "Comprimidos" }, { ".rar", "Comprimidos" }, { ".7z", "Comprimidos" },
        { ".tar", "Comprimidos" }, { ".gz", "Comprimidos" }, { ".xz", "Comprimidos" },
        { ".py", "Código" }, { ".js", "Código" }, { ".ts", "Código" },
        { ".cs", "Código" }, { ".cpp", "Código" }, { ".java", "Código" },
        { ".go", "Código" }, { ".rs", "Código" }, { ".html", "Código" },
        { ".css", "Código" }, { ".php", "Código" }, { ".rb", "Código" },
        { ".psd", "Diseño" }, { ".ai", "Diseño" }, { ".xd", "Diseño" },
        { ".fig", "Diseño" }, { ".sketch", "Diseño" },
    };
}

public class AppStats
{
    public int TotalMoved { get; set; } = 0;
    public Dictionary<string, int> ByCategory { get; set; } = [];
}

public class ConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OrganizadorDescargas");
    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    private readonly Lock _lock = new();
    private AppConfig _config = new();
    private int _version = 0;

    public int Version => _version;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public ConfigService()
    {
        Directory.CreateDirectory(ConfigDir);
        Load();
    }

    private void Load()
    {
        if (!File.Exists(ConfigFile))
        {
            Save();
            return;
        }
        try
        {
            var json = File.ReadAllText(ConfigFile);
            var stored = JsonSerializer.Deserialize<AppConfig>(json, JsonOpts);
            if (stored != null)
            {
                _config = stored;
                // Merge default rules so new defaults always apply
                foreach (var kv in AppConfig.DefaultRules())
                    _config.ExtensionRules.TryAdd(kv.Key, kv.Value);
            }
        }
        catch { Save(); }
    }

    public AppConfig Get()
    {
        lock (_lock) return _config;
    }

    public void Save()
    {
        lock (_lock)
        {
            Directory.CreateDirectory(ConfigDir);
            File.WriteAllText(ConfigFile, JsonSerializer.Serialize(_config, JsonOpts));
        }
    }

    public void Update(Action<AppConfig> mutate)
    {
        lock (_lock)
        {
            mutate(_config);
            _version++;
        }
        Save();
    }

    public void IncrementStat(string category)
    {
        lock (_lock)
        {
            _config.Stats.TotalMoved++;
            _config.Stats.ByCategory.TryGetValue(category, out var count);
            _config.Stats.ByCategory[category] = count + 1;
            _version++;
        }
        Save();
    }
}
