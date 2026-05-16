using OrganizadorDescargas.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OrganizadorDescargas.Services;

public class FolderOrganizerService(ConfigService config)
{
    public async Task<ScanResult> ScanAsync(string folderPath, Dictionary<string, string>? extraRules = null)
    {
        var items = new List<FilePreviewItem>();
        var rules = config.Get().ExtensionRules;

        await Task.Run(() =>
        {
            foreach (var filePath in Directory.EnumerateFiles(folderPath))
            {
                var name = Path.GetFileName(filePath);
                var ext = Path.GetExtension(filePath).ToLowerInvariant();

                rules.TryGetValue(ext, out var category);
                if (category == null && extraRules != null)
                    extraRules.TryGetValue(ext, out category);

                var dest = category != null
                    ? Path.Combine(folderPath, category, name)
                    : null;

                items.Add(new FilePreviewItem(filePath, name, ext, category, dest));
            }
        });

        return new ScanResult(folderPath, items);
    }

    public async Task<int> ApplyAsync(ScanResult scan)
    {
        int moved = 0;
        await Task.Run(() =>
        {
            foreach (var item in scan.Files)
            {
                if (item.Category == null || item.Destination == null) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(item.Destination)!);
                var dest = NoCollision(item.Destination);
                File.Move(item.SourcePath, dest);
                moved++;
            }
        });
        return moved;
    }

    private static string NoCollision(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        int i = 1;
        string candidate;
        do { candidate = Path.Combine(dir, $"{name} ({i++}){ext}"); }
        while (File.Exists(candidate));
        return candidate;
    }
}
