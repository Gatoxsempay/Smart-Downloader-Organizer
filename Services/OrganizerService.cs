using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrganizadorDescargas.Services;

public record LogEntry(DateTime Time, string Message, bool IsError = false);
public record FileMoved(string FileName, string Category, string Destination);

public class OrganizerService
{
    private readonly ConfigService _config;
    private FileSystemWatcher? _watcher;
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public event Action<LogEntry>? LogEmitted;
    public event Action<FileMoved>? FileMoved;
    public event Action<bool>? StatusChanged;

    public OrganizerService(ConfigService config)
    {
        _config = config;
    }

    public void Start()
    {
        if (_isRunning) return;
        var folder = _config.Get().MonitoredFolder;
        if (!Directory.Exists(folder))
        {
            Log($"Carpeta no encontrada: {folder}", true);
            return;
        }

        if (_config.Get().OrganizeOnStart)
            Task.Run(OrganizeExisting);

        _watcher = new FileSystemWatcher(folder)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true,
        };
        _watcher.Created += OnFileCreated;
        _watcher.Renamed += OnFileRenamed;

        _isRunning = true;
        StatusChanged?.Invoke(true);
        Log("Monitoreo iniciado.");
    }

    public void Stop()
    {
        if (!_isRunning) return;
        _watcher?.Dispose();
        _watcher = null;
        _isRunning = false;
        StatusChanged?.Invoke(false);
        Log("Monitoreo detenido.");
    }

    public void OrganizeNow()
    {
        Task.Run(OrganizeExisting);
    }

    public void Reverse()
    {
        Task.Run(ReverseOrganize);
    }

    // ── file events ──────────────────────────────────────────────────────────

    private void OnFileCreated(object _, FileSystemEventArgs e)
    {
        Task.Run(() => TryMoveFile(e.FullPath));
    }

    private void OnFileRenamed(object _, RenamedEventArgs e)
    {
        Task.Run(() => TryMoveFile(e.FullPath));
    }

    private void OrganizeExisting()
    {
        var cfg = _config.Get();
        var folder = cfg.MonitoredFolder;
        if (!Directory.Exists(folder)) return;
        foreach (var file in Directory.GetFiles(folder))
            TryMoveFile(file);
    }

    private void TryMoveFile(string path)
    {
        // Short wait for file to finish writing
        Thread.Sleep(500);
        if (!File.Exists(path)) return;

        var cfg = _config.Get();
        var name = Path.GetFileName(path).ToLowerInvariant();
        var ext = Path.GetExtension(path).ToLowerInvariant();

        if (cfg.ExcludedExtensions.Contains(ext)) return;
        if (cfg.ExcludedNames.Contains(name)) return;

        if (!cfg.ExtensionRules.TryGetValue(ext, out var category)) return;

        var destDir = Path.Combine(cfg.MonitoredFolder, category);
        Directory.CreateDirectory(destDir);

        var destPath = NoCollision(destDir, Path.GetFileName(path));
        try
        {
            File.Move(path, destPath);
            _config.IncrementStat(category);
            var entry = new FileMoved(Path.GetFileName(path), category, destPath);
            FileMoved?.Invoke(entry);
            Log($"Movido: {Path.GetFileName(path)} → {category}/{Path.GetFileName(destPath)}");
        }
        catch (Exception ex)
        {
            Log($"Error al mover {Path.GetFileName(path)}: {ex.Message}", true);
        }
    }

    private void ReverseOrganize()
    {
        var folder = _config.Get().MonitoredFolder;
        if (!Directory.Exists(folder)) return;
        Log("Iniciando restauración...");

        foreach (var subDir in Directory.GetDirectories(folder))
        {
            foreach (var file in Directory.GetFiles(subDir))
            {
                var dest = NoCollision(folder, Path.GetFileName(file));
                try
                {
                    File.Move(file, dest);
                    Log($"Restaurado: {Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    Log($"Error al restaurar {Path.GetFileName(file)}: {ex.Message}", true);
                }
            }
            try
            {
                if (!Directory.EnumerateFileSystemEntries(subDir).Any())
                    Directory.Delete(subDir);
            }
            catch { }
        }
        Log("Restauración completada.");
    }

    private static string NoCollision(string dir, string fileName)
    {
        var dest = Path.Combine(dir, fileName);
        if (!File.Exists(dest)) return dest;

        var nameNoExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var i = 1;
        do
        {
            dest = Path.Combine(dir, $"{nameNoExt} ({i}){ext}");
            i++;
        } while (File.Exists(dest));
        return dest;
    }

    private void Log(string msg, bool isError = false)
    {
        LogEmitted?.Invoke(new LogEntry(DateTime.Now, msg, isError));
    }
}
