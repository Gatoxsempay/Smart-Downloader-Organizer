using System.Collections.Generic;
using System.Linq;

namespace OrganizadorDescargas.Models;

public record FilePreviewItem(
    string SourcePath,
    string FileName,
    string Extension,
    string? Category,
    string? Destination);

public class ScanResult(string folderPath, List<FilePreviewItem> files)
{
    public string FolderPath { get; } = folderPath;
    public List<FilePreviewItem> Files { get; } = files;
    public int TotalFiles => Files.Count;
    public int FilesWithCategory => Files.Count(f => f.Category != null);
    public int FilesWithoutCategory => Files.Count(f => f.Category == null);
    public IEnumerable<string> Categories => Files
        .Where(f => f.Category != null)
        .Select(f => f.Category!)
        .Distinct()
        .OrderBy(c => c);
}
