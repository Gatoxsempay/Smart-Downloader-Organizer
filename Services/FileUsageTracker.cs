using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrganizadorDescargas.Services;

public sealed class FileUsageTracker
{
    static readonly string DataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OrganizadorDescargas", "file_usage.json");

    readonly ConcurrentDictionary<string, int> _counts;

    public FileUsageTracker()
    {
        try
        {
            if (File.Exists(DataPath))
            {
                var dict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, int>>(
                               File.ReadAllText(DataPath));
                if (dict != null)
                {
                    _counts = new ConcurrentDictionary<string, int>(dict, StringComparer.OrdinalIgnoreCase);
                    return;
                }
            }
        }
        catch { }
        _counts = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    public int  GetCount(string path) => _counts.TryGetValue(path, out int c) ? c : 0;

    public void RecordOpen(string path)
    {
        _counts.AddOrUpdate(path, 1, (_, old) => old + 1);
        _ = Task.Run(Flush);
    }

    void Flush()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DataPath)!);
            File.WriteAllText(DataPath, JsonSerializer.Serialize(
                new System.Collections.Generic.Dictionary<string, int>(_counts)));
        }
        catch { }
    }
}
